// ---------------------------------------------------------------------------
//  Copyright (c) 2025, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.Core;
using Chem4Word.Core.Helpers;
using Chem4Word.Core.UI;
using Chem4Word.Core.UI.Forms;
using Chem4Word.Core.UI.Wpf;
using System;
using System.Drawing;
using System.Reflection;
using System.Text;
using System.Windows.Forms;

namespace Chem4Word.UI.WPF
{
    public partial class SettingsHost : Form
    {
        private static string _product = Assembly.GetExecutingAssembly().FullName.Split(',')[0];
        private static string _class = MethodBase.GetCurrentMethod().DeclaringType?.Name;

        private bool _closedInCode = false;

        public System.Windows.Point TopLeft { get; set; }

        public Chem4WordOptions SystemOptions { get; set; }

        public string ActiveTab { get; set; }

        public SettingsHost()
        {
            InitializeComponent();
        }

        private void OnClick_WpfButton(object sender, EventArgs e)
        {
            WpfEventArgs args = (WpfEventArgs)e;
            switch (args.Button.ToLower())
            {
                case "ok":
                    DialogResult = DialogResult.OK;
                    if (elementHost1.Child is SettingsControl sc)
                    {
                        if (sc.Dirty)
                        {
                            SystemOptions = sc.SystemOptions;
                            SystemOptions.Save();
                            sc.Dirty = false;
                        }
                        Hide();
                    }
                    break;

                case "cancel":
                    DialogResult = DialogResult.Cancel;
                    _closedInCode = true;
                    Hide();
                    break;
            }
        }

        private void OnLoad_SettingsHost(object sender, EventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                var sc = new SettingsControl();
                elementHost1.Child = sc;
                sc.TopLeft = TopLeft;
                sc.SystemOptions = SystemOptions;
                sc.ActiveTab = ActiveTab;
                sc.OnButtonClick += OnClick_WpfButton;

                using (new WaitCursor())
                {
                    if (!PointHelper.PointIsEmpty(TopLeft))
                    {
                        Left = (int)TopLeft.X;
                        Top = (int)TopLeft.Y;
                        var screen = Screen.FromControl(this);
                        var sensible = PointHelper.SensibleTopLeft(TopLeft, screen, Width, Height);
                        Left = (int)sensible.X;
                        Top = (int)sensible.Y;
                    }

                    MinimumSize = new Size(1000, 600);
                }
            }
            catch (Exception ex)
            {
                new ReportError(Globals.Chem4WordV3.Telemetry, TopLeft, module, ex).ShowDialog();
            }
        }

        private void OnFormClosing_SettingsHost(object sender, FormClosingEventArgs e)
        {
            if (!_closedInCode)
            {
                if (elementHost1.Child is SettingsControl sc && sc.Dirty)
                {
                    StringBuilder sb = new StringBuilder();
                    sb.AppendLine("Do you wish to save your changes?");
                    sb.AppendLine("  Click 'Yes' to save your changes and exit.");
                    sb.AppendLine("  Click 'No' to discard your changes and exit.");
                    sb.AppendLine("  Click 'Cancel' to return to the form.");

                    DialogResult dr = UserInteractions.AskUserYesNoCancel(sb.ToString());
                    switch (dr)
                    {
                        case DialogResult.Cancel:
                            e.Cancel = true;
                            break;

                        case DialogResult.Yes:
                            SystemOptions = sc.SystemOptions;
                            SystemOptions.Save();
                            DialogResult = DialogResult.OK;
                            break;

                        case DialogResult.No:
                            DialogResult = DialogResult.Cancel;
                            break;
                    }
                }
            }
        }
    }
}