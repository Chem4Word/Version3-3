// ---------------------------------------------------------------------------
//  Copyright (c) 2022, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace IChem4Word.Contracts.Dto
{
    public class ChemistryDataObject
    {
        public long Id { get; set; }
        public byte[] Chemistry { get; set; }
        public string DataType { get; set; }
        public string Name { get; set; }
        public string Formula { get; set; }
        public double MolWeight { get; set; }
        public List<ChemistryTagDataObject> Tags { get; set; } = new List<ChemistryTagDataObject>();
        public List<ChemistryNameDataObject> Names { get; set; } = new List<ChemistryNameDataObject>();
    }
}