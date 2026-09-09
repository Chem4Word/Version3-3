// ---------------------------------------------------------------------------
//  Copyright (c) 2026, The .NET Foundation.
//  This software is released under the Apache Licence, Version 2.0.
//  The licence and further copyright text can be found in the file LICENCE.md
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
            bool canExecute = Clipboard.ContainsData(ModelConstants.FormatCML) || Clipboard.ContainsData(ModelConstants.FormatSDFile) || Clipboard.ContainsText();
            return canExecute;
        }

        public override void Execute(object parameter)
        {
            CMLConverter cmlConverter = new CMLConverter();
            SdFileConverter sdfConverter = new SdFileConverter();
            SketchElConverter sketchElConverter = new SketchElConverter();

            string pastedData = string.Empty;

            if (Clipboard.ContainsData(ModelConstants.FormatCML))
            {
                string pastedCML = (string)Clipboard.GetData(ModelConstants.FormatCML);
                pastedData = pastedCML;
                EditController.PasteCML(pastedCML, parameter as Point?);
            }
            else if (Clipboard.ContainsText())
            {
                bool failedCML = false;
                bool failedOther = false;

                string pastedText = Clipboard.GetText();
                pastedData = pastedText;

                Model model = null;
                //try to convert the pasted text with the CML converter first
                try
                {
                    model = cmlConverter.Import(pastedText);
                }
                catch
                {
                    failedCML = true;
                }

                if (failedCML)
                {
                    if (pastedText.Contains(ModelConstants.M_END))
                    {
                        model = sdfConverter.Import(pastedText);
                        failedOther = model.AllErrors.Any();
                    }
                    else if (pastedText.StartsWith("SketchEl!"))
                    {
                        model = sketchElConverter.Import(pastedText);
                        failedOther = model.AllErrors.Any();
                    }
                    else
                    {
                        failedOther = true;
                    }
                }

                if (failedCML && failedOther)
                {
                    if (model != null && model.AllErrors.Any())
                    {
                        Core.UserInteractions.InformUser("Unable to paste text as chemistry: " + Environment.NewLine + string.Join(Environment.NewLine, model.AllErrors));
                    }
                    else
                    {
                        Core.UserInteractions.InformUser("Unable to paste text as chemistry: unknown error.");
                    }
                }
                else
                {
                    EditController.PasteModel(model, pastedData, pasteAt: (Point)parameter);
                }
            }
        }
    }
}
