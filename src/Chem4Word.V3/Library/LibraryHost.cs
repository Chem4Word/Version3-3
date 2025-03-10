﻿// ---------------------------------------------------------------------------
//  Copyright (c) 2025, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.Core.UI;
using Chem4Word.Core.UI.Forms;
using System;
using System.Reflection;
using System.Windows.Forms;

namespace Chem4Word.Library
{
    public partial class LibraryHost : UserControl
    {
        private static string _product = Assembly.GetExecutingAssembly().FullName.Split(',')[0];
        private static string _class = MethodBase.GetCurrentMethod().DeclaringType?.Name;

        private LibraryController _libraryController;

        public LibraryHost()
        {
            InitializeComponent();
        }

        public void Clear()
        {
            _libraryController = null;
        }

        public override void Refresh()
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                using (new WaitCursor())
                {
                    var libraryViewControl = new LibraryViewControl();
                    elementHost1.Child = libraryViewControl;

                    _libraryController = new LibraryController(Globals.Chem4WordV3.Telemetry);
                    libraryViewControl.DataContext = _libraryController;
                }
            }
            catch (Exception ex)
            {
                using (var form = new ReportError(Globals.Chem4WordV3.Telemetry, Globals.Chem4WordV3.WordTopLeft, module, ex))
                {
                    form.ShowDialog();
                }
            }
        }

        private void OnLoaded_LibraryHost(object sender, EventArgs e)
        {
            var wpfChild = new LibraryViewControl();
            elementHost1.Child = wpfChild;
        }
    }
}