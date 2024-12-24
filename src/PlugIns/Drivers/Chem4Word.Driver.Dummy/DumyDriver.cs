// ---------------------------------------------------------------------------
//  Copyright (c) 2025, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using IChem4Word.Contracts;
using IChem4Word.Contracts.Dto;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;

namespace Chem4Word.Driver.Dummy
{
    public class DummyDriver : IChem4WordLibraryWriter
    {
        public string Name => "Dummy Library Driver";
        public string Description => "Dummy Library Driver Writer implementing IChem4WordLibraryWriter";

        public Point TopLeft { get; set; }
        public IChem4WordTelemetry Telemetry { get; set; }
        public DatabaseDetails DatabaseDetails { get; set; }
        public string BackupFolder { get; set; }
        public string FileName { get; set; }

        public void StartTransaction()
        {
            Debugger.Break();
        }

        public void CommitTransaction()
        {
            Debugger.Break();
        }

        public void RollbackTransaction()
        {
            Debugger.Break();
        }

        public void CreateNewDatabase(string filename)
        {
            Debugger.Break();
        }

        public Dictionary<string, string> GetProperties() => new Dictionary<string, string>();

        public DatabaseFileProperties GetDatabaseFileProperties(string fileName) =>
            new DatabaseFileProperties
            {
                IsSqliteDatabase = true,
                IsChem4Word = true,
                Properties = new Dictionary<string, string>()
            };

        public Dictionary<string, int> GetSubstanceNamesWithIds() => new Dictionary<string, int>();

        public List<ChemistryDataObject> GetAllChemistry() => new List<ChemistryDataObject>();

        public long AddChemistry(ChemistryDataObject chemistry) => -1;

        public void UpdateChemistry(ChemistryDataObject chemistry)
        {
            Debugger.Break();
        }

        public ChemistryDataObject GetChemistryById(long id) => new ChemistryDataObject();

        public void DeleteAllChemistry()
        {
            Debugger.Break();
        }

        public void DeleteChemistryById(long id)
        {
            Debugger.Break();
        }

        public List<LibraryTagDataObject> GetAllTags() => new List<LibraryTagDataObject>();

        public void AddTags(long id, List<string> tags)
        {
            Debugger.Break();
        }
    }
}