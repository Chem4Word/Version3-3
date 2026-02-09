// ---------------------------------------------------------------------------
//  Copyright (c) 2026, The .NET Foundation.
//  This software is released under the Apache Licence, Version 2.0.
//  The licence and further copyright text can be found in the file LICENCE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.ACME.Enums;
using Chem4Word.ACME.Graphics;
using Chem4Word.Core.Helpers;
using Chem4Word.Model2;
using Chem4Word.Model2.Enums;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

namespace Chem4Word.ACME.Drawing.Visuals
{
    /// <summary>
    /// Draws an electron pusher arrow
    /// </summary>
    public class ElectronPusherVisual : AtomVisual
    {
        #region Properties

        public ElectronPusher ParentPusher { get; }

        #endregion Properties

        #region Constructors

        public ElectronPusherVisual(ElectronPusher ep)
        {
            ParentPusher = ep;
        }

        #endregion Constructors

        public override void Render()
        {
            var firstChemistryVisual = ChemicalVisuals[ParentPusher.StartChemistry] as ChemicalVisual;

            Point? startPoint = new Point(0, 0);
            Point? endPoint = new Point(0, 0);

            Point? lineStart = new Point(0, 0);
            Point? lineEnd = new Point(0, 0);
            List<DrawingVisual> secondChemistryVisuals = new List<DrawingVisual>();

            foreach (StructuralObject structuralObject in ParentPusher.EndChemistries)
            {
                secondChemistryVisuals.Add(ChemicalVisuals[structuralObject]);
            }

            ElectronPusher electronPusher = ParentPusher;
            RecalcPusherMetrics(electronPusher, firstChemistryVisual, secondChemistryVisuals,
                                ref lineStart, ref lineEnd,
                                electronPusher.FirstControlPoint, electronPusher.SecondControlPoint);

            Pen outline = new Pen(Brushes.DarkRed, 2);
            BezierArrow arrow = null;

            if (ParentPusher.PusherType == ElectronPusherType.CurlyArrow || ParentPusher.PusherType == ElectronPusherType.DoubleArrow)
            {
                bool doubleHeaded = ParentPusher.PusherType == ElectronPusherType.DoubleArrow;
                arrow = new BezierArrow
                {
                StartPoint = lineStart.Value,
                    FirstControlPoint = ParentPusher.FirstControlPoint,
                    SecondControlPoint = ParentPusher.SecondControlPoint,
                EndPoint = lineEnd.Value,
                    MaxHeadLength = ACMEGlobals.ElectronPusherHeadSize,
                    HeadFractionLength = ACMEGlobals.ElectronPusherHeadFractionLength,
                    ArrowHeadClosed = true,
                    ArrowEnds = ParentPusher.PusherType == ElectronPusherType.CurlyArrow
                                  ? ArrowEnds.End
                                  : ArrowEnds.Both,
                    Stroke = outline.Brush
                };
            }
            else if (ParentPusher.PusherType == ElectronPusherType.FishHook)
            {
                var newFishHook = new FishHookArrow
                {
                    StartPoint = lineStart.Value,
                FirstControlPoint = ParentPusher.FirstControlPoint,
                SecondControlPoint = ParentPusher.SecondControlPoint,
                    EndPoint = lineEnd.Value,
                MaxHeadLength = ACMEGlobals.ElectronPusherHeadSize,
                HeadFractionLength = ACMEGlobals.ElectronPusherHeadFractionLength,
                    ArrowHeadClosed = true,
                  
                    Stroke = outline.Brush
            };
                var offset = newFishHook.BarbOffset(ParentPusher.StartPoint, ParentPusher.EndPoint);
                newFishHook.EndPoint -= offset;
                arrow = newFishHook;
            }

            //draw the arrow
            using (DrawingContext dc = RenderOpen())
            {
                arrow.DrawArrowGeometry(dc, outline, arrow.Stroke);

                //draw an overlay

                SolidColorBrush outliner;
#if SHOWBOUNDS
                outliner = new SolidColorBrush(Colors.LightGreen)
                {
                    Opacity = 0.4d
                };

                dc.DrawLine(new Pen(outliner,3),  lineStart.Value, ParentPusher.FirstControlPoint);
                dc.DrawRectangle(outliner, new Pen(outliner, 3), new Rect(ParentPusher.FirstControlPoint, new Size(5,5)));
                dc.DrawLine(new Pen(outliner, 3), lineEnd.v, ParentPusher.SecondControlPoint);
                dc.DrawEllipse(outliner, new Pen(outliner, 3), ParentPusher.SecondControlPoint, 5, 5);
#else

#endif
                outliner = new SolidColorBrush(Colors.Transparent) { Opacity = 0d };
                Pen outlinePen = new Pen(outliner, arrow.StrokeThickness * 2);
                arrow.DrawArrowGeometry(dc, outlinePen, outliner);

                //if the arrow ends in empty space between two atoms, draw a dashed line
                if (secondChemistryVisuals.Count == 2)
                {
                    Brush dashedBrush = new SolidColorBrush(Colors.DarkRed)
                    {
                        Opacity = 0.5d
                    };
                    Pen dashedPen = new Pen(dashedBrush, 1)
                    {
                        DashStyle = DashStyles.Dash,
                    };

                    AtomVisual secondChemistryVisual1 = secondChemistryVisuals[0] as AtomVisual;
                    AtomVisual secondChemistryVisual2 = secondChemistryVisuals[1] as AtomVisual;
                    Point? start =
                        secondChemistryVisual1.GetIntersection(secondChemistryVisual1.Position,
                                                               secondChemistryVisual2.Position);
                    Point? end =
                        secondChemistryVisual2.GetIntersection(secondChemistryVisual1.Position,
                                                               secondChemistryVisual2.Position);
                    dc.DrawLine(dashedPen, start ?? secondChemistryVisual1.Position, end ?? secondChemistryVisual2.Position);
                }
            }
        }

        public static void RecalcPusherMetrics(ElectronPusher electronPusher, ChemicalVisual firstChemistryVisual,
                                               List<DrawingVisual> secondChemistryVisuals,
                                               ref Point? lineStart, ref Point? lineEnd, Point firstControlPoint,
                                               Point secondControlPoint)
        {
            //first, work out where the pusher should start from
            if (firstChemistryVisual is AtomVisual av)
            {
                //TODO: why would this be null?
                //answer: it's null because the first control point sits *inside* the hull of the drawing visual!
                //so we push it out ten times in the right direction
                lineStart = av.GetIntersection(av.ParentAtom.Position, av.ParentAtom.Position + (firstControlPoint - av.ParentAtom.Position) * 10) ?? av.Position;
            }
            else if (firstChemistryVisual is BondVisual bv)
            {
                //TODO: why would this be null?
                //answer: it's null because the first control point sits *inside* the hull of the drawing visual!
                //so we push it out ten times in the right direction
                lineStart = bv.GetIntersection(bv.ParentBond.MidPoint, bv.ParentBond.MidPoint + (firstControlPoint - bv.ParentBond.MidPoint) * 10) ?? bv.ParentBond.MidPoint;
            }

            //now work out where the pusher should end

            if (secondChemistryVisuals.Count == 2)
            {
                //both chemistry visuals should be atoms
                var av1 = secondChemistryVisuals[0] as AtomVisual;
                var av2 = secondChemistryVisuals[1] as AtomVisual;
                lineEnd = GeometryTool.GetMidPoint(av1.Position, av2.Position);
            }
            else
            {
                switch (secondChemistryVisuals[0])
                {
                    case AtomVisual a:
                        lineEnd = a.GetIntersection(a.Position, a.Position + (secondControlPoint - a.Position) * 10) ?? a.Position;
                        break;

                    case BondVisual bv:
                        lineEnd = bv.GetIntersection(bv.ParentBond.MidPoint, bv.ParentBond.MidPoint + (secondControlPoint - bv.ParentBond.MidPoint) * 10) ??
                                  bv.ParentBond.MidPoint;
                        break;
                }
            }
        }

        protected override HitTestResult HitTestCore(PointHitTestParameters hitTestParameters)
        {
            return new PointHitTestResult(this, hitTestParameters.HitPoint);
        }
    }
}
