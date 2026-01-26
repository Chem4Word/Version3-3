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

namespace Chem4Word.Renderer.OoXmlV4.OoXml
{
    // ReSharper disable PossiblyMistakenUseOfParamsMethod
    [SuppressMessage("Minor Code Smell", "S3220:Method calls should not resolve ambiguously to overloads with \"params\"", Justification = "<OoXml>")]
    public class OoXmlRenderer
    {
        // DrawingML Units
        // https://startbigthinksmall.wordpress.com/2010/01/04/points-inches-and-emus-measuring-units-in-office-open-xml/
        // EMU Calculator
        // http://lcorneliussen.de/raw/dashboards/ooxml/

        private static string _class = MethodBase.GetCurrentMethod().DeclaringType?.Name;
        private static string _product = Assembly.GetExecutingAssembly().FullName.Split(',')[0];

        private readonly Model _chemistryModel;
        private readonly OoXmlV4Options _options;
        private readonly IChem4WordTelemetry _telemetry;
        private Rect _boundingBoxOfAllAtoms;
        private Rect _boundingBoxOfEverything;

        private RendererInputs _inputs;
        private RendererOutputs _outputs;

        private double _meanBondLength;
        private long _ooxmlId;
        private Point _topLeft;

        // Inputs to positioner
        private Dictionary<char, TtfCharacter> _TtfCharacterSet;

        private Wpg.WordprocessingGroup _wordprocessingGroup;

        public OoXmlRenderer(Model model, OoXmlV4Options options, IChem4WordTelemetry telemetry, Point topLeft)
        {
            var module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            _telemetry = telemetry;
            _telemetry.Write(module, "Verbose", "Called");

            _options = options;
            _topLeft = topLeft;
            _chemistryModel = model;
            _meanBondLength = model.MeanBondLength;

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
            _meanBondLength = _chemistryModel.MeanBondLength;
            if (_chemistryModel.GetAllBonds().Count == 0)
            {
                _meanBondLength = CoreConstants.StandardBondLength;
            }

            // Initialise progress monitoring
            var progress = new Progress
            {
                TopLeft = _topLeft
            };

            _inputs = new RendererInputs
            {
                Progress = progress,
                Options = _options,
                TtfCharacterSet = _TtfCharacterSet,
                Telemetry = _telemetry,
                MeanBondLength = _meanBondLength,
                Model = _chemistryModel,
            };

            // This is where the magic starts to happen
            var positioner = new OoXmlPositioner(_inputs);
            _outputs = positioner.Position();

            _boundingBoxOfAllAtoms = _chemistryModel.BoundingBoxOfCmlPoints;
            _boundingBoxOfEverything = OoXmlHelper.GetAllCharacterExtents(_chemistryModel, _outputs);

            // This is where we make it all "tidy"
            var beautifier = new OoXmlBeautifier(_inputs, _outputs);
            beautifier.Beautify();

            // Create Base OoXml Objects
            var run = CreateRun();

            // Render molecule Group Brackets
            if (_chemistryModel.ShowMoleculeGrouping)
            {
                foreach (var group in _outputs.GroupBrackets)
                {
                    var bracketColour = _chemistryModel.ShowColouredAtoms ? OoXmlColours.Bracket : OoXmlColours.Black;
                    DrawGroupBrackets(group, _meanBondLength * 0.5, OoXmlConstants.AcsLineWidth * 2, bracketColour);
                }
            }

            // Render molecule brackets
            foreach (var moleculeBracket in _outputs.MoleculeBrackets)
            {
                DrawMoleculeBrackets(moleculeBracket, OoXmlConstants.AcsLineWidth, OoXmlColours.Black);
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
                        case ReactionType.Theoretical:
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

            // Render Bond Lines
            foreach (var bondLine in _outputs.BondLines)
            {
                switch (bondLine.Style)
                {
                    case BondLineStyle.Thick:
                    case BondLineStyle.Wedge:
                        DrawFilledBond(bondLine.CurrentOutline, bondLine.BondPath, bondLine.Colour);
                        //_outputs.Diagnostics.Polygons.Add(new DiagnosticPolygon(bondLine.OriginalOutline, OoXmlColours.VibrantGreen));
                        //_outputs.Diagnostics.Polygons.Add(new DiagnosticPolygon(bondLine.CurrentOutline, OoXmlColours.Cyan));
                        break;

                    case BondLineStyle.Hatch:
                        DrawHatchBond(bondLine.CurrentOutline, bondLine.BondPath, bondLine.Colour);
                        //_outputs.Diagnostics.Polygons.Add(new DiagnosticPolygon(bondLine.OriginalOutline, OoXmlColours.VibrantGreen));
                        //_outputs.Diagnostics.Polygons.Add(new DiagnosticPolygon(bondLine.CurrentOutline, OoXmlColours.Cyan));
                        break;

                    default:
                        // These should be all other single stroke lines for bonds
                        DrawBondLine(bondLine.Start, bondLine.End, bondLine.BondPath, bondLine.Style, bondLine.Colour, bondLine.Width);
                        break;
                }
            }

            // Render Atom and Molecule Characters
            foreach (var character in _outputs.AtomLabelCharacters)
            {
                DrawCharacter(character);
            }

            // Render the Electrons
            foreach (List<OoXmlElectron> listOfElectrons in _outputs.AtomsWithElectrons.Values)
            {
                foreach (OoXmlElectron electron in listOfElectrons)
                {
                    switch (electron.Electron.TypeOfElectron)
                    {
                        case ElectronType.Radical:
                            DrawFilledCircle(electron.Points[0], electron.Electron.Path, BondOffset() / 2, electron.Colour);
                            break;

                        case ElectronType.LonePair:
                            DrawFilledCircle(electron.Points[0], electron.Electron.Path, BondOffset() / 2, electron.Colour);
                            DrawFilledCircle(electron.Points[1], electron.Electron.Path, BondOffset() / 2, electron.Colour);
                            break;

                        case ElectronType.Carbenoid:
                            DrawBondLine(electron.Points[0], electron.Points[1], electron.Electron.Path, BondLineStyle.Solid, electron.Colour);
                            break;
                    }
                }
            }

            // Render the Pushers
            if (_outputs.Pushers.Any())
            {
                foreach (OoXmlElectronPusher pusher in _outputs.Pushers)
                {
                    DrawElectronPusher(pusher.StartPoint, pusher.EndPoint, pusher.FirstControlPoint, pusher.SecondControlPoint, pusher.Path);
                }
            }

            // Render Diagnostic Markers
            RenderDiagnostics();

            _telemetry.Write(module, "Timing", $"Rendering {_chemistryModel.TotalMoleculesCount} molecules with {_chemistryModel.TotalAtomsCount} atoms and {_chemistryModel.TotalBondsCount} bonds took {SafeDouble.AsString0(swr.ElapsedMilliseconds)} ms; Average Bond Length: {SafeDouble.AsString(_chemistryModel.MeanBondLength)}");

            ShutDownProgress(progress);

            return run;
         }

        private void RenderDiagnostics()
        {
            var spotSize = _meanBondLength * OoXmlConstants.MultipleBondOffsetPercentage / 3;

            if (_options.ShowAtomPositions)
            {
                foreach (var atom in _chemistryModel.GetAllAtoms())
                {
                    var extents = new Rect(new Point(atom.Position.X - spotSize, atom.Position.Y - spotSize),
                                           new Point(atom.Position.X + spotSize, atom.Position.Y + spotSize));
                    DrawShape(extents, A.ShapeTypeValues.Ellipse, true, OoXmlColours.Red);
                }

                foreach (var scheme in _chemistryModel.ReactionSchemes.Values)
                {
                    foreach (var reaction in scheme.Reactions.Values)
                    {
                        var head = new Rect(new Point(reaction.HeadPoint.X - spotSize, reaction.HeadPoint.Y - spotSize),
                                            new Point(reaction.HeadPoint.X + spotSize, reaction.HeadPoint.Y + spotSize));
                        DrawShape(head, A.ShapeTypeValues.Ellipse, true, OoXmlColours.Green);

                        var tail = new Rect(new Point(reaction.TailPoint.X - spotSize, reaction.TailPoint.Y - spotSize),
                                            new Point(reaction.TailPoint.X + spotSize, reaction.TailPoint.Y + spotSize));
                        DrawShape(tail, A.ShapeTypeValues.Ellipse, true, OoXmlColours.Green);
                    }
                }

                foreach (var annotation in _chemistryModel.Annotations.Values)
                {
                    var extents = new Rect(new Point(annotation.Position.X - spotSize, annotation.Position.Y - spotSize),
                                           new Point(annotation.Position.X + spotSize, annotation.Position.Y + spotSize));
                    DrawShape(extents, A.ShapeTypeValues.Ellipse, true, OoXmlColours.Blue);
                }
            }

            if (_options.ShowMoleculeBoundingBoxes)
            {
                foreach (var item in _outputs.AllMoleculeExtents)
                {
                    DrawBox(item.AtomExtents, OoXmlColours.Red, .25);
                    DrawBox(item.InternalCharacterExtents, OoXmlColours.Green, .25);
                    DrawBox(item.ExternalCharacterExtents, OoXmlColours.Blue, .25);
                }

                DrawBox(_boundingBoxOfAllAtoms, OoXmlColours.Red, .25);
                DrawBox(_boundingBoxOfEverything, OoXmlColours.Black, .25);
            }

            if (_options.ShowHulls)
            {
                foreach (var hull in _outputs.ConvexHulls)
                {
                    var points = hull.Value.ToList();
                    DrawPolygon(points, true, OoXmlColours.Red, 0.25);
                }
            }

            if (_options.ShowCharacterBoundingBoxes)
            {
                foreach (var atom in _chemistryModel.GetAllAtoms())
                {
                    var chars = _outputs.AtomLabelCharacters.FindAll(a => a.ParentAtom.Equals(atom.Path));
                    var atomCharsRect = Rect.Empty;
                    AddCharacterBoundingBoxes(chars, atomCharsRect);
                    if (!atomCharsRect.IsEmpty)
                    {
                        DrawBox(atomCharsRect, OoXmlColours.Orange, 0.5);
                    }

                    var reactionCharacters = _outputs.AtomLabelCharacters.FindAll(a => a.ParentAtom.StartsWith("/rs"));
                    atomCharsRect = Rect.Empty;
                    AddCharacterBoundingBoxes(reactionCharacters, atomCharsRect);

                    // Local Function
                    void AddCharacterBoundingBoxes(List<AtomLabelCharacter> atomLabelCharacters, Rect rect)
                    {
                        foreach (var alc in atomLabelCharacters)
                        {
                            var thisBoundingBox = new Rect(alc.Position,
                                                           new Size(OoXmlHelper.ScaleCsTtfToCml(alc.Character.Width, _meanBondLength),
                                                                    OoXmlHelper.ScaleCsTtfToCml(alc.Character.Height, _meanBondLength)));
                            if (alc.IsSmaller)
                            {
                                thisBoundingBox = new Rect(alc.Position,
                                                           new Size(
                                                               OoXmlHelper.ScaleCsTtfToCml(alc.Character.Width, _meanBondLength) *
                                                               OoXmlConstants.SubscriptScaleFactor,
                                                               OoXmlHelper.ScaleCsTtfToCml(alc.Character.Height, _meanBondLength) *
                                                               OoXmlConstants.SubscriptScaleFactor));
                            }

                            DrawBox(thisBoundingBox, OoXmlColours.Green, 0.25);

                            rect.Union(thisBoundingBox);
                        }
                    }
                }
            }

            if (_options.ShowCharacterGroupBoundingBoxes)
            {
                foreach (var rectangle in _outputs.Diagnostics.Rectangles)
                {
                    DrawBox(rectangle.BoundingBox, rectangle.Colour, OoXmlConstants.AcsLineWidth / 2);
                }
            }

            if (_options.ShowBondCrossingPoints)
            {
                foreach (var point in _outputs.CrossingPoints)
                {
                    var extents = new Rect(new Point(point.X - spotSize * 2, point.Y - spotSize * 2),
                                           new Point(point.X + spotSize * 2, point.Y + spotSize * 2));
                    DrawShape(extents, A.ShapeTypeValues.Ellipse, true, OoXmlColours.Orange);
                }
            }

            if (_options.ShowRingCentres)
            {
                foreach (var point in _outputs.RingCenters)
                {
                    var extents = new Rect(new Point(point.X - spotSize, point.Y - spotSize),
                                           new Point(point.X + spotSize, point.Y + spotSize));
                    DrawShape(extents, A.ShapeTypeValues.Ellipse, true, OoXmlColours.Green);
                }

                // ToDo: [MAW] Experimental code to add inner circles for aromatic rings
                foreach (var innerCircle in _outputs.InnerCircles)
                {
                    var smallerCircle = new InnerCircle
                    {
                        Centre = innerCircle.Centre
                    };
                    // Move all points towards centre
                    foreach (var point in innerCircle.Points)
                    {
                        var vector = smallerCircle.Centre - point;
                        var innerPoint = point + vector * OoXmlConstants.MultipleBondOffsetPercentage;
                        smallerCircle.Points.Add(innerPoint);

                        //Rect extents = new Rect(new Point(innerPoint.X - spotSize, innerPoint.Y - spotSize),
                        //                        new Point(innerPoint.X + spotSize, innerPoint.Y + spotSize))
                        //DrawShape(extents, A.ShapeTypeValues.Ellipse, false, OoXmlColours.Red, 0.5)
                    }

                    DrawInnerCircle(smallerCircle, OoXmlColours.Red, 0.5);
                    //DrawPolygon(smallerCircle.Points, OoXmlColours.Green, 0.5)
                }
            }

            if (_options.ShowHulls)
            {
                foreach (var hull in _outputs.ConvexHulls)
                {
                    var points = hull.Value.ToList();
                    DrawPolygon(points, true, OoXmlColours.Red, 0.25);
                }
            }

            // Finally draw any debugging diagnostics
            foreach (var line in _outputs.Diagnostics.Lines)
            {
                DrawBondLine(line.Start, line.End, "", line.Style, line.Colour, 0.5);
            }

            foreach (var polygon in _outputs.Diagnostics.Polygons)
            {
                DrawPolygon(polygon.Points, true, polygon.Colour, 0.33);
            }

            foreach (var spot in _outputs.Diagnostics.Points)
            {
                var half = spot.Diameter / 2;
                var extents = new Rect(new Point(spot.Point.X - half, spot.Point.Y - half),
                                       new Point(spot.Point.X + half, spot.Point.Y + half));
                DrawShape(extents, A.ShapeTypeValues.Ellipse, true, spot.Colour);
            }
        }

        private double BondOffset()
            => _meanBondLength * OoXmlConstants.MultipleBondOffsetPercentage;

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

        private void DrawCharacter(AtomLabelCharacter alc)
        {
            var characterPosition = new Point(alc.Position.X, alc.Position.Y);
            characterPosition.Offset(-_boundingBoxOfEverything.Left, -_boundingBoxOfEverything.Top);

            var emuWidth = OoXmlHelper.ScaleCsTtfToEmu(alc.Character.Width, _meanBondLength);
            var emuHeight = OoXmlHelper.ScaleCsTtfToEmu(alc.Character.Height, _meanBondLength);
            if (alc.IsSmaller)
            {
                emuWidth = OoXmlHelper.ScaleCsTtfSubScriptToEmu(alc.Character.Width, _meanBondLength);
                emuHeight = OoXmlHelper.ScaleCsTtfSubScriptToEmu(alc.Character.Height, _meanBondLength);
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
                    X = $"{OoXmlHelper.ScaleCsTtfSubScriptToEmu(ttfPoint.X - alc.Character.OriginX, _meanBondLength)}",
                    Y = $"{OoXmlHelper.ScaleCsTtfSubScriptToEmu(alc.Character.Height + ttfPoint.Y - (alc.Character.Height + alc.Character.OriginY), _meanBondLength)}"
                };
                return pp;
            }

            A.Point MakeNormalPoint(TtfPoint ttfPoint)
            {
                var pp = new A.Point
                {
                    X = $"{OoXmlHelper.ScaleCsTtfToEmu(ttfPoint.X - alc.Character.OriginX, _meanBondLength)}",
                    Y = $"{OoXmlHelper.ScaleCsTtfToEmu(alc.Character.Height + ttfPoint.Y - (alc.Character.Height + alc.Character.OriginY), _meanBondLength)}"
                };
                return pp;
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
            step *= OoXmlHelper.ScaleCmlToEmu(15 * OoXmlConstants.MultipleBondOffsetPercentage);

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

        private void DrawBondLine(Point bondStart, Point bondEnd, string bondPath,
                                  BondLineStyle lineStyle = BondLineStyle.Solid,
                                  string colour = OoXmlColours.Black,
                                  double lineWidth = OoXmlConstants.AcsLineWidth)
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
                    DrawStraightLine(bondStart, bondEnd, bondPath, BondLineStyle.Zero, OoXmlColours.Green, lineWidth);
                    break;
            }
        }

        private void DrawBox(Rect cmlExtents,
                             string lineColour = OoXmlColours.Black,
                             double lineWidth = OoXmlConstants.AcsLineWidth)
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

                var emuLineWidth = (Int32Value)(lineWidth * OoXmlConstants.EmusPerWordPoint);
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

        private void DrawFilledBond(List<Point> points, string bondPath,
                                   string colour = OoXmlColours.Black)
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
            var outline = new A.Outline { Width = Int32Value.FromInt32((int)OoXmlConstants.AcsLineWidthEmus), CapType = A.LineCapValues.Round };
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

                var emuLineWidth = (Int32Value)(lineWidth * OoXmlConstants.EmusPerWordPoint);
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

        private void DrawHatchBond(List<Point> points, string bondPath,
                                   string colour = OoXmlColours.Black)
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
            var outline = new A.Outline { Width = Int32Value.FromInt32((int)OoXmlConstants.AcsLineWidthEmus), CapType = A.LineCapValues.Round };
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

            var emuLineWidth = (Int32Value)(lineWidth * OoXmlConstants.EmusPerWordPoint);
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

            var emuLineWidth = (Int32Value)(lineWidth * OoXmlConstants.EmusPerWordPoint);
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

            var emuLineWidth = (Int32Value)(lineWidth * OoXmlConstants.EmusPerWordPoint);
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
                DrawPolygon(points, false, OoXmlColours.Black, OoXmlConstants.AcsLineWidth);
            }
        }

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
                    GeometryTool.AdjustLineAboutMidpoint(ref p1, ref p2, -_meanBondLength / OoXmlConstants.LineShrinkPixels);
                    break;

                case ReactionType.ReversibleBiasedForward:
                    GeometryTool.AdjustLineAboutMidpoint(ref p3, ref p4, -_meanBondLength / OoXmlConstants.LineShrinkPixels);
                    break;
            }

            DrawPolygon(new List<Point> { p1, p2, BarbLocation(p1, p2) }, false, OoXmlColours.Black, OoXmlConstants.AcsLineWidth);
            DrawPolygon(new List<Point> { p3, p4, BarbLocation(p3, p4) }, false, OoXmlColours.Black, OoXmlConstants.AcsLineWidth);

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

        private void DrawFilledCircle(Point position, string path, double diameter, string colour)
        {
            var extents = new Rect(new Point(position.X - diameter / 2, position.Y - diameter / 2),
                                   new Point(position.X + diameter / 2, position.Y + diameter / 2));
            DrawShape(extents, A.ShapeTypeValues.Ellipse, true, colour);
        }

        private void DrawShape(Rect cmlExtents, A.ShapeTypeValues shape, bool filled, string colour,
                               double outlineWidth = OoXmlConstants.AcsLineWidth, string path = "")
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
            var shapeName = "Shape" + path;
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
                var emuLineWidth = (Int32Value)(outlineWidth * OoXmlConstants.EmusPerWordPoint);
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

        private void DrawElectronPusher(Point startPoint, Point endPoint,
                                        Point firstControlPoint, Point secondControlPoint,
                                        string pusherPath)
        {
            var tuple = OffsetFourPoints(startPoint, firstControlPoint, secondControlPoint, endPoint);
            var cmlStartPoint = tuple.Start;
            var cmlFirstPoint = tuple.First;
            var cmlSecondPoint = tuple.Second;
            var cmlEndPoint = tuple.End;
            var cmlLineExtents = tuple.Extents;

            var emuTop = OoXmlHelper.ScaleCmlToEmu(cmlLineExtents.Top);
            var emuLeft = OoXmlHelper.ScaleCmlToEmu(cmlLineExtents.Left);
            var emuWidth = OoXmlHelper.ScaleCmlToEmu(cmlLineExtents.Width);
            var emuHeight = OoXmlHelper.ScaleCmlToEmu(cmlLineExtents.Height);

            //var xOffset = xMin;
            //var yOffset = yMin;

            var id = _ooxmlId++;
            var suffix = string.IsNullOrEmpty(pusherPath) ? id.ToString() : pusherPath;
            var shapeName = "Pusher Arrow " + suffix;

            var wordprocessingShape = CreateShape(id, shapeName);
            var shapeProperties = CreateShapeProperties(wordprocessingShape, emuTop, emuLeft, emuWidth, emuHeight);

            var pathList = new A.PathList();

            var path = new A.Path { Width = emuWidth, Height = emuHeight };

            var moveTo = new A.MoveTo();
            var startingPoint = new A.Point
            {
                X = OoXmlHelper.ScaleCmlToEmu(cmlStartPoint.X).ToString(),
                Y = OoXmlHelper.ScaleCmlToEmu(cmlStartPoint.Y).ToString()
            };
            moveTo.Append(startingPoint);
            path.Append(moveTo);

            var cubicBezierCurveTo = new A.CubicBezierCurveTo();

            var p1 = new A.Point
            {
                X = OoXmlHelper.ScaleCmlToEmu(cmlFirstPoint.X).ToString(),
                Y = OoXmlHelper.ScaleCmlToEmu(cmlFirstPoint.Y).ToString()
            };
            cubicBezierCurveTo.Append(p1);

            var p2 = new A.Point
            {
                X = OoXmlHelper.ScaleCmlToEmu(cmlSecondPoint.X).ToString(),
                Y = OoXmlHelper.ScaleCmlToEmu(cmlSecondPoint.Y).ToString()
            };
            cubicBezierCurveTo.Append(p2);

            var p3 = new A.Point
            {
                X = OoXmlHelper.ScaleCmlToEmu(cmlEndPoint.X).ToString(),
                Y = OoXmlHelper.ScaleCmlToEmu(cmlEndPoint.Y).ToString()
            };
            cubicBezierCurveTo.Append(p3);

            path.Append(cubicBezierCurveTo);
            pathList.Append(path);

            var lineWidth = OoXmlConstants.AcsLineWidth;
            var emuLineWidth = (Int32Value)(lineWidth * OoXmlConstants.EmusPerWordPoint);
            var outline = new A.Outline { Width = emuLineWidth, CapType = A.LineCapValues.Round };

            var solidFill = new A.SolidFill();
            var rgbColorModelHex = new A.RgbColorModelHex { Val = OoXmlColours.DarkRed };
            solidFill.Append(rgbColorModelHex);
            outline.Append(solidFill);

            // Draw the arrow head
            var tailEnd = new A.TailEnd { Type = A.LineEndValues.Triangle };
            outline.Append(tailEnd);

            shapeProperties.Append(CreateCustomGeometry(pathList));
            shapeProperties.Append(outline);

            wordprocessingShape.Append(CreateShapeStyle());

            var textBodyProperties = new Wps.TextBodyProperties();
            wordprocessingShape.Append(textBodyProperties);

            _wordprocessingGroup.Append(wordprocessingShape);

            Debug.WriteLine(wordprocessingShape.ToString());
        }

        private void DrawSingleLinedReactionArrow(Reaction reaction)
        {
            var lineStart = reaction.TailPoint;
            var lineEnd = reaction.HeadPoint;

            var tuple = OffsetTwoPoints(lineStart, lineEnd);
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

            var emuLineWidth = (Int32Value)(OoXmlConstants.AcsLineWidth * OoXmlConstants.EmusPerWordPoint);
            var outline = new A.Outline { Width = emuLineWidth, CapType = A.LineCapValues.Round };

            var solidFill = new A.SolidFill();
            var rgbColorModelHex = new A.RgbColorModelHex { Val = OoXmlColours.Black };
            solidFill.Append(rgbColorModelHex);
            outline.Append(solidFill);

            if (reaction.ReactionType == ReactionType.Theoretical)
            {
                outline.Append(new A.PresetDash { Val = A.PresetLineDashValues.Dash });
            }

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

                var crossArmLength = OoXmlConstants.MultipleBondOffsetPercentage * _chemistryModel.MeanBondLength;
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
            var tuple = OffsetTwoPoints(bondStart, bondEnd);
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

            var emuLineWidth = (Int32Value)(lineWidth * OoXmlConstants.EmusPerWordPoint);
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
            var tuple = OffsetTwoPoints(bondStart, bondEnd);
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

            var lineWidth = OoXmlConstants.AcsLineWidth;
            var emuLineWidth = (Int32Value)(lineWidth * OoXmlConstants.EmusPerWordPoint);
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

        private void LoadFont()
        {
            var json = ResourceHelper.GetStringResource(Assembly.GetExecutingAssembly(), "Arial.json");
            _TtfCharacterSet = JsonConvert.DeserializeObject<Dictionary<char, TtfCharacter>>(json);
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

        private (Point Start, Point End, Rect Extents) OffsetTwoPoints(Point start, Point end)
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

        private (Point Start, Point End, Point First, Point Second, Rect Extents) OffsetFourPoints(
            Point start, Point first, Point second, Point end)
        {
            var startPoint = new Point(start.X, start.Y);
            var endPoint = new Point(end.X, end.Y);
            var firstPoint = new Point(first.X, first.Y);
            var secondPoint = new Point(second.X, second.Y);
            var extents = new Rect(startPoint, endPoint);

            // Move Extents and Points to have 0,0 Top Left Reference
            startPoint.Offset(-_boundingBoxOfEverything.Left, -_boundingBoxOfEverything.Top);
            firstPoint.Offset(-_boundingBoxOfEverything.Left, -_boundingBoxOfEverything.Top);
            secondPoint.Offset(-_boundingBoxOfEverything.Left, -_boundingBoxOfEverything.Top);
            endPoint.Offset(-_boundingBoxOfEverything.Left, -_boundingBoxOfEverything.Top);
            extents.Offset(-_boundingBoxOfEverything.Left, -_boundingBoxOfEverything.Top);

            // Move points into New Extents
            startPoint.Offset(-extents.Left, -extents.Top);
            firstPoint.Offset(-extents.Left, -extents.Top);
            secondPoint.Offset(-extents.Left, -extents.Top);
            endPoint.Offset(-extents.Left, -extents.Top);

            // Return a Tuple with the results
            return (Start: startPoint, End: endPoint, First: firstPoint, Second: secondPoint, Extents: extents);
        }

        /// <summary>
        /// Sets the canvas size to accommodate any extra space required by label characters
        /// </summary>
        private void SetCanvasSizeX()
        {
            _boundingBoxOfAllAtoms = _chemistryModel.BoundingBoxOfCmlPoints;

            _boundingBoxOfEverything = _boundingBoxOfAllAtoms;

            foreach (var alc in _outputs.AtomLabelCharacters)
            {
                if (alc.IsSmaller)
                {
                    var r = new Rect(alc.Position,
                                     new Size(OoXmlHelper.ScaleCsTtfToCml(alc.Character.Width, _meanBondLength) * OoXmlConstants.SubscriptScaleFactor,
                                              OoXmlHelper.ScaleCsTtfToCml(alc.Character.Height, _meanBondLength) * OoXmlConstants.SubscriptScaleFactor));
                    _boundingBoxOfEverything.Union(r);
                }
                else
                {
                    var r = new Rect(alc.Position,
                                     new Size(OoXmlHelper.ScaleCsTtfToCml(alc.Character.Width, _meanBondLength),
                                              OoXmlHelper.ScaleCsTtfToCml(alc.Character.Height, _meanBondLength)));
                    _boundingBoxOfEverything.Union(r);
                }
            }

            foreach (var group in _outputs.AllMoleculeExtents)
            {
                _boundingBoxOfEverything.Union(group.ExternalCharacterExtents);
            }

            // Bullet proofing - Error seen in telemetry :-
            // System.InvalidOperationException: Cannot call this method on the Empty Rect.
            //   at System.Windows.Rect.Inflate(Double width, Double height)
            if (_boundingBoxOfEverything == Rect.Empty)
            {
                _boundingBoxOfEverything = new Rect(new Point(0, 0), new Size(OoXmlConstants.DrawingMargin * 10, OoXmlConstants.DrawingMargin * 10));
            }
            else
            {
                _boundingBoxOfEverything.Inflate(OoXmlConstants.DrawingMargin, OoXmlConstants.DrawingMargin);
            }
        }

        private void ShutDownProgress(Progress pb)
        {
            pb.Value = 0;
            pb.Hide();
            pb.Close();
        }
    }
}
