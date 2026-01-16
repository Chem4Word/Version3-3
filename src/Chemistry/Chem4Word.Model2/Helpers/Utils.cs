// ---------------------------------------------------------------------------
//  Copyright (c) 2026, The .NET Foundation.
//  This software is released under the Apache Licence, Version 2.0.
//  The licence and further copyright text can be found in the file LICENCE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.Core.Enums;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Chem4Word.Model2.Helpers
{
    public static class Utils
    {
        public static bool AreAllH(this IEnumerable<Atom> atomlist)
        {
            return atomlist.All(a => a.Element as Element == ModelGlobals.PeriodicTable.H);
        }

        public static bool ContainNoH(this IEnumerable<Atom> atomList)
        {
            return atomList.All(a => a.Element as Element != ModelGlobals.PeriodicTable.H && a.ImplicitHydrogenCount == 0);
        }

        public static Atom GetFirstNonH(this IEnumerable<Atom> atomList)
        {
            return atomList.FirstOrDefault(a => a.Element as Element != ModelGlobals.PeriodicTable.H);
        }

        public static int GetHCount(this IEnumerable<Atom> atomList)
        {
            return atomList.Count(a => a.Element as Element == ModelGlobals.PeriodicTable.H);
        }

        public static int GetNonHCount(this IEnumerable<Atom> atomList)
        {
            return atomList.Count() - atomList.GetHCount();
        }

        public static CompassPoints NextCompassPoint(CompassPoints? point)
        {
            CompassPoints result = CompassPoints.North;

            if (point != null)
            {
                Array points = Enum.GetValues(typeof(CompassPoints));
                int index = (int)point;
                index++;

                if (index < points.Length)
                {
                    result = (CompassPoints)index;
                }
            }

            return result;
        }
    }
}
