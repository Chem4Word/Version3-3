// ---------------------------------------------------------------------------
//  Copyright (c) 2026, The .NET Foundation.
//  This software is released under the Apache Licence, Version 2.0.
//  The licence and further copyright text can be found in the file LICENCE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.ACME.Controls;
using Chem4Word.ACME.Drawing.Visuals;
using Chem4Word.Model2;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace Chem4Word.ACME.Adorners.Selectors
{
    public class ElectronPusherSelectionAdorner : BaseSelectionAdorner
    {
        private enum DraggedControlPoint
        {
            None,
            FirstControlPoint,
            SecondControlPoint
        }

        private DraggedControlPoint _draggedControlPoint;

        private bool _resizing;

        private readonly SolidColorBrush _solidColorBrush;
        private readonly Pen _dashPen;
        private readonly double _thumbWidth;
        private Point _firstControlPointTemp;
        private Point _secondControlPointTemp;
        private readonly double _halfThumbWidth;
        private Point _newSecondControlPoint;
        private Point _newFirstControlPoint;
        private readonly ElectronPusherVisual _adornedVisual;

        private const double DisplacementOffsetFactor = 0.5;

        public bool Resizing
        {
            get
            {
                return _resizing;
            }
            set
            {
                _resizing = value;
                EditController.HighlightActive = !value;
            }
        }

        public ElectronPusher ParentPusher { get; }

        public double Length
        {
            get
            {
                return (ParentPusher.EndPoint - ParentPusher.StartPoint).Length;
            }
        }

        public ElectronPusherSelectionAdorner(EditorCanvas editorCanvas, ElectronPusherVisual electronPusherVisual) : base(editorCanvas)
        {
            _adornedVisual = electronPusherVisual;
            ParentPusher = electronPusherVisual.ParentPusher;

            _firstControlPointTemp = AdjustedControlPoint(ParentPusher.FirstControlPoint, ParentPusher.StartPoint);
            _secondControlPointTemp = AdjustedControlPoint(ParentPusher.SecondControlPoint, ParentPusher.EndPoint);

            _solidColorBrush = (SolidColorBrush)FindResource("GrabHandleFillBrush");
            _dashPen = new Pen((SolidColorBrush)FindResource(AcmeConstants.DrawAdornerBrush), 1);
            _thumbWidth = 10;
            _halfThumbWidth = _thumbWidth / 2;

            BindHandlers();
        }

        private void BuildHandle(DrawingContext drawingContext, Point centre, Brush handleFillBrush, Pen handleBorderPen)
        {
            double radius = Math.Min(_halfThumbWidth, Length / 5);
            drawingContext.DrawEllipse(handleFillBrush, handleBorderPen, centre, radius, radius);
        }

        protected void BindHandlers()
        {
            //detach the handlers to stop them interfering with dragging
            DetachHandlers();

            MouseLeftButtonDown += ThisAdorner_MouseLeftButtonDown;
            MouseLeftButtonUp += ThisAdorner_MouseLeftButtonUp;
            MouseMove += ThisAdorner_MouseMove;
        }

        private void ThisAdorner_MouseMove(object sender, MouseEventArgs e)
        {
            CurrentLocation = e.GetPosition(CurrentEditor);
            Keyboard.Focus(this);

            if (Resizing)
            {
                Mouse.Capture((ElectronPusherSelectionAdorner)sender);
                if (_draggedControlPoint == DraggedControlPoint.FirstControlPoint)
                {
                    _firstControlPointTemp = CurrentLocation;
                }
                else if (_draggedControlPoint == DraggedControlPoint.SecondControlPoint)
                {
                    _secondControlPointTemp = CurrentLocation;
                }

                InvalidateVisual();

                EditController.SendStatus((AcmeConstants.UnlockStatusText, "", ""));
            }
            else
            {
                if (NearTo(_firstControlPointTemp, CurrentLocation) || NearTo(_secondControlPointTemp, CurrentLocation))
                {
                    Cursor = Cursors.SizeAll;
                }
            }

            e.Handled = true;
        }

        private void ThisAdorner_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (Resizing)
            {
                ((EditController)CurrentEditor.Controller).EndElectronPusherResize(ParentPusher, _newFirstControlPoint, _newSecondControlPoint);
            }

            Resizing = false;
        }

        private void ThisAdorner_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            OriginalLocation = e.GetPosition(CurrentEditor);
            CurrentLocation = OriginalLocation;
            if (NearTo(_firstControlPointTemp, CurrentLocation))
            {
                _draggedControlPoint = DraggedControlPoint.FirstControlPoint;
            }
            else if (NearTo(_secondControlPointTemp, CurrentLocation))
            {
                _draggedControlPoint = DraggedControlPoint.SecondControlPoint;
            }

            Resizing = _draggedControlPoint == DraggedControlPoint.FirstControlPoint
                       || _draggedControlPoint == DraggedControlPoint.SecondControlPoint;

            e.Handled = true;
        }

        private bool NearTo(Point controlPoint, Point currentLocation)
        {
            return (currentLocation - controlPoint).LengthSquared <= _halfThumbWidth * _halfThumbWidth;
        }

        private Point UnadjustedControlPoint(Point adjustedPoint, Point reference)
        {
            Vector offsetVector = adjustedPoint - reference;
            return offsetVector / DisplacementOffsetFactor + reference;
        }

        private Point AdjustedControlPoint(Point unadjustedPoint, Point reference)
        {
            Vector offsetVector = unadjustedPoint - reference;
            return offsetVector * DisplacementOffsetFactor + reference;
        }

        public Point CurrentLocation { get; set; }

        public Point OriginalLocation { get; set; }

        protected new void DetachHandlers()
        {
            MouseLeftButtonDown -= ThisAdorner_MouseLeftButtonDown;
            MouseLeftButtonUp -= ThisAdorner_MouseLeftButtonUp;
            MouseMove -= ThisAdorner_MouseMove;
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            //draw dashed lines from the control points to the ends of the arrow
            Point? startPoint = ParentPusher.StartPoint;
            Point? endPoint = ParentPusher.EndPoint;
            Point? lineStart = startPoint;
            Point? lineEnd = endPoint;

            List<DrawingVisual> secondChemistryVisuals = new List<DrawingVisual>();
            foreach (StructuralObject chemistry in ParentPusher.EndChemistries)
            {
                secondChemistryVisuals.Add(CurrentEditor.ChemicalVisuals[chemistry] as ChemicalVisual);
            }

            ChemicalVisual startVisual = null;

            switch (ParentPusher.StartChemistry)
            {
                case Atom _:
                case Bond _:
                    startVisual = CurrentEditor.ChemicalVisuals[ParentPusher.StartChemistry] as ChemicalVisual;
                    break;

                case Electron electron:
                    {
                        AtomVisual parentAtomVisual = CurrentEditor.ChemicalVisuals[electron.Parent] as AtomVisual;
                        foreach (ElectronVisual childVisual in parentAtomVisual.ElectronVisuals.Values)
                        {
                            if (childVisual.ParentElectron == ParentPusher.StartChemistry)
                            {
                                startVisual = childVisual;
                            }
                        }

                        break;
                    }
            }

            ElectronPusher electronPusher = ParentPusher;

            ElectronPusherVisual.RecalcPusherMetrics(electronPusher, startVisual, secondChemistryVisuals, ref lineStart, ref lineEnd, startPoint.Value, endPoint.Value);

            var traceBrush = (FindResource(AcmeConstants.AdornerFillBrush) as SolidColorBrush).Clone();
            traceBrush.Opacity = 1.0;

            _newFirstControlPoint = UnadjustedControlPoint(_firstControlPointTemp, ParentPusher.StartPoint);
            _newSecondControlPoint = UnadjustedControlPoint(_secondControlPointTemp, ParentPusher.EndPoint);

            drawingContext.DrawLine(_dashPen, ParentPusher.StartPoint, _firstControlPointTemp);
            drawingContext.DrawLine(_dashPen, ParentPusher.EndPoint, _secondControlPointTemp);

            //draw the handles
            BuildHandle(drawingContext, _firstControlPointTemp, _solidColorBrush, _dashPen);
            BuildHandle(drawingContext, _secondControlPointTemp, _solidColorBrush, _dashPen);
#if SHOWBOUNDS
            Brush overlayBrush = new SolidColorBrush(Colors.LightSalmon) { Opacity = 20 / 100.0 };
            Pen overlayPen = new Pen(overlayBrush, 2.0);
#else
            Brush overlayBrush = Brushes.Transparent;

            Pen overlayPen = new Pen(overlayBrush, 2.0)
            {
                DashStyle = DashStyles.Dash
            };
#endif

            var pusherVisual = CurrentEditor.ChemicalVisuals[ParentPusher];

            //delegated the drawing of the arrow to the parent pusher visual
            (pusherVisual as ElectronPusherVisual)?.DrawMainArrowGeometry(drawingContext, new Pen(traceBrush, 3), secondChemistryVisuals);

            Rect adornedElementRect = _adornedVisual.Bounds;
            Rect newRect1 = new Rect(new Point(_firstControlPointTemp.X - _thumbWidth, _firstControlPointTemp.Y - _thumbWidth),
                                     new Point(_firstControlPointTemp.X + _thumbWidth, _firstControlPointTemp.Y + _thumbWidth));
            Rect newRect2 = new Rect(new Point(_secondControlPointTemp.X - _thumbWidth, _secondControlPointTemp.Y - _thumbWidth),
                                     new Point(_secondControlPointTemp.X + _thumbWidth, _secondControlPointTemp.Y + _thumbWidth));

            adornedElementRect.Union(newRect1);
            adornedElementRect.Union(newRect2);
            adornedElementRect.Union(pusherVisual.Drawing.Bounds);

            //draw a big transparent rectangle over the whole thing to capture hit testing
            drawingContext.DrawRectangle(overlayBrush, overlayPen, adornedElementRect);
        }
    }
}
