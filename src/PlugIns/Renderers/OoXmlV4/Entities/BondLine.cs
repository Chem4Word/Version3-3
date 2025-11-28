// ---------------------------------------------------------------------------
//  Copyright (c) 2026, The .NET Foundation.
//  This software is released under the Apache Licence, Version 2.0.
//  The licence and further copyright text can be found in the file LICENCE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.Core.Helpers;
using Chem4Word.Model2;
using Chem4Word.Renderer.OoXmlV4.Enums;
using Chem4Word.Renderer.OoXmlV4.OoXml;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;

namespace Chem4Word.Renderer.OoXmlV4.Entities
{
    public class BondLine
    {
        private Rect _boundingBox = Rect.Empty;
        private Point _end;
        private Point _innerEnd;
        private Point _innerStart;
        private Point _leftTail;
        private Point _outerEnd;
        private Point _outerStart;
        private Point _rightTail;
        private Point _start;

        public Bond Bond { get; }
        public string BondPath => Bond != null ? Bond.Path : string.Empty;

        private Dictionary<int, Dictionary<string, Point>> _outlines { get; set; } = new Dictionary<int, Dictionary<string, Point>>();

        public Rect BoundingBox
        {
            get
            {
                if (_boundingBox.IsEmpty)
                {
                    switch (Style)
                    {
                        case BondLineStyle.Thick:
                            _boundingBox = GetBoundingBox(CurrentOutline);
                            break;

                        case BondLineStyle.Wedge:
                        case BondLineStyle.Hatch:
                            _boundingBox = GetBoundingBox(CurrentOutline);
                            break;

                        default:
                            _boundingBox = new Rect(Start, End);
                            break;
                    }
                }

                return _boundingBox;
            }
        }

        public string Colour { get; set; } = OoXmlColours.Black;

        public List<Point> CurrentOutline
        {
            get
            {
                if (Style == BondLineStyle.Thick)
                {
                    return new List<Point>
                           {
                               InnerStart,
                               InnerEnd,
                               OuterEnd,
                               OuterStart
                           };
                }

                return new List<Point>
                       {
                           Nose,
                           LeftTail,
                           Tail,
                           RightTail
                       };
            }
        }

        public Point End
        {
            get => _end;
            set
            {
                if (_start != value)
                {
                    _end = value;
                    AddOutlinePoints(nameof(End));
                }
            }
        }

        public string EndAtomPath => Bond != null ? Bond.EndAtom.Path : string.Empty;

        public Point InnerEnd
        {
            get => _innerEnd;
            set
            {
                if (_start != value)
                {
                    _innerEnd = value;
                    AddOutlinePoints(nameof(InnerEnd));
                }
            }
        }

        public Point InnerStart
        {
            get => _innerStart;
            set
            {
                if (_start != value)
                {
                    _innerStart = value;
                    AddOutlinePoints(nameof(InnerStart));
                }
            }
        }

        public Point LeftTail
        {
            get => _leftTail;
            set
            {
                if (_start != value)
                {
                    _leftTail = value;
                    AddOutlinePoints(nameof(LeftTail));
                }
            }
        }

        public Point Nose => Start;
        public double Offset { get; set; }

        public List<Point> OriginalOutline
        {
            get
            {
                if (Style == BondLineStyle.Thick)
                {
                    return new List<Point>
                           {
                               GetOriginalPoint(nameof(InnerStart)),
                               GetOriginalPoint(nameof(InnerEnd)),
                               GetOriginalPoint(nameof(OuterEnd)),
                               GetOriginalPoint(nameof(OuterStart))
                           };
                }

                return new List<Point>
                       {
                           GetOriginalPoint(nameof(Nose)),
                           GetOriginalPoint(nameof(LeftTail)),
                           GetOriginalPoint(nameof(Tail)),
                           GetOriginalPoint(nameof(RightTail))
                       };
            }
        }

        public Point OuterEnd
        {
            get => _outerEnd;
            set
            {
                if (_start != value)
                {
                    _outerEnd = value;
                    AddOutlinePoints(nameof(OuterEnd));
                }
            }
        }

        public Point OuterStart
        {
            get => _outerStart;
            set
            {
                if (_start != value)
                {
                    _outerStart = value;
                    AddOutlinePoints(nameof(OuterStart));
                }
            }
        }

        public Point RightTail
        {
            get => _rightTail;
            set
            {
                if (_start != value)
                {
                    _rightTail = value;
                    AddOutlinePoints(nameof(RightTail));
                }
            }
        }

        public Point Start
        {
            get => _start;
            set
            {
                if (_start != value)
                {
                    _start = value;
                    AddOutlinePoints(nameof(Start));
                }
            }
        }

        public string StartAtomPath => Bond != null ? Bond.StartAtom.Path : string.Empty;
        public BondLineStyle Style { get; private set; }
        public Point Tail => End;

        public double Width { get; set; } = OoXmlConstants.AcsLineWidth;

        #region Constructors

        public BondLine(BondLineStyle style, Bond bond, double medianBondLength = 0.0)
        {
            Style = style;
            Bond = bond;

            if (bond != null)
            {
                _start = bond.StartAtom.Position;
                _end = bond.EndAtom.Position;
            }

            if (medianBondLength > 0.0)
            {
                CalculateOutlinePoints(medianBondLength);
            }
        }

        public BondLine(BondLineStyle style, Point startPoint, Point endPoint, Bond bond, double medianBondLength = 0.0)
        {
            Style = style;
            Bond = bond;

            _start = startPoint;
            _end = endPoint;

            if (medianBondLength > 0.0)
            {
                CalculateOutlinePoints(BondOffset(medianBondLength));
            }
        }

        #endregion Constructors

        #region Methods

        public void CalculateOutlinePoints(double medianBondLength)
        {
            _boundingBox = Rect.Empty;

            Offset = BondOffset(medianBondLength) / 2;

            var originalInside = GetParallel(Offset);
            var originalOutside = GetParallel(-Offset);

            if (Style == BondLineStyle.Thick)
            {
                _innerStart = originalInside.Start;
                _innerEnd = originalInside.End;
                _outerStart = originalOutside.Start;
                _outerEnd = originalOutside.End;
            }
            else
            {
                _leftTail = new Point(originalInside.End.X, originalInside.End.Y);
                _rightTail = new Point(originalOutside.End.X, originalOutside.End.Y);
            }

            AddOutlinePoints();
        }

        public BondLine Copy()
        {
            var copy = new BondLine(Style, Start, End, Bond)
            {
                Colour = Colour,
                Width = Width
            };

            return copy;
        }

        public Point GetOriginalPoint(string pointName)
        {
            var result = new Point();

            if (_outlines.TryGetValue(0, out var value))
            {
                switch (pointName)
                {
                    // General bonds
                    case nameof(Start):
                        result = value[nameof(Start)];
                        break;

                    case nameof(End):
                        result = value[nameof(End)];
                        break;

                    // Thick bonds
                    case nameof(InnerStart):
                        result = value[nameof(InnerStart)];
                        break;

                    case nameof(InnerEnd):
                        result = value[nameof(InnerEnd)];
                        break;

                    case nameof(OuterStart):
                        result = value[nameof(OuterStart)];
                        break;

                    case nameof(OuterEnd):
                        result = value[nameof(OuterEnd)];
                        break;

                    // Wedge or Hash bonds
                    case nameof(Nose):
                        result = value[nameof(Nose)];
                        break;

                    case nameof(LeftTail):
                        result = value[nameof(LeftTail)];
                        break;

                    case nameof(Tail):
                        result = value[nameof(Tail)];
                        break;

                    case nameof(RightTail):
                        result = value[nameof(RightTail)];
                        break;

                    default:
                        // Should never get here if the nameof(XXX) has been used in the calling routine
                        Debug.WriteLine($"Unknown Point '{pointName}'");
                        Debugger.Break();
                        break;
                }
            }

            return result;
        }

        public BondLine GetParallel(double offset)
        {
            var simpleLine = new SimpleLine(Start, End);
            var offsetLine = simpleLine.GetParallel(offset);

            return new BondLine(Style, offsetLine.Start, offsetLine.End, Bond)
            {
                Colour = Colour,
                Style = Style,
                Width = Width
            };
        }

        public void SetLineStyle(BondLineStyle style)
        {
            Style = style;
        }

        public void Shrink(double value)
        {
            var start = Start;
            var end = End;
            GeometryTool.AdjustLineAboutMidpoint(ref start, ref end, value);
            Start = start;
            End = end;
        }

        public override string ToString()
        {
            var result = $"{Style} from {PointHelper.AsString(Start)} to {PointHelper.AsString(End)}";
            if (Bond != null)
            {
                result += $" [{Bond}]";
            }

            return result;
        }

        private static double BondOffset(double medianBondLength)
                    => medianBondLength * OoXmlConstants.MultipleBondOffsetPercentage;

        private void AddOutlinePoints(string changed = null)
        {
            switch (Style)
            {
                case BondLineStyle.Thick:
                    var thickPoints = new Dictionary<string, Point>
                                      {
                                          { nameof(Start), Start },
                                          { nameof(End), End },
                                          { nameof(InnerStart), InnerStart },
                                          { nameof(InnerEnd), InnerEnd },
                                          { nameof(OuterStart), OuterStart },
                                          { nameof(OuterEnd), OuterEnd }
                                      };
                    _outlines.Add(_outlines.Count, thickPoints);
                    break;

                case BondLineStyle.Wedge:
                case BondLineStyle.Hatch:
                    var wedgePoints = new Dictionary<string, Point>
                                      {
                                          { nameof(Start), Start },
                                          { nameof(Nose), Nose },
                                          { nameof(LeftTail), LeftTail },
                                          { nameof(Tail), Tail },
                                          { nameof(End), End },
                                          { nameof(RightTail), RightTail }
                                      };
                    _outlines.Add(_outlines.Count, wedgePoints);
                    break;
            }

            if (string.IsNullOrEmpty(changed))
            {
                Debug.WriteLine($"Bond '{BondPath}' Outlines: {_outlines.Count + 1}");
            }
            else
            {
                Debug.WriteLine($"Bond '{BondPath}' Outlines: {_outlines.Count + 1} Triggered by change of '{changed}'");
            }
        }

        private Rect GetBoundingBox(List<Point> points)
        {
            var minX = double.MaxValue;
            var minY = double.MaxValue;

            var maxX = double.MinValue;
            var maxY = double.MinValue;

            foreach (var point in points)
            {
                minX = Math.Min(minX, point.X);
                minY = Math.Min(minY, point.Y);
                maxX = Math.Max(maxX, point.X);
                maxY = Math.Max(maxY, point.Y);
            }

            return new Rect(minX, minY, maxX - minX, maxY - minY);
        }

        #endregion Methods

        [Obsolete("No Longer used")]
        private static void TrimByVector(Point line1Start, Point line1End, Point line2Start, Point line2End, ref Vector vector)
        {
            var crossingPoint = GeometryTool.GetIntersection(line1Start, line1End, line2Start, line2End);
            if (crossingPoint != null)
            {
                var v = crossingPoint.Value - line1Start;
                if (v.Length < vector.Length)
                {
                    vector = v;
                }
            }
        }
    }
}