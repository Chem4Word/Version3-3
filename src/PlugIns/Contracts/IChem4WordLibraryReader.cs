// ---------------------------------------------------------------------------
//  Copyright (c) 2025, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using IChem4Word.Contracts.Dto;
using System.Collections.Generic;

namespace IChem4Word.Contracts
{
    public interface IChem4WordLibraryReader : IChem4WordLibraryBase
    {
        DatabaseFileProperties GetDatabaseFileProperties(string fileName);

        Dictionary<string, int> GetSubstanceNamesWithIds();

        List<ChemistryDataObject> GetAllChemistry();

        ChemistryDataObject GetChemistryById(long id);

        List<LibraryTagDataObject> GetAllTags();
    }
}