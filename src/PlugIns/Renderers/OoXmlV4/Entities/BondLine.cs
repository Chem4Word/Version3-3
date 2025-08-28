// ---------------------------------------------------------------------------
//  Copyright (c) 2025, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.Core.Helpers;
using Chem4Word.Model2;
using Chem4Word.Renderer.OoXmlV4.Enums;
using Chem4Word.Renderer.OoXmlV4.OOXML;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;

namespace Chem4Word.Renderer.OoXmlV4.Entities
{
    public class BondLine
    {
        public Bond Bond { get; }

        public string BondPath => Bond != null ? Bond.Path : string.Empty;

        public string StartAtomPath => Bond != null ? Bond.StartAtom.Path : string.Empty;

        public string EndAtomPath => Bond != null ? Bond.EndAtom.Path : string.Empty;

        public BondLineStyle Style { get; private set; }

        public string Colour { get; set; } = OoXmlHelper.Black;
        public double Width { get; set; } = OoXmlHelper.AcsLineWidth;

        private Point _start;

        private BondLine _originalInside;
        private BondLine _originalOutside;

        /// <summary>
        /// For a Wedge or Hatch bond this is the nose of the wedge
        /// </summary>
        public Point Start
        {
            get => _start;
            set => _start = value;
        }

        private Point _end;

        /// <summary>
        /// For a Wedge or Hatch bond this is the centre of the "tail"
        /// </summary>
        public Point End
        {
            get => _end;
            set => _end = value;
        }

        public Point Nose => Start;
        public Point Tail => End;

        public Point InnerStart { get; set; }
        public Point InnerEnd { get; set; }
        public Point OuterStart { get; set; }
        public Point OuterEnd { get; set; }

        /// <summary>
        /// Only relevant to Wedge or Hatch bond
        /// </summary>
        public Point LeftTail { get; set; }

        /// <summary>
        /// Only relevant to Wedge or Hatch bond
        /// </summary>
        public Point RightTail { get; set; }

        private Rect _boundingBox = Rect.Empty;

        public BondLine Copy()
        {
            var copy = new BondLine(Style, Start, End, Bond)
            {
                Colour = Colour,
                Width = Width
            };

            return copy;
        }

        public void Shrink(double value)
        {
            GeometryTool.AdjustLineAboutMidpoint(ref _start, ref _end, value);
        }

        public Rect BoundingBox
        {
            get
            {
                if (_boundingBox.IsEmpty)
                {
                    _boundingBox = new Rect(Start, End);
                }

                return _boundingBox;
            }
        }

        public BondLine(BondLineStyle style, Bond bond)
        {
            Style = style;
            Bond = bond;

            if (bond != null)
            {
                Start = bond.StartAtom.Position;
                End = bond.EndAtom.Position;
            }
        }

        public BondLine(BondLineStyle style, Point startPoint, Point endPoint, Bond bond)
            : this(style, startPoint, endPoint)
            => Bond = bond;

        private BondLine(BondLineStyle style, Point startPoint, Point endPoint)
        {
            Style = style;
            Start = startPoint;
            End = endPoint;
        }

        private static double BondOffset(double medianBondLength)
            => medianBondLength * OoXmlHelper.MultipleBondOffsetPercentage;

        public List<Point> Outline
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

        public Point GetOriginalPoint(string pointName)
        {
            switch (pointName)
            {
                case nameof(InnerStart):
                case nameof(Nose):
                    return _originalInside.Start;

                case nameof(InnerEnd):
                    return _originalInside.End;

                case nameof(OuterStart):
                    return _originalOutside.Start;

                case nameof(OuterEnd):
                    return _originalOutside.End;

                case nameof(LeftTail):
                    return new Point(_originalInside.End.X, _originalInside.End.Y);

                case nameof(RightTail):
                    return new Point(_originalOutside.End.X, _originalOutside.End.Y);
            }

            // Should never get here if the nameof(XXX) has been used in the calling routine
            Debugger.Break();
            return new Point();
        }

        public void CalculateInitialOutlinePoints(double medianBondLength)
        {
            _originalInside = GetParallel(BondOffset(medianBondLength) / 2);
            _originalOutside = GetParallel(-BondOffset(medianBondLength) / 2);

            if (Style == BondLineStyle.Thick)
            {
                InnerStart = _originalInside.Start;
                InnerEnd = _originalInside.End;
                OuterStart = _originalOutside.Start;
                OuterEnd = _originalOutside.End;
            }
            else
            {
                LeftTail = new Point(_originalInside.End.X, _originalInside.End.Y);
                RightTail = new Point(_originalOutside.End.X, _originalOutside.End.Y);
            }
        }

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

        public BondLine GetParallel(double offset)
        {
            var simpleLine = new SimpleLine(Start, End);
            var offsetLine = simpleLine.GetParallel(offset);

            return new BondLine(Style, offsetLine.Start, offsetLine.End, Bond)
            {
                Colour = Colour
            };
        }

        public void SetLineStyle(BondLineStyle style)
        {
            Style = style;
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
    }
}