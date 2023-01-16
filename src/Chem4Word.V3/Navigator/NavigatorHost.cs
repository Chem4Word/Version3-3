// ---------------------------------------------------------------------------
//  Copyright (c) 2023, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.ACME;
using Microsoft.Office.Interop.Word;
using System.Windows.Forms;

namespace Chem4Word.Navigator
{
    public partial class NavigatorHost : UserControl
    {
        private AcmeOptions _acmeOptions;

        public NavigatorHost()
        {
            InitializeComponent();
            _acmeOptions = new AcmeOptions();
        }

        public void SetDocument(Document document)
        {
            if (elementHost.Child is NavigatorViewControl nvc)
            {
                _acmeOptions = new AcmeOptions(Globals.Chem4WordV3.AddInInfo.ProductAppDataPath);
                nvc.SetOptions(_acmeOptions);

                var controller = new NavigatorController(document);
                nvc.ActiveDocument = document;

                nvc.DataContext = controller;
            }
        }

        private void NavigatorHost_Load(object sender, System.EventArgs e)
        {
            var wpfChild = new NavigatorViewControl();
            elementHost.Child = wpfChild;
        }
    }
}