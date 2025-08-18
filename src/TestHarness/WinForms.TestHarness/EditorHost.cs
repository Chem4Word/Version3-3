﻿// ---------------------------------------------------------------------------
//  Copyright (c) 2025, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.ACME;
using Chem4Word.Core;
using Chem4Word.Core.UI.Wpf;
using Chem4Word.Model2;
using Chem4Word.Model2.Converters.CML;
using Chem4Word.Model2.Helpers;
using Chem4Word.Telemetry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Forms;
using Chem4Word.ACME.Enums;
using Size = System.Drawing.Size;

namespace WinForms.TestHarness
{
    public partial class EditorHost : Form
    {
        public string OutputCml { get; set; }

        private RenderingOptions DefaultRenderingOptions { get; set; } = new RenderingOptions();

        private readonly string _editorType;

        public EditorHost(string cml, string type, int defaultBondLength)
        {
            InitializeComponent();
            _editorType = type;

            Model model;

            var used1D = SimulateGetUsed1DLabels(cml);

            MessageFromWpf.Text = "";

            var helper = new SystemHelper();
            var telemetry = new TelemetryWriter(true, true, helper);

            switch (_editorType)
            {
                case "ACME":
                    var acmeEditor = new Editor();
                    acmeEditor.InitializeComponent();
                    elementHost1.Child = acmeEditor;

                    // Configure Control
                    acmeEditor.ShowFeedback = false;
                    acmeEditor.TopLeft = new Point(Left, Top);
                    acmeEditor.Telemetry = telemetry;

                    var options = DefaultRenderingOptions.Copy();
                    options.DefaultBondLength = defaultBondLength;
                    acmeEditor.SetProperties(cml, used1D, options);
                    model = acmeEditor.ActiveController.Model;

                    if (model.Molecules.Count == 0)
                    {
                        Text = "ACME - New structure";
                    }
                    else
                    {
                        Text = "ACME - Editing " + FormulaHelper.FormulaPartsAsUnicode(FormulaHelper.ParseFormulaIntoParts(model.ConciseFormula));
                    }

                    acmeEditor.OnFeedbackChange += OnFeedbackChange_AcmeEditor;

                    break;

                case "LABELS":
                    var labelsEditor = new LabelsEditor();
                    labelsEditor.InitializeComponent();
                    elementHost1.Child = labelsEditor;

                    // Configure Control
                    labelsEditor.TopLeft = new Point(Left, Top);
                    labelsEditor.Used1D = used1D;
                    labelsEditor.PopulateTreeView(cml);

                    break;

                default:
                    var cmlEditor = new CmlEditor();
                    cmlEditor.InitializeComponent();
                    elementHost1.Child = cmlEditor;

                    // Configure Control
                    cmlEditor.Cml = cml;

                    break;
            }
        }

        private void OnFeedbackChange_AcmeEditor(object sender, WpfEventArgs e)
        {
            MessageFromWpf.Text = e.Message;

            var activeController = ((Editor)elementHost1.Child).ActiveController;
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

        private List<string> SimulateGetUsed1DLabels(string cml)
        {
            var used1D = new List<string>();

            if (!string.IsNullOrEmpty(cml))
            {
                var cc = new CMLConverter();
                var model = cc.Import(cml);

                foreach (var property in model.AllTextualProperties)
                {
                    if (property.FullType != null
                        && (property.FullType.Equals(CMLConstants.ValueChem4WordCaption)
                            || property.FullType.Equals(CMLConstants.ValueChem4WordFormula)
                            || property.FullType.Equals(CMLConstants.ValueChem4WordSynonym)))
                    {
                        used1D.Add($"{property.Id}:{model.CustomXmlPartGuid}");
                    }
                }
            }

            return used1D;
        }

        private void OnLoad_EditorHost(object sender, EventArgs e)
        {
            MinimumSize = new Size(900, 600);

            // Fix bottom panel
            var margin = StatusPanel.Height - Save.Bottom;
            VerticalSplitter.SplitterDistance = VerticalSplitter.Height - Save.Height - margin * 2;
            VerticalSplitter.FixedPanel = FixedPanel.Panel2;
            VerticalSplitter.IsSplitterFixed = true;

            switch (_editorType)
            {
                case "ACME":
                    if (elementHost1.Child is Editor acmeEditor)
                    {
                        acmeEditor.TopLeft = new Point(Location.X + Chem4Word.Core.Helpers.Constants.TopLeftOffset, Location.Y + Chem4Word.Core.Helpers.Constants.TopLeftOffset);
                    }
                    break;

                case "LABELS":
                    if (elementHost1.Child is LabelsEditor labelsEditor)
                    {
                        labelsEditor.TopLeft = new Point(Location.X + Chem4Word.Core.Helpers.Constants.TopLeftOffset, Location.Y + Chem4Word.Core.Helpers.Constants.TopLeftOffset);
                    }
                    break;

                default:
                    // Do Nothing
                    break;
            }
        }

        private void OnClick_Save(object sender, EventArgs e)
        {
            var cc = new CMLConverter();
            DialogResult = DialogResult.Cancel;

            switch (_editorType)
            {
                case "ACME":
                    if (elementHost1.Child is Editor acmeEditor
                        && acmeEditor.IsDirty)
                    {
                        DialogResult = DialogResult.OK;
                        var model = acmeEditor.EditedModel;
                        // Replace any temporary Ids which are Guids
                        model.ReLabelGuids();
                        OutputCml = cc.Export(model);
                    }
                    break;

                case "LABELS":
                    if (elementHost1.Child is LabelsEditor labelsEditor
                        && labelsEditor.IsDirty)
                    {
                        DialogResult = DialogResult.OK;
                        OutputCml = cc.Export(labelsEditor.EditedModel);
                    }
                    break;

                default:
                    if (elementHost1.Child is CmlEditor cmlEditor
                        && cmlEditor.IsDirty)
                    {
                        DialogResult = DialogResult.OK;
                        OutputCml = cc.Export(cmlEditor.EditedModel);
                    }
                    break;
            }
            Hide();
        }

        private void OnClick_Cancel(object sender, EventArgs e)
        {
            Hide();
        }

        private void OnFormClosing_EditorHost(object sender, FormClosingEventArgs e)
        {
            if (DialogResult != DialogResult.OK && e.CloseReason == CloseReason.UserClosing)
            {
                var sb = new StringBuilder();
                sb.AppendLine("Do you wish to save your changes?");
                sb.AppendLine("  Click 'Yes' to save your changes and exit.");
                sb.AppendLine("  Click 'No' to discard your changes and exit.");
                sb.AppendLine("  Click 'Cancel' to return to the form.");

                var cc = new CMLConverter();

                switch (_editorType)
                {
                    case "ACME":
                        if (elementHost1.Child is Editor acmeEditor
                            && acmeEditor.IsDirty)
                        {
                            var dr = UserInteractions.AskUserYesNoCancel(sb.ToString());
                            switch (dr)
                            {
                                case DialogResult.Cancel:
                                    e.Cancel = true;
                                    break;

                                case DialogResult.Yes:
                                    DialogResult = DialogResult.OK;
                                    var model = acmeEditor.EditedModel;
                                    // Replace any temporary Ids which are Guids
                                    model.ReLabelGuids();
                                    OutputCml = cc.Export(model);
                                    Hide();
                                    break;

                                case DialogResult.No:
                                    break;
                            }
                        }
                        break;

                    case "LABELS":
                        if (elementHost1.Child is LabelsEditor labelsEditor
                            && labelsEditor.IsDirty)
                        {
                            var dr = UserInteractions.AskUserYesNoCancel(sb.ToString());
                            switch (dr)
                            {
                                case DialogResult.Cancel:
                                    e.Cancel = true;
                                    break;

                                case DialogResult.Yes:
                                    DialogResult = DialogResult.OK;
                                    OutputCml = cc.Export(labelsEditor.EditedModel);
                                    Hide();
                                    break;

                                case DialogResult.No:
                                    break;
                            }
                        }
                        break;

                    default:
                        if (elementHost1.Child is CmlEditor editor
                            && editor.IsDirty)
                        {
                            var dr = UserInteractions.AskUserYesNoCancel(sb.ToString());
                            switch (dr)
                            {
                                case DialogResult.Cancel:
                                    e.Cancel = true;
                                    break;

                                case DialogResult.Yes:
                                    DialogResult = DialogResult.OK;
                                    OutputCml = cc.Export(editor.EditedModel);
                                    Hide();
                                    break;

                                case DialogResult.No:
                                    break;
                            }
                        }
                        break;
                }
            }
        }
    }
}