﻿// ---------------------------------------------------------------------------
//  Copyright (c) 2025, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.Model2;
using Chem4Word.Model2.Converters.CML;
using System.Windows.Controls;

namespace Chem4Word.ACME
{
    /// <summary>
    /// Interaction logic for CmlEditor.xaml
    /// </summary>
    public partial class CmlEditor : UserControl, IHostedWpfEditor
    {
        public CmlEditor()
        {
            InitializeComponent();
        }

        public string Cml
        {
            set
            {
                CmlText.Text = value;
                IsDirty = false;
            }
        }

        public double DefaultBondLength { get; set; }

        public bool IsDirty { get; private set; }

        public Model EditedModel
        {
            get
            {
                CMLConverter cc = new CMLConverter();
                return cc.Import(CmlText.Text);
            }
        }

        private void OnTextChanged_CmlText(object sender, TextChangedEventArgs e)
        {
            IsDirty = true;
        }
    }
}