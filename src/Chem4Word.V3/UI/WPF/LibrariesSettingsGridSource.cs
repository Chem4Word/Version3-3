// ---------------------------------------------------------------------------
//  Copyright (c) 2022, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

namespace Chem4Word.UI.WPF
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