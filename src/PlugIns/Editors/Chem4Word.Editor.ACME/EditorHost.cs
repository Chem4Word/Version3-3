// ---------------------------------------------------------------------------
//  Copyright (c) 2025, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.ACME.Enums;
using Chem4Word.Core;
using Chem4Word.Core.Helpers;
using Chem4Word.Core.UI;
using Chem4Word.Core.UI.Wpf;
using Chem4Word.Model2;
using Chem4Word.Model2.Converters.CML;
using Chem4Word.Model2.Helpers;
using IChem4Word.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Forms;
using Size = System.Drawing.Size;

namespace Chem4Word.Editor.ACME
{
    public partial class EditorHost : Form
    {
        private static readonly string _product = Assembly.GetExecutingAssembly().FullName.Split(',')[0];
        private static readonly string _class = MethodBase.GetCurrentMethod().DeclaringType?.Name;

        public Point TopLeft { get; set; }
        public IChem4WordTelemetry Telemetry { get; set; }

        public Size FormSize { get; set; }

        public string OutputCml { get; set; }

        private readonly string _cml;
        private readonly List<string> _used1DProperties;
        private readonly RenderingOptions _defaultRenderingOptions;

        private bool IsLoading { get; set; } = true;

        public EditorHost(string cml, List<string> used1DProperties, string defaultOptions)
        {
            using (new WaitCursor())
            {
                InitializeComponent();

                _cml = cml;
                _used1DProperties = used1DProperties;
                _defaultRenderingOptions = new RenderingOptions(defaultOptions);
            }
        }

        private void OnLocationChanged_EditorHost(object sender, EventArgs e)
        {
            if (!IsLoading)
            {
                TopLeft = new Point(Left + Constants.TopLeftOffset / 2, Top + Constants.TopLeftOffset / 2);
                if (elementHost1.Child is Chem4Word.ACME.Editor editor)
                {
                    editor.TopLeft = TopLeft;
                }
            }
        }

        private void OnLoad_EditorHost(object sender, EventArgs e)
        {
            using (new WaitCursor())
            {
                IsLoading = true;

                if (!PointHelper.PointIsEmpty(TopLeft))
                {
                    Left = (int)TopLeft.X;
                    Top = (int)TopLeft.Y;
                    var screen = Screen.FromControl(this);
                    var sensible = PointHelper.SensibleTopLeft(TopLeft, screen, Width, Height);
                    Left = (int)sensible.X;
                    Top = (int)sensible.Y;
                }

                MinimumSize = new Size(900, 600);

                if (FormSize.Width != 0 && FormSize.Height != 0)
                {
                    Width = FormSize.Width;
                    Height = FormSize.Height;
                }

                // Set Up WPF UC
                if (elementHost1.Child is Chem4Word.ACME.Editor editor)
                {
                    editor.ShowFeedback = false;
                    editor.TopLeft = TopLeft;
                    editor.Telemetry = Telemetry;
                    editor.SetProperties(_cml, _used1DProperties, _defaultRenderingOptions);
                    editor.OnFeedbackChange += OnFeedbackChange_AcmeEditor;

                    var model = editor.ActiveController.Model;
                    if (model == null || model.Molecules.Count == 0)
                    {
                        Text = "ACME - New structure";
                    }
                    else
                    {
                        var parts = FormulaHelper.ParseFormulaIntoParts(model.ConciseFormula);
                        var formulaPartsAsUnicode = FormulaHelper.FormulaPartsAsUnicode(parts);
                        Text = "ACME - Editing " + formulaPartsAsUnicode;
                    }
                }

                IsLoading = false;
            }
        }

        private void OnFeedbackChange_AcmeEditor(object sender, WpfEventArgs e)
        {
            var activeController = ((Chem4Word.ACME.Editor)elementHost1.Child).ActiveController;
            var activeModel = activeController.Model;
            bool hasReactions = (activeModel.ReactionSchemes.Any() &&
                                 activeModel.ReactionSchemes.First().Value.Reactions.Count > 0);
            bool moleculesSelected = (activeController.SelectionType == SelectionTypeCode.Molecule);
            if (hasReactions && moleculesSelected || !hasReactions)
            {
                MWTDisplay.Text = e.MolecularWeight;
                FormulaDisplay.Text = e.Formula;
            }
            else
            {
                MWTDisplay.Text = "";
                FormulaDisplay.Text = "";
            }
        }

        private void OnClick_Save(object sender, EventArgs e)
        {
            var module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            var cc = new CMLConverter();
            DialogResult = DialogResult.Cancel;

            if (elementHost1.Child is Chem4Word.ACME.Editor editor
                && editor.IsDirty)
            {
                DialogResult = DialogResult.OK;
                OutputCml = cc.Export(editor.EditedModel);
            }
            Telemetry.Write(module, "Verbose", $"Result: {DialogResult}");
            Hide();
        }

        private void OnClick_Cancel(object sender, EventArgs e)
        {
            var module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            DialogResult = DialogResult.Cancel;
            Telemetry.Write(module, "Verbose", $"Result: {DialogResult}");
            Hide();
        }

        private void OnFormClosing_EditorHost(object sender, FormClosingEventArgs e)
        {
            var module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            if (DialogResult != DialogResult.OK && e.CloseReason == CloseReason.UserClosing)
            {
                if (elementHost1.Child is Chem4Word.ACME.Editor editor
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
                            var model = editor.EditedModel;
                            // Replace any temporary Ids which are Guids
                            model.ReLabelGuids();
                            var cc = new CMLConverter();
                            OutputCml = cc.Export(model);
                            Telemetry.Write(module, "Verbose", $"Result: {DialogResult}");
                            Hide();
                            editor = null;
                            break;

                        case DialogResult.No:
                            Telemetry.Write(module, "Verbose", $"Result: {DialogResult}");
                            editor = null;
                            break;
                    }
                }
            }
        }
    }
}