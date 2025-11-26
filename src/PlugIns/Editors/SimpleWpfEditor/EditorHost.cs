// ---------------------------------------------------------------------------
//  Copyright (c) 2025, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.ACME;
using Chem4Word.Core;
using Chem4Word.Core.Helpers;
using Chem4Word.Model2.Converters.CML;
using System;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using Point = System.Windows.Point;

namespace Chem4Word.Editor.SimpleWpfEditor
{
    public partial class EditorHost : Form
    {
        public System.Windows.Point TopLeft { get; set; }

        public double DefaultBondLength { get; set; }

        public Size FormSize { get; set; }

        public string OutputCml { get; set; }
        private string _cml;

        public EditorHost(string cml)
        {
            InitializeComponent();
            _cml = cml;
        }

        private void OnLoad_EditorHost(object sender, EventArgs e)
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

            MinimumSize = new Size(300, 200);

            if (FormSize.Width != 0 && FormSize.Height != 0)
            {
                Width = FormSize.Width;
                Height = FormSize.Height;
            }

            // Set Up WPF UC
            if (elementHost1.Child is CmlEditor editor)
            {
                editor.Cml = _cml;
                editor.DefaultBondLength = DefaultBondLength;
            }
        }

        private void OnClick_Save(object sender, EventArgs e)
        {
            var cc = new CMLConverter();
            DialogResult = DialogResult.Cancel;

            if (elementHost1.Child is CmlEditor editor
                && editor.IsDirty)
            {
                DialogResult = DialogResult.OK;
                OutputCml = cc.Export(editor.EditedModel);
            }
            Hide();
        }

        private void OnClick_Cancel(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Hide();
        }

        private void OnFormClosing_EditorHost(object sender, FormClosingEventArgs e)
        {
            if (DialogResult != DialogResult.OK && e.CloseReason == CloseReason.UserClosing
                                                && elementHost1.Child is CmlEditor editor
                                                && editor.IsDirty)
            {
                var sb = new StringBuilder();
                sb.AppendLine("Do you wish to save your changes?");
                sb.AppendLine("  Click 'Yes' to save your changes and exit.");
                sb.AppendLine("  Click 'No' to discard your changes and exit.");
                sb.AppendLine("  Click 'Cancel' to return to the form.");

                var dr = UserInteractions.AskUserYesNoCancel(sb.ToString());
                switch (dr)
                {
                    case DialogResult.Cancel:
                        e.Cancel = true;
                        break;

                    case DialogResult.Yes:
                        DialogResult = DialogResult.OK;
                        var cc = new CMLConverter();
                        OutputCml = cc.Export(editor.EditedModel);
                        Hide();
                        break;

                    case DialogResult.No:
                        break;
                }
            }
        }
    }
}
