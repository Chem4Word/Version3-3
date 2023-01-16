// ---------------------------------------------------------------------------
//  Copyright (c) 2023, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System;
using System.Reflection;
using System.Windows.Forms;
using Chem4Word.ACME;
using Chem4Word.Core.UI;
using Chem4Word.Core.UI.Forms;

namespace Chem4Word.Library
{
    public partial class LibraryHost : UserControl
    {
        private static string _product = Assembly.GetExecutingAssembly().FullName.Split(',')[0];
        private static string _class = MethodBase.GetCurrentMethod().DeclaringType?.Name;

        private LibraryController _libraryController;
        private AcmeOptions _editorOptions;

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
                if (elementHost1.Child is LibraryViewControl lvc)
                {
                    _editorOptions = new AcmeOptions(Globals.Chem4WordV3.AddInInfo.ProductAppDataPath);
                    lvc.SetOptions(_editorOptions);

                    using (new WaitCursor())
                    {
                        if (_libraryController == null)
                        {
                            _libraryController = new LibraryController(Globals.Chem4WordV3.Telemetry);
                        }
                        lvc.DataContext = _libraryController;
                    }
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

        private void LibraryHost_Load(object sender, EventArgs e)
        {
            var wpfChild = new LibraryViewControl();
            elementHost1.Child = wpfChild;
        }
    }
}