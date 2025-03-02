﻿// ---------------------------------------------------------------------------
//  Copyright (c) 2025, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace Chem4Word.ACME.Controls
{
    public class ZoomBox : Control
    {
        private Thumb _zoomThumb;
        private Canvas _zoomCanvas;
        private Slider _zoomSlider;

        #region DPs

        #region ScrollViewer

        public ScrollViewer ScrollViewer
        {
            get => (ScrollViewer)GetValue(ScrollViewerProperty);
            set => SetValue(ScrollViewerProperty, value);
        }

        public static readonly DependencyProperty ScrollViewerProperty =
            DependencyProperty.Register("ScrollViewer", typeof(ScrollViewer), typeof(ZoomBox));

        #endregion ScrollViewer

        #region Canvas

        public static readonly DependencyProperty CanvasProperty =
            DependencyProperty.Register("Canvas", typeof(Canvas), typeof(ZoomBox),
                new FrameworkPropertyMetadata(null,
                    OnCanvasChanged));

        public Canvas Canvas
        {
            get => (Canvas)GetValue(CanvasProperty);
            set => SetValue(CanvasProperty, value);
        }

        private static void OnCanvasChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ZoomBox target = (ZoomBox)d;
            Canvas oldDesignerCanvas = (Canvas)e.OldValue;
            Canvas newDesignerCanvas = target.Canvas;
            target.OnCanvasChanged(oldDesignerCanvas, newDesignerCanvas);
        }

        protected virtual void OnCanvasChanged(Canvas oldDesignerCanvas, Canvas newDesignerCanvas)
        {
            if (oldDesignerCanvas != null)
            {
                oldDesignerCanvas.LayoutUpdated -= DesignerCanvas_LayoutUpdated;
                oldDesignerCanvas.MouseWheel -= DesignerCanvas_MouseWheel;
            }

            if (newDesignerCanvas != null)
            {
                newDesignerCanvas.LayoutUpdated += DesignerCanvas_LayoutUpdated;
                newDesignerCanvas.MouseWheel += DesignerCanvas_MouseWheel;
                newDesignerCanvas.LayoutTransform = Canvas.LayoutTransform;
            }
        }

        #endregion Canvas

        #endregion DPs

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            if (ScrollViewer == null)
            {
                return;
            }

            _zoomThumb = Template.FindName("PART_ZoomThumb", this) as Thumb;
            if (_zoomThumb == null)
            {
                Debugger.Break();
                throw new Exception("PART_ZoomThumb template is missing!");
            }

            _zoomCanvas = Template.FindName("PART_ZoomCanvas", this) as Canvas;
            if (_zoomCanvas == null)
            {
                Debugger.Break();
                throw new Exception("PART_ZoomCanvas template is missing!");
            }

            _zoomSlider = Template.FindName("PART_ZoomSlider", this) as Slider;
            if (_zoomSlider == null)
            {
                Debugger.Break();
                throw new Exception("PART_ZoomSlider template is missing!");
            }

            _zoomThumb.DragDelta += Thumb_DragDelta;
            _zoomSlider.ValueChanged += ZoomSlider_ValueChanged;
            Canvas.LayoutTransform = new ScaleTransform();
        }

        private void ZoomSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            double scale = e.NewValue / e.OldValue;
            double halfViewportHeight = ScrollViewer.ViewportHeight / 2;
            double newVerticalOffset = ((ScrollViewer.VerticalOffset + halfViewportHeight) * scale - halfViewportHeight);
            double halfViewportWidth = ScrollViewer.ViewportWidth / 2;
            double newHorizontalOffset = ((ScrollViewer.HorizontalOffset + halfViewportWidth) * scale - halfViewportWidth);
            (Canvas.LayoutTransform as ScaleTransform).ScaleX *= scale;
            (Canvas.LayoutTransform as ScaleTransform).ScaleY *= scale;
            InvalidateScale(out _, out _, out _);
            ScrollViewer.ScrollToHorizontalOffset(newHorizontalOffset);
            ScrollViewer.ScrollToVerticalOffset(newVerticalOffset);
        }

        private void Thumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            InvalidateScale(out var scale, out _, out _);
            ScrollViewer.ScrollToHorizontalOffset(ScrollViewer.HorizontalOffset + e.HorizontalChange / scale);
            ScrollViewer.ScrollToVerticalOffset(ScrollViewer.VerticalOffset + e.VerticalChange / scale);
        }

        private void DesignerCanvas_LayoutUpdated(object sender, EventArgs e)
        {
            InvalidateScale(out var scale, out var xOffset, out var yOffset);
            if (_zoomThumb != null)
            {
                _zoomThumb.Width = ScrollViewer.ViewportWidth * scale;
                _zoomThumb.Height = ScrollViewer.ViewportHeight * scale;
                Canvas.SetLeft(_zoomThumb, xOffset + ScrollViewer.HorizontalOffset * scale);
                Canvas.SetTop(_zoomThumb, yOffset + ScrollViewer.VerticalOffset * scale);
            }
        }

        private void DesignerCanvas_MouseWheel(object sender, EventArgs e)
        {
            MouseWheelEventArgs wheel = (MouseWheelEventArgs)e;

            //divide the value by 15 so that it is smoother

            double value = wheel.Delta / 15;
            if (Math.Abs(value) > 10d)
            {
                value = 10 * Math.Sign(value);
            }

            _zoomSlider.Value += value;
            wheel.Handled = true;
        }

        private void InvalidateScale(out double scale, out double xOffset, out double yOffset)
        {
            if (Canvas == null || !(Canvas.LayoutTransform is ScaleTransform st))
            {
                scale = 1.0;
                xOffset = 0d;
                yOffset = 0d;
            }
            else
            {
                double w = Canvas.ActualWidth * st.ScaleX;
                double h = Canvas.ActualHeight * st.ScaleY;

                // zoom canvas size
                double x = _zoomCanvas.ActualWidth;
                double y = _zoomCanvas.ActualHeight;
                double scaleX = x / w;
                double scaleY = y / h;
                scale = (scaleX < scaleY) ? scaleX : scaleY;
                xOffset = (x - scale * w) / 2;
                yOffset = (y - scale * h) / 2;
            }
        }
    }
}