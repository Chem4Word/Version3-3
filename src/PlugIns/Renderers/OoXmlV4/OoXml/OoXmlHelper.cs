// ---------------------------------------------------------------------------
//  Copyright (c) 2026, The .NET Foundation.
//  This software is released under the Apache Licence, Version 2.0.
//  The licence and further copyright text can be found in the file LICENCE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.Core;
using Chem4Word.Core.Helpers;
using Chem4Word.Model2;
using Chem4Word.Renderer.OoXmlV4.Entities;
using Chem4Word.Renderer.OoXmlV4.TTF;
using DocumentFormat.OpenXml;
using System;
using System.Collections.Generic;
using System.Windows;

namespace Chem4Word.Renderer.OoXmlV4.OoXml
{
    public static class OoXmlHelper
    {
        public static double BracketOffset(double bondLength)
            => bondLength * OoXmlConstants.BracketOffsetPercentage;

        public static Rect GetAllCharacterExtents(Model model, RendererOutputs outputs)
        {
            Rect characterExtents = model.BoundingBoxOfCmlPoints;

            foreach (AtomLabelCharacter character in outputs.AtomLabelCharacters)
            {
                if (character.IsSmaller)
                {
                    Rect r = new Rect(character.Position,
                                      new Size(ScaleCsTtfToCml(character.Character.Width, model.MeanBondLength) * OoXmlConstants.SubscriptScaleFactor,
                                               ScaleCsTtfToCml(character.Character.Height, model.MeanBondLength) * OoXmlConstants.SubscriptScaleFactor));
                    characterExtents.Union(r);
                }
                else
                {
                    Rect r = new Rect(character.Position,
                                      new Size(ScaleCsTtfToCml(character.Character.Width, model.MeanBondLength),
                                               ScaleCsTtfToCml(character.Character.Height, model.MeanBondLength)));
                    characterExtents.Union(r);
                }
            }

            foreach (MoleculeExtents group in outputs.AllMoleculeExtents)
            {
                characterExtents.Union(group.ExternalCharacterExtents);
            }

            // Bullet proofing - Error seen in telemetry :-
            // System.InvalidOperationException: Cannot call this method on the Empty Rect.
            //   at System.Windows.Rect.Inflate(Double width, Double height)
            if (characterExtents == Rect.Empty)
            {
                characterExtents = new Rect(new Point(0, 0), new Size(OoXmlConstants.DrawingMargin * 10, OoXmlConstants.DrawingMargin * 10));
            }
            else
            {
                characterExtents.Inflate(OoXmlConstants.DrawingMargin, OoXmlConstants.DrawingMargin);
            }

            return characterExtents;
        }

        /// <summary>
        /// Scales a CML X or Y co-ordinate to DrawingML Units (EMU)
        /// </summary>
        /// <param name="XorY"></param>
        /// <returns></returns>
        public static Int64Value ScaleCmlToEmu(double XorY)
        {
            double scaled = XorY * OoXmlConstants.EmusPerCmlPoint;
            return Int64Value.FromInt64((long)scaled);
        }

        #region C# TTF

        // These calculations yield a font which has a point size of 8 at a bond length of 20
        private static double EmusPerCsTtfPoint(double bondLength)
            => bondLength / 2.5;

        /// <summary>
        /// Scales a CS TTF SubScript X or Y co-ordinate to DrawingML Units (EMU)
        /// <param name="XorY"></param>
        /// <param name="bondLength"></param>
        /// </summary>
        public static Int64Value ScaleCsTtfSubScriptToEmu(double XorY, double bondLength)
        {
            if (bondLength > 0.1)
            {
                double scaled = XorY * EmusPerCsTtfPointSubscript(bondLength);
                return Int64Value.FromInt64((long)scaled);
            }
            else
            {
                double scaled = XorY * EmusPerCsTtfPointSubscript(20);
                return Int64Value.FromInt64((long)scaled);
            }
        }

        public static List<Point> BoundingBox(List<AtomLabelCharacter> characters, double meanBondLength, double margin)
        {
            List<Point> points = new List<Point>();

            foreach (AtomLabelCharacter character in characters)
            {
                points.AddRange(BoundingBox(character, meanBondLength, margin));
            }

            return GeometryTool.MakeConvexHull(points);
        }

        /// <summary>
        /// Returns a bounding box of the character
        /// </summary>
        /// <param name="character"></param>
        /// <param name="meanBondLength"></param>
        /// <param name="margin"></param>
        /// <returns></returns>
        public static List<Point> BoundingBox(AtomLabelCharacter character, double meanBondLength, double margin)
        {
            List<Point> points = new List<Point>();

            double offsetX;
            double offsetY;

            if (character.IsSmaller)
            {
                offsetX = ScaleCsTtfSubScriptToCml(character.Character.Width, meanBondLength);
                offsetY = ScaleCsTtfSubScriptToCml(character.Character.Height, meanBondLength);
            }
            else
            {
                offsetX = ScaleCsTtfToCml(character.Character.Width, meanBondLength);
                offsetY = ScaleCsTtfToCml(character.Character.Height, meanBondLength);
            }

            // Top Left
            points.Add(new Point(character.Position.X - margin, character.Position.Y - margin));
            // Top Right
            points.Add(new Point(character.Position.X + offsetX + margin, character.Position.Y - margin));
            // Bottom Right
            points.Add(new Point(character.Position.X + offsetX + margin, character.Position.Y + offsetY + margin));
            // Bottom Left
            points.Add(new Point(character.Position.X - margin, character.Position.Y + offsetY + margin));

            return points;
        }

        /// <summary>
        /// Returns a list of points from the TTF definition of a character, this is normally the first contour defined
        /// </summary>
        /// <param name="character"></param>
        /// <param name="meanBondLength"></param>
        /// <returns></returns>
        public static List<Point> SimpleHull(AtomLabelCharacter character, double meanBondLength)
        {
            List<Point> points = new List<Point>();

            // Handle special cases where we need to include all the contours
            //  when the character has the tittle (or diacritic mark)
            bool hasTittle = character.Character.Character == 'i'
                             || character.Character.Character == 'j';

            foreach (TtfContour contour in character.Character.Contours)
            {
                foreach (TtfPoint ttfPoint in contour.Points)
                {
                    points.Add(TtfToCml(character, new Point(ttfPoint.X, ttfPoint.Y), meanBondLength));
                }

                if (!hasTittle)
                {
                    break;
                }
            }

            return points;
        }

        /// <summary>
        /// Returns a list of points from the TTF definition of each character
        /// Then tweaked to follow our coding standards
        /// </summary>
        /// <param name="characters"></param>
        /// <param name="meanBondLength"></param>
        /// <returns></returns>
        public static List<Point> SimpleHull(List<AtomLabelCharacter> characters, double meanBondLength)
        {
            List<Point> points = new List<Point>();

            foreach (AtomLabelCharacter character in characters)
            {
                points.AddRange(SimpleHull(character, meanBondLength));
            }

            return GeometryTool.MakeConvexHull(points);
        }

        /// <summary>
        /// Scales a C# TTF X or Y co-ordinate to CML Units (for a subscript character)
        /// </summary>
        /// <param name="XorY"></param>
        /// <param name="bondLength"></param>
        /// <returns></returns>
        public static double ScaleCsTtfSubScriptToCml(double XorY, double bondLength)
        {
            double scaled = XorY * OoXmlConstants.SubscriptScaleFactor;
            if (bondLength > 0.1)
            {
                return scaled / CsTtfToCml(bondLength);
            }

            return scaled / CsTtfToCml(20);
        }

        /// <summary>
        /// Scales a C# TTF X or Y co-ordinate to CML Units
        /// </summary>
        /// <param name="XorY"></param>
        /// <param name="bondLength"></param>
        /// <returns></returns>
        public static double ScaleCsTtfToCml(double XorY, double bondLength)
        {
            if (bondLength > 0.1)
            {
                return XorY / CsTtfToCml(bondLength);
            }

            return XorY / CsTtfToCml(20);
        }

        /// <summary>
        /// Scales a C# TTF X or Y co-ordinate to DrawingML Units (EMU)
        /// </summary>
        /// <param name="XorY"></param>
        /// <param name="bondLength"></param>
        /// <returns></returns>
        public static Int64Value ScaleCsTtfToEmu(double XorY, double bondLength)
        {
            if (bondLength > 0.1)
            {
                double scaled = XorY * EmusPerCsTtfPoint(bondLength);
                return Int64Value.FromInt64((long)scaled);
            }
            else
            {
                double scaled = XorY * EmusPerCsTtfPoint(20);
                return Int64Value.FromInt64((long)scaled);
            }
        }

        private static double CsTtfToCml(double bondLength)
        {
            if (bondLength > 0.1)
            {
                return OoXmlConstants.EmusPerCmlPoint / EmusPerCsTtfPoint(bondLength);
            }
            else
            {
                return OoXmlConstants.EmusPerCmlPoint / EmusPerCsTtfPoint(20);
            }
        }

        private static double EmusPerCsTtfPointSubscript(double bondLength)
        {
            if (bondLength > 0.1)
            {
                return EmusPerCsTtfPoint(bondLength) * OoXmlConstants.SubscriptScaleFactor;
            }
            else
            {
                return EmusPerCsTtfPoint(20) * OoXmlConstants.SubscriptScaleFactor;
            }
        }

        #endregion C# TTF

        #region CoPilot

        /// <summary>
        /// Returns a list of points from the TTF definition of a character
        ///
        /// * Generated by CoPilot *
        /// Then tweaked to follow our coding standards
        /// </summary>
        /// <param name="character"></param>
        /// <param name="meanBondLength"></param>
        /// <returns></returns>
        public static List<Point> ComplexHull(AtomLabelCharacter character, double meanBondLength)
        {
            List<Point> points = new List<Point>();

            // Handle special cases where we need to include all the contours
            //  when the character has the tittle (or diacritic mark)
            bool hasTittle = character.Character.Character == 'i'
                             || character.Character.Character == 'j';

            foreach (TtfContour contour in character.Character.Contours)
            {
                List<(TtfPoint P0, TtfPoint C, TtfPoint P1)> segments = GetQuadraticSegments(contour.Points);

                foreach ((TtfPoint p0, TtfPoint c, TtfPoint p1) in segments)
                {
                    // If the quadratic segment is actually a straight line, don't oversample.
                    if (IsStraightSegment(p0, c, p1))
                    {
                        // Convert endpoints once and add.
                        Point a = TtfToCml(character, new Point(p0.X, p0.Y), meanBondLength);
                        Point b = TtfToCml(character, new Point(p1.X, p1.Y), meanBondLength);
                        AddIfNotDuplicate(points, a); // optional de-dup to keep list lean
                        AddIfNotDuplicate(points, b);
                    }
                    else
                    {
                        // True curve → sample
                        List<Point> samples = SampleQuadratic(p0, c, p1, character, meanBondLength, 6);
                        points.AddRange(samples);
                    }
                }

                if (!hasTittle)
                {
                    break;
                }
            }

            return points;
        }

        /// <summary>
        /// Returns a list of points from the TTF definition of each character
        ///
        /// * Generated by CoPilot *
        /// Then tweaked to follow our coding standards
        /// </summary>
        /// <param name="characters"></param>
        /// <param name="meanBondLength"></param>
        /// <returns></returns>
        public static List<Point> ComplexHull(List<AtomLabelCharacter> characters, double meanBondLength)
        {
            List<Point> points = new List<Point>();

            foreach (AtomLabelCharacter character in characters)
            {
                points.AddRange(ComplexHull(character, meanBondLength));
            }

            return GeometryTool.MakeConvexHull(points);
        }

        private static List<(TtfPoint P0, TtfPoint C, TtfPoint P1)> GetQuadraticSegments(List<TtfPoint> ttfPoints)
        {
            List<(TtfPoint, TtfPoint, TtfPoint)> segments = new List<(TtfPoint, TtfPoint, TtfPoint)>();
            int n = ttfPoints.Count;

            // First build the on-curve sequence with implied ttfPoints
            List<TtfPoint> outline = new List<TtfPoint>();
            for (int i = 0; i < n; i++)
            {
                TtfPoint p0 = GetTtfPoint(i);
                TtfPoint p1 = GetTtfPoint(i + 1);

                bool p0On = p0.Type == TtfPoint.PointType.Start ||
                            p0.Type == TtfPoint.PointType.Line ||
                            p0.Type == TtfPoint.PointType.CurveOn;

                bool p1On = p1.Type == TtfPoint.PointType.Start ||
                            p1.Type == TtfPoint.PointType.Line ||
                            p1.Type == TtfPoint.PointType.CurveOn;

                if (p0On)
                {
                    outline.Add(p0);
                }
                else
                {
                    if (p1On)
                    {
                        outline.Add(p1);
                    }
                    else
                    {
                        outline.Add(new TtfPoint
                        {
                            X = (p0.X + p1.X) / 2,
                            Y = (p0.Y + p1.Y) / 2,
                            Type = TtfPoint.PointType.CurveOn
                        });
                    }
                }
            }

            // Now emit quadratic segments: (on, off, on) pattern implied
            for (int i = 0; i < outline.Count; i++)
            {
                TtfPoint p0 = outline[i];
                TtfPoint p1 = outline[(i + 1) % outline.Count];

                // Find the matching off-curve control point in original TTF
                // by scanning backwards from p1
                TtfPoint off = null;
                for (int j = 1; j <= 2; j++)
                {
                    TtfPoint raw = GetTtfPoint(i + j);
                    if (raw.Type == TtfPoint.PointType.CurveOff)
                    {
                        off = raw;
                        break;
                    }
                }

                if (off == null)
                {
                    // Straight-line fallback (no off-curve). Using p0 is fine
                    // IsStraightSegment will treat this as a line.
                    off = p0;
                }

                segments.Add((p0, off, p1));
            }

            return segments;

            // Local Function
            TtfPoint GetTtfPoint(int i)
            {
                return ttfPoints[(i + n) % n];
            }
        }

        private static List<Point> SampleQuadratic(TtfPoint p0, TtfPoint c, TtfPoint p1, AtomLabelCharacter character, double meanBondLength, int samples)
        {
            List<Point> list = new List<Point>(samples + 1);

            for (int i = 0; i <= samples; i++)
            {
                double t = (double)i / samples;
                double mt = 1 - t;

                double x = mt * mt * p0.X +
                           2 * mt * t * c.X +
                           t * t * p1.X;

                double y = mt * mt * p0.Y +
                           2 * mt * t * c.Y +
                           t * t * p1.Y;

                list.Add(TtfToCml(character, new Point(x, y), meanBondLength));
            }

            return list;
        }

        private static Point TtfToCml(AtomLabelCharacter character, Point point, double meanBondLength)
        {
            double originX = character.Character.OriginX;
            double originY = character.Character.OriginY;
            double height = character.Character.Height;
            double posX = character.Position.X;
            double posY = character.Position.Y;

            if (character.IsSmaller)
            {
                double cx = posX + ScaleCsTtfSubScriptToCml(point.X - originX, meanBondLength);
                double cy = posY + ScaleCsTtfSubScriptToCml(height + point.Y - (height + originY), meanBondLength);
                return new Point(cx, cy);
            }
            else
            {
                double cx = posX + ScaleCsTtfToCml(point.X - originX, meanBondLength);
                double cy = posY + ScaleCsTtfToCml(height + point.Y - (height + originY), meanBondLength);
                return new Point(cx, cy);
            }
        }

        /// <summary>
        /// Adds point if it's not virtually identical to the last appended point.
        /// Keeps the working set small; final hull is unaffected either way.
        /// </summary>
        private static void AddIfNotDuplicate(List<Point> points, Point p)
        {
            if (points.Count == 0)
            {
                points.Add(p);
                return;
            }

            Point last = points[points.Count - 1];
            if (Math.Abs(last.X - p.X) > CoreConstants.Epsilon || Math.Abs(last.Y - p.Y) > CoreConstants.Epsilon)
            {
                points.Add(p);
            }
        }

        /// <summary>
        /// Returns true if the quadratic Bezier (p0, c, p1) lies on a straight line.
        /// This uses the perpendicular distance of 'c' to the line through p0->p1.
        /// </summary>
        private static bool IsStraightSegment(TtfPoint p0, TtfPoint c, TtfPoint p1)
        {
            double vx = p1.X - p0.X;
            double vy = p1.Y - p0.Y;

            // Degenerate (zero-length segment) -> treat as straight
            double len = Math.Sqrt(vx * vx + vy * vy);
            if (len < CoreConstants.Epsilon)
            {
                return true;
            }

            // Perpendicular distance from C to line P0-P1: |(C-P0) x V| / |V|
            double cx = c.X - p0.X;
            double cy = c.Y - p0.Y;
            double cross = Math.Abs(cx * vy - cy * vx);
            double distance = cross / len;

            return distance <= CoreConstants.Epsilon;
        }

        #endregion CoPilot
    }
}
