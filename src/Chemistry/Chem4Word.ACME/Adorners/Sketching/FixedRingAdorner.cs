﻿// ---------------------------------------------------------------------------
//  Copyright (c) 2025, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.ACME.Behaviors;
using Chem4Word.ACME.Controls;
using Chem4Word.ACME.Models;
using Chem4Word.ACME.Utils;
using Chem4Word.Model2.Annotations;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace Chem4Word.ACME.Adorners.Sketching
{
    public class FixedRingAdorner : Adorner
    {
        public Pen BondPen { get; }
        public List<Point> Placements { get; }
        public bool Unsaturated { get; }
        public EditorCanvas CurrentEditor { get; }
        private readonly RingDrawer _ringDrawer;

        public FixedRingAdorner([NotNull] UIElement adornedElement, double bondThickness, List<Point> placements,
                                bool unsaturated = false, bool greyedOut = false) : base(adornedElement)
        {
            Cursor = CursorUtils.Pencil;
            if (!greyedOut)
            {
                BondPen = new Pen((SolidColorBrush)FindResource(Common.DrawAdornerBrush), bondThickness);
            }
            else
            {
                BondPen = new Pen((SolidColorBrush)FindResource(Common.BlockedAdornerBrush), bondThickness);
            }

            var myAdornerLayer = AdornerLayer.GetAdornerLayer(adornedElement);
            myAdornerLayer.Add(this);
            Placements = placements;
            Unsaturated = unsaturated;
            CurrentEditor = (EditorCanvas)adornedElement;
            _ringDrawer = new RingDrawer(this);
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            var newPlacements = new List<NewAtomPlacement>();

            RingBehavior.FillExistingAtoms(Placements, Placements, newPlacements, CurrentEditor);

            _ringDrawer.DrawNRing(drawingContext, newPlacements);
        }
    }
}