// ---------------------------------------------------------------------------
//  Copyright (c) 2026, The .NET Foundation.
//  This software is released under the Apache Licence, Version 2.0.
//  The licence and further copyright text can be found in the file LICENCE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

namespace Chem4Word.Model2.Cleaners.V1
{
    internal struct Term
    {
        public readonly int I;
        public readonly int J;
        public readonly double Target;
        public readonly double Weight;
        public readonly bool LowerBoundOnly;

        public Term(int i, int j, double target, double weight, bool lowerBoundOnly)
        {
            I = i;
            J = j;
            Target = target;
            Weight = weight;
            LowerBoundOnly = lowerBoundOnly;
        }
    }
}
