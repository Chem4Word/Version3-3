// ---------------------------------------------------------------------------
//  Copyright (c) 2025, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

namespace IChem4Word.Contracts.Dto
{
    public class ChemistryNameDataObject
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string NameSpace { get; set; }
        public string Tag { get; set; }
        public long ChemistryId { get; set; }
    }
}