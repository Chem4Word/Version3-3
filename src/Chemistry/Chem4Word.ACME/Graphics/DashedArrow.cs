// ---------------------------------------------------------------------------
//  Copyright (c) 2026, The .NET Foundation.
//  This software is released under the Apache Licence, Version 2.0.
//  The licence and further copyright text can be found in the file LICENCE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------
using System.Windows.Media;

namespace Chem4Word.ACME.Graphics
{
    public class DashedArrow : StraightArrow
    {
        protected override PathFigure DrawTheShaft(DrawingContext drawingContext, Pen outlinePen, Pen overlayPen,
                                                   Brush overlayBrush)
        {
            var shaftFigure = Shaft();
            var shaftFigures = new PathFigureCollection() { shaftFigure };
            outlinePen.StartLineCap = PenLineCap.Round;
            outlinePen.EndLineCap = PenLineCap.Round;

            Pen shaftPen = outlinePen.Clone();

            shaftPen.DashStyle = new DashStyle() { Dashes = new DoubleCollection() { 2, 4 } };
            var lineGeometry = new PathGeometry(shaftFigures);
            drawingContext.DrawGeometry(null, shaftPen, lineGeometry);

            var overlay = lineGeometry.GetWidenedPathGeometry(overlayPen);

            drawingContext.DrawGeometry(overlayBrush, null, overlay);
            return shaftFigure;
        }
    }
}