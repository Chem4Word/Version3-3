// ---------------------------------------------------------------------------
//  Copyright (c) 2026, The .NET Foundation.
//  This software is released under the Apache Licence, Version 2.0.
//  The licence and further copyright text can be found in the file LICENCE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace Chem4Word.Model2.Cleaners.V2
{
    internal sealed class Fragment
    {
        public readonly Dictionary<int, Point> Positions = new Dictionary<int, Point>();

        public Rect Bounds()
        {
            double minX = Positions.Values.Min(p => p.X), maxX = Positions.Values.Max(p => p.X);
            double minY = Positions.Values.Min(p => p.Y), maxY = Positions.Values.Max(p => p.Y);
            return new Rect(minX, minY, maxX - minX, maxY - minY);
        }

        public void Translate(double dx, double dy)
        {
            foreach (int key in Positions.Keys.ToList())
            {
                Positions[key] = new Point(Positions[key].X + dx, Positions[key].Y + dy);
            }
        }
    }
}
