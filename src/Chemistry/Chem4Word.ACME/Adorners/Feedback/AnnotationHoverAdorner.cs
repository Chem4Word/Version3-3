// ---------------------------------------------------------------------------
//  Copyright (c) 2026, The .NET Foundation.
//  This software is released under the Apache Licence, Version 2.0.
//  The licence and further copyright text can be found in the file LICENCE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.ACME.Drawing.Visuals;

using System.Windows;
using System.Windows.Media;

namespace Chem4Word.ACME.Adorners.Feedback
{
    public class AnnotationHoverAdorner : BaseHoverAdorner
    {
        public AnnotationHoverAdorner(UIElement adornedElement, AnnotationVisual targetedVisual)
            : base(adornedElement, targetedVisual)
        {
            //calls directly into base constructor
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            Rect atomBounds = Rect.Empty;

            AnnotationVisual annotationVisual = (AnnotationVisual)TargetedVisual;
            atomBounds = annotationVisual.ContentBounds;

            if (atomBounds != Rect.Empty)
            {
                atomBounds.Inflate(2.0, 2.0);
            }

            StreamGeometry sg = DrawAnnotationBrackets(atomBounds);

            drawingContext.DrawGeometry(BracketBrush, BracketPen, sg);
        }

        private static StreamGeometry DrawAnnotationBrackets(Rect atomBounds)
        {
            StreamGeometry geometry = new StreamGeometry();

            using (StreamGeometryContext context = geometry.Open())
            {
                // Define the size of the brackets
                double bracketSize = 5.0;

                // Top-left bracket
                context.BeginFigure(new Point(atomBounds.Left, atomBounds.Top + bracketSize), false, false);
                context.LineTo(new Point(atomBounds.Left, atomBounds.Top), true, false);
                context.LineTo(new Point(atomBounds.Left + bracketSize, atomBounds.Top), true, false);

                // Top-right bracket
                context.BeginFigure(new Point(atomBounds.Right, atomBounds.Top + bracketSize), false, false);
                context.LineTo(new Point(atomBounds.Right, atomBounds.Top), true, false);
                context.LineTo(new Point(atomBounds.Right - bracketSize, atomBounds.Top), true, false);

                // Bottom-left bracket
                context.BeginFigure(new Point(atomBounds.Left, atomBounds.Bottom - bracketSize), false, false);
                context.LineTo(new Point(atomBounds.Left, atomBounds.Bottom), true, false);
                context.LineTo(new Point(atomBounds.Left + bracketSize, atomBounds.Bottom), true, false);

                // Bottom-right bracket
                context.BeginFigure(new Point(atomBounds.Right, atomBounds.Bottom - bracketSize), false, false);
                context.LineTo(new Point(atomBounds.Right, atomBounds.Bottom), true, false);
                context.LineTo(new Point(atomBounds.Right - bracketSize, atomBounds.Bottom), true, false);
            }

            geometry.Freeze();
            return geometry;
        }

        private const double DisplacementOffsetFactor = 0.5;

        private Point AdjustedControlPoint(Point unadjustedPoint, Point reference)
        {
            Vector offsetVector = unadjustedPoint - reference;
            return offsetVector * DisplacementOffsetFactor + reference;
        }
    }
}
