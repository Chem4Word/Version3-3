﻿// ---------------------------------------------------------------------------
//  Copyright (c) 2025, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.Core.Helpers;
using Chem4Word.Core.UI.Forms;
using Chem4Word.Model2;
using System;
using System.Reflection;
using System.Windows.Forms;

namespace Chem4Word.UI
{
    public partial class ImportErrors : Form
    {
        private static string _product = Assembly.GetExecutingAssembly().FullName.Split(',')[0];
        private static string _class = MethodBase.GetCurrentMethod().DeclaringType?.Name;

        public Model Model { get; set; }

        public System.Windows.Point TopLeft { get; set; }

        private DialogResult _dialogResult = DialogResult.Abort;

        public ImportErrors()
        {
            InitializeComponent();
        }

        private void OnLoad_ImportErrors(object sender, EventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
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

                display1.Chemistry = Model;
                Errors.Text = String.Join(Environment.NewLine, Model.AllErrors);
                Warnings.Text = String.Join(Environment.NewLine, Model.AllWarnings);
                if (Model.AllErrors.Count > 0)
                {
                    Continue.Enabled = false;
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

        private void OnClick_Continue(object sender, EventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            _dialogResult = DialogResult.OK;
            DialogResult = DialogResult.OK;
        }

        private void OnClick_Abort(object sender, EventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            _dialogResult = DialogResult.Cancel;
            DialogResult = DialogResult.Cancel;
        }

        private void OnFormClosing_ImportErrors(object sender, FormClosingEventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            if (_dialogResult == DialogResult.Abort)
            {
                if (Model.AllErrors.Count > 0)
                {
                    DialogResult = DialogResult.Cancel;
                }
                else
                {
                    DialogResult = DialogResult.OK;
                }
            }
            else
            {
                DialogResult = _dialogResult;
            }
        }
    }
}