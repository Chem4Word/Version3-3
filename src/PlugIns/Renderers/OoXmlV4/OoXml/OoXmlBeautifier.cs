// ---------------------------------------------------------------------------
//  Copyright (c) 2026, The .NET Foundation.
//  This software is released under the Apache Licence, Version 2.0.
//  The licence and further copyright text can be found in the file LICENCE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.Core;
using Chem4Word.Core.Helpers;
using Chem4Word.Core.UI.Forms;
using Chem4Word.Model2;
using Chem4Word.Model2.Enums;
using Chem4Word.Renderer.OoXmlV4.Entities;
using Chem4Word.Renderer.OoXmlV4.Enums;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Windows;

namespace Chem4Word.Renderer.OoXmlV4.OoXml
{
    public class OoXmlBeautifier
    {
        private static string _class = MethodBase.GetCurrentMethod()?.DeclaringType?.Name;
        private static string _product = Assembly.GetExecutingAssembly().FullName.Split(',')[0];

        private List<BondLine> _thickBondLines;

        private List<BondLine> _wedgeOrHatchBondLines;

        public OoXmlBeautifier(RendererInputs inputs, RendererOutputs outputs)
        {
            Inputs = inputs;
            Outputs = outputs;
        }

        private RendererInputs Inputs { get; }
        private RendererOutputs Outputs { get; }

        public void Beautify()
        {
            string module = $"{MethodBase.GetCurrentMethod().Name}()";

            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            _wedgeOrHatchBondLines = Outputs.BondLines.Where(t => t.Style == BondLineStyle.Wedge || t.Style == BondLineStyle.Hatch).ToList();
            _thickBondLines = Outputs.BondLines.Where(t => t.Style == BondLineStyle.Thick).ToList();

            // Generate initial outline points for first pass (clipping around characters)
            GenerateOutlinePoints(_wedgeOrHatchBondLines);
            GenerateOutlinePoints(_thickBondLines);

            // 5. Shrink Bond Lines so that they don't overlap any atom characters
            if (Inputs.Options.ClipBondLines)
            {
                ShrinkBondLinesToExcludeOwnAtomCharacters(Inputs.Progress);
                ShrinkBondLinesThatCrossOtherAtomCharacters(Inputs.Progress);
            }

            BeautifyDoubleBonds();

            if (Inputs.Options.ClipCrossingBonds)
            {
                // Experimental: Add mask underneath long bond lines of bonds detected as having crossing points
                DetectCrossingLines();
                // Make it look like we are clipping the overlapping bonds
                AddMaskBehindCrossedBonds();
            }

            BeautifyStereoBondLines();

            Inputs.Telemetry.Write(module, "Timing", $"Beautify took {SafeDouble.AsString0(stopwatch.ElapsedMilliseconds)}ms");
        }

        private static bool IsLonger(Vector v1, Vector v2)
            => v1.Length > v2.Length;

        private void AddMaskBehindCrossedBonds()
        {
            // Add mask underneath long bond lines of bonds detected as having crossing points
            foreach (CrossedBonds crossedBonds in Inputs.Model.CrossedBonds.Values)
            {
                // Find all lines for this bond
                List<BondLine> lines = Outputs.BondLines.Where(b => b.BondPath.Equals(crossedBonds.LongBond.Path)).ToList();
                foreach (BondLine line in lines)
                {
                    // Create two copies for use later on
                    BondLine replacement = line.Copy();
                    BondLine mask = line.Copy();

                    // Remove the line so we can add two more so that layering is correct
                    Outputs.BondLines.Remove(line);

                    // Set up mask which goes behind the replacement
                    mask.SetLineStyle(BondLineStyle.Solid);
                    // Change this from OoXmlColours.White to OoXmlColours.Yellow to see mask
                    mask.Colour = OoXmlColours.Yellow;
                    mask.Width = OoXmlConstants.AcsLineWidth * 4;
                    double shrinkBy = (mask.Start - mask.End).Length * OoXmlConstants.MultipleBondOffsetPercentage / 1.5;
                    mask.Shrink(-shrinkBy);

                    // Add mask
                    Outputs.BondLines.Add(mask);
                    // Add replacement so that it's on top of mask
                    Outputs.BondLines.Add(replacement);
                }
            }
        }

        private void BeautifyDoubleBondLines(Atom atom, string bondPath)
        {
            if (atom.Element is Element element
                && element == ModelGlobals.PeriodicTable.C
                && atom.Bonds.ToList().Count == 3)
            {
                bool isInRing = atom.IsInRing;
                List<BondLine> lines = Outputs.BondLines.Where(bl => bl.BondPath.Equals(bondPath)).ToList();
                if (lines.Any())
                {
                    List<Bond> otherLines;
                    if (isInRing)
                    {
                        otherLines = atom.Bonds.Where(b => !b.Path.Equals(bondPath)).ToList();
                    }
                    else
                    {
                        otherLines = atom.Bonds.Where(b => !b.Path.Equals(bondPath) && b.Order.Equals(ModelConstants.OrderSingle)).ToList();
                    }

                    if (lines.Count == 2 && otherLines.Count == 2)
                    {
                        BondLine line1 = Outputs.BondLines.FirstOrDefault(bl => bl.BondPath.Equals(otherLines[0].Path));
                        BondLine line2 = Outputs.BondLines.FirstOrDefault(bl => bl.BondPath.Equals(otherLines[1].Path));
                        if (line1 != null && line2 != null)
                        {
                            TrimBondLines(lines, line1, line2, isInRing);
                        }
                        else
                        {
                            // Hopefully never hit this
                            Debugger.Break();
                        }
                    }
                }
            }
        }

        /// <summary>
        ///Rendering molecular sketches for publication quality output
        /// Alex M Clark (AMC)
        /// Implement beautification of semi open double bonds and double bonds touching rings
        /// </summary>
        private void BeautifyDoubleBonds()
        {
            Progress pb = Inputs.Progress;

            int moleculeNo = 0;

            foreach (Molecule molecule in Inputs.Model.Molecules.Values)
            {
                // Obtain list of Double Bonds with Placement of BondDirection.None
                List<Bond> doubleBonds = molecule.Bonds.Where(b => b.OrderValue.HasValue
                                                                   && b.OrderValue.Value == 2
                                                                   && b.Placement == BondDirection.None).ToList();
                if (doubleBonds.Count > 0)
                {
                    pb.Message = $"Processing Double Bonds in Molecule {moleculeNo}";
                    pb.Value = 0;
                    pb.Maximum = doubleBonds.Count;

                    foreach (Bond doubleBond in doubleBonds)
                    {
                        BeautifyDoubleBondLines(doubleBond.StartAtom, doubleBond.Path);
                        BeautifyDoubleBondLines(doubleBond.EndAtom, doubleBond.Path);
                    }
                }
            }
        }

        private void BeautifyStereoBondLines()
        {
            // Re Generate initial outline points for second pass (beautifying joins with other bonds)
            GenerateOutlinePoints(_wedgeOrHatchBondLines);
            GenerateOutlinePoints(_thickBondLines);

            BeautifyWedgeOrHatchBondLines(_wedgeOrHatchBondLines);
            BeautifyThickBondLines(_thickBondLines);
        }

        private void BeautifyThickBondLines(List<BondLine> thickBondLines)
        {
            foreach (BondLine thickBondLine in thickBondLines)
            {
                List<BondLine> wedgeBondLines = Outputs.BondLines
                                                       .Where(l => (l.Style == BondLineStyle.Wedge || l.Style == BondLineStyle.Hatch)
                                                                   && (l.Tail == thickBondLine.Start || l.Tail == thickBondLine.End))
                                                       .ToList();

                wedgeBondLines.Remove(thickBondLine);

                if (wedgeBondLines.Any())
                {
                    ClippingLine innerClip = new ClippingLine(thickBondLine.InnerStart, thickBondLine.InnerEnd, ClippingLineType.ExtendBoth);
                    ClippingLine outerClip = new ClippingLine(thickBondLine.OuterStart, thickBondLine.OuterEnd, ClippingLineType.ExtendBoth);

                    foreach (BondLine wedgeBondLine in wedgeBondLines)
                    {
                        ClippingLine leftClip = new ClippingLine(wedgeBondLine.Start, wedgeBondLine.LeftTail, ClippingLineType.ExtendEnd);
                        ClippingLine rightClip = new ClippingLine(wedgeBondLine.Start, wedgeBondLine.RightTail, ClippingLineType.ExtendEnd);

                        UpdateTailIfIntersectingAndLonger(wedgeBondLine, leftClip, outerClip, isLeft: true);
                        UpdateTailIfIntersectingAndLonger(wedgeBondLine, rightClip, innerClip, isLeft: false);
                        UpdateTailIfIntersectingAndLonger(wedgeBondLine, rightClip, outerClip, isLeft: false);
                        UpdateTailIfIntersectingAndLonger(wedgeBondLine, leftClip, innerClip, isLeft: true);

                        HandleThickIntersections(thickBondLine, leftClip, rightClip, innerClip, outerClip);
                    }
                }
            }
        }

        private void BeautifyWedgeOrHatchBondLines(List<BondLine> wedgeOrHatchBondLines)
        {
            foreach (BondLine thisBondLine in wedgeOrHatchBondLines)
            {
                List<BondLine> otherBondLines = Outputs.BondLines
                                                       .Where(l => l.Style != BondLineStyle.Thick
                                                                   && (l.Start == thisBondLine.Tail || l.End == thisBondLine.Tail))
                                                       .ToList();

                otherBondLines.Remove(thisBondLine);

                if (otherBondLines.Any())
                {
                    ClippingLine thisLeftClip = new ClippingLine(thisBondLine.Start, thisBondLine.LeftTail, ClippingLineType.ExtendEnd);
                    ClippingLine thisRightClip = new ClippingLine(thisBondLine.Start, thisBondLine.RightTail, ClippingLineType.ExtendEnd);

                    foreach (BondLine otherBondLine in otherBondLines)
                    {
                        ClippingLine otherClip = otherBondLine.Start == thisBondLine.Tail
                            ? new ClippingLine(otherBondLine.End, otherBondLine.Start, ClippingLineType.ExtendEnd)
                            : new ClippingLine(otherBondLine.Start, otherBondLine.End, ClippingLineType.ExtendEnd);

                        if (otherBondLines.Count == 1)
                        {
                            UpdateTailIfIntersecting(thisBondLine, thisLeftClip, otherClip, isLeft: true);
                            UpdateTailIfIntersecting(thisBondLine, thisRightClip, otherClip, isLeft: false);
                        }
                        else
                        {
                            UpdateTailIfIntersectingAndLonger(thisBondLine, thisLeftClip, otherClip, isLeft: true);
                            UpdateTailIfIntersectingAndLonger(thisBondLine, thisRightClip, otherClip, isLeft: false);
                        }

                        if ((otherBondLine.Style == BondLineStyle.Wedge || otherBondLine.Style == BondLineStyle.Hatch)
                            && otherBondLine.Tail == thisBondLine.Tail)
                        {
                            UpdatedTailPointsOfSharedWedges(thisBondLine, otherBondLine, thisLeftClip, thisRightClip);
                        }
                    }
                }
            }
        }

        private void DetectCrossingLines()
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            Model model = Inputs.Model;

            Stopwatch sw = new Stopwatch();
            sw.Start();

            model.DetectCrossingLines();
            foreach (KeyValuePair<string, CrossedBonds> crossedBond in model.CrossedBonds)
            {
                Outputs.CrossingPoints.Add(crossedBond.Value.CrossingPoint);
            }
            sw.Stop();
            Inputs.Telemetry.Write(module, "Timing", $"Detection of {model.CrossedBonds.Count} line crossing points took {SafeDouble.AsString0(sw.ElapsedMilliseconds)} ms");
        }

        private void GenerateOutlinePoints(List<BondLine> bondLines)
        {
            foreach (BondLine bondLine in bondLines)
            {
                switch (bondLine.Style)
                {
                    case BondLineStyle.Thick:
                        bondLine.CalculateOutlinePoints(Inputs.MeanBondLength * CoreConstants.ThickToDoubleScaleFactor);
                        break;

                    case BondLineStyle.Wedge:
                        List<BondLine> touching = GetTouchingBondLines(bondLine);
                        IEnumerable<BondLine> thick = touching.Where(t => t.Style == BondLineStyle.Thick);
                        if (thick.Any())
                        {
                            bondLine.CalculateOutlinePoints(Inputs.MeanBondLength * CoreConstants.ThickToDoubleScaleFactor);
                        }
                        else
                        {
                            bondLine.CalculateOutlinePoints(Inputs.MeanBondLength);
                        }
                        break;

                    default:
                        bondLine.CalculateOutlinePoints(Inputs.MeanBondLength);
                        break;
                }
            }
        }

        private List<BondLine> GetTouchingBondLines(BondLine thisBondLine)
        {
            List<BondLine> otherBondLines = Outputs.BondLines
                                                   .Where(l => l.Start == thisBondLine.End
                                                               || l.End == thisBondLine.Start
                                                               || l.End == thisBondLine.End
                                                               || l.Start == thisBondLine.Start)
                                                   .ToList();
            otherBondLines.Remove(thisBondLine);

            return otherBondLines;
        }

        private void HandleIntersection(BondLine bondLine, ClippingLine line1, ClippingLine line2,
                                               string startPropName, string endPropName,
                                               Dictionary<string, Point> originalPoints)
        {
            Point? intersection = GeometryTool.GetIntersection(line1.Start, line1.End, line2.Start, line2.End);
            if (!intersection.HasValue)
            {
                return;
            }

            Point originalStart = originalPoints[startPropName];
            Point originalEnd = originalPoints[endPropName];

            if (IsLonger(originalStart - intersection.Value, originalStart - originalEnd))
            {
                SetBondLinePoint(bondLine, endPropName, intersection.Value);
            }
            if (IsLonger(originalEnd - intersection.Value, originalEnd - originalStart))
            {
                SetBondLinePoint(bondLine, startPropName, intersection.Value);
            }
        }

        private void HandleThickIntersections(BondLine thickBondLine,
                              ClippingLine leftWedgeClippingLine, ClippingLine rightWedgeClippingLine,
                              ClippingLine innerClippingLine, ClippingLine outerClippingLine)
        {
            Dictionary<string, Point> originalPoints = new Dictionary<string, Point>
                                                       {
                                                           { nameof(thickBondLine.InnerStart), thickBondLine.GetOriginalPoint(nameof(thickBondLine.InnerStart)) },
                                                           { nameof(thickBondLine.InnerEnd), thickBondLine.GetOriginalPoint(nameof(thickBondLine.InnerEnd)) },
                                                           { nameof(thickBondLine.OuterStart), thickBondLine.GetOriginalPoint(nameof(thickBondLine.OuterStart)) },
                                                           { nameof(thickBondLine.OuterEnd), thickBondLine.GetOriginalPoint(nameof(thickBondLine.OuterEnd)) }
                                                       };

            HandleIntersection(thickBondLine, leftWedgeClippingLine, outerClippingLine, nameof(thickBondLine.OuterStart), nameof(thickBondLine.OuterEnd), originalPoints);
            HandleIntersection(thickBondLine, rightWedgeClippingLine, innerClippingLine, nameof(thickBondLine.InnerStart), nameof(thickBondLine.InnerEnd), originalPoints);
            HandleIntersection(thickBondLine, leftWedgeClippingLine, innerClippingLine, nameof(thickBondLine.InnerStart), nameof(thickBondLine.InnerEnd), originalPoints);
            HandleIntersection(thickBondLine, rightWedgeClippingLine, outerClippingLine, nameof(thickBondLine.OuterStart), nameof(thickBondLine.OuterEnd), originalPoints);
        }

        private void SetBondLinePoint(BondLine bondLine, string propertyName, Point newPoint)
        {
            switch (propertyName)
            {
                case nameof(bondLine.InnerStart):
                    bondLine.InnerStart = newPoint;
                    break;

                case nameof(bondLine.InnerEnd):
                    bondLine.InnerEnd = newPoint;
                    break;

                case nameof(bondLine.OuterStart):
                    bondLine.OuterStart = newPoint;
                    break;

                case nameof(bondLine.OuterEnd):
                    bondLine.OuterEnd = newPoint;
                    break;
            }
        }

        private void ShrinkBondLine(Point start, Point end, KeyValuePair<string, List<Point>> hull, BondLine bondLine, int index)
        {
            Point[] result = GeometryTool.ClipLineWithPolygon(start, end, hull.Value, out bool lineStartsOutsidePolygon);

            Point newStart;
            Point newEnd;

            switch (result.Length)
            {
                case 3:
                    if (lineStartsOutsidePolygon)
                    {
                        newStart = new Point(result[0].X, result[0].Y);
                        newEnd = new Point(result[1].X, result[1].Y);

                        switch (index)
                        {
                            case 0:
                                bondLine.Start = newStart;
                                bondLine.End = newEnd;
                                break;

                            case 1:
                                SimpleLine simple1 = new SimpleLine(newStart, newEnd).GetParallel(-bondLine.Offset);
                                bondLine.Start = simple1.Start;
                                bondLine.End = simple1.End;
                                break;

                            case 2:
                                SimpleLine simple2 = new SimpleLine(newStart, newEnd).GetParallel(bondLine.Offset);
                                bondLine.Start = simple2.Start;
                                bondLine.End = simple2.End;
                                break;
                        }
                    }
                    else
                    {
                        newStart = new Point(result[1].X, result[1].Y);
                        newEnd = new Point(result[2].X, result[2].Y);

                        switch (index)
                        {
                            case 0:
                                bondLine.Start = newStart;
                                bondLine.End = newEnd;
                                break;

                            case 1:
                                SimpleLine simple1 = new SimpleLine(newStart, newEnd).GetParallel(-bondLine.Offset);
                                bondLine.Start = simple1.Start;
                                bondLine.End = simple1.End;
                                break;

                            case 2:
                                SimpleLine simple2 = new SimpleLine(newStart, newEnd).GetParallel(bondLine.Offset);
                                bondLine.Start = simple2.Start;
                                bondLine.End = simple2.End;
                                break;
                        }
                    }

                    bondLine.CalculateOutlinePoints(Inputs.MeanBondLength);

                    break;

                case 2:
                    if (!lineStartsOutsidePolygon)
                    {
                        // This line is totally inside so remove it!
                        Outputs.BondLines.Remove(bondLine);
                    }
                    break;

                default:
                    Debugger.Break();
                    break;
            }
        }

        // Shrink bond lines so that they do not overlap this atom's label characters
        private void ShrinkBondLinesToExcludeOwnAtomCharacters(Progress pb)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod()?.Name}()";

            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            if (Outputs.ConvexHulls.Count > 1)
            {
                pb.Show();
            }
            pb.Message = "Clipping Bond Lines - Pass 1";
            pb.Value = 0;
            pb.Maximum = Outputs.ConvexHulls.Count;

            foreach (KeyValuePair<string, List<Point>> hull in Outputs.ConvexHulls)
            {
                pb.Increment(1);

                // select lines which start or end with this atom
                List<BondLine> bondLines = (from line in Outputs.BondLines
                                            where line.StartAtomPath == hull.Key || line.EndAtomPath == hull.Key
                                            select line)
                    .ToList();

                foreach (BondLine bondLine in bondLines)
                {
                    Point start;
                    Point end;

                    if (bondLine.Style == BondLineStyle.Thick)
                    {
                        for (int index = 0; index < 3; index++)
                        {
                            switch (index)
                            {
                                case 0:
                                    start = new Point(bondLine.Start.X, bondLine.Start.Y);
                                    end = new Point(bondLine.End.X, bondLine.End.Y);
                                    ShrinkBondLine(start, end, hull, bondLine, index);
                                    break;

                                case 1:
                                    start = new Point(bondLine.InnerStart.X, bondLine.InnerStart.Y);
                                    end = new Point(bondLine.InnerEnd.X, bondLine.InnerEnd.Y);
                                    ShrinkBondLine(start, end, hull, bondLine, index);
                                    break;

                                case 2:
                                    start = new Point(bondLine.OuterStart.X, bondLine.OuterStart.Y);
                                    end = new Point(bondLine.OuterEnd.X, bondLine.OuterEnd.Y);
                                    ShrinkBondLine(start, end, hull, bondLine, index);
                                    break;
                            }
                        }
                    }
                    else
                    {
                        start = new Point(bondLine.Start.X, bondLine.Start.Y);
                        end = new Point(bondLine.End.X, bondLine.End.Y);

                        ShrinkBondLine(start, end, hull, bondLine, 0);
                    }
                }
            }

            stopwatch.Stop();
            Inputs.Telemetry.Write(module, "Timing", $"Clipping Bond Lines [{Inputs.Options.HullMode}] - Pass 1 took {SafeDouble.AsString0(stopwatch.ElapsedMilliseconds)} ms");
        }

        // Shrink bond lines so that they do not overlap any atom's label characters
        private void ShrinkBondLinesThatCrossOtherAtomCharacters(Progress pb)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod()?.Name}()";

            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            List<string> atomPaths = Outputs.AtomLabelCharacters
                                            .Where(a => a.AtomPath.StartsWith("/m"))
                                            .Select(a => a.AtomPath)
                                            .Distinct()
                                            .ToList();

            if (atomPaths.Count > 1)
            {
                pb.Show();
            }
            pb.Message = "Clipping Bond Lines - Pass 2";
            pb.Value = 0;
            pb.Maximum = atomPaths.Count;

            foreach (string path in atomPaths)
            {
                pb.Increment(1);

                List<Point> clippingHull = Outputs.ConvexHulls[path];

                Rect boundingBox = GeometryTool.GetBoundingBox(clippingHull);

                // Select Lines which may require trimming
                // By using LINQ to implement the following SQL
                // Where (L.Right Between Cbb.Left And Cbb.Right)
                //    Or (L.Left Between Cbb.Left And Cbb.Right)
                //    Or (L.Top Between Cbb.Top And Cbb.Botton)
                //    Or (L.Bottom Between Cbb.Top And Cbb.Botton)

                List<BondLine> bondLines = (from line in Outputs.BondLines
                                            where (boundingBox.Left <= line.BoundingBox.Right && line.BoundingBox.Right <= boundingBox.Right)
                                                  || (boundingBox.Left <= line.BoundingBox.Left && line.BoundingBox.Left <= boundingBox.Right)
                                                  || (boundingBox.Top <= line.BoundingBox.Top && line.BoundingBox.Top <= boundingBox.Bottom)
                                                  || (boundingBox.Top <= line.BoundingBox.Bottom && line.BoundingBox.Bottom <= boundingBox.Bottom)
                                            select line).ToList();

                foreach (BondLine bondLine in bondLines)
                {
                    if (!(bondLine.Bond.Stereo == BondStereo.Wedge
                          || bondLine.Bond.Stereo == BondStereo.Hatch
                          || bondLine.Bond.Stereo == BondStereo.Thick))
                    {
                        bool intersects = !GeometryTool.Intersects(bondLine.Start, bondLine.End, clippingHull);
                        if (intersects)
                        {
                            Point[] points = GeometryTool.ClipLineWithPolygon(bondLine.Start, bondLine.End, clippingHull, out bool _);

                            // We only need to consider when the hull of an atom's symbol has cut through a bond line as shown below
                            //
                            // -----[H]------
                            //
                            if (points.Length == 4)
                            {
                                // 1. Generate new line
                                BondLine extraLine = new BondLine(bondLine.Style, points[0], points[1], bondLine.Bond);
                                Outputs.BondLines.Add(extraLine);

                                // 2. Trim existing line
                                bondLine.Start = points[2];
                                bondLine.End = points[3];
                            }
                        }
                    }
                }
            }

            stopwatch.Stop();
            Inputs.Telemetry.Write(module, "Timing", $"Clipping Bond Lines [{Inputs.Options.HullMode}] - Pass 2 took {SafeDouble.AsString0(stopwatch.ElapsedMilliseconds)} ms");
        }

        private bool TrimBondLine(BondLine leftOrRight, BondLine line, bool isInRing)
        {
            // Make a longer version of the line
            Point startLonger = new Point(leftOrRight.Start.X, leftOrRight.Start.Y);
            Point endLonger = new Point(leftOrRight.End.X, leftOrRight.End.Y);
            GeometryTool.AdjustLineAboutMidpoint(ref startLonger, ref endLonger, Inputs.MeanBondLength / 5);

            // See if they intersect at one end
            Point? crossingPoint = GeometryTool.GetIntersection(startLonger, endLonger, line.Start, line.End);

            // If they intersect update the main line
            bool crosses = crossingPoint != null;
            if (crosses)
            {
                double l1 = GeometryTool.DistanceBetween(crossingPoint.Value, leftOrRight.Start);
                double l2 = GeometryTool.DistanceBetween(crossingPoint.Value, leftOrRight.End);
                if (l1 > l2)
                {
                    leftOrRight.End = new Point(crossingPoint.Value.X, crossingPoint.Value.Y);
                }
                else
                {
                    leftOrRight.Start = new Point(crossingPoint.Value.X, crossingPoint.Value.Y);
                }

                if (!isInRing)
                {
                    l1 = GeometryTool.DistanceBetween(crossingPoint.Value, line.Start);
                    l2 = GeometryTool.DistanceBetween(crossingPoint.Value, line.End);
                    if (l1 > l2)
                    {
                        line.End = new Point(crossingPoint.Value.X, crossingPoint.Value.Y);
                    }
                    else
                    {
                        line.Start = new Point(crossingPoint.Value.X, crossingPoint.Value.Y);
                    }
                }
            }

            return crosses;
        }

        private void TrimBondLines(List<BondLine> mainPair, BondLine line1, BondLine line2, bool isInRing)
        {
            // Only two of these calls are expected to do anything
            if (!TrimBondLine(mainPair[0], line1, isInRing))
            {
                TrimBondLine(mainPair[0], line2, isInRing);
            }
            // Only two of these calls are expected to do anything
            if (!TrimBondLine(mainPair[1], line1, isInRing))
            {
                TrimBondLine(mainPair[1], line2, isInRing);
            }
        }

        private void UpdatedTailPointsOfSharedWedges(BondLine thisBondLine, BondLine otherBondLine, ClippingLine thisLeftClip, ClippingLine thisRightClip)
        {
            Point otherLeftTail = otherBondLine.GetOriginalPoint(nameof(otherBondLine.LeftTail));
            Point otherRightTail = otherBondLine.GetOriginalPoint(nameof(otherBondLine.RightTail));

            ClippingLine otherLeftClip = new ClippingLine(otherBondLine.Nose, otherLeftTail, ClippingLineType.ExtendEnd);
            ClippingLine otherRightClip = new ClippingLine(otherBondLine.Nose, otherRightTail, ClippingLineType.ExtendEnd);

            UpdateTailIfIntersectingAndLonger(otherBondLine, otherLeftClip, thisLeftClip, isLeft: true);
            UpdateTailIfIntersectingAndLonger(otherBondLine, otherRightClip, thisRightClip, isLeft: false);
            UpdateTailIfIntersectingAndLonger(thisBondLine, thisLeftClip, otherRightClip, isLeft: true);
            UpdateTailIfIntersectingAndLonger(thisBondLine, thisRightClip, otherLeftClip, isLeft: false);
        }

        private void UpdateTailIfIntersecting(BondLine bondLine, ClippingLine bondClip, ClippingLine otherClip, bool isLeft)
        {
            Point? intersection = GeometryTool.GetIntersection(bondClip.Start, bondClip.End, otherClip.Start, otherClip.End);
            if (intersection.HasValue)
            {
                if (isLeft)
                {
                    bondLine.LeftTail = intersection.Value;
                }
                else
                {
                    bondLine.RightTail = intersection.Value;
                }
            }
        }

        private void UpdateTailIfIntersectingAndLonger(BondLine bondLine, ClippingLine bondClip, ClippingLine otherClip, bool isLeft)
        {
            Point? intersection = GeometryTool.GetIntersection(bondClip.Start, bondClip.End, otherClip.Start, otherClip.End);
            if (intersection.HasValue)
            {
                Point currentTail = isLeft ? bondLine.LeftTail : bondLine.RightTail;
                if (IsLonger(bondLine.Start - intersection.Value, bondLine.Start - currentTail))
                {
                    if (isLeft)
                    {
                        bondLine.LeftTail = intersection.Value;
                    }
                    else
                    {
                        bondLine.RightTail = intersection.Value;
                    }
                }
            }
        }
    }
}
