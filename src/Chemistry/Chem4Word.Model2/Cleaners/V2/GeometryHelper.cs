// ---------------------------------------------------------------------------
//  Copyright (c) 2026, The .NET Foundation.
//  This software is released under the Apache Licence, Version 2.0.
//  The licence and further copyright text can be found in the file LICENCE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace Chem4Word.Model2.Cleaners.V2
{
    internal static class GeometryHelper
    {
        internal const double Degrees = Math.PI / 180;

        /// <summary>
        /// Directions for 'count' new bonds around an atom whose existing bonds point along 'taken'.
        /// Gaps containing a 'forbidden' direction (a ring interior) are skipped unless there is nothing else;
        /// the new bonds are shared out so the widest gaps get the most, and are spread evenly within each gap.
        /// </summary>
        internal static double[] SpreadIntoGaps(IList<double> taken, int count, IEnumerable<double> forbidden)
        {
            if (count == 0)
            {
                return new double[0];
            }
            if (taken.Count == 0)
            {
                return Enumerable.Range(0, count).Select(k => 2 * Math.PI * k / count).ToArray();
            }

            double[] sorted = taken.Select(NormalizeAngle).OrderBy(a => a).ToArray();
            List<(double start, double size)> gaps = new List<(double start, double size)>();
            for (int i = 0; i < sorted.Length; i++)
            {
                double size = sorted.Length == 1 ? 2 * Math.PI : NormalizeAngle(sorted[(i + 1) % sorted.Length] - sorted[i]);
                gaps.Add((sorted[i], size));
            }

            List<double> blocked = forbidden.ToList();
            List<(double start, double size)> open = gaps.Where(g => !blocked.Any(f => NormalizeAngle(f - g.start) < g.size)).ToList();
            if (open.Count == 0)
            {
                open = gaps;
            }

            int[] shares = new int[open.Count];
            for (int k = 0; k < count; k++)
            {
                int widest = Enumerable.Range(0, open.Count)
                                       .OrderByDescending(i => Math.Round(open[i].size / (shares[i] + 1), 6))
                                       .ThenByDescending(i => open[i].size)
                                       .First();
                shares[widest]++;
            }

            List<double> result = new List<double>();
            for (int i = 0; i < open.Count; i++)
            {
                for (int j = 0; j < shares[i]; j++)
                {
                    result.Add(open[i].start + open[i].size * (j + 1) / (shares[i] + 1));
                }
            }
            return result.ToArray();
        }

        internal static double NormalizeAngle(double a)
        {
            a %= 2 * Math.PI;
            return a < 0 ? a + 2 * Math.PI : a;
        }

        internal static double Angle(Point from, Point to) => Math.Atan2(to.Y - from.Y, to.X - from.X);

        internal static Point Offset(Point origin, double angle, double length) =>
            new Point(origin.X + length * Math.Cos(angle), origin.Y + length * Math.Sin(angle));

        internal static Point Centroid(IEnumerable<Point> points)
        {
            List<Point> list = points.ToList();
            return new Point(list.Average(p => p.X), list.Average(p => p.Y));
        }
    }
}
