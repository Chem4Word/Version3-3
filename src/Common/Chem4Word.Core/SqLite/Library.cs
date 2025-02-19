// ---------------------------------------------------------------------------
//  Copyright (c) 2025, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.Core.Helpers;
using Chem4Word.Core.UI.Forms;
using IChem4Word.Contracts;
using IChem4Word.Contracts.Dto;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Point = System.Windows.Point;

namespace Chem4Word.Core.SqLite
{
    public class Library
    {
        private static readonly string _product = Assembly.GetExecutingAssembly().FullName.Split(',')[0];
        private static readonly string _class = MethodBase.GetCurrentMethod()?.DeclaringType?.Name;

        private readonly IChem4WordTelemetry _telemetry;
        private Point _topLeft;
        private FileInfo _databaseFile;
        private DirectoryInfo _backupDirectory;

        private List<string> _tables = new List<string>();
        private List<Patch> _patches = new List<Patch>();

        public DatabaseFileProperties Database { get; } = new DatabaseFileProperties();

        public Library(IChem4WordTelemetry telemetry, string fileName, string backupFolder, Point topLeft)
        {
            _telemetry = telemetry;
            _topLeft = topLeft;
            _databaseFile = new FileInfo(fileName);
            _backupDirectory = new DirectoryInfo(backupFolder);

            DetermineProperties();
        }

        public static void CreateNewDatabase(string filename)
        {
            SQLiteConnection.CreateFile(filename);
            var conn = new SQLiteConnection($"Data Source={filename};Synchronous=Full");
            conn.Open();

            var resource = ResourceHelper.GetStringResource(Assembly.GetExecutingAssembly(), "CreateV3-1Database.sql");
            var lines = resource.Split(Environment.NewLine.ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                if (!string.IsNullOrEmpty(line) && !line.StartsWith("--"))
                {
                    var command = new SQLiteCommand(line, conn);
                    command.ExecuteNonQuery();
                }
            }

            conn.Close();
        }

        #region Data I/O

        public SQLiteConnection LibraryConnection()
        {
            // Source https://www.connectionstrings.com/sqlite/
            var conn = new SQLiteConnection($"Data Source={_databaseFile.FullName};Synchronous=Full");
            return conn.OpenAndReturn();
        }

        /// <summary>
        /// This is called by LoadNamesFromLibrary in Add-In which happens on right click
        /// </summary>
        /// <returns></returns>
        public Dictionary<string, int> GetSubstanceNamesWithIds()
        {
            var module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod()?.Name}()";
            var allNames = new Dictionary<string, int>();

            try
            {
                var sw = new Stopwatch();
                sw.Start();

                using (var conn = LibraryConnection())
                {
                    using (var names = GetDistinctNamesCommand(conn))
                    {
                        if (names != null)
                        {
                            while (names.Read())
                            {
                                var name = names["Name"] as string;
                                // Exclude any names that are three characters or fewer
                                if (!string.IsNullOrEmpty(name) && name.Length > 3)
                                {
                                    var id = int.Parse(names["ChemistryId"].ToString());
                                    if (!allNames.ContainsKey(name))
                                    {
                                        allNames.Add(name, id);
                                    }
                                }
                            }
                        }
                    }
                }

                sw.Stop();
#if DEBUG
                // Task 810
                _telemetry.Write(module, "Timing", $"Reading {SafeDouble.AsString0(allNames.Count)} Chemical names from '{_databaseFile.Name}' took {SafeDouble.AsString0(sw.ElapsedMilliseconds)}ms");
#endif
            }
            catch (Exception ex)
            {
                using (var form = new ReportError(_telemetry, _topLeft, module, ex))
                {
                    form.ShowDialog();
                }
            }

            return allNames;
        }

        /// <summary>
        /// This is called via Microsoft.Office.Tools.CustomTaskPanel.OnVisibleChanged and Chem4Word.CustomRibbon.OnShowLibraryClick
        /// </summary>
        /// <returns></returns>
        public List<ChemistryDataObject> GetAllChemistry()
        {
            var module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod()?.Name}()";
            var results = new List<ChemistryDataObject>();

            try
            {
                var sw = new Stopwatch();

                using (var conn = LibraryConnection())
                {
                    sw.Start();

                    using (var chemistry = GetAllChemistryCommand(conn))
                    {
                        if (chemistry != null)
                        {
                            while (chemistry.Read())
                            {
                                var id = (long)chemistry["Id"];
                                var dto = new ChemistryDataObject
                                {
                                    Id = id,
                                    Chemistry = (byte[])chemistry["chemistry"],
                                    DataType = chemistry["datatype"] as string,
                                    Name = chemistry["name"] as string,
                                    Formula = chemistry["formula"] as string,
                                    Names = GetChemicalNamesCommand(conn, id),
                                    Formulae = GetChemicalFormulaeCommand(conn, id),
                                    Captions = GetChemicalCaptionsCommand(conn, id),
                                    Tags = GetChemistryTagsCommand(conn, id),
                                };

                                // Handle new field(s) which may be null
                                var molWeight = chemistry["molweight"].ToString();
                                if (!string.IsNullOrEmpty(molWeight)
                                    && SafeDouble.TryParse(molWeight, out var result))
                                {
                                    dto.MolWeight = result;
                                }
                                results.Add(dto);
                            }
                        }
                    }

                    var sb = new StringBuilder();
                    sb.AppendLine("SELECT DataType, COUNT(1) AS Frequency");
                    sb.AppendLine("FROM Gallery");
                    sb.AppendLine("GROUP BY DataType");

                    var command = new SQLiteCommand(sb.ToString(), conn);

                    var sb2 = new StringBuilder();

                    using (var reader = command.ExecuteReader())
                    {
                        if (reader != null)
                        {
                            while (reader.Read())
                            {
                                sb2.Append(reader["DataType"] as string);
                                sb2.Append(":");
                                sb2.Append((long)reader["Frequency"]);
                                sb2.Append(" ");
                            }
                        }
                    }

                    sw.Stop();
                    var taken = sw.ElapsedMilliseconds;

                    _telemetry?.Write(module, "Timing", $"Reading {SafeDouble.AsString0(results.Count)} structures [{sb2}] from '{_databaseFile.Name}' took {SafeDouble.AsString0(taken)}ms");
                }
            }
            catch (Exception ex)
            {
                using (var form = new ReportError(_telemetry, _topLeft, module, ex))
                {
                    form.ShowDialog();
                }

                results = null;
            }

            return results;
        }

        public long AddChemistry(ChemistryDataObject chemistry)
        {
            long result;

            using (var conn = LibraryConnection())
            {
                result = AddChemistryCommand(conn, chemistry);
            }

            return result;
        }

        private void AddNameCommand(SQLiteConnection conn, ChemistryNameDataObject name, long id)
        {
            var sb = new StringBuilder();

            sb.AppendLine("INSERT INTO ChemicalNames");
            sb.AppendLine(" (Name, Namespace, Tag, ChemistryID)");
            sb.AppendLine("VALUES");
            sb.AppendLine(" (@name, @namespace, @tag, @chemistryId)");

            var command = new SQLiteCommand(sb.ToString(), conn);
            command.Parameters.Add("@name", DbType.String, name.Name.Length).Value = name.Name;
            command.Parameters.Add("@namespace", DbType.String, name.NameSpace.Length).Value = name.NameSpace;
            command.Parameters.Add("@tag", DbType.String, name.Tag.Length).Value = name.Tag;
            command.Parameters.Add("@chemistryId", DbType.Int64).Value = id;

            command.ExecuteNonQuery();
        }

        private void AddFormulaCommand(SQLiteConnection conn, ChemistryNameDataObject formula, long id)
        {
            var sb = new StringBuilder();

            sb.AppendLine("INSERT INTO ChemicalFormulae");
            sb.AppendLine(" (Name, Namespace, Tag, ChemistryID)");
            sb.AppendLine("VALUES");
            sb.AppendLine(" (@name, @namespace, @tag, @chemistryId)");

            var command = new SQLiteCommand(sb.ToString(), conn);
            command.Parameters.Add("@name", DbType.String, formula.Name.Length).Value = formula.Name;
            command.Parameters.Add("@namespace", DbType.String, formula.NameSpace.Length).Value = formula.NameSpace;
            command.Parameters.Add("@tag", DbType.String, formula.Tag.Length).Value = formula.Tag;
            command.Parameters.Add("@chemistryId", DbType.Int64).Value = id;

            command.ExecuteNonQuery();
        }

        private void AddCaptionCommand(SQLiteConnection conn, ChemistryNameDataObject caption, long id)
        {
            var sb = new StringBuilder();

            sb.AppendLine("INSERT INTO ChemicalCaptions");
            sb.AppendLine(" (Name, Namespace, Tag, ChemistryID)");
            sb.AppendLine("VALUES");
            sb.AppendLine(" (@name, @namespace, @tag, @chemistryId)");

            var command = new SQLiteCommand(sb.ToString(), conn);
            command.Parameters.Add("@name", DbType.String, caption.Name.Length).Value = caption.Name;
            command.Parameters.Add("@namespace", DbType.String, caption.NameSpace.Length).Value = caption.NameSpace;
            command.Parameters.Add("@tag", DbType.String, caption.Tag.Length).Value = caption.Tag;
            command.Parameters.Add("@chemistryId", DbType.Int64).Value = id;

            command.ExecuteNonQuery();
        }

        public long AddChemistryCommand(SQLiteConnection conn, ChemistryDataObject chemistry)
        {
            var sb = new StringBuilder();

            sb.AppendLine("INSERT INTO Gallery");
            sb.AppendLine(" (Chemistry, Name, Formula, MolWeight, DataType)");
            sb.AppendLine("VALUES");
            sb.AppendLine(" (@blob, @name, @formula, @weight, @datatype)");

            var command = new SQLiteCommand(sb.ToString(), conn);
            command.Parameters.Add("@blob", DbType.Binary, chemistry.Chemistry.Length).Value = chemistry.Chemistry;
            command.Parameters.Add("@name", DbType.String, chemistry.Name.Length).Value = chemistry.Name;
            command.Parameters.Add("@formula", DbType.String, chemistry.Formula.Length).Value = chemistry.Formula;
            command.Parameters.Add("@weight", DbType.Double).Value = chemistry.MolWeight;
            command.Parameters.Add("@datatype", DbType.String, chemistry.Formula.Length).Value = chemistry.DataType;

            command.ExecuteNonQuery();

            var sql = "SELECT last_insert_rowid()";
            var cmd = new SQLiteCommand(sql, conn);
            var id = (long)cmd.ExecuteScalar();

            foreach (var name in chemistry.Names)
            {
                AddNameCommand(conn, name, id);
            }
            foreach (var formula in chemistry.Formulae)
            {
                AddFormulaCommand(conn, formula, id);
            }
            foreach (var caption in chemistry.Captions)
            {
                AddCaptionCommand(conn, caption, id);
            }

            //ToDo: [V3.3] Insert :-
            // chemistry.Tags

            return id;
        }

        public void UpdateChemistry(ChemistryDataObject chemistry)
        {
            var module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod()?.Name}()";

            _telemetry.Write(module, "Information", $"Updating Id:{chemistry.Id} Formula: {chemistry.Formula}; Name {chemistry.Name}");

            using (var conn = LibraryConnection())
            {
                UpdateChemistryCommand(conn, chemistry);
            }
        }

        public void UpdateChemistryCommand(SQLiteConnection conn, ChemistryDataObject chemistry)
        {
            var sb = new StringBuilder();
            sb.AppendLine("UPDATE GALLERY");
            sb.AppendLine("SET Name = @name, Chemistry = @blob, Formula = @formula, MolWeight = @weight, DataType = @datatype");
            sb.AppendLine("WHERE ID = @id");

            var command = new SQLiteCommand(sb.ToString(), conn);
            command.Parameters.Add("@id", DbType.Int64).Value = chemistry.Id;
            command.Parameters.Add("@blob", DbType.Binary, chemistry.Chemistry.Length).Value = chemistry.Chemistry;
            command.Parameters.Add("@name", DbType.String, chemistry.Name.Length).Value = chemistry.Name;
            command.Parameters.Add("@formula", DbType.String, chemistry.Formula.Length).Value = chemistry.Formula;
            command.Parameters.Add("@weight", DbType.Double).Value = chemistry.MolWeight;
            command.Parameters.Add("@datatype", DbType.String, chemistry.DataType.Length).Value = chemistry.DataType;

            command.ExecuteNonQuery();

            DeleteNamesCommand(conn, chemistry.Id);
            foreach (var name in chemistry.Names)
            {
                AddNameCommand(conn, name, chemistry.Id);
            }

            DeleteFormulaeCommand(conn, chemistry.Id);
            foreach (var formula in chemistry.Formulae)
            {
                AddFormulaCommand(conn, formula, chemistry.Id);
            }

            DeleteCaptionsCommand(conn, chemistry.Id);
            foreach (var caption in chemistry.Captions)
            {
                AddCaptionCommand(conn, caption, chemistry.Id);
            }

            //ToDo: [V3.3] Update/Replace :-
            // chemistry.Tags
        }

        public ChemistryDataObject GetChemistryById(long id)
        {
            var result = new ChemistryDataObject();

            using (var conn = LibraryConnection())
            {
                using (var chemistry = GetChemistryByIdCommand(conn, id))
                {
                    if (chemistry != null)
                    {
                        while (chemistry.Read())
                        {
                            result.Id = id;
                            result.DataType = chemistry["datatype"] as string;
                            result.Chemistry = (byte[])chemistry["Chemistry"];
                            result.Name = chemistry["name"] as string;
                            result.Formula = chemistry["formula"] as string;

                            // Handle new field(s) which might be null
                            var molWeight = chemistry["molweight"].ToString();
                            if (!string.IsNullOrEmpty(molWeight))
                            {
                                result.MolWeight = SafeDouble.Parse(molWeight);
                            }

                            result.Names = GetChemicalNamesCommand(conn, id);
                            result.Formulae = GetChemicalFormulaeCommand(conn, id);
                            result.Captions = GetChemicalCaptionsCommand(conn, id);
                            result.Tags = GetChemistryTagsCommand(conn, id);
                        }
                    }
                }
            }

            return result;
        }

        private SQLiteDataReader GetChemistryByIdCommand(SQLiteConnection conn, long id)
        {
            var sb = new StringBuilder();
            sb.AppendLine("SELECT Id, Chemistry, Name, Formula, MolWeight, DataType");
            sb.AppendLine("FROM Gallery");
            sb.AppendLine("WHERE ID = @id");

            var command = new SQLiteCommand(sb.ToString(), conn);
            command.Parameters.Add("@id", DbType.Int64).Value = id;

            return command.ExecuteReader();
        }

        public void DeleteAllChemistry()
        {
            using (var conn = LibraryConnection())
            {
                DeleteAllChemistryCommand(conn);
            }
        }

        private void DeleteAllChemistryCommand(SQLiteConnection conn)
        {
            var commands = new List<SQLiteCommand>();

            commands.Add(new SQLiteCommand("DELETE FROM ChemicalNames", conn));
            commands.Add(new SQLiteCommand("DELETE FROM ChemicalFormulae", conn));
            commands.Add(new SQLiteCommand("DELETE FROM ChemicalCaptions", conn));

            commands.Add(new SQLiteCommand("DELETE FROM TaggedChemistry", conn));
            commands.Add(new SQLiteCommand("DELETE FROM Tags", conn));

            commands.Add(new SQLiteCommand("DELETE FROM Gallery", conn));
#if DEBUG
            commands.Add(new SQLiteCommand("UPDATE SQLITE_SEQUENCE SET SEQ=0 WHERE NAME='Gallery'", conn));

            commands.Add(new SQLiteCommand("UPDATE SQLITE_SEQUENCE SET SEQ=0 WHERE NAME='ChemicalNames'", conn));
            commands.Add(new SQLiteCommand("UPDATE SQLITE_SEQUENCE SET SEQ=0 WHERE NAME='ChemicalFormulae'", conn));
            commands.Add(new SQLiteCommand("UPDATE SQLITE_SEQUENCE SET SEQ=0 WHERE NAME='ChemicalCaptions'", conn));
#endif

            using (var transaction = conn.BeginTransaction())
            {
                foreach (var sqLiteCommand in commands)
                {
                    sqLiteCommand.ExecuteNonQuery();
                }
                transaction.Commit();

                var vacuum = new SQLiteCommand("VACUUM", conn);
                vacuum.ExecuteNonQuery();
            }
        }

        public void DeleteChemistryById(long id)
        {
            using (var conn = LibraryConnection())
            {
                DeleteChemistryByIdCommand(conn, id);
            }
        }

        public void DeleteChemistryByIdCommand(SQLiteConnection conn, long id)
        {
            var sb = new StringBuilder();

            using (var tr = conn.BeginTransaction())
            {
                DeleteNamesCommand(conn, id);
                DeleteFormulaeCommand(conn, id);
                DeleteCaptionsCommand(conn, id);

                DeleteTagsCommand(conn, id);

                sb.AppendLine("DELETE FROM Gallery");
                sb.AppendLine("WHERE ID = @id");

                var command = new SQLiteCommand(sb.ToString(), conn);
                command.Parameters.Add("@id", DbType.Int64, 20).Value = id;
                command.ExecuteNonQuery();

                tr.Commit();
            }
        }

        public void AddTags(long id, List<string> tags)
        {
            using (var conn = LibraryConnection())
            {
                AddTagsCommand(conn, id, tags);
            }
        }

        private void AddTagsCommand(SQLiteConnection conn, long id, List<string> tags)
        {
            var sb = new StringBuilder();
            sb.AppendLine("INSERT INTO TaggedChemistry");
            sb.AppendLine(" (ChemistryId, TagId, Sequence)");
            sb.AppendLine("VALUES");
            sb.AppendLine(" (@id, @tagId, @sequence)");

            var command = new SQLiteCommand(sb.ToString(), conn);

            using (var tr = conn.BeginTransaction())
            {
                DeleteTagsCommand(conn, id);

                var sequence = 0;
                foreach (var tag in tags)
                {
                    var tagId = GetTagCommand(conn, tag);
                    if (tagId == -1)
                    {
                        tagId = AddTagCommand(conn, tag);
                    }

                    if (tagId > 0)
                    {
                        command.Parameters.Add("@id", DbType.Int32).Value = id;
                        command.Parameters.Add("@tagId", DbType.Int32).Value = tagId;
                        command.Parameters.Add("@sequence", DbType.Int32).Value = sequence++;
                        command.ExecuteNonQuery();
                    }
                }

                tr.Commit();
            }
        }

        private long GetTagCommand(SQLiteConnection conn, string tag)
        {
            long result = -1;

            var sb = new StringBuilder();
            sb.AppendLine("SELECT Id");
            sb.AppendLine("FROM Tags");
            sb.AppendLine("WHERE Tag = @tag");

            var command = new SQLiteCommand(sb.ToString(), conn);
            command.Parameters.Add("@tag", DbType.String, tag.Length).Value = tag;

            using (var reader = command.ExecuteReader())
            {
                if (reader != null)
                {
                    while (reader.Read())
                    {
                        result = (long)reader["Id"];
                    }
                }
            }

            return result;
        }

        private long AddTagCommand(SQLiteConnection conn, string tag)
        {
            var sb = new StringBuilder();
            sb.AppendLine("INSERT INTO Tags");
            sb.AppendLine(" (Tag)");
            sb.AppendLine("VALUES");
            sb.AppendLine(" (@tag)");

            var command = new SQLiteCommand(sb.ToString(), conn);
            command.Parameters.Add("@tag", DbType.String, tag.Length).Value = tag;
            command.ExecuteNonQuery();

            var sql = "SELECT last_insert_rowid()";
            var cmd = new SQLiteCommand(sql, conn);

            return (Int64)cmd.ExecuteScalar();
        }

        public List<LibraryTagDataObject> GetAllTags()
        {
            List<LibraryTagDataObject> result;

            using (var conn = LibraryConnection())
            {
                result = GetAllTagsCommand(conn);
            }

            return result;
        }

        private List<LibraryTagDataObject> GetAllTagsCommand(SQLiteConnection conn)
        {
            var result = new List<LibraryTagDataObject>();

            var sb = new StringBuilder();
            sb.AppendLine("SELECT t.Tag, t.Id AS TagId, (SELECT COUNT(1) FROM TaggedChemistry tc WHERE tc.TagId = t.Id) as Frequency");
            sb.AppendLine("FROM Tags t");
            sb.AppendLine("ORDER BY Frequency DESC");

            var command = new SQLiteCommand(sb.ToString(), conn);

            using (var reader = command.ExecuteReader())
            {
                if (reader != null)
                {
                    while (reader.Read())
                    {
                        var dto = new LibraryTagDataObject
                        {
                            Text = reader["Tag"] as string,
                            Id = (long)reader["TagId"],
                            Frequency = (long)reader["Frequency"]
                        };
                        result.Add(dto);
                    }
                }
            }

            return result;
        }

        private SQLiteDataReader GetDistinctNamesCommand(SQLiteConnection conn)
        {
            var sb = new StringBuilder();
            sb.AppendLine("SELECT DISTINCT Name, ChemistryId");
            sb.AppendLine("FROM ChemicalNames");
            sb.AppendLine("WHERE NOT (Namespace = 'chem4word' AND Tag = 'cev_freq')");
            sb.AppendLine(" AND NOT (Namespace = 'pubchem' AND Tag = 'Id')");
            sb.AppendLine(" AND NOT (Name = 'chemical compound')");
            sb.AppendLine("UNION");
            sb.AppendLine("SELECT DISTINCT Name, Id");
            sb.AppendLine("FROM Gallery");

            var command = new SQLiteCommand(sb.ToString(), conn);
            return command.ExecuteReader();
        }

        private List<ChemistryTagDataObject> GetChemistryTagsCommand(SQLiteConnection conn, long id)
        {
            var result = new List<ChemistryTagDataObject>();

            var sb = new StringBuilder();
            sb.AppendLine("SELECT c.ChemistryId, c.Sequence, t.Tag, t.Id");
            sb.AppendLine("FROM TaggedChemistry c");
            sb.AppendLine("JOIN Tags t ON c.TagId = t.Id");
            sb.AppendLine("WHERE c.ChemistryId = @id");
            sb.AppendLine("ORDER BY c.ChemistryId, c.Sequence");

            var command = new SQLiteCommand(sb.ToString(), conn);
            command.Parameters.Add("@id", DbType.Int64).Value = id;

            using (var reader = command.ExecuteReader())
            {
                if (reader != null)
                {
                    while (reader.Read())
                    {
                        var dto = new ChemistryTagDataObject
                        {
                            Text = reader["Tag"] as string,
                            Sequence = (long)reader["Sequence"],
                            TagId = (long)reader["Id"],
                            ChemistryId = (long)reader["ChemistryId"]
                        };
                        result.Add(dto);
                    }
                }
            }

            return result;
        }

        private List<ChemistryNameDataObject> GetChemicalNamesCommand(SQLiteConnection conn, long id)
        {
            var results = new List<ChemistryNameDataObject>();

            var sb = new StringBuilder();
            sb.AppendLine("SELECT Id, Name, Namespace, Tag, ChemistryID");
            sb.AppendLine("FROM ChemicalNames");
            sb.AppendLine("WHERE ChemistryId = @id");

            var command = new SQLiteCommand(sb.ToString(), conn);
            command.Parameters.Add("@id", DbType.Int64).Value = id;

            using (var reader = command.ExecuteReader())
            {
                if (reader != null)
                {
                    while (reader.Read())
                    {
                        var dto = new ChemistryNameDataObject
                        {
                            Id = (long)reader["Id"],
                            Name = reader["Name"] as string,
                            NameSpace = reader["Namespace"] as string,
                            Tag = reader["Tag"] as string,
                            ChemistryId = (long)reader["ChemistryId"]
                        };
                        results.Add(dto);
                    }
                }
            }

            return results;
        }

        private List<ChemistryNameDataObject> GetChemicalFormulaeCommand(SQLiteConnection conn, long id)
        {
            var results = new List<ChemistryNameDataObject>();

            var sb = new StringBuilder();
            sb.AppendLine("SELECT Id, Name, Namespace, Tag, ChemistryID");
            sb.AppendLine("FROM ChemicalFormulae");
            sb.AppendLine("WHERE ChemistryId = @id");

            var command = new SQLiteCommand(sb.ToString(), conn);
            command.Parameters.Add("@id", DbType.Int64).Value = id;

            using (var reader = command.ExecuteReader())
            {
                if (reader != null)
                {
                    while (reader.Read())
                    {
                        var dto = new ChemistryNameDataObject
                        {
                            Id = (long)reader["Id"],
                            Name = reader["Name"] as string,
                            NameSpace = reader["Namespace"] as string,
                            Tag = reader["Tag"] as string,
                            ChemistryId = (long)reader["ChemistryId"]
                        };
                        results.Add(dto);
                    }
                }
            }

            return results;
        }

        private List<ChemistryNameDataObject> GetChemicalCaptionsCommand(SQLiteConnection conn, long id)
        {
            var results = new List<ChemistryNameDataObject>();

            var sb = new StringBuilder();
            sb.AppendLine("SELECT Id, Name, Namespace, Tag, ChemistryID");
            sb.AppendLine("FROM ChemicalCaptions");
            sb.AppendLine("WHERE ChemistryId = @id");

            var command = new SQLiteCommand(sb.ToString(), conn);
            command.Parameters.Add("@id", DbType.Int64).Value = id;

            using (var reader = command.ExecuteReader())
            {
                if (reader != null)
                {
                    while (reader.Read())
                    {
                        var dto = new ChemistryNameDataObject
                        {
                            Id = (long)reader["Id"],
                            Name = reader["Name"] as string,
                            NameSpace = reader["Namespace"] as string,
                            Tag = reader["Tag"] as string,
                            ChemistryId = (long)reader["ChemistryId"]
                        };
                        results.Add(dto);
                    }
                }
            }

            return results;
        }

        private SQLiteDataReader GetAllChemistryCommand(SQLiteConnection conn)
        {
            var sb = new StringBuilder();

            sb.AppendLine("SELECT Id, Chemistry, Name, Formula, MolWeight, DataType");
            sb.AppendLine("FROM Gallery");
            sb.AppendLine("ORDER BY Name");

            var command = new SQLiteCommand(sb.ToString(), conn);
            return command.ExecuteReader();
        }

        private void DeleteNamesCommand(SQLiteConnection conn, long chemistryId)
        {
            var sb = new StringBuilder();
            sb.AppendLine("DELETE FROM ChemicalNames");
            sb.AppendLine("WHERE ChemistryId = @id");

            var nameCommand = new SQLiteCommand(sb.ToString(), conn);
            nameCommand.Parameters.Add("@id", DbType.Int64, 20).Value = chemistryId;
            nameCommand.ExecuteNonQuery();
        }

        private void DeleteFormulaeCommand(SQLiteConnection conn, long chemistryId)
        {
            var sb = new StringBuilder();
            sb.AppendLine("DELETE FROM ChemicalFormulae");
            sb.AppendLine("WHERE ChemistryId = @id");

            var nameCommand = new SQLiteCommand(sb.ToString(), conn);
            nameCommand.Parameters.Add("@id", DbType.Int64, 20).Value = chemistryId;
            nameCommand.ExecuteNonQuery();
        }

        private void DeleteCaptionsCommand(SQLiteConnection conn, long chemistryId)
        {
            var sb = new StringBuilder();
            sb.AppendLine("DELETE FROM ChemicalCaptions");
            sb.AppendLine("WHERE ChemistryId = @id");

            var nameCommand = new SQLiteCommand(sb.ToString(), conn);
            nameCommand.Parameters.Add("@id", DbType.Int64, 20).Value = chemistryId;
            nameCommand.ExecuteNonQuery();
        }

        private void DeleteTagsCommand(SQLiteConnection conn, long chemistryId)
        {
            var sb = new StringBuilder();
            sb.AppendLine("DELETE FROM TaggedChemistry");
            sb.AppendLine("WHERE ChemistryId = @id");

            var tagCommand = new SQLiteCommand(sb.ToString(), conn);
            tagCommand.Parameters.Add("@id", DbType.Int64, 20).Value = chemistryId;
            tagCommand.ExecuteNonQuery();
        }

        #endregion Data I/O

        #region Properties

        private void DetermineProperties()
        {
            var module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod()?.Name}()";

            try
            {
                if (File.Exists(_databaseFile.FullName))
                {
                    Database.FileExists = true;

                    // Source https://www.connectionstrings.com/sqlite/
                    var conn = new SQLiteConnection($"Data Source={_databaseFile.FullName};Synchronous=Full");
                    conn.Open();

                    // If we get here, the file we tried to open is a valid SQLite database
                    Database.IsSqliteDatabase = true;

                    Database.IsReadOnly = conn.IsReadOnly(conn.Database);

                    Database.IsChem4Word = CheckGalleryExists(conn) && CheckChemicalNamesExists(conn);

                    if (Database.IsChem4Word)
                    {
                        // Read patches from resource
                        var resource = ResourceHelper.GetStringResource(Assembly.GetExecutingAssembly(), "Patches.json");
                        _patches = JsonConvert.DeserializeObject<List<Patch>>(resource);

                        Database.RequiresPatching = CheckIfDatabaseRequiresPatching(conn);

                        // if database requires patching do patching
                        if (!Database.IsReadOnly && Database.RequiresPatching)
                        {
                            DoPatching(conn);
                        }

                        // Finally read properties table
                        GetProperties(conn);
                    }

                    conn.Close();
                }
                else
                {
                    Database.FileExists = false;
                }
            }
            catch (Exception exception)
            {
                // If we get an Exception assume this is NOT a SQLite database
                Database.IsSqliteDatabase = false;

                _telemetry.Write(module, "Exception", $"{exception.Message}");
                _telemetry.Write(module, "Exception(Data)", $"{exception.StackTrace}");
            }
        }

        private void GetProperties(SQLiteConnection conn)
        {
            if (TableExists(conn, "Properties"))
            {
                var sb = new StringBuilder();
                sb.AppendLine("SELECT Key, Value");
                sb.AppendLine("FROM Properties");

                var command = new SQLiteCommand(sb.ToString(), conn);
                var rows = command.ExecuteReader();
                if (rows != null)
                {
                    while (rows.Read())
                    {
                        Database.Properties.Add(rows["Key"].ToString(), rows["Value"].ToString());
                    }
                }

                if (!Database.Properties.ContainsKey("Id"))
                {
                    var guid = Guid.NewGuid().ToString("D");
                    Database.Properties.Add("Id", guid);
                    if (!Database.IsReadOnly)
                    {
                        WriteProperty(conn, "Id", guid);
                    }
                }

                Database.Properties.Add("Count", CountOfStructures(conn));
            }
        }

        private void WriteProperty(SQLiteConnection conn, string key, string value)
        {
            var sb = new StringBuilder();
            sb.AppendLine("INSERT INTO Properties");
            sb.AppendLine(" (Key, Value)");
            sb.AppendLine("VALUES");
            sb.AppendLine(" (@key, @value)");

            var command = new SQLiteCommand(sb.ToString(), conn);
            command.Parameters.Add("@key", DbType.String, key.Length).Value = key;
            command.Parameters.Add("@value", DbType.String, value.Length).Value = value;

            command.ExecuteNonQuery();
        }

        private string CountOfStructures(SQLiteConnection conn)
        {
            var sb = new StringBuilder();
            sb.AppendLine("SELECT COUNT(1)");
            sb.AppendLine("FROM Gallery");

            var cmd = new SQLiteCommand(sb.ToString(), conn);

            var count = (long)cmd.ExecuteScalar();
            return count.ToString("N0");
        }

        #endregion Properties

        #region Patching

        private void DoPatching(SQLiteConnection conn)
        {
            var currentVersion = Version.Parse("0.0.0");
            var targetVersion = _patches.Max(p => p.Version);

            currentVersion = GetPatchLevel(conn, currentVersion);

            if (currentVersion < targetVersion)
            {
                // Backup before patching
                var fileInfo = new FileInfo(_databaseFile.FullName);
                if (fileInfo.DirectoryName != null)
                {
                    var backupFile = FileHelper.BackupFile(fileInfo, _backupDirectory, true, false);

                    if (!ApplyPatches(conn, currentVersion))
                    {
                        // If patching fails, revert to previous version
                        conn.Close();

                        if (File.Exists(backupFile))
                        {
                            File.Delete(_databaseFile.FullName);
                            File.Move(backupFile, _databaseFile.FullName);
                        }

                        conn.Open();
                    }
                }
            }
        }

        private bool ApplyPatches(SQLiteConnection conn, Version currentVersion)
        {
            var module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod()?.Name}()";

            var result = true;

            try
            {
                foreach (var patch in _patches)
                {
                    if (patch.Version > currentVersion)
                    {
                        foreach (var script in patch.Scripts)
                        {
                            try
                            {
                                _telemetry.Write(module, "Information", $"Applying {patch.Version} patch '{script}' to {_databaseFile.FullName}");
                                var command = new SQLiteCommand(script, conn);
                                command.ExecuteNonQuery();
                            }
                            catch (Exception exception)
                            {
                                Debug.WriteLine(exception);
                                _telemetry.Write(module, "Exception", exception.ToString());
                            }
                        }
                        WritePatchRecordCommand(conn, patch.Version);
                    }
                }
            }
            catch (Exception ex)
            {
                _telemetry.Write(module, "Exception", $"Exception {ex.Message}");
                result = false;
            }

            return result;
        }

        private void WritePatchRecordCommand(SQLiteConnection conn, Version version)
        {
            var module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod()?.Name}()";

            try
            {
                // Add Patch record
                var versionString = version.ToString(3);
                var sb = new StringBuilder();

                sb.AppendLine("INSERT INTO Patches");
                sb.AppendLine(" (Version, Applied)");
                sb.AppendLine("VALUES");
                sb.AppendLine(" (@version, @applied)");

                var command = new SQLiteCommand(sb.ToString(), conn);
                command.Parameters.Add("@version", DbType.String, versionString.Length).Value = versionString;
                var applied = SafeDate.ToShortDate(DateTime.Today);
                command.Parameters.Add("@applied", DbType.String, applied.Length).Value = applied;

                command.ExecuteNonQuery();

                // Properties table only exists in 3.3.1 or greater
                if (version >= new Version(3, 3, 1))
                {
                    // Delete old record
                    sb = new StringBuilder();

                    sb.AppendLine("DELETE FROM Properties");
                    sb.AppendLine("WHERE Key = 'Version'");

                    command = new SQLiteCommand(sb.ToString(), conn);
                    command.ExecuteNonQuery();

                    // Add new record in Properties
                    sb = new StringBuilder();

                    sb.AppendLine("INSERT INTO Properties");
                    sb.AppendLine(" (Key, Value)");
                    sb.AppendLine("VALUES");
                    sb.AppendLine(" ('Version', @version)");

                    command = new SQLiteCommand(sb.ToString(), conn);
                    command.Parameters.Add("@version", DbType.String, versionString.Length).Value = versionString;
                    command.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                _telemetry.Write(module, "Exception", $"Exception {ex.Message}");
            }
        }

        private bool CheckIfDatabaseRequiresPatching(SQLiteConnection conn)
        {
            var currentVersion = Version.Parse("0.0.0");
            var targetVersion = _patches.Max(p => p.Version);

            if (TableExists(conn, "Patches"))
            {
                currentVersion = GetPatchLevel(conn, currentVersion);
            }

            return currentVersion < targetVersion;
        }

        private Version GetPatchLevel(SQLiteConnection conn, Version currentVersion)
        {
            var module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod()?.Name}()";

            try
            {
                if (TableExists(conn, "Patches"))
                {
                    var sb = new StringBuilder();
                    sb.AppendLine("SELECT Version, Applied");
                    sb.AppendLine("FROM Patches");

                    var command = new SQLiteCommand(sb.ToString(), conn);
                    var dataReader = command.ExecuteReader();

                    if (dataReader != null)
                    {
                        while (dataReader.Read())
                        {
                            if (dataReader["Version"] is string version)
                            {
                                var thisVersion = Version.Parse(version);
                                if (thisVersion > currentVersion)
                                {
                                    currentVersion = thisVersion;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                _telemetry.Write(module, "Exception", $"{exception.Message}");
                _telemetry.Write(module, "Exception(Data)", $"{exception.StackTrace}");

                Debugger.Break();
            }

            return currentVersion;
        }

        #endregion Patching

        #region Checks

        private void GetListOfTablesAndViews(SQLiteConnection conn)
        {
            var module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod()?.Name}()";

            try
            {
                var sb = new StringBuilder();
                sb.AppendLine("SELECT Name, Type");
                sb.AppendLine("FROM SQLITE_MASTER");
                sb.AppendLine("WHERE Type IN ('table','view')");

                var command = new SQLiteCommand(sb.ToString(), conn);
                var dataReader = command.ExecuteReader();

                if (dataReader != null)
                {
                    while (dataReader.Read())
                    {
                        if (dataReader["Name"] is string name)
                        {
                            _tables.Add(name);
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                _telemetry.Write(module, "Exception", $"{exception.Message}");
                _telemetry.Write(module, "Exception(Data)", $"{exception.StackTrace}");

                Debugger.Break();
            }
        }

        private bool TableExists(SQLiteConnection conn, string tableName)
        {
            if (!_tables.Any())
            {
                GetListOfTablesAndViews(conn);
            }

            return _tables.Contains(tableName);
        }

        private List<TableColumn> GetListOfColumns(SQLiteConnection conn, string table)
        {
            var module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod()?.Name}()";

            var foundColumns = new List<TableColumn>();

            try
            {
                var sb = new StringBuilder();
                sb.AppendLine("SELECT Name, Type, [NotNull]");
                sb.AppendLine($"FROM PRAGMA_TABLE_INFO('{table}')");

                var command = new SQLiteCommand(sb.ToString(), conn);
                var dataReader = command.ExecuteReader();
                if (dataReader != null)
                {
                    while (dataReader.Read())
                    {
                        var column = new TableColumn
                        {
                            Name = dataReader["Name"].ToString(),
                            Type = dataReader["Type"].ToString(),
                            NotNull = dataReader["NotNull"].ToString()
                        };
                        foundColumns.Add(column);
                    }
                }
            }
            catch (Exception exception)
            {
                _telemetry.Write(module, "Exception", $"{exception.Message}");
                _telemetry.Write(module, "Exception(Data)", $"{exception.StackTrace}");

                Debugger.Break();
            }

            return foundColumns;
        }

        private bool CheckGalleryExists(SQLiteConnection conn)
        {
            const string tableName = "Gallery";

            var result = false;

            if (TableExists(conn, tableName))
            {
                var expectedColumns = new List<TableColumn>
                                      {
                                          new TableColumn {Name = "Chemistry", Type = "BLOB", NotNull = "1"},
                                          new TableColumn {Name = "Name", Type = "TEXT", NotNull = "1"},
                                          new TableColumn {Name = "Formula", Type = "TEXT", NotNull = "0"}
                                      };

                var count = 0;
                var foundColumns = GetListOfColumns(conn, tableName);

                foreach (var expectedColumn in expectedColumns)
                {
                    foreach (var foundColumn in foundColumns)
                    {
                        if (expectedColumn.Equals(foundColumn))
                        {
                            count++;
                            break;
                        }
                    }
                }

                result = count == expectedColumns.Count;
            }

            return result;
        }

        private bool CheckChemicalNamesExists(SQLiteConnection conn)
        {
            const string tableName = "ChemicalNames";

            var result = false;

            if (TableExists(conn, tableName))
            {
                var expectedColumns = new List<TableColumn>
                                      {
                                          new TableColumn {Name = "Name", Type = "TEXT", NotNull = "1"},
                                          new TableColumn {Name = "Namespace", Type = "TEXT", NotNull = "1"},
                                          new TableColumn {Name = "Tag", Type = "TEXT", NotNull = "1"}
                                      };

                var count = 0;
                var foundColumns = GetListOfColumns(conn, tableName);

                foreach (var expectedColumn in expectedColumns)
                {
                    foreach (var foundColumn in foundColumns)
                    {
                        if (expectedColumn.Equals(foundColumn))
                        {
                            count++;
                            break;
                        }
                    }
                }

                result = count == expectedColumns.Count;
            }

            return result;
        }

        #endregion Checks
    }
}