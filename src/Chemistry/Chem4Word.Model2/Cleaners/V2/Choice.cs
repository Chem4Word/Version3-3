// ---------------------------------------------------------------------------
//  Copyright (c) 2026, The .NET Foundation.
//  This software is released under the Apache Licence, Version 2.0.
//  The licence and further copyright text can be found in the file LICENCE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System.Collections.Generic;

namespace Chem4Word.Model2.Cleaners.V2
{
    // One alternative for a constraint: the angle of each neighbour, in each atom's own frame.
    internal sealed class Choice
    {
        public double Weight;
        public int Mirror = 1;
        public readonly List<(int atom, double[] angles)> Angles = new List<(int atom, double[] angles)>();
    }
}
