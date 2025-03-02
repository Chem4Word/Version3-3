﻿// ---------------------------------------------------------------------------
//  Copyright (c) 2025, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.Core.Helpers;
using IChem4Word.Contracts;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using Color = System.Drawing.Color;

namespace Chem4Word.Core.UI.Forms
{
    public partial class ReportError : Form
    {
        private IChem4WordTelemetry _telemetry;

        private static readonly string _product = Assembly.GetExecutingAssembly().FullName.Split(',')[0];
        private static readonly string _class = MethodBase.GetCurrentMethod().DeclaringType?.Name;

        private string _exceptionMessage = string.Empty;
        private string _operation = string.Empty;
        private string _callStack = string.Empty;

        public System.Windows.Point TopLeft { get; set; }

        public ReportError(IChem4WordTelemetry telemetry, System.Windows.Point topLeft, string operation, Exception ex)
        {
            InitializeComponent();

            try
            {
                TopLeft = topLeft;
                _telemetry = telemetry;

                _operation = operation;
                _callStack = ex.ToString();
                _exceptionMessage = ex.Message;
                if (ex.InnerException != null)
                {
                    _exceptionMessage += Environment.NewLine + ex.InnerException.Message;
                }
            }
            catch (Exception)
            {
                // Do Nothing
            }
        }

        private void OnLoad_ErrorReport(object sender, EventArgs e)
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

            try
            {
                textBox1.Text = _exceptionMessage;
            }
            catch (Exception)
            {
                // Do Nothing
            }
        }

        private void OnClick_Submit(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
            Close();
        }

        private void OnFormClosing_ReportError(object sender, FormClosingEventArgs e)
        {
            if (_telemetry != null)
            {
                if (!string.IsNullOrEmpty(_exceptionMessage))
                {
                    _telemetry.Write(_operation, "Exception", _exceptionMessage);
                }
                if (!string.IsNullOrEmpty(_callStack))
                {
                    _telemetry.Write(_operation, "Exception", _callStack);
                }

                if (DialogResult == DialogResult.OK)
                {
                    if (!string.IsNullOrEmpty(UserEmailAddress.Text))
                    {
                        _telemetry.Write(_operation, "Exception(Data)", UserEmailAddress.Text);
                    }
                    if (!string.IsNullOrEmpty(UserComments.Text))
                    {
                        _telemetry.Write(_operation, "Exception(Data)", UserComments.Text);
                    }
                }
            }
        }

        private void OnLinkClicked_KBLinkLabel(object sender, LinkLabelLinkClickedEventArgs e)
        {
            var module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod()?.Name}()";
            _telemetry.Write(module, "Action", "Triggered");
            Process.Start("https://www.chem4word.co.uk/knowledge-base/");
        }

        private void OnTextChanged_UserEmailAddress(object sender, EventArgs e)
        {
            UserEmailAddress.BackColor = StringHelper.IsValidEmail(UserEmailAddress.Text)
                ? SystemColors.Window
                : Color.Salmon;
        }
    }
}