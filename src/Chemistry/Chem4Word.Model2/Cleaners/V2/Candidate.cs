// ---------------------------------------------------------------------------
//  Copyright (c) 2026, The .NET Foundation.
//  This software is released under the Apache Licence, Version 2.0.
//  The licence and further copyright text can be found in the file LICENCE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System.Collections.Generic;

namespace Chem4Word.Model2.Cleaners.V2
{
    internal sealed class Candidate
    {
        public double[] X;
        public double[] Y;
        public double Congestion;
        public List<(int, int)> Clashes;

        public bool IsBetterThan(Candidate other) =>
            Clashes.Count < other.Clashes.Count || (Clashes.Count == other.Clashes.Count && Congestion < other.Congestion);
    }
}
