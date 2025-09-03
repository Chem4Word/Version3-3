// ---------------------------------------------------------------------------
//  Copyright (c) 2025, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace Chem4Word.ACME.Controls
{
    public class DragHandle : Thumb
    {
        public Point MidPoint { get; set; }

        public DragHandle(string styleName = AcmeConstants.GrabHandleStyle, Cursor cursor = null)
        {
            Style = (Style)FindResource(AcmeConstants.GrabHandleStyle);
            if (cursor is null)
            {
                cursor = Cursors.Hand;
            }
            Cursor = cursor;
            IsHitTestVisible = true;
        }

        private Rect GetRectOfObject(FrameworkElement _element)
        {
            Rect rectangleBounds = _element.RenderTransform.TransformBounds(
                new Rect(_element.RenderSize));

            return rectangleBounds;
        }

        public Rect BoundingBox => GetRectOfObject(this);

        public Point Centroid
        {
            get
            {
                var bb = BoundingBox;
                return new Point((bb.Left + bb.Right) / 2, (bb.Top + bb.Bottom) / 2);
            }
        }
    }
}