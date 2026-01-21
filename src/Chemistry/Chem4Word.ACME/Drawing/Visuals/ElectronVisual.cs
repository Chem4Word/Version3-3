// ---------------------------------------------------------------------------
//  Copyright (c) 2026, The .NET Foundation.
//  This software is released under the Apache Licence, Version 2.0.
//  The licence and further copyright text can be found in the file LICENCE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.ACME.Drawing.Text;
using Chem4Word.Core.Helpers;
using Chem4Word.Model2;
using Chem4Word.Model2.Enums;
using System.Windows;
using System.Windows.Media;

namespace Chem4Word.ACME.Drawing.Visuals
{
    /// <summary>
    /// Draws a symbol indicating a single electron or an electron pair, attached to an atom
    /// </summary>
    public class ElectronVisual : AtomVisual
    {
        #region Properties

        public AtomVisual ParentVisual { get; protected set; }
        public DrawingContext Context { get; set; }

        public Electron ParentElectron { get; set; }
        public AtomTextMetrics ParentMetrics { get; set; }
        public AtomTextMetrics Metrics { get; protected set; }
        public AtomTextMetrics HydrogenMetrics { get; set; }
        public AtomTextMetrics ChargeMetrics { get; set; }
        public AtomTextMetrics ElectronMetrics { get; set; }

        #endregion Properties

        #region Constructors

        public ElectronVisual(AtomVisual parentVisual,
                           DrawingContext drawingContext, AtomTextMetrics mainAtomMetrics, AtomTextMetrics hMetrics, AtomTextMetrics chargeMetrics)
        {
            Context = drawingContext;
            ParentVisual = parentVisual;
            ParentMetrics = mainAtomMetrics;
            HydrogenMetrics = hMetrics;
            ChargeMetrics = chargeMetrics;
            SymbolSize = parentVisual.SymbolSize;
            Fill = parentVisual.Fill;
        }

        #endregion Constructors

        public override void Render()
        {
            Point center = ParentVisual.Position;

            //first, work out from the placement property what the vector is
            double offsetAngle = 45 * (int)ParentElectron.Placement.Value;

            Matrix rotator = new Matrix();
            rotator.Rotate(offsetAngle);

            //make it long enough to clear the atom symbol
            Vector placementVector = 1000 * GeometryTool.ScreenNorth * rotator;

            //and intersect it with the convex hull to find the edge point
            Point? endPoint = ParentVisual.GetIntersection(center, center + placementVector);

            //now extend it by the standoff distance plus the size of the electron symbol
            Vector unitVector = placementVector;
            unitVector.Normalize();
            placementVector = endPoint.Value - center + unitVector * (ParentVisual.SymbolSize / 4);
            Point electronCenter = center + placementVector;

            //this is the centre of the electron symbol
            //if we're drawing a radical, then draw a simple dot
            double radius = SymbolSize / 10;
            double offset;
            if (ParentElectron.Count == 1)
            {
                offset = radius;
                Context.DrawEllipse(Fill, null, electronCenter, radius, radius);
            }
            else //two electrons - draw a pair of dots or a line for carbenoids
            {
                Pen pen = new Pen(Fill, AcmeConstants.BondThickness)
                {
                    StartLineCap = PenLineCap.Square,
                    EndLineCap = PenLineCap.Square
                };
                radius = SymbolSize / 15;
                Vector perpendicular = unitVector.Perpendicular();

                if (ParentElectron.TypeOfElectron == ElectronType.Carbenoid) //draw a line
                {
                    offset = radius * 4;
                    perpendicular *= offset;
                    Context.DrawLine(pen, electronCenter + perpendicular, electronCenter - perpendicular);
                }
                else //draw two dots
                {
                    offset = radius * 2;
                    perpendicular *= offset;
                    Context.DrawEllipse(Fill, pen, electronCenter + perpendicular, radius, radius);
                    Context.DrawEllipse(Fill, pen, electronCenter - perpendicular, radius, radius);
                }
            }

            //now draw a transparent circle on top of the electron visual to aid hit testing
            Context.DrawEllipse(Brushes.Transparent, null, electronCenter, offset, offset);
            Metrics = new AtomTextMetrics
            {
                TotalBoundingBox = new Rect(new Point(electronCenter.X - offset, electronCenter.Y - offset),
                                            new Point(electronCenter.X + offset, electronCenter.Y + offset)),
            };
        }
    }
}
