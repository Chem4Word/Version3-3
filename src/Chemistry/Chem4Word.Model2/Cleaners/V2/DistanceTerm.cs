// ---------------------------------------------------------------------------
//  Copyright (c) 2026, The .NET Foundation.
//  This software is released under the Apache Licence, Version 2.0.
//  The licence and further copyright text can be found in the file LICENCE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

namespace Chem4Word.Model2.Cleaners.V2
{
    internal struct DistanceTerm
    {
        public readonly int I;
        public readonly int J;
        public readonly double Target2;
        public readonly double Weight;
        public readonly bool LowerBoundOnly;
        public readonly bool IsBond;

        public DistanceTerm(int i, int j, double target2, double weight, bool lowerBoundOnly, bool isBond = false)
        {
            I = i;
            J = j;
            Target2 = target2;
            Weight = weight;
            LowerBoundOnly = lowerBoundOnly;
            IsBond = isBond;
        }
    }
}
