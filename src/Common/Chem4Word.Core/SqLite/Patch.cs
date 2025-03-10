﻿// ---------------------------------------------------------------------------
//  Copyright (c) 2025, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace Chem4Word.Core.SqLite
{
    public class Patch
    {
        public Version Version { get; set; }
        public List<string> Scripts { get; set; } = new List<string>();
    }
}