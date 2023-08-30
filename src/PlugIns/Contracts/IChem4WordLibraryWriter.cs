// ---------------------------------------------------------------------------
//  Copyright (c) 2023, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using IChem4Word.Contracts.Dto;
using System.Collections.Generic;

namespace IChem4Word.Contracts
{
    public interface IChem4WordLibraryWriter : IChem4WordLibraryReader
    {
        void StartTransaction();

        void CommitTransaction();

        void RollbackTransaction();

        void CreateNewDatabase(string filename);

        long AddChemistry(ChemistryDataObject chemistry);

        void UpdateChemistry(ChemistryDataObject chemistry);

        void DeleteAllChemistry();

        void DeleteChemistryById(long id);

        void AddTags(long id, List<string> tags);
    }
}