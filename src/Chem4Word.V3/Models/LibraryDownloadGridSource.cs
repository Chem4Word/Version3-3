﻿// ---------------------------------------------------------------------------
//  Copyright (c) 2025, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

namespace Chem4Word.Models
{
    public class LibraryDownloadGridSource
    {
        public bool RequiresPayment { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Sku { get; set; }
    }
}