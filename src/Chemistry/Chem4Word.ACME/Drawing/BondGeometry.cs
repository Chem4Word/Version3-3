// ---------------------------------------------------------------------------
//  Copyright (c) 2026, The .NET Foundation.
//  This software is released under the Apache Licence, Version 2.0.
//  The licence and further copyright text can be found in the file LICENCE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.ACME.Drawing.LayoutSupport;
using Chem4Word.Core;
using Chem4Word.Core.Helpers;
using Chem4Word.Model2;
using Chem4Word.Model2.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace Chem4Word.ACME.Drawing
{
    /// <summary>
    ///     Static class to handle bond geometries
    /// </summary>
    public static class BondGeometry
    {
        /// <summary>
        ///  Returns the geometry of a wedge bond.  Hatch bonds use the same geometry
        ///     but a different brush.
        /// </summary>
        /// <param name="desc">Layout defining the bond shape</param>
        /// <param name="perp">perpendicular vector to the bond</param>
        public static void GetWedgePoints(WedgeBondLayout desc, Vector perp)
        {
            desc.FirstCorner = desc.End + perp;

            desc.SecondCorner = desc.End - perp;

            desc.Boundary.AddRange(new[] { desc.Start, desc.FirstCorner, desc.SecondCorner });
        }

        /// <summary>
        /// Gets the geometry of a wedge bond.
        /// </summary>
        /// <param name="desc">WedgeBondLayout which is populated</param>
        /// <param name="standardBondLength">Standard bond length as defined by the model</param>
        /// <param name="standoff">Boundary width between atom label and bond terminus</param>
        public static void GetWedgeBondGeometry(WedgeBondLayout desc, double standardBondLength, double standoff)
        {
            //get the width of the wedge bond's thick end
            var bondVector = desc.PrincipleVector;
            var perpVector = bondVector.Perpendicular();
            perpVector.Normalize();
            perpVector *= standardBondLength * ModelConstants.BondOffsetPercentage;

            // shrink the bond so it doesn't overlap any AtomVisuals
            AdjustTerminus(ref desc.Start, desc.End, desc.StartAtomHull, standoff);
            AdjustTerminus(ref desc.End, desc.Start, desc.EndAtomHull, standoff);

            //then draw it
            GetWedgePoints(desc, perpVector);
            //and pass it back as a Geometry
            StreamGeometry sg;
            sg = desc.GetOutline();
            sg.Freeze();
            desc.DefiningGeometry = sg;
        }

        /// <summary>
        ///     Defines the three parallel lines of a Triple bond.
        /// </summary>
        /// <param name="standardBondLength">Standard bond length as defined by the model</param>
        /// <param name="standoff">Boundary width between atom label and bond terminus</param>
        /// <param name="layout.StartAtomHull">AtomVisual defining the starting atom</param>
        /// <param name="layout.EndAtomHull">AtomVisual defining the end atom</param>
        /// <returns></returns>
        public static void GetTripleBondGeometry(TripleBondLayout layout, double standardBondLength,
                                                 double standoff)
        {
            //start by getting the six points that define a standard triple bond
            GetTripleBondPoints(layout, standardBondLength, standoff);
            //and draw it
            var sg = new StreamGeometry();
            using (var sgc = sg.Open())
            {
                sgc.BeginFigure(layout.Start, false, false);
                sgc.LineTo(layout.Start, true, false);
                sgc.BeginFigure(layout.SecondaryStart, false, false);
                sgc.LineTo(layout.SecondaryEnd, true, false);
                sgc.BeginFigure(layout.TertiaryStart, false, false);
                sgc.LineTo(layout.TertiaryEnd, true, false);
                sgc.Close();
            }

            sg.Freeze();
            layout.DefiningGeometry = sg;
        }

        /// <summary>
        /// 'Draws' the triple bond
        /// </summary>StartAtomVisual
        /// <param name="layout">TripleBondLayout which is populated</param>
        /// <param name="standardBondLength">Standard bond length as defined by the model</param>
        /// <param name="standoff">Boundary width between atom label and bond terminus</param>
        public static void GetTripleBondPoints(TripleBondLayout layout, double standardBondLength, double standoff)
        {
            //get a standard perpendicular vector
            var v = layout.PrincipleVector;
            var normal = v.Perpendicular();
            normal.Normalize();

            //offset the secondaries
            var distance = standardBondLength * ModelConstants.BondOffsetPercentage;
            layout.SecondaryStart = layout.Start + normal * distance;
            layout.SecondaryEnd = layout.SecondaryStart + v;

            layout.TertiaryStart = layout.Start - normal * distance;
            layout.TertiaryEnd = layout.TertiaryStart + v;
            //adjust the line ends
            if (layout.StartAtomHull != null)
            {
                AdjustTerminus(ref layout.Start, layout.End, layout.StartAtomHull, standoff);
                AdjustTerminus(ref layout.SecondaryStart, layout.SecondaryEnd, layout.StartAtomHull,
                               standoff);
                AdjustTerminus(ref layout.TertiaryStart, layout.TertiaryEnd, layout.StartAtomHull,
                               standoff);
            }

            if (layout.EndAtomHull != null)
            {
                AdjustTerminus(ref layout.End, layout.Start, layout.EndAtomHull, standoff);
                AdjustTerminus(ref layout.SecondaryEnd, layout.SecondaryStart, layout.EndAtomHull,
                               standoff);
                AdjustTerminus(ref layout.TertiaryEnd, layout.TertiaryStart, layout.EndAtomHull, standoff);
            }

            //and define the boundary for hit testing
            layout.Boundary.Clear();
            layout.Boundary.AddRange(new[]
                                         {
                                             layout.SecondaryStart, layout.SecondaryEnd,
                                             layout.TertiaryEnd, layout.TertiaryStart
                                         });
        }

        /// <summary>
        ///     draws the two parallel lines of a double bond
        ///     These bonds can either straddle the atom-atom line or fall to one or other side of it
        /// </summary>
        /// <param name="layout">DoubleBondLayout which is populated</param>
        /// <param name="standardBondLength">Standard bond length as defined by the model</param>
        /// <param name="standoff"></param>
        /// <returns></returns>
        public static void GetDoubleBondGeometry(DoubleBondLayout layout, double standardBondLength,
                                                 double standoff)

        {
            //get the standard points for a double bond
            GetAdjustedDoubleBondPoints(layout, standardBondLength, standoff);
            //and draw it
            var sg = new StreamGeometry();
            using (var sgc = sg.Open())
            {
                sgc.BeginFigure(layout.Start, false, false);
                sgc.LineTo(layout.End, true, false);
                sgc.BeginFigure(layout.SecondaryStart, false, false);
                sgc.LineTo(layout.SecondaryEnd, true, false);
                sgc.Close();
            }

            sg.Freeze();
            layout.DefiningGeometry = sg;
        }

        private static void GetAdjustedDoubleBondPoints(DoubleBondLayout layout, double standardBondLength,
                                                        double standoff)
        {
            GetDoubleBondPoints(layout, standardBondLength);
            //adjust the line ends
            if (layout.StartAtomHull != null)
            {
                AdjustTerminus(ref layout.Start, layout.End, layout.StartAtomHull, standoff);
                AdjustTerminus(ref layout.SecondaryStart, layout.SecondaryEnd, layout.StartAtomHull,
                               standoff);
            }

            if (layout.EndAtomHull != null)
            {
                AdjustTerminus(ref layout.End, layout.Start, layout.EndAtomHull, standoff);
                AdjustTerminus(ref layout.SecondaryEnd, layout.SecondaryStart, layout.EndAtomHull,
                               standoff);
            }
        }

        /// <summary>
        ///     Defines a double bond
        /// </summary>
        /// <param name="layout">DoubleBondLayout which is populated</param>
        /// <param name="standardBondLength">Standard bond length as defined by the model</param>
        /// <returns></returns>
        public static void GetDoubleBondPoints(DoubleBondLayout layout, double standardBondLength)
        {
            Point? layoutSecondaryStart;
            Point? layoutSecondaryEnd;

            //use a struct here to return the values
            GetDefaultDoubleBondPoints(layout, standardBondLength);

            if (layout.PrimaryCentroid != null)
            {
                //now, if there is a centroid defined, the bond is part of a ring
                Point? workingCentroid = null;
                //work out whether the bond is placed inside or outside the ring
                var principleVector = layout.PrincipleVector;
                var centreVector = layout.PrimaryCentroid - layout.Start;
                var computedPlacement = BondDirection.None;

                var crossProduct = Vector.CrossProduct(centreVector.Value, principleVector);
                if (!double.IsNaN(crossProduct))
                {
                    computedPlacement = (BondDirection)Math.Sign(crossProduct);
                }

                if (layout.Placement != BondDirection.None)
                {
                    if (computedPlacement == layout.Placement) //then we have nothing to worry about
                    {
                        workingCentroid = layout.PrimaryCentroid;
                    }
                    else //we need to adjust the points according to the other centroid
                    {
                        workingCentroid = layout.SecondaryCentroid;
                    }
                }

                if (workingCentroid != null)
                {
                    var bondVector = (layout.End - layout.Start);
                    var midPoint = bondVector / 2 + layout.Start;

                    var angle = Math.Abs(Vector.AngleBetween(workingCentroid.Value - midPoint, bondVector));

                    if (angle >= 80 && angle <= 100) //probably convex ring
                    {
                        //shorten the second bond to fit neatly within the ring
                        layoutSecondaryStart = GeometryTool.GetIntersection(layout.Start, workingCentroid.Value,
                            layout.SecondaryStart,
                            layout.SecondaryEnd);
                        layoutSecondaryEnd = GeometryTool.GetIntersection(layout.End, workingCentroid.Value,
                                                                              layout.SecondaryStart,
                                                                              layout.SecondaryEnd);
                        var tempPoint3 = layoutSecondaryStart ?? layout.SecondaryStart;
                        var tempPoint4 = layoutSecondaryEnd ?? layout.SecondaryEnd;

                        layout.SecondaryStart = tempPoint3;
                        layout.SecondaryEnd = tempPoint4;
                    }
                    else //probably concave ring, so shorten by half the bond offset value
                    {
                        if (layout.StartNeigbourPositions != null && layout.EndNeighbourPositions != null)
                        {
                            SplitBondAngles(ref layout.SecondaryStart, layout.SecondaryEnd,
                                            layout.StartNeigbourPositions, layout.Start);
                            SplitBondAngles(ref layout.SecondaryEnd, layout.SecondaryStart,
                                            layout.EndNeighbourPositions, layout.End);
                        }
                    }
                }
                else //no centroid so split the angles anyway
                {
                    if (layout.StartNeigbourPositions != null && layout.EndNeighbourPositions != null)
                    {
                        SplitBondAngles(ref layout.SecondaryStart, layout.SecondaryEnd,
                                        layout.StartNeigbourPositions, layout.Start);
                        SplitBondAngles(ref layout.SecondaryEnd, layout.SecondaryStart,
                                        layout.EndNeighbourPositions, layout.End);
                    }
                }

                //get the boundary for hit testing purposes
                layout.Boundary.Clear();
                layout.Boundary.AddRange(new[]
                                             {
                                                 layout.Start, layout.End, layout.SecondaryEnd,
                                                 layout.SecondaryStart
                                             });
            }
        }

        /// <summary>
        ///Splits the angle between a secondary bond and any neighbouring bonds.
        ///This is used in special cases where we can't get a meaningful centroid
        /// </summary>
        /// <param name="secondaryStart">Start of the double bond's secondary: to be clipped</param>
        /// <param name="secondaryEnd">End of the double bond's secondary</param>
        /// <param name="atomPosList">Positions of atoms attached to the primary bond's atom</param>
        /// <param name="primaryAtomPos">Position of the primary bond's atom</param>
        private static void SplitBondAngles(ref Point secondaryStart, Point secondaryEnd, List<Point> atomPosList,
                                            Point primaryAtomPos)
        {
            foreach (Point neighbourPos in atomPosList)
            {
                Point? intersection =
                    GeometryTool.GetIntersection(secondaryStart, secondaryEnd, primaryAtomPos, neighbourPos);
                if (intersection != null)
                {
                    //need to shorten the line again
                    Vector splitVector = neighbourPos - primaryAtomPos;
                    Vector secondaryVector = secondaryEnd - secondaryStart;
                    double splitAngle = Vector.AngleBetween(secondaryVector, splitVector) / 2;
                    Matrix rotator = new Matrix();
                    rotator.Rotate(-splitAngle);
                    splitVector = splitVector * rotator;
                    Point? temp = GeometryTool.GetIntersection(secondaryStart, secondaryEnd, primaryAtomPos,
                                                               primaryAtomPos + splitVector);
                    if (temp != null)
                    {
                        secondaryStart = temp.Value;
                    }

                    break;
                }
            }
        }

        /// <summary>
        /// Gets an unadjusted set of points for a double bond
        /// </summary>
        /// <param name="layout">DoubleBondLayout which is populated</param>
        /// <param name="standardBondLength">Standard bond length as defined by the model</param>
        private static void GetDefaultDoubleBondPoints(DoubleBondLayout layout, double standardBondLength)
        {
            var v = layout.PrincipleVector;
            var normal = v.Perpendicular();
            normal.Normalize();

            var distance = standardBondLength * ModelConstants.BondOffsetPercentage;
            //first, calculate the default bond points as if there were no rings involved
            var tempStart = layout.Start;
            //offset according to placement
            switch (layout.Placement)
            {
                //case BondDirection.None is covered by default

                case BondDirection.Clockwise:
                    layout.SecondaryStart = tempStart - normal * 2 * distance;
                    layout.SecondaryEnd = layout.SecondaryStart + v;
                    break;

                case BondDirection.Anticlockwise:
                    layout.SecondaryStart = tempStart + normal * 2 * distance;
                    layout.SecondaryEnd = layout.SecondaryStart + v;
                    break;

                default:
                    layout.Start = tempStart + normal * distance;
                    layout.End = layout.Start + v;

                    layout.SecondaryStart = tempStart - normal * distance;
                    layout.SecondaryEnd = layout.SecondaryStart + v;
                    break;
            }
        }

        /// <summary>
        /// Draws the crossed double bond to indicate indeterminate geometry
        /// </summary>
        /// <param name="layout">BondLayout to hadle the physical bond shape</param>
        /// <param name="standardBondLength">Model's standard bond length</param>
        /// <param name="standoff">Boundary width between atom label and bond terminus</param>
        public static void GetCrossedDoubleGeometry(DoubleBondLayout layout, double standardBondLength,
                                                    double standoff)
        {
            var v = layout.PrincipleVector;
            var normal = v.Perpendicular();
            normal.Normalize();

            Point point1, point2, point3, point4;

            var distance = standardBondLength * ModelConstants.BondOffsetPercentage;

            point1 = layout.Start + normal * distance;
            point2 = point1 + v;

            point3 = layout.Start - normal * distance;
            point4 = point3 + v;

            if (layout.StartAtomHull != null)
            {
                AdjustTerminus(ref point1, point2, layout.StartAtomHull, standoff);
                AdjustTerminus(ref point3, point4, layout.StartAtomHull, standoff);
            }

            if (layout.StartAtomHull != null)
            {
                AdjustTerminus(ref point2, point1, layout.StartAtomHull, standoff);
                AdjustTerminus(ref point4, point3, layout.StartAtomHull, standoff);
            }

            var sg = new StreamGeometry();
            using (var sgc = sg.Open())
            {
                sgc.BeginFigure(point1, false, false);
                sgc.LineTo(point4, true, false);
                sgc.BeginFigure(point2, false, false);
                sgc.LineTo(point3, true, false);
                sgc.Close();
            }

            sg.Freeze();
            layout.DefiningGeometry = sg;
            layout.Boundary.Clear();
            layout.Boundary.AddRange(new[] { point1, point2, point4, point3 });
        }

        public static void GetSingleBondGeometry(BondLayout layout, double standoff)
        {
            var start = layout.Start;
            var end = layout.End;

            var sg = new StreamGeometry();

            if (layout.StartAtomHull != null)
            {
                AdjustTerminus(ref start, end, layout.StartAtomHull, standoff);
            }

            if (layout.EndAtomHull != null)
            {
                AdjustTerminus(ref end, start, layout.EndAtomHull, standoff);
            }

            using (var sgc = sg.Open())
            {
                sgc.BeginFigure(start, false, false);
                sgc.LineTo(end, true, false);
                sgc.Close();
            }

            sg.Freeze();
            layout.DefiningGeometry = sg;
        }

        private static Point? GetIntersection(Point start, Point end, List<Point> atomHull)
        {
            for (int i = 0; i < atomHull.Count; i++)
            {
                Point? p;
                if ((p = GeometryTool.GetIntersection(start, end, atomHull[i], atomHull[(i + 1) % atomHull.Count])) !=
                    null)
                {
                    return p;
                }
            }

            return null;
        }

        /// <summary>
        /// Adjusts the StartPoint of a bond to avoid the atom visual
        /// </summary>
        /// <param name="startPoint">Moveable start point</param>
        /// <param name="endPoint">Fixed end point</param>
        /// <param name="atomHull">List of Points delineating the hull of the atom label</param>
        /// <param name="standoff">Boundary width between atom label and bond terminus</param>
        private static void AdjustTerminus(ref Point startPoint, Point endPoint, List<Point> atomHull, double standoff)
        {
            if (atomHull != null
                && startPoint != endPoint)
            {
                var displacement = endPoint - startPoint;

                var intersection = GetIntersection(startPoint, endPoint, atomHull);
                if (intersection != null)
                {
                    displacement.Normalize();
                    displacement = displacement * standoff;
                    var tempPoint = new Point(intersection.Value.X, intersection.Value.Y) + displacement;
                    startPoint = new Point(tempPoint.X, tempPoint.Y);
                }
            }
        }

        /// <summary>
        /// Quite ghastly routine to draw a wiggly bond
        /// </summary>
        /// <param name="layout">BondLayout which is populated</param>
        /// <param name="standardBondLength">Standard bond length as defined by the model</param>
        /// <param name="standoff"></param>
        public static void GetWavyBondGeometry(BondLayout layout, double standardBondLength, double standoff)
        {
            var sg = new StreamGeometry();

            //first do the adjustment for any atom visuals
            if (layout.StartAtomHull != null)
            {
                AdjustTerminus(ref layout.Start, layout.End, layout.StartAtomHull, standoff);
            }

            if (layout.EndAtomHull != null)
            {
                AdjustTerminus(ref layout.End, layout.Start, layout.EndAtomHull, standoff);
            }

            //Work out the control points for a quadratic Bezier by sprouting alternately along the bond line
            Vector halfAWiggle;
            using (var sgc = sg.Open())
            {
                var bondVector = layout.PrincipleVector;
                //come up with a number of wiggles that looks aesthetically sensible
                var noOfWiggles =
                    (int)Math.Ceiling(bondVector.Length / (standardBondLength * ModelConstants.BondOffsetPercentage * 2));
                if (noOfWiggles < 3)
                {
                    noOfWiggles = 3;
                }

                //now calculate a wiggle vector that is 60 degrees from the bond angle
                var wiggleLength = bondVector.Length / noOfWiggles;

                halfAWiggle = bondVector;
                halfAWiggle.Normalize();
                halfAWiggle *= wiggleLength / 2;

                //work out left and right sprouting vectors
                var toLeft = new Matrix();
                toLeft.Rotate(-60);
                var toRight = new Matrix();
                toRight.Rotate(60);

                var leftVector = halfAWiggle * toLeft;
                var rightVector = halfAWiggle * toRight;

                var allpoints = new List<Point>();
                //allpoints holds the control points for the bezier
                allpoints.Add(layout.Start);

                var lastPoint = layout.Start;
                //move along the bond vector, sprouting control points alternately
                for (var i = 0; i < noOfWiggles; i++)
                {
                    var leftPoint = lastPoint + leftVector;
                    allpoints.Add(leftPoint);
                    allpoints.Add(lastPoint + halfAWiggle);
                    var rightPoint = lastPoint + halfAWiggle + rightVector;
                    allpoints.Add(rightPoint);
                    lastPoint += halfAWiggle * 2;
                    allpoints.Add(lastPoint);
                }

                allpoints.Add(layout.End);
                BezierFromPoints(sgc, allpoints);
                sgc.Close();
            }

            //define the boundary
            layout.Boundary.Clear();
            layout.Boundary.AddRange(new[]
                                         {
                                             layout.Start - halfAWiggle.Perpendicular(),
                                             layout.End - halfAWiggle.Perpendicular(),
                                             layout.End + halfAWiggle.Perpendicular(),
                                             layout.Start + halfAWiggle.Perpendicular()
                                         });

            sg.Freeze();
            layout.DefiningGeometry = sg;

            //local function
            void BezierFromPoints(StreamGeometryContext sgc, List<Point> allpoints)
            {
                sgc.BeginFigure(allpoints[0], false, false);
                sgc.PolyQuadraticBezierTo(allpoints.Skip(1).ToArray(), true, true);
            }
        }

        /// <summary>
        /// Chamfers or forks the end of a wedge bond under special circumstances
        /// (one or more incoming single bonds)
        /// </summary>
        /// <param name="layout"> WedgeBondLayout to be populated</param>
        /// <param name="standardBondLength">Standard bond length as defined by the model</param>
        /// <param name="otherAtomPoints">List of positions of atoms splaying from the end atom</param>
        /// <param name="standoff"></param>
        public static void GetChamferedWedgeGeometry(WedgeBondLayout layout,
                                                     double standardBondLength,
                                                     List<Point> otherAtomPoints, double standoff)
        {
            var bondVector = layout.PrincipleVector;

            //first get an unaltered bond
            GetWedgeBondGeometry(layout, standardBondLength, standoff);

            var firstEdgeVector = layout.FirstCorner - layout.Start;
            var secondEdgeVector = layout.SecondCorner - layout.Start;

            //get the two bonds with widest splay

            var widestPoints = from Point p in otherAtomPoints
                               orderby Math.Abs(Vector.AngleBetween(bondVector, p - layout.End)) descending
                               select p;

            //the scaling factors are what we multiply the bond edge vectors by
            double firstScalingFactor = 0d;
            double secondScalingFactor = 0d;

            //work out the biggest scaling factor for either long edge
            foreach (var point in widestPoints)
            {
                GeometryTool.IntersectLines(layout.Start,
                                            layout.FirstCorner,
                                            layout.End,
                                            point, out var firstEdgeCut, out var otherBond1Cut);
                GeometryTool.IntersectLines(layout.Start,
                                            layout.SecondCorner,
                                            layout.End,
                                            point, out var secondEdgeCut, out var otherBond2Cut);
                if (otherAtomPoints.Count == 1)
                {
                    if (firstEdgeCut > firstScalingFactor)
                    {
                        firstScalingFactor = firstEdgeCut;
                    }

                    if (secondEdgeCut > secondScalingFactor)
                    {
                        secondScalingFactor = secondEdgeCut;
                    }
                }
                else
                {
                    if (firstEdgeCut > firstScalingFactor && otherBond1Cut < 1d && otherBond1Cut > 0d)
                    {
                        firstScalingFactor = firstEdgeCut;
                    }

                    if (secondEdgeCut > secondScalingFactor && otherBond2Cut < 1d && otherBond2Cut > 0d)
                    {
                        secondScalingFactor = secondEdgeCut;
                    }
                }
            }

            //and multiply the edges by the scaling factors
            layout.FirstCorner = firstEdgeVector * firstScalingFactor + layout.Start;
            layout.SecondCorner = secondEdgeVector * secondScalingFactor + layout.Start;

            layout.Outlined = true;

            var sg = layout.GetOutline();
            sg.Freeze();
            layout.DefiningGeometry = sg;
        }

        /// <summary>
        /// Will modify both the layouts for the wedge and thick bonds
        /// Can be called twice in succession for a thick bond terminated
        /// at either end by wedges.
        /// </summary>
        /// <param name="wbl">An already built WedgeBondLayout object</param>
        /// <param name="tbl">An already built ThickBondLayout object</param>
        /// <param name="standardBondLength"></param>
        public static void GetWedgeToThickGeometry(WedgeBondLayout wbl, ThickBondLayout tbl,
                                                     double standardBondLength)
        {
            //first we work out the thick bond vector pointing towards the wedge bond
            //determine whether the bonds beet end to end or end to start
            bool endToEnd = (wbl.End - tbl.End).LengthSquared < (wbl.End - tbl.Start).LengthSquared;
            //if the bonds meet end to start then reverse the vector
            Vector tbVector;
            if (endToEnd)
            {
                tbVector = tbl.End - tbl.Start;
            }
            else
            {
                tbVector = tbl.Start - tbl.End;
            }

            //if the bonds are joined in a straight line then just exit
            //otherwise weirdness starts
            var angle = Math.Abs(Vector.AngleBetween(wbl.PrincipleVector, tbVector));
            if (170 <= angle && angle <= 180)
            {
                return;
            }

            //simply add both vectors together to get the split

            Vector splitVector = (wbl.PrincipleVector + tbVector) * 10;//defines the middle line between the two bonds, make it big

            //now do lots of intersections
            //intersect the sides of the thick bonds with the split lines
            if (!endToEnd)
            {
                GeometryTool.IntersectLines(tbl.ThirdCorner, tbl.FourthCorner, tbl.Start, tbl.Start + splitVector,
                                            out double t1, out double _);
                GeometryTool.IntersectLines(tbl.SecondCorner, tbl.FirstCorner, tbl.Start, tbl.Start + splitVector,
                                            out double t2, out double _);

                tbl.FourthCorner = tbl.ThirdCorner + (tbl.FourthCorner - tbl.ThirdCorner) * t1;
                tbl.FirstCorner = tbl.SecondCorner + (tbl.FirstCorner - tbl.SecondCorner) * t2;

                wbl.FirstCorner = tbl.FirstCorner;
                wbl.SecondCorner = tbl.FourthCorner;
                wbl.End = (wbl.SecondCorner - wbl.FirstCorner) * 0.5 + wbl.FirstCorner;
            }
            else
            {
                GeometryTool.IntersectLines(tbl.FirstCorner, tbl.SecondCorner, tbl.End, tbl.End + splitVector,
                                            out double t3, out double u3);
                GeometryTool.IntersectLines(tbl.FourthCorner, tbl.ThirdCorner, tbl.End, tbl.End + splitVector,
                                            out double t4, out double u1u4);

                tbl.SecondCorner = tbl.FirstCorner + (tbl.SecondCorner - tbl.FirstCorner) * t3;
                tbl.ThirdCorner = tbl.FourthCorner + (tbl.ThirdCorner - tbl.FourthCorner) * t4;

                wbl.FirstCorner = tbl.SecondCorner;
                wbl.SecondCorner = tbl.ThirdCorner;
                wbl.End = (wbl.SecondCorner - wbl.FirstCorner) * 0.5 + wbl.FirstCorner;
            }

            var sg = new StreamGeometry();

            using (StreamGeometryContext sgc = sg.Open())
            {
                sgc.BeginFigure(wbl.Start, true, true);
                sgc.LineTo(wbl.FirstCorner, true, true);
                sgc.LineTo(wbl.SecondCorner, true, true);
                sgc.Close();
            }

            sg.Freeze();
            wbl.DefiningGeometry = sg;

            var sg2 = new StreamGeometry();

            using (StreamGeometryContext sgc2 = sg2.Open())
            {
                sgc2.BeginFigure(tbl.FirstCorner, true, true);
                sgc2.LineTo(tbl.SecondCorner, true, true);
                sgc2.LineTo(tbl.ThirdCorner, true, true);
                sgc2.LineTo(tbl.FourthCorner, true, true);
                sgc2.Close();
            }

            sg2.Freeze();
            tbl.DefiningGeometry = sg2;
        }

        public static void GetWedgeToThickGeometry2(WedgeBondLayout wbl, double standardBondLength, Bond thickBond)
        {
            Vector perp = thickBond.BondVector.Perpendicular();
            perp.Normalize();
            double perpDistance = standardBondLength * ModelConstants.BondOffsetPercentage;
            Vector offset = perp * perpDistance;

            var sg = new StreamGeometry();

            using (StreamGeometryContext sgc = sg.Open())
            {
                sgc.BeginFigure(wbl.Start, true, true);
                sgc.LineTo(wbl.End + offset, true, true);
                sgc.LineTo(wbl.End - offset, true, true);
                sgc.Close();
            }

            sg.Freeze();
            wbl.DefiningGeometry = sg;
        }

        /// <summary>
        /// Joins two wedge bonds at the thick end
        /// using an aesthetically pleasing angle
        /// Assumes that the test has already been performed
        /// </summary>
        /// <param name="wbl"></param>
        /// <param name="standardBondLength"></param>
        /// <param name="otherWedge"></param>
        public static void GetWedgeToWedgeGeometry(WedgeBondLayout wbl, double standardBondLength, Bond otherWedge)
        {
            Vector otherWedgeVector = otherWedge.BondVector; //this points towards the common end atom
            Vector splitVector = wbl.PrincipleVector + otherWedgeVector;//defines the middle line between the two bonds

            //intersect the sides of the wedge bonds with the split lines
            GeometryTool.IntersectLines(wbl.Start, wbl.FirstCorner, wbl.End, wbl.End + splitVector, out double t1, out double u1);
            GeometryTool.IntersectLines(wbl.Start, wbl.SecondCorner, wbl.End, wbl.End + splitVector, out double t3, out double u3);

            //do the inner side overlaps first
            if (0 < t1 && t1 < 1.0)
            {
                wbl.FirstCorner = (wbl.FirstCorner - wbl.Start) * t1 + wbl.Start;
                //then the outer overlaps
                GeometryTool.IntersectLines(wbl.Start, wbl.SecondCorner, wbl.End, wbl.End - splitVector, out double t4, out double u4);
                wbl.SecondCorner = (wbl.SecondCorner - wbl.Start) * t4 + wbl.Start;
            }
            else if (0 < t3 && t3 < 1.0)
            {
                wbl.SecondCorner = (wbl.SecondCorner - wbl.Start) * t3 + wbl.Start;
                GeometryTool.IntersectLines(wbl.Start, wbl.FirstCorner, wbl.End, wbl.End - splitVector, out double t2, out double u2);
                wbl.FirstCorner = (wbl.FirstCorner - wbl.Start) * t2 + wbl.Start;
            }

            StreamGeometry sg = new StreamGeometry();
            using (StreamGeometryContext sgc = sg.Open())
            {
                sgc.BeginFigure(wbl.Start, true, true);
                sgc.LineTo(wbl.SecondCorner, true, true);
                sgc.LineTo(wbl.FirstCorner, true, true);
                sgc.Close();
            }

            sg.Freeze();
            wbl.DefiningGeometry = sg;
        }

        public static void GetThickBondGeometry(ThickBondLayout tbl, double standardBondLength, double standoff)
        {
            var start = tbl.Start;
            var end = tbl.End;

            if (tbl.StartAtomHull != null)
            {
                AdjustTerminus(ref start, end, tbl.StartAtomHull, standoff);
            }

            if (tbl.EndAtomHull != null)
            {
                AdjustTerminus(ref end, start, tbl.EndAtomHull, standoff);
            }

            var bl = new BondLayout()
            {
                Start = start,
                End = end,
                StartAtomHull = tbl.StartAtomHull,
                EndAtomHull = tbl.EndAtomHull,
            };

            GetSingleBondGeometry(bl, standoff);
            var v = bl.PrincipleVector;
            var normal = v.Perpendicular();
            normal.Normalize();

            double perpDistance = standardBondLength * ModelConstants.BondOffsetPercentage * CoreConstants.ThickToDoubleScaleFactor;

            tbl.Start = bl.Start;
            tbl.End = bl.End;
            tbl.FirstCorner = bl.Start + normal * perpDistance;
            tbl.SecondCorner = bl.End + normal * perpDistance;
            tbl.ThirdCorner = bl.End - normal * perpDistance;
            tbl.FourthCorner = bl.Start - normal * perpDistance;

            var sg = new StreamGeometry();
            using (StreamGeometryContext sgc = sg.Open())
            {
                sgc.BeginFigure(tbl.FirstCorner, true, true);
                sgc.LineTo(tbl.SecondCorner, true, true);
                sgc.LineTo(tbl.ThirdCorner, true, true);
                sgc.LineTo(tbl.FourthCorner, true, true);
                sgc.Close();
            }

            sg.Freeze();
            tbl.DefiningGeometry = sg;
        }
    }
}