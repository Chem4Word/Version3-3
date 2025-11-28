// ---------------------------------------------------------------------------
//  Copyright (c) 2026, The .NET Foundation.
//  This software is released under the Apache Licence, Version 2.0.
//  The licence and further copyright text can be found in the file LICENCE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.ACME.Models;
using System;
using System.Collections;

namespace Chem4Word.Helpers
{
    public class ChemistryObjectComparer : IComparer
    {
        public int Compare(object x, object y)
        {
            if (x is ChemistryObject xo && y is ChemistryObject yo)
            {
                return string.Compare(xo.Name, yo.Name, StringComparison.OrdinalIgnoreCase);
            }

            return 0;
        }
    }
}