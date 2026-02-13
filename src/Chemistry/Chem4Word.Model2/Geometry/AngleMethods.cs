// ---------------------------------------------------------------------------
//  Copyright (c) 2026, The .NET Foundation.
//  This software is released under the Apache Licence, Version 2.0.
//  The licence and further copyright text can be found in the file LICENCE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.Core.Enums;
using Chem4Word.Core.Helpers;
using Chem4Word.Model2.Enums;
using System.Windows;
using System.Windows.Media;

namespace Chem4Word.Model2.Geometry
{
    public static class AngleMethods
    {
        public static Vector ToVector(this ClockDirections dir)
        {
            Matrix rotator = new Matrix();
            rotator.Rotate((int)dir.ToDegrees());
            return GeometryTool.ScreenNorth * rotator;
        }

        public static double ToDegrees(this ClockDirections cd)
        {
            return 30 * ((int)cd % 12);
        }

        /// <summary>
        /// Splits the angle between two clock directions
        /// </summary>
        /// <param name="first"></param>
        /// <param name="second"></param>
        /// <returns>A clock direction pointing to the new direction </returns>
        public static ClockDirections Split(this ClockDirections first, ClockDirections second)
        {
            return (ClockDirections)((((int)first + (int)second) % 12) / 2);
        }

        public static double ToDegrees(this CompassPoints cp)
        {
            return 45 * (int)cp;
        }
    }
}
