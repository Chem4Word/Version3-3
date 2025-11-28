// ---------------------------------------------------------------------------
//  Copyright (c) 2026, The .NET Foundation.
//  This software is released under the Apache Licence, Version 2.0.
//  The licence and further copyright text can be found in the file LICENCE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.Core.UI.Forms;
using Chem4Word.Model2;
using IChem4Word.Contracts;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Windows;
using System.Windows.Forms;

namespace Chem4Word.Editor.SimpleWpfEditor
{
    public class Editor : IChem4WordEditor
    {
        private static string _product = Assembly.GetExecutingAssembly().FullName.Split(',')[0];
        private static string _class = MethodBase.GetCurrentMethod().DeclaringType?.Name;

        public string Name => "Example Wpf Structure Editor";

        public string Description => "This is a PoC to show that a WPF editor can be made";

        public Point TopLeft { get; set; }

        public bool HasSettings => false;
        public bool CanEditNestedMolecules => true;
        public bool CanEditFunctionalGroups => true;
        public bool CanEditReactions => true;
        public bool RequiresSeedAtom => true;

        public string SettingsPath { get; set; }

        public string DefaultRenderingOptions { get; set; }
        public List<string> Used1DProperties { get; set; }

        public string Cml { get; set; }

        public Dictionary<string, string> Properties { get; set; }

        public IChem4WordTelemetry Telemetry { get; set; }

        public Editor()
        {
            // Nothing to do here
        }

        public bool ChangeSettings(Point topLeft) => false;

        public DialogResult Edit()
        {
            var dialogResult = DialogResult.Cancel;

            var module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                Telemetry.Write(module, "Verbose", "Called");

                using (var host = new EditorHost(Cml))
                {
                    var renderingOptions = new RenderingOptions(DefaultRenderingOptions);
                    host.TopLeft = TopLeft;
                    host.DefaultBondLength = renderingOptions.DefaultBondLength;

                    var showDialog = host.ShowDialog();
                    if (showDialog == DialogResult.OK)
                    {
                        dialogResult = showDialog;
                        Cml = host.OutputCml;
                    }

                    host.Close();
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