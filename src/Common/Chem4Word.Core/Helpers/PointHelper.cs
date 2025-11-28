// ---------------------------------------------------------------------------
//  Copyright (c) 2026, The .NET Foundation.
//  This software is released under the Apache Licence, Version 2.0.
//  The licence and further copyright text can be found in the file LICENCE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using Point = System.Windows.Point;

namespace Chem4Word.Core.Helpers
{
    public static class PointHelper
    {
        public static bool PointIsEmpty(Point point)
            => point.Equals(new Point(0, 0));

        /// <summary>
        /// Returns a point which can be used as the top left hand corner of a form without hiding any part of it off bottom right of current screen
        /// </summary>
        /// <param name="point">Starting point</param>
        /// <param name="screen">Screen object</param>
        /// <param name="width">Desired width</param>
        /// <param name="height">Desired Height</param>
        /// <returns></returns>
        public static Point SensibleTopLeft(Point point, Screen screen, int width, int height)
        {
            Debug.WriteLine($"Point: {AsString0(point)} Screen WorkingArea: {screen.WorkingArea}");
            Rectangle bounds = screen.WorkingArea;

            int x = (int)point.X;
            int y = (int)point.Y;

            // Clamp X so form fits horizontally
            if (x + width > bounds.Right)
            {
                x = bounds.Right - width;
            }

            if (x < bounds.Left)
            {
                x = bounds.Left;
            }

            // Clamp Y so form fits vertically
            if (y + height > bounds.Bottom)
            {
                y = bounds.Bottom - height;
            }

            if (y < bounds.Top)
            {
                y = bounds.Top;
            }

            Point result = new Point(x, y);

            Debug.WriteLine($"  Result: {AsString0(result)}");

            return result;
        }

        public static string AsString(Point p)
            => $"{SafeDouble.AsString4(p.X)},{SafeDouble.AsString4(p.Y)}";

        public static string AsString0(Point p)
            => $"X: {SafeDouble.AsString0(p.X)} Y:{SafeDouble.AsString0(p.Y)}";

        public static object AsCMLString(Point p) =>
            $"{SafeDouble.AsCMLString(p.X)},{SafeDouble.AsCMLString(p.Y)}";
    }
}
