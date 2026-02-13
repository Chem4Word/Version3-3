// ---------------------------------------------------------------------------
//  Copyright (c) 2026, The .NET Foundation.
//  This software is released under the Apache Licence, Version 2.0.
//  The licence and further copyright text can be found in the file LICENCE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System;
using System.Linq;
using System.Windows;

namespace Chem4Word.Model2.Geometry
{
    public static class Geometry<T>
    {
        /// <summary>
        /// gets the centroid of an array of points
        /// </summary>
        /// <param name="poly">Polygon represented as array of objects, sorted in anticlockwise order</param>
        /// <param name="getPosition">Lambda to return position of T</param>
        /// <returns>Point as geocenter</returns>
        public static Point? GetCentroid(T[] poly, Func<T, Point> getPosition)
        {
            double accumulatedArea = 0.0f;
            double centerX = 0.0f;
            double centerY = 0.0f;
            if (poly.Any())
            {
                for (int i = 0, j = poly.Length - 1; i < poly.Length; j = i++)
                {
                    double temp = getPosition(poly[i]).X * getPosition(poly[j]).Y
                                  - getPosition(poly[j]).X * getPosition(poly[i]).Y;
                    accumulatedArea += temp;
                    centerX += (getPosition(poly[i]).X + getPosition(poly[j]).X) * temp;
                    centerY += (getPosition(poly[i]).Y + getPosition(poly[j]).Y) * temp;
                }

                if (Math.Abs(accumulatedArea) < 1E-7f)
                {
                    return null; // Avoid division by zero
                }

                accumulatedArea *= 3f;
                return new Point(centerX / accumulatedArea, centerY / accumulatedArea);
            }

            return null;
        }
    }
}
