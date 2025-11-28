// ---------------------------------------------------------------------------
//  Copyright (c) 2026, The .NET Foundation.
//  This software is released under the Apache Licence, Version 2.0.
//  The licence and further copyright text can be found in the file LICENCE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.Core.Helpers;
using System.Windows;

namespace Chem4Word.Renderer.OoXmlV4.Entities
{
    public class SimpleLine
    {
        public SimpleLine(Point startPoint, Point endPoint)
        {
            Start = startPoint;
            End = endPoint;
        }

        public Point End { get; }
        public Point Start { get; }

        public SimpleLine GetParallel(double offset)
        {
            var vector = End - Start;
            vector.Normalize();
            var perpendicular = vector.Perpendicular() * offset;

            return new SimpleLine(Start - perpendicular, End - perpendicular);
        }
    }
}