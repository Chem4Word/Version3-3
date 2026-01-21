// ---------------------------------------------------------------------------
//  Copyright (c) 2026, The .NET Foundation.
//  This software is released under the Apache Licence, Version 2.0.
//  The licence and further copyright text can be found in the file LICENCE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.Core.Enums;
using Chem4Word.Core.Helpers;
using Chem4Word.Core.UI.Forms;
using Chem4Word.Model2;
using Chem4Word.Model2.Enums;
using Chem4Word.Renderer.OoXmlV4.Entities;
using Chem4Word.Renderer.OoXmlV4.Entities.Diagnostic;
using Chem4Word.Renderer.OoXmlV4.Enums;
using Chem4Word.Renderer.OoXmlV4.TTF;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Media;
using System.Xml;
using Point = System.Windows.Point;
using Size = System.Windows.Size;

namespace Chem4Word.Renderer.OoXmlV4.OoXml
{
    public class OoXmlPositioner
    {
        private static string _class = MethodBase.GetCurrentMethod().DeclaringType?.Name;
        private static string _product = Assembly.GetExecutingAssembly().FullName.Split(',')[0];
        private TtfCharacter _hydrogenCharacter;

        public OoXmlPositioner(RendererInputs inputs) => Inputs = inputs;

        private RendererInputs Inputs { get; }
        private RendererOutputs Outputs { get; } = new RendererOutputs();

        /// <summary>
        /// Carries out the following
        /// 1. Position atom Label characters
        /// 2. Position bond lines
        /// 3. Position brackets (molecules and groups)
        /// 4. Position molecule label characters
        /// 5. Shrink bond lines to not clash with atom labels
        /// 5.1 Position molecular weight characters
        /// 6. Add mask underneath long bond lines of bonds detected as having crossing points
        /// </summary>
        /// <returns>PositionerOutputs a class to hold all of the required output types</returns>
        public RendererOutputs Position()
        {
            _hydrogenCharacter = Inputs.TtfCharacterSet['H'];

            var moleculeNo = 0;

            foreach (var mol in Inputs.Model.Molecules.Values)
            {
                // Operations 1 to 4
                ProcessMolecule(mol, Inputs.Progress, ref moleculeNo);
            }

            // Render reaction and annotation texts
            ProcessReactionTexts();
            ProcessAnnotationTexts();

            // 5.1 Add molecular weight
            ProcessMolecularWeight();

            // We are done now so we can return the final values
            return Outputs;
        }

        private void AddAnnotationCharacters(Annotation annotation, string path, List<FunctionalGroupTerm> terms,
                                             TextBlockJustification justification = TextBlockJustification.Left)
        {
            // Position characters
            var groupOfCharacters = GroupOfCharactersFromTerms(annotation.Position, annotation.Path, terms, justification);

            if (groupOfCharacters.Characters.Any())
            {
                // Centre group on annotation position
                groupOfCharacters.AdjustPosition(annotation.Position - groupOfCharacters.BoundingBox.TopLeft);

                // Transfer to output
                foreach (var character in groupOfCharacters.Characters)
                {
                    Outputs.AtomLabelCharacters.Add(character);
                }

                // Finally create diagnostics
                Outputs.Diagnostics.Rectangles.Add(new DiagnosticRectangle(Inflate(groupOfCharacters.BoundingBox, OoXmlConstants.AcsLineWidth / 2), OoXmlColours.VibrantGreen));
                Outputs.ConvexHulls.Add(path, ConvexHull(path));
            }
        }

        private void AddMolecularWeightAsCharacters(string molecularWeight, Point centrePoint, string path)
        {
            var point = new Point(centrePoint.X, centrePoint.Y);

            // Measure string
            var boundingBox = MeasureString(molecularWeight, point);

            // Place string characters such that they are hanging below the "line"
            if (boundingBox != Rect.Empty)
            {
                var place = new Point(point.X - boundingBox.Width / 2, point.Y + (point.Y - boundingBox.Top));
                PlaceString(molecularWeight, place, path);
            }

            Outputs.AllCharacterExtents = OoXmlHelper.GetAllCharacterExtents(Inputs.Model, Outputs);
        }

        private void AddMoleculeCaptionsAsCharacters(List<TextualProperty> labels, Point centrePoint, string moleculePath)
        {
            var point = new Point(centrePoint.X, centrePoint.Y);

            foreach (var label in labels)
            {
                // Measure string
                var boundingBox = MeasureString(label.Value, point);

                // Place string characters such that they are hanging below the "line"
                if (boundingBox != Rect.Empty)
                {
                    var place = new Point(point.X - boundingBox.Width / 2, point.Y + (point.Y - boundingBox.Top));
                    PlaceString(label.Value, place, moleculePath);
                }

                // Move to next line
                point.Offset(0, boundingBox.Height + Inputs.MeanBondLength * OoXmlConstants.MultipleBondOffsetPercentage / 2);
            }
        }

        private void AddReactionCharacters(Reaction reaction, List<FunctionalGroupTerm> terms,
                                           bool isReagent = true, TextBlockJustification justification = TextBlockJustification.Centre)
        {
            var path = reaction.Path + (isReagent ? "/reagent" : "/conditions");

            var groupOfCharacters = GroupOfCharactersFromTerms(reaction.MidPoint, path, terms, justification);
            if (groupOfCharacters.Characters.Any())
            {
                // Position characters
                // Centre group on reaction midpoint
                groupOfCharacters.AdjustPosition(reaction.MidPoint - groupOfCharacters.Centre);

                // March away from reaction midpoint
                var vector = OffsetVector(reaction, isReagent);

                bool isOutside;
                var maxLoops = 0;
                do
                {
                    groupOfCharacters.AdjustPosition(vector);
                    var hull = ConvexHull(groupOfCharacters.Characters);
                    isOutside = GeometryTool.IsOutside(reaction.HeadPoint, reaction.TailPoint, hull);

                    if (maxLoops++ >= 10)
                    {
                        break;
                    }
                } while (!isOutside);
                groupOfCharacters.AdjustPosition(vector);

                // Transfer to output
                foreach (var character in groupOfCharacters.Characters)
                {
                    Outputs.AtomLabelCharacters.Add(character);
                }

                // Finally create diagnostics
                Outputs.Diagnostics.Rectangles.Add(new DiagnosticRectangle(Inflate(groupOfCharacters.BoundingBox, OoXmlConstants.AcsLineWidth / 2), OoXmlColours.VibrantGreen));
                Outputs.ConvexHulls.Add(path, ConvexHull(path));
            }
        }

        private double BondOffset() => Inputs.MeanBondLength * OoXmlConstants.MultipleBondOffsetPercentage;

        private Rect CharacterExtents(Molecule mol, Rect existing)
        {
            var chars = Outputs.AtomLabelCharacters.Where(m => m.ParentMolecule.StartsWith(mol.Path)).ToList();
            foreach (var c in chars)
            {
                if (c.IsSmaller)
                {
                    var r = new Rect(c.Position,
                                     new Size(OoXmlHelper.ScaleCsTtfToCml(c.Character.Width, Inputs.MeanBondLength) * OoXmlConstants.SubscriptScaleFactor,
                                              OoXmlHelper.ScaleCsTtfToCml(c.Character.Height, Inputs.MeanBondLength) * OoXmlConstants.SubscriptScaleFactor));
                    existing.Union(r);
                }
                else
                {
                    var r = new Rect(c.Position,
                                     new Size(OoXmlHelper.ScaleCsTtfToCml(c.Character.Width, Inputs.MeanBondLength),
                                              OoXmlHelper.ScaleCsTtfToCml(c.Character.Height, Inputs.MeanBondLength)));
                    existing.Union(r);
                }
            }

            return existing;
        }

        private List<Point> ConvexHull(string atomPath)
        {
            var chars = Outputs.AtomLabelCharacters.Where(m => m.ParentAtom == atomPath).ToList();
            return ConvexHull(chars);
        }

        private List<Point> ConvexHull(List<AtomLabelCharacter> chars)
        {
            var points = new List<Point>();

            var margin = OoXmlConstants.CmlCharacterMargin;
            foreach (var c in chars)
            {
                // Top Left --
                points.Add(new Point(c.Position.X - margin, c.Position.Y - margin));
                if (c.IsSmaller)
                {
                    points.Add(new Point(c.Position.X + OoXmlHelper.ScaleCsTtfToCml(c.Character.Width, Inputs.MeanBondLength) * OoXmlConstants.SubscriptScaleFactor + margin,
                                         c.Position.Y - margin));
                    points.Add(new Point(c.Position.X + OoXmlHelper.ScaleCsTtfToCml(c.Character.Width, Inputs.MeanBondLength) * OoXmlConstants.SubscriptScaleFactor + margin,
                                         c.Position.Y + OoXmlHelper.ScaleCsTtfToCml(c.Character.Height, Inputs.MeanBondLength) * OoXmlConstants.SubscriptScaleFactor + margin));
                    points.Add(new Point(c.Position.X - margin,
                                         c.Position.Y + OoXmlHelper.ScaleCsTtfToCml(c.Character.Height, Inputs.MeanBondLength) * OoXmlConstants.SubscriptScaleFactor + margin));
                }
                else
                {
                    points.Add(new Point(c.Position.X + OoXmlHelper.ScaleCsTtfToCml(c.Character.Width, Inputs.MeanBondLength) + margin,
                                         c.Position.Y - margin));
                    points.Add(new Point(c.Position.X + OoXmlHelper.ScaleCsTtfToCml(c.Character.Width, Inputs.MeanBondLength) + margin,
                                         c.Position.Y + OoXmlHelper.ScaleCsTtfToCml(c.Character.Height, Inputs.MeanBondLength) + margin));
                    points.Add(new Point(c.Position.X - margin,
                                         c.Position.Y + OoXmlHelper.ScaleCsTtfToCml(c.Character.Height, Inputs.MeanBondLength) + margin));
                }
            }

            return GeometryTool.MakeConvexHull(points);
        }

        /// <summary>
        /// Creates the lines for a bond
        /// </summary>
        /// <param name="bond"></param>
        private void CreateBondLineObjects(Bond bond)
        {
            var bondStart = bond.StartAtom.Position;

            var bondEnd = bond.EndAtom.Position;

            #region Create Bond Line objects

            switch (bond.Order)
            {
                case ModelConstants.OrderZero:
                case "unknown":
                    Outputs.BondLines.Add(new BondLine(BondLineStyle.Zero, bond));
                    break;

                case ModelConstants.OrderPartial01:
                    Outputs.BondLines.Add(new BondLine(BondLineStyle.Half, bond));
                    break;

                case "1":
                case ModelConstants.OrderSingle:
                    switch (bond.Stereo)
                    {
                        case BondStereo.None:
                            Outputs.BondLines.Add(new BondLine(BondLineStyle.Solid, bond));
                            break;

                        case BondStereo.Hatch:
                            Outputs.BondLines.Add(new BondLine(BondLineStyle.Hatch, bond, Inputs.MeanBondLength));
                            break;

                        case BondStereo.Wedge:
                            Outputs.BondLines.Add(new BondLine(BondLineStyle.Wedge, bond, Inputs.MeanBondLength));
                            break;

                        case BondStereo.Indeterminate:
                            Outputs.BondLines.Add(new BondLine(BondLineStyle.Wavy, bond));
                            break;

                        case BondStereo.Thick:
                            Outputs.BondLines.Add(new BondLine(BondLineStyle.Thick, bond, Inputs.MeanBondLength));
                            break;

                        default:
                            Outputs.BondLines.Add(new BondLine(BondLineStyle.Solid, bond));
                            break;
                    }
                    break;

                case ModelConstants.OrderPartial12:
                case ModelConstants.OrderAromatic:

                    BondLine onePointFive;
                    BondLine onePointFiveDashed;
                    Point onePointFiveStart;
                    Point onePointFiveEnd;

                    switch (bond.Placement)
                    {
                        case BondDirection.Clockwise:
                            onePointFive = new BondLine(BondLineStyle.Solid, bond);
                            Outputs.BondLines.Add(onePointFive);
                            onePointFiveDashed = onePointFive.GetParallel(BondOffset());
                            onePointFiveStart = new Point(onePointFiveDashed.Start.X, onePointFiveDashed.Start.Y);
                            onePointFiveEnd = new Point(onePointFiveDashed.End.X, onePointFiveDashed.End.Y);
                            GeometryTool.AdjustLineAboutMidpoint(ref onePointFiveStart, ref onePointFiveEnd, -(BondOffset() / OoXmlConstants.LineShrinkPixels));
                            onePointFiveDashed = new BondLine(BondLineStyle.Half, onePointFiveStart, onePointFiveEnd, bond);
                            Outputs.BondLines.Add(onePointFiveDashed);
                            break;

                        case BondDirection.Anticlockwise:
                            onePointFive = new BondLine(BondLineStyle.Solid, bond);
                            Outputs.BondLines.Add(onePointFive);
                            onePointFiveDashed = onePointFive.GetParallel(-BondOffset());
                            onePointFiveStart = new Point(onePointFiveDashed.Start.X, onePointFiveDashed.Start.Y);
                            onePointFiveEnd = new Point(onePointFiveDashed.End.X, onePointFiveDashed.End.Y);
                            GeometryTool.AdjustLineAboutMidpoint(ref onePointFiveStart, ref onePointFiveEnd, -(BondOffset() / OoXmlConstants.LineShrinkPixels));
                            onePointFiveDashed = new BondLine(BondLineStyle.Half, onePointFiveStart, onePointFiveEnd, bond);
                            Outputs.BondLines.Add(onePointFiveDashed);
                            break;

                        case BondDirection.None:
                            onePointFive = new BondLine(BondLineStyle.Solid, bond);
                            Outputs.BondLines.Add(onePointFive.GetParallel(-(BondOffset() / 2)));
                            onePointFiveDashed = onePointFive.GetParallel(BondOffset() / 2);
                            onePointFiveDashed.SetLineStyle(BondLineStyle.Half);
                            Outputs.BondLines.Add(onePointFiveDashed);
                            break;
                    }
                    break;

                case "2":
                case ModelConstants.OrderDouble:
                    if (bond.Stereo == BondStereo.Indeterminate) //crossing bonds
                    {
                        // Crossed lines
                        var d = new BondLine(BondLineStyle.Solid, bondStart, bondEnd, bond);
                        var d1 = d.GetParallel(-(BondOffset() / 2));
                        var d2 = d.GetParallel(BondOffset() / 2);
                        Outputs.BondLines.Add(new BondLine(BondLineStyle.Solid, new Point(d1.Start.X, d1.Start.Y), new Point(d2.End.X, d2.End.Y), bond));
                        Outputs.BondLines.Add(new BondLine(BondLineStyle.Solid, new Point(d2.Start.X, d2.Start.Y), new Point(d1.End.X, d1.End.Y), bond));
                    }
                    else
                    {
                        switch (bond.Placement)
                        {
                            case BondDirection.Anticlockwise:
                                var da = new BondLine(BondLineStyle.Solid, bond);
                                Outputs.BondLines.Add(da);
                                Outputs.BondLines.Add(PlaceSecondaryLine(da, da.GetParallel(-BondOffset())));
                                break;

                            case BondDirection.Clockwise:
                                var dc = new BondLine(BondLineStyle.Solid, bond);
                                Outputs.BondLines.Add(dc);
                                Outputs.BondLines.Add(PlaceSecondaryLine(dc, dc.GetParallel(BondOffset())));
                                break;

                                // Local Function
                                BondLine PlaceSecondaryLine(BondLine primaryLine, BondLine secondaryLine)
                                {
                                    var primaryMidpoint = GeometryTool.GetMidPoint(primaryLine.Start, primaryLine.End);
                                    var secondaryMidpoint = GeometryTool.GetMidPoint(secondaryLine.Start, secondaryLine.End);

                                    var startPointa = secondaryLine.Start;
                                    var endPointa = secondaryLine.End;

                                    Point? centre = null;

                                    var clip = false;

                                    // Does bond have a primary ring?
                                    if (bond.PrimaryRing != null && bond.PrimaryRing.Centroid != null)
                                    {
                                        // Get angle between bond and vector to primary ring centre
                                        centre = bond.PrimaryRing.Centroid.Value;
                                        var primaryRingVector = primaryMidpoint - centre.Value;
                                        var angle = GeometryTool.AngleBetween(bond.BondVector, primaryRingVector);

                                        // Does bond have a secondary ring?
                                        if (bond.SubsidiaryRing != null && bond.SubsidiaryRing.Centroid != null)
                                        {
                                            // Get angle between bond and vector to secondary ring centre
                                            var centre2 = bond.SubsidiaryRing.Centroid.Value;
                                            var secondaryRingVector = primaryMidpoint - centre2;
                                            var angle2 = GeometryTool.AngleBetween(bond.BondVector, secondaryRingVector);

                                            // Get angle in which the offset line has moved with respect to the bond line
                                            var offsetVector = primaryMidpoint - secondaryMidpoint;
                                            var offsetAngle = GeometryTool.AngleBetween(bond.BondVector, offsetVector);

                                            // If in the same direction as secondary ring centre, use it
                                            if (Math.Sign(angle2) == Math.Sign(offsetAngle))
                                            {
                                                centre = centre2;
                                            }
                                        }

                                        // Is projection to centre at right angles +/- 10 degrees
                                        if (Math.Abs(angle) > 80 && Math.Abs(angle) < 100)
                                        {
                                            clip = true;
                                        }

                                        // Is secondary line outside of the "selected" ring
                                        var distance1 = primaryRingVector.Length;
                                        var distance2 = (secondaryMidpoint - centre.Value).Length;
                                        if (distance2 > distance1)
                                        {
                                            clip = false;
                                        }
                                    }

                                    if (clip)
                                    {
                                        var outIntersectP1 = GeometryTool.GetIntersection(startPointa, endPointa, bondStart, centre.Value);
                                        var outIntersectP2 = GeometryTool.GetIntersection(startPointa, endPointa, bondEnd, centre.Value);

                                        if (Inputs.Options.ShowDoubleBondTrimmingLines)
                                        {
                                            // Diagnostics
                                            Outputs.Diagnostics.Lines.Add(new DiagnosticLine(bond.StartAtom.Position, centre.Value, BondLineStyle.Dotted, OoXmlColours.Red));
                                            Outputs.Diagnostics.Lines.Add(new DiagnosticLine(bond.EndAtom.Position, centre.Value, BondLineStyle.Dotted, OoXmlColours.Red));
                                        }

                                        return new BondLine(BondLineStyle.Solid, outIntersectP1.Value, outIntersectP2.Value, bond);
                                    }
                                    else
                                    {
                                        GeometryTool.AdjustLineAboutMidpoint(ref startPointa, ref endPointa, -(BondOffset() / OoXmlConstants.LineShrinkPixels));
                                        return TrimSecondaryLine(new BondLine(BondLineStyle.Solid, startPointa, endPointa, bond));
                                    }
                                }

                                // Local Function
                                BondLine TrimSecondaryLine(BondLine bondLine)
                                {
                                    var otherStartBonds = bond.StartAtom.Bonds.Except(new[] { bond }).ToList();
                                    var otherEndBonds = bond.EndAtom.Bonds.Except(new[] { bond }).ToList();

                                    foreach (var otherBond in otherStartBonds)
                                    {
                                        TrimSecondaryBondLine(bond.StartAtom.Position, bond.EndAtom.Position,
                                                              otherBond.OtherAtom(bond.StartAtom).Position);
                                    }

                                    foreach (var otherBond in otherEndBonds)
                                    {
                                        TrimSecondaryBondLine(bond.EndAtom.Position, bond.StartAtom.Position,
                                                              otherBond.OtherAtom(bond.EndAtom).Position);
                                    }

                                    void TrimSecondaryBondLine(Point common, Point left, Point right)
                                    {
                                        var v1 = left - common;
                                        var v2 = right - common;
                                        var angle = GeometryTool.AngleBetween(v1, v2);
                                        var matrix = new Matrix();
                                        matrix.Rotate(angle / 2);
                                        v1 = v1 * 2 * matrix;
                                        if (Inputs.Options.ShowDoubleBondTrimmingLines)
                                        {
                                            Outputs.Diagnostics.Lines.Add(new DiagnosticLine(common, common + v1, BondLineStyle.Dotted, OoXmlColours.Blue));
                                        }

                                        var meetingPoint = GeometryTool.GetIntersection(bondLine.Start, bondLine.End,
                                                                      common, common + v1);
                                        if (meetingPoint != null)
                                        {
                                            if (common == bondLine.Bond.StartAtom.Position)
                                            {
                                                bondLine.Start = meetingPoint.Value;
                                            }
                                            if (common == bondLine.Bond.EndAtom.Position)
                                            {
                                                bondLine.End = meetingPoint.Value;
                                            }
                                        }
                                    }

                                    return bondLine;
                                }

                            default:
                                switch (bond.Stereo)
                                {
                                    case BondStereo.Cis:
                                        var dcc = new BondLine(BondLineStyle.Solid, bond);
                                        Outputs.BondLines.Add(dcc);
                                        var blnewc = dcc.GetParallel(BondOffset());
                                        var startPointn = blnewc.Start;
                                        var endPointn = blnewc.End;
                                        GeometryTool.AdjustLineAboutMidpoint(ref startPointn, ref endPointn, -(BondOffset() / OoXmlConstants.LineShrinkPixels));
                                        Outputs.BondLines.Add(new BondLine(BondLineStyle.Solid, startPointn, endPointn, bond));
                                        break;

                                    case BondStereo.Trans:
                                        var dtt = new BondLine(BondLineStyle.Solid, bond);
                                        Outputs.BondLines.Add(dtt);
                                        var blnewt = dtt.GetParallel(BondOffset());
                                        var startPointt = blnewt.Start;
                                        var endPointt = blnewt.End;
                                        GeometryTool.AdjustLineAboutMidpoint(ref startPointt, ref endPointt, -(BondOffset() / OoXmlConstants.LineShrinkPixels));
                                        Outputs.BondLines.Add(new BondLine(BondLineStyle.Solid, startPointt, endPointt, bond));
                                        break;

                                    default:
                                        var dp = new BondLine(BondLineStyle.Solid, bond);
                                        Outputs.BondLines.Add(dp.GetParallel(-(BondOffset() / 2)));
                                        Outputs.BondLines.Add(dp.GetParallel(BondOffset() / 2));
                                        break;
                                }
                                break;
                        }
                    }
                    break;

                case ModelConstants.OrderPartial23:
                    BondLine twoPointFive;
                    BondLine twoPointFiveDashed;
                    BondLine twoPointFiveParallel;
                    Point twoPointFiveStart;
                    Point twoPointFiveEnd;
                    switch (bond.Placement)
                    {
                        case BondDirection.Clockwise:
                            // Central bond line
                            twoPointFive = new BondLine(BondLineStyle.Solid, bond);
                            Outputs.BondLines.Add(twoPointFive);
                            // Solid bond line
                            twoPointFiveParallel = twoPointFive.GetParallel(-BondOffset());
                            twoPointFiveStart = new Point(twoPointFiveParallel.Start.X, twoPointFiveParallel.Start.Y);
                            twoPointFiveEnd = new Point(twoPointFiveParallel.End.X, twoPointFiveParallel.End.Y);
                            GeometryTool.AdjustLineAboutMidpoint(ref twoPointFiveStart, ref twoPointFiveEnd, -(BondOffset() / OoXmlConstants.LineShrinkPixels));
                            twoPointFiveParallel = new BondLine(BondLineStyle.Solid, twoPointFiveStart, twoPointFiveEnd, bond);
                            Outputs.BondLines.Add(twoPointFiveParallel);
                            // Dashed bond line
                            twoPointFiveDashed = twoPointFive.GetParallel(BondOffset());
                            twoPointFiveStart = new Point(twoPointFiveDashed.Start.X, twoPointFiveDashed.Start.Y);
                            twoPointFiveEnd = new Point(twoPointFiveDashed.End.X, twoPointFiveDashed.End.Y);
                            GeometryTool.AdjustLineAboutMidpoint(ref twoPointFiveStart, ref twoPointFiveEnd, -(BondOffset() / OoXmlConstants.LineShrinkPixels));
                            twoPointFiveDashed = new BondLine(BondLineStyle.Half, twoPointFiveStart, twoPointFiveEnd, bond);
                            Outputs.BondLines.Add(twoPointFiveDashed);
                            break;

                        case BondDirection.Anticlockwise:
                            // Central bond line
                            twoPointFive = new BondLine(BondLineStyle.Solid, bond);
                            Outputs.BondLines.Add(twoPointFive);
                            // Dashed bond line
                            twoPointFiveDashed = twoPointFive.GetParallel(-BondOffset());
                            twoPointFiveStart = new Point(twoPointFiveDashed.Start.X, twoPointFiveDashed.Start.Y);
                            twoPointFiveEnd = new Point(twoPointFiveDashed.End.X, twoPointFiveDashed.End.Y);
                            GeometryTool.AdjustLineAboutMidpoint(ref twoPointFiveStart, ref twoPointFiveEnd, -(BondOffset() / OoXmlConstants.LineShrinkPixels));
                            twoPointFiveDashed = new BondLine(BondLineStyle.Half, twoPointFiveStart, twoPointFiveEnd, bond);
                            Outputs.BondLines.Add(twoPointFiveDashed);
                            // Solid bond line
                            twoPointFiveParallel = twoPointFive.GetParallel(BondOffset());
                            twoPointFiveStart = new Point(twoPointFiveParallel.Start.X, twoPointFiveParallel.Start.Y);
                            twoPointFiveEnd = new Point(twoPointFiveParallel.End.X, twoPointFiveParallel.End.Y);
                            GeometryTool.AdjustLineAboutMidpoint(ref twoPointFiveStart, ref twoPointFiveEnd, -(BondOffset() / OoXmlConstants.LineShrinkPixels));
                            twoPointFiveParallel = new BondLine(BondLineStyle.Solid, twoPointFiveStart, twoPointFiveEnd, bond);
                            Outputs.BondLines.Add(twoPointFiveParallel);
                            break;

                        case BondDirection.None:
                            twoPointFive = new BondLine(BondLineStyle.Solid, bond);
                            Outputs.BondLines.Add(twoPointFive);
                            Outputs.BondLines.Add(twoPointFive.GetParallel(-BondOffset()));
                            twoPointFiveDashed = twoPointFive.GetParallel(BondOffset());
                            twoPointFiveDashed.SetLineStyle(BondLineStyle.Half);
                            Outputs.BondLines.Add(twoPointFiveDashed);
                            break;
                    }
                    break;

                case "3":
                case ModelConstants.OrderTriple:
                    var triple = new BondLine(BondLineStyle.Solid, bond);
                    Outputs.BondLines.Add(triple);
                    Outputs.BondLines.Add(triple.GetParallel(BondOffset()));
                    Outputs.BondLines.Add(triple.GetParallel(-BondOffset()));
                    break;

                default:
                    // Draw a single line, so that there is something to see
                    Outputs.BondLines.Add(new BondLine(BondLineStyle.Solid, bond));
                    break;
            }

            #endregion Create Bond Line objects
        }

        private void CreateElementCharacters(Atom atom)
        {
            var module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            var atomSymbol = atom.AtomSymbol;
            var showImplicitHydrogenCharacters = atom.ShowImplicitHydrogenCharacters;

            if (!string.IsNullOrEmpty(atomSymbol))
            {
                #region Set Up Atom Colour

                string atomColour = OoXmlColours.Black;
                if (Inputs.Model.ShowColouredAtoms
                    && atom.Element.Colour != null)
                {
                    atomColour = atom.Element.Colour;
                    // Strip out # as OoXml does not use it
                    atomColour = atomColour.Replace("#", "");
                }

                #endregion Set Up Atom Colour

                // Create main character group
                var main = new GroupOfCharacters(atom.Position, atom.Path, atom.Parent.Path,
                                                 Inputs.TtfCharacterSet, Inputs.MeanBondLength);
                main.AddString(atomSymbol, atomColour);

                // Create a special group for the first character
                var firstCharacter = new GroupOfCharacters(atom.Position, atom.Path, atom.Parent.Path,
                                                           Inputs.TtfCharacterSet, Inputs.MeanBondLength);
                firstCharacter.AddCharacter(atomSymbol[0], atomColour);

                // Distance to move horizontally to midpoint of whole label
                var x = atom.Position.X - main.Centre.X;
                // Distance to move vertically to midpoint of first character
                var y = atom.Position.Y - firstCharacter.Centre.Y;

                // Move to new position
                main.AdjustPosition(new Vector(x, y));

                // Get orientation of other character groups
                var orientation = atom.ImplicitHPlacement;

                // Implicit Hydrogen Labels
                GroupOfCharacters hydrogens = null;

                if (showImplicitHydrogenCharacters)
                {
                    // Determine position of implicit hydrogen labels
                    var implicitHCount = atom.ImplicitHydrogenCount;
                    if (implicitHCount > 0)
                    {
                        hydrogens = new GroupOfCharacters(atom.Position, atom.Path, atom.Parent.Path,
                                                          Inputs.TtfCharacterSet, Inputs.MeanBondLength);

                        // Create characters
                        hydrogens.AddCharacter('H', atomColour);
                        if (implicitHCount > 1)
                        {
                            foreach (var character in implicitHCount.ToString())
                            {
                                hydrogens.AddCharacter(character, atomColour, true);
                            }
                        }

                        // Adjust position of block
                        switch (orientation)
                        {
                            case CompassPoints.North:
                                hydrogens.AdjustPosition(main.BoundingBox.TopLeft - hydrogens.BoundingBox.BottomLeft);
                                hydrogens.Nudge(CompassPoints.East, main.BoundingBox.Width / 2 - OoXmlHelper.ScaleCsTtfToCml(_hydrogenCharacter.Width, Inputs.MeanBondLength) / 2);
                                break;

                            case CompassPoints.East:
                                hydrogens.AdjustPosition(main.BoundingBox.TopRight - hydrogens.BoundingBox.TopLeft);
                                break;

                            case CompassPoints.South:
                                hydrogens.AdjustPosition(main.BoundingBox.BottomLeft - hydrogens.BoundingBox.TopLeft);
                                hydrogens.Nudge(CompassPoints.East, main.BoundingBox.Width / 2 - OoXmlHelper.ScaleCsTtfToCml(_hydrogenCharacter.Width, Inputs.MeanBondLength) / 2);
                                break;

                            case CompassPoints.West:
                                hydrogens.AdjustPosition(main.BoundingBox.TopLeft - hydrogens.BoundingBox.TopRight);
                                break;
                        }
                        hydrogens.Nudge(orientation);
                    }
                }

                // Charge
                GroupOfCharacters charge = null;

                var chargeValue = atom.FormalCharge ?? 0;
                var absCharge = Math.Abs(chargeValue);

                if (absCharge > 0)
                {
                    charge = new GroupOfCharacters(atom.Position, atom.Path, atom.Parent.Path,
                                                      Inputs.TtfCharacterSet, Inputs.MeanBondLength);

                    // Create characters
                    var chargeSign = Math.Sign(chargeValue) > 0 ? "+" : "-";
                    var digits = absCharge == 1 ? chargeSign : $"{absCharge}{chargeSign}";

                    foreach (var character in digits)
                    {
                        charge.AddCharacter(character, atomColour, true);
                    }

                    // Adjust position of charge
                    if (hydrogens == null)
                    {
                        charge.AdjustPosition(main.BoundingBox.TopRight - charge.WestCentre);
                        charge.Nudge(CompassPoints.East);
                    }
                    else
                    {
                        var destination = main.BoundingBox.TopRight;

                        switch (orientation)
                        {
                            case CompassPoints.North:
                                if (hydrogens.BoundingBox.Right >= main.BoundingBox.Right)
                                {
                                    destination.X = hydrogens.BoundingBox.Right;
                                }
                                charge.AdjustPosition(destination - charge.WestCentre);
                                charge.Nudge(CompassPoints.East);
                                break;

                            case CompassPoints.East:
                                charge.AdjustPosition(destination - charge.SouthCentre);
                                charge.Nudge(CompassPoints.North);
                                break;

                            case CompassPoints.South:
                            case CompassPoints.West:
                                charge.AdjustPosition(destination - charge.WestCentre);
                                charge.Nudge(CompassPoints.East);
                                break;
                        }
                    }
                }

                // Isotope
                GroupOfCharacters isotope = null;

                var isoValue = atom.IsotopeNumber ?? 0;

                if (isoValue > 0)
                {
                    isotope = new GroupOfCharacters(atom.Position, atom.Path, atom.Parent.Path,
                                                    Inputs.TtfCharacterSet, Inputs.MeanBondLength);

                    foreach (var character in isoValue.ToString())
                    {
                        isotope.AddCharacter(character, atomColour, true);
                    }

                    // Adjust position of isotope
                    if (hydrogens == null)
                    {
                        isotope.AdjustPosition(main.BoundingBox.TopLeft - isotope.EastCentre);
                        isotope.Nudge(CompassPoints.West);
                    }
                    else
                    {
                        var destination = main.BoundingBox.TopLeft;

                        switch (orientation)
                        {
                            case CompassPoints.North:
                                if (hydrogens.BoundingBox.Left <= main.BoundingBox.Left)
                                {
                                    destination.X = hydrogens.BoundingBox.Left;
                                }
                                isotope.AdjustPosition(destination - isotope.EastCentre);
                                isotope.Nudge(CompassPoints.West);
                                break;

                            case CompassPoints.East:
                            case CompassPoints.South:
                                isotope.AdjustPosition(destination - isotope.EastCentre);
                                isotope.Nudge(CompassPoints.West);
                                break;

                            case CompassPoints.West:
                                isotope.AdjustPosition(destination - isotope.SouthCentre);
                                isotope.Nudge(CompassPoints.North);
                                break;
                        }
                    }
                }

                // Transfer to output
                foreach (var character in main.Characters)
                {
                    Outputs.AtomLabelCharacters.Add(character);
                }

                Outputs.Diagnostics.Rectangles.Add(new DiagnosticRectangle(Inflate(main.BoundingBox, OoXmlConstants.AcsLineWidth / 2), OoXmlColours.VibrantGreen));

                if (hydrogens != null)
                {
                    foreach (var character in hydrogens.Characters)
                    {
                        Outputs.AtomLabelCharacters.Add(character);
                    }

                    Outputs.Diagnostics.Rectangles.Add(new DiagnosticRectangle(Inflate(hydrogens.BoundingBox, OoXmlConstants.AcsLineWidth / 2), OoXmlColours.Purple));
                }

                if (charge != null)
                {
                    foreach (var character in charge.Characters)
                    {
                        Outputs.AtomLabelCharacters.Add(character);
                    }

                    Outputs.Diagnostics.Rectangles.Add(new DiagnosticRectangle(Inflate(charge.BoundingBox, OoXmlConstants.AcsLineWidth / 2), OoXmlColours.Red));
                }

                if (isotope != null)
                {
                    foreach (var character in isotope.Characters)
                    {
                        Outputs.AtomLabelCharacters.Add(character);
                    }

                    Outputs.Diagnostics.Rectangles.Add(new DiagnosticRectangle(Inflate(isotope.BoundingBox, OoXmlConstants.AcsLineWidth / 2), OoXmlColours.LightBlue));
                }

                // Generate Convex Hull
                Outputs.ConvexHulls.Add(atom.Path, ConvexHull(atom.Path));
            }
        }

        private void CreateFunctionalGroupCharacters(Atom atom)
        {
            var fg = atom.Element as FunctionalGroup;
            var reverse = atom.FunctionalGroupPlacement == CompassPoints.West;

            #region Set Up Atom Colour

            var atomColour = OoXmlColours.Black;
            if (Inputs.Model.ShowColouredAtoms
                && fg?.Colour != null)
            {
                atomColour = fg.Colour;
                // Strip out # as OoXml does not use it
                atomColour = atomColour.Replace("#", "");
            }

            #endregion Set Up Atom Colour

            if (fg != null)
            {
                var terms = fg.ExpandIntoTerms(reverse);

                // Create a special group for the first character
                var firstCapital = new GroupOfCharacters(atom.Position, atom.Path, atom.Parent.Path,
                                                           Inputs.TtfCharacterSet, Inputs.MeanBondLength);

                var main = new GroupOfCharacters(atom.Position, atom.Path, atom.Parent.Path,
                                                 Inputs.TtfCharacterSet, Inputs.MeanBondLength);

                var auxiliary = new GroupOfCharacters(atom.Position, atom.Path, atom.Parent.Path,
                                                      Inputs.TtfCharacterSet, Inputs.MeanBondLength);

                bool firstCapitalFound = false;

                // Generate characters
                foreach (var term in terms)
                {
                    if (term.IsAnchor)
                    {
                        main.AddParts(term.Parts, atomColour);
                        if (!firstCapitalFound)
                        {
                            firstCapital.AddCharacter(term.FirstCaptial, atomColour);
                            firstCapitalFound = true;
                        }
                    }
                    else
                    {
                        auxiliary.AddParts(term.Parts, atomColour);
                    }
                }

                // Position characters
                if (firstCapitalFound)
                {
                    // Distance to move horizontally to midpoint of whole label
                    var x = atom.Position.X - main.Centre.X;
                    // Distance to move vertically to midpoint of first character
                    var y = atom.Position.Y - firstCapital.Centre.Y;

                    // Move to new position
                    main.AdjustPosition(new Vector(x, y));
                }
                else
                {
                    // Fallback to old method
                    main.AdjustPosition(atom.Position - main.Centre);
                }

                Outputs.Diagnostics.Rectangles.Add(new DiagnosticRectangle(Inflate(main.BoundingBox, OoXmlConstants.AcsLineWidth / 2), OoXmlColours.VibrantGreen));

                if (auxiliary.Characters.Any())
                {
                    switch (atom.FunctionalGroupPlacement)
                    {
                        case CompassPoints.East:
                            auxiliary.AdjustPosition(main.BoundingBox.TopRight - auxiliary.BoundingBox.TopLeft);
                            auxiliary.Nudge(CompassPoints.East);
                            break;

                        case CompassPoints.West:
                            auxiliary.AdjustPosition(main.BoundingBox.TopLeft - auxiliary.BoundingBox.TopRight);
                            auxiliary.Nudge(CompassPoints.West);
                            break;
                    }

                    Outputs.Diagnostics.Rectangles.Add(new DiagnosticRectangle(Inflate(auxiliary.BoundingBox, OoXmlConstants.AcsLineWidth / 2), OoXmlColours.DarkOrange));
                }

                // Transfer to output
                foreach (var character in main.Characters)
                {
                    Outputs.AtomLabelCharacters.Add(character);
                }

                if (auxiliary.Characters.Any())
                {
                    foreach (var character in auxiliary.Characters)
                    {
                        Outputs.AtomLabelCharacters.Add(character);
                    }
                }
            }

            // Generate Convex Hull
            Outputs.ConvexHulls.Add(atom.Path, ConvexHull(atom.Path));
        }

        private Point GetCharacterPosition(Point cursorPosition, TtfCharacter character)
        {
            var position = new Point(cursorPosition.X + OoXmlHelper.ScaleCsTtfToCml(character.OriginX, Inputs.MeanBondLength),
                                     cursorPosition.Y + OoXmlHelper.ScaleCsTtfToCml(character.OriginY, Inputs.MeanBondLength));

            return position;
        }

        private GroupOfCharacters GroupOfCharactersFromTerms(Point position, string path, List<FunctionalGroupTerm> terms, TextBlockJustification justification)
        {
            var groupOfCharacters = new GroupOfCharacters(position, path, path,
                                              Inputs.TtfCharacterSet, Inputs.MeanBondLength);

            var lineNumber = 0;

            if (terms != null)
            {
                // Generate characters
                foreach (var term in terms)
                {
                    if (term.Parts.Any())
                    {
                        // Measure
                        var measure = new GroupOfCharacters(new Point(0, 0), null, null,
                                                            Inputs.TtfCharacterSet, Inputs.MeanBondLength);
                        measure.AddParts(term.Parts, OoXmlColours.Black);

                        if (lineNumber++ > 0)
                        {
                            // Apply NewLine with measured offset
                            switch (justification)
                            {
                                case TextBlockJustification.Left:
                                    groupOfCharacters.NewLine();
                                    break;

                                case TextBlockJustification.Centre:
                                    groupOfCharacters.NewLine(
                                        groupOfCharacters.BoundingBox.Width / 2 - measure.BoundingBox.Width / 2);
                                    break;

                                case TextBlockJustification.Right:
                                    groupOfCharacters.NewLine(
                                        groupOfCharacters.BoundingBox.Width - measure.BoundingBox.Width);
                                    break;
                            }
                        }

                        // Add Characters for real
                        groupOfCharacters.AddParts(term.Parts, OoXmlColours.Black);
                    }
                }
            }

            return groupOfCharacters;
        }

        private Rect Inflate(Rect r, double x)
        {
            var r1 = r;
            r1.Inflate(x, x);
            return r1;
        }

        private Rect MeasureString(string text, Point startPoint)
        {
            var boundingBox = Rect.Empty;
            var cursor = new Point(startPoint.X, startPoint.Y);

            for (var idx = 0; idx < text.Length; idx++)
            {
                var c = Inputs.TtfCharacterSet[OoXmlConstants.DefaultCharacter];
                var chr = text[idx];
                if (Inputs.TtfCharacterSet.ContainsKey(chr))
                {
                    c = Inputs.TtfCharacterSet[chr];
                }

                if (c != null)
                {
                    var position = GetCharacterPosition(cursor, c);

                    var thisRect = new Rect(new Point(position.X, position.Y),
                                            new Size(OoXmlHelper.ScaleCsTtfToCml(c.Width, Inputs.MeanBondLength),
                                                     OoXmlHelper.ScaleCsTtfToCml(c.Height, Inputs.MeanBondLength)));

                    boundingBox.Union(thisRect);

                    if (idx < text.Length - 1)
                    {
                        // Move to next Character position
                        cursor.Offset(OoXmlHelper.ScaleCsTtfToCml(c.IncrementX, Inputs.MeanBondLength), 0);
                    }
                }
            }

            return boundingBox;
        }

        private Vector OffsetVector(Reaction reaction, bool isReagent)
        {
            var arrowIsBackwards = reaction.TailPoint.X > reaction.HeadPoint.X;
            double perpendicularAngle = 90;
            if (arrowIsBackwards)
            {
                perpendicularAngle = -perpendicularAngle;
            }

            var rotator = new Matrix();
            var userOffset = isReagent ? reaction.ReagentsBlockOffset : reaction.ConditionsBlockOffset;
            if (userOffset is null)
            {
                // above or below the arrow
                if (isReagent)
                {
                    rotator.Rotate(-perpendicularAngle);
                }
                else
                {
                    rotator.Rotate(perpendicularAngle);
                }
            }
            else
            {
                // ToDo: [MAW] Implement if required
                Debugger.Break();
            }

            // Create the perpendicular vector
            var perpendicularVector = reaction.ReactionVector;
            perpendicularVector.Normalize();
            perpendicularVector *= rotator;

            perpendicularVector *= OoXmlConstants.MultipleBondOffsetPercentage * Inputs.MeanBondLength;

            return perpendicularVector;
        }

        private void PlaceString(string text, Point startPoint, string path)
        {
            var cursor = new Point(startPoint.X, startPoint.Y);

            for (var idx = 0; idx < text.Length; idx++)
            {
                var c = Inputs.TtfCharacterSet[OoXmlConstants.DefaultCharacter];
                var chr = text[idx];
                if (Inputs.TtfCharacterSet.ContainsKey(chr))
                {
                    c = Inputs.TtfCharacterSet[chr];
                }

                if (c != null)
                {
                    var position = GetCharacterPosition(cursor, c);

                    var alc = new AtomLabelCharacter(position, c, OoXmlColours.Black, path, path);
                    Outputs.AtomLabelCharacters.Add(alc);

                    if (idx < text.Length - 1)
                    {
                        // Move to next Character position
                        cursor.Offset(OoXmlHelper.ScaleCsTtfToCml(c.IncrementX, Inputs.MeanBondLength), 0);
                    }
                }
            }
        }

        private void ProcessAnnotationTexts()
        {
            foreach (var annotation in Inputs.Model.Annotations.Values)
            {
                if (!string.IsNullOrEmpty(annotation.Xaml))
                {
                    var terms = TermsFromFlowDocument(annotation.Xaml);
                    AddAnnotationCharacters(annotation, annotation.Path, terms);
                }
            }

            Outputs.AllCharacterExtents = OoXmlHelper.GetAllCharacterExtents(Inputs.Model, Outputs);
        }

        private void ProcessAtoms(Molecule mol, Progress pb, int moleculeNo)
        {
            // Create Characters
            if (mol.Atoms.Count > 1)
            {
                pb.Show();
            }
            pb.Message = $"Processing Atoms in Molecule {moleculeNo}";
            pb.Value = 0;
            pb.Maximum = mol.Atoms.Count;

            foreach (var atom in mol.Atoms.Values)
            {
                pb.Increment(1);
                if (atom.Element is Element)
                {
                    CreateElementCharacters(atom);
                }

                if (atom.Element is FunctionalGroup)
                {
                    CreateFunctionalGroupCharacters(atom);
                }
            }
        }

        private void ProcessBonds(Molecule mol, Progress pb, int moleculeNo)
        {
            if (mol.Bonds.Count > 0)
            {
                pb.Show();
            }
            pb.Message = $"Processing Bonds in Molecule {moleculeNo}";
            pb.Value = 0;
            pb.Maximum = mol.Bonds.Count;

            foreach (var bond in mol.Bonds)
            {
                pb.Increment(1);
                CreateBondLineObjects(bond);
            }
        }

        private void ProcessMolecularWeight()
        {
            if (Inputs.Model.ShowMolecularWeight)
            {
                var point = new Point(Outputs.AllCharacterExtents.Left
                                      + Outputs.AllCharacterExtents.Width / 2,
                                      Outputs.AllCharacterExtents.Bottom
                                      + Inputs.MeanBondLength * OoXmlConstants.MultipleBondOffsetPercentage / 2);

                AddMolecularWeightAsCharacters(SafeDouble.AsCMLString(Inputs.Model.MolecularWeight), point, "molWeight");

                Outputs.AllCharacterExtents = OoXmlHelper.GetAllCharacterExtents(Inputs.Model, Outputs);
            }
        }

        private void ProcessMolecule(Molecule mol, Progress pb, ref int molNumber)
        {
            molNumber++;

            // 1. Position Atom Label Characters
            ProcessAtoms(mol, pb, molNumber);

            // 2. Position Bond Lines
            ProcessBonds(mol, pb, molNumber);

            // Populate diagnostic data
            foreach (var ring in mol.Rings)
            {
                if (ring.Centroid.HasValue)
                {
                    var centre = ring.Centroid.Value;
                    Outputs.RingCenters.Add(centre);

                    var innerCircle = new InnerCircle();
                    // Traverse() obtains list of atoms in anti-clockwise direction around ring
                    innerCircle.Points.AddRange(ring.Traverse().Select(a => a.Position).ToList());
                    innerCircle.Centre = centre;
                    //Outputs.InnerCircles.Add(innerCircle)
                }
            }

            // Recurse into any child molecules
            foreach (var child in mol.Molecules.Values)
            {
                ProcessMolecule(child, pb, ref molNumber);
            }

            // Determine Extents

            // Atoms <= InternalCharacters <= GroupBrackets <= MoleculesBrackets <= ExternalCharacters

            // Atoms & InternalCharacters
            var thisMoleculeExtents = new MoleculeExtents(mol.Path, mol.BoundingBox);
            thisMoleculeExtents.SetInternalCharacterExtents(CharacterExtents(mol, thisMoleculeExtents.AtomExtents));
            Outputs.AllMoleculeExtents.Add(thisMoleculeExtents);

            // Grouped Molecules
            if (mol.IsGrouped)
            {
                var boundingBox = Rect.Empty;

                var childGroups = Outputs.AllMoleculeExtents.Where(g => g.Path.StartsWith($"{mol.Path}/")).ToList();
                foreach (var child in childGroups)
                {
                    boundingBox.Union(child.ExternalCharacterExtents);
                }

                if (boundingBox != Rect.Empty)
                {
                    boundingBox.Union(thisMoleculeExtents.ExternalCharacterExtents);
                    if (Inputs.Model.ShowMoleculeGrouping)
                    {
                        boundingBox = Inflate(boundingBox, OoXmlHelper.BracketOffset(Inputs.MeanBondLength));
                        Outputs.GroupBrackets.Add(boundingBox);
                    }
                    thisMoleculeExtents.SetGroupBracketExtents(boundingBox);
                }
            }

            // 3. Add required Brackets
            var showBrackets = mol.ShowMoleculeBrackets.HasValue && mol.ShowMoleculeBrackets.Value
                               || mol.Count.HasValue && mol.Count.Value > 0
                               || mol.FormalCharge.HasValue && mol.FormalCharge.Value != 0
                               || mol.SpinMultiplicity.HasValue && mol.SpinMultiplicity.Value > 1;

            var rect = thisMoleculeExtents.GroupBracketsExtents;
            var children = Outputs.AllMoleculeExtents.Where(g => g.Path.StartsWith($"{mol.Path}/")).ToList();
            foreach (var child in children)
            {
                rect.Union(child.GroupBracketsExtents);
            }

            if (showBrackets)
            {
                rect = Inflate(rect, OoXmlHelper.BracketOffset(Inputs.MeanBondLength));
                Outputs.MoleculeBrackets.Add(rect);
            }
            thisMoleculeExtents.SetMoleculeBracketExtents(rect);

            var characters = string.Empty;

            if (mol.FormalCharge.HasValue && mol.FormalCharge.Value != 0)
            {
                // Add FormalCharge at top right
                var charge = mol.FormalCharge.Value;
                var absCharge = Math.Abs(charge);

                var chargeSign = Math.Sign(charge) > 0 ? "+" : "-";
                characters = absCharge == 1 ? chargeSign : $"{absCharge}{chargeSign}";
            }

            if (mol.SpinMultiplicity.HasValue && mol.SpinMultiplicity.Value > 1)
            {
                // Append SpinMultiplicity
                switch (mol.SpinMultiplicity.Value)
                {
                    case 2:
                        characters += "•";
                        break;

                    case 3:
                        characters += "••";
                        break;
                }
            }

            if (!string.IsNullOrEmpty(characters))
            {
                // Draw characters at top right (outside of any brackets)
                var point = new Point(thisMoleculeExtents.MoleculeBracketsExtents.Right
                                      + OoXmlConstants.MultipleBondOffsetPercentage * Inputs.MeanBondLength,
                                      thisMoleculeExtents.MoleculeBracketsExtents.Top
                                      + OoXmlHelper.ScaleCsTtfToCml(_hydrogenCharacter.Height, Inputs.MeanBondLength) / 2);
                PlaceString(characters, point, mol.Path);
            }

            if (mol.Count.HasValue && mol.Count.Value > 0)
            {
                // Draw Count at bottom right
                var point = new Point(thisMoleculeExtents.MoleculeBracketsExtents.Right
                                      + OoXmlConstants.MultipleBondOffsetPercentage * Inputs.MeanBondLength,
                                      thisMoleculeExtents.MoleculeBracketsExtents.Bottom
                                      + OoXmlHelper.ScaleCsTtfToCml(_hydrogenCharacter.Height, Inputs.MeanBondLength) / 2);
                PlaceString($"{mol.Count}", point, mol.Path);
            }

            if (mol.Count.HasValue
                || mol.FormalCharge.HasValue
                || mol.SpinMultiplicity.HasValue)
            {
                // Recalculate as we have just added extra characters
                thisMoleculeExtents.SetExternalCharacterExtents(CharacterExtents(mol, thisMoleculeExtents.MoleculeBracketsExtents));
            }

            // 4. Position Molecule Label Characters
            // Handle optional rendering of molecule labels centered on brackets (if any) and below any molecule property characters
            if (Inputs.Model.ShowMoleculeCaptions && mol.Captions.Any())
            {
                var point = new Point(thisMoleculeExtents.MoleculeBracketsExtents.Left
                                        + thisMoleculeExtents.MoleculeBracketsExtents.Width / 2,
                                      thisMoleculeExtents.ExternalCharacterExtents.Bottom
                                        + Inputs.MeanBondLength * OoXmlConstants.MultipleBondOffsetPercentage / 2);

                AddMoleculeCaptionsAsCharacters(mol.Captions.ToList(), point, mol.Path);
                // Recalculate as we have just added extra characters
                thisMoleculeExtents.SetExternalCharacterExtents(CharacterExtents(mol, thisMoleculeExtents.MoleculeBracketsExtents));
            }
        }

        private void ProcessReactionTexts()
        {
            foreach (var scheme in Inputs.Model.ReactionSchemes.Values)
            {
                foreach (var reaction in scheme.Reactions.Values)
                {
                    if (!string.IsNullOrEmpty(reaction.ReagentText))
                    {
                        var terms = TermsFromFlowDocument(reaction.ReagentText);
                        AddReactionCharacters(reaction, terms, true);
                    }

                    if (!string.IsNullOrEmpty(reaction.ConditionsText))
                    {
                        var terms = TermsFromFlowDocument(reaction.ConditionsText);
                        AddReactionCharacters(reaction, terms, false);
                    }
                }
            }

            Outputs.AllCharacterExtents = OoXmlHelper.GetAllCharacterExtents(Inputs.Model, Outputs);
        }

        private List<FunctionalGroupTerm> TermsFromFlowDocument(string flowDocument)
        {
            var result = new List<FunctionalGroupTerm>();

            var xml = new XmlDocument();
            xml.LoadXml(flowDocument);

            var root = xml.FirstChild.FirstChild;
            var term = new FunctionalGroupTerm();

            foreach (XmlNode node in root.ChildNodes)
            {
                switch (node.LocalName)
                {
                    case "Run":
                        var part = new FunctionalGroupPart
                        {
                            Text = node.InnerText
                        };
                        if (!string.IsNullOrEmpty(part.Text))
                        {
                            if (node.Attributes?["BaselineAlignment"] != null)
                            {
                                var alignment = node.Attributes["BaselineAlignment"].Value;
                                switch (alignment)
                                {
                                    case "Subscript":
                                        part.Type = FunctionalGroupPartType.Subscript;
                                        break;

                                    case "Superscript":
                                        part.Type = FunctionalGroupPartType.Superscript;
                                        break;
                                }
                            }
                            term.Parts.Add(part);
                        }
                        break;

                    case "LineBreak":
                        result.Add(term);
                        term = new FunctionalGroupTerm();
                        break;
                }
            }

            // Handle "missing" LineBreak
            if (term.Parts.Any())
            {
                result.Add(term);
            }

            // Add 'fake' space character to any blank lines
            foreach (var line in result)
            {
                if (line.Parts.Count == 0)
                {
                    line.Parts.Add(new FunctionalGroupPart
                    {
                        Text = " "
                    });
                }
            }

            return result;
        }
    }
}
