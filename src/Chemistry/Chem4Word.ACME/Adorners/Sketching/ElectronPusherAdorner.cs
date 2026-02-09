// ---------------------------------------------------------------------------
//  Copyright (c) 2026, The .NET Foundation.
//  This software is released under the Apache Licence, Version 2.0.
//  The licence and further copyright text can be found in the file LICENCE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------
using Chem4Word.ACME.Controls;
using Chem4Word.ACME.Drawing.Visuals;
using Chem4Word.ACME.Graphics;
using Chem4Word.Model2;
using Chem4Word.Model2.Enums;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace Chem4Word.ACME.Adorners.Sketching
{
    public struct PusherMetrics
    {
        public Point StartPoint;
        public Point EndPoint;
        public Point FirstControlPoint;
        public Point SecondControlPoint;
    }

    public class ElectronPusherAdorner : Adorner
    {
        public Pen BondPen { get; }
        public bool CanRender { get; set; }
        public EditorCanvas CurrentEditor { get; }
        public Point FirstControlPoint { get; set; }

        public StructuralObject Target { get; set; }
        public StructuralObject Source { get; set; }

        public Point SecondControlPoint { get; }
        public BezierArrow MyArrow { get; private set; }
        public Pen DrawPen { get; set; }

        public ElectronPusherAdorner(StructuralObject startChemistry, UIElement adornedElement,
                                     Point currentPosition, ElectronPusherType type,
                                     double drawThickness) : base(adornedElement)
        {
            CurrentEditor = (EditorCanvas)adornedElement;
            SolidColorBrush drawingBrush;

            Source = startChemistry; //cannot be null
            Target = CurrentEditor.ActiveChemistry; //can be null

            bool greyedOut = !(Target is Atom || Target is Bond);
            CanRender = false;
            //greyed out if not anchored on a targetable structural object
            drawingBrush = (SolidColorBrush)FindResource(greyedOut ? AcmeConstants.BlockedAdornerBrush : AcmeConstants.DrawAdornerBrush);

            DrawPen = new Pen(drawingBrush, drawThickness);

            var myAdornerLayer = AdornerLayer.GetAdornerLayer(adornedElement);
            myAdornerLayer.Add(this);
            Point start, end;
            switch (Source)
            {
                case Electron electron:
                    start = (CurrentEditor.ChemicalVisuals[electron] as ElectronVisual).Centroid;
                    break;

                case Atom atom:
                    start = atom.Position;
                    break;

                case Bond bond:
                    start = bond.MidPoint;
                    break;

                default:
                    start = new Point(0, 0);
                    break;
            }

            switch (Target)
            {
                case Atom atom when Target != Source:
                    end = atom.Position;
                    break;

                case Bond bond when Target != Source:
                    end = bond.MidPoint;
                    break;

                default: //null
                    end = currentPosition;
                    break;
            }

            //determine whether we are in cramped space mode
            double length = (end - start).Length;
            bool crampedSpaceMode = length < CurrentEditor.Controller.Model.MeanBondLength /2;

            var proposedMetrics = DesiredGeometry(crampedSpaceMode, start, currentPosition, end);
            
            //draw the arrow, locking if necessary to the target
            CanRender = (currentPosition - start).Length >= 10;
            FirstControlPoint = proposedMetrics.FirstControlPoint;
            SecondControlPoint = proposedMetrics.SecondControlPoint;
            if (CanRender)
            {
                MyArrow = new BezierArrow
                {
                    StartPoint = proposedMetrics.StartPoint,
                    FirstControlPoint = proposedMetrics.FirstControlPoint,
                    SecondControlPoint = proposedMetrics.SecondControlPoint,
                    EndPoint = proposedMetrics.EndPoint,
                    MaxHeadLength = ACMEGlobals.ElectronPusherHeadSize,
                    HeadFractionLength = ACMEGlobals.ElectronPusherHeadFractionLength,
                    ArrowHeadClosed = true,
                    Stroke = DrawPen.Brush
                };
            }
            InvalidateVisual();
        }
        /// <summary>
        /// Calculates the control points for the electron pusher arrow
        /// </summary>
        /// <param name="crampedSpace">Switch which indicates whether the drawing space is 'cramped'</param>
        /// <param name="start">Start point of the arrow</param>
        /// <param name="currentMousePosition">Where we are currently drawing</param>
        /// <param name="end">Optional eventual arrow end point: provide if you over a bond or atom</param>
        /// <returns></returns>
        public static PusherMetrics DesiredGeometry(bool crampedSpace, Point start, Point currentMousePosition, Point? end = null)
        {
            Point endPoint = end ?? currentMousePosition;

            Vector spanVector = endPoint - start;

            Vector lineOfSight = currentMousePosition - start;
            int sense;
            //determine whether the vector lies clockwise or counterclockwise of any target line of sight vector

            if (Vector.CrossProduct(spanVector, lineOfSight) > 0)
            {
                sense = 1;
            }
            else
            {
                sense = -1;
            }

            //now calculate the control points for the arrow
            Vector perpOffset = spanVector / 2;

            Vector separation;

            int angle;

            if (crampedSpace)
            {
                angle = 90;
                separation = spanVector;
                perpOffset *= 4;
            }
            else
            {
                angle = 60;
                separation = perpOffset;
            }

            Matrix rotateMatrix = new Matrix();
            rotateMatrix.Rotate(sense * angle);
            return new PusherMetrics
            {
                EndPoint = endPoint,
                FirstControlPoint = start + (perpOffset * rotateMatrix),
                SecondControlPoint = start + (perpOffset * rotateMatrix) + separation,
                StartPoint = start
            };
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            if (CanRender)
            {
                base.OnRender(drawingContext);
                MyArrow.DrawArrowGeometry(drawingContext, DrawPen, MyArrow.Stroke);
            }
        }

        public static (Point StartPoint, Point EndPoint, Point FirstControlPoint, Point SecondControlPoint)
            RecalcControlPoints(ElectronPusher electronPusher, double modelMeanBondLength)
        {
            Point startPoint;
            Point newEndPoint = electronPusher.EndPoint;
            if (electronPusher.StartChemistry is Atom atom)
            {
                startPoint = atom.Position;
            }
            else
            //startChemistry is Bond
            {
                Bond bond = electronPusher.StartChemistry as Bond;
                startPoint = bond.MidPoint;
            }
            Vector spanVector = newEndPoint - startPoint;
            Vector halfSpan = spanVector / 2;
            Vector span = halfSpan;
            float angle = 60;
            Matrix rotateMatrix = new Matrix();

            double minOffset = modelMeanBondLength / 4;

            //squish out a short arrow into a half circle
            if (halfSpan.Length < minOffset)
            {
                halfSpan.Normalize();
                angle = 90;
                halfSpan *= minOffset;
                span = spanVector;
            }
            if (!(Vector.AngleBetween(electronPusher.FirstControlPoint - startPoint, spanVector) < 0))
            {
                angle = -angle;
            }

            rotateMatrix.Rotate(angle);
            Vector controlVector = halfSpan;
            Point firstPoint = startPoint + (controlVector * rotateMatrix);
            rotateMatrix.Rotate(-angle);
            Point secondPoint = firstPoint + (span * rotateMatrix);
            return (startPoint, newEndPoint, firstPoint, secondPoint);
        }
    }
}
