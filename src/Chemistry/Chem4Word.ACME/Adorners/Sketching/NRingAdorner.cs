﻿// ---------------------------------------------------------------------------
//  Copyright (c) 2025, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.ACME.Drawing;
using Chem4Word.ACME.Drawing.Text;
using Chem4Word.ACME.Utils;
using Chem4Word.Model2.Annotations;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

namespace Chem4Word.ACME.Adorners.Sketching
{
    public class NRingAdorner : FixedRingAdorner
    {
        public Point EndPoint { get; }
        public Point StartPoint { get; }
        public bool GreyedOut { get; }

        public NRingAdorner([NotNull] UIElement adornedElement, double bondThickness, List<Point> placements, Point startPoint, Point endPoint, bool greyedOut = false) : base(adornedElement, bondThickness, placements, greyedOut: greyedOut)
        {
            PreviewMouseUp += OnPreviewMouseUp_NRingAdorner;
            PreviewKeyDown += OnPreviewKeyDown_NRingAdorner;
            MouseUp += OnMouseUp_NRingAdorner;
            StartPoint = startPoint;
            EndPoint = endPoint;
            GreyedOut = greyedOut;
            Focusable = true;
            Focus();
        }

        private void OnPreviewKeyDown_NRingAdorner(object sender, System.Windows.Input.KeyEventArgs e)
        {
            CurrentEditor.RaiseEvent(e);
        }

        private void OnMouseUp_NRingAdorner(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            CurrentEditor.RaiseEvent(e);
        }

        private void OnPreviewMouseUp_NRingAdorner(object sender, System.Windows.Input.MouseEventArgs e)
        {
            CurrentEditor.RaiseEvent(e);
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            DrawPlacementArrow(drawingContext);
            DrawRingSize(drawingContext);
        }

        public float PixelsPerDip()
        {
            return (float)VisualTreeHelper.GetDpi(this).PixelsPerDip;
        }

        private void DrawRingSize(DrawingContext drawingContext)
        {
            Point pos = (EndPoint - StartPoint) * 0.5 + StartPoint;
            int ringSize = Placements.Count;
            Brush fillBrush;
            if (GreyedOut)
            {
                fillBrush = (Brush)FindResource(Common.BlockedAdornerBrush);
            }
            else
            {
                fillBrush = BondPen.Brush.Clone();
            }
            DrawRingSize(drawingContext, ringSize, pos, PixelsPerDip(), fillBrush, CurrentEditor.Controller.SymbolSize);
        }

        public static void DrawRingSize(DrawingContext drawingContext, int ringSize, Point pos, float pixelsPerDip, Brush fillBrush, double chemistrySymbolSize)
        {
            string ringLabel = ringSize.ToString();
            var symbolText = new GlyphText(ringLabel,
                                           GlyphUtils.SymbolTypeface,
                                           chemistrySymbolSize,
                                           pixelsPerDip);

            Pen boundaryPen = new Pen(fillBrush, 2.0);

            symbolText.MeasureAtCenter(pos);
            var textMetricsBoundingBox = symbolText.TextMetrics.BoundingBox;

            double radius = (textMetricsBoundingBox.BottomRight - textMetricsBoundingBox.TopLeft).Length / 2 * 1.1;

            drawingContext.DrawEllipse(new SolidColorBrush(SystemColors.WindowColor), boundaryPen, pos, radius, radius);
            symbolText.Fill = fillBrush;
            symbolText.DrawAtBottomLeft(textMetricsBoundingBox.BottomLeft, drawingContext);
        }

        private void DrawPlacementArrow(DrawingContext dc)
        {
            Brush fillBrush;
            if (GreyedOut)
            {
                fillBrush = (Brush)FindResource(Common.BlockedAdornerBrush);
            }
            else
            {
                fillBrush = BondPen.Brush.Clone();
            }

            var fatArrow = FatArrowGeometry.GetArrowGeometry(StartPoint, EndPoint);
            dc.DrawGeometry(fillBrush, null, fatArrow);
        }

        ~NRingAdorner()
        {
            PreviewMouseUp -= OnPreviewMouseUp_NRingAdorner;
            PreviewKeyDown -= OnPreviewKeyDown_NRingAdorner;
            MouseUp -= OnMouseUp_NRingAdorner;
        }
    }
}