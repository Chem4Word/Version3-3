// ---------------------------------------------------------------------------
//  Copyright (c) 2026, The .NET Foundation.
//  This software is released under the Apache Licence, Version 2.0.
//  The licence and further copyright text can be found in the file LICENCE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

namespace Chem4Word.Models
{
    public class LibrariesSettingsGridSource
    {
        public string Name { get; set; }
        public string FileName { get; set; }
        public string Connection { get; set; }
        public string Count { get; set; }
        public bool Dictionary { get; set; }
        public string Locked { get; set; }
        public string License { get; set; }
        public bool IsDefault { get; set; }
    }
}