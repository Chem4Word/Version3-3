// ---------------------------------------------------------------------------
//  Copyright (c) 2026, The .NET Foundation.
//  This software is released under the Apache Licence, Version 2.0.
//  The licence and further copyright text can be found in the file LICENCE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------
using Chem4Word.ACME.Adorners.Sketching;
using Chem4Word.ACME.Controls;
using Chem4Word.ACME.Utils;
using Chem4Word.Model2;
using Chem4Word.Model2.Enums;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;

namespace Chem4Word.ACME.Behaviors
{
    public class ElectronPusherBehaviour : BaseEditBehavior
    {
        public ElectronPusherType ElectronPusherType { get; set; }
        private Window _parent;

        private Cursor _lastCursor;

        private ElectronPusherDrawAdorner _adorner;

        public ElectronPusherDrawAdorner DrawAdorner
        {
            get => _adorner;
            set => _adorner = value;
        }

        protected override void OnAttached()
        {
            base.OnAttached();
            CurrentEditor = (EditorCanvas)AssociatedObject;
            _parent = Application.Current.MainWindow;
            _lastCursor = CurrentEditor.Cursor;
            CurrentEditor.Cursor = CursorUtils.Pencil;
            AttachHandlers();
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            CurrentEditor.Cursor = _lastCursor;
            DetachHandlers();
            ClearTemporaries();
        }

        private void DetachHandlers()
        {
            CurrentEditor.MouseLeftButtonDown -= OnMouseLeftButtonDown_CurrentEditor;
            CurrentEditor.MouseMove -= OnMouseMove_CurrentEditor;
            CurrentEditor.MouseLeftButtonUp -= OnMouseLeftButtonUp_CurrentEditor;
            CurrentEditor.KeyDown -= OnKeyDown_CurrentEditor;
            if (_parent != null)
            {
                _parent.MouseLeftButtonDown -= OnMouseLeftButtonDown_CurrentEditor;
                _parent.MouseLeftButtonUp -= OnMouseLeftButtonUp_CurrentEditor;
            }
        }

        private void AttachHandlers()
        {
            CurrentEditor.MouseLeftButtonDown += OnMouseLeftButtonDown_CurrentEditor;
            CurrentEditor.MouseMove += OnMouseMove_CurrentEditor;
            CurrentEditor.MouseLeftButtonUp += OnMouseLeftButtonUp_CurrentEditor;
            CurrentEditor.KeyDown += OnKeyDown_CurrentEditor;
            CurrentEditor.IsHitTestVisible = true;
            CurrentEditor.Focusable = true;
            if (_parent != null)
            {
                _parent.MouseLeftButtonDown += OnMouseLeftButtonDown_CurrentEditor;
                _parent.MouseLeftButtonUp += OnMouseLeftButtonUp_CurrentEditor;
            }
        }

        private void OnKeyDown_CurrentEditor(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                Abort();
            }
        }

        private void OnMouseLeftButtonUp_CurrentEditor(object sender, MouseButtonEventArgs e)
        {
            var targetChemistry = CurrentEditor.ActiveChemistry;
            if (!(targetChemistry is null) && !(StartChemistry is null) && targetChemistry != StartChemistry)
            {
                var currentPos = e.GetPosition(CurrentEditor);
                EditController.AddElectronPusher(StartChemistry, targetChemistry, ElectronPusherType, DrawAdorner.FirstControlPoint, DrawAdorner.SecondControlPoint);
            }
            ClearTemporaries();
            CurrentEditor.ReleaseMouseCapture();
            MouseIsDown = false;
        }

        private void OnMouseMove_CurrentEditor(object sender, MouseEventArgs e)
        {
            var currentPos = e.GetPosition(CurrentEditor);

            if (_adorner != null)
            {
                RemoveEPAdorner();
            }
            if (Dragging(e))
            {
                DrawAdorner = new ElectronPusherDrawAdorner(StartChemistry, CurrentEditor, currentPos, ElectronPusherType, EditController.EditBondThickness);
            }

            e.Handled = true;
        }

        private void OnMouseLeftButtonDown_CurrentEditor(object sender, MouseButtonEventArgs e)
        {
            LastPos = e.GetPosition(CurrentEditor);
            CurrentEditor.CaptureMouse();
            StartChemistry = CurrentEditor.ActiveChemistry;
            if (!(StartChemistry is null) && (StartChemistry is Bond || StartChemistry is Atom))
            {
                IsDrawing = true;
            }
            else
            {
                StartChemistry = null;
            }

            Mouse.Capture(CurrentEditor);
            Keyboard.Focus(CurrentEditor);
            MouseIsDown = true;
        }

        public bool MouseIsDown { get; set; }

        public StructuralObject StartChemistry { get; set; }

        public Point LastPos { get; set; }

        public override void Abort()
        {
            CurrentEditor.ReleaseMouseCapture();
            CurrentStatus = ("", "", "");
            ClearTemporaries();
        }

        private void ClearTemporaries()
        {
            if (DrawAdorner != null)
            {
                RemoveEPAdorner();
            }

            IsDrawing = false;
            //clear this to prevent a weird bug in drawing
            CurrentEditor.ActiveChemistry = null;
            CurrentEditor.Focus();
        }

        public bool IsDrawing { get; set; }

        private bool Dragging(MouseEventArgs e)
        {
            return e.LeftButton == MouseButtonState.Pressed && IsDrawing;
        }

        private void RemoveEPAdorner()
        {
            var layer = AdornerLayer.GetAdornerLayer(CurrentEditor);

            layer.Remove(_adorner);
            _adorner = null;
        }
    }
}
