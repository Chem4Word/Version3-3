// ---------------------------------------------------------------------------
//  Copyright (c) 2023, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.Driver.Open.SqLite;
using IChem4Word.Contracts;
using IChem4Word.Contracts.Dto;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Reflection;
using Point = System.Windows.Point;

namespace Chem4Word.Driver.Open
{
    public class OpenDriver : IChem4WordDriver
    {
        private static string _product = Assembly.GetExecutingAssembly().FullName.Split(',')[0];
        private static string _class = MethodBase.GetCurrentMethod().DeclaringType?.Name;

        public string Name => "SQLite Standard";
        public string Description => "This is the standard Chem4Word SQLite Library Driver";

        public Point TopLeft { get; set; }

        public IChem4WordTelemetry Telemetry { get; set; }
        public DatabaseDetails DatabaseDetails { get; set; }
        public string BackupFolder { get; set; }

        private SQLiteTransaction _transaction;
        private Library _library;

        public void StartTransaction()
        {
            _library = new Library(Telemetry, DatabaseDetails, BackupFolder, TopLeft);
            var connection = _library.LibraryConnection();
            _transaction = connection.BeginTransaction();
        }

        public void CommitTransaction()
        {
            if (_transaction != null)
            {
                var conn = _transaction.Connection;
                _transaction.Commit();

#if DEBUG
                var command = new SQLiteCommand("VACUUM", conn);
                command.ExecuteNonQuery();
#endif

                conn.Close();
                conn.Dispose();
            }

            _transaction = null;
            _library = null;
        }

        public void RollbackTransaction()
        {
            if (_transaction != null)
            {
                var conn = _transaction.Connection;
                _transaction.Rollback();

                conn.Close();
                conn.Dispose();
            }

            _transaction = null;
            _library = null;
        }

        public void CreateNewDatabase(DatabaseDetails details)
        {
            Library.CreateNewDatabase(details.Connection);

            var library = new Library(Telemetry, details, BackupFolder, TopLeft);
            // Fetch it's properties, which will apply patches
            library.GetProperties();
        }

        public DatabaseFileProperties GetDatabaseFileProperties(DatabaseDetails details)
        {
            var module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod()?.Name}()";

            var result = new DatabaseFileProperties();

            if (File.Exists(details.Connection))
            {
                try
                {
                    var library = new Library(Telemetry, details, BackupFolder, TopLeft);

                    result.IsReadOnly = library.IsReadOnly();

                    var foundGallery = library.TableExists("Gallery");
                    var foundChemicalNames = library.TableExists("ChemicalNames");

                    if (foundGallery && foundChemicalNames)
                    {
                        result.IsChem4Word = library.CheckGalleryExists() && library.CheckChemicalNamesExists();
                    }

                    result.RequiresPatching = library.RequiresPatching();
                }
                catch (Exception exception)
                {
                    Telemetry.Write(module, "Exception", exception.Message);
                    Telemetry.Write(module, "Exception", exception.StackTrace);
                }
            }

            return result;
        }

        public Dictionary<string, string> GetProperties()
        {
            var result = new Dictionary<string, string>();

            if (DatabaseDetails != null)
            {
                var library = new Library(Telemetry, DatabaseDetails, BackupFolder, TopLeft);
                result = library.GetProperties();
            }

            return result;
        }

        public Dictionary<string, int> GetSubstanceNamesWithIds()
        {
            var result = new Dictionary<string, int>();

            if (DatabaseDetails != null)
            {
                var library = new Library(Telemetry, DatabaseDetails, BackupFolder, TopLeft);
                result = library.GetSubstanceNamesWithIds();
            }

            return result;
        }

        public List<ChemistryDataObject> GetAllChemistry()
        {
            var result = new List<ChemistryDataObject>();

            if (DatabaseDetails != null)
            {
                var library = new Library(Telemetry, DatabaseDetails, BackupFolder, TopLeft);
                result = library.GetAllChemistry();
            }

            return result;
        }

        public long AddChemistry(ChemistryDataObject chemistry)
        {
            long result = -1;

            if (DatabaseDetails != null)
            {
                if (_transaction != null)
                {
                    result = _library.AddChemistry(_transaction.Connection, chemistry);
                }
                else
                {
                    var library = new Library(Telemetry, DatabaseDetails, BackupFolder, TopLeft);
                    result = library.AddChemistry(chemistry);
                }
            }

            return result;
        }

        public void UpdateChemistry(ChemistryDataObject chemistry)
        {
            if (DatabaseDetails != null)
            {
                if (_transaction != null)
                {
                    _library.UpdateChemistry(_transaction.Connection, chemistry);
                }
                else
                {
                    var library = new Library(Telemetry, DatabaseDetails, BackupFolder, TopLeft);
                    library.UpdateChemistry(chemistry);
                }
            }
        }

        public ChemistryDataObject GetChemistryById(long id)
        {
            var result = new ChemistryDataObject();

            if (DatabaseDetails != null)
            {
                var library = new Library(Telemetry, DatabaseDetails, BackupFolder, TopLeft);
                result = library.GetChemistryById(id);
            }

            return result;
        }

        public void DeleteAllChemistry()
        {
            if (DatabaseDetails != null)
            {
                var library = new Library(Telemetry, DatabaseDetails, BackupFolder, TopLeft);
                library.DeleteAllChemistry();
            }
        }

        public void DeleteChemistryById(long id)
        {
            if (DatabaseDetails != null)
            {
                if (_transaction != null)
                {
                    _library.DeleteChemistryById(_transaction.Connection, id);
                }
                else
                {
                    var library = new Library(Telemetry, DatabaseDetails, BackupFolder, TopLeft);
                    library.DeleteChemistryById(id);
                }
            }
        }

        public void AddTags(long id, List<string> tags)
        {
            if (DatabaseDetails != null)
            {
                var library = new Library(Telemetry, DatabaseDetails, BackupFolder, TopLeft);
                library.AddTags(id, tags);
            }
        }

        public List<LibraryTagDataObject> GetAllTags()
        {
            var result = new List<LibraryTagDataObject>();

            if (DatabaseDetails != null)
            {
                var library = new Library(Telemetry, DatabaseDetails, BackupFolder, TopLeft);
                result = library.GetAllTags();
            }

            return result;
        }
    }
}