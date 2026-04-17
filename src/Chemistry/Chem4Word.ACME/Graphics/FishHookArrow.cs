// ---------------------------------------------------------------------------
//  Copyright (c) 2026, The .NET Foundation.
//  This software is released under the Apache Licence, Version 2.0.
//  The licence and further copyright text can be found in the file LICENCE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.ACME.Utils;
using Chem4Word.Core;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

namespace Chem4Word.ACME.Graphics
{
    public class FishHookArrow : BezierArrow
    {
        public Vector BarbOffset(Point startPoint, Point endPoint)
        {
            var shaftLength = WPFGeometry.GetPathFigureLength(Shaft());
            var shaftVector = endPoint - startPoint;
            var whl = GetWorkingHeadLength(shaftLength);
            shaftVector.Normalize();
            var barbOffset = whl * Math.Abs(Math.Sin(HeadAngle)) * shaftVector;
            return barbOffset;
        }

        public override PathFigure ArrowHeadGeometry(PathFigure line, bool reverse = false)
        {
            var shaftLength = WPFGeometry.GetPathFigureLength(line);
            var headLength = GetWorkingHeadLength(shaftLength);

            Point[] ends = DetermineBarbEnds(EndPoint, SecondControlPoint, headLength, barbOffset: headLength * Math.Sin(HeadAngle));

            // Work out which point is the furthest away from the start point use that
            Vector vector0 = StartPoint - ends[0];
            Vector vector1 = StartPoint - ends[1];
            Point barbPoint = vector0.Length > vector1.Length
                ? ends[0]
                : ends[1];
            LineSegment barb = new LineSegment(barbPoint, true);
            PathSegmentCollection psc = new PathSegmentCollection { barb };

            PathFigure pathfig = new PathFigure(EndPoint, psc, false);

            return pathfig;
        }

        private Point[] DetermineBarbEnds(Point endPoint, Point controlPoint,
                                              double headLength = 8.0, double barbOffset = 4.0)
        {
            List<Point> points = new List<Point>();

            // Tangent direction at the end (end - control)
            Vector tangent = endPoint - controlPoint;

            if (tangent.Length < CoreConstants.Epsilon)
            {
                return points.ToArray();
            }

            tangent.Normalize(); // unit direction

            // Normal (perpendicular) vectors
            Vector leftNormal = new Vector(-tangent.Y, tangent.X);
            Vector rightNormal = new Vector(tangent.Y, -tangent.X);

            // Base of the arrow head
            Point basePoint = endPoint - (tangent * headLength);

            // Left barb
            points.Add(basePoint + (leftNormal * barbOffset));

            // Right barb
            points.Add(basePoint + (rightNormal * barbOffset));

            return points.ToArray();
        }
    }
}
