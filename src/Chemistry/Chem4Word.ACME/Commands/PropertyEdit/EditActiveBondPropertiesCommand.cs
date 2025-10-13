// ---------------------------------------------------------------------------
//  Copyright (c) 2025, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.Model2;

namespace Chem4Word.ACME.Commands.PropertyEdit
{
    public class EditActiveBondPropertiesCommand : BaseCommand
    {
        public EditActiveBondPropertiesCommand(EditController controller) : base(controller)
        {
        }

        public override bool CanExecute(object parameter)
        {
            return true;
        }

        public override void Execute(object parameter)
        {
            EditController.EditBondProperties(parameter as Bond);
        }
    }
}
