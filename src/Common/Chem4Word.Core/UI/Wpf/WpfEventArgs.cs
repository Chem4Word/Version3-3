// ---------------------------------------------------------------------------
//  Copyright (c) 2025, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System;

namespace Chem4Word.Core.UI.Wpf
{
    public class WpfEventArgs : EventArgs
    {
        // General properties
        public string Button { get; set; }

        public string ButtonDetails { get; set; }

        // Properties used for ACME Status Bar
        public string Message { get; set; }

        public string Formula { get; set; }
        public string MolecularWeight { get; set; }
    }
}
