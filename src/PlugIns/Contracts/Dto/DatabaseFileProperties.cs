// ---------------------------------------------------------------------------
//  Copyright (c) 2025, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System.Collections.Generic;

namespace IChem4Word.Contracts.Dto
{
    public class DatabaseFileProperties
    {
        public bool FileExists { get; set; }
        public bool IsSqliteDatabase { get; set; }
        public bool IsReadOnly { get; set; }
        public bool IsChem4Word { get; set; }
        public bool RequiresPatching { get; set; }

        public Dictionary<string, string> Properties { get; set; } = new Dictionary<string, string>();
    }
}