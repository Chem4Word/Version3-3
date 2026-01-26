// ---------------------------------------------------------------------------
//  Copyright (c) 2026, The .NET Foundation.
//  This software is released under the Apache Licence, Version 2.0.
//  The licence and further copyright text can be found in the file LICENCE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.Core.Helpers;
using Chem4Word.Model2;
using Chem4Word.Model2.Enums;
using System;
using System.Collections.Generic;
using System.Windows;

namespace Chem4Word.Renderer.OoXmlV4.Entities
{
    public class OoXmlElectron
    {
        public Atom ParentAtom { get; set; }
        public double MeanBondLength { get; set; }

        public Point Position { get; set; }
        public string Colour { get; set; }

        public Electron Electron { get; set; }

        public double RadicalDiameter { get; set; }

        public List<Point> Points { get; set; }

        public void GeneratePoints(Point position, double offset)
        {
            Points = new List<Point>();

            switch (Electron.TypeOfElectron)
            {
                case ElectronType.Radical:
                    Points.Add(position);
                    break;

                case ElectronType.LonePair:
                case ElectronType.Carbenoid:

                    Vector line = ParentAtom.Position - position;

                    Vector perpendicular = line.Perpendicular();
                    perpendicular.Normalize();
                    perpendicular *= offset;

                    Point r1 = position;
                    r1 += perpendicular;
                    Points.Add(r1);

                    Point r2 = position;
                    r2 -= perpendicular;
                    Points.Add(r2);
                    break;
            }
        }

        #region Hulls

        public List<Point> Hull()
        {
            double width = Math.Ceiling(RadicalDiameter) + 1;
            if (Electron.TypeOfElectron == ElectronType.Radical)
            {
                return GeometryTool.HullOfCircle(Position, width);
            }

            List<Point> points = GeometryTool.HullOfCircle(Points[0], width);
            points.AddRange(GeometryTool.HullOfCircle(Points[1], width));
            return GeometryTool.MakeConvexHull(points);
        }

        public List<Point> CircleHull()
        {
            double width = Math.Ceiling(RadicalDiameter) + 1;
            return GeometryTool.HullOfCircle(Position, width);
        }

        public List<Point> SquareHull()
        {
            double width = Math.Ceiling(RadicalDiameter) + 1;

            Rect thisRect = new Rect(new Point(Position.X - width / 2, Position.Y - width / 2),
                                     new Size(width, width));

            List<Point> result = new List<Point>
                                 {
                                     thisRect.TopLeft,
                                     thisRect.TopRight,
                                     thisRect.BottomLeft,
                                     thisRect.BottomRight
                                 };

            result = GeometryTool.MakeConvexHull(result);

            return result;
        }

        #endregion Hulls
    }
}
