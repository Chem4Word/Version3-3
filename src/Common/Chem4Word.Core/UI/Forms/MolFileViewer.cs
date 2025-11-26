// ---------------------------------------------------------------------------
//  Copyright (c) 2025, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.Core.Helpers;
using System;
using System.Windows;
using System.Windows.Forms;

namespace Chem4Word.Core.UI.Forms
{
    public partial class MolFileViewer : Form
    {
        public string Message { get; set; }
        public System.Windows.Point TopLeft { get; set; }
        private Screen _screen;

        public MolFileViewer(System.Windows.Point topLeft, Screen screen, string sdFile)
        {
            InitializeComponent();
            _screen = screen;

            TopLeft = topLeft;
            Message = sdFile;
        }

        private void OnLoad_TextViewer(object sender, EventArgs e)
        {
            if (!PointHelper.PointIsEmpty(TopLeft))
            {
                Left = (int)TopLeft.X;
                Top = (int)TopLeft.Y;

                Point sensible = PointHelper.SensibleTopLeft(new Point(Left, Top), _screen, Width, Height);

                Left = (int)sensible.X;
                Top = (int)sensible.Y;
            }

            try
            {
                textBox1.Text = Message;
                textBox1.SelectionStart = 0;
                textBox1.SelectionLength = 1;
            }
            catch (Exception)
            {
                // Do Nothing
            }
        }
    }
}
