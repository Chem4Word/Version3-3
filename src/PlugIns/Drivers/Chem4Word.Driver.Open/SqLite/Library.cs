// ---------------------------------------------------------------------------
//  Copyright (c) 2022, The .NET Foundation.
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
using System.Windows;

namespace Chem4Word.Driver.Open.SqLite
{
    public class Library
    {
        private static readonly string _product = Assembly.GetExecutingAssembly().FullName.Split(',')[0];
        private static readonly string _class = MethodBase.GetCurrentMethod().DeclaringType?.Name;

        private readonly IChem4WordTelemetry _telemetry;
        private Point _topLeft;
        private DatabaseDetails _details;

        private readonly List<Patch> _patches;

        public Library(IChem4WordTelemetry telemetry, DatabaseDetails details, Point topLeft)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            _telemetry = telemetry;
            _topLeft = topLeft;
            _details = details;

            // Read patches from resource
            var resource = ResourceHelper.GetStringResource(Assembly.GetExecutingAssembly(), "Patches.json");
            _patches = JsonConvert.DeserializeObject<List<Patch>>(resource);

            Patch(_patches.Max(p => p.Version));
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

        internal SQLiteConnection LibraryConnection()
        {
            // Source https://www.connectionstrings.com/sqlite/
            var conn = new SQLiteConnection($"Data Source={_details.Connection};Synchronous=Full");

            return conn.OpenAndReturn();
        }

        private void Patch(Version targetVersion)
        {
            using (SQLiteConnection conn = LibraryConnection())
            {
                bool patchTableExists = false;

                var currentVersion = Version.Parse("0.0.0");

                using (SQLiteDataReader tables = GetListOfTablesAndViews(conn))
                {
                    if (tables != null)
                    {
                        while (tables.Read())
                        {
                            if (tables["Name"] is string name)
                            {
                                if (name.Equals("Patches"))
                                {
                                    patchTableExists = true;
                                }
                            }
                        }
                    }
                }

                if (patchTableExists)
                {
                    // Read current patch level
                    using (SQLiteDataReader patches = GetListOfPatches(conn))
                    {
                        if (patches != null)
                        {
                            while (patches.Read())
                            {
                                if (patches["Version"] is string version)
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

                if (currentVersion < targetVersion)
                {
                    // Backup before patching
                    var fileInfo = new FileInfo(_details.Connection);
                    if (fileInfo.DirectoryName != null)
                    {
                        var backup = Path.Combine(fileInfo.DirectoryName, @"..\Backups", $"{SafeDate.ToIsoFilePrefix(DateTime.Now)} {_details.ShortFileName}");
                        File.Copy(_details.Connection, backup);

                        if (!ApplyPatches(conn, currentVersion))
                        {
                            // If patching fails, revert to previous version
                            File.Delete(_details.Connection);
                            File.Copy(backup, _details.Connection);
                        }
                    }
                }
            }
        }

        private SQLiteDataReader GetListOfPatches(SQLiteConnection conn)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            SQLiteDataReader result = null;

            try
            {
                var sb = new StringBuilder();
                sb.AppendLine("SELECT Version, Applied");
                sb.AppendLine("FROM Patches");

                var command = new SQLiteCommand(sb.ToString(), conn);
                result = command.ExecuteReader();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Exception {ex.Message} in {module}");
            }

            return result;
        }

        private SQLiteDataReader GetListOfTablesAndViews(SQLiteConnection conn)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            SQLiteDataReader result = null;

            try
            {
                var sb = new StringBuilder();
                sb.AppendLine("SELECT t.Name, t.Type");
                sb.AppendLine("FROM sqlite_master t");
                sb.AppendLine("WHERE t.Type IN ('table','view')");

                var command = new SQLiteCommand(sb.ToString(), conn);
                result = command.ExecuteReader();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Exception {ex.Message} in {module}");
            }

            return result;
        }

        private bool ApplyPatches(SQLiteConnection conn, Version currentVersion)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            bool result = true;

            try
            {
                foreach (Patch patch in _patches)
                {
                    if (patch.Version > currentVersion)
                    {
                        foreach (string script in patch.Scripts)
                        {
                            _telemetry.Write(module, "Information", $"Applying {patch.Version} patch '{script}' to {_details.Connection}");
                            var command = new SQLiteCommand(script, conn);
                            command.ExecuteNonQuery();
                        }
                        AddPatchRecord(conn, patch.Version);
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

        private void AddPatchRecord(SQLiteConnection conn, Version version)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

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

        public Dictionary<string, string> GetProperties()
        {
            var results = new Dictionary<string, string>();

            using (SQLiteConnection conn = LibraryConnection())
            {
                using (SQLiteDataReader rows = GetProperties(conn))
                {
                    if (rows != null)
                    {
                        while (rows.Read())
                        {
                            results.Add(rows["Key"].ToString(), rows["Value"].ToString());
                        }
                    }
                }

                if (!results.ContainsKey("Id"))
                {
                    var guid = Guid.NewGuid().ToString("D");
                    AddProperty(conn, "Id", guid);
                    results.Add("Id", guid);
                }

                results.Add("Count", CountOfStructures(conn));
            }

            return results;
        }

        private string CountOfStructures(SQLiteConnection conn)
        {
            var sb = new StringBuilder();
            sb.AppendLine("SELECT COUNT(1)");
            sb.AppendLine("FROM Gallery");

            var cmd = new SQLiteCommand(sb.ToString(), conn);

            var count = (long)cmd.ExecuteScalar();
            return count.ToString();
        }

        private void AddProperty(SQLiteConnection conn, string key, string value)
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

        private SQLiteDataReader GetProperties(SQLiteConnection conn)
        {
            var sb = new StringBuilder();
            sb.AppendLine("SELECT Key, Value");
            sb.AppendLine("FROM Properties");

            var command = new SQLiteCommand(sb.ToString(), conn);
            return command.ExecuteReader();
        }

        /// <summary>
        /// This is called by LoadNamesFromLibrary in Add-In which happens on right click
        /// </summary>
        /// <returns></returns>
        public Dictionary<string, int> GetSubstanceNamesWithIds()
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            var allNames = new Dictionary<string, int>();

            var sw = new Stopwatch();
            sw.Start();

            try
            {
                using (SQLiteConnection conn = LibraryConnection())
                {
                    using (SQLiteDataReader names = GetAllNames(conn))
                    {
                        if (names != null)
                        {
                            while (names.Read())
                            {
                                var name = names["Name"] as string;
                                // Exclude any names that are three characters or less
                                if (!string.IsNullOrEmpty(name) && name.Length > 3)
                                {
                                    int id = int.Parse(names["ChemistryId"].ToString());
                                    if (!allNames.ContainsKey(name))
                                    {
                                        allNames.Add(name, id);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                using (var form = new ReportError(_telemetry, _topLeft, module, ex))
                {
                    form.ShowDialog();
                }
            }

            sw.Stop();
#if DEBUG
            // Task 810
            _telemetry.Write(module, "Timing", $"Reading {allNames.Count} Chemical names took {SafeDouble.AsString0(sw.ElapsedMilliseconds)}ms");
#endif
            return allNames;
        }

        /// <summary>
        /// This is called via Microsoft.Office.Tools.CustomTaskPanel.OnVisibleChanged and Chem4Word.CustomRibbon.OnShowLibraryClick
        /// </summary>
        /// <returns></returns>
        public List<ChemistryDataObject> GetAllChemistry()
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            var results = new List<ChemistryDataObject>();

            try
            {
                var sw = new Stopwatch();

                using (SQLiteConnection conn = LibraryConnection())
                {
                    sw.Start();

                    var allTaggedItems = GetAllChemistryTags(conn);
                    var allNames = GetAllChemicalNames(conn);
                    var allFormulae = GetAllChemicalFormulae(conn);
                    var allCaptions = GetAllChemicalCaptions(conn);

                    using (SQLiteDataReader chemistry = GetAllChemistry(conn))
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
                                    Tags = allTaggedItems.Where(t => t.ChemistryId == id).ToList(),
                                    Names = allNames.Where(t => t.ChemistryId == id).ToList(),
                                    Formulae = allFormulae.Where(t => t.ChemistryId == id).ToList(),
                                    Captions = allCaptions.Where(t => t.ChemistryId == id).ToList()
                                };

                                // Handle new field(s) which may be null
                                var molWeight = chemistry["molweight"].ToString();
                                if (!string.IsNullOrEmpty(molWeight))
                                {
                                    dto.MolWeight = double.Parse(molWeight);
                                }
                                results.Add(dto);
                            }
                        }
                    }

                    sw.Stop();
                    _telemetry?.Write(module, "Timing", $"Reading {results.Count} structures took {SafeDouble.AsString0(sw.ElapsedMilliseconds)}ms");
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

            using (SQLiteConnection conn = LibraryConnection())
            {
                result = AddChemistry(conn, chemistry);
            }

            return result;
        }

        private void AddName(SQLiteConnection conn, ChemistryNameDataObject name, long id)
        {
            var sb = new StringBuilder();

            sb.AppendLine("INSERT INTO ChemicalNames");
            sb.AppendLine(" (Name, Namespace, Tag, ChemistryID)");
            sb.AppendLine("VALUES");
            sb.AppendLine(" (@name, @namespace, @tag, @chemstryId)");

            var command = new SQLiteCommand(sb.ToString(), conn);
            command.Parameters.Add("@name", DbType.String, name.Name.Length).Value = name.Name;
            command.Parameters.Add("@namespace", DbType.String, name.NameSpace.Length).Value = name.NameSpace;
            command.Parameters.Add("@tag", DbType.String, name.Tag.Length).Value = name.Tag;
            command.Parameters.Add("@chemstryId", DbType.Int64).Value = id;

            command.ExecuteNonQuery();
        }

        private void AddFormula(SQLiteConnection conn, ChemistryNameDataObject formula, long id)
        {
            var sb = new StringBuilder();

            sb.AppendLine("INSERT INTO ChemicalFormulae");
            sb.AppendLine(" (Name, Namespace, Tag, ChemistryID)");
            sb.AppendLine("VALUES");
            sb.AppendLine(" (@name, @namespace, @tag, @chemstryId)");

            var command = new SQLiteCommand(sb.ToString(), conn);
            command.Parameters.Add("@name", DbType.String, formula.Name.Length).Value = formula.Name;
            command.Parameters.Add("@namespace", DbType.String, formula.NameSpace.Length).Value = formula.NameSpace;
            command.Parameters.Add("@tag", DbType.String, formula.Tag.Length).Value = formula.Tag;
            command.Parameters.Add("@chemstryId", DbType.Int64).Value = id;

            command.ExecuteNonQuery();
        }

        private void AddCaption(SQLiteConnection conn, ChemistryNameDataObject caption, long id)
        {
            var sb = new StringBuilder();

            sb.AppendLine("INSERT INTO ChemicalCaptions");
            sb.AppendLine(" (Name, Namespace, Tag, ChemistryID)");
            sb.AppendLine("VALUES");
            sb.AppendLine(" (@name, @namespace, @tag, @chemstryId)");

            var command = new SQLiteCommand(sb.ToString(), conn);
            command.Parameters.Add("@name", DbType.String, caption.Name.Length).Value = caption.Name;
            command.Parameters.Add("@namespace", DbType.String, caption.NameSpace.Length).Value = caption.NameSpace;
            command.Parameters.Add("@tag", DbType.String, caption.Tag.Length).Value = caption.Tag;
            command.Parameters.Add("@chemstryId", DbType.Int64).Value = id;

            command.ExecuteNonQuery();
        }

        internal long AddChemistry(SQLiteConnection conn, ChemistryDataObject chemistry)
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

            string sql = "SELECT last_insert_rowid()";
            var cmd = new SQLiteCommand(sql, conn);
            var id = (long)cmd.ExecuteScalar();

            foreach (var name in chemistry.Names)
            {
                AddName(conn, name, id);
            }
            foreach (var formula in chemistry.Formulae)
            {
                AddFormula(conn, formula, id);
            }
            foreach (var caption in chemistry.Captions)
            {
                AddCaption(conn, caption, id);
            }

            //ToDo: [V3.3] Insert :-
            // chemistry.Tags

            return id;
        }

        public void UpdateChemistry(ChemistryDataObject chemistry)
        {
            using (SQLiteConnection conn = LibraryConnection())
            {
                UpdateChemistry(conn, chemistry);
            }
        }

        internal void UpdateChemistry(SQLiteConnection conn, ChemistryDataObject chemistry)
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

            DeleteNames(conn, chemistry.Id);
            foreach (var name in chemistry.Names)
            {
                AddName(conn, name, chemistry.Id);
            }

            DeleteFormulae(conn, chemistry.Id);
            foreach (var formula in chemistry.Formulae)
            {
                AddFormula(conn, formula, chemistry.Id);
            }

            DeleteCaptions(conn, chemistry.Id);
            foreach (var caption in chemistry.Captions)
            {
                AddCaption(conn, caption, chemistry.Id);
            }

            //ToDo: [V3.3] Update/Replace :-
            // chemistry.Tags
        }

        public ChemistryDataObject GetChemistryById(long id)
        {
            var result = new ChemistryDataObject();

            using (SQLiteConnection conn = LibraryConnection())
            {
                //ToDo: Implement filtering by Id so that we get less data here ...
                var allNames = GetAllChemicalNames(conn);
                var allFormulae = GetAllChemicalFormulae(conn);
                var allCaptions = GetAllChemicalCaptions(conn);

                using (SQLiteDataReader chemistry = GetChemistryById(conn, id))
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
                                result.MolWeight = double.Parse(molWeight);
                            }

                            result.Names = allNames.Where(t => t.ChemistryId == id).ToList();
                            result.Formulae = allFormulae.Where(t => t.ChemistryId == id).ToList();
                            result.Captions = allCaptions.Where(t => t.ChemistryId == id).ToList();

                            // ToDo: [V3.3] Get Tags ?
                        }
                    }
                }
            }

            return result;
        }

        private SQLiteDataReader GetChemistryById(SQLiteConnection conn, long id)
        {
            var sb = new StringBuilder();
            sb.AppendLine("SELECT Id, Chemistry, Name, Formula");
            sb.AppendLine("FROM Gallery");
            sb.AppendLine("WHERE ID = @id");

            var command = new SQLiteCommand(sb.ToString(), conn);
            command.Parameters.Add("@id", DbType.Int64).Value = id;

            return command.ExecuteReader();
        }

        public void DeleteAllChemistry()
        {
            using (SQLiteConnection conn = LibraryConnection())
            {
                DeleteAllChemistry(conn);
            }
        }

        private void DeleteAllChemistry(SQLiteConnection conn)
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
            commands.Add(new SQLiteCommand("UPDATE SQLITE_SEQUENCE SET SEQ=0 WHERE NAME='Tags'", conn));
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
            using (SQLiteConnection conn = LibraryConnection())
            {
                DeleteChemistryById(conn, id);
            }
        }

        internal void DeleteChemistryById(SQLiteConnection conn, long id)
        {
            var sb = new StringBuilder();

            using (SQLiteTransaction tr = conn.BeginTransaction())
            {
                DeleteNames(conn, id);
                DeleteFormulae(conn, id);
                DeleteCaptions(conn, id);

                DeleteTags(conn, id);

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
            using (SQLiteConnection conn = LibraryConnection())
            {
                AddTags(conn, id, tags);
            }
        }

        private void AddTags(SQLiteConnection conn, long id, List<string> tags)
        {
            var sb = new StringBuilder();
            sb.AppendLine("INSERT INTO TaggedChemistry");
            sb.AppendLine(" (ChemistryId, TagId, Sequence)");
            sb.AppendLine("VALUES");
            sb.AppendLine(" (@id, @tagId, @sequence)");

            var command = new SQLiteCommand(sb.ToString(), conn);

            using (SQLiteTransaction tr = conn.BeginTransaction())
            {
                DeleteTags(conn, id);

                int sequence = 0;
                foreach (string tag in tags)
                {
                    var tagId = GetTag(conn, tag);
                    if (tagId == -1)
                    {
                        tagId = AddTag(conn, tag);
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

        private long GetTag(SQLiteConnection conn, string tag)
        {
            long result = -1;

            var sb = new StringBuilder();
            sb.AppendLine("SELECT Id");
            sb.AppendLine("FROM Tags");
            sb.AppendLine("WHERE Tag = @tag");

            var command = new SQLiteCommand(sb.ToString(), conn);
            command.Parameters.Add("@tag", DbType.String, tag.Length).Value = tag;

            using (SQLiteDataReader reader = command.ExecuteReader())
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

        private long AddTag(SQLiteConnection conn, string tag)
        {
            var sb = new StringBuilder();
            sb.AppendLine("INSERT INTO Tags");
            sb.AppendLine(" (Tag)");
            sb.AppendLine("VALUES");
            sb.AppendLine(" (@tag)");

            var command = new SQLiteCommand(sb.ToString(), conn);
            command.Parameters.Add("@tag", DbType.String, tag.Length).Value = tag;
            command.ExecuteNonQuery();

            string sql = "SELECT last_insert_rowid()";
            var cmd = new SQLiteCommand(sql, conn);

            return (Int64)cmd.ExecuteScalar();
        }

        public List<LibraryTagDataObject> GetAllTags()
        {
            var result = new List<LibraryTagDataObject>();

            using (SQLiteConnection conn = LibraryConnection())
            {
                result = GetAllTags(conn);
            }

            return result;
        }

        private List<LibraryTagDataObject> GetAllTags(SQLiteConnection conn)
        {
            var result = new List<LibraryTagDataObject>();

            var sb = new StringBuilder();
            sb.AppendLine("SELECT t.Tag, t.Id AS TagId, (SELECT COUNT(1) FROM TaggedChemistry tc WHERE tc.TagId = t.Id) as Frequency");
            sb.AppendLine("FROM Tags t");
            sb.AppendLine("ORDER BY Frequency DESC");

            var command = new SQLiteCommand(sb.ToString(), conn);

            using (SQLiteDataReader reader = command.ExecuteReader())
            {
                if (reader != null)
                {
                    while (reader.Read())
                    {
                        var dto = new LibraryTagDataObject();
                        dto.Text = reader["Tag"] as string;
                        dto.Id = (long)reader["TagId"];
                        dto.Frequency = (long)reader["Frequency"];
                        result.Add(dto);
                    }
                }
            }

            return result;
        }

        private SQLiteDataReader GetAllNames(SQLiteConnection conn)
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

        private List<ChemistryTagDataObject> GetAllChemistryTags(SQLiteConnection conn)
        {
            var result = new List<ChemistryTagDataObject>();

            var sb = new StringBuilder();
            sb.AppendLine("SELECT c.ChemistryId, c.Sequence, t.Tag, t.Id");
            sb.AppendLine("FROM TaggedChemistry c");
            sb.AppendLine("JOIN Tags t ON c.TagId = t.Id");
            sb.AppendLine("ORDER BY c.ChemistryId, c.Sequence");

            var command = new SQLiteCommand(sb.ToString(), conn);

            using (SQLiteDataReader reader = command.ExecuteReader())
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

        private List<ChemistryNameDataObject> GetAllChemicalNames(SQLiteConnection conn)
        {
            var results = new List<ChemistryNameDataObject>();

            var sb = new StringBuilder();
            sb.AppendLine("SELECT ChemicalNameId, Name, Namespace, Tag, ChemistryID");
            sb.AppendLine("FROM ChemicalNames");

            var command = new SQLiteCommand(sb.ToString(), conn);

            using (SQLiteDataReader reader = command.ExecuteReader())
            {
                if (reader != null)
                {
                    while (reader.Read())
                    {
                        var dto = new ChemistryNameDataObject();
                        dto.Id = (long)reader["ChemicalNameId"];
                        dto.Name = reader["Name"] as string;
                        dto.NameSpace = reader["Namespace"] as string;
                        dto.Tag = reader["Tag"] as string;
                        dto.ChemistryId = (long)reader["ChemistryId"];
                        results.Add(dto);
                    }
                }
            }

            return results;
        }

        private List<ChemistryNameDataObject> GetAllChemicalFormulae(SQLiteConnection conn)
        {
            var results = new List<ChemistryNameDataObject>();

            var sb = new StringBuilder();
            sb.AppendLine("SELECT ChemicalFormulaId, Name, Namespace, Tag, ChemistryID");
            sb.AppendLine("FROM ChemicalFormulae");

            var command = new SQLiteCommand(sb.ToString(), conn);

            using (SQLiteDataReader reader = command.ExecuteReader())
            {
                if (reader != null)
                {
                    while (reader.Read())
                    {
                        var dto = new ChemistryNameDataObject();
                        dto.Id = (long)reader["ChemicalFormulaId"];
                        dto.Name = reader["Name"] as string;
                        dto.NameSpace = reader["Namespace"] as string;
                        dto.Tag = reader["Tag"] as string;
                        dto.ChemistryId = (long)reader["ChemistryId"];
                        results.Add(dto);
                    }
                }
            }

            return results;
        }

        private List<ChemistryNameDataObject> GetAllChemicalCaptions(SQLiteConnection conn)
        {
            var results = new List<ChemistryNameDataObject>();

            var sb = new StringBuilder();
            sb.AppendLine("SELECT ChemicalCaptionId, Name, Namespace, Tag, ChemistryID");
            sb.AppendLine("FROM ChemicalCaptions");

            var command = new SQLiteCommand(sb.ToString(), conn);

            using (SQLiteDataReader reader = command.ExecuteReader())
            {
                if (reader != null)
                {
                    while (reader.Read())
                    {
                        var dto = new ChemistryNameDataObject();
                        dto.Id = (long)reader["ChemicalCaptionId"];
                        dto.Name = reader["Name"] as string;
                        dto.NameSpace = reader["Namespace"] as string;
                        dto.Tag = reader["Tag"] as string;
                        dto.ChemistryId = (long)reader["ChemistryId"];
                        results.Add(dto);
                    }
                }
            }

            return results;
        }

        private SQLiteDataReader GetAllChemistry(SQLiteConnection conn)
        {
            var sb = new StringBuilder();

            sb.AppendLine("SELECT Id, Chemistry, Name, Formula, MolWeight, DataType");
            sb.AppendLine("FROM Gallery");
            sb.AppendLine("ORDER BY Name");

            var command = new SQLiteCommand(sb.ToString(), conn);
            return command.ExecuteReader();
        }

        private void DeleteNames(SQLiteConnection conn, long chemistryId)
        {
            var sb = new StringBuilder();
            sb.AppendLine("DELETE FROM ChemicalNames");
            sb.AppendLine("WHERE ChemistryId = @id");

            var nameCommand = new SQLiteCommand(sb.ToString(), conn);
            nameCommand.Parameters.Add("@id", DbType.Int64, 20).Value = chemistryId;
            nameCommand.ExecuteNonQuery();
        }

        private void DeleteFormulae(SQLiteConnection conn, long chemistryId)
        {
            var sb = new StringBuilder();
            sb.AppendLine("DELETE FROM ChemicalFormulae");
            sb.AppendLine("WHERE ChemistryId = @id");

            var nameCommand = new SQLiteCommand(sb.ToString(), conn);
            nameCommand.Parameters.Add("@id", DbType.Int64, 20).Value = chemistryId;
            nameCommand.ExecuteNonQuery();
        }

        private void DeleteCaptions(SQLiteConnection conn, long chemistryId)
        {
            var sb = new StringBuilder();
            sb.AppendLine("DELETE FROM ChemicalCaptions");
            sb.AppendLine("WHERE ChemistryId = @id");

            var nameCommand = new SQLiteCommand(sb.ToString(), conn);
            nameCommand.Parameters.Add("@id", DbType.Int64, 20).Value = chemistryId;
            nameCommand.ExecuteNonQuery();
        }

        private void DeleteTags(SQLiteConnection conn, long chemistryId)
        {
            var sb = new StringBuilder();
            sb.AppendLine("DELETE FROM TaggedChemistry");
            sb.AppendLine("WHERE ChemistryId = @id");

            var tagCommand = new SQLiteCommand(sb.ToString(), conn);
            tagCommand.Parameters.Add("@id", DbType.Int64, 20).Value = chemistryId;
            tagCommand.ExecuteNonQuery();
        }
    }
}