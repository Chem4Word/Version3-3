// ---------------------------------------------------------------------------
//  Copyright (c) 2026, The .NET Foundation.
//  This software is released under the Apache Licence, Version 2.0.
//  The licence and further copyright text can be found in the file LICENCE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System;
using System.Windows;

namespace Chem4Word.Renderer.OoXmlV4.Helpers
{
    public static class BezierUtils
    {
        public static Rect CubicBezierBounds(Point p0, Point p1, Point p2, Point p3)
        {
            double[] ts = new double[10];
            int count = 0;

            ts[count++] = 0.0;
            ts[count++] = 1.0;

            AddDerivativeRoots(p0.X, p1.X, p2.X, p3.X, ts, ref count);
            AddDerivativeRoots(p0.Y, p1.Y, p2.Y, p3.Y, ts, ref count);

            double minX = double.PositiveInfinity;
            double minY = double.PositiveInfinity;
            double maxX = double.NegativeInfinity;
            double maxY = double.NegativeInfinity;

            for (int i = 0; i < count; i++)
            {
                double t = ts[i];
                if (t < 0.0 || t > 1.0)
                {
                    continue;
                }

                Point p = EvaluateCubic(p0, p1, p2, p3, t);

                if (p.X < minX) minX = p.X;
                if (p.Y < minY) minY = p.Y;
                if (p.X > maxX) maxX = p.X;
                if (p.Y > maxY) maxY = p.Y;
            }

            return new Rect(new Point(minX, minY), new Point(maxX, maxY));
        }

        private static void AddDerivativeRoots(double p0, double p1, double p2, double p3,
                                               double[] ts, ref int count)
        {
            double a = -p0 + 3 * p1 - 3 * p2 + p3;
            double b = 2 * (p0 - 2 * p1 + p2);
            double c = -p0 + p1;

            if (Math.Abs(a) < 1e-12)
            {
                if (Math.Abs(b) > 1e-12)
                {
                    double t = -c / b;
                    ts[count++] = t;
                }
                return;
            }

            double disc = b * b - 4 * a * c;
            if (disc < 0)
            {
                return;
            }

            double sqrtD = Math.Sqrt(disc);

            ts[count++] = (-b + sqrtD) / (2 * a);
            ts[count++] = (-b - sqrtD) / (2 * a);
        }

        private static Point EvaluateCubic(Point p0, Point p1, Point p2, Point p3, double t)
        {
            double u = 1 - t;

            double x =
                u * u * u * p0.X +
                3 * u * u * t * p1.X +
                3 * u * t * t * p2.X +
                t * t * t * p3.X;

            double y =
                u * u * u * p0.Y +
                3 * u * u * t * p1.Y +
                3 * u * t * t * p2.Y +
                t * t * t * p3.Y;

            return new Point(x, y);
        }
    }
}
