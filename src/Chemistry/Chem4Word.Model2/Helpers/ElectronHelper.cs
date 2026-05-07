// ---------------------------------------------------------------------------
//  Copyright (c) 2026, The .NET Foundation.
//  This software is released under the Apache Licence, Version 2.0.
//  The licence and further copyright text can be found in the file LICENCE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.Core.Enums;
using Chem4Word.Model2.Enums;

namespace Chem4Word.Model2.Helpers
{
    public static class ElectronHelper
    {
        public static Electron MakeElectron(Atom parent, int index, ElectronType type, CompassPoints? compassPoint = null)
        {
            Electron electron = new Electron
            {
                Id = $"e{index}",
                TypeOfElectron = type,
                Parent = parent,
                ExplicitPlacement = compassPoint
            };

            return electron;
        }
    }
}
