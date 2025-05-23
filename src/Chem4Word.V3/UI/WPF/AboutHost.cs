﻿// ---------------------------------------------------------------------------
//  Copyright (c) 2025, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.Core.Helpers;
using Chem4Word.Core.UI;
using Chem4Word.Core.UI.Forms;
using System;
using System.Reflection;
using System.Windows.Forms;

namespace Chem4Word.UI.WPF
{
    public partial class AboutHost : Form
    {
        private static string _product = Assembly.GetExecutingAssembly().FullName.Split(',')[0];
        private static string _class = MethodBase.GetCurrentMethod().DeclaringType?.Name;

        public string VersionString { get; set; }
        public System.Windows.Point TopLeft { get; set; }

        public AboutHost()
        {
            InitializeComponent();
        }

        private void OnLoad_AboutHost(object sender, EventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
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

                    aboutControl1.TopLeft = TopLeft;
                    aboutControl1.VersionString = VersionString;
                }
            }
            catch (Exception ex)
            {
                using (var form = new ReportError(Globals.Chem4WordV3.Telemetry, TopLeft, module, ex))
                {
                    form.ShowDialog();
                }
            }
        }
    }
}