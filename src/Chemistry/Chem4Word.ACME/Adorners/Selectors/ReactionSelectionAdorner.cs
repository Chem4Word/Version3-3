// ---------------------------------------------------------------------------
//  Copyright (c) 2026, The .NET Foundation.
//  This software is released under the Apache Licence, Version 2.0.
//  The licence and further copyright text can be found in the file LICENCE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.ACME.Controls;
using Chem4Word.ACME.Drawing.Visuals;
using Chem4Word.ACME.Graphics;
using Chem4Word.ACME.Utils;
using Chem4Word.Model2;
using Chem4Word.Model2.Enums;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace Chem4Word.ACME.Adorners.Selectors
{
    public class ReactionSelectionAdorner : BaseSelectionAdorner
    {
        private static double _halfThumbWidth;

        private static Snapper _resizeSnapper;
        private readonly DrawingVisual _headHandle = new DrawingVisual();
        private readonly DrawingVisual _tailHandle = new DrawingVisual();
        private DrawingVisual _draggedVisual;
        private bool _resizing;
        private bool _dragging;
        private readonly SolidColorBrush _solidColorBrush;
        private readonly Pen _dashPen;

        private Point OriginalLocation { get; set; }
        private Point CurrentLocation { get; set; }

        public ReactionSelectionAdorner(EditorCanvas currentEditor, ReactionVisual reactionVisual) : base(currentEditor)
        {
            _solidColorBrush = (SolidColorBrush)FindResource(AcmeConstants.DrawAdornerBrush);
            _dashPen = new Pen(_solidColorBrush, 1);

            _halfThumbWidth = AcmeConstants.ThumbWidth / 2;
            AdornedReactionVisual = reactionVisual;
            AdornedReaction = reactionVisual.ParentReaction;

            AttachHandlers();
            EditController.SendStatus((AcmeConstants.DefaultStatusText, "", ""));
        }

        private ReactionVisual AdornedReactionVisual { get; }

        public Reaction AdornedReaction { get; }

        private Vector HeadDisplacement { get; set; }
        private Vector TailDisplacement { get; set; }

        private void DisableHandlers()
        {
            //detach the handlers to stop them interfering with dragging
            MouseLeftButtonDown -= OnMouseLeftButtonDown_BaseSelectionAdorner;
            MouseLeftButtonUp -= OnMouseLeftButtonUp_BaseSelectionAdorner;
            PreviewMouseLeftButtonDown -= OnPreviewMouseLeftButtonDown_BaseSelectionAdorner;
            PreviewMouseLeftButtonUp -= OnPreviewMouseLeftButtonUp_BaseSelectionAdorner;
        }

        protected new void AttachHandlers()
        {
            //detach the handlers to stop them interfering with dragging
            DisableHandlers();

            MouseLeftButtonDown += OnMouseLeftButtonDown_ReactionSelectionAdorner;
            MouseLeftButtonUp += OnMouseLeftButtonUp_ReactionSelectionAdorner;
            PreviewMouseMove += OnMouseMove_ReactionSelectionAdorner;
            PreviewMouseLeftButtonDown += OnPreviewMouseLeftButtonDown_ReactionSelectionAdorner;
        }

        private void OnPreviewMouseLeftButtonDown_ReactionSelectionAdorner(object sender, MouseButtonEventArgs e)
        {
            OriginalLocation = e.GetPosition(CurrentEditor);
            CurrentLocation = OriginalLocation;

            if (e.ClickCount == 2)
            {
                if (AdornedReactionVisual.ConditionsBlockRect.Contains(CurrentLocation))
                {
                    e.Handled = true;
                }
                else if (AdornedReactionVisual.ReagentsBlockRect.Contains(CurrentLocation))
                {
                    e.Handled = true;
                }
            }
        }

        private void OnMouseLeftButtonDown_ReactionSelectionAdorner(object sender, MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);

            OriginalLocation = e.GetPosition(CurrentEditor);
            CurrentLocation = OriginalLocation;
            Mouse.Capture((ReactionSelectionAdorner)sender);

            DrawingVisual dv = null;

            //get rid of the original handles
            RemoveHandle(_headHandle);
            RemoveHandle(_tailHandle);

            if ((CurrentLocation - AdornedReaction.HeadPoint).LengthSquared <= _halfThumbWidth * _halfThumbWidth)
            {
                dv = _headHandle;
            }
            else if ((OriginalLocation - AdornedReaction.TailPoint).LengthSquared <= _halfThumbWidth * _halfThumbWidth)
            {
                dv = _tailHandle;
            }

            if (dv == _headHandle)
            {
                TailDisplacement = new Vector(0d, 0d);
                _draggedVisual = _headHandle;
                _resizing = true;
                _dragging = false;
                _resizeSnapper = new Snapper(AdornedReaction.HeadPoint, EditController, 15);
            }
            else if (dv == _tailHandle)
            {
                HeadDisplacement = new Vector(0d, 0d);
                _draggedVisual = _tailHandle;
                _resizing = true;
                _dragging = false;
                _resizeSnapper = new Snapper(AdornedReaction.TailPoint, EditController, 15);
            }
            else
            {
                //don't drag if we're on the text blocks
                _dragging = !(AdornedReactionVisual.ReagentsBlockRect.Contains(OriginalLocation) | AdornedReactionVisual.ConditionsBlockRect.Contains(OriginalLocation));
                _resizing = false;
            }
            e.Handled = true;
        }

        private void OnMouseMove_ReactionSelectionAdorner(object sender, MouseEventArgs e)
        {
            CurrentLocation = e.GetPosition(CurrentEditor);
            if (_resizing || _dragging)
            {
                Keyboard.Focus(this);

                if (_resizing)
                {
                    Mouse.Capture((ReactionSelectionAdorner)sender);
                    if (_draggedVisual == _headHandle)
                    {
                        CurrentLocation = AdornedReaction.TailPoint + _resizeSnapper.SnapVector(AdornedReaction.Angle, CurrentLocation - AdornedReaction.TailPoint);
                        HeadDisplacement = CurrentLocation - AdornedReaction.HeadPoint;
                    }
                    else if (_draggedVisual == _tailHandle)
                    {
                        CurrentLocation = AdornedReaction.HeadPoint + _resizeSnapper.SnapVector(AdornedReaction.Angle, CurrentLocation - AdornedReaction.HeadPoint);
                        TailDisplacement = CurrentLocation - AdornedReaction.TailPoint;
                    }
                    EditController.SendStatus((AcmeConstants.UnlockStatusText, "", ""));
                }
                else if (_dragging)
                {
                    HeadDisplacement = CurrentLocation - OriginalLocation;
                    TailDisplacement = HeadDisplacement;
                }

                InvalidateVisual();
            }
            else
            {
                if (AdornedReactionVisual.ReagentsBlockRect.Contains(CurrentLocation))
                {
                    EditController.SendStatus((AcmeConstants.EditReagentsStatusText, "", ""));
                }
                else if (AdornedReactionVisual.ConditionsBlockRect.Contains(CurrentLocation))
                {
                    EditController.SendStatus((AcmeConstants.EditConditionsStatusText, "", ""));
                }
            }
            e.Handled = true;
        }

        private void OnMouseLeftButtonUp_ReactionSelectionAdorner(object sender, MouseButtonEventArgs e)
        {
            if (_resizing || _dragging)
            {
                EditController.MoveReaction(AdornedReaction, AdornedReaction.TailPoint + TailDisplacement, AdornedReaction.HeadPoint + HeadDisplacement);
            }

            _resizing = false;
            _dragging = false;

            ReleaseMouseCapture();
            InvalidateVisual();
            e.Handled = true;
            EditController.SendStatus((AcmeConstants.DefaultStatusText, "", ""));
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            Brush handleFillBrush = (Brush)FindResource(AcmeConstants.AdornerFillBrush);
            Pen handleBorderPen = (Pen)FindResource(AcmeConstants.AdornerBorderPen);

            RemoveHandle(_headHandle);
            RemoveHandle(_tailHandle);

            Point newTailPoint = AdornedReaction.TailPoint;
            Point newHeadPoint = AdornedReaction.HeadPoint;

            base.OnRender(drawingContext);
            if (_resizing || _dragging)
            {
                newTailPoint = AdornedReaction.TailPoint + TailDisplacement;
                newHeadPoint = AdornedReaction.HeadPoint + HeadDisplacement;
                Debug.WriteLine($"New Tail Point = {newTailPoint}, New Head Point = {newHeadPoint}");
                var tempReaction = new Reaction()
                {
                    ConditionsText = AdornedReaction.ConditionsText,
                    HeadPoint = newHeadPoint,
                    ReactionType = AdornedReaction.ReactionType,
                    ReagentText = AdornedReaction.ReagentText,
                    TailPoint = newTailPoint
                };
                var rv = new ReactionVisual(tempReaction);
                rv.TextSize = EditController.BlockTextSize;
                rv.ScriptSize = EditController.BlockTextSize * AcmeConstants.ScriptScalingFactor;
                rv.RenderFullGeometry(AdornedReaction.ReactionType, AdornedReaction.TailPoint,
                                      AdornedReaction.HeadPoint,
                                      drawingContext, AdornedReaction.ReagentText, AdornedReaction.ConditionsText,
                                      _dashPen,
                                      _solidColorBrush);
            }
            else
            {
                BuildHandle(drawingContext, _headHandle, newHeadPoint, handleFillBrush, handleBorderPen);
                BuildHandle(drawingContext, _tailHandle, newTailPoint, handleFillBrush, handleBorderPen);

                foreach (var reactant in AdornedReaction.Reactants.Values)
                {
                    BuildRoleIndicator(drawingContext, reactant, false);
                }
                foreach (var product in AdornedReaction.Products.Values)
                {
                    BuildRoleIndicator(drawingContext, product, true);
                }
            }

            Arrow arrowVisual = GetArrowShape(newTailPoint, newHeadPoint, AdornedReaction);
            arrowVisual.DrawArrowGeometry(drawingContext, _dashPen, _solidColorBrush);
            arrowVisual.GetOverlayPen(out Brush overlayBrush, out Pen overlayPen);
            arrowVisual.DrawArrowGeometry(drawingContext, overlayPen, overlayBrush);

            if (AdornedReactionVisual.ReagentsBlockRect != Rect.Empty)
            {
                DrawReagentsBlockOutline(drawingContext);
            }
            if (AdornedReactionVisual.ConditionsBlockRect != Rect.Empty)
            {
                DrawConditionsBlockOutline(drawingContext);
            }
        }

        private void BuildRoleIndicator(DrawingContext drawingContext, Molecule mol, bool isProduct)
        {
            Brush productBrush = (Brush)FindResource(AcmeConstants.ResourceKeyProductIndicatorBrush);
            Brush reactantBrush = (Brush)FindResource(AcmeConstants.ResourceKeyReactantIndicatorBrush);
            Brush arrowBrush = (Brush)FindResource(AcmeConstants.ResourceKeyArrowIndicatorBrush);
            Brush background = (Brush)FindResource(AcmeConstants.ResourceKeyIndicatorBackgroundBrush);
            //set up metrics
            var bondLength = CurrentEditor.Controller.Model.XamlBondLength;
            var linelength = bondLength;
            var lineThickness = linelength / 10;
            Vector goLeft = new Vector(-linelength, 0);
            Vector goRight = -goLeft;

            //now draw the role circles
            Brush reactantFill = null;
            Pen reactantPen = null;
            Brush productFill = null;
            Pen productPen = null;
            if (isProduct)
            {
                productFill = productBrush;
                reactantPen = new Pen(reactantBrush, lineThickness);
            }
            else
            {
                reactantFill = reactantBrush;
                productPen = new Pen(productBrush, lineThickness);
            }

            double radius = linelength / 4;

            Point reactantCenter = mol.Centre + goLeft;
            Point productCenter = mol.Centre + goRight;

            //draw the background
            Pen backgroundPen = new Pen(background, radius * 2.5) { EndLineCap = PenLineCap.Round, StartLineCap = PenLineCap.Round };
            drawingContext.DrawLine(backgroundPen, reactantCenter, productCenter);

            //draw the indicators
            drawingContext.DrawEllipse(reactantFill, reactantPen, reactantCenter, radius, radius);
            drawingContext.DrawEllipse(productFill, productPen, productCenter, radius, radius);

            //now draw the arrow
            Vector arrowVector = productCenter - reactantCenter;
            arrowVector.Normalize();
            arrowVector *= radius * 1.5;
            Point arrowStart = reactantCenter + arrowVector;
            Point arrowEnd = productCenter - arrowVector;
            Arrow arrowVisual = new StraightArrow { StartPoint = arrowStart, EndPoint = arrowEnd, HeadLength = radius * 1.6, ArrowHeadClosed = false };
            Pen outlinePen = new Pen(arrowBrush, lineThickness);
            arrowVisual.DrawArrowGeometry(drawingContext, outlinePen, null);
        }

        /// <summary>
        /// Draws a dashed highlight around the conditions block
        /// </summary>
        /// <param name="drawingContext"></param>
        private void DrawConditionsBlockOutline(DrawingContext drawingContext)
        {
            DrawBlockRect(drawingContext, AdornedReactionVisual.ConditionsBlockRect);
        }

        /// <summary>
        /// Draws a dashed highlight around a rectangle
        /// </summary>
        /// <param name="drawingContext">Passed in from the calling OnRender method</param>
        /// <param name="blockBounds">Rectangle describing the layout of the block</param>
        private void DrawBlockRect(DrawingContext drawingContext, Rect blockBounds)
        {
            Pen handleBorderPen = new Pen { Brush = (Brush)FindResource(AcmeConstants.AdornerBorderBrush), DashStyle = DashStyles.Dash };
            drawingContext.DrawRectangle((Brush)FindResource(AcmeConstants.AdornerFillBrush), handleBorderPen, blockBounds);
        }

        /// <summary>
        /// Draws a dashed highlight around the reagents block
        /// </summary>
        /// <param name="drawingContext"></param>
        private void DrawReagentsBlockOutline(DrawingContext drawingContext)
        {
            DrawBlockRect(drawingContext, AdornedReactionVisual.ReagentsBlockRect);
        }

        /// <summary>
        /// builds a grab handle for the tail or head of the arrow
        /// </summary>
        private void BuildHandle(DrawingContext drawingContext, DrawingVisual handle, Point centre, Brush handleFillBrush, Pen handleBorderPen)
        {
            drawingContext.DrawEllipse(handleFillBrush, handleBorderPen, centre, _halfThumbWidth, _halfThumbWidth);
            AddVisualChild(handle);
            AddLogicalChild(handle);
        }

        private void RemoveHandle(DrawingVisual handle)
        {
            if (VisualChildren.Contains(handle))
            {
                RemoveVisualChild(handle);
                RemoveLogicalChild(handle);
            }
        }

        public static Arrow GetArrowShape(Point newStartPoint, Point newEndPoint, Reaction adornedReaction)
        {
            Arrow arrowVisual;
            switch (adornedReaction.ReactionType)
            {
                case ReactionType.Reversible:
                    arrowVisual = new EquilibriumArrow { StartPoint = newStartPoint, EndPoint = newEndPoint };
                    break;

                case ReactionType.ReversibleBiasedForward:
                    arrowVisual = new EquilibriumArrow { StartPoint = newStartPoint, EndPoint = newEndPoint, Bias = EquilibriumBias.Forward };
                    break;

                case ReactionType.ReversibleBiasedReverse:
                    arrowVisual = new EquilibriumArrow { StartPoint = newStartPoint, EndPoint = newEndPoint, Bias = EquilibriumBias.Backward };
                    break;

                case ReactionType.Blocked:
                    arrowVisual = new BlockedArrow { StartPoint = newStartPoint, EndPoint = newEndPoint };
                    break;

                case ReactionType.Resonance:
                    arrowVisual = new StraightArrow { StartPoint = newStartPoint, EndPoint = newEndPoint, ArrowEnds = Enums.ArrowEnds.Both };
                    break;

                case ReactionType.Retrosynthetic:
                    arrowVisual = new RetrosyntheticArrow { StartPoint = newStartPoint, EndPoint = newEndPoint };
                    break;

                case ReactionType.Theoretical:
                    arrowVisual = new DashedArrow { StartPoint = newStartPoint, EndPoint = newEndPoint };
                    break;

                default:
                    arrowVisual = new StraightArrow { StartPoint = newStartPoint, EndPoint = newEndPoint };
                    break;
            }

            return arrowVisual;
        }
    }
}