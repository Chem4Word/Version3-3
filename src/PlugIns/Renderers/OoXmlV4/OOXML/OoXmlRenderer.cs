// ---------------------------------------------------------------------------
//  Copyright (c) 2025, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.Core.Helpers;
using Chem4Word.Core.UI.Forms;
using Chem4Word.Model2;
using Chem4Word.Renderer.OoXmlV4.Entities;
using Chem4Word.Renderer.OoXmlV4.Enums;
using Chem4Word.Renderer.OoXmlV4.TTF;
using DocumentFormat.OpenXml;
using IChem4Word.Contracts;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Media;
using A = DocumentFormat.OpenXml.Drawing;
using Drawing = DocumentFormat.OpenXml.Wordprocessing.Drawing;
using Point = System.Windows.Point;
using Run = DocumentFormat.OpenXml.Wordprocessing.Run;
using Wp = DocumentFormat.OpenXml.Drawing.Wordprocessing;
using Wpg = DocumentFormat.OpenXml.Office2010.Word.DrawingGroup;
using Wps = DocumentFormat.OpenXml.Office2010.Word.DrawingShape;

namespace Chem4Word.Renderer.OoXmlV4.OOXML
{
    // ReSharper disable PossiblyMistakenUseOfParamsMethod
    [SuppressMessage("Minor Code Smell", "S3220:Method calls should not resolve ambiguously to overloads with \"params\"", Justification = "<OoXml>")]
    public class OoXmlRenderer
    {
        // DrawingML Units
        // https://startbigthinksmall.wordpress.com/2010/01/04/points-inches-and-emus-measuring-units-in-office-open-xml/
        // EMU Calculator
        // http://lcorneliussen.de/raw/dashboards/ooxml/

        private static string _product = Assembly.GetExecutingAssembly().FullName.Split(',')[0];
        private static string _class = MethodBase.GetCurrentMethod().DeclaringType?.Name;

        private Wpg.WordprocessingGroup _wordprocessingGroup;
        private long _ooxmlId;
        private Rect _boundingBoxOfEverything;
        private Rect _boundingBoxOfAllAtoms;

        // Inputs to positioner
        private Dictionary<char, TtfCharacter> _TtfCharacterSet;

        private readonly OoXmlV4Options _options;
        private readonly IChem4WordTelemetry _telemetry;
        private Point _topLeft;
        private readonly Model _chemistryModel;
        private double _medianBondLength;

        // Outputs of positioner
        private PositionerOutputs _positionerOutputs;

        public OoXmlRenderer(Model model, OoXmlV4Options options, IChem4WordTelemetry telemetry, Point topLeft)
        {
            var module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            _telemetry = telemetry;
            _telemetry.Write(module, "Verbose", "Called");

            _options = options;
            _topLeft = topLeft;
            _chemistryModel = model;
            _medianBondLength = model.MeanBondLength;

            LoadFont();
        }

        public Run GenerateRun()
        {
            var module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            _telemetry.Write(module, "Verbose", "Called");

            var swr = new Stopwatch();
            swr.Start();

            // Initialise OoXml Object counter
            _ooxmlId = 1;

            //set the median bond length
            _medianBondLength = _chemistryModel.MeanBondLength;
            if (_chemistryModel.GetAllBonds().Count == 0)
            {
                _medianBondLength = Constants.StandardBondLength;
            }

            // Initialise progress monitoring
            var progress = new Progress
            {
                TopLeft = _topLeft
            };

            var positioner = new OoXmlPositioner(new PositionerInputs
            {
                Progress = progress,
                Options = _options,
                TtfCharacterSet = _TtfCharacterSet,
                Telemetry = _telemetry,
                MeanBondLength = _medianBondLength,
                Model = _chemistryModel,
            });

            _positionerOutputs = positioner.Position();

            _boundingBoxOfAllAtoms = _chemistryModel.BoundingBoxOfCmlPoints;
            _boundingBoxOfEverything = OoXmlHelper.GetAllCharacterExtents(_chemistryModel, _positionerOutputs);

            // Create Base OoXml Objects
            var run = CreateRun();

            // Render molecule Group Brackets
            if (_chemistryModel.ShowMoleculeGrouping)
            {
                foreach (var group in _positionerOutputs.GroupBrackets)
                {
                    var bracketColour = _chemistryModel.ShowColouredAtoms ? "00bbff" : OoXmlHelper.Black;
                    DrawGroupBrackets(group, _medianBondLength * 0.5, OoXmlHelper.AcsLineWidth * 2, bracketColour);
                }
            }

            // Render molecule brackets
            foreach (var moleculeBracket in _positionerOutputs.MoleculeBrackets)
            {
                DrawMoleculeBrackets(moleculeBracket, OoXmlHelper.AcsLineWidth, OoXmlHelper.Black);
            }

            // Render reaction arrows
            foreach (var scheme in _chemistryModel.ReactionSchemes.Values)
            {
                foreach (var reaction in scheme.Reactions.Values)
                {
                    switch (reaction.ReactionType)
                    {
                        case ReactionType.Normal:
                        case ReactionType.Blocked:
                        case ReactionType.Resonance:
                            DrawSingleLinedReactionArrow(reaction);
                            break;

                        case ReactionType.Retrosynthetic:
                            DrawRetrosyntheticArrow(reaction);
                            break;

                        case ReactionType.ReversibleBiasedReverse:
                        case ReactionType.ReversibleBiasedForward:
                        case ReactionType.Reversible:
                            DrawReversibleArrow(reaction);
                            break;
                    }
                }
            }

            // Render Diagnostic Markers
            if (_options.ShowMoleculeBoundingBoxes)
            {
                foreach (var item in _positionerOutputs.AllMoleculeExtents)
                {
                    DrawBox(item.AtomExtents, "ff0000", .25);
                    DrawBox(item.InternalCharacterExtents, "00ff00", .25);
                    DrawBox(item.ExternalCharacterExtents, "0000ff", .25);
                }

                DrawBox(_boundingBoxOfAllAtoms, "ff0000", .25);
                DrawBox(_boundingBoxOfEverything, OoXmlHelper.Black, .25);
            }

            if (_options.ShowHulls)
            {
                foreach (var hull in _positionerOutputs.ConvexHulls)
                {
                    var points = hull.Value.ToList();
                    DrawPolygon(points, true, "ff0000", 0.25);
                }
            }

            if (_options.ShowCharacterBoundingBoxes)
            {
                foreach (var atom in _chemistryModel.GetAllAtoms())
                {
                    var chars = _positionerOutputs.AtomLabelCharacters.FindAll(a => a.ParentAtom.Equals(atom.Path));
                    var atomCharsRect = Rect.Empty;
                    AddCharacterBoundingBoxes(chars, atomCharsRect);
                    if (!atomCharsRect.IsEmpty)
                    {
                        DrawBox(atomCharsRect, "ffa500", 0.5);
                    }

                    var reactionCharacters = _positionerOutputs.AtomLabelCharacters.FindAll(a => a.ParentAtom.StartsWith("/rs"));
                    atomCharsRect = Rect.Empty;
                    AddCharacterBoundingBoxes(reactionCharacters, atomCharsRect);

                    // Local Function
                    void AddCharacterBoundingBoxes(List<AtomLabelCharacter> atomLabelCharacters, Rect rect)
                    {
                        foreach (var alc in atomLabelCharacters)
                        {
                            var thisBoundingBox = new Rect(alc.Position,
                                                           new Size(OoXmlHelper.ScaleCsTtfToCml(alc.Character.Width, _medianBondLength),
                                                                    OoXmlHelper.ScaleCsTtfToCml(alc.Character.Height, _medianBondLength)));
                            if (alc.IsSmaller)
                            {
                                thisBoundingBox = new Rect(alc.Position,
                                                           new Size(
                                                               OoXmlHelper.ScaleCsTtfToCml(alc.Character.Width, _medianBondLength) *
                                                               OoXmlHelper.SubscriptScaleFactor,
                                                               OoXmlHelper.ScaleCsTtfToCml(alc.Character.Height, _medianBondLength) *
                                                               OoXmlHelper.SubscriptScaleFactor));
                            }

                            DrawBox(thisBoundingBox, "00ff00", 0.25);

                            rect.Union(thisBoundingBox);
                        }
                    }
                }
            }

            if (_options.ShowCharacterGroupBoundingBoxes)
            {
                foreach (var rectangle in _positionerOutputs.Diagnostics.Rectangles)
                {
                    DrawBox(rectangle.BoundingBox, rectangle.Colour, OoXmlHelper.AcsLineWidth / 2);
                }
            }

            var spotSize = _medianBondLength * OoXmlHelper.MultipleBondOffsetPercentage / 3;

            if (_options.ShowBondCrossingPoints)
            {
                foreach (var point in _positionerOutputs.CrossingPoints)
                {
                    var extents = new Rect(new Point(point.X - spotSize * 2, point.Y - spotSize * 2),
                                           new Point(point.X + spotSize * 2, point.Y + spotSize * 2));
                    DrawShape(extents, A.ShapeTypeValues.Ellipse, true, "ffa500");
                }
            }

            if (_options.ShowRingCentres)
            {
                foreach (var point in _positionerOutputs.RingCenters)
                {
                    var extents = new Rect(new Point(point.X - spotSize, point.Y - spotSize),
                                           new Point(point.X + spotSize, point.Y + spotSize));
                    DrawShape(extents, A.ShapeTypeValues.Ellipse, true, "00ff00");
                }

                // ToDo: [MAW] - Experimental code to add inner circles for aromatic rings
                foreach (var innerCircle in _positionerOutputs.InnerCircles)
                {
                    var smallerCircle = new InnerCircle
                    {
                        Centre = innerCircle.Centre
                    };
                    // Move all points towards centre
                    foreach (var point in innerCircle.Points)
                    {
                        var vector = smallerCircle.Centre - point;
                        var innerPoint = point + vector * OoXmlHelper.MultipleBondOffsetPercentage;
                        smallerCircle.Points.Add(innerPoint);

                        //Rect extents = new Rect(new Point(innerPoint.X - spotSize, innerPoint.Y - spotSize),
                        //                        new Point(innerPoint.X + spotSize, innerPoint.Y + spotSize))
                        //DrawShape(extents, A.ShapeTypeValues.Ellipse, false, "ff0000", 0.5)
                    }

                    DrawInnerCircle(smallerCircle, "00ff00", 0.5);
                    //DrawPolygon(smallerCircle.Points, "00ff00", 0.5)
                }
            }

            if (_options.ShowAtomPositions)
            {
                foreach (var atom in _chemistryModel.GetAllAtoms())
                {
                    var extents = new Rect(new Point(atom.Position.X - spotSize, atom.Position.Y - spotSize),
                                           new Point(atom.Position.X + spotSize, atom.Position.Y + spotSize));
                    DrawShape(extents, A.ShapeTypeValues.Ellipse, true, "ff0000");
                }

                foreach (var scheme in _chemistryModel.ReactionSchemes.Values)
                {
                    foreach (var reaction in scheme.Reactions.Values)
                    {
                        var head = new Rect(new Point(reaction.HeadPoint.X - spotSize, reaction.HeadPoint.Y - spotSize),
                                               new Point(reaction.HeadPoint.X + spotSize, reaction.HeadPoint.Y + spotSize));
                        DrawShape(head, A.ShapeTypeValues.Ellipse, true, "00ff00");

                        var tail = new Rect(new Point(reaction.TailPoint.X - spotSize, reaction.TailPoint.Y - spotSize),
                                               new Point(reaction.TailPoint.X + spotSize, reaction.TailPoint.Y + spotSize));
                        DrawShape(tail, A.ShapeTypeValues.Ellipse, true, "00ff00");
                    }
                }

                foreach (var annotation in _chemistryModel.Annotations.Values)
                {
                    var extents = new Rect(new Point(annotation.Position.X - spotSize, annotation.Position.Y - spotSize),
                                           new Point(annotation.Position.X + spotSize, annotation.Position.Y + spotSize));
                    DrawShape(extents, A.ShapeTypeValues.Ellipse, true, "0000ff");
                }
            }

            if (_options.ShowHulls)
            {
                foreach (var hull in _positionerOutputs.ConvexHulls)
                {
                    var points = hull.Value.ToList();
                    DrawPolygon(points, true, "ff0000", 0.25);
                }
            }

            BeautifyStereoBondLines();

            // Render Bond Lines
            foreach (var bondLine in _positionerOutputs.BondLines)
            {
                switch (bondLine.Style)
                {
                    case BondLineStyle.Wedge:
                        DrawFilledBond(bondLine.Outline, bondLine.BondPath, bondLine.Colour);
                        //_positionerOutputs.Diagnostics.Polygons.Add(bondLine.Outline);
                        break;

                    case BondLineStyle.Hatch:
                        DrawHatchBond(bondLine.Outline, bondLine.BondPath, bondLine.Colour);
                        //_positionerOutputs.Diagnostics.Polygons.Add(bondLine.Outline);
                        break;

                    case BondLineStyle.Thick:
                        DrawFilledBond(bondLine.Outline, bondLine.BondPath, bondLine.Colour);
                        //_positionerOutputs.Diagnostics.Polygons.Add(bondLine.Outline);
                        break;

                    default:
                        // These should be all other single stroke lines for bonds
                        DrawBondLine(bondLine.Start, bondLine.End, bondLine.BondPath, bondLine.Style, bondLine.Colour, bondLine.Width);
                        break;
                }
            }

            // Render Atom and Molecule Characters
            foreach (var character in _positionerOutputs.AtomLabelCharacters)
            {
                DrawCharacter(character);
            }

            // Any other general diagnostics

            // Finally draw any debugging diagnostics
            foreach (var line in _positionerOutputs.Diagnostics.Lines)
            {
                DrawBondLine(line.Start, line.End, "", line.Style, line.Colour, 0.5);
            }

            foreach (var polygon in _positionerOutputs.Diagnostics.Polygons)
            {
                DrawPolygon(polygon, true, "00ff00", 0.25);
            }

            foreach (var spot in _positionerOutputs.Diagnostics.Points)
            {
                var half = spot.Diameter / 2;
                var extents = new Rect(new Point(spot.Point.X - half, spot.Point.Y - half),
                                       new Point(spot.Point.X + half, spot.Point.Y + half));
                DrawShape(extents, A.ShapeTypeValues.Ellipse, true, spot.Colour);
            }

            _telemetry.Write(module, "Timing", $"Rendering {_chemistryModel.TotalMoleculesCount} molecules with {_chemistryModel.TotalAtomsCount} atoms and {_chemistryModel.TotalBondsCount} bonds took {SafeDouble.AsString0(swr.ElapsedMilliseconds)} ms; Average Bond Length: {SafeDouble.AsString(_chemistryModel.MeanBondLength)}");

            ShutDownProgress(progress);

            return run;
        }

        private void BeautifyStereoBondLines()
        {
            var thickBondLines = _positionerOutputs.BondLines.Where(t => t.Style == BondLineStyle.Thick).ToList();
            GenerateInitialOutlinePoints(thickBondLines);

            var wedgeOrHatchBondLines = _positionerOutputs.BondLines.Where(t => t.Style == BondLineStyle.Wedge || t.Style == BondLineStyle.Hatch).ToList();
            GenerateInitialOutlinePoints(wedgeOrHatchBondLines);

            BeautifyWedgeOrHatchBondLines(wedgeOrHatchBondLines);
            BeautifyThickBondLines(thickBondLines);
        }

        private void BeautifyWedgeOrHatchBondLines(List<BondLine> wedgeOrHatchBondLines)
        {
            foreach (var thisBondLine in wedgeOrHatchBondLines)
            {
                var otherBondLines = _positionerOutputs.BondLines
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

        private void BeautifyThickBondLines(List<BondLine> thickBondLines)
        {
            foreach (var thickBondLine in thickBondLines)
            {
                var wedgeBondLines = _positionerOutputs.BondLines
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

        private void GenerateInitialOutlinePoints(List<BondLine> bondLines)
        {
            foreach (var bondLine in bondLines)
            {
                bondLine.CalculateInitialOutlinePoints(_medianBondLength);
            }
        }

        private static bool IsLonger(Vector v1, Vector v2)
            => v1.Length > v2.Length;

        private void DrawReversibleArrow(Reaction reaction)
        {
            var simpleLine = new SimpleLine(reaction.TailPoint, reaction.HeadPoint);
            var reversibleTopLine = simpleLine.GetParallel(BondOffset() / 2);
            var reversibleBottomLine = simpleLine.GetParallel(-BondOffset() / 2);

            var p1 = reversibleTopLine.Start;
            var p2 = reversibleTopLine.End;

            var p3 = reversibleBottomLine.End;
            var p4 = reversibleBottomLine.Start;

            switch (reaction.ReactionType)
            {
                case ReactionType.ReversibleBiasedReverse:
                    GeometryTool.AdjustLineAboutMidpoint(ref p1, ref p2, -_medianBondLength / OoXmlHelper.LineShrinkPixels);
                    break;

                case ReactionType.ReversibleBiasedForward:
                    GeometryTool.AdjustLineAboutMidpoint(ref p3, ref p4, -_medianBondLength / OoXmlHelper.LineShrinkPixels);
                    break;
            }

            DrawPolygon(new List<Point> { p1, p2, BarbLocation(p1, p2) }, false, OoXmlHelper.Black, OoXmlHelper.AcsLineWidth);
            DrawPolygon(new List<Point> { p3, p4, BarbLocation(p3, p4) }, false, OoXmlHelper.Black, OoXmlHelper.AcsLineWidth);

            // Local Function
            Point BarbLocation(Point tailPoint, Point headPoint)
            {
                var barbVector = tailPoint - headPoint;
                barbVector.Normalize();
                barbVector *= BondOffset();

                var rotator = new Matrix();
                rotator.Rotate(45);
                barbVector *= rotator;

                var result = headPoint + barbVector;

                return result;
            }
        }

        private void DrawRetrosyntheticArrow(Reaction reaction)
        {
            var vector = reaction.HeadPoint - reaction.TailPoint;
            var perpendicular = vector.Perpendicular();
            perpendicular.Normalize();
            var offset = perpendicular * BondOffset() / 2;
            var topLeft = reaction.TailPoint - offset;
            var topRight = reaction.HeadPoint - offset;
            var bottomLeft = reaction.TailPoint + offset;
            var bottomRight = reaction.HeadPoint + offset;

            var barb = offset * 4;
            var rotator = new Matrix();
            rotator.Rotate(-45);
            var topHeadEnd = reaction.HeadPoint - barb * rotator;
            rotator.Rotate(+90);
            var bottomHeadEnd = reaction.HeadPoint + barb * rotator;

            var topCrossing = GeometryTool.GetIntersection(topLeft, topRight, reaction.HeadPoint, topHeadEnd);
            var bottomCrossing = GeometryTool.GetIntersection(bottomLeft, bottomRight, reaction.HeadPoint, bottomHeadEnd);

            if (topCrossing != null && bottomCrossing != null)
            {
                // Drawing as a polygon to avoid nasty crossing points at topCrossing and bottomCrossing
                var points = new List<Point>
                             {
                                 topLeft,
                                 topCrossing.Value,
                                 topHeadEnd,
                                 reaction.HeadPoint,
                                 bottomHeadEnd,
                                 bottomCrossing.Value,
                                 bottomLeft
                             };
                DrawPolygon(points, false, OoXmlHelper.Black, OoXmlHelper.AcsLineWidth);
            }
        }

        public void DrawCharacter(AtomLabelCharacter alc)
        {
            var characterPosition = new Point(alc.Position.X, alc.Position.Y);
            characterPosition.Offset(-_boundingBoxOfEverything.Left, -_boundingBoxOfEverything.Top);

            var emuWidth = OoXmlHelper.ScaleCsTtfToEmu(alc.Character.Width, _medianBondLength);
            var emuHeight = OoXmlHelper.ScaleCsTtfToEmu(alc.Character.Height, _medianBondLength);
            if (alc.IsSmaller)
            {
                emuWidth = OoXmlHelper.ScaleCsTtfSubScriptToEmu(alc.Character.Width, _medianBondLength);
                emuHeight = OoXmlHelper.ScaleCsTtfSubScriptToEmu(alc.Character.Height, _medianBondLength);
            }
            var emuTop = OoXmlHelper.ScaleCmlToEmu(characterPosition.Y);
            var emuLeft = OoXmlHelper.ScaleCmlToEmu(characterPosition.X);

            var parent = alc.ParentAtom.Equals(alc.ParentMolecule) ? alc.ParentMolecule : alc.ParentAtom;
            var shapeName = $"Character {alc.Character.Character} of {parent}";
            var wordprocessingShape = CreateShape(_ooxmlId++, shapeName);
            var shapeProperties = CreateShapeProperties(wordprocessingShape, emuTop, emuLeft, emuWidth, emuHeight);

            // Start of the lines

            var pathList = new A.PathList();

            var path = new A.Path { Width = emuWidth, Height = emuHeight };

            foreach (var contour in alc.Character.Contours)
            {
                var i = 0;

                while (i < contour.Points.Count)
                {
                    var thisPoint = contour.Points[i];
                    TtfPoint nextPoint = null;
                    if (i < contour.Points.Count - 1)
                    {
                        nextPoint = contour.Points[i + 1];
                    }

                    switch (thisPoint.Type)
                    {
                        case TtfPoint.PointType.Start:
                            var moveTo = new A.MoveTo();
                            if (alc.IsSmaller)
                            {
                                var point = MakeSubscriptPoint(thisPoint);
                                moveTo.Append(point);
                                path.Append(moveTo);
                            }
                            else
                            {
                                var point = MakeNormalPoint(thisPoint);
                                moveTo.Append(point);
                                path.Append(moveTo);
                            }
                            i++;
                            break;

                        case TtfPoint.PointType.Line:
                            var lineTo = new A.LineTo();
                            if (alc.IsSmaller)
                            {
                                var point = MakeSubscriptPoint(thisPoint);
                                lineTo.Append(point);
                                path.Append(lineTo);
                            }
                            else
                            {
                                var point = MakeNormalPoint(thisPoint);
                                lineTo.Append(point);
                                path.Append(lineTo);
                            }
                            i++;
                            break;

                        case TtfPoint.PointType.CurveOff:
                            var quadraticBezierCurveTo = new A.QuadraticBezierCurveTo();
                            if (alc.IsSmaller)
                            {
                                var pointA = MakeSubscriptPoint(thisPoint);
                                var pointB = MakeSubscriptPoint(nextPoint);
                                quadraticBezierCurveTo.Append(pointA);
                                quadraticBezierCurveTo.Append(pointB);
                                path.Append(quadraticBezierCurveTo);
                            }
                            else
                            {
                                var pointA = MakeNormalPoint(thisPoint);
                                var pointB = MakeNormalPoint(nextPoint);
                                quadraticBezierCurveTo.Append(pointA);
                                quadraticBezierCurveTo.Append(pointB);
                                path.Append(quadraticBezierCurveTo);
                            }
                            i++;
                            i++;
                            break;

                        case TtfPoint.PointType.CurveOn:
                            // Should never get here !
                            i++;
                            break;
                    }
                }

                var closeShapePath = new A.CloseShapePath();
                path.Append(closeShapePath);
            }

            pathList.Append(path);

            // End of the lines

            var solidFill = new A.SolidFill();

            // Set Colour
            var rgbColorModelHex = new A.RgbColorModelHex { Val = alc.Colour };
            solidFill.Append(rgbColorModelHex);

            shapeProperties.Append(CreateCustomGeometry(pathList));
            shapeProperties.Append(solidFill);

            wordprocessingShape.Append(CreateShapeStyle());

            var textBodyProperties = new Wps.TextBodyProperties();
            wordprocessingShape.Append(textBodyProperties);

            _wordprocessingGroup.Append(wordprocessingShape);

            // Local Functions
            A.Point MakeSubscriptPoint(TtfPoint ttfPoint)
            {
                var pp = new A.Point
                {
                    X = $"{OoXmlHelper.ScaleCsTtfSubScriptToEmu(ttfPoint.X - alc.Character.OriginX, _medianBondLength)}",
                    Y = $"{OoXmlHelper.ScaleCsTtfSubScriptToEmu(alc.Character.Height + ttfPoint.Y - (alc.Character.Height + alc.Character.OriginY), _medianBondLength)}"
                };
                return pp;
            }

            A.Point MakeNormalPoint(TtfPoint ttfPoint)
            {
                var pp = new A.Point
                {
                    X = $"{OoXmlHelper.ScaleCsTtfToEmu(ttfPoint.X - alc.Character.OriginX, _medianBondLength)}",
                    Y = $"{OoXmlHelper.ScaleCsTtfToEmu(alc.Character.Height + ttfPoint.Y - (alc.Character.Height + alc.Character.OriginY), _medianBondLength)}"
                };
                return pp;
            }
        }

        private void DrawBondLine(Point bondStart, Point bondEnd, string bondPath,
                                  BondLineStyle lineStyle = BondLineStyle.Solid,
                                  string colour = OoXmlHelper.Black,
                                  double lineWidth = OoXmlHelper.AcsLineWidth)
        {
            switch (lineStyle)
            {
                case BondLineStyle.Solid:
                case BondLineStyle.Zero:
                case BondLineStyle.Half:
                case BondLineStyle.Dotted: // Diagnostics
                case BondLineStyle.Dashed: // Diagnostics
                    DrawStraightLine(bondStart, bondEnd, bondPath, lineStyle, colour, lineWidth);
                    break;

                case BondLineStyle.Wavy:
                    DrawWavyLine(bondStart, bondEnd, bondPath, colour);
                    break;

                case BondLineStyle.Thick:
                    // Do Nothing this is implemented elsewhere
                    break;

                default:
                    // Diagnostic dotted green lines
                    DrawStraightLine(bondStart, bondEnd, bondPath, BondLineStyle.Zero, "00ff00", lineWidth);
                    break;
            }
        }

        private List<SimpleLine> CreateHatchLines(List<Point> points)
        {
            var lines = new List<SimpleLine>();

            var wedgeStart = points[0];
            var wedgeEndMiddle = points[2];

            // Vector pointing from wedgeStart to wedgeEndMiddle
            var direction = wedgeEndMiddle - wedgeStart;
            var rightAngles = new Matrix();
            rightAngles.Rotate(90);
            var perpendicular = direction * rightAngles;

            var step = direction;
            step.Normalize();
            step *= OoXmlHelper.ScaleCmlToEmu(15 * OoXmlHelper.MultipleBondOffsetPercentage);

            var steps = (int)Math.Ceiling(direction.Length / step.Length);
            var stepLength = direction.Length / steps;

            step.Normalize();
            step *= stepLength;

            var p0 = wedgeStart + step;
            var p1 = p0 + perpendicular;
            var p2 = p0 - perpendicular;

            var r = GeometryTool.ClipLineWithPolygon(p1, p2, points, out _);
            while (r.Length > 2)
            {
                if (r.Length == 4)
                {
                    lines.Add(new SimpleLine(r[1], r[2]));
                }

                if (r.Length == 6)
                {
                    lines.Add(new SimpleLine(r[1], r[2]));
                    lines.Add(new SimpleLine(r[3], r[4]));
                }

                p0 += step;
                p1 = p0 + perpendicular;
                p2 = p0 - perpendicular;

                r = GeometryTool.ClipLineWithPolygon(p1, p2, points, out _);
            }

            // Define Tail Lines
            lines.Add(new SimpleLine(wedgeEndMiddle, points[1]));
            lines.Add(new SimpleLine(wedgeEndMiddle, points[3]));

            return lines;
        }

        private void DrawShape(Rect cmlExtents, A.ShapeTypeValues shape, bool filled, string colour,
                               double outlineWidth = OoXmlHelper.AcsLineWidth)
        {
            var emuWidth = OoXmlHelper.ScaleCmlToEmu(cmlExtents.Width);
            var emuHeight = OoXmlHelper.ScaleCmlToEmu(cmlExtents.Height);
            var emuTop = OoXmlHelper.ScaleCmlToEmu(cmlExtents.Top);
            var emuLeft = OoXmlHelper.ScaleCmlToEmu(cmlExtents.Left);

            var location = new Point(emuLeft, emuTop);
            var size = new Size(emuWidth, emuHeight);
            location.Offset(OoXmlHelper.ScaleCmlToEmu(-_boundingBoxOfEverything.Left), OoXmlHelper.ScaleCmlToEmu(-_boundingBoxOfEverything.Top));
            var boundingBox = new Rect(location, size);

            emuWidth = (Int64Value)boundingBox.Width;
            emuHeight = (Int64Value)boundingBox.Height;
            emuTop = (Int64Value)boundingBox.Top;
            emuLeft = (Int64Value)boundingBox.Left;

            var id = UInt32Value.FromUInt32((uint)_ooxmlId++);
            var shapeName = "Shape" + id;
            var wordprocessingShape = CreateShape(id, shapeName);

            var shapeProperties = new Wps.ShapeProperties();

            var transform2D = new A.Transform2D();
            var offset = new A.Offset { X = emuLeft, Y = emuTop };
            var extents = new A.Extents { Cx = emuWidth, Cy = emuHeight };
            transform2D.Append(offset);
            transform2D.Append(extents);
            shapeProperties.Append(transform2D);

            var adjustValueList = new A.AdjustValueList();
            var presetGeometry = new A.PresetGeometry { Preset = shape };
            presetGeometry.Append(adjustValueList);
            shapeProperties.Append(presetGeometry);

            if (filled)
            {
                // Set shape fill colour
                var solidFill = new A.SolidFill();
                var rgbColorModelHex = new A.RgbColorModelHex { Val = colour };
                solidFill.Append(rgbColorModelHex);
                shapeProperties.Append(solidFill);
            }
            else
            {
                // Set shape outline and colour
                var emuLineWidth = (Int32Value)(outlineWidth * OoXmlHelper.EmusPerWordPoint);
                var outline = new A.Outline { Width = emuLineWidth, CapType = A.LineCapValues.Round };
                var rgbColorModelHex2 = new A.RgbColorModelHex { Val = colour };
                var outlineFill = new A.SolidFill();
                outlineFill.Append(rgbColorModelHex2);
                outline.Append(outlineFill);
                shapeProperties.Append(outline);
            }

            wordprocessingShape.Append(shapeProperties);
            wordprocessingShape.Append(CreateShapeStyle());

            var textBodyProperties = new Wps.TextBodyProperties();
            wordprocessingShape.Append(textBodyProperties);

            _wordprocessingGroup.Append(wordprocessingShape);
        }

        private void DrawHatchBond(List<Point> points, string bondPath,
                                   string colour = OoXmlHelper.Black)
        {
            var cmlExtents = new Rect(points[0], points[points.Count - 1]);

            for (var i = 0; i < points.Count - 1; i++)
            {
                cmlExtents.Union(new Rect(points[i], points[i + 1]));
            }

            // Move Extents to have 0,0 Top Left Reference
            cmlExtents.Offset(-_boundingBoxOfEverything.Left, -_boundingBoxOfEverything.Top);

            var emuTop = OoXmlHelper.ScaleCmlToEmu(cmlExtents.Top);
            var emuLeft = OoXmlHelper.ScaleCmlToEmu(cmlExtents.Left);
            var emuWidth = OoXmlHelper.ScaleCmlToEmu(cmlExtents.Width);
            var emuHeight = OoXmlHelper.ScaleCmlToEmu(cmlExtents.Height);

            var shapeName = "Hatch " + bondPath;

            var wordprocessingShape = CreateShape(_ooxmlId++, shapeName);
            var shapeProperties = CreateShapeProperties(wordprocessingShape, emuTop, emuLeft, emuWidth, emuHeight);

            // Start of the lines

            var pathList = new A.PathList();

            // Draw a small circle for the starting point
            var xx = 0.5;
            var extents = new Rect(new Point(points[0].X - xx, points[0].Y - xx), new Point(points[0].X + xx, points[0].Y + xx));
            DrawShape(extents, A.ShapeTypeValues.Ellipse, true, colour);

            // Pre offset and scale the extents
            var scaledPoints = new List<Point>();
            foreach (var point in points)
            {
                point.Offset(-_boundingBoxOfEverything.Left, -_boundingBoxOfEverything.Top);
                point.Offset(-cmlExtents.Left, -cmlExtents.Top);
                scaledPoints.Add(new Point(OoXmlHelper.ScaleCmlToEmu(point.X), OoXmlHelper.ScaleCmlToEmu(point.Y)));
            }

            var lines = CreateHatchLines(scaledPoints);

            foreach (var line in lines)
            {
                var path = new A.Path { Width = emuWidth, Height = emuHeight };

                var moveTo = new A.MoveTo();
                var startPoint = new A.Point
                {
                    X = line.Start.X.ToString("0"),
                    Y = line.Start.Y.ToString("0")
                };

                moveTo.Append(startPoint);
                path.Append(moveTo);

                var lineTo = new A.LineTo();
                var endPoint = new A.Point
                {
                    X = line.End.X.ToString("0"),
                    Y = line.End.Y.ToString("0")
                };
                lineTo.Append(endPoint);
                path.Append(lineTo);

                pathList.Append(path);
            }

            // End of the lines

            shapeProperties.Append(CreateCustomGeometry(pathList));

            // Set shape fill colour
            var insideFill = new A.SolidFill();
            var rgbColorModelHex = new A.RgbColorModelHex { Val = colour };
            insideFill.Append(rgbColorModelHex);

            shapeProperties.Append(insideFill);

            // Set shape outline colour
            var outline = new A.Outline { Width = Int32Value.FromInt32((int)OoXmlHelper.AcsLineWidthEmus), CapType = A.LineCapValues.Round };
            var rgbColorModelHex2 = new A.RgbColorModelHex { Val = colour };
            var outlineFill = new A.SolidFill();
            outlineFill.Append(rgbColorModelHex2);
            outline.Append(outlineFill);

            shapeProperties.Append(outline);

            wordprocessingShape.Append(CreateShapeStyle());

            var textBodyProperties = new Wps.TextBodyProperties();
            wordprocessingShape.Append(textBodyProperties);

            _wordprocessingGroup.Append(wordprocessingShape);
        }

        private A.Point MakePoint(Point pp, Rect cmlExtents)
        {
            pp.Offset(-_boundingBoxOfEverything.Left, -_boundingBoxOfEverything.Top);
            pp.Offset(-cmlExtents.Left, -cmlExtents.Top);
            return new A.Point
            {
                X = $"{OoXmlHelper.ScaleCmlToEmu(pp.X)}",
                Y = $"{OoXmlHelper.ScaleCmlToEmu(pp.Y)}"
            };
        }

        private void DrawFilledBond(List<Point> points, string bondPath,
                                   string colour = OoXmlHelper.Black)
        {
            var cmlExtents = new Rect(points[0], points[points.Count - 1]);

            for (var i = 0; i < points.Count - 1; i++)
            {
                cmlExtents.Union(new Rect(points[i], points[i + 1]));
            }

            // Move Extents to have 0,0 Top Left Reference
            cmlExtents.Offset(-_boundingBoxOfEverything.Left, -_boundingBoxOfEverything.Top);

            var emuTop = OoXmlHelper.ScaleCmlToEmu(cmlExtents.Top);
            var emuLeft = OoXmlHelper.ScaleCmlToEmu(cmlExtents.Left);
            var emuWidth = OoXmlHelper.ScaleCmlToEmu(cmlExtents.Width);
            var emuHeight = OoXmlHelper.ScaleCmlToEmu(cmlExtents.Height);

            var shapeName = "Wedge " + bondPath;

            var wordprocessingShape = CreateShape(_ooxmlId++, shapeName);
            var shapeProperties = CreateShapeProperties(wordprocessingShape, emuTop, emuLeft, emuWidth, emuHeight);

            // Start of the lines

            var pathList = new A.PathList();

            var path = new A.Path { Width = emuWidth, Height = emuHeight };

            var moveTo = new A.MoveTo();
            moveTo.Append(MakePoint(points[0], cmlExtents));
            path.Append(moveTo);

            for (var i = 1; i < points.Count; i++)
            {
                var lineTo = new A.LineTo();
                lineTo.Append(MakePoint(points[i], cmlExtents));
                path.Append(lineTo);
            }

            var closeShapePath = new A.CloseShapePath();
            path.Append(closeShapePath);

            pathList.Append(path);

            // End of the lines

            shapeProperties.Append(CreateCustomGeometry(pathList));

            // Set shape fill colour
            var insideFill = new A.SolidFill();
            var rgbColorModelHex = new A.RgbColorModelHex { Val = colour };
            insideFill.Append(rgbColorModelHex);

            shapeProperties.Append(insideFill);

            // Set shape outline colour
            var outline = new A.Outline { Width = Int32Value.FromInt32((int)OoXmlHelper.AcsLineWidthEmus), CapType = A.LineCapValues.Round };
            var rgbColorModelHex2 = new A.RgbColorModelHex { Val = colour };
            var outlineFill = new A.SolidFill();
            outlineFill.Append(rgbColorModelHex2);
            outline.Append(outlineFill);

            shapeProperties.Append(outline);

            wordprocessingShape.Append(CreateShapeStyle());

            var textBodyProperties = new Wps.TextBodyProperties();
            wordprocessingShape.Append(textBodyProperties);

            _wordprocessingGroup.Append(wordprocessingShape);
        }

        private void DrawMoleculeBrackets(Rect cmlExtents, double lineWidth, string lineColour)
        {
            var emuWidth = OoXmlHelper.ScaleCmlToEmu(cmlExtents.Width);
            var emuHeight = OoXmlHelper.ScaleCmlToEmu(cmlExtents.Height);
            var emuTop = OoXmlHelper.ScaleCmlToEmu(cmlExtents.Top);
            var emuLeft = OoXmlHelper.ScaleCmlToEmu(cmlExtents.Left);

            var location = new Point(emuLeft, emuTop);
            var size = new Size(emuWidth, emuHeight);
            location.Offset(OoXmlHelper.ScaleCmlToEmu(-_boundingBoxOfEverything.Left), OoXmlHelper.ScaleCmlToEmu(-_boundingBoxOfEverything.Top));
            var boundingBox = new Rect(location, size);

            emuWidth = (Int64Value)boundingBox.Width;
            emuHeight = (Int64Value)boundingBox.Height;
            emuTop = (Int64Value)boundingBox.Top;
            emuLeft = (Int64Value)boundingBox.Left;

            var shapeName = "Box " + _ooxmlId++;

            var wordprocessingShape = CreateShape(_ooxmlId, shapeName);
            var shapeProperties = CreateShapeProperties(wordprocessingShape, emuTop, emuLeft, emuWidth, emuHeight);

            // Start of the lines

            var pathList = new A.PathList();

            var gap = boundingBox.Width * 0.8;
            var leftSide = (emuWidth - gap) / 2;
            var rightSide = emuWidth - leftSide;

            // Left Path
            var path1 = new A.Path { Width = emuWidth, Height = emuHeight };

            var moveTo = new A.MoveTo();
            var point1 = new A.Point { X = leftSide.ToString("0"), Y = "0" };
            moveTo.Append(point1);

            // Mid Point
            var lineTo1 = new A.LineTo();
            var point2 = new A.Point { X = "0", Y = "0" };
            lineTo1.Append(point2);

            // Last Point
            var lineTo2 = new A.LineTo();
            var point3 = new A.Point { X = "0", Y = boundingBox.Height.ToString("0") };
            lineTo2.Append(point3);

            // Mid Point
            var lineTo3 = new A.LineTo();
            var point4 = new A.Point { X = leftSide.ToString("0"), Y = boundingBox.Height.ToString("0") };
            lineTo3.Append(point4);

            path1.Append(moveTo);
            path1.Append(lineTo1);
            path1.Append(lineTo2);
            path1.Append(lineTo3);

            pathList.Append(path1);

            // Right Path
            var path2 = new A.Path { Width = emuWidth, Height = emuHeight };

            var moveTo2 = new A.MoveTo();
            var point5 = new A.Point { X = rightSide.ToString("0"), Y = "0" };
            moveTo2.Append(point5);

            // Mid Point
            var lineTo4 = new A.LineTo();
            var point6 = new A.Point { X = boundingBox.Width.ToString("0"), Y = "0" };
            lineTo4.Append(point6);

            // Last Point
            var lineTo5 = new A.LineTo();
            var point7 = new A.Point { X = boundingBox.Width.ToString("0"), Y = boundingBox.Height.ToString("0") };
            lineTo5.Append(point7);

            // Mid Point
            var lineTo6 = new A.LineTo();
            var point8 = new A.Point { X = rightSide.ToString("0"), Y = boundingBox.Height.ToString("0") };
            lineTo6.Append(point8);

            path2.Append(moveTo2);
            path2.Append(lineTo4);
            path2.Append(lineTo5);
            path2.Append(lineTo6);

            pathList.Append(path2);

            // End of the lines

            shapeProperties.Append(CreateCustomGeometry(pathList));

            var emuLineWidth = (Int32Value)(lineWidth * OoXmlHelper.EmusPerWordPoint);
            var outline = new A.Outline { Width = emuLineWidth, CapType = A.LineCapValues.Round };

            var solidFill = new A.SolidFill();
            var rgbColorModelHex = new A.RgbColorModelHex { Val = lineColour };
            solidFill.Append(rgbColorModelHex);
            outline.Append(solidFill);

            shapeProperties.Append(outline);

            wordprocessingShape.Append(CreateShapeStyle());

            var textBodyProperties = new Wps.TextBodyProperties();
            wordprocessingShape.Append(textBodyProperties);

            _wordprocessingGroup.Append(wordprocessingShape);
        }

        private void DrawGroupBrackets(Rect cmlExtents, double armLength, double lineWidth, string lineColour)
        {
            if (cmlExtents != Rect.Empty)
            {
                var emuWidth = OoXmlHelper.ScaleCmlToEmu(cmlExtents.Width);
                var emuHeight = OoXmlHelper.ScaleCmlToEmu(cmlExtents.Height);
                var emuTop = OoXmlHelper.ScaleCmlToEmu(cmlExtents.Top);
                var emuLeft = OoXmlHelper.ScaleCmlToEmu(cmlExtents.Left);

                var location = new Point(emuLeft, emuTop);
                var size = new Size(emuWidth, emuHeight);
                location.Offset(OoXmlHelper.ScaleCmlToEmu(-_boundingBoxOfEverything.Left), OoXmlHelper.ScaleCmlToEmu(-_boundingBoxOfEverything.Top));
                var boundingBox = new Rect(location, size);
                var armLengthEmu = OoXmlHelper.ScaleCmlToEmu(armLength);

                emuWidth = (Int64Value)boundingBox.Width;
                emuHeight = (Int64Value)boundingBox.Height;
                emuTop = (Int64Value)boundingBox.Top;
                emuLeft = (Int64Value)boundingBox.Left;

                var shapeName = "Box " + _ooxmlId++;

                var wordprocessingShape = CreateShape(_ooxmlId, shapeName);
                var shapeProperties = CreateShapeProperties(wordprocessingShape, emuTop, emuLeft, emuWidth, emuHeight);

                // Start of the lines

                var pathList = new A.PathList();

                pathList.Append(MakeCorner(boundingBox, "TopLeft", armLengthEmu));
                pathList.Append(MakeCorner(boundingBox, "TopRight", armLengthEmu));
                pathList.Append(MakeCorner(boundingBox, "BottomLeft", armLengthEmu));
                pathList.Append(MakeCorner(boundingBox, "BottomRight", armLengthEmu));

                // End of the lines

                shapeProperties.Append(CreateCustomGeometry(pathList));

                var emuLineWidth = (Int32Value)(lineWidth * OoXmlHelper.EmusPerWordPoint);
                var outline = new A.Outline { Width = emuLineWidth, CapType = A.LineCapValues.Round };

                var solidFill = new A.SolidFill();
                var rgbColorModelHex = new A.RgbColorModelHex { Val = lineColour };
                solidFill.Append(rgbColorModelHex);
                outline.Append(solidFill);

                shapeProperties.Append(outline);

                wordprocessingShape.Append(CreateShapeStyle());

                var textBodyProperties = new Wps.TextBodyProperties();
                wordprocessingShape.Append(textBodyProperties);

                _wordprocessingGroup.Append(wordprocessingShape);

                // Local function
                A.Path MakeCorner(Rect bbRect, string corner, double armsSize)
                {
                    var path = new A.Path { Width = (Int64Value)bbRect.Width, Height = (Int64Value)bbRect.Height };

                    var p0 = new A.Point();
                    var p1 = new A.Point();
                    var p2 = new A.Point();

                    switch (corner)
                    {
                        case "TopLeft":
                            p0 = new A.Point
                            {
                                X = armsSize.ToString("0"),
                                Y = "0"
                            };
                            p1 = new A.Point
                            {
                                X = "0",
                                Y = "0"
                            };
                            p2 = new A.Point
                            {
                                X = "0",
                                Y = armsSize.ToString("0")
                            };
                            break;

                        case "TopRight":
                            p0 = new A.Point
                            {
                                X = (bbRect.Width - armsSize).ToString("0"),
                                Y = "0"
                            };
                            p1 = new A.Point
                            {
                                X = bbRect.Width.ToString("0"),
                                Y = "0"
                            };
                            p2 = new A.Point
                            {
                                X = bbRect.Width.ToString("0"),
                                Y = armsSize.ToString("0")
                            };
                            break;

                        case "BottomLeft":
                            p0 = new A.Point
                            {
                                X = "0",
                                Y = (bbRect.Height - armsSize).ToString("0")
                            };
                            p1 = new A.Point
                            {
                                X = "0",
                                Y = bbRect.Height.ToString("0")
                            };
                            p2 = new A.Point
                            {
                                X = armsSize.ToString("0"),
                                Y = bbRect.Height.ToString("0")
                            };
                            break;

                        case "BottomRight":
                            p0 = new A.Point
                            {
                                X = bbRect.Width.ToString("0"),
                                Y = (bbRect.Height - armsSize).ToString("0")
                            };
                            p1 = new A.Point
                            {
                                X = bbRect.Width.ToString("0"),
                                Y = bbRect.Height.ToString("0")
                            };
                            p2 = new A.Point
                            {
                                X = (bbRect.Width - armsSize).ToString("0"),
                                Y = bbRect.Height.ToString("0")
                            };
                            break;
                    }

                    var moveTo = new A.MoveTo();
                    moveTo.Append(p0);
                    path.Append(moveTo);

                    var lineTo1 = new A.LineTo();
                    lineTo1.Append(p1);
                    path.Append(lineTo1);

                    var lineTo2 = new A.LineTo();
                    lineTo2.Append(p2);
                    path.Append(lineTo2);

                    return path;
                }
            }
        }

        private void DrawBox(Rect cmlExtents,
                             string lineColour = OoXmlHelper.Black,
                             double lineWidth = OoXmlHelper.AcsLineWidth)
        {
            if (cmlExtents != Rect.Empty)
            {
                var emuWidth = OoXmlHelper.ScaleCmlToEmu(cmlExtents.Width);
                var emuHeight = OoXmlHelper.ScaleCmlToEmu(cmlExtents.Height);
                var emuTop = OoXmlHelper.ScaleCmlToEmu(cmlExtents.Top);
                var emuLeft = OoXmlHelper.ScaleCmlToEmu(cmlExtents.Left);

                var location = new Point(emuLeft, emuTop);
                var size = new Size(emuWidth, emuHeight);
                location.Offset(OoXmlHelper.ScaleCmlToEmu(-_boundingBoxOfEverything.Left), OoXmlHelper.ScaleCmlToEmu(-_boundingBoxOfEverything.Top));
                var boundingBox = new Rect(location, size);

                emuWidth = (Int64Value)boundingBox.Width;
                emuHeight = (Int64Value)boundingBox.Height;
                emuTop = (Int64Value)boundingBox.Top;
                emuLeft = (Int64Value)boundingBox.Left;

                var shapeName = "Box " + _ooxmlId++;

                var wordprocessingShape = CreateShape(_ooxmlId, shapeName);
                var shapeProperties = CreateShapeProperties(wordprocessingShape, emuTop, emuLeft, emuWidth, emuHeight);

                // Start of the lines

                var pathList = new A.PathList();

                var path = new A.Path { Width = emuWidth, Height = emuHeight };

                // Starting Point
                var moveTo = new A.MoveTo();
                var point1 = new A.Point { X = "0", Y = "0" };
                moveTo.Append(point1);

                // Mid Point
                var lineTo1 = new A.LineTo();
                var point2 = new A.Point { X = boundingBox.Width.ToString("0"), Y = "0" };
                lineTo1.Append(point2);

                // Mid Point
                var lineTo2 = new A.LineTo();
                var point3 = new A.Point { X = boundingBox.Width.ToString("0"), Y = boundingBox.Height.ToString("0") };
                lineTo2.Append(point3);

                // Last Point
                var lineTo3 = new A.LineTo();
                var point4 = new A.Point { X = "0", Y = boundingBox.Height.ToString("0") };
                lineTo3.Append(point4);

                // Back to Start Point
                var lineTo4 = new A.LineTo();
                var point5 = new A.Point { X = "0", Y = "0" };
                lineTo4.Append(point5);

                path.Append(moveTo);
                path.Append(lineTo1);
                path.Append(lineTo2);
                path.Append(lineTo3);
                path.Append(lineTo4);

                pathList.Append(path);

                // End of the lines

                shapeProperties.Append(CreateCustomGeometry(pathList));

                var emuLineWidth = (Int32Value)(lineWidth * OoXmlHelper.EmusPerWordPoint);
                var outline = new A.Outline { Width = emuLineWidth, CapType = A.LineCapValues.Round };

                var solidFill = new A.SolidFill();
                var rgbColorModelHex = new A.RgbColorModelHex { Val = lineColour };
                solidFill.Append(rgbColorModelHex);
                outline.Append(solidFill);

                shapeProperties.Append(outline);

                wordprocessingShape.Append(CreateShapeStyle());

                var textBodyProperties = new Wps.TextBodyProperties();
                wordprocessingShape.Append(textBodyProperties);

                _wordprocessingGroup.Append(wordprocessingShape);
            }
        }

        private void DrawInnerCircle(InnerCircle innerCircle, string lineColour, double lineWidth)
        {
            var cmlExtents = new Rect(innerCircle.Points[0], innerCircle.Points[innerCircle.Points.Count - 1]);

            for (var i = 0; i < innerCircle.Points.Count - 1; i++)
            {
                cmlExtents.Union(new Rect(innerCircle.Points[i], innerCircle.Points[i + 1]));
            }

            // Move Extents to have 0,0 Top Left Reference
            cmlExtents.Offset(-_boundingBoxOfEverything.Left, -_boundingBoxOfEverything.Top);

            var emuTop = OoXmlHelper.ScaleCmlToEmu(cmlExtents.Top);
            var emuLeft = OoXmlHelper.ScaleCmlToEmu(cmlExtents.Left);
            var emuWidth = OoXmlHelper.ScaleCmlToEmu(cmlExtents.Width);
            var emuHeight = OoXmlHelper.ScaleCmlToEmu(cmlExtents.Height);

            var id = _ooxmlId++;
            var shapeName = "InnerCircle " + id;

            var allpoints = new List<Point>();

            var startingPoint = GeometryTool.GetMidPoint(innerCircle.Points[innerCircle.Points.Count - 1], innerCircle.Points[0]);
            var leftPoint = startingPoint;
            var middlePoint = innerCircle.Points[0];
            var rightPoint = GeometryTool.GetMidPoint(innerCircle.Points[0], innerCircle.Points[1]);
            allpoints.Add(leftPoint);
            allpoints.Add(middlePoint);
            allpoints.Add(rightPoint);

            for (var i = 1; i < innerCircle.Points.Count - 1; i++)
            {
                leftPoint = GeometryTool.GetMidPoint(innerCircle.Points[i - 1], innerCircle.Points[i]);
                middlePoint = innerCircle.Points[i];
                rightPoint = GeometryTool.GetMidPoint(innerCircle.Points[i], innerCircle.Points[i + 1]);

                allpoints.Add(leftPoint);
                allpoints.Add(middlePoint);
                allpoints.Add(rightPoint);
            }

            leftPoint = GeometryTool.GetMidPoint(innerCircle.Points[innerCircle.Points.Count - 2], innerCircle.Points[innerCircle.Points.Count - 1]);
            middlePoint = innerCircle.Points[innerCircle.Points.Count - 1];
            rightPoint = GeometryTool.GetMidPoint(innerCircle.Points[innerCircle.Points.Count - 1], innerCircle.Points[0]);
            allpoints.Add(leftPoint);
            allpoints.Add(middlePoint);
            allpoints.Add(rightPoint);

            var wordprocessingShape = CreateShape(id, shapeName);
            var shapeProperties = CreateShapeProperties(wordprocessingShape, emuTop, emuLeft, emuWidth, emuHeight);

            // Start of the lines

            var pathList = new A.PathList();

            var path = new A.Path { Width = emuWidth, Height = emuHeight };

            // First point
            var moveTo = new A.MoveTo();
            moveTo.Append(MakePoint(startingPoint, cmlExtents));
            path.Append(moveTo);

            // Create the Curved Lines
            for (var i = 0; i < allpoints.Count; i += 3)
            {
                var cubicBezierCurveTo = new A.CubicBezierCurveTo();

                for (var j = 0; j < 3; j++)
                {
                    var curvePoint = MakePoint(allpoints[i + j], cmlExtents);
                    cubicBezierCurveTo.Append(curvePoint);
                }
                path.Append(cubicBezierCurveTo);
            }

            pathList.Append(path);

            // End of the lines

            var emuLineWidth = (Int32Value)(lineWidth * OoXmlHelper.EmusPerWordPoint);
            var outline = new A.Outline { Width = emuLineWidth, CapType = A.LineCapValues.Round };

            var solidFill = new A.SolidFill();
            var rgbColorModelHex = new A.RgbColorModelHex { Val = lineColour };
            solidFill.Append(rgbColorModelHex);
            outline.Append(solidFill);

            shapeProperties.Append(CreateCustomGeometry(pathList));
            shapeProperties.Append(outline);

            wordprocessingShape.Append(CreateShapeStyle());

            var textBodyProperties = new Wps.TextBodyProperties();
            wordprocessingShape.Append(textBodyProperties);

            _wordprocessingGroup.Append(wordprocessingShape);
        }

        private void DrawPolygon(List<Point> points, bool isClosed, string lineColour, double lineWidth)
        {
            var cmlExtents = new Rect(points[0], points[points.Count - 1]);

            for (var i = 0; i < points.Count - 1; i++)
            {
                cmlExtents.Union(new Rect(points[i], points[i + 1]));
            }

            // Move Extents to have 0,0 Top Left Reference
            cmlExtents.Offset(-_boundingBoxOfEverything.Left, -_boundingBoxOfEverything.Top);

            var emuTop = OoXmlHelper.ScaleCmlToEmu(cmlExtents.Top);
            var emuLeft = OoXmlHelper.ScaleCmlToEmu(cmlExtents.Left);
            var emuWidth = OoXmlHelper.ScaleCmlToEmu(cmlExtents.Width);
            var emuHeight = OoXmlHelper.ScaleCmlToEmu(cmlExtents.Height);

            var id = _ooxmlId++;
            var shapeName = "Polygon " + id;

            var wordprocessingShape = CreateShape(id, shapeName);
            var shapeProperties = CreateShapeProperties(wordprocessingShape, emuTop, emuLeft, emuWidth, emuHeight);

            // Start of the lines

            var pathList = new A.PathList();

            var path = new A.Path { Width = emuWidth, Height = emuHeight };

            // First point
            var moveTo = new A.MoveTo();
            moveTo.Append(MakePoint(points[0], cmlExtents));
            path.Append(moveTo);

            // Remaining points
            for (var i = 1; i < points.Count; i++)
            {
                var lineTo = new A.LineTo();
                lineTo.Append(MakePoint(points[i], cmlExtents));
                path.Append(lineTo);
            }

            if (isClosed)
            {
                // Close the path
                var closeShapePath = new A.CloseShapePath();
                path.Append(closeShapePath);
            }

            pathList.Append(path);

            // End of the lines

            var emuLineWidth = (Int32Value)(lineWidth * OoXmlHelper.EmusPerWordPoint);
            var outline = new A.Outline { Width = emuLineWidth, CapType = A.LineCapValues.Round };

            var solidFill = new A.SolidFill();
            var rgbColorModelHex = new A.RgbColorModelHex { Val = lineColour };
            solidFill.Append(rgbColorModelHex);
            outline.Append(solidFill);

            shapeProperties.Append(CreateCustomGeometry(pathList));
            shapeProperties.Append(outline);

            wordprocessingShape.Append(CreateShapeStyle());

            var textBodyProperties = new Wps.TextBodyProperties();
            wordprocessingShape.Append(textBodyProperties);

            _wordprocessingGroup.Append(wordprocessingShape);
        }

        private void DrawSingleLinedReactionArrow(Reaction reaction)
        {
            var lineStart = reaction.TailPoint;
            var lineEnd = reaction.HeadPoint;

            var tuple = OffsetPoints(lineStart, lineEnd);
            var cmlStartPoint = tuple.Start;
            var cmlEndPoint = tuple.End;
            var cmlLineExtents = tuple.Extents;

            var emuTop = OoXmlHelper.ScaleCmlToEmu(cmlLineExtents.Top);
            var emuLeft = OoXmlHelper.ScaleCmlToEmu(cmlLineExtents.Left);
            var emuWidth = OoXmlHelper.ScaleCmlToEmu(cmlLineExtents.Width);
            var emuHeight = OoXmlHelper.ScaleCmlToEmu(cmlLineExtents.Height);

            var id = _ooxmlId++;
            var suffix = string.IsNullOrEmpty(reaction.Path) ? id.ToString() : reaction.Path;
            var shapeName = "Reaction Arrow " + suffix;

            var wordprocessingShape = CreateShape(id, shapeName);
            var shapeProperties = CreateShapeProperties(wordprocessingShape, emuTop, emuLeft, emuWidth, emuHeight);

            // Add the line
            var pathList = new A.PathList();

            var path = new A.Path { Width = emuWidth, Height = emuHeight };

            var moveTo = new A.MoveTo();
            var point1 = new A.Point { X = OoXmlHelper.ScaleCmlToEmu(cmlEndPoint.X).ToString(), Y = OoXmlHelper.ScaleCmlToEmu(cmlEndPoint.Y).ToString() };
            moveTo.Append(point1);
            path.Append(moveTo);

            var lineTo = new A.LineTo();
            var point2 = new A.Point { X = OoXmlHelper.ScaleCmlToEmu(cmlStartPoint.X).ToString(), Y = OoXmlHelper.ScaleCmlToEmu(cmlStartPoint.Y).ToString() };
            lineTo.Append(point2);
            path.Append(lineTo);

            pathList.Append(path);

            var emuLineWidth = (Int32Value)(OoXmlHelper.AcsLineWidth * OoXmlHelper.EmusPerWordPoint);
            var outline = new A.Outline { Width = emuLineWidth, CapType = A.LineCapValues.Round };

            var solidFill = new A.SolidFill();
            var rgbColorModelHex = new A.RgbColorModelHex { Val = OoXmlHelper.Black };
            solidFill.Append(rgbColorModelHex);
            outline.Append(solidFill);

            // Add Arrow Head
            var headEnd = new A.HeadEnd
            {
                Type = A.LineEndValues.Triangle,
                Width = A.LineEndWidthValues.Small,
                Length = A.LineEndLengthValues.Small
            };
            outline.Append(headEnd);

            if (reaction.ReactionType == ReactionType.Resonance)
            {
                var tailEnd = new A.TailEnd
                {
                    Type = A.LineEndValues.Triangle,
                    Width = A.LineEndWidthValues.Small,
                    Length = A.LineEndLengthValues.Small
                };
                outline.Append(tailEnd);
            }

            // Add the cross if required
            if (reaction.ReactionType == ReactionType.Blocked)
            {
                var shaftVector = cmlEndPoint - cmlStartPoint;
                var midpoint = cmlStartPoint + shaftVector * 0.5;

                var crossArmLength = OoXmlHelper.MultipleBondOffsetPercentage * _chemistryModel.MeanBondLength;
                var points = new Point[4];

                var rotator = new Matrix();
                rotator.Rotate(-45);
                var shaftUnit = shaftVector;
                shaftUnit.Normalize();

                for (var i = 0; i < 4; i++)
                {
                    rotator.Rotate(90);
                    points[i] = midpoint + shaftUnit * crossArmLength * rotator;
                }

                AddCrossLine(points[0], points[2]);
                AddCrossLine(points[1], points[3]);
            }

            shapeProperties.Append(CreateCustomGeometry(pathList));
            shapeProperties.Append(outline);

            wordprocessingShape.Append(CreateShapeStyle());

            var textBodyProperties = new Wps.TextBodyProperties();
            wordprocessingShape.Append(textBodyProperties);

            _wordprocessingGroup.Append(wordprocessingShape);

            // Local Function
            void AddCrossLine(Point startPoint, Point endPoint)
            {
                var path1 = new A.Path { Width = emuWidth, Height = emuHeight };

                var moveTo2 = new A.MoveTo();
                var point3 = new A.Point { X = OoXmlHelper.ScaleCmlToEmu(endPoint.X).ToString(), Y = OoXmlHelper.ScaleCmlToEmu(endPoint.Y).ToString() };
                moveTo2.Append(point3);
                path1.Append(moveTo2);

                var lineTo2 = new A.LineTo();
                var point4 = new A.Point { X = OoXmlHelper.ScaleCmlToEmu(startPoint.X).ToString(), Y = OoXmlHelper.ScaleCmlToEmu(startPoint.Y).ToString() };
                lineTo2.Append(point4);
                path1.Append(lineTo2);
                pathList.Append(path1);
            }
        }

        private void DrawStraightLine(Point bondStart, Point bondEnd, string bondPath,
                                      BondLineStyle lineStyle, string lineColour, double lineWidth)
        {
            var tuple = OffsetPoints(bondStart, bondEnd);
            var cmlStartPoint = tuple.Start;
            var cmlEndPoint = tuple.End;
            var cmlLineExtents = tuple.Extents;

            var emuTop = OoXmlHelper.ScaleCmlToEmu(cmlLineExtents.Top);
            var emuLeft = OoXmlHelper.ScaleCmlToEmu(cmlLineExtents.Left);
            var emuWidth = OoXmlHelper.ScaleCmlToEmu(cmlLineExtents.Width);
            var emuHeight = OoXmlHelper.ScaleCmlToEmu(cmlLineExtents.Height);

            var id = _ooxmlId++;
            var suffix = string.IsNullOrEmpty(bondPath) ? id.ToString() : bondPath;
            var shapeName = "Straight Line " + suffix;

            var wordprocessingShape = CreateShape(id, shapeName);
            var shapeProperties = CreateShapeProperties(wordprocessingShape, emuTop, emuLeft, emuWidth, emuHeight);

            // Start of the lines

            var pathList = new A.PathList();

            var path = new A.Path { Width = emuWidth, Height = emuHeight };

            var moveTo = new A.MoveTo();
            var point1 = new A.Point { X = OoXmlHelper.ScaleCmlToEmu(cmlStartPoint.X).ToString(), Y = OoXmlHelper.ScaleCmlToEmu(cmlStartPoint.Y).ToString() };
            moveTo.Append(point1);
            path.Append(moveTo);

            var lineTo = new A.LineTo();
            var point2 = new A.Point { X = OoXmlHelper.ScaleCmlToEmu(cmlEndPoint.X).ToString(), Y = OoXmlHelper.ScaleCmlToEmu(cmlEndPoint.Y).ToString() };
            lineTo.Append(point2);
            path.Append(lineTo);

            pathList.Append(path);

            // End of the lines

            var emuLineWidth = (Int32Value)(lineWidth * OoXmlHelper.EmusPerWordPoint);
            var outline = new A.Outline { Width = emuLineWidth, CapType = A.LineCapValues.Round };

            var solidFill = new A.SolidFill();
            var rgbColorModelHex = new A.RgbColorModelHex { Val = lineColour };
            solidFill.Append(rgbColorModelHex);
            outline.Append(solidFill);

            switch (lineStyle)
            {
                case BondLineStyle.Zero:
                    outline.Append(new A.PresetDash { Val = A.PresetLineDashValues.SystemDot });
                    break;

                case BondLineStyle.Half:
                    outline.Append(new A.PresetDash { Val = A.PresetLineDashValues.SystemDash });
                    break;

                case BondLineStyle.Dotted: // Diagnostics
                    outline.Append(new A.PresetDash { Val = A.PresetLineDashValues.Dot });
                    break;

                case BondLineStyle.Dashed: // Diagnostics
                    outline.Append(new A.PresetDash { Val = A.PresetLineDashValues.Dash });
                    break;
            }

            if (!string.IsNullOrEmpty(bondPath) && _options.ShowBondDirection)
            {
                var tailEnd = new A.TailEnd
                {
                    Type = A.LineEndValues.Arrow,
                    Width = A.LineEndWidthValues.Small,
                    Length = A.LineEndLengthValues.Small
                };
                outline.Append(tailEnd);
            }

            shapeProperties.Append(CreateCustomGeometry(pathList));
            shapeProperties.Append(outline);

            wordprocessingShape.Append(CreateShapeStyle());

            var textBodyProperties = new Wps.TextBodyProperties();
            wordprocessingShape.Append(textBodyProperties);

            _wordprocessingGroup.Append(wordprocessingShape);
        }

        private void DrawWavyLine(Point bondStart, Point bondEnd, string bondPath, string lineColour)
        {
            var tuple = OffsetPoints(bondStart, bondEnd);
            var cmlStartPoint = tuple.Start;
            var cmlEndPoint = tuple.End;
            var cmlLineExtents = tuple.Extents;

            // Calculate wiggles

            var bondVector = cmlEndPoint - cmlStartPoint;
            var noOfWiggles = (int)Math.Ceiling(bondVector.Length / BondOffset());
            if (noOfWiggles < 1)
            {
                noOfWiggles = 1;
            }

            var wiggleLength = bondVector.Length / noOfWiggles;
            Debug.WriteLine($"v.Length: {bondVector.Length} noOfWiggles: {noOfWiggles}");

            var originalWigglePortion = bondVector;
            originalWigglePortion.Normalize();
            originalWigglePortion *= wiggleLength / 2;

            var toLeft = new Matrix();
            toLeft.Rotate(-60);
            var toRight = new Matrix();
            toRight.Rotate(60);
            var leftVector = originalWigglePortion * toLeft;
            var rightVector = originalWigglePortion * toRight;

            var allpoints = new List<Point>();

            var lastPoint = cmlStartPoint;

            for (var i = 0; i < noOfWiggles; i++)
            {
                // Left
                allpoints.Add(lastPoint);
                var leftPoint = lastPoint + leftVector;
                allpoints.Add(leftPoint);
                var midPoint = lastPoint + originalWigglePortion;
                allpoints.Add(midPoint);

                // Right
                allpoints.Add(midPoint);
                var rightPoint = lastPoint + originalWigglePortion + rightVector;
                allpoints.Add(rightPoint);
                lastPoint += originalWigglePortion * 2;
                allpoints.Add(lastPoint);
            }

            var minX = double.MaxValue;
            var maxX = double.MinValue;
            var minY = double.MaxValue;
            var maxY = double.MinValue;

            foreach (var p in allpoints)
            {
                maxX = Math.Max(p.X + cmlLineExtents.Left, maxX);
                minX = Math.Min(p.X + cmlLineExtents.Left, minX);
                maxY = Math.Max(p.Y + cmlLineExtents.Top, maxY);
                minY = Math.Min(p.Y + cmlLineExtents.Top, minY);
            }

            var newExtents = new Rect(minX, minY, maxX - minX, maxY - minY);
            var xOffset = cmlLineExtents.Left - newExtents.Left;
            var yOffset = cmlLineExtents.Top - newExtents.Top;

            var emuTop = OoXmlHelper.ScaleCmlToEmu(newExtents.Top);
            var emuLeft = OoXmlHelper.ScaleCmlToEmu(newExtents.Left);
            var emuWidth = OoXmlHelper.ScaleCmlToEmu(newExtents.Width);
            var emuHeight = OoXmlHelper.ScaleCmlToEmu(newExtents.Height);

            var shapeName = "Wavy Line " + bondPath;

            var wordprocessingShape = CreateShape(_ooxmlId++, shapeName);
            var shapeProperties = CreateShapeProperties(wordprocessingShape, emuTop, emuLeft, emuWidth, emuHeight);

            // Start of the lines

            var pathList = new A.PathList();

            var path = new A.Path { Width = emuWidth, Height = emuHeight };

            var moveTo = new A.MoveTo();
            var firstPoint = new A.Point { X = OoXmlHelper.ScaleCmlToEmu(cmlStartPoint.X + xOffset).ToString(), Y = OoXmlHelper.ScaleCmlToEmu(cmlStartPoint.Y + yOffset).ToString() };
            moveTo.Append(firstPoint);
            path.Append(moveTo);

            // Create the Curved Lines
            for (var i = 0; i < allpoints.Count; i += 3)
            {
                var cubicBezierCurveTo = new A.CubicBezierCurveTo();

                for (var j = 0; j < 3; j++)
                {
                    var nextPoint = new A.Point { X = OoXmlHelper.ScaleCmlToEmu(allpoints[i + j].X + xOffset).ToString(), Y = OoXmlHelper.ScaleCmlToEmu(allpoints[i + j].Y + yOffset).ToString() };
                    cubicBezierCurveTo.Append(nextPoint);
                }
                path.Append(cubicBezierCurveTo);
            }

            pathList.Append(path);

            // End of the lines

            var lineWidth = OoXmlHelper.AcsLineWidth;
            var emuLineWidth = (Int32Value)(lineWidth * OoXmlHelper.EmusPerWordPoint);
            var outline = new A.Outline { Width = emuLineWidth, CapType = A.LineCapValues.Round };

            var solidFill = new A.SolidFill();
            var rgbColorModelHex = new A.RgbColorModelHex { Val = lineColour };
            solidFill.Append(rgbColorModelHex);
            outline.Append(solidFill);

            if (_options.ShowBondDirection)
            {
                var tailEnd = new A.TailEnd { Type = A.LineEndValues.Stealth };
                outline.Append(tailEnd);
            }

            shapeProperties.Append(CreateCustomGeometry(pathList));
            shapeProperties.Append(outline);

            wordprocessingShape.Append(CreateShapeStyle());

            var textBodyProperties = new Wps.TextBodyProperties();
            wordprocessingShape.Append(textBodyProperties);

            _wordprocessingGroup.Append(wordprocessingShape);
        }

        private Wps.WordprocessingShape CreateShape(long id, string shapeName)
        {
            var id32 = UInt32Value.FromUInt32((uint)id);
            var wordprocessingShape = new Wps.WordprocessingShape();
            var nonVisualDrawingProperties = new Wps.NonVisualDrawingProperties { Id = id32, Name = shapeName };
            var nonVisualDrawingShapeProperties = new Wps.NonVisualDrawingShapeProperties();

            wordprocessingShape.Append(nonVisualDrawingProperties);
            wordprocessingShape.Append(nonVisualDrawingShapeProperties);

            return wordprocessingShape;
        }

        private Wps.ShapeProperties CreateShapeProperties(Wps.WordprocessingShape wordprocessingShape,
                                                          Int64Value emuTop, Int64Value emuLeft, Int64Value emuWidth, Int64Value emuHeight)
        {
            var shapeProperties = new Wps.ShapeProperties();

            wordprocessingShape.Append(shapeProperties);

            var transform2D = new A.Transform2D();
            var offset = new A.Offset { X = emuLeft, Y = emuTop };
            var extents = new A.Extents { Cx = emuWidth, Cy = emuHeight };
            transform2D.Append(offset);
            transform2D.Append(extents);
            shapeProperties.Append(transform2D);

            return shapeProperties;
        }

        private Wps.ShapeStyle CreateShapeStyle()
        {
            var shapeStyle = new Wps.ShapeStyle();
            var lineReference = new A.LineReference { Index = (UInt32Value)0U };
            var fillReference = new A.FillReference { Index = (UInt32Value)0U };
            var effectReference = new A.EffectReference { Index = (UInt32Value)0U };
            var fontReference = new A.FontReference { Index = A.FontCollectionIndexValues.Minor };

            shapeStyle.Append(lineReference);
            shapeStyle.Append(fillReference);
            shapeStyle.Append(effectReference);
            shapeStyle.Append(fontReference);

            return shapeStyle;
        }

        private A.CustomGeometry CreateCustomGeometry(A.PathList pathList)
        {
            var customGeometry = new A.CustomGeometry();
            var adjustValueList = new A.AdjustValueList();
            var rectangle = new A.Rectangle { Left = "l", Top = "t", Right = "r", Bottom = "b" };
            customGeometry.Append(adjustValueList);
            customGeometry.Append(rectangle);
            customGeometry.Append(pathList);
            return customGeometry;
        }

        private Run CreateRun()
        {
            var run = new Run();

            var drawing = new Drawing();
            run.Append(drawing);

            var inline = new Wp.Inline
            {
                DistanceFromTop = (UInt32Value)0U,
                DistanceFromLeft = (UInt32Value)0U,
                DistanceFromBottom = (UInt32Value)0U,
                DistanceFromRight = (UInt32Value)0U
            };
            drawing.Append(inline);

            var width = OoXmlHelper.ScaleCmlToEmu(_boundingBoxOfEverything.Width);
            var height = OoXmlHelper.ScaleCmlToEmu(_boundingBoxOfEverything.Height);
            var extent = new Wp.Extent { Cx = width, Cy = height };

            var effectExtent = new Wp.EffectExtent
            {
                TopEdge = 0L,
                LeftEdge = 0L,
                BottomEdge = 0L,
                RightEdge = 0L
            };

            inline.Append(extent);
            inline.Append(effectExtent);

            var inlineId = UInt32Value.FromUInt32((uint)_ooxmlId);
            var docProperties = new Wp.DocProperties
            {
                Id = inlineId,
                Name = "Chem4Word Structure"
            };

            inline.Append(docProperties);

            var graphic = new A.Graphic();
            graphic.AddNamespaceDeclaration("a", "http://schemas.openxmlformats.org/drawingml/2006/main");

            inline.Append(graphic);

            var graphicData = new A.GraphicData
            {
                Uri = "http://schemas.microsoft.com/office/word/2010/wordprocessingGroup"
            };

            graphic.Append(graphicData);

            _wordprocessingGroup = new Wpg.WordprocessingGroup();
            graphicData.Append(_wordprocessingGroup);

            var nonVisualGroupDrawingShapeProperties = new Wpg.NonVisualGroupDrawingShapeProperties();

            var groupShapeProperties = new Wpg.GroupShapeProperties();

            var transformGroup = new A.TransformGroup();
            var offset = new A.Offset { X = 0L, Y = 0L };
            var extents = new A.Extents { Cx = width, Cy = height };
            var childOffset = new A.ChildOffset { X = 0L, Y = 0L };
            var childExtents = new A.ChildExtents { Cx = width, Cy = height };

            transformGroup.Append(offset);
            transformGroup.Append(extents);
            transformGroup.Append(childOffset);
            transformGroup.Append(childExtents);

            groupShapeProperties.Append(transformGroup);

            _wordprocessingGroup.Append(nonVisualGroupDrawingShapeProperties);
            _wordprocessingGroup.Append(groupShapeProperties);

            return run;
        }

        private (Point Start, Point End, Rect Extents) OffsetPoints(Point start, Point end)
        {
            var startPoint = new Point(start.X, start.Y);
            var endPoint = new Point(end.X, end.Y);
            var extents = new Rect(startPoint, endPoint);

            // Move Extents and Points to have 0,0 Top Left Reference
            startPoint.Offset(-_boundingBoxOfEverything.Left, -_boundingBoxOfEverything.Top);
            endPoint.Offset(-_boundingBoxOfEverything.Left, -_boundingBoxOfEverything.Top);
            extents.Offset(-_boundingBoxOfEverything.Left, -_boundingBoxOfEverything.Top);

            // Move points into New Extents
            startPoint.Offset(-extents.Left, -extents.Top);
            endPoint.Offset(-extents.Left, -extents.Top);

            // Return a Tuple with the results
            return (Start: startPoint, End: endPoint, Extents: extents);
        }

        private void LoadFont()
        {
            var json = ResourceHelper.GetStringResource(Assembly.GetExecutingAssembly(), "Arial.json");
            _TtfCharacterSet = JsonConvert.DeserializeObject<Dictionary<char, TtfCharacter>>(json);
        }

        /// <summary>
        /// Sets the canvas size to accommodate any extra space required by label characters
        /// </summary>
        private void SetCanvasSizeX()
        {
            _boundingBoxOfAllAtoms = _chemistryModel.BoundingBoxOfCmlPoints;

            _boundingBoxOfEverything = _boundingBoxOfAllAtoms;

            foreach (var alc in _positionerOutputs.AtomLabelCharacters)
            {
                if (alc.IsSmaller)
                {
                    var r = new Rect(alc.Position,
                                     new Size(OoXmlHelper.ScaleCsTtfToCml(alc.Character.Width, _medianBondLength) * OoXmlHelper.SubscriptScaleFactor,
                                              OoXmlHelper.ScaleCsTtfToCml(alc.Character.Height, _medianBondLength) * OoXmlHelper.SubscriptScaleFactor));
                    _boundingBoxOfEverything.Union(r);
                }
                else
                {
                    var r = new Rect(alc.Position,
                                     new Size(OoXmlHelper.ScaleCsTtfToCml(alc.Character.Width, _medianBondLength),
                                              OoXmlHelper.ScaleCsTtfToCml(alc.Character.Height, _medianBondLength)));
                    _boundingBoxOfEverything.Union(r);
                }
            }

            foreach (var group in _positionerOutputs.AllMoleculeExtents)
            {
                _boundingBoxOfEverything.Union(group.ExternalCharacterExtents);
            }

            // Bullet proofing - Error seen in telemetry :-
            // System.InvalidOperationException: Cannot call this method on the Empty Rect.
            //   at System.Windows.Rect.Inflate(Double width, Double height)
            if (_boundingBoxOfEverything == Rect.Empty)
            {
                _boundingBoxOfEverything = new Rect(new Point(0, 0), new Size(OoXmlHelper.DrawingMargin * 10, OoXmlHelper.DrawingMargin * 10));
            }
            else
            {
                _boundingBoxOfEverything.Inflate(OoXmlHelper.DrawingMargin, OoXmlHelper.DrawingMargin);
            }
        }

        private double BondOffset()
            => _medianBondLength * OoXmlHelper.MultipleBondOffsetPercentage;

        private void ShutDownProgress(Progress pb)
        {
            pb.Value = 0;
            pb.Hide();
            pb.Close();
        }
    }
}