// ---------------------------------------------------------------------------
//  Copyright (c) 2023, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.Core.SqLite;
using IChem4Word.Contracts;
using IChem4Word.Contracts.Dto;
using System.Collections.Generic;
using System.Data.SQLite;
using Point = System.Windows.Point;

namespace Chem4Word.Driver.Open
{
    public class OpenDriver : IChem4WordLibraryWriter
    {
        public string Name => "SQLite Standard";
        public string Description => "This is the standard Chem4Word SQLite Library Driver";

        public Point TopLeft { get; set; }

        public IChem4WordTelemetry Telemetry { get; set; }
        public string FileName { get; set; }
        public string BackupFolder { get; set; }

        private SQLiteTransaction _transaction;
        private Library _library;

        public void StartTransaction()
        {
            _library = new Library(Telemetry, FileName, BackupFolder, TopLeft);
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

        public void CreateNewDatabase(string filename)
        {
            Library.CreateNewDatabase(filename);
            // Call this to ensure that any patching is done
            _ = new Library(Telemetry, filename, BackupFolder, TopLeft);
        }

        public DatabaseFileProperties GetDatabaseFileProperties(string fileName)
        {
            var library = new Library(Telemetry, fileName, BackupFolder, TopLeft);
            return library.Database;
        }

        public Dictionary<string, int> GetSubstanceNamesWithIds()
        {
            var library = new Library(Telemetry, FileName, BackupFolder, TopLeft);
            return library.GetSubstanceNamesWithIds();
        }

        public List<ChemistryDataObject> GetAllChemistry()
        {
            var library = new Library(Telemetry, FileName, BackupFolder, TopLeft);
            return library.GetAllChemistry();
        }

        public long AddChemistry(ChemistryDataObject chemistry)
        {
            long result = -1;

            if (_transaction != null)
            {
                result = _library.AddChemistryCommand(_transaction.Connection, chemistry);
            }
            else
            {
                var library = new Library(Telemetry, FileName, BackupFolder, TopLeft);
                result = library.AddChemistry(chemistry);
            }

            return result;
        }

        public void UpdateChemistry(ChemistryDataObject chemistry)
        {
            if (_transaction != null)
            {
                _library.UpdateChemistryCommand(_transaction.Connection, chemistry);
            }
            else
            {
                var library = new Library(Telemetry, FileName, BackupFolder, TopLeft);
                library.UpdateChemistry(chemistry);
            }
        }

        public ChemistryDataObject GetChemistryById(long id)
        {
            var library = new Library(Telemetry, FileName, BackupFolder, TopLeft);
            return library.GetChemistryById(id);
        }

        public void DeleteAllChemistry()
        {
            var library = new Library(Telemetry, FileName, BackupFolder, TopLeft);
            library.DeleteAllChemistry();
        }

        public void DeleteChemistryById(long id)
        {
            if (_transaction != null)
            {
                _library.DeleteChemistryByIdCommand(_transaction.Connection, id);
            }
            else
            {
                var library = new Library(Telemetry, FileName, BackupFolder, TopLeft);
                library.DeleteChemistryById(id);
            }
        }

        public void AddTags(long id, List<string> tags)
        {
            var library = new Library(Telemetry, FileName, BackupFolder, TopLeft);
            library.AddTags(id, tags);
        }

        public List<LibraryTagDataObject> GetAllTags()
        {
            var library = new Library(Telemetry, FileName, BackupFolder, TopLeft);
            return library.GetAllTags();
        }
    }
}