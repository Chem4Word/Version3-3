// ---------------------------------------------------------------------------
//  Copyright (c) 2025, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.Core.Helpers;
using Chem4Word.Core.UI.Forms;
using IChem4Word.Contracts;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Forms;

namespace Chem4Word.Editor.ACME
{
    public class Editor : IChem4WordEditor
    {
        private static string _product = Assembly.GetExecutingAssembly().FullName.Split(',')[0];
        private static string _class = MethodBase.GetCurrentMethod().DeclaringType?.Name;

        public string Name => Constants.DefaultEditorPlugIn;

        public string Description => "This is the standard editor for Chem4Word 2025. ACME stands for Advanced CML-based Molecule Editor.";

        public bool HasSettings => false;

        public bool CanEditNestedMolecules => true;
        public bool CanEditFunctionalGroups => true;
        public bool CanEditReactions => true;
        public bool RequiresSeedAtom => false;

        public string SettingsPath { get; set; }

        public string DefaultRenderingOptions { get; set; }

        public List<string> Used1DProperties { get; set; }

        public Point TopLeft { get; set; }

        public IChem4WordTelemetry Telemetry { get; set; }

        public Dictionary<string, string> Properties { get; set; }

        public string Cml { get; set; }

        public bool ChangeSettings(Point topLeft) => false;

        public DialogResult Edit()
        {
            var module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            var dialogResult = DialogResult.Cancel;

            try
            {
                Telemetry.Write(module, "Verbose", "Called");

                if (string.IsNullOrEmpty(DefaultRenderingOptions))
                {
                    Debugger.Break();
                }

                using (var host = new EditorHost(Cml, Used1DProperties, DefaultRenderingOptions))
                {
                    host.TopLeft = TopLeft;
                    host.Telemetry = Telemetry;

                    var showDialog = host.ShowDialog();
                    if (showDialog == DialogResult.OK)
                    {
                        dialogResult = showDialog;
                        Cml = host.OutputCml;
                    }
                }
            }
            catch (Exception ex)
            {
                new ReportError(Telemetry, TopLeft, module, ex).ShowDialog();
            }

            return dialogResult;
        }
    }
}