﻿// ---------------------------------------------------------------------------
//  Copyright (c) 2025, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.Core.Helpers;
using System.Windows.Forms;

namespace Chem4Word.Core.UI.Forms
{
    public partial class Progress : Form
    {
        private const int CP_NOCLOSE_BUTTON = 0x200;

        public System.Windows.Point TopLeft { get; set; }

        public int Value
        {
            set
            {
                if (value >= customProgressBar1.Minimum && value <= customProgressBar1.Maximum)
                {
                    customProgressBar1.Value = value;
                    SetProgressBarText();
                }
            }
        }

        public int Minimum
        {
            set
            {
                if (value >= 0)
                {
                    customProgressBar1.Minimum = value;
                }
            }
        }

        public int Maximum
        {
            set
            {
                if (value > 0)
                {
                    customProgressBar1.Maximum = value;
                }
            }
        }

        public string Message
        {
            set
            {
                try
                {
                    label1.Text = value;
                    Application.DoEvents();
                    Refresh();
                }
                catch
                {
                    //
                }
            }
        }

        public Progress()
        {
            InitializeComponent();
        }

        public void Increment(int value)
        {
            customProgressBar1.Value += value;
            SetProgressBarText();
        }

        private void SetProgressBarText()
        {
            if (customProgressBar1.Value > 0)
            {
                customProgressBar1.Text = $"{customProgressBar1.Value}/{customProgressBar1.Maximum}";
            }
            else
            {
                customProgressBar1.Text = "";
            }
        }

        // Disable Close Button
        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams myCp = base.CreateParams;
#if DEBUG
#else
                myCp.ClassStyle = myCp.ClassStyle | CP_NOCLOSE_BUTTON;
#endif
                return myCp;
            }
        }

        private void OnLoad_FormProgress(object sender, System.EventArgs e)
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
#if DEBUG
            this.TopMost = false;
#endif
        }
    }
}