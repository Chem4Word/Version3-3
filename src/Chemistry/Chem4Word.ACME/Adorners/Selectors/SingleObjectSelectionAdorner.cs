// ---------------------------------------------------------------------------
//  Copyright (c) 2026, The .NET Foundation.
//  This software is released under the Apache Licence, Version 2.0.
//  The licence and further copyright text can be found in the file LICENCE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.ACME.Controls;
using Chem4Word.ACME.Drawing.Visuals;
using Chem4Word.Model2;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using static Chem4Word.ACME.Utils.GraphicsHelpers;
using Geometry = System.Windows.Media.Geometry;

namespace Chem4Word.ACME.Adorners.Selectors
{
    public class SingleObjectSelectionAdorner : MultiObjectAdorner
    {
        //this is the main grab area for the molecule
        protected Thumb BigThumb;

        public List<Molecule> AdornedMolecules => AdornedObjects.OfType<Molecule>().ToList();
        public List<Reaction> AdornedReactions => AdornedObjects.OfType<Reaction>().ToList();

        public List<Annotation> AdornedAnnotations => AdornedObjects.OfType<Annotation>().ToList();

        //tracks the last operation performed
        protected Transform LastOperation;

        //status flag
        protected bool Dragging;

        //tracks the amount of travel during drag operations
        protected double DragXTravel;

        protected double DragYTravel;

        //where the dragging starts
        protected Point StartPos;

        protected bool IsWorking => Dragging;

        public Rect BoundingBox => _bigBoundingBox;
        private Rect _bigBoundingBox;

        private Geometry _ghostMolecule;

        public SingleObjectSelectionAdorner(EditorCanvas currentEditor, Molecule molecule)
            : this(currentEditor, new List<StructuralObject> { molecule })
        {
        }

        public SingleObjectSelectionAdorner(EditorCanvas currentEditor, List<Molecule> mols)
            : this(currentEditor, mols.ConvertAll(m => (StructuralObject)m))
        {
        }

        public SingleObjectSelectionAdorner(EditorCanvas currentEditor, List<StructuralObject> molecules) : base(currentEditor, molecules)
        {
            BuildBigDragArea();

            DisableHandlers();
            Focusable = false;
            IsHitTestVisible = true;

            Focusable = true;

            //TODO: [DCD] Investigate crash
            //Keyboard.Focus(this);
        }

        protected void DisableHandlers()
        {
            //detach the handlers to stop them interfering with dragging
            PreviewMouseLeftButtonDown -= OnPreviewMouseLeftButtonDown_BaseSelectionAdorner;
            MouseLeftButtonDown -= OnMouseLeftButtonDown_BaseSelectionAdorner;
            PreviewMouseMove -= OnPreviewMouseMove_BaseSelectionAdorner;
            PreviewMouseLeftButtonUp -= OnPreviewMouseLeftButtonUp_BaseSelectionAdorner;
            MouseLeftButtonUp -= OnMouseLeftButtonUp_BaseSelectionAdorner;
        }

        private void OnMouseLeftButtonDown_BigThumb(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                RaiseEvent(e);
            }
        }

        /// <summary>
        /// Creates the big thumb that allows dragging a molecule around the canvas
        /// </summary>
        private void BuildBigDragArea()
        {
            BigThumb = new Thumb();
            VisualChildren.Add(BigThumb);
            BigThumb.IsHitTestVisible = true;

            BigThumb.Style = (Style)FindResource(AcmeConstants.ThumbStyle);
            BigThumb.Cursor = Cursors.Hand;
            BigThumb.DragStarted += OnDragStarted_BigThumb;
            BigThumb.DragCompleted += OnDragCompleted_BigThumb;
            BigThumb.DragDelta += OnDragDelta_BigThumb;
            BigThumb.MouseLeftButtonDown += OnMouseLeftButtonDown_BigThumb;

            BigThumb.Focusable = true;

            //TODO: [DCD] Investigate crash
            //Keyboard.Focus(BigThumb);
        }

        /// <summary>
        /// Override this to change the appearance of the main area
        /// </summary>
        /// <param name="drawingContext"></param>
        protected override void OnRender(DrawingContext drawingContext)
        {
            var ghostPen = (Pen)FindResource(AcmeConstants.AdornerBorderPen);
            var ghostBrush = (Brush)FindResource(AcmeConstants.AdornerFillBrush);
            if (IsWorking)
            {
                //take a snapshot of the molecule
                if (_ghostMolecule == null)
                {
                    _ghostMolecule = CurrentEditor.GhostMolecules(AdornedMolecules);
                }

                _ghostMolecule.Transform = LastOperation;

                drawingContext.DrawGeometry(ghostBrush, ghostPen, _ghostMolecule);

                foreach (Reaction r in AdornedReactions)
                {
                    var arrow = ReactionSelectionAdorner.GetArrowShape(LastOperation.Transform(r.TailPoint), LastOperation.Transform(r.HeadPoint), r);
                    arrow.DrawArrowGeometry(drawingContext, ghostPen, ghostBrush);
                }
                foreach (Annotation an in AdornedAnnotations)
                {
                    var annotationVisual = CurrentEditor.ChemicalVisuals[an] as AnnotationVisual;
                    if (annotationVisual != null)
                    {
                        var textDrawing = annotationVisual.Drawing;
                        textDrawing.Transform = LastOperation;
                        IterateDrawingGroup(textDrawing, drawingContext, ghostPen, ghostBrush, LastOperation);
                    }
                }
                base.OnRender(drawingContext);
            }
        }

        // Arrange the Adorners.
        protected override Size ArrangeOverride(Size finalSize)
        {
            // desiredWidth and desiredHeight are the width and height of the element that's being adorned.
            // These will be used to place the ResizingAdorner at the corners of the adorned element.
            _bigBoundingBox = CurrentEditor.GetCombinedBoundingBox(AdornedObjects);

            if (LastOperation != null)
            {
                _bigBoundingBox = LastOperation.TransformBounds(BoundingBox);
            }

            //put a box right around the entire shebang
            BigThumb.Arrange(BoundingBox);
            Canvas.SetLeft(BigThumb, BoundingBox.Left);
            Canvas.SetTop(BigThumb, BoundingBox.Top);
            BigThumb.Height = BoundingBox.Height;
            BigThumb.Width = BoundingBox.Width;

            // Return the final size.
            return finalSize;
        }

        #region Events

        public event DragCompletedEventHandler DragIsCompleted;

        #endregion Events

        #region MouseIsDown

        private void OnDragStarted_BigThumb(object sender, DragStartedEventArgs e)
        {
            Dragging = true;

            DragXTravel = 0.0d;
            DragYTravel = 0.0d;
            Keyboard.Focus(this);
            StartPos = new Point(Canvas.GetLeft(BigThumb), Canvas.GetTop(BigThumb));
            LastOperation = new TranslateTransform();
        }

        private void OnDragDelta_BigThumb(object sender, DragDeltaEventArgs e)
        {
            //update how far it's travelled so far
            double horizontalChange = e.HorizontalChange;
            double verticalChange = e.VerticalChange;

            DragXTravel += horizontalChange;
            DragYTravel += verticalChange;

            double vOffset = DragYTravel;
            double hOffset = DragXTravel;

            var lastTranslation = (TranslateTransform)LastOperation;

            lastTranslation.X = hOffset;
            lastTranslation.Y = vOffset;

            InvalidateVisual();
        }

        /// <summary>
        /// Handles all drag events from all thumbs.
        /// The actual transformation is set in other code
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnDragCompleted_BigThumb(object sender, DragCompletedEventArgs e)
        {
            if (!e.Canceled)
            {
                var lastTranslation = (TranslateTransform)LastOperation;
                lastTranslation.X = DragXTravel;
                lastTranslation.Y = DragYTravel;

                InvalidateVisual();

                //move the molecule
                EditController.TransformObjects(LastOperation, AdornedObjects);

                RaiseDragCompleted(sender, e);

                CurrentEditor.SuppressRedraw = false;

                foreach (Molecule adornedMolecule in AdornedMolecules)
                {
                    adornedMolecule.UpdateVisual();
                }
            }
            else
            {
                EditController.RemoveFromSelection(AdornedObjects);
            }
            Dragging = false;
        }

        #endregion MouseIsDown

        protected void RaiseDragCompleted(object sender, DragCompletedEventArgs dragCompletedEventArgs)
        {
            DragIsCompleted?.Invoke(this, dragCompletedEventArgs);
        }
    }
}
