﻿// ---------------------------------------------------------------------------
//  Copyright (c) 2025, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.ACME.Utils;
using Chem4Word.Model2;
using System.Linq;
using System.Windows;

namespace Chem4Word.ACME.Commands.Editing
{
    public class PickElementCommand : BaseCommand
    {
        private readonly EditController _controller;

        public PickElementCommand(EditController controller) : base(controller)
        {
            _controller = controller;
        }

        public override bool CanExecute(object parameter) => true;

        public override void Execute(object parameter)
        {
            PTPopup popupPicker = new PTPopup();
            var mosusePosition = System.Windows.Forms.Control.MousePosition;
            popupPicker.CentrePoint = new Point(mosusePosition.X, mosusePosition.Y);
            UIUtils.ShowDialog(popupPicker, _controller.CurrentEditor);
            var popupPickerSelectedElement = popupPicker.SelectedElement;
            if (popupPickerSelectedElement != null)
            {
                if (!_controller.AtomOptions.Any(ao => ao.Element == popupPickerSelectedElement))
                {
                    var newOption = new AtomOption(popupPickerSelectedElement as Element);
                    _controller.AtomOptions.Add(newOption);
                    _controller.SelectedElement = popupPickerSelectedElement;
                }
            }
        }
    }
}