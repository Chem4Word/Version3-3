// ---------------------------------------------------------------------------
//  Copyright (c) 2026, The .NET Foundation.
//  This software is released under the Apache Licence, Version 2.0.
//  The licence and further copyright text can be found in the file LICENCE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Microsoft.Office.Interop.Word;
using System.Windows.Forms;

namespace Chem4Word.Navigator
{
    public partial class NavigatorHost : UserControl
    {
        public NavigatorHost()
        {
            InitializeComponent();
        }

        public void SetDocument(Document document)
        {
            if (elementHost.Child is NavigatorViewControl nvc)
            {
                var controller = new NavigatorController(document);
                nvc.ActiveDocument = document;

                nvc.DataContext = controller;
            }
        }

        private void OnLoad_NavigatorHost(object sender, System.EventArgs e)
        {
            var wpfChild = new NavigatorViewControl();
            elementHost.Child = wpfChild;
        }
    }
}