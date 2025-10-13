// ---------------------------------------------------------------------------
//  Copyright (c) 2025, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.Model2;
using Chem4Word.Model2.Converters.CML;
using Chem4Word.Model2.Converters.MDL;
using Chem4Word.Model2.Converters.SketchEl;
using System;
using System.Linq;
using System.Windows;

namespace Chem4Word.ACME.Commands.Editing
{
    public class PasteCommand : BaseCommand
    {
        public PasteCommand(EditController controller) : base(controller)
        {
        }

        public override bool CanExecute(object parameter)
        {
            var canExecute = Clipboard.ContainsData(ModelConstants.FormatCML) || Clipboard.ContainsData(ModelConstants.FormatSDFile) || Clipboard.ContainsText();
            return canExecute;
        }

        public override void Execute(object parameter)
        {
            var cmlConverter = new CMLConverter();
            var sdfConverter = new SdFileConverter();
            var sketchElConverter = new SketchElConverter();

            if (Clipboard.ContainsData(ModelConstants.FormatCML))
            {
                string pastedCML = (string)Clipboard.GetData(ModelConstants.FormatCML);
                EditController.PasteCML(pastedCML, parameter as Point?);
            }
            else if (Clipboard.ContainsText())
            {
                bool failedCML = false;
                bool failedOther = false;
                string pastedText = Clipboard.GetText();
                Model buffer = null;
                //try to convert the pasted text with the CML converter first
                try
                {
                    buffer = cmlConverter.Import(pastedText);
                }
                catch
                {
                    failedCML = true;
                }

                if (failedCML)
                {
                    if (pastedText.Contains(ModelConstants.M_END))
                    {
                        buffer = sdfConverter.Import(pastedText);
                        failedOther = buffer.AllErrors.Any();
                    }
                    else if (pastedText.StartsWith("SketchEl!"))
                    {
                        buffer = sketchElConverter.Import(pastedText);
                        failedOther = buffer.AllErrors.Any();
                    }
                    else
                    {
                        failedOther = true;
                    }
                }

                if (failedCML && failedOther)
                {
                    if (buffer != null && buffer.AllErrors.Any())
                    {
                        Chem4Word.Core.UserInteractions.InformUser("Unable to paste text as chemistry: " + Environment.NewLine + string.Join(Environment.NewLine, buffer.AllErrors));
                    }
                    else
                    {
                        Chem4Word.Core.UserInteractions.InformUser("Unable to paste text as chemistry: unknown error.");
                    }
                }
                else
                {
                    EditController.PasteModel(buffer, pasteAt: (Point)parameter);
                }
            }
        }
    }
}
