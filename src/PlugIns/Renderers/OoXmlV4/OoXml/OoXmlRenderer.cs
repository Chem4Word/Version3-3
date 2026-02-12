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
using Chem4Word.Renderer.OoXmlV4.Entities.Diagnostic;
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
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

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
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            _telemetry.Write(module, "Verbose", "Called");

            Stopwatch swr = new Stopwatch();
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
            Progress progress = new Progress
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
            OoXmlPositioner positioner = new OoXmlPositioner(_inputs);
            _outputs = positioner.Position();

            _boundingBoxOfAllAtoms = _chemistryModel.BoundingBoxOfCmlPoints;
            _boundingBoxOfEverything = OoXmlHelper.GetAllCharacterExtents(_chemistryModel, _outputs);

            // This is where we make it all "tidy"
            OoXmlBeautifier beautifier = new OoXmlBeautifier(_inputs, _outputs);
            beautifier.Beautify();

            // Create Base OoXml Objects
            Run run = CreateRun();

            // Render molecule Group Brackets
            if (_chemistryModel.ShowMoleculeGrouping)
            {
                foreach (Rect group in _outputs.GroupBrackets)
                {
                    string bracketColour = _chemistryModel.ShowColouredAtoms ? OoXmlColours.Bracket : OoXmlColours.Black;
                    DrawGroupBrackets(group, _meanBondLength * 0.5, OoXmlConstants.AcsLineWidth * 2, bracketColour);
                }
            }

            // Render molecule brackets
            foreach (Rect moleculeBracket in _outputs.MoleculeBrackets)
            {
                DrawMoleculeBrackets(moleculeBracket, OoXmlConstants.AcsLineWidth, OoXmlColours.Black);
            }

            // Render reaction arrows
            foreach (ReactionScheme scheme in _chemistryModel.ReactionSchemes.Values)
            {
                foreach (Reaction reaction in scheme.Reactions.Values)
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
            foreach (BondLine bondLine in _outputs.BondLines)
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
            foreach (AtomLabelCharacter character in _outputs.AtomLabelCharacters)
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
                            DrawRadicalDot(electron.Points[0], electron.Electron.Path, BondOffset() / 2, electron.Colour, true);
                            break;

                        case ElectronType.LonePair:
                            DrawRadicalDot(electron.Points[0], electron.Electron.Path, BondOffset() / 2, electron.Colour, true);
                            DrawRadicalDot(electron.Points[1], electron.Electron.Path, BondOffset() / 2, electron.Colour, true);
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
                    DrawElectronPusher(pusher.StartPoint, pusher.EndPoint,
                                       pusher.FirstControlPoint, pusher.SecondControlPoint,
                                       pusher.FishHookPoint, pusher.PusherType, pusher.Path);
                }
            }

            // Render Diagnostic Markers
            if (Debugger.IsAttached)
            {
                RenderDiagnostics();
            }

            _telemetry.Write(module, "Timing", $"Rendering {_chemistryModel.TotalMoleculesCount} molecules with {_chemistryModel.TotalAtomsCount} atoms and {_chemistryModel.TotalBondsCount} bonds took {SafeDouble.AsString0(swr.ElapsedMilliseconds)} ms; Average Bond Length: {SafeDouble.AsString(_chemistryModel.MeanBondLength)}");

            ShutDownProgress(progress);

            return run;
        }

        private void RenderDiagnostics()
        {
            double spotSize = _meanBondLength * OoXmlConstants.MultipleBondOffsetPercentage / 3;

            if (_options.ShowAtomPositions)
            {
                foreach (Atom atom in _chemistryModel.GetAllAtoms())
                {
                    Rect extents = new Rect(new Point(atom.Position.X - spotSize, atom.Position.Y - spotSize),
                                            new Point(atom.Position.X + spotSize, atom.Position.Y + spotSize));
                    DrawShape(extents, A.ShapeTypeValues.Ellipse, true, OoXmlColours.Red);
                }

                foreach (ReactionScheme scheme in _chemistryModel.ReactionSchemes.Values)
                {
                    foreach (Reaction reaction in scheme.Reactions.Values)
                    {
                        Rect head = new Rect(new Point(reaction.HeadPoint.X - spotSize, reaction.HeadPoint.Y - spotSize),
                                             new Point(reaction.HeadPoint.X + spotSize, reaction.HeadPoint.Y + spotSize));
                        DrawShape(head, A.ShapeTypeValues.Ellipse, true, OoXmlColours.Green);

                        Rect tail = new Rect(new Point(reaction.TailPoint.X - spotSize, reaction.TailPoint.Y - spotSize),
                                             new Point(reaction.TailPoint.X + spotSize, reaction.TailPoint.Y + spotSize));
                        DrawShape(tail, A.ShapeTypeValues.Ellipse, true, OoXmlColours.Green);
                    }
                }

                foreach (Annotation annotation in _chemistryModel.Annotations.Values)
                {
                    Rect extents = new Rect(new Point(annotation.Position.X - spotSize, annotation.Position.Y - spotSize),
                                            new Point(annotation.Position.X + spotSize, annotation.Position.Y + spotSize));
                    DrawShape(extents, A.ShapeTypeValues.Ellipse, true, OoXmlColours.Blue);
                }
            }

            if (_options.ShowMoleculeBoundingBoxes)
            {
                foreach (MoleculeExtents item in _outputs.AllMoleculeExtents)
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
                foreach (KeyValuePair<string, List<Point>> hull in _outputs.ConvexHulls)
                {
                    List<Point> points = hull.Value.ToList();
                    DrawPolygon(points, true, OoXmlColours.Red, 0.25);
                }
            }

            if (_options.ShowCharacterBoundingBoxes)
            {
                foreach (Atom atom in _chemistryModel.GetAllAtoms())
                {
                    List<AtomLabelCharacter> chars = _outputs.AtomLabelCharacters.FindAll(a => a.ParentAtom.Equals(atom.Path));
                    Rect atomCharsRect = Rect.Empty;
                    AddCharacterBoundingBoxes(chars, atomCharsRect);
                    if (!atomCharsRect.IsEmpty)
                    {
                        DrawBox(atomCharsRect, OoXmlColours.Orange, 0.5);
                    }

                    List<AtomLabelCharacter> reactionCharacters = _outputs.AtomLabelCharacters.FindAll(a => a.ParentAtom.StartsWith("/rs"));
                    atomCharsRect = Rect.Empty;
                    AddCharacterBoundingBoxes(reactionCharacters, atomCharsRect);

                    // Local Function
                    void AddCharacterBoundingBoxes(List<AtomLabelCharacter> atomLabelCharacters, Rect rect)
                    {
                        foreach (AtomLabelCharacter alc in atomLabelCharacters)
                        {
                            Rect thisBoundingBox = new Rect(alc.Position,
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
                foreach (DiagnosticRectangle rectangle in _outputs.Diagnostics.Rectangles)
                {
                    DrawBox(rectangle.BoundingBox, rectangle.Colour, OoXmlConstants.AcsLineWidth / 2);
                }
            }

            if (_options.ShowBondCrossingPoints)
            {
                foreach (Point point in _outputs.CrossingPoints)
                {
                    Rect extents = new Rect(new Point(point.X - spotSize * 2, point.Y - spotSize * 2),
                                            new Point(point.X + spotSize * 2, point.Y + spotSize * 2));
                    DrawShape(extents, A.ShapeTypeValues.Ellipse, true, OoXmlColours.Orange);
                }
            }

            if (_options.ShowRingCentres)
            {
                foreach (Point point in _outputs.RingCenters)
                {
                    Rect extents = new Rect(new Point(point.X - spotSize, point.Y - spotSize),
                                            new Point(point.X + spotSize, point.Y + spotSize));
                    DrawShape(extents, A.ShapeTypeValues.Ellipse, true, OoXmlColours.Green);
                }

                // ToDo: [MAW] Experimental code to add inner circles for aromatic rings
                foreach (InnerCircle innerCircle in _outputs.InnerCircles)
                {
                    InnerCircle smallerCircle = new InnerCircle
                                                {
                                                    Centre = innerCircle.Centre
                                                };
                    // Move all points towards centre
                    foreach (Point point in innerCircle.Points)
                    {
                        Vector vector = smallerCircle.Centre - point;
                        Point innerPoint = point + vector * OoXmlConstants.MultipleBondOffsetPercentage;
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
                foreach (KeyValuePair<string, List<Point>> hull in _outputs.ConvexHulls)
                {
                    List<Point> points = hull.Value.ToList();
                    DrawPolygon(points, true, OoXmlColours.Red, 0.25);
                }
            }

            // Finally draw any debugging diagnostics
            foreach (DiagnosticLine line in _outputs.Diagnostics.Lines)
            {
                DrawBondLine(line.Start, line.End, "", line.Style, line.Colour, 0.5);
            }

            foreach (DiagnosticPolygon polygon in _outputs.Diagnostics.Polygons)
            {
                DrawPolygon(polygon.Points, true, polygon.Colour, 0.33);
            }

            foreach (DiagnosticSpot spot in _outputs.Diagnostics.Points)
            {
                double half = spot.Diameter / 2;
                Rect extents = new Rect(new Point(spot.Point.X - half, spot.Point.Y - half),
                                        new Point(spot.Point.X + half, spot.Point.Y + half));
                DrawShape(extents, A.ShapeTypeValues.Ellipse, spot.Filled, spot.Colour);
            }
        }

        private double BondOffset()
            => _meanBondLength * OoXmlConstants.MultipleBondOffsetPercentage;

        private A.CustomGeometry CreateCustomGeometry(A.PathList pathList)
        {
            A.CustomGeometry customGeometry = new A.CustomGeometry();
            A.AdjustValueList adjustValueList = new A.AdjustValueList();
            A.Rectangle rectangle = new A.Rectangle { Left = "l", Top = "t", Right = "r", Bottom = "b" };
            customGeometry.Append(adjustValueList);
            customGeometry.Append(rectangle);
            customGeometry.Append(pathList);
            return customGeometry;
        }

        private void DrawCharacter(AtomLabelCharacter alc)
        {
            Point characterPosition = new Point(alc.Position.X, alc.Position.Y);
            characterPosition.Offset(-_boundingBoxOfEverything.Left, -_boundingBoxOfEverything.Top);

            Int64Value emuWidth = OoXmlHelper.ScaleCsTtfToEmu(alc.Character.Width, _meanBondLength);
            Int64Value emuHeight = OoXmlHelper.ScaleCsTtfToEmu(alc.Character.Height, _meanBondLength);
            if (alc.IsSmaller)
            {
                emuWidth = OoXmlHelper.ScaleCsTtfSubScriptToEmu(alc.Character.Width, _meanBondLength);
                emuHeight = OoXmlHelper.ScaleCsTtfSubScriptToEmu(alc.Character.Height, _meanBondLength);
            }
            Int64Value emuTop = OoXmlHelper.ScaleCmlToEmu(characterPosition.Y);
            Int64Value emuLeft = OoXmlHelper.ScaleCmlToEmu(characterPosition.X);

            string parent = alc.ParentAtom.Equals(alc.ParentMolecule) ? alc.ParentMolecule : alc.ParentAtom;
            string shapeName = $"Character {alc.Character.Character} of {parent}";
            Wps.WordprocessingShape wordprocessingShape = CreateShape(_ooxmlId++, shapeName);
            Wps.ShapeProperties shapeProperties = CreateShapeProperties(wordprocessingShape, emuTop, emuLeft, emuWidth, emuHeight);

            // Start of the lines

            A.PathList pathList = new A.PathList();

            A.Path path = new A.Path { Width = emuWidth, Height = emuHeight };

            foreach (TtfContour contour in alc.Character.Contours)
            {
                int i = 0;

                while (i < contour.Points.Count)
                {
                    TtfPoint thisPoint = contour.Points[i];
                    TtfPoint nextPoint = null;
                    if (i < contour.Points.Count - 1)
                    {
                        nextPoint = contour.Points[i + 1];
                    }

                    switch (thisPoint.Type)
                    {
                        case TtfPoint.PointType.Start:
                            A.MoveTo moveTo = new A.MoveTo();
                            if (alc.IsSmaller)
                            {
                                A.Point point = MakeSubscriptPoint(thisPoint);
                                moveTo.Append(point);
                                path.Append(moveTo);
                            }
                            else
                            {
                                A.Point point = MakeNormalPoint(thisPoint);
                                moveTo.Append(point);
                                path.Append(moveTo);
                            }
                            i++;
                            break;

                        case TtfPoint.PointType.Line:
                            A.LineTo lineTo = new A.LineTo();
                            if (alc.IsSmaller)
                            {
                                A.Point point = MakeSubscriptPoint(thisPoint);
                                lineTo.Append(point);
                                path.Append(lineTo);
                            }
                            else
                            {
                                A.Point point = MakeNormalPoint(thisPoint);
                                lineTo.Append(point);
                                path.Append(lineTo);
                            }
                            i++;
                            break;

                        case TtfPoint.PointType.CurveOff:
                            A.QuadraticBezierCurveTo quadraticBezierCurveTo = new A.QuadraticBezierCurveTo();
                            if (alc.IsSmaller)
                            {
                                A.Point pointA = MakeSubscriptPoint(thisPoint);
                                A.Point pointB = MakeSubscriptPoint(nextPoint);
                                quadraticBezierCurveTo.Append(pointA);
                                quadraticBezierCurveTo.Append(pointB);
                                path.Append(quadraticBezierCurveTo);
                            }
                            else
                            {
                                A.Point pointA = MakeNormalPoint(thisPoint);
                                A.Point pointB = MakeNormalPoint(nextPoint);
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

                A.CloseShapePath closeShapePath = new A.CloseShapePath();
                path.Append(closeShapePath);
            }

            pathList.Append(path);

            // End of the lines

            A.SolidFill solidFill = new A.SolidFill();

            // Set Colour
            A.RgbColorModelHex rgbColorModelHex = new A.RgbColorModelHex { Val = alc.Colour };
            solidFill.Append(rgbColorModelHex);

            shapeProperties.Append(CreateCustomGeometry(pathList));
            shapeProperties.Append(solidFill);

            wordprocessingShape.Append(CreateShapeStyle());

            Wps.TextBodyProperties textBodyProperties = new Wps.TextBodyProperties();
            wordprocessingShape.Append(textBodyProperties);

            _wordprocessingGroup.Append(wordprocessingShape);

            // Local Functions
            A.Point MakeSubscriptPoint(TtfPoint ttfPoint)
            {
                A.Point pp = new A.Point
                             {
                                 X = $"{OoXmlHelper.ScaleCsTtfSubScriptToEmu(ttfPoint.X - alc.Character.OriginX, _meanBondLength)}",
                                 Y = $"{OoXmlHelper.ScaleCsTtfSubScriptToEmu(alc.Character.Height + ttfPoint.Y - (alc.Character.Height + alc.Character.OriginY), _meanBondLength)}"
                             };
                return pp;
            }

            A.Point MakeNormalPoint(TtfPoint ttfPoint)
            {
                A.Point pp = new A.Point
                             {
                                 X = $"{OoXmlHelper.ScaleCsTtfToEmu(ttfPoint.X - alc.Character.OriginX, _meanBondLength)}",
                                 Y = $"{OoXmlHelper.ScaleCsTtfToEmu(alc.Character.Height + ttfPoint.Y - (alc.Character.Height + alc.Character.OriginY), _meanBondLength)}"
                             };
                return pp;
            }
        }

        private List<SimpleLine> CreateHatchLines(List<Point> points)
        {
            List<SimpleLine> lines = new List<SimpleLine>();

            Point wedgeStart = points[0];
            Point wedgeEndMiddle = points[2];

            // Vector pointing from wedgeStart to wedgeEndMiddle
            Vector direction = wedgeEndMiddle - wedgeStart;
            Matrix rightAngles = new Matrix();
            rightAngles.Rotate(90);
            Vector perpendicular = direction * rightAngles;

            Vector step = direction;
            step.Normalize();
            step *= OoXmlHelper.ScaleCmlToEmu(15 * OoXmlConstants.MultipleBondOffsetPercentage);

            int steps = (int)Math.Ceiling(direction.Length / step.Length);
            double stepLength = direction.Length / steps;

            step.Normalize();
            step *= stepLength;

            Point p0 = wedgeStart + step;
            Point p1 = p0 + perpendicular;
            Point p2 = p0 - perpendicular;

            Point[] r = GeometryTool.ClipLineWithPolygon(p1, p2, points, out _);
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
            Run run = new Run();

            Drawing drawing = new Drawing();
            run.Append(drawing);

            Wp.Inline inline = new Wp.Inline
                               {
                                   DistanceFromTop = (UInt32Value)0U,
                                   DistanceFromLeft = (UInt32Value)0U,
                                   DistanceFromBottom = (UInt32Value)0U,
                                   DistanceFromRight = (UInt32Value)0U
                               };
            drawing.Append(inline);

            Int64Value width = OoXmlHelper.ScaleCmlToEmu(_boundingBoxOfEverything.Width);
            Int64Value height = OoXmlHelper.ScaleCmlToEmu(_boundingBoxOfEverything.Height);

            Wp.Extent extent = new Wp.Extent { Cx = width, Cy = height };

            Wp.EffectExtent effectExtent = new Wp.EffectExtent
                                           {
                                               TopEdge = 0L,
                                               LeftEdge = 0L,
                                               BottomEdge = 0L,
                                               RightEdge = 0L
                                           };

            inline.Append(extent);
            inline.Append(effectExtent);

            UInt32Value inlineId = UInt32Value.FromUInt32((uint)_ooxmlId);
            Wp.DocProperties docProperties = new Wp.DocProperties
                                             {
                                                 Id = inlineId,
                                                 Name = "Chem4Word Structure"
                                             };

            inline.Append(docProperties);

            A.Graphic graphic = new A.Graphic();
            graphic.AddNamespaceDeclaration("a", "http://schemas.openxmlformats.org/drawingml/2006/main");

            inline.Append(graphic);

            A.GraphicData graphicData = new A.GraphicData
                                        {
                                            Uri = "http://schemas.microsoft.com/office/word/2010/wordprocessingGroup"
                                        };

            graphic.Append(graphicData);

            _wordprocessingGroup = new Wpg.WordprocessingGroup();
            graphicData.Append(_wordprocessingGroup);

            Wpg.NonVisualGroupDrawingShapeProperties nonVisualGroupDrawingShapeProperties = new Wpg.NonVisualGroupDrawingShapeProperties();

            Wpg.GroupShapeProperties groupShapeProperties = new Wpg.GroupShapeProperties();

            A.TransformGroup transformGroup = new A.TransformGroup();
            A.Offset offset = new A.Offset { X = 0L, Y = 0L };
            A.Extents extents = new A.Extents { Cx = width, Cy = height };
            A.ChildOffset childOffset = new A.ChildOffset { X = 0L, Y = 0L };
            A.ChildExtents childExtents = new A.ChildExtents { Cx = width, Cy = height };

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
            UInt32Value id32 = UInt32Value.FromUInt32((uint)id);
            Wps.WordprocessingShape wordprocessingShape = new Wps.WordprocessingShape();
            Wps.NonVisualDrawingProperties nonVisualDrawingProperties = new Wps.NonVisualDrawingProperties { Id = id32, Name = shapeName };
            Wps.NonVisualDrawingShapeProperties nonVisualDrawingShapeProperties = new Wps.NonVisualDrawingShapeProperties();

            wordprocessingShape.Append(nonVisualDrawingProperties);
            wordprocessingShape.Append(nonVisualDrawingShapeProperties);

            return wordprocessingShape;
        }

        private Wps.ShapeProperties CreateShapeProperties(Wps.WordprocessingShape wordprocessingShape,
                                                          Int64Value emuTop, Int64Value emuLeft, Int64Value emuWidth, Int64Value emuHeight)
        {
            Wps.ShapeProperties shapeProperties = new Wps.ShapeProperties();

            wordprocessingShape.Append(shapeProperties);

            A.Transform2D transform2D = new A.Transform2D();
            A.Offset offset = new A.Offset { X = emuLeft, Y = emuTop };
            A.Extents extents = new A.Extents { Cx = emuWidth, Cy = emuHeight };
            transform2D.Append(offset);
            transform2D.Append(extents);
            shapeProperties.Append(transform2D);

            return shapeProperties;
        }

        private Wps.ShapeStyle CreateShapeStyle()
        {
            Wps.ShapeStyle shapeStyle = new Wps.ShapeStyle();
            A.LineReference lineReference = new A.LineReference { Index = (UInt32Value)0U };
            A.FillReference fillReference = new A.FillReference { Index = (UInt32Value)0U };
            A.EffectReference effectReference = new A.EffectReference { Index = (UInt32Value)0U };
            A.FontReference fontReference = new A.FontReference { Index = A.FontCollectionIndexValues.Minor };

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
                Int64Value emuWidth = OoXmlHelper.ScaleCmlToEmu(cmlExtents.Width);
                Int64Value emuHeight = OoXmlHelper.ScaleCmlToEmu(cmlExtents.Height);
                Int64Value emuTop = OoXmlHelper.ScaleCmlToEmu(cmlExtents.Top);
                Int64Value emuLeft = OoXmlHelper.ScaleCmlToEmu(cmlExtents.Left);

                Point location = new Point(emuLeft, emuTop);
                Size size = new Size(emuWidth, emuHeight);
                location.Offset(OoXmlHelper.ScaleCmlToEmu(-_boundingBoxOfEverything.Left), OoXmlHelper.ScaleCmlToEmu(-_boundingBoxOfEverything.Top));
                Rect boundingBox = new Rect(location, size);

                emuWidth = (Int64Value)boundingBox.Width;
                emuHeight = (Int64Value)boundingBox.Height;
                emuTop = (Int64Value)boundingBox.Top;
                emuLeft = (Int64Value)boundingBox.Left;

                string shapeName = "Box " + _ooxmlId++;

                Wps.WordprocessingShape wordprocessingShape = CreateShape(_ooxmlId, shapeName);
                Wps.ShapeProperties shapeProperties = CreateShapeProperties(wordprocessingShape, emuTop, emuLeft, emuWidth, emuHeight);

                // Start of the lines

                A.PathList pathList = new A.PathList();

                A.Path path = new A.Path { Width = emuWidth, Height = emuHeight };

                // Starting Point
                A.MoveTo moveTo = new A.MoveTo();
                A.Point point1 = new A.Point { X = "0", Y = "0" };
                moveTo.Append(point1);

                // Mid Point
                A.LineTo lineTo1 = new A.LineTo();
                A.Point point2 = new A.Point { X = boundingBox.Width.ToString("0"), Y = "0" };
                lineTo1.Append(point2);

                // Mid Point
                A.LineTo lineTo2 = new A.LineTo();
                A.Point point3 = new A.Point { X = boundingBox.Width.ToString("0"), Y = boundingBox.Height.ToString("0") };
                lineTo2.Append(point3);

                // Last Point
                A.LineTo lineTo3 = new A.LineTo();
                A.Point point4 = new A.Point { X = "0", Y = boundingBox.Height.ToString("0") };
                lineTo3.Append(point4);

                // Back to Start Point
                A.LineTo lineTo4 = new A.LineTo();
                A.Point point5 = new A.Point { X = "0", Y = "0" };
                lineTo4.Append(point5);

                path.Append(moveTo);
                path.Append(lineTo1);
                path.Append(lineTo2);
                path.Append(lineTo3);
                path.Append(lineTo4);

                pathList.Append(path);

                // End of the lines

                shapeProperties.Append(CreateCustomGeometry(pathList));

                Int32Value emuLineWidth = (Int32Value)(lineWidth * OoXmlConstants.EmusPerWordPoint);
                A.Outline outline = new A.Outline { Width = emuLineWidth, CapType = A.LineCapValues.Round };

                A.SolidFill solidFill = new A.SolidFill();
                A.RgbColorModelHex rgbColorModelHex = new A.RgbColorModelHex { Val = lineColour };
                solidFill.Append(rgbColorModelHex);
                outline.Append(solidFill);

                shapeProperties.Append(outline);

                wordprocessingShape.Append(CreateShapeStyle());

                Wps.TextBodyProperties textBodyProperties = new Wps.TextBodyProperties();
                wordprocessingShape.Append(textBodyProperties);

                _wordprocessingGroup.Append(wordprocessingShape);
            }
        }

        private void DrawFilledBond(List<Point> points, string bondPath,
                                   string colour = OoXmlColours.Black)
        {
            Rect cmlExtents = new Rect(points[0], points[points.Count - 1]);

            for (int i = 0; i < points.Count - 1; i++)
            {
                cmlExtents.Union(new Rect(points[i], points[i + 1]));
            }

            // Move Extents to have 0,0 Top Left Reference
            cmlExtents.Offset(-_boundingBoxOfEverything.Left, -_boundingBoxOfEverything.Top);

            Int64Value emuTop = OoXmlHelper.ScaleCmlToEmu(cmlExtents.Top);
            Int64Value emuLeft = OoXmlHelper.ScaleCmlToEmu(cmlExtents.Left);
            Int64Value emuWidth = OoXmlHelper.ScaleCmlToEmu(cmlExtents.Width);
            Int64Value emuHeight = OoXmlHelper.ScaleCmlToEmu(cmlExtents.Height);

            string shapeName = "Wedge " + bondPath;

            Wps.WordprocessingShape wordprocessingShape = CreateShape(_ooxmlId++, shapeName);
            Wps.ShapeProperties shapeProperties = CreateShapeProperties(wordprocessingShape, emuTop, emuLeft, emuWidth, emuHeight);

            // Start of the lines

            A.PathList pathList = new A.PathList();

            A.Path path = new A.Path { Width = emuWidth, Height = emuHeight };

            A.MoveTo moveTo = new A.MoveTo();
            moveTo.Append(MakePoint(points[0], cmlExtents));
            path.Append(moveTo);

            for (int i = 1; i < points.Count; i++)
            {
                A.LineTo lineTo = new A.LineTo();
                lineTo.Append(MakePoint(points[i], cmlExtents));
                path.Append(lineTo);
            }

            A.CloseShapePath closeShapePath = new A.CloseShapePath();
            path.Append(closeShapePath);

            pathList.Append(path);

            // End of the lines

            shapeProperties.Append(CreateCustomGeometry(pathList));

            // Set shape fill colour
            A.SolidFill insideFill = new A.SolidFill();
            A.RgbColorModelHex rgbColorModelHex = new A.RgbColorModelHex { Val = colour };
            insideFill.Append(rgbColorModelHex);

            shapeProperties.Append(insideFill);

            // Set shape outline colour
            A.Outline outline = new A.Outline { Width = Int32Value.FromInt32((int)OoXmlConstants.AcsLineWidthEmus), CapType = A.LineCapValues.Round };
            A.RgbColorModelHex rgbColorModelHex2 = new A.RgbColorModelHex { Val = colour };
            A.SolidFill outlineFill = new A.SolidFill();
            outlineFill.Append(rgbColorModelHex2);
            outline.Append(outlineFill);

            shapeProperties.Append(outline);

            wordprocessingShape.Append(CreateShapeStyle());

            Wps.TextBodyProperties textBodyProperties = new Wps.TextBodyProperties();
            wordprocessingShape.Append(textBodyProperties);

            _wordprocessingGroup.Append(wordprocessingShape);
        }

        private void DrawGroupBrackets(Rect cmlExtents, double armLength, double lineWidth, string lineColour)
        {
            if (cmlExtents != Rect.Empty)
            {
                Int64Value emuWidth = OoXmlHelper.ScaleCmlToEmu(cmlExtents.Width);
                Int64Value emuHeight = OoXmlHelper.ScaleCmlToEmu(cmlExtents.Height);
                Int64Value emuTop = OoXmlHelper.ScaleCmlToEmu(cmlExtents.Top);
                Int64Value emuLeft = OoXmlHelper.ScaleCmlToEmu(cmlExtents.Left);

                Point location = new Point(emuLeft, emuTop);
                Size size = new Size(emuWidth, emuHeight);
                location.Offset(OoXmlHelper.ScaleCmlToEmu(-_boundingBoxOfEverything.Left), OoXmlHelper.ScaleCmlToEmu(-_boundingBoxOfEverything.Top));
                Rect boundingBox = new Rect(location, size);
                Int64Value armLengthEmu = OoXmlHelper.ScaleCmlToEmu(armLength);

                emuWidth = (Int64Value)boundingBox.Width;
                emuHeight = (Int64Value)boundingBox.Height;
                emuTop = (Int64Value)boundingBox.Top;
                emuLeft = (Int64Value)boundingBox.Left;

                string shapeName = "Box " + _ooxmlId++;

                Wps.WordprocessingShape wordprocessingShape = CreateShape(_ooxmlId, shapeName);
                Wps.ShapeProperties shapeProperties = CreateShapeProperties(wordprocessingShape, emuTop, emuLeft, emuWidth, emuHeight);

                // Start of the lines

                A.PathList pathList = new A.PathList();

                pathList.Append(MakeCorner(boundingBox, "TopLeft", armLengthEmu));
                pathList.Append(MakeCorner(boundingBox, "TopRight", armLengthEmu));
                pathList.Append(MakeCorner(boundingBox, "BottomLeft", armLengthEmu));
                pathList.Append(MakeCorner(boundingBox, "BottomRight", armLengthEmu));

                // End of the lines

                shapeProperties.Append(CreateCustomGeometry(pathList));

                Int32Value emuLineWidth = (Int32Value)(lineWidth * OoXmlConstants.EmusPerWordPoint);
                A.Outline outline = new A.Outline { Width = emuLineWidth, CapType = A.LineCapValues.Round };

                A.SolidFill solidFill = new A.SolidFill();
                A.RgbColorModelHex rgbColorModelHex = new A.RgbColorModelHex { Val = lineColour };
                solidFill.Append(rgbColorModelHex);
                outline.Append(solidFill);

                shapeProperties.Append(outline);

                wordprocessingShape.Append(CreateShapeStyle());

                Wps.TextBodyProperties textBodyProperties = new Wps.TextBodyProperties();
                wordprocessingShape.Append(textBodyProperties);

                _wordprocessingGroup.Append(wordprocessingShape);

                // Local function
                A.Path MakeCorner(Rect bbRect, string corner, double armsSize)
                {
                    A.Path path = new A.Path { Width = (Int64Value)bbRect.Width, Height = (Int64Value)bbRect.Height };

                    A.Point p0 = new A.Point();
                    A.Point p1 = new A.Point();
                    A.Point p2 = new A.Point();

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

                    A.MoveTo moveTo = new A.MoveTo();
                    moveTo.Append(p0);
                    path.Append(moveTo);

                    A.LineTo lineTo1 = new A.LineTo();
                    lineTo1.Append(p1);
                    path.Append(lineTo1);

                    A.LineTo lineTo2 = new A.LineTo();
                    lineTo2.Append(p2);
                    path.Append(lineTo2);

                    return path;
                }
            }
        }

        private void DrawHatchBond(List<Point> points, string bondPath,
                                   string colour = OoXmlColours.Black)
        {
            Rect cmlExtents = new Rect(points[0], points[points.Count - 1]);

            for (int i = 0; i < points.Count - 1; i++)
            {
                cmlExtents.Union(new Rect(points[i], points[i + 1]));
            }

            // Move Extents to have 0,0 Top Left Reference
            cmlExtents.Offset(-_boundingBoxOfEverything.Left, -_boundingBoxOfEverything.Top);

            Int64Value emuTop = OoXmlHelper.ScaleCmlToEmu(cmlExtents.Top);
            Int64Value emuLeft = OoXmlHelper.ScaleCmlToEmu(cmlExtents.Left);
            Int64Value emuWidth = OoXmlHelper.ScaleCmlToEmu(cmlExtents.Width);
            Int64Value emuHeight = OoXmlHelper.ScaleCmlToEmu(cmlExtents.Height);

            string shapeName = "Hatch " + bondPath;

            Wps.WordprocessingShape wordprocessingShape = CreateShape(_ooxmlId++, shapeName);
            Wps.ShapeProperties shapeProperties = CreateShapeProperties(wordprocessingShape, emuTop, emuLeft, emuWidth, emuHeight);

            // Start of the lines

            A.PathList pathList = new A.PathList();

            // Draw a small circle for the starting point
            double xx = 0.5;
            Rect extents = new Rect(new Point(points[0].X - xx, points[0].Y - xx), new Point(points[0].X + xx, points[0].Y + xx));
            DrawShape(extents, A.ShapeTypeValues.Ellipse, true, colour);

            // Pre offset and scale the extents
            List<Point> scaledPoints = new List<Point>();
            foreach (Point point in points)
            {
                point.Offset(-_boundingBoxOfEverything.Left, -_boundingBoxOfEverything.Top);
                point.Offset(-cmlExtents.Left, -cmlExtents.Top);
                scaledPoints.Add(new Point(OoXmlHelper.ScaleCmlToEmu(point.X), OoXmlHelper.ScaleCmlToEmu(point.Y)));
            }

            List<SimpleLine> lines = CreateHatchLines(scaledPoints);

            foreach (SimpleLine line in lines)
            {
                A.Path path = new A.Path { Width = emuWidth, Height = emuHeight };

                A.MoveTo moveTo = new A.MoveTo();
                A.Point startPoint = new A.Point
                                     {
                                         X = line.Start.X.ToString("0"),
                                         Y = line.Start.Y.ToString("0")
                                     };

                moveTo.Append(startPoint);
                path.Append(moveTo);

                A.LineTo lineTo = new A.LineTo();
                A.Point endPoint = new A.Point
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
            A.SolidFill insideFill = new A.SolidFill();
            A.RgbColorModelHex rgbColorModelHex = new A.RgbColorModelHex { Val = colour };
            insideFill.Append(rgbColorModelHex);

            shapeProperties.Append(insideFill);

            // Set shape outline colour
            A.Outline outline = new A.Outline { Width = Int32Value.FromInt32((int)OoXmlConstants.AcsLineWidthEmus), CapType = A.LineCapValues.Round };
            A.RgbColorModelHex rgbColorModelHex2 = new A.RgbColorModelHex { Val = colour };
            A.SolidFill outlineFill = new A.SolidFill();
            outlineFill.Append(rgbColorModelHex2);
            outline.Append(outlineFill);

            shapeProperties.Append(outline);

            wordprocessingShape.Append(CreateShapeStyle());

            Wps.TextBodyProperties textBodyProperties = new Wps.TextBodyProperties();
            wordprocessingShape.Append(textBodyProperties);

            _wordprocessingGroup.Append(wordprocessingShape);
        }

        private void DrawInnerCircle(InnerCircle innerCircle, string lineColour, double lineWidth)
        {
            Rect cmlExtents = new Rect(innerCircle.Points[0], innerCircle.Points[innerCircle.Points.Count - 1]);

            for (int i = 0; i < innerCircle.Points.Count - 1; i++)
            {
                cmlExtents.Union(new Rect(innerCircle.Points[i], innerCircle.Points[i + 1]));
            }

            // Move Extents to have 0,0 Top Left Reference
            cmlExtents.Offset(-_boundingBoxOfEverything.Left, -_boundingBoxOfEverything.Top);

            Int64Value emuTop = OoXmlHelper.ScaleCmlToEmu(cmlExtents.Top);
            Int64Value emuLeft = OoXmlHelper.ScaleCmlToEmu(cmlExtents.Left);
            Int64Value emuWidth = OoXmlHelper.ScaleCmlToEmu(cmlExtents.Width);
            Int64Value emuHeight = OoXmlHelper.ScaleCmlToEmu(cmlExtents.Height);

            long id = _ooxmlId++;
            string shapeName = "InnerCircle " + id;

            List<Point> allpoints = new List<Point>();

            Point startingPoint = GeometryTool.GetMidPoint(innerCircle.Points[innerCircle.Points.Count - 1], innerCircle.Points[0]);
            Point leftPoint = startingPoint;
            Point middlePoint = innerCircle.Points[0];
            Point rightPoint = GeometryTool.GetMidPoint(innerCircle.Points[0], innerCircle.Points[1]);
            allpoints.Add(leftPoint);
            allpoints.Add(middlePoint);
            allpoints.Add(rightPoint);

            for (int i = 1; i < innerCircle.Points.Count - 1; i++)
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

            Wps.WordprocessingShape wordprocessingShape = CreateShape(id, shapeName);
            Wps.ShapeProperties shapeProperties = CreateShapeProperties(wordprocessingShape, emuTop, emuLeft, emuWidth, emuHeight);

            // Start of the lines

            A.PathList pathList = new A.PathList();

            A.Path path = new A.Path { Width = emuWidth, Height = emuHeight };

            // First point
            A.MoveTo moveTo = new A.MoveTo();
            moveTo.Append(MakePoint(startingPoint, cmlExtents));
            path.Append(moveTo);

            // Create the Curved Lines
            for (int i = 0; i < allpoints.Count; i += 3)
            {
                A.CubicBezierCurveTo cubicBezierCurveTo = new A.CubicBezierCurveTo();

                for (int j = 0; j < 3; j++)
                {
                    A.Point curvePoint = MakePoint(allpoints[i + j], cmlExtents);
                    cubicBezierCurveTo.Append(curvePoint);
                }
                path.Append(cubicBezierCurveTo);
            }

            pathList.Append(path);

            // End of the lines

            Int32Value emuLineWidth = (Int32Value)(lineWidth * OoXmlConstants.EmusPerWordPoint);
            A.Outline outline = new A.Outline { Width = emuLineWidth, CapType = A.LineCapValues.Round };

            A.SolidFill solidFill = new A.SolidFill();
            A.RgbColorModelHex rgbColorModelHex = new A.RgbColorModelHex { Val = lineColour };
            solidFill.Append(rgbColorModelHex);
            outline.Append(solidFill);

            shapeProperties.Append(CreateCustomGeometry(pathList));
            shapeProperties.Append(outline);

            wordprocessingShape.Append(CreateShapeStyle());

            Wps.TextBodyProperties textBodyProperties = new Wps.TextBodyProperties();
            wordprocessingShape.Append(textBodyProperties);

            _wordprocessingGroup.Append(wordprocessingShape);
        }

        private void DrawMoleculeBrackets(Rect cmlExtents, double lineWidth, string lineColour)
        {
            Int64Value emuWidth = OoXmlHelper.ScaleCmlToEmu(cmlExtents.Width);
            Int64Value emuHeight = OoXmlHelper.ScaleCmlToEmu(cmlExtents.Height);
            Int64Value emuTop = OoXmlHelper.ScaleCmlToEmu(cmlExtents.Top);
            Int64Value emuLeft = OoXmlHelper.ScaleCmlToEmu(cmlExtents.Left);

            Point location = new Point(emuLeft, emuTop);
            Size size = new Size(emuWidth, emuHeight);
            location.Offset(OoXmlHelper.ScaleCmlToEmu(-_boundingBoxOfEverything.Left), OoXmlHelper.ScaleCmlToEmu(-_boundingBoxOfEverything.Top));
            Rect boundingBox = new Rect(location, size);

            emuWidth = (Int64Value)boundingBox.Width;
            emuHeight = (Int64Value)boundingBox.Height;
            emuTop = (Int64Value)boundingBox.Top;
            emuLeft = (Int64Value)boundingBox.Left;

            string shapeName = "Box " + _ooxmlId++;

            Wps.WordprocessingShape wordprocessingShape = CreateShape(_ooxmlId, shapeName);
            Wps.ShapeProperties shapeProperties = CreateShapeProperties(wordprocessingShape, emuTop, emuLeft, emuWidth, emuHeight);

            // Start of the lines

            A.PathList pathList = new A.PathList();

            double gap = boundingBox.Width * 0.8;
            double leftSide = (emuWidth - gap) / 2;
            double rightSide = emuWidth - leftSide;

            // Left Path
            A.Path path1 = new A.Path { Width = emuWidth, Height = emuHeight };

            A.MoveTo moveTo = new A.MoveTo();
            A.Point point1 = new A.Point { X = leftSide.ToString("0"), Y = "0" };
            moveTo.Append(point1);

            // Mid Point
            A.LineTo lineTo1 = new A.LineTo();
            A.Point point2 = new A.Point { X = "0", Y = "0" };
            lineTo1.Append(point2);

            // Last Point
            A.LineTo lineTo2 = new A.LineTo();
            A.Point point3 = new A.Point { X = "0", Y = boundingBox.Height.ToString("0") };
            lineTo2.Append(point3);

            // Mid Point
            A.LineTo lineTo3 = new A.LineTo();
            A.Point point4 = new A.Point { X = leftSide.ToString("0"), Y = boundingBox.Height.ToString("0") };
            lineTo3.Append(point4);

            path1.Append(moveTo);
            path1.Append(lineTo1);
            path1.Append(lineTo2);
            path1.Append(lineTo3);

            pathList.Append(path1);

            // Right Path
            A.Path path2 = new A.Path { Width = emuWidth, Height = emuHeight };

            A.MoveTo moveTo2 = new A.MoveTo();
            A.Point point5 = new A.Point { X = rightSide.ToString("0"), Y = "0" };
            moveTo2.Append(point5);

            // Mid Point
            A.LineTo lineTo4 = new A.LineTo();
            A.Point point6 = new A.Point { X = boundingBox.Width.ToString("0"), Y = "0" };
            lineTo4.Append(point6);

            // Last Point
            A.LineTo lineTo5 = new A.LineTo();
            A.Point point7 = new A.Point { X = boundingBox.Width.ToString("0"), Y = boundingBox.Height.ToString("0") };
            lineTo5.Append(point7);

            // Mid Point
            A.LineTo lineTo6 = new A.LineTo();
            A.Point point8 = new A.Point { X = rightSide.ToString("0"), Y = boundingBox.Height.ToString("0") };
            lineTo6.Append(point8);

            path2.Append(moveTo2);
            path2.Append(lineTo4);
            path2.Append(lineTo5);
            path2.Append(lineTo6);

            pathList.Append(path2);

            // End of the lines

            shapeProperties.Append(CreateCustomGeometry(pathList));

            Int32Value emuLineWidth = (Int32Value)(lineWidth * OoXmlConstants.EmusPerWordPoint);
            A.Outline outline = new A.Outline { Width = emuLineWidth, CapType = A.LineCapValues.Round };

            A.SolidFill solidFill = new A.SolidFill();
            A.RgbColorModelHex rgbColorModelHex = new A.RgbColorModelHex { Val = lineColour };
            solidFill.Append(rgbColorModelHex);
            outline.Append(solidFill);

            shapeProperties.Append(outline);

            wordprocessingShape.Append(CreateShapeStyle());

            Wps.TextBodyProperties textBodyProperties = new Wps.TextBodyProperties();
            wordprocessingShape.Append(textBodyProperties);

            _wordprocessingGroup.Append(wordprocessingShape);
        }

        private void DrawPolygon(List<Point> points, bool isClosed, string lineColour, double lineWidth)
        {
            Rect cmlExtents = new Rect(points[0], points[points.Count - 1]);

            for (int i = 0; i < points.Count - 1; i++)
            {
                cmlExtents.Union(new Rect(points[i], points[i + 1]));
            }

            // Move Extents to have 0,0 Top Left Reference
            cmlExtents.Offset(-_boundingBoxOfEverything.Left, -_boundingBoxOfEverything.Top);

            Int64Value emuTop = OoXmlHelper.ScaleCmlToEmu(cmlExtents.Top);
            Int64Value emuLeft = OoXmlHelper.ScaleCmlToEmu(cmlExtents.Left);
            Int64Value emuWidth = OoXmlHelper.ScaleCmlToEmu(cmlExtents.Width);
            Int64Value emuHeight = OoXmlHelper.ScaleCmlToEmu(cmlExtents.Height);

            long id = _ooxmlId++;
            string shapeName = "Polygon " + id;

            Wps.WordprocessingShape wordprocessingShape = CreateShape(id, shapeName);
            Wps.ShapeProperties shapeProperties = CreateShapeProperties(wordprocessingShape, emuTop, emuLeft, emuWidth, emuHeight);

            // Start of the lines

            A.PathList pathList = new A.PathList();

            A.Path path = new A.Path { Width = emuWidth, Height = emuHeight };

            // First point
            A.MoveTo moveTo = new A.MoveTo();
            moveTo.Append(MakePoint(points[0], cmlExtents));
            path.Append(moveTo);

            // Remaining points
            for (int i = 1; i < points.Count; i++)
            {
                A.LineTo lineTo = new A.LineTo();
                lineTo.Append(MakePoint(points[i], cmlExtents));
                path.Append(lineTo);
            }

            if (isClosed)
            {
                // Close the path
                A.CloseShapePath closeShapePath = new A.CloseShapePath();
                path.Append(closeShapePath);
            }

            pathList.Append(path);

            // End of the lines

            Int32Value emuLineWidth = (Int32Value)(lineWidth * OoXmlConstants.EmusPerWordPoint);
            A.Outline outline = new A.Outline { Width = emuLineWidth, CapType = A.LineCapValues.Round };

            A.SolidFill solidFill = new A.SolidFill();
            A.RgbColorModelHex rgbColorModelHex = new A.RgbColorModelHex { Val = lineColour };
            solidFill.Append(rgbColorModelHex);
            outline.Append(solidFill);

            shapeProperties.Append(CreateCustomGeometry(pathList));
            shapeProperties.Append(outline);

            wordprocessingShape.Append(CreateShapeStyle());

            Wps.TextBodyProperties textBodyProperties = new Wps.TextBodyProperties();
            wordprocessingShape.Append(textBodyProperties);

            _wordprocessingGroup.Append(wordprocessingShape);
        }

        private void DrawRetrosyntheticArrow(Reaction reaction)
        {
            Vector vector = reaction.HeadPoint - reaction.TailPoint;
            Vector perpendicular = vector.Perpendicular();
            perpendicular.Normalize();
            Vector offset = perpendicular * BondOffset() / 2;
            Point topLeft = reaction.TailPoint - offset;
            Point topRight = reaction.HeadPoint - offset;
            Point bottomLeft = reaction.TailPoint + offset;
            Point bottomRight = reaction.HeadPoint + offset;

            Vector barb = offset * 4;
            Matrix rotator = new Matrix();
            rotator.Rotate(-45);
            Point topHeadEnd = reaction.HeadPoint - barb * rotator;
            rotator.Rotate(+90);
            Point bottomHeadEnd = reaction.HeadPoint + barb * rotator;

            Point? topCrossing = GeometryTool.GetIntersection(topLeft, topRight, reaction.HeadPoint, topHeadEnd);
            Point? bottomCrossing = GeometryTool.GetIntersection(bottomLeft, bottomRight, reaction.HeadPoint, bottomHeadEnd);

            if (topCrossing != null && bottomCrossing != null)
            {
                // Drawing as a polygon to avoid nasty crossing points at topCrossing and bottomCrossing
                List<Point> points = new List<Point>
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
            SimpleLine simpleLine = new SimpleLine(reaction.TailPoint, reaction.HeadPoint);
            SimpleLine reversibleTopLine = simpleLine.GetParallel(BondOffset() / 2);
            SimpleLine reversibleBottomLine = simpleLine.GetParallel(-BondOffset() / 2);

            Point p1 = reversibleTopLine.Start;
            Point p2 = reversibleTopLine.End;

            Point p3 = reversibleBottomLine.End;
            Point p4 = reversibleBottomLine.Start;

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
                Vector barbVector = tailPoint - headPoint;
                barbVector.Normalize();
                barbVector *= BondOffset();

                Matrix rotator = new Matrix();
                rotator.Rotate(45);
                barbVector *= rotator;

                Point result = headPoint + barbVector;

                return result;
            }
        }

        private void DrawRadicalDot(Point position, string path, double diameter, string colour, bool filled)
        {
            Rect extents = new Rect(new Point(position.X - diameter / 2, position.Y - diameter / 2),
                                    new Point(position.X + diameter / 2, position.Y + diameter / 2));
            DrawShape(extents, A.ShapeTypeValues.Ellipse, filled, colour, path: path);
        }

        private void DrawShape(Rect cmlExtents, A.ShapeTypeValues shape, bool filled, string colour,
                               double outlineWidth = OoXmlConstants.AcsLineWidth, string path = "")
        {
            Int64Value emuWidth = OoXmlHelper.ScaleCmlToEmu(cmlExtents.Width);
            Int64Value emuHeight = OoXmlHelper.ScaleCmlToEmu(cmlExtents.Height);
            Int64Value emuTop = OoXmlHelper.ScaleCmlToEmu(cmlExtents.Top);
            Int64Value emuLeft = OoXmlHelper.ScaleCmlToEmu(cmlExtents.Left);

            Point location = new Point(emuLeft, emuTop);
            Size size = new Size(emuWidth, emuHeight);
            location.Offset(OoXmlHelper.ScaleCmlToEmu(-_boundingBoxOfEverything.Left), OoXmlHelper.ScaleCmlToEmu(-_boundingBoxOfEverything.Top));
            Rect boundingBox = new Rect(location, size);

            emuWidth = (Int64Value)boundingBox.Width;
            emuHeight = (Int64Value)boundingBox.Height;
            emuTop = (Int64Value)boundingBox.Top;
            emuLeft = (Int64Value)boundingBox.Left;

            UInt32Value id = UInt32Value.FromUInt32((uint)_ooxmlId++);
            string shapeName = "Shape" + path;
            Wps.WordprocessingShape wordprocessingShape = CreateShape(id, shapeName);

            Wps.ShapeProperties shapeProperties = new Wps.ShapeProperties();

            A.Transform2D transform2D = new A.Transform2D();
            A.Offset offset = new A.Offset { X = emuLeft, Y = emuTop };
            A.Extents extents = new A.Extents { Cx = emuWidth, Cy = emuHeight };
            transform2D.Append(offset);
            transform2D.Append(extents);
            shapeProperties.Append(transform2D);

            A.AdjustValueList adjustValueList = new A.AdjustValueList();
            A.PresetGeometry presetGeometry = new A.PresetGeometry { Preset = shape };
            presetGeometry.Append(adjustValueList);
            shapeProperties.Append(presetGeometry);

            if (filled)
            {
                // Set shape fill colour
                A.SolidFill solidFill = new A.SolidFill();
                A.RgbColorModelHex rgbColorModelHex = new A.RgbColorModelHex { Val = colour };
                solidFill.Append(rgbColorModelHex);
                shapeProperties.Append(solidFill);
            }
            else
            {
                // Set shape outline and colour
                Int32Value emuLineWidth = (Int32Value)(outlineWidth / 2 * OoXmlConstants.EmusPerWordPoint);
                A.Outline outline = new A.Outline { Width = emuLineWidth, CapType = A.LineCapValues.Round };
                A.RgbColorModelHex rgbColorModelHex2 = new A.RgbColorModelHex { Val = colour };
                A.SolidFill outlineFill = new A.SolidFill();
                outlineFill.Append(rgbColorModelHex2);
                outline.Append(outlineFill);
                shapeProperties.Append(outline);
            }

            wordprocessingShape.Append(shapeProperties);
            wordprocessingShape.Append(CreateShapeStyle());

            Wps.TextBodyProperties textBodyProperties = new Wps.TextBodyProperties();
            wordprocessingShape.Append(textBodyProperties);

            _wordprocessingGroup.Append(wordprocessingShape);
        }

        private void DrawElectronPusher(Point startPoint, Point endPoint,
                                        Point firstControlPoint, Point secondControlPoint,
                                        Point hookEndPoint, ElectronPusherType pusherType, string pusherPath)
        {
            List<Point> listOfPoints = new List<Point> { startPoint, firstControlPoint, secondControlPoint, endPoint };
            if (pusherType == ElectronPusherType.FishHook)
            {
                listOfPoints.Add(hookEndPoint);
            }
            OffsetPointsResult offsets = OffsetPoints(listOfPoints);

            Point cmlStartPoint = offsets.Points[0];
            Point cmlFirstPoint = offsets.Points[1];
            Point cmlSecondPoint = offsets.Points[2];
            Point cmlEndPoint = offsets.Points[3];
            Point cmlHookEndPoint = new Point();

            if (pusherType == ElectronPusherType.FishHook)
            {
                cmlHookEndPoint = offsets.Points[4];
            }

            Rect cmlLineExtents = offsets.Extents;

            Int64Value emuTop = OoXmlHelper.ScaleCmlToEmu(cmlLineExtents.Top);
            Int64Value emuLeft = OoXmlHelper.ScaleCmlToEmu(cmlLineExtents.Left);
            Int64Value emuWidth = OoXmlHelper.ScaleCmlToEmu(cmlLineExtents.Width);
            Int64Value emuHeight = OoXmlHelper.ScaleCmlToEmu(cmlLineExtents.Height);

            long id = _ooxmlId++;
            string suffix = string.IsNullOrEmpty(pusherPath) ? id.ToString() : pusherPath;
            string shapeName = "Pusher Arrow " + suffix;

            Wps.WordprocessingShape wordprocessingShape = CreateShape(id, shapeName);
            Wps.ShapeProperties shapeProperties = CreateShapeProperties(wordprocessingShape, emuTop, emuLeft, emuWidth, emuHeight);

            A.PathList pathList = new A.PathList();

            A.Path path = new A.Path { Width = emuWidth, Height = emuHeight };

            A.MoveTo moveTo = new A.MoveTo();
            moveTo.Append(OoXmlPoint(cmlStartPoint));
            path.Append(moveTo);

            A.CubicBezierCurveTo cubicBezierCurveTo = new A.CubicBezierCurveTo();

            AddPointToCurve(cmlFirstPoint);
            AddPointToCurve(cmlSecondPoint);
            AddPointToCurve(cmlEndPoint);

            path.Append(cubicBezierCurveTo);
            pathList.Append(path);

            switch (pusherType)
            {
                case ElectronPusherType.FishHook:
                    A.Path hookPath = new A.Path { Width = emuWidth, Height = emuHeight };

                    A.MoveTo m1 = new A.MoveTo();
                    m1.Append(OoXmlPoint(cmlEndPoint));
                    A.LineTo l1 = new A.LineTo();
                    l1.Append(OoXmlPoint(cmlHookEndPoint));

                    hookPath.Append(m1);
                    hookPath.Append(l1);

                    pathList.Append(hookPath);

                    break;
            }

            double lineWidth = OoXmlConstants.AcsLineWidth;
            Int32Value emuLineWidth = (Int32Value)(lineWidth * OoXmlConstants.EmusPerWordPoint);
            A.Outline outline = new A.Outline { Width = emuLineWidth, CapType = A.LineCapValues.Round };

            A.SolidFill solidFill = new A.SolidFill();
            string colour = OoXmlColours.Black;
            if (_inputs.Model.ShowColouredAtoms)
            {
                colour = OoXmlColours.DarkRed;
            }
            A.RgbColorModelHex rgbColorModelHex = new A.RgbColorModelHex { Val = colour };
            solidFill.Append(rgbColorModelHex);
            outline.Append(solidFill);

            switch (pusherType)
            {
                // When creating all OoXml lines the arrow heads appear to be the wrong way round !
                case ElectronPusherType.CurlyArrow:
                    A.TailEnd curlyArrowHead = MakeTailEnd();
                    outline.Append(curlyArrowHead);
                    break;

                case ElectronPusherType.DoubleArrow:
                    // Note - The OoXml HeadEnd MUST be added before the TailEnd
                    A.HeadEnd doubleArrowTail = MakeHeadEnd();
                    outline.Append(doubleArrowTail);

                    A.TailEnd doubleArrowHead = MakeTailEnd();
                    outline.Append(doubleArrowHead);
                    break;
            }

            shapeProperties.Append(CreateCustomGeometry(pathList));
            shapeProperties.Append(outline);

            wordprocessingShape.Append(CreateShapeStyle());

            Wps.TextBodyProperties textBodyProperties = new Wps.TextBodyProperties();
            wordprocessingShape.Append(textBodyProperties);

            _wordprocessingGroup.Append(wordprocessingShape);

            // Local Functions
            void AddPointToCurve(Point point)
            {
                cubicBezierCurveTo.Append(OoXmlPoint(point));
            }

            // return a pair of arrow end attributes
            (A.LineEndWidthValues width, A.LineEndLengthValues length) GetEndSize(double meanBondLength)
            {
                if (meanBondLength < 33)
                {
                    return (A.LineEndWidthValues.Small, A.LineEndLengthValues.Small);
                }

                if (meanBondLength < 66)
                {
                    return (A.LineEndWidthValues.Medium, A.LineEndLengthValues.Medium);
                }

                //Fallthrough meanBondLength >= 66
                return (A.LineEndWidthValues.Large, A.LineEndLengthValues.Large);
            }

            A.TailEnd MakeTailEnd()
            {
                (A.LineEndWidthValues width, A.LineEndLengthValues length) = GetEndSize(_meanBondLength);
                return new A.TailEnd
                {
                    Type = A.LineEndValues.Triangle,
                    Width = width,
                    Length = length
                };
            }

            A.HeadEnd MakeHeadEnd()
            {
                (A.LineEndWidthValues width, A.LineEndLengthValues length) = GetEndSize(_meanBondLength);
                return new A.HeadEnd
                {
                    Type = A.LineEndValues.Triangle,
                    Width = width,
                    Length = length
                };
            }
        }

        private A.Point OoXmlPoint(Point cmlPoint)
        {
            return new A.Point
            {
                X = OoXmlHelper.ScaleCmlToEmu(cmlPoint.X).ToString(),
                Y = OoXmlHelper.ScaleCmlToEmu(cmlPoint.Y).ToString()
            };
        }

        private void DrawSingleLinedReactionArrow(Reaction reaction)
        {
            List<Point> listOfPoints = new List<Point> { reaction.TailPoint, reaction.HeadPoint };
            OffsetPointsResult offsets = OffsetPoints(listOfPoints);

            Point cmlStartPoint = offsets.Points[0];
            Point cmlEndPoint = offsets.Points[1];
            Rect cmlLineExtents = offsets.Extents;

            Int64Value emuTop = OoXmlHelper.ScaleCmlToEmu(cmlLineExtents.Top);
            Int64Value emuLeft = OoXmlHelper.ScaleCmlToEmu(cmlLineExtents.Left);
            Int64Value emuWidth = OoXmlHelper.ScaleCmlToEmu(cmlLineExtents.Width);
            Int64Value emuHeight = OoXmlHelper.ScaleCmlToEmu(cmlLineExtents.Height);

            long id = _ooxmlId++;
            string suffix = string.IsNullOrEmpty(reaction.Path) ? id.ToString() : reaction.Path;
            string shapeName = "Reaction Arrow " + suffix;

            Wps.WordprocessingShape wordprocessingShape = CreateShape(id, shapeName);
            Wps.ShapeProperties shapeProperties = CreateShapeProperties(wordprocessingShape, emuTop, emuLeft, emuWidth, emuHeight);

            // Add the line
            A.PathList pathList = new A.PathList();

            A.Path path = new A.Path { Width = emuWidth, Height = emuHeight };

            A.MoveTo moveTo = new A.MoveTo();
            A.Point point1 = OoXmlPoint(cmlEndPoint);
            moveTo.Append(point1);
            path.Append(moveTo);

            A.LineTo lineTo = new A.LineTo();
            A.Point point2 = OoXmlPoint(cmlStartPoint);
            lineTo.Append(point2);
            path.Append(lineTo);

            pathList.Append(path);

            Int32Value emuLineWidth = (Int32Value)(OoXmlConstants.AcsLineWidth * OoXmlConstants.EmusPerWordPoint);
            A.Outline outline = new A.Outline { Width = emuLineWidth, CapType = A.LineCapValues.Round };

            A.SolidFill solidFill = new A.SolidFill();
            A.RgbColorModelHex rgbColorModelHex = new A.RgbColorModelHex { Val = OoXmlColours.Black };
            solidFill.Append(rgbColorModelHex);
            outline.Append(solidFill);

            if (reaction.ReactionType == ReactionType.Theoretical)
            {
                outline.Append(new A.PresetDash { Val = A.PresetLineDashValues.Dash });
            }

            // Add Arrow Head
            A.HeadEnd headEnd = new A.HeadEnd
                                {
                                    Type = A.LineEndValues.Triangle,
                                    Width = A.LineEndWidthValues.Small,
                                    Length = A.LineEndLengthValues.Small
                                };
            outline.Append(headEnd);

            if (reaction.ReactionType == ReactionType.Resonance)
            {
                A.TailEnd tailEnd = new A.TailEnd
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
                Vector shaftVector = cmlEndPoint - cmlStartPoint;
                Point midpoint = cmlStartPoint + shaftVector * 0.5;

                double crossArmLength = OoXmlConstants.MultipleBondOffsetPercentage * _chemistryModel.MeanBondLength;
                Point[] points = new Point[4];

                Matrix rotator = new Matrix();
                rotator.Rotate(-45);
                Vector shaftUnit = shaftVector;
                shaftUnit.Normalize();

                for (int i = 0; i < 4; i++)
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

            Wps.TextBodyProperties textBodyProperties = new Wps.TextBodyProperties();
            wordprocessingShape.Append(textBodyProperties);

            _wordprocessingGroup.Append(wordprocessingShape);

            // Local Function
            void AddCrossLine(Point startPoint, Point endPoint)
            {
                A.Path path1 = new A.Path { Width = emuWidth, Height = emuHeight };

                A.MoveTo moveTo2 = new A.MoveTo();
                A.Point point3 = OoXmlPoint(endPoint);
                moveTo2.Append(point3);
                path1.Append(moveTo2);

                A.LineTo lineTo2 = new A.LineTo();
                A.Point point4 = OoXmlPoint(startPoint);
                lineTo2.Append(point4);
                path1.Append(lineTo2);
                pathList.Append(path1);
            }
        }

        private void DrawStraightLine(Point bondStart, Point bondEnd, string bondPath,
                                      BondLineStyle lineStyle, string lineColour, double lineWidth)
        {
            List<Point> listOfPoints = new List<Point> { bondStart, bondEnd };
            OffsetPointsResult offsets = OffsetPoints(listOfPoints);

            Point cmlStartPoint = offsets.Points[0];
            Point cmlEndPoint = offsets.Points[1];
            Rect cmlLineExtents = offsets.Extents;

            Int64Value emuTop = OoXmlHelper.ScaleCmlToEmu(cmlLineExtents.Top);
            Int64Value emuLeft = OoXmlHelper.ScaleCmlToEmu(cmlLineExtents.Left);
            Int64Value emuWidth = OoXmlHelper.ScaleCmlToEmu(cmlLineExtents.Width);
            Int64Value emuHeight = OoXmlHelper.ScaleCmlToEmu(cmlLineExtents.Height);

            long id = _ooxmlId++;
            string suffix = string.IsNullOrEmpty(bondPath) ? id.ToString() : bondPath;
            string shapeName = "Straight Line " + suffix;

            Wps.WordprocessingShape wordprocessingShape = CreateShape(id, shapeName);
            Wps.ShapeProperties shapeProperties = CreateShapeProperties(wordprocessingShape, emuTop, emuLeft, emuWidth, emuHeight);

            // Start of the lines

            A.PathList pathList = new A.PathList();

            A.Path path = new A.Path { Width = emuWidth, Height = emuHeight };

            A.MoveTo moveTo = new A.MoveTo();
            A.Point point1 = OoXmlPoint(cmlStartPoint);
            moveTo.Append(point1);
            path.Append(moveTo);

            A.LineTo lineTo = new A.LineTo();
            A.Point point2 = OoXmlPoint(cmlEndPoint);
            lineTo.Append(point2);
            path.Append(lineTo);

            pathList.Append(path);

            // End of the lines

            Int32Value emuLineWidth = (Int32Value)(lineWidth * OoXmlConstants.EmusPerWordPoint);
            A.Outline outline = new A.Outline { Width = emuLineWidth, CapType = A.LineCapValues.Round };

            A.SolidFill solidFill = new A.SolidFill();
            A.RgbColorModelHex rgbColorModelHex = new A.RgbColorModelHex { Val = lineColour };
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
                // When creating bond lines, the arrow heads seem to be the wrong way round
                //  don't know why but our lines start at the OoXml end and finish at the OoXml start

                A.TailEnd tailEnd = new A.TailEnd
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

            Wps.TextBodyProperties textBodyProperties = new Wps.TextBodyProperties();
            wordprocessingShape.Append(textBodyProperties);

            _wordprocessingGroup.Append(wordprocessingShape);
        }

        private void DrawWavyLine(Point bondStart, Point bondEnd, string bondPath, string lineColour)
        {
            List<Point> listOfPoints = new List<Point> { bondStart, bondEnd };
            OffsetPointsResult offsets = OffsetPoints(listOfPoints);

            Point cmlStartPoint = offsets.Points[0];
            Point cmlEndPoint = offsets.Points[1];
            Rect cmlLineExtents = offsets.Extents;

            // Calculate wiggles

            Vector bondVector = cmlEndPoint - cmlStartPoint;
            int noOfWiggles = (int)Math.Ceiling(bondVector.Length / BondOffset());
            if (noOfWiggles < 1)
            {
                noOfWiggles = 1;
            }

            double wiggleLength = bondVector.Length / noOfWiggles;
            Debug.WriteLine($"v.Length: {bondVector.Length} noOfWiggles: {noOfWiggles}");

            Vector originalWigglePortion = bondVector;
            originalWigglePortion.Normalize();
            originalWigglePortion *= wiggleLength / 2;

            Matrix toLeft = new Matrix();
            toLeft.Rotate(-60);
            Matrix toRight = new Matrix();
            toRight.Rotate(60);
            Vector leftVector = originalWigglePortion * toLeft;
            Vector rightVector = originalWigglePortion * toRight;

            List<Point> allpoints = new List<Point>();

            Point lastPoint = cmlStartPoint;

            for (int i = 0; i < noOfWiggles; i++)
            {
                // Left
                allpoints.Add(lastPoint);
                Point leftPoint = lastPoint + leftVector;
                allpoints.Add(leftPoint);
                Point midPoint = lastPoint + originalWigglePortion;
                allpoints.Add(midPoint);

                // Right
                allpoints.Add(midPoint);
                Point rightPoint = lastPoint + originalWigglePortion + rightVector;
                allpoints.Add(rightPoint);
                lastPoint += originalWigglePortion * 2;
                allpoints.Add(lastPoint);
            }

            double minX = double.MaxValue;
            double maxX = double.MinValue;
            double minY = double.MaxValue;
            double maxY = double.MinValue;

            foreach (Point p in allpoints)
            {
                maxX = Math.Max(p.X + cmlLineExtents.Left, maxX);
                minX = Math.Min(p.X + cmlLineExtents.Left, minX);
                maxY = Math.Max(p.Y + cmlLineExtents.Top, maxY);
                minY = Math.Min(p.Y + cmlLineExtents.Top, minY);
            }

            Rect newExtents = new Rect(minX, minY, maxX - minX, maxY - minY);
            double xOffset = cmlLineExtents.Left - newExtents.Left;
            double yOffset = cmlLineExtents.Top - newExtents.Top;

            Int64Value emuTop = OoXmlHelper.ScaleCmlToEmu(newExtents.Top);
            Int64Value emuLeft = OoXmlHelper.ScaleCmlToEmu(newExtents.Left);
            Int64Value emuWidth = OoXmlHelper.ScaleCmlToEmu(newExtents.Width);
            Int64Value emuHeight = OoXmlHelper.ScaleCmlToEmu(newExtents.Height);

            string shapeName = "Wavy Line " + bondPath;

            Wps.WordprocessingShape wordprocessingShape = CreateShape(_ooxmlId++, shapeName);
            Wps.ShapeProperties shapeProperties = CreateShapeProperties(wordprocessingShape, emuTop, emuLeft, emuWidth, emuHeight);

            // Start of the lines

            A.PathList pathList = new A.PathList();

            A.Path path = new A.Path { Width = emuWidth, Height = emuHeight };

            A.MoveTo moveTo = new A.MoveTo();
            A.Point firstPoint = OoXmlPoint(new Point(cmlStartPoint.X + xOffset, cmlStartPoint.Y + yOffset));
            moveTo.Append(firstPoint);
            path.Append(moveTo);

            // Create the Curved Lines
            for (int i = 0; i < allpoints.Count; i += 3)
            {
                A.CubicBezierCurveTo cubicBezierCurveTo = new A.CubicBezierCurveTo();

                for (int j = 0; j < 3; j++)
                {
                    A.Point nextPoint = OoXmlPoint(new Point(allpoints[i + j].X + xOffset, allpoints[i + j].Y + yOffset));
                    cubicBezierCurveTo.Append(nextPoint);
                }
                path.Append(cubicBezierCurveTo);
            }

            pathList.Append(path);

            // End of the lines

            double lineWidth = OoXmlConstants.AcsLineWidth;
            Int32Value emuLineWidth = (Int32Value)(lineWidth * OoXmlConstants.EmusPerWordPoint);
            A.Outline outline = new A.Outline { Width = emuLineWidth, CapType = A.LineCapValues.Round };

            A.SolidFill solidFill = new A.SolidFill();
            A.RgbColorModelHex rgbColorModelHex = new A.RgbColorModelHex { Val = lineColour };
            solidFill.Append(rgbColorModelHex);
            outline.Append(solidFill);

            if (_options.ShowBondDirection)
            {
                // When creating bond lines, the arrow heads seem to be the wrong way round
                //  don't know why but our lines start at the OoXml end and finish at the OoXml start

                A.TailEnd tailEnd = new A.TailEnd { Type = A.LineEndValues.Stealth };
                outline.Append(tailEnd);
            }

            shapeProperties.Append(CreateCustomGeometry(pathList));
            shapeProperties.Append(outline);

            wordprocessingShape.Append(CreateShapeStyle());

            Wps.TextBodyProperties textBodyProperties = new Wps.TextBodyProperties();
            wordprocessingShape.Append(textBodyProperties);

            _wordprocessingGroup.Append(wordprocessingShape);
        }

        private void LoadFont()
        {
            string json = ResourceHelper.GetStringResource(Assembly.GetExecutingAssembly(), "Arial.json");
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

        private OffsetPointsResult OffsetPoints(List<Point> points)
        {
            OffsetPointsResult result = new OffsetPointsResult();

            double xMax = points.Select(a => a.X).Max();
            double xMin = points.Select(a => a.X).Min();

            double yMax = points.Select(a => a.Y).Max();
            double yMin = points.Select(a => a.Y).Min();

            Rect extents = new Rect(new Point(xMin, yMin), new Point(xMax, yMax));

            // Move Extents to have 0,0 Top Left Reference
            extents.Offset(-_boundingBoxOfEverything.Left, -_boundingBoxOfEverything.Top);

            Point[] pp = points.ToArray();

            // Pass 1 - Move Points to have 0,0 Top Left Reference
            for (int i = 0; i < pp.Length; i++)
            {
                Point p = new Point(pp[i].X, pp[i].Y);
                p.Offset(-_boundingBoxOfEverything.Left, -_boundingBoxOfEverything.Top);
                pp[i] = p;
            }

            // Pass 2 - Move points into New Extents
            for (int i = 0; i < pp.Length; i++)
            {
                Point p = new Point(pp[i].X, pp[i].Y);
                p.Offset(-extents.Left, -extents.Top);
                pp[i] = p;
            }

            result.Extents = extents;
            result.Points = pp.ToList();

            return result;
        }

        /// <summary>
        /// Sets the canvas size to accommodate any extra space required by label characters
        /// </summary>
        private void SetCanvasSizeX()
        {
            _boundingBoxOfAllAtoms = _chemistryModel.BoundingBoxOfCmlPoints;

            _boundingBoxOfEverything = _boundingBoxOfAllAtoms;

            foreach (AtomLabelCharacter alc in _outputs.AtomLabelCharacters)
            {
                if (alc.IsSmaller)
                {
                    Rect r = new Rect(alc.Position,
                                      new Size(OoXmlHelper.ScaleCsTtfToCml(alc.Character.Width, _meanBondLength) * OoXmlConstants.SubscriptScaleFactor,
                                               OoXmlHelper.ScaleCsTtfToCml(alc.Character.Height, _meanBondLength) * OoXmlConstants.SubscriptScaleFactor));
                    _boundingBoxOfEverything.Union(r);
                }
                else
                {
                    Rect r = new Rect(alc.Position,
                                      new Size(OoXmlHelper.ScaleCsTtfToCml(alc.Character.Width, _meanBondLength),
                                               OoXmlHelper.ScaleCsTtfToCml(alc.Character.Height, _meanBondLength)));
                    _boundingBoxOfEverything.Union(r);
                }
            }

            foreach (MoleculeExtents group in _outputs.AllMoleculeExtents)
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
