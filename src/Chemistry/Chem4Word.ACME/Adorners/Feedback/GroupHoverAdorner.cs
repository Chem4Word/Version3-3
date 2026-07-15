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
    public class GroupHoverAdorner : BaseHoverAdorner
    {
        private const double Spacing = 2.0;

        public GroupHoverAdorner(UIElement adornedElement, GroupVisual targetedVisual) 
            : base(adornedElement, targetedVisual)
        {
            //calls directly into base constructor
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);
            DrawBrackets(TargetedVisual.ContentBounds);
        }
    }
}
