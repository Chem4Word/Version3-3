// ---------------------------------------------------------------------------
//  Copyright (c) 2026, The .NET Foundation.
//  This software is released under the Apache Licence, Version 2.0.
//  The licence and further copyright text can be found in the file LICENCE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.Core.Helpers;
using System.Diagnostics;

namespace Chem4Word.ACME.Commands
{
    public class FuseCommand : BaseCommand
    {
        public FuseCommand(EditController controller) : base(controller)
        {
        }

        public override bool CanExecute(object parameter)
        {
            return false;
        }

        public override void Execute(object parameter)
        {
            string message = $"We should never get here !";
            Debug.WriteLine(message);
            Debugger.Break();
        }
    }
}
