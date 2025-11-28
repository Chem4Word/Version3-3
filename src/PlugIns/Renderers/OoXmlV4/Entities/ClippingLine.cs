// ---------------------------------------------------------------------------
//  Copyright (c) 2026, The .NET Foundation.
//  This software is released under the Apache Licence, Version 2.0.
//  The licence and further copyright text can be found in the file LICENCE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.Renderer.OoXmlV4.Enums;
using System.Windows;

namespace Chem4Word.Renderer.OoXmlV4.Entities
{
    public class ClippingLine
    {
        public Point Start { get; }

        public Point End { get; }

        public ClippingLine(Point startPoint, Point endPoint, ClippingLineType type = ClippingLineType.Standard)
        {
            switch (type)
            {
                case ClippingLineType.ExtendStart:
                    Start = GetExtendedLineEndPoint(endPoint, startPoint);
                    End = endPoint;
                    break;

                case ClippingLineType.ExtendEnd:
                    Start = startPoint;
                    End = GetExtendedLineEndPoint(startPoint, endPoint);
                    break;

                case ClippingLineType.ExtendBoth:
                    Start = GetExtendedLineEndPoint(endPoint, startPoint);
                    End = GetExtendedLineEndPoint(startPoint, endPoint);
                    break;

                default:
                    Start = startPoint;
                    End = endPoint;
                    break;
            }
        }

        private static Point GetExtendedLineEndPoint(Point p1, Point p2)
        {
            var newX = p1.X + 2 * (p2.X - p1.X);
            var newY = p1.Y + 2 * (p2.Y - p1.Y);

            return new Point(newX, newY);
        }
    }
}