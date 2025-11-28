// ---------------------------------------------------------------------------
//  Copyright (c) 2026, The .NET Foundation.
//  This software is released under the Apache Licence, Version 2.0.
//  The licence and further copyright text can be found in the file LICENCE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.ACME.Drawing.Visuals;
using Chem4Word.Model2.Annotations;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace Chem4Word.ACME.Adorners.Feedback
{
    public abstract class BaseHoverAdorner : Adorner
    {
        public SolidColorBrush BracketBrush { get; }
        public Pen BracketPen { get; }
        public ChemicalVisual TargetedVisual { get; }

        protected BaseHoverAdorner([NotNull] UIElement adornedElement, [NotNull] ChemicalVisual targetedVisual) : base(adornedElement)
        {
            BracketBrush = new SolidColorBrush(ACMEGlobals.HoverAdornerColor);

            BracketPen = new Pen(BracketBrush, AcmeConstants.HoverAdornerThickness);
            BracketPen.StartLineCap = PenLineCap.Round;
            BracketPen.EndLineCap = PenLineCap.Round;

            TargetedVisual = targetedVisual;

            var myAdornerLayer = AdornerLayer.GetAdornerLayer(adornedElement);
            myAdornerLayer.Add(this);
        }
    }
}