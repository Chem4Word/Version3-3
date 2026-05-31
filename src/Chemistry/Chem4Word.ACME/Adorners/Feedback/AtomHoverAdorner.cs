// ---------------------------------------------------------------------------
//  Copyright (c) 2026, The .NET Foundation.
//  This software is released under the Apache Licence, Version 2.0.
//  The licence and further copyright text can be found in the file LICENCE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.ACME.Drawing.Visuals;
using Chem4Word.Core.Helpers;
using Chem4Word.Model2;
using System.Windows;
using System.Windows.Media;

namespace Chem4Word.ACME.Adorners.Feedback
{
    public class AtomHoverAdorner : BaseHoverAdorner
    {
        public AtomHoverAdorner(UIElement adornedElement, AtomVisual targetedVisual) : base(adornedElement, targetedVisual)
        {
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);
            StreamGeometry sg = new StreamGeometry();

            Rect atomBounds = Rect.Empty;

            if (TargetedVisual is ElectronVisual ev)
            {
                atomBounds = ev.Bounds;
            }
            else if (TargetedVisual is AtomVisual av && av.ParentAtom != null)
            {
                atomBounds = av.Bounds;
            }
            else if (TargetedVisual is ElectronPusherVisual epv)
            {
                atomBounds = epv.Bounds;
                ElectronPusher pusher = epv.ParentPusher;
                Rect r1 = GeometryTool.BoundingBoxOfPoint(AdjustedControlPoint(pusher.FirstControlPoint, pusher.StartPoint), 4.0);
                Rect r2 = GeometryTool.BoundingBoxOfPoint(AdjustedControlPoint(pusher.SecondControlPoint, pusher.EndPoint), 4.0);
                atomBounds.Union(r1);
                atomBounds.Union(r2);
                atomBounds.Inflate(3.0, 3.0);
            }

            if (atomBounds != Rect.Empty)
            {
                atomBounds.Inflate(2.0, 2.0);
            }

            Vector twiddle = new Vector(3, 0.0);
            using (StreamGeometryContext sgc = sg.Open())
            {
                sgc.BeginFigure(atomBounds.TopLeft + twiddle, false, false);
                sgc.LineTo(atomBounds.TopLeft, true, true);
                sgc.LineTo(atomBounds.BottomLeft, true, true);
                sgc.LineTo(atomBounds.BottomLeft + twiddle, true, true);

                sgc.BeginFigure(atomBounds.TopRight - twiddle, false, false);
                sgc.LineTo(atomBounds.TopRight, true, true);
                sgc.LineTo(atomBounds.BottomRight, true, true);
                sgc.LineTo(atomBounds.BottomRight - twiddle, true, true);
                sgc.Close();
            }

            drawingContext.DrawGeometry(BracketBrush, BracketPen, sg);
        }

        private const double DisplacementOffsetFactor = 0.5;

        private Point AdjustedControlPoint(Point unadjustedPoint, Point reference)
        {
            Vector offsetVector = unadjustedPoint - reference;
            return offsetVector * DisplacementOffsetFactor + reference;
        }
    }
}
