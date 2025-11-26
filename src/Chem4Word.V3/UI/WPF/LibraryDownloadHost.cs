// ---------------------------------------------------------------------------
//  Copyright (c) 2025, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.Core.Helpers;
using Chem4Word.Core.UI.Forms;
using Chem4Word.Core.UI.Wpf;
using System;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using Point = System.Windows.Point;

namespace Chem4Word.UI.WPF
{
    public partial class LibraryDownloadHost : Form
    {
        private static string _product = Assembly.GetExecutingAssembly().FullName.Split(',')[0];
        private static string _class = MethodBase.GetCurrentMethod()?.DeclaringType?.Name;

        public System.Windows.Point TopLeft { get; set; }

        public LibraryDownloadHost()
        {
            InitializeComponent();
        }

        private void OnClick_WpfButton(object sender, EventArgs e)
        {
            var args = (WpfEventArgs)e;
            switch (args.Button.ToLower())
            {
                case "finished":
                    Hide();
                    break;
            }
        }

        private void OnLoad_LibraryDownloadHost(object sender, EventArgs e)
        {
            var module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod()?.Name}()";

            try
            {
                var control = new LibraryDownloadControl();
                elementHost1.Child = control;
                control.TopLeft = TopLeft;
                control.OnButtonClick += OnClick_WpfButton;

                if (!PointHelper.PointIsEmpty(TopLeft))
                {
                    Left = (int)TopLeft.X;
                    Top = (int)TopLeft.Y;
                    var screen = Screen.FromControl(this);
                    var sensible = PointHelper.SensibleTopLeft(new Point(Left, Top), screen, Width, Height);
                    Left = (int)sensible.X;
                    Top = (int)sensible.Y;
                }

                MinimumSize = new Size(850, 450);
            }
            catch (Exception exception)
            {
                new ReportError(Globals.Chem4WordV3.Telemetry, TopLeft, module, exception).ShowDialog();
            }
        }
    }
}
