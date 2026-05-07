// ---------------------------------------------------------------------------
//  Copyright (c) 2026, The .NET Foundation.
//  This software is released under the Apache Licence, Version 2.0.
//  The licence and further copyright text can be found in the file LICENCE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

namespace Chem4Word.ACME.Commands.Atom
{
    public class RemoveRadicalElectronsCommand : BaseCommand
    {
        private EditController _controller;

        public RemoveRadicalElectronsCommand(EditController controller) : base(controller)
        {
            _controller = controller;
        }

        public override bool CanExecute(object parameter)
        {
            return EditController.CanRemoveRadical(parameter as Model2.Atom);
        }

        public override void Execute(object parameter)
        {
            _controller.RemoveRadicalElectrons(parameter as Model2.Atom);
            _controller.RemoveFromSelection(parameter);
        }
    }
}
