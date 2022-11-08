// ---------------------------------------------------------------------------
//  Copyright (c) 2022, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.Driver.Open.SqLite;
using IChem4Word.Contracts;
using IChem4Word.Contracts.Dto;
using System.Collections.Generic;
using System.Data.SQLite;
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

        public Dictionary<string, string> GetProperties()
        {
            var result = new Dictionary<string, string>();

            if (DatabaseDetails != null)
            {
                var library = new Library(Telemetry, DatabaseDetails, TopLeft);
                result = library.GetProperties();
            }

            return result;
        }

        public Dictionary<string, int> GetLibraryNames()
        {
            var result = new Dictionary<string, int>();

            if (DatabaseDetails != null)
            {
                var library = new Library(Telemetry, DatabaseDetails, TopLeft);
                result = library.GetLibraryNames();
            }

            return result;
        }

        public List<ChemistryDataObject> GetAllChemistry()
        {
            var result = new List<ChemistryDataObject>();

            if (DatabaseDetails != null)
            {
                var library = new Library(Telemetry, DatabaseDetails, TopLeft);
                result = library.GetAllChemistry();
            }

            return result;
        }

        public long AddChemistry(ChemistryDataObject chemistry)
        {
            long result = -1;

            if (DatabaseDetails != null)
            {
                var library = new Library(Telemetry, DatabaseDetails, TopLeft);
                result = library.AddChemistry(chemistry);
            }

            return result;
        }

        public void UpdateChemistry(ChemistryDataObject chemistry)
        {
            if (DatabaseDetails != null)
            {
                var library = new Library(Telemetry, DatabaseDetails, TopLeft);
                library.UpdateChemistry(chemistry);
            }
        }

        public ChemistryDataObject GetChemistryById(long id)
        {
            var result = new ChemistryDataObject();

            if (DatabaseDetails != null)
            {
                var library = new Library(Telemetry, DatabaseDetails, TopLeft);
                result = library.GetChemistryById(id);
            }

            return result;
        }

        public void DeleteAllChemistry()
        {
            if (DatabaseDetails != null)
            {
                var library = new Library(Telemetry, DatabaseDetails, TopLeft);
                library.DeleteAllChemistry();
            }
        }

        public void DeleteChemistryById(long id)
        {
            if (DatabaseDetails != null)
            {
                var library = new Library(Telemetry, DatabaseDetails, TopLeft);
                library.DeleteChemistryById(id);
            }
        }

        public void AddTags(long id, List<string> tags)
        {
            if (DatabaseDetails != null)
            {
                var library = new Library(Telemetry, DatabaseDetails, TopLeft);
                library.AddTags(id, tags);
            }
        }

        public List<LibraryTagDataObject> GetAllTags()
        {
            var result = new List<LibraryTagDataObject>();

            if (DatabaseDetails != null)
            {
                var library = new Library(Telemetry, DatabaseDetails, TopLeft);
                result = library.GetAllTags();
            }

            return result;
        }
    }
}