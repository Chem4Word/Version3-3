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
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Windows;

namespace Chem4Word.Renderer.OoXmlV4.OoXml
{
    public class OoXmlBeautifier
    {
        private static string _class = MethodBase.GetCurrentMethod().DeclaringType?.Name;
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
            _wedgeOrHatchBondLines = Outputs.BondLines.Where(t => t.Style == BondLineStyle.Wedge || t.Style == BondLineStyle.Hatch).ToList();
            _thickBondLines = Outputs.BondLines.Where(t => t.Style == BondLineStyle.Thick).ToList();

            // Generate initial outline points for first pass (clipping around characters)
            GenerateOutlinePoints(_wedgeOrHatchBondLines);
            GenerateOutlinePoints(_thickBondLines);

            // 5. Shrink Bond Lines so that they don't overlap any atom characters
            if (Inputs.Options.ClipBondLines)
            {
                // ToDo: [MAW] Handle Thick Bonds
                ShrinkBondLinesToExcludeAtomCharacters(Inputs.Progress);
                ShrinkBondLinesThatCrossAtomCharacters(Inputs.Progress);
            }

            BeautifyDoubleBonds();

            if (Inputs.Options.ClipCrossingBonds)
            {
                DetectCrossingLines();
            }

            // Make it look like we are clipping the overlapping bonds
            AddMaskBehindCrossedBonds();

            BeautifyStereoBondLines();
        }

        private static bool IsLonger(Vector v1, Vector v2)
            => v1.Length > v2.Length;

        private void AddMaskBehindCrossedBonds()
        {
            // Add mask underneath long bond lines of bonds detected as having crossing points
            foreach (var crossedBonds in Inputs.Model.CrossedBonds.Values)
            {
                // Find all lines for this bond
                var lines = Outputs.BondLines.Where(b => b.BondPath.Equals(crossedBonds.LongBond.Path)).ToList();
                foreach (var line in lines)
                {
                    // Create two copies for use later on
                    var replacement = line.Copy();
                    var mask = line.Copy();

                    // Remove the line so we can add two more so that layering is correct
                    Outputs.BondLines.Remove(line);

                    // Set up mask which goes behind the replacement
                    mask.SetLineStyle(BondLineStyle.Solid);
                    // Change this to OoXmlColours.Yellow to see mask
                    mask.Colour = OoXmlColours.White;
                    mask.Width = OoXmlConstants.AcsLineWidth * 8;
                    var shrinkBy = (mask.Start - mask.End).Length * OoXmlConstants.MultipleBondOffsetPercentage / 1.5;
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
                var isInRing = atom.IsInRing;
                var lines = Outputs.BondLines.Where(bl => bl.BondPath.Equals(bondPath)).ToList();
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
                        var line1 = Outputs.BondLines.FirstOrDefault(bl => bl.BondPath.Equals(otherLines[0].Path));
                        var line2 = Outputs.BondLines.FirstOrDefault(bl => bl.BondPath.Equals(otherLines[1].Path));
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
            var pb = Inputs.Progress;

            var moleculeNo = 0;

            foreach (var molecule in Inputs.Model.Molecules.Values)
            {
                // Obtain list of Double Bonds with Placement of BondDirection.None
                var doubleBonds = molecule.Bonds.Where(b => b.OrderValue.HasValue
                                                            && b.OrderValue.Value == 2
                                                            && b.Placement == BondDirection.None).ToList();
                if (doubleBonds.Count > 0)
                {
                    pb.Message = $"Processing Double Bonds in Molecule {moleculeNo}";
                    pb.Value = 0;
                    pb.Maximum = doubleBonds.Count;

                    foreach (var doubleBond in doubleBonds)
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
            foreach (var thickBondLine in thickBondLines)
            {
                var wedgeBondLines = Outputs.BondLines
                    .Where(l => (l.Style == BondLineStyle.Wedge || l.Style == BondLineStyle.Hatch)
                                && (l.Tail == thickBondLine.Start || l.Tail == thickBondLine.End))
                    .ToList();

                wedgeBondLines.Remove(thickBondLine);

                if (wedgeBondLines.Any())
                {
                    var innerClip = new ClippingLine(thickBondLine.InnerStart, thickBondLine.InnerEnd, ClippingLineType.ExtendBoth);
                    var outerClip = new ClippingLine(thickBondLine.OuterStart, thickBondLine.OuterEnd, ClippingLineType.ExtendBoth);

                    foreach (var wedgeBondLine in wedgeBondLines)
                    {
                        var leftClip = new ClippingLine(wedgeBondLine.Start, wedgeBondLine.LeftTail, ClippingLineType.ExtendEnd);
                        var rightClip = new ClippingLine(wedgeBondLine.Start, wedgeBondLine.RightTail, ClippingLineType.ExtendEnd);

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
            foreach (var thisBondLine in wedgeOrHatchBondLines)
            {
                var otherBondLines = Outputs.BondLines
                    .Where(l => l.Style != BondLineStyle.Thick
                                && (l.Start == thisBondLine.Tail || l.End == thisBondLine.Tail))
                    .ToList();

                otherBondLines.Remove(thisBondLine);

                if (otherBondLines.Any())
                {
                    var thisLeftClip = new ClippingLine(thisBondLine.Start, thisBondLine.LeftTail, ClippingLineType.ExtendEnd);
                    var thisRightClip = new ClippingLine(thisBondLine.Start, thisBondLine.RightTail, ClippingLineType.ExtendEnd);

                    foreach (var otherBondLine in otherBondLines)
                    {
                        var otherClip = otherBondLine.Start == thisBondLine.Tail
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
            var module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            var model = Inputs.Model;

            var sw = new Stopwatch();
            sw.Start();

            model.DetectCrossingLines();
            foreach (var crossedBond in model.CrossedBonds)
            {
                Outputs.CrossingPoints.Add(crossedBond.Value.CrossingPoint);
            }
            sw.Stop();
            Inputs.Telemetry.Write(module, "Timing", $"Detection of {model.CrossedBonds.Count} line crossing points took {SafeDouble.AsString0(sw.ElapsedMilliseconds)} ms");
        }

        private void GenerateOutlinePoints(List<BondLine> bondLines)
        {
            foreach (var bondLine in bondLines)
            {
                switch (bondLine.Style)
                {
                    case BondLineStyle.Thick:
                        bondLine.CalculateOutlinePoints(Inputs.MeanBondLength * CoreConstants.ThickToDoubleScaleFactor);
                        break;

                    case BondLineStyle.Wedge:
                        var touching = GetTouchingBondLines(bondLine);
                        var thick = touching.Where(t => t.Style == BondLineStyle.Thick);
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
            var otherBondLines = Outputs.BondLines
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
            var intersection = GeometryTool.GetIntersection(line1.Start, line1.End, line2.Start, line2.End);
            if (!intersection.HasValue) return;

            var originalStart = originalPoints[startPropName];
            var originalEnd = originalPoints[endPropName];

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
            var originalPoints = new Dictionary<string, Point>
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
            var result = GeometryTool.ClipLineWithPolygon(start, end, hull.Value, out var lineStartsOutsidePolygon);

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
                                var simple1 = new SimpleLine(newStart, newEnd).GetParallel(-bondLine.Offset);
                                bondLine.Start = simple1.Start;
                                bondLine.End = simple1.End;
                                break;

                            case 2:
                                var simple2 = new SimpleLine(newStart, newEnd).GetParallel(bondLine.Offset);
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
                                var simple1 = new SimpleLine(newStart, newEnd).GetParallel(-bondLine.Offset);
                                bondLine.Start = simple1.Start;
                                bondLine.End = simple1.End;
                                break;

                            case 2:
                                var simple2 = new SimpleLine(newStart, newEnd).GetParallel(bondLine.Offset);
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
            }
        }

        private void ShrinkBondLinesThatCrossAtomCharacters(Progress pb)
        {
            // so that they do not overlap label characters at their ends

            if (Outputs.AtomLabelCharacters.Count > 1)
            {
                pb.Show();
            }
            pb.Message = "Clipping Bond Lines - Pass 2";
            pb.Value = 0;
            pb.Maximum = Outputs.AtomLabelCharacters.Count;

            foreach (var alc in Outputs.AtomLabelCharacters)
            {
                pb.Increment(1);

                var width = OoXmlHelper.ScaleCsTtfToCml(alc.Character.Width, Inputs.MeanBondLength);
                var height = OoXmlHelper.ScaleCsTtfToCml(alc.Character.Height, Inputs.MeanBondLength);

                if (alc.IsSmaller)
                {
                    // Shrink bounding box
                    width *= OoXmlConstants.SubscriptScaleFactor;
                    height *= OoXmlConstants.SubscriptScaleFactor;
                }

                // Create rectangle of the bounding box with a suitable clipping margin
                var cbb = new Rect(alc.Position.X - OoXmlConstants.CmlCharacterMargin,
                                   alc.Position.Y - OoXmlConstants.CmlCharacterMargin,
                                   width + (OoXmlConstants.CmlCharacterMargin * 2),
                                   height + (OoXmlConstants.CmlCharacterMargin * 2));

                // Just in case we end up splitting a line into two
                var extraBondLines = new List<BondLine>();

                // Select Lines which may require trimming
                // By using LINQ to implement the following SQL
                // Where (L.Right Between Cbb.Left And Cbb.Right)
                //    Or (L.Left Between Cbb.Left And Cbb.Right)
                //    Or (L.Top Between Cbb.Top And Cbb.Botton)
                //    Or (L.Bottom Between Cbb.Top And Cbb.Botton)

                var bondLines = (from line in Outputs.BondLines
                                 where (cbb.Left <= line.BoundingBox.Right && line.BoundingBox.Right <= cbb.Right)
                                       || (cbb.Left <= line.BoundingBox.Left && line.BoundingBox.Left <= cbb.Right)
                                       || (cbb.Top <= line.BoundingBox.Top && line.BoundingBox.Top <= cbb.Bottom)
                                       || (cbb.Top <= line.BoundingBox.Bottom && line.BoundingBox.Bottom <= cbb.Bottom)
                                 select line).ToList();
                foreach (var bondLine in bondLines)
                {
                    if (!(bondLine.Bond.Stereo == BondStereo.Wedge
                          || bondLine.Bond.Stereo == BondStereo.Hatch
                          || bondLine.Bond.Stereo == BondStereo.Hatch))
                    {
                        var start = new Point(bondLine.Start.X, bondLine.Start.Y);
                        var end = new Point(bondLine.End.X, bondLine.End.Y);

                        var attempts = 0;
                        if (CohenSutherland.ClipLine(cbb, ref start, ref end, out attempts))
                        {
                            var bClipped = false;

                            if (Math.Abs(bondLine.Start.X - start.X) < CoreConstants.Epsilon && Math.Abs(bondLine.Start.Y - start.Y) < CoreConstants.Epsilon)
                            {
                                bondLine.Start = new Point(end.X, end.Y);
                                bClipped = true;
                            }
                            if (Math.Abs(bondLine.End.X - end.X) < CoreConstants.Epsilon && Math.Abs(bondLine.End.Y - end.Y) < CoreConstants.Epsilon)
                            {
                                bondLine.End = new Point(start.X, start.Y);
                                bClipped = true;
                            }

                            if (!bClipped && bondLine.Bond != null)
                            {
                                // Line was clipped at both ends
                                // 1. Generate new line
                                var extraLine = new BondLine(bondLine.Style, new Point(end.X, end.Y), new Point(bondLine.End.X, bondLine.End.Y), bondLine.Bond);
                                extraBondLines.Add(extraLine);
                                // 2. Trim existing line
                                bondLine.End = new Point(start.X, start.Y);
                            }
                        }
                        if (attempts >= 15)
                        {
                            Debug.WriteLine("Clipping failed !");
                        }
                    }
                }

                // Add any extra lines generated by this character into the List of Bond Lines
                foreach (var bl in extraBondLines)
                {
                    Outputs.BondLines.Add(bl);
                }
            }
        }

        private void ShrinkBondLinesToExcludeAtomCharacters(Progress pb)
        {
            // so that they do not overlap label characters of other Atoms

            if (Outputs.ConvexHulls.Count > 1)
            {
                pb.Show();
            }
            pb.Message = "Clipping Bond Lines - Pass 1";
            pb.Value = 0;
            pb.Maximum = Outputs.ConvexHulls.Count;

            foreach (var hull in Outputs.ConvexHulls)
            {
                pb.Increment(1);

                // select lines which start or end with this atom
                var bondLines = (from line in Outputs.BondLines
                                 where line.StartAtomPath == hull.Key || line.EndAtomPath == hull.Key
                                 select line)
                    .ToList();

                foreach (var bondLine in bondLines)
                {
                    Point start;
                    Point end;

                    if (bondLine.Style == BondLineStyle.Thick)
                    {
                        for (var index = 0; index < 3; index++)
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
        }

        private bool TrimBondLine(BondLine leftOrRight, BondLine line, bool isInRing)
        {
            // Make a longer version of the line
            var startLonger = new Point(leftOrRight.Start.X, leftOrRight.Start.Y);
            var endLonger = new Point(leftOrRight.End.X, leftOrRight.End.Y);
            GeometryTool.AdjustLineAboutMidpoint(ref startLonger, ref endLonger, Inputs.MeanBondLength / 5);

            // See if they intersect at one end
            var crossingPoint = GeometryTool.GetIntersection(startLonger, endLonger, line.Start, line.End);

            // If they intersect update the main line
            bool crosses = crossingPoint != null;
            if (crosses)
            {
                var l1 = GeometryTool.DistanceBetween(crossingPoint.Value, leftOrRight.Start);
                var l2 = GeometryTool.DistanceBetween(crossingPoint.Value, leftOrRight.End);
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
            var otherLeftTail = otherBondLine.GetOriginalPoint(nameof(otherBondLine.LeftTail));
            var otherRightTail = otherBondLine.GetOriginalPoint(nameof(otherBondLine.RightTail));

            var otherLeftClip = new ClippingLine(otherBondLine.Nose, otherLeftTail, ClippingLineType.ExtendEnd);
            var otherRightClip = new ClippingLine(otherBondLine.Nose, otherRightTail, ClippingLineType.ExtendEnd);

            UpdateTailIfIntersectingAndLonger(otherBondLine, otherLeftClip, thisLeftClip, isLeft: true);
            UpdateTailIfIntersectingAndLonger(otherBondLine, otherRightClip, thisRightClip, isLeft: false);
            UpdateTailIfIntersectingAndLonger(thisBondLine, thisLeftClip, otherRightClip, isLeft: true);
            UpdateTailIfIntersectingAndLonger(thisBondLine, thisRightClip, otherLeftClip, isLeft: false);
        }

        private void UpdateTailIfIntersecting(BondLine bondLine, ClippingLine bondClip, ClippingLine otherClip, bool isLeft)
        {
            var intersection = GeometryTool.GetIntersection(bondClip.Start, bondClip.End, otherClip.Start, otherClip.End);
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
            var intersection = GeometryTool.GetIntersection(bondClip.Start, bondClip.End, otherClip.Start, otherClip.End);
            if (intersection.HasValue)
            {
                var currentTail = isLeft ? bondLine.LeftTail : bondLine.RightTail;
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