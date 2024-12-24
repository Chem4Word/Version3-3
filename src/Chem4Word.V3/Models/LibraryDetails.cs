// ---------------------------------------------------------------------------
//  Copyright (c) 2025, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

namespace Chem4Word.Models
{
    public class LibraryDetails
    {
        public bool Allowed { get; set; }

        public string OriginalFileName { get; set; }

        public string Driver { get; set; }

        public string Message { get; set; }
    }
}