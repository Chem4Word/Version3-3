// ---------------------------------------------------------------------------
//  Copyright (c) 2025, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.Model2;
using Chem4Word.Model2.Enums;

namespace Chem4Word.ACME.Commands.PropertyEdit
{
    public class FlipBondStereoCommand : BaseCommand
    {
        public FlipBondStereoCommand(EditController controller) : base(controller)
        {
        }

        public override bool CanExecute(object parameter)
        {
            if (!(parameter is Bond affectedBond))
            {
                return false;
            }

            return (affectedBond.Stereo == BondStereo.Wedge || affectedBond.Stereo == BondStereo.Hatch);
        }

        public override void Execute(object parameter)
        {
            EditController.SwapBondDirection(parameter as Bond);
        }
    }
}
