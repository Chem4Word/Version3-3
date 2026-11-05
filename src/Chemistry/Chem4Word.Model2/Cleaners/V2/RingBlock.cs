// ---------------------------------------------------------------------------
//  Copyright (c) 2026, The .NET Foundation.
//  This software is released under the Apache Licence, Version 2.0.
//  The licence and further copyright text can be found in the file LICENCE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System.Collections.Generic;
using System.Windows;

namespace Chem4Word.Model2.Cleaners.V2
{
    // A set of rings joined by shared atoms, laid out once in Cartesian coordinates and then treated as a rigid body.
    internal sealed class RingBlock
    {
        public int Id;
        public int[] Atoms;
        public List<int[]> Rings = new List<int[]>();
        public Dictionary<int, Point> Coords = new Dictionary<int, Point>();

        // Direction of every neighbour (in and out of the block) of each member atom, in the block's frame.
        public readonly Dictionary<int, double[]> Angles = new Dictionary<int, double[]>();
    }
}
