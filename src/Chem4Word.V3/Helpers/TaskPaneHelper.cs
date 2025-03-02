﻿// ---------------------------------------------------------------------------
//  Copyright (c) 2025, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.Core;
using Chem4Word.Core.Helpers;
using Chem4Word.Core.UI.Forms;
using Chem4Word.Model2.Converters.CML;
using Microsoft.Office.Interop.Word;
using System;
using System.Reflection;

namespace Chem4Word.Helpers
{
    internal static class TaskPaneHelper
    {
        private static string _product = Assembly.GetExecutingAssembly().FullName.Split(',')[0];
        private static string _class = MethodBase.GetCurrentMethod()?.DeclaringType?.Name;

        public static void InsertChemistry(bool isCopy, Application application, string cml, bool fromLibrary)
        {
            var module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod()?.Name}()";

            var document = application.ActiveDocument;
            var selection = application.Selection;

            ContentControl contentControl = null;

            if (Globals.Chem4WordV3.SystemOptions == null)
            {
                Globals.Chem4WordV3.LoadOptions();
            }

            var allowed = true;
            var reason = "";

            if (Globals.Chem4WordV3.ChemistryAllowed)
            {
                if (selection.ContentControls.Count > 0)
                {
                    contentControl = selection.ContentControls[1];
                    if (contentControl.Title != null && contentControl.Title.Equals(Constants.ContentControlTitle))
                    {
                        reason = "a chemistry object is selected";
                        allowed = false;
                    }
                }
            }
            else
            {
                reason = Globals.Chem4WordV3.ChemistryProhibitedReason;
                allowed = false;
            }

            if (allowed)
            {
                try
                {
                    var cmlConverter = new CMLConverter();
                    var model = cmlConverter.Import(cml);

                    if (fromLibrary)
                    {
                        if (Globals.Chem4WordV3.SystemOptions.RemoveExplicitHydrogensOnImportFromLibrary)
                        {
                            model.RemoveExplicitHydrogens();
                        }

                        var outcome = model.EnsureBondLength(Globals.Chem4WordV3.SystemOptions.BondLength,
                                               Globals.Chem4WordV3.SystemOptions.SetBondLengthOnImportFromLibrary);
                        if (!string.IsNullOrEmpty(outcome))
                        {
                            Globals.Chem4WordV3.Telemetry.Write(module, "Information", outcome);
                        }

                        model.ExplicitC = Globals.Chem4WordV3.SystemOptions.ExplicitC;
                        model.ExplicitH = Globals.Chem4WordV3.SystemOptions.ExplicitH;
                        model.ShowColouredAtoms = Globals.Chem4WordV3.SystemOptions.ShowColouredAtoms;
                        model.ShowMoleculeGrouping = Globals.Chem4WordV3.SystemOptions.ShowMoleculeGrouping;
                    }

                    contentControl = ChemistryHelper.Insert2DChemistry(document, cmlConverter.Export(model), isCopy);
                }
                catch (Exception ex)
                {
                    using (var form = new ReportError(Globals.Chem4WordV3.Telemetry, Globals.Chem4WordV3.WordTopLeft, module, ex))
                    {
                        form.ShowDialog();
                    }
                }
                finally
                {
                    if (contentControl != null)
                    {
                        // Move selection point into the Content Control which was just edited or added
                        application.Selection.SetRange(contentControl.Range.Start, contentControl.Range.End);
                    }
                }
            }
            else
            {
                UserInteractions.WarnUser($"You can't insert a chemistry object because {reason}");
                Globals.Chem4WordV3.EventsEnabled = true;
            }
        }
    }
}