// ---------------------------------------------------------------------------
//  Copyright (c) 2025, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.ACME.Controls;
using Chem4Word.ACME.Drawing.Visuals;
using Chem4Word.ACME.Utils;
using Chem4Word.Core.Helpers;
using Chem4Word.Model2;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

// ReSharper disable once IdentifierTypo
namespace Chem4Word.ACME.Adorners.Selectors
{
    public class MoleculeSelectionAdorner : SingleObjectSelectionAdorner
    {
        private const double MinTravelWidth = 10;

        //static as they need to be set only when the adorner is first created
        private static double? _thumbWidth;

        private static double _halfThumbWidth;
        private static double _rotateThumbWidth;

        //some things to grab hold of

        //side handles
        protected readonly DragHandle LeftHandle;

        protected readonly DragHandle RightHandle;
        protected readonly DragHandle BottomHandle;
        protected readonly DragHandle TopHandle;

        //corner handles
        protected readonly DragHandle TopLeftHandle;

        protected readonly DragHandle TopRightHandle;
        protected readonly DragHandle BottomLeftHandle;
        protected readonly DragHandle BottomRightHandle;

        //the rotator
        protected readonly DragHandle RotateHandle; //Grab hold of this to rotate the molecule

        //flags
        protected bool Resizing;

        protected bool IsRotating;

        private double _rotateAngle;
        private Point _centroid;
        private Point _rotateThumbPos;
        private Snapper _rotateSnapper;
        private double _yPlacement;
        private double _xPlacement;
        private ScaleOperationParams _scaleOperationParams;
        private Point _newThumbPos;

        public MoleculeSelectionAdorner(EditorCanvas currentEditor, List<StructuralObject> objects)
            : base(currentEditor, objects)
        {
            if (_thumbWidth == null)
            {
                _thumbWidth = 15;
                _halfThumbWidth = _thumbWidth.Value / 2;
                _rotateThumbWidth = _thumbWidth.Value;
            }

            BuildAdornerCorner(ref LeftHandle, Cursors.SizeWE);
            BuildAdornerCorner(ref RightHandle, Cursors.SizeWE);
            BuildAdornerCorner(ref BottomHandle, Cursors.SizeNS);
            BuildAdornerCorner(ref TopHandle, Cursors.SizeNS);

            BuildAdornerCorner(ref TopLeftHandle, Cursors.SizeNWSE);
            BuildAdornerCorner(ref TopRightHandle, Cursors.SizeNESW);
            BuildAdornerCorner(ref BottomLeftHandle, Cursors.SizeNESW);
            BuildAdornerCorner(ref BottomRightHandle, Cursors.SizeNWSE);

            BuildRotateThumb(out RotateHandle);

            SetCentroid();
            SetBoundingBox();
            AttachHandlers();

            IsHitTestVisible = true;
            Focusable = true;
            Focus();
        }

        #region Properties

        public double AspectRatio { get; set; }

        public new Rect BoundingBox { get; set; }

        private new bool IsWorking => Dragging || Resizing || IsRotating;
        public Point RotateThumbPosition { get; set; }

        #endregion Properties

        #region Methods

        protected new void AttachHandlers()
        {
            //detach the base class handlers to stop them interfering
            DisableHandlers();

            //wire up the event handling
            //starting the drag
            TopLeftHandle.DragStarted += OnResizeStarted;
            TopRightHandle.DragStarted += OnResizeStarted;
            BottomLeftHandle.DragStarted += OnResizeStarted;
            BottomRightHandle.DragStarted += OnResizeStarted;

            LeftHandle.DragStarted += OnResizeStarted;
            RightHandle.DragStarted += OnResizeStarted;
            TopHandle.DragStarted += OnResizeStarted;
            BottomHandle.DragStarted += OnResizeStarted;

            //side handles
            LeftHandle.DragDelta += HandleDragDelta;
            RightHandle.DragDelta += HandleDragDelta;
            BottomHandle.DragDelta += HandleDragDelta;
            TopHandle.DragDelta += HandleDragDelta;

            //dragging
            //corner handles
            TopLeftHandle.DragDelta += HandleDragDelta;
            TopRightHandle.DragDelta += HandleDragDelta;
            BottomLeftHandle.DragDelta += HandleDragDelta;
            BottomRightHandle.DragDelta += HandleDragDelta;

            //completing the dragging
            LeftHandle.DragCompleted += OnResizeCompleted;
            RightHandle.DragCompleted += OnResizeCompleted;
            BottomHandle.DragCompleted += OnResizeCompleted;
            TopHandle.DragCompleted += OnResizeCompleted;

            TopLeftHandle.DragCompleted += OnResizeCompleted;
            TopRightHandle.DragCompleted += OnResizeCompleted;
            BottomLeftHandle.DragCompleted += OnResizeCompleted;
            BottomRightHandle.DragCompleted += OnResizeCompleted;
        }

        private void OnResizeCompleted(object sender, DragCompletedEventArgs e)
        {
            Resizing = false;

            var xTravel = e.HorizontalChange;
            var yTravel = e.VerticalChange;
            var finalLocation = new Point(_scaleOperationParams.Finish.X + xTravel, _scaleOperationParams.Finish.Y + yTravel);
            var projection = (finalLocation - _scaleOperationParams.Start).Project(_scaleOperationParams.OriginalSense);

            SetScalingFactors(projection, out var xScale, out var yScale);
            LastOperation = new ScaleTransform(xScale, yScale, _scaleOperationParams.Start.X, _scaleOperationParams.Start.Y);

            EditController.TransformObjects(LastOperation, AdornedObjects);

            SetBoundingBox();
            ResizeCompleted?.Invoke(this, e);
            SetCentroid();
            InvalidateVisual();

            (sender as DragHandle)?.ReleaseMouseCapture();

            _scaleOperationParams = null;
        }

        private void SetScalingFactors(Vector projection, out double xScale, out double yScale)
        {
            if (projection.X == 0)
            {
                xScale = 1.0;
            }
            else
            {
                xScale = projection.X / _scaleOperationParams.OriginalSense.X;
            }

            if (projection.Y == 0)
            {
                yScale = 1.0;
            }
            else
            {
                yScale = projection.Y / _scaleOperationParams.OriginalSense.Y;
            }
        }

        private void OnResizeStarted(object sender, DragStartedEventArgs e)
        {
            DragHandle handle = (DragHandle)sender;
            DragHandle counterpart = GetCounterpart(handle);

            //get the midpoint of both thumbs
            Point start = counterpart.MidPoint;// - offset;
            Point finish = handle.MidPoint;// - offset;
            _scaleOperationParams = new ScaleOperationParams { Start = start, Finish = finish, Origin = Mouse.GetPosition(CurrentEditor) };
            Resizing = true;
            Dragging = false;
            Keyboard.Focus(this);
            Mouse.Capture((DragHandle)sender);
            SetBoundingBox();
            DragXTravel = 0.0d;
            DragYTravel = 0.0d;
        }

        //used to determining the scale object
        //the start point of the scale is the centre of the counterpart
        //to the handle being dragged (which is the end)
        private DragHandle GetCounterpart(DragHandle handle)
        {
            if (handle == RightHandle)
            {
                return LeftHandle;
            }

            if (handle == LeftHandle)
            {
                return RightHandle;
            }

            if (handle == TopHandle)
            {
                return BottomHandle;
            }

            if (handle == BottomHandle)
            {
                return TopHandle;
            }

            if (handle == TopLeftHandle)
            {
                return BottomRightHandle;
            }

            if (handle == BottomRightHandle)
            {
                return TopLeftHandle;
            }

            if (handle == TopRightHandle)
            {
                return BottomLeftHandle;
            }

            if (handle == BottomLeftHandle)
            {
                return TopRightHandle;
            }

            return null;
        }

        private void BuildRotateThumb(out DragHandle rotateThumb)
        {
            rotateThumb = new DragHandle
            {
                IsHitTestVisible = true
            };

            RotateHandle.Width = _rotateThumbWidth;
            RotateHandle.Height = _rotateThumbWidth;
            RotateHandle.Cursor = Cursors.Hand;
            rotateThumb.Style = (Style)FindResource(AcmeConstants.RotateThumbStyle);
            rotateThumb.DragStarted += OnRotateStarted;
            rotateThumb.DragDelta += OnDragDelta_RotateThumb;
            rotateThumb.DragCompleted += OnRotateCompleted;

            VisualChildren.Add(rotateThumb);
        }

        private void OnRotateCompleted(object sender, DragCompletedEventArgs e)
        {
            if (LastOperation != null)
            {
                EditController.TransformObjects(LastOperation, AdornedObjects);
                SetBoundingBox();
                SetCentroid();

                (sender as DragHandle)?.ReleaseMouseCapture();
            }
            InvalidateVisual();
            _scaleOperationParams = null;
            IsRotating = false;
            Resizing = false;
        }

        private void OnRotateStarted(object sender, DragStartedEventArgs e)
        {
            IsRotating = true;
            if (_rotateAngle == 0.0d)
            {
                //we have not yet rotated anything
                //so take a snapshot of the centroid of the molecule
                SetCentroid();
            }
            else
            {
                //capture the starting point of the thumb
                _rotateThumbPos = RotateHandle.Centroid;
                RotateThumbPosition = _rotateThumbPos;
            }
        }

        private void OnDragDelta_RotateThumb(object sender, DragDeltaEventArgs e)
        {
            if (IsRotating)
            {
                Point mouse = Mouse.GetPosition(CurrentEditor);

                var displacement = mouse - _centroid;

                double snapAngle = Vector.AngleBetween(GeometryTool.ScreenNorth,
                                                       _rotateSnapper.SnapVector(0, displacement));
                _rotateAngle = snapAngle;
                LastOperation = new RotateTransform(snapAngle, _centroid.X, _centroid.Y);

                InvalidateVisual();
            }
        }

        private void SetCentroid()
        {
            _centroid = GeometryTool.GetCentroid(CurrentEditor.GetCombinedBoundingBox(AdornedObjects));
            _rotateSnapper = new Snapper(_centroid, CurrentEditor.Controller as EditController);
        }

        public event DragCompletedEventHandler ResizeCompleted;

        private void SetBoundingBox()
        {
            BoundingBox = CurrentEditor.GetCombinedBoundingBox(AdornedObjects);
            //and work out the aspect ratio for later resizing
            AspectRatio = BoundingBox.Width / BoundingBox.Height;
        }

        private void BuildAdornerCorner(ref DragHandle cornerThumb, Cursor customizedCursor)
        {
            if (cornerThumb != null)
            {
                return;
            }

            cornerThumb = new DragHandle(cursor: customizedCursor);

            SetThumbStyle(cornerThumb);
            VisualChildren.Add(cornerThumb);
        }

        protected virtual void SetThumbStyle(DragHandle cornerThumb)
        {
            cornerThumb.Style = (Style)FindResource(AcmeConstants.GrabHandleStyle);
        }

        #endregion Methods

        #region Overrides

        // Arrange the Adorners.
        protected override Size ArrangeOverride(Size finalSize)
        {
            // desiredWidth and desiredHeight are the width and height of the element that's being adorned.
            // These will be used to place the ResizingAdorner at the corners of the adorned element.
            var boundingBox = CurrentEditor.GetCombinedBoundingBox(AdornedObjects);

            if (LastOperation != null)
            {
                boundingBox = LastOperation.TransformBounds(boundingBox);
            }

            double middle = (boundingBox.Left + boundingBox.Right) / 2;
            double centre = (boundingBox.Top + boundingBox.Bottom) / 2;

            TopHandle.Arrange(new Rect(middle - _halfThumbWidth, boundingBox.Top - _halfThumbWidth, _thumbWidth.Value,
                                       _thumbWidth.Value));
            TopHandle.MidPoint = new Point(middle, boundingBox.Top);

            BottomHandle.Arrange(new Rect(middle - _halfThumbWidth, boundingBox.Bottom - _halfThumbWidth, _thumbWidth.Value,
                                       _thumbWidth.Value));
            BottomHandle.MidPoint = new Point(middle, boundingBox.Bottom);

            RightHandle.Arrange(new Rect(boundingBox.Right - _halfThumbWidth, centre - _halfThumbWidth, _thumbWidth.Value,
                                       _thumbWidth.Value));
            RightHandle.MidPoint = new Point(boundingBox.Right, centre);

            LeftHandle.Arrange(new Rect(boundingBox.Left - _halfThumbWidth, centre - _halfThumbWidth, _thumbWidth.Value,
                                         _thumbWidth.Value));
            LeftHandle.MidPoint = new Point(boundingBox.Left, centre);

            TopLeftHandle.Arrange(new Rect(boundingBox.Left - _halfThumbWidth, boundingBox.Top - _halfThumbWidth, _thumbWidth.Value,
                                           _thumbWidth.Value));
            TopLeftHandle.MidPoint = boundingBox.TopLeft;

            TopRightHandle.Arrange(new Rect(boundingBox.Left + boundingBox.Width - _halfThumbWidth, boundingBox.Top - _halfThumbWidth,
                                            _thumbWidth.Value,
                                            _thumbWidth.Value));
            TopRightHandle.MidPoint = boundingBox.TopRight;

            BottomLeftHandle.Arrange(new Rect(boundingBox.Left - _halfThumbWidth, boundingBox.Top + boundingBox.Height - _halfThumbWidth,
                                              _thumbWidth.Value, _thumbWidth.Value));
            BottomLeftHandle.MidPoint = boundingBox.BottomLeft;

            BottomRightHandle.Arrange(new Rect(boundingBox.Left + boundingBox.Width - _halfThumbWidth,
                                               boundingBox.Height + boundingBox.Top - _halfThumbWidth, _thumbWidth.Value,
                                               _thumbWidth.Value));
            BottomRightHandle.MidPoint = boundingBox.BottomRight;

            if (IsDragging())
            {
                RotateHandle.Visibility = Visibility.Hidden;
            }
            else
            {
                RotateHandle.Visibility = Visibility.Visible;
                SetCentroid();
            }

            Vector rotateThumbTweak = new Vector(-RotateHandle.BoundingBox.Width / 2, -RotateHandle.BoundingBox.Height / 2);

            if (IsRotating && LastOperation != null)
            {
                _newThumbPos = LastOperation.Transform(_rotateThumbPos);
                RotateHandle.Arrange(new Rect(_newThumbPos + rotateThumbTweak, new Size(RotateHandle.Width, RotateHandle.Height)));

                RotateThumbPosition = _newThumbPos;
            }
            else
            {
                _xPlacement = (boundingBox.Left + boundingBox.Right) / 2;
                _yPlacement = boundingBox.Top - RotateHandle.Height * 3;

                _rotateThumbPos = new Point(_xPlacement, _yPlacement);

                RotateThumbPosition = _rotateThumbPos;
                RotateHandle.Arrange(new Rect(_rotateThumbPos + rotateThumbTweak, new Size(RotateHandle.Width, RotateHandle.Height)));
            }

            var visibility = (IsRotating && LastOperation != null) ? Visibility.Collapsed : Visibility.Visible;

            TopHandle.Visibility = visibility;
            BottomHandle.Visibility = visibility;
            RightHandle.Visibility = visibility;
            LeftHandle.Visibility = visibility;

            TopLeftHandle.Visibility = visibility;
            TopRightHandle.Visibility = visibility;
            BottomLeftHandle.Visibility = visibility;
            BottomRightHandle.Visibility = visibility;

            BigThumb.Visibility = visibility;

            base.ArrangeOverride(finalSize);
            return finalSize;
        }

        private bool IsDragging() =>
            BigThumb.IsDragging
            || TopLeftHandle.IsDragging
            || TopRightHandle.IsDragging
            || BottomLeftHandle.IsDragging
            || BottomRightHandle.IsDragging
            || TopHandle.IsDragging
            || LeftHandle.IsDragging
            || BottomHandle.IsDragging
            || RightHandle.IsDragging;

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            var ghostBrush = (Brush)FindResource(AcmeConstants.AdornerBorderBrush);
            var ghostPen = new Pen(ghostBrush, 1.0);

            if (!IsDragging())
            {
                drawingContext.DrawLine(ghostPen, _centroid, RotateThumbPosition);
                drawingContext.DrawEllipse(ghostBrush, ghostPen, _centroid, 2, 2);
            }

            if (IsWorking)
            {
                //identify which Molecule the atom belongs to
                //take a snapshot of the molecule
                var ghost = CurrentEditor.GhostMolecules(AdornedMolecules);
                ghost.Transform = LastOperation;
                drawingContext.DrawGeometry(ghostBrush, ghostPen, ghost);
                foreach (Reaction r in AdornedReactions)
                {
                    var newEndPoint = LastOperation.Transform(r.HeadPoint);
                    var newStartPoint = LastOperation.Transform(r.TailPoint);

                    //create temporary Reactions and visuals and throw them away afterwards
                    var tempReaction = new Reaction()
                    {
                        ConditionsText = r.ConditionsText,
                        HeadPoint = newEndPoint,
                        ReactionType = r.ReactionType,
                        ReagentText = r.ReagentText,
                        TailPoint = newStartPoint
                    };

                    var rv = new ReactionVisual(tempReaction)
                    {
                        TextSize = EditController.BlockTextSize,
                        ScriptSize = EditController.BlockTextSize * AcmeConstants.ScriptScalingFactor
                    };
                    rv.RenderFullGeometry(r.ReactionType, newStartPoint,
                                          newEndPoint,
                                          drawingContext, r.ReagentText, r.ConditionsText, ghostPen,
                                          ghostBrush);

                    var arrow = ReactionSelectionAdorner.GetArrowShape(newStartPoint,
                                                                       newEndPoint, r);
                    arrow.DrawArrowGeometry(drawingContext, ghostPen, ghostBrush);
                }
            }
        }

        #endregion Overrides

        #region Resizing

        // Handler for resizing from the bottom-right.
        private void IncrementDragging(DragDeltaEventArgs args)
        {
            var argsHorizontalChange = args.HorizontalChange;
            var argsVerticalChange = args.VerticalChange;

            if (double.IsNaN(argsHorizontalChange))
            {
                argsHorizontalChange = 0d;
            }

            if (double.IsNaN(argsVerticalChange))
            {
                argsVerticalChange = 0d;
            }

            DragXTravel += argsHorizontalChange;
            DragYTravel += argsVerticalChange;
        }

        private void HandleDragDelta(object sender, DragDeltaEventArgs args)
        {
            IncrementDragging(args);
            if (NotDraggingBackwards())
            {
                var currentPoint = new Point(_scaleOperationParams.Origin.X + DragXTravel, _scaleOperationParams.Origin.Y + DragYTravel);

                //we need to project the drag operation along a specific axis
                var projection = (currentPoint - _scaleOperationParams.Start).Project(_scaleOperationParams.OriginalSense);
                //and the scale according to movement along that axis

                SetScalingFactors(projection, out var xScale, out var yScale);

                LastOperation = new ScaleTransform(xScale, yScale, _scaleOperationParams.Start.X, _scaleOperationParams.Start.Y);
            }
            InvalidateVisual();
        }

        private bool NotDraggingBackwards() => BigThumb.Height >= MinTravelWidth && BigThumb.Width >= MinTravelWidth;

        #endregion Resizing

        #region Nested Classes

        private class ScaleOperationParams
        {
            public Point Start { get; set; }
            public Point Finish { get; set; }

            public Vector OriginalSense => Finish - Start;

            public Point Origin { get; set; }
        }

        #endregion Nested Classes
    }
}
