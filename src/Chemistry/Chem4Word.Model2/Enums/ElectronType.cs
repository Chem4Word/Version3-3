// ---------------------------------------------------------------------------
//  Copyright (c) 2026, The .NET Foundation.
//  This software is released under the Apache Licence, Version 2.0.
//  The licence and further copyright text can be found in the file LICENCE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System.ComponentModel;

namespace Chem4Word.Model2.Enums
{
    public enum ElectronType
    {
        [Description("Radical")]
        Radical = 1,
        [Description("Lone Pair")]
        LonePair = 2,
        [Description("Carbenoid")]
        Carbenoid = 3
    }
}
