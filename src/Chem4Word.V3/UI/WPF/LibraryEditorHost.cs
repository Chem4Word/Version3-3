// ---------------------------------------------------------------------------
//  Copyright (c) 2026, The .NET Foundation.
//  This software is released under the Apache Licence, Version 2.0.
//  The licence and further copyright text can be found in the file LICENCE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.Core.Helpers;
using Chem4Word.Core.UI;
using Chem4Word.Core.UI.Wpf;
using IChem4Word.Contracts;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using Point = System.Windows.Point;

namespace Chem4Word.UI.WPF
{
    public partial class LibraryEditorHost : Form
    {
        private static string _product = Assembly.GetExecutingAssembly().FullName.Split(',')[0];
        private static string _class = MethodBase.GetCurrentMethod()?.DeclaringType?.Name;

        public IChem4WordTelemetry Telemetry { get; set; }
        public string SelectedDatabase { get; set; }

        public System.Windows.Point TopLeft { get; set; }

        private IChem4WordLibraryWriter _driver;

        public LibraryEditorHost()
        {
            InitializeComponent();
        }

        private void OnLoad_LibraryEditorHost(object sender, EventArgs e)
        {
            if (!PointHelper.PointIsEmpty(TopLeft))
            {
                Left = (int)TopLeft.X;
                Top = (int)TopLeft.Y;
                var screen = Screen.FromControl(this);
                var sensible = PointHelper.SensibleTopLeft(new Point(Left, Top), screen, Width, Height);
                Left = (int)sensible.X;
                Top = (int)sensible.Y;
            }

            using (new WaitCursor())
            {
                var editor = new LibraryEditorControl(Globals.Chem4WordV3.Telemetry);
                elementHost1.Child = editor;
                editor.DefaultBondLength = Globals.Chem4WordV3.SystemOptions.BondLength;

                var details = Globals.Chem4WordV3.ListOfDetectedLibraries.AvailableDatabases
                                     .FirstOrDefault(l => l.DisplayName.Equals(SelectedDatabase));
                if (details != null)
                {
                    _driver = (IChem4WordLibraryWriter)Globals.Chem4WordV3.GetDriverPlugIn(details.Driver);
                    if (_driver != null)
                    {
                        _driver.FileName = details.Connection;

                        var controller = new LibraryEditorViewModel(Telemetry, _driver);
                        editor.TopLeft = TopLeft;

                        editor.SetDriver(_driver);
                        editor.DataContext = controller;
                        editor.UpdateStatusBar();
                        Text = $"Editing Library '{_driver.FileName}'";

                        editor.OnSelectionChange -= OnSelectionChange_LibraryEditorControl;
                        editor.OnSelectionChange += OnSelectionChange_LibraryEditorControl;
                    }
                }

                MinimumSize = new Size(1000, 600);
            }
        }

        private void OnSelectionChange_LibraryEditorControl(object sender, WpfEventArgs e)
        {
            Debug.WriteLine($"{e.Button} {e.ButtonDetails}");
        }
    }
}
