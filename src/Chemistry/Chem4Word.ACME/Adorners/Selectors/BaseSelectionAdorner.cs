﻿// ---------------------------------------------------------------------------
//  Copyright (c) 2025, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.ACME.Controls;
using Chem4Word.Model2.Annotations;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace Chem4Word.ACME.Adorners.Selectors
{
    /// <summary>
    /// Selection adorners are generally 'immutable':  they are drawn once and subsequently destroyed
    /// rather than being visually modified.  This makes coding much easier.
    /// When changing the visual appearance of a selection, create a new instance of the adorner
    /// supplying the element to the constructor along with the editor canvas
    /// </summary>
    public abstract class BaseSelectionAdorner : Adorner
    {
        protected double AdornerOpacity;

        #region Shared Properties

        //the editor that the Adorner attaches to
        public EditorCanvas CurrentEditor => (EditorCanvas)AdornedElement;

        public EditController EditController => (CurrentEditor.Controller as EditController);
        public SolidColorBrush RenderBrush { get; protected set; }

        [NotNull]
        public AdornerLayer AdornerLayer { get; private set; }

        public VisualCollection VisualChildren { get; set; }

        protected override int VisualChildrenCount => VisualChildren.Count;

        public virtual DragCompletedEventHandler DragCompleted { get; set; }

        #endregion Shared Properties

        #region Constructors

        protected BaseSelectionAdorner(EditorCanvas currentEditor) : base(currentEditor)
        {
            VisualChildren = new VisualCollection(this);
            DefaultSettings();
            AttachHandlers();
            BondToLayer();
            AdornerOpacity = 0.25;
        }

        protected override Visual GetVisualChild(int index)
        {
            return VisualChildren[index];
        }

        /// <summary>
        /// Bonds the adorner to the current editor
        /// </summary>
        private void BondToLayer()
        {
            AdornerLayer = AdornerLayer.GetAdornerLayer(CurrentEditor);
            AdornerLayer.Add(this);
        }

        ~BaseSelectionAdorner()
        {
            DetachHandlers();
        }

        #endregion Constructors

        #region Methods

        /// <summary>
        /// Attaches the default handlers to the adorner.
        /// These simply relay events to the CurrentEditor
        /// The CurrentEditor then forwards them to the attached Behavior
        /// </summary>
        protected virtual void AttachHandlers()
        {
            MouseLeftButtonDown += OnMouseLeftButtonDown_BaseSelectionAdorner;
            PreviewMouseLeftButtonDown += OnPreviewMouseLeftButtonDown_BaseSelectionAdorner;
            MouseLeftButtonDown += OnMouseLeftButtonDown_BaseSelectionAdorner;
            PreviewMouseLeftButtonDown += OnPreviewMouseLeftButtonDown_BaseSelectionAdorner;
            MouseMove += OnMouseMove_BaseSelectionAdorner;
            PreviewMouseMove += OnPreviewMouseMove_BaseSelectionAdorner;
            PreviewMouseLeftButtonUp += OnPreviewMouseLeftButtonUp_BaseSelectionAdorner;
            PreviewMouseRightButtonUp += OnPreviewMouseRightButtonUp_BaseSelectionAdorner;
            MouseLeftButtonUp += OnMouseLeftButtonUp_BaseSelectionAdorner;
            PreviewKeyDown += OnPreviewKeyDown_BaseSelectionAdorner;
            KeyDown += OnKeyDown_BaseSelectionAdorner;
        }

        protected virtual void OnKeyDown_BaseSelectionAdorner(object sender, KeyEventArgs e)
        {
            CurrentEditor.RaiseEvent(e);
        }

        protected void DetachHandlers()
        {
            MouseLeftButtonDown -= OnMouseLeftButtonDown_BaseSelectionAdorner;
            PreviewMouseLeftButtonDown -= OnPreviewMouseLeftButtonDown_BaseSelectionAdorner;
            MouseLeftButtonDown -= OnMouseLeftButtonDown_BaseSelectionAdorner;
            PreviewMouseLeftButtonDown -= OnPreviewMouseLeftButtonDown_BaseSelectionAdorner;
            MouseMove -= OnMouseMove_BaseSelectionAdorner;
            PreviewMouseMove -= OnPreviewMouseMove_BaseSelectionAdorner;
            PreviewMouseLeftButtonUp -= OnPreviewMouseLeftButtonUp_BaseSelectionAdorner;
            PreviewMouseRightButtonUp -= OnPreviewMouseRightButtonUp_BaseSelectionAdorner;
            MouseLeftButtonUp -= OnMouseLeftButtonUp_BaseSelectionAdorner;
            PreviewKeyDown -= OnPreviewKeyDown_BaseSelectionAdorner;
            KeyDown -= OnKeyDown_BaseSelectionAdorner;
        }

        protected void DefaultSettings()
        {
            IsHitTestVisible = true;
            RenderBrush = new SolidColorBrush(SystemColors.HighlightColor);
            RenderBrush.Opacity = AdornerOpacity;
        }

        #endregion Methods

        #region Event Handlers

        //override these methods in derived classes to handle specific events
        //The forwarding chain for events is adorner -> CurrentEditor -> attached behaviour
        protected virtual void OnPreviewKeyDown_BaseSelectionAdorner(object sender, KeyEventArgs e)
        {
            CurrentEditor.RaiseEvent(e);
        }

        protected virtual void OnMouseLeftButtonUp_BaseSelectionAdorner(object sender, MouseButtonEventArgs e)
        {
            CurrentEditor.RaiseEvent(e);
        }

        protected virtual void OnPreviewMouseRightButtonUp_BaseSelectionAdorner(object sender, MouseButtonEventArgs e)
        {
            CurrentEditor.RaiseEvent(e);
        }

        protected virtual void OnPreviewMouseLeftButtonUp_BaseSelectionAdorner(object sender, MouseButtonEventArgs e)
        {
            CurrentEditor.RaiseEvent(e);
        }

        protected virtual void OnPreviewMouseMove_BaseSelectionAdorner(object sender, MouseEventArgs e)
        {
            CurrentEditor.RaiseEvent(e);
        }

        protected virtual void OnMouseMove_BaseSelectionAdorner(object sender, MouseEventArgs e)
        {
            CurrentEditor.RaiseEvent(e);
        }

        protected virtual void OnPreviewMouseLeftButtonDown_BaseSelectionAdorner(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            CurrentEditor.RaiseEvent(e);
        }

        protected virtual void OnMouseLeftButtonDown_BaseSelectionAdorner(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            CurrentEditor.RaiseEvent(e);
        }

        #endregion Event Handlers
    }
}