// ---------------------------------------------------------------------------
//  Copyright (c) 2022, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System.Collections.Generic;
using System.Windows;
using IChem4Word.Contracts.Dto;

namespace IChem4Word.Contracts
{
    public interface IChem4WordDriver
    {
        string Name { get; }

        string Description { get; }

        Point TopLeft { get; set; }

        IChem4WordTelemetry Telemetry { get; set; }
        DatabaseDetails DatabaseDetails { get; set; }

        // I/O
        Dictionary<string, string> GetProperties();
        Dictionary<string, int> GetLibraryNames();

        List<ChemistryDataObject> GetAllChemistry();
        long AddChemistry(ChemistryDataObject chemistry);
        void UpdateChemistry(ChemistryDataObject chemistry);
        ChemistryDataObject GetChemistryById(long id);
        void DeleteAllChemistry();
        void DeleteChemistryById(long id);

        List<LibraryTagDataObject> GetAllTags();
        void AddTags(long Id, List<string> tags);
    }
}