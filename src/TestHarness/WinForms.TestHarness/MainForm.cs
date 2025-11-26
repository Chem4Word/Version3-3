// ---------------------------------------------------------------------------
//  Copyright (c) 2025, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.ACME;
using Chem4Word.Core.Helpers;
using Chem4Word.Model2;
using Chem4Word.Model2.Converters.CML;
using Chem4Word.Model2.Converters.JSON;
using Chem4Word.Model2.Converters.MDL;
using Chem4Word.Model2.Converters.ProtocolBuffers;
using Chem4Word.Model2.Converters.SketchEl;
using Chem4Word.Model2.Formula;
using Chem4Word.Renderer.OoXmlV4;
using Chem4Word.Searcher.ChEBIPlugin;
using Chem4Word.Searcher.OpsinPlugIn;
using Chem4Word.Searcher.PubChemPlugIn;
using Chem4Word.Shared;
using Chem4Word.Telemetry;
using Chem4Word.WebServices;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media;
using Color = System.Drawing.Color;
using MessageBox = System.Windows.Forms.MessageBox;

namespace WinForms.TestHarness
{
    public partial class MainForm : Form
    {
        private const int DefaultBondLength = 25;

        private Stack<Model> _undoStack = new Stack<Model>();
        private Stack<Model> _redoStack = new Stack<Model>();

        private SystemHelper _helper;
        private TelemetryWriter _telemetry;

        private static string _product = Assembly.GetExecutingAssembly().FullName.Split(',')[0];
        private static string _class = MethodBase.GetCurrentMethod().DeclaringType?.Name;

        private string _lastCml = string.Empty;

        private OoXmlV4Options _renderOptions;
        private ConfigWatcher _configWatcher;

        public MainForm()
        {
            InitializeComponent();

            _helper = new SystemHelper();
            _telemetry = new TelemetryWriter(true, true, _helper);

            var location = Assembly.GetExecutingAssembly().Location;
            var path = Path.GetDirectoryName(location);

            // Use either path or null below
            _renderOptions = new OoXmlV4Options(null);
        }

        private void OnClick_LoadStructure(object sender, EventArgs e)
        {
            var module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                Model model = null;

                var sb = new StringBuilder();
                sb.Append("All molecule files (*.cml, *.xml, *.mol, *.sdf, *.json, *.pbuff, *.el)|*.cml;*.xml;*.mol;*.sdf;*.json;*.pbuff;*.el");
                sb.Append("|CML molecule files (*.cml, *.xml)|*.cml;*.xml");
                sb.Append("|MDL molecule files (*.mol, *.sdf)|*.mol;*.sdf");
                sb.Append("|ChemDoodle Web json files (*.json)|*.json");
                sb.Append("|Protocol Buffer (*.pbuff)|*.pbuff");
                sb.Append("|SketchEl molecule files (*.el)|*.el");

                openFileDialog1.Title = "Open Structure";
                openFileDialog1.InitialDirectory = Environment.SpecialFolder.MyDocuments.ToString();
                openFileDialog1.Filter = sb.ToString();
                openFileDialog1.FileName = "";
                openFileDialog1.ShowHelp = false;

                var dr = openFileDialog1.ShowDialog();

                if (dr == DialogResult.OK)
                {
                    var fileType = Path.GetExtension(openFileDialog1.FileName).ToLower();
                    var filename = Path.GetFileName(openFileDialog1.FileName);
                    var mol = File.ReadAllText(openFileDialog1.FileName);

                    var cmlConverter = new CMLConverter();
                    var fileConverter = new SdFileConverter();

                    Stopwatch stopwatch;
                    TimeSpan elapsed1 = default;
                    TimeSpan elapsed2;

                    switch (fileType)
                    {
                        case ".mol":
                        case ".sdf":
                            stopwatch = new Stopwatch();
                            stopwatch.Start();
                            model = fileConverter.Import(mol);
                            stopwatch.Stop();
                            elapsed1 = stopwatch.Elapsed;
                            break;

                        case ".cml":
                        case ".xml":
                            stopwatch = new Stopwatch();
                            stopwatch.Start();
                            model = cmlConverter.Import(mol);
                            stopwatch.Stop();
                            elapsed1 = stopwatch.Elapsed;
                            break;

                        case ".json":
                            stopwatch = new Stopwatch();
                            stopwatch.Start();
                            var jsonConvertor = new JSONConverter();
                            model = jsonConvertor.Import(mol);
                            stopwatch.Stop();
                            elapsed1 = stopwatch.Elapsed;
                            break;

                        case ".el":
                            stopwatch = new Stopwatch();
                            stopwatch.Start();
                            var sketchElConverter = new SketchElConverter();
                            model = sketchElConverter.Import(mol);
                            stopwatch.Stop();
                            elapsed1 = stopwatch.Elapsed;
                            break;

                        case ".pbuff":
                            var protocolBufferConverter = new ProtocolBufferConverter();

                            stopwatch = new Stopwatch();
                            stopwatch.Start();

                            var inputBytes = File.ReadAllBytes(openFileDialog1.FileName);
                            model = protocolBufferConverter.Import(inputBytes);

                            stopwatch.Stop();
                            elapsed1 = stopwatch.Elapsed;
                            break;
                    }

                    if (model != null)
                    {
                        if (model.AllWarnings.Count > 0)
                        {
                            _telemetry.Write(module, "Warnings", string.Join(Environment.NewLine, model.AllWarnings));
                            MessageBox.Show(string.Join(Environment.NewLine, model.AllWarnings), "Model has warnings!");
                        }

                        if (model.AllErrors.Count == 0)
                        {
                            var originalBondLength = model.MeanBondLength;
                            model.EnsureBondLength(DefaultBondLength, false);

                            if (string.IsNullOrEmpty(model.CustomXmlPartGuid))
                            {
                                model.CustomXmlPartGuid = Guid.NewGuid().ToString("N");
                            }

                            if (!string.IsNullOrEmpty(_lastCml))
                            {
                                var clone = cmlConverter.Import(_lastCml);
                                Debug.WriteLine($"Pushing F: {clone.ConciseFormula} BL: {clone.MeanBondLength:#,##0.00} onto Stack");
                                _undoStack.Push(clone);
                            }

                            stopwatch = new Stopwatch();
                            stopwatch.Start();
                            _lastCml = cmlConverter.Export(model);
                            stopwatch.Stop();
                            elapsed2 = stopwatch.Elapsed;

                            _telemetry.Write(module, "Information", $"File: '{filename}'; Original bond length {originalBondLength:#,##0.00}");
                            _telemetry.Write(module, "Timing", $"Import took {elapsed1}; Export took {elapsed2}");
                            ShowChemistry(filename, model);
                        }
                        else
                        {
                            _telemetry.Write(module, "Exception(Data)", string.Join(Environment.NewLine, model.AllErrors));
                            MessageBox.Show(string.Join(Environment.NewLine, model.AllErrors), "Model has Errors!");
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                _telemetry.Write(module, "Exception", $"Exception: {exception.Message}");
                _telemetry.Write(module, "Exception(Data)", $"Exception: {exception}");
                MessageBox.Show(exception.StackTrace, exception.Message);
            }
        }

        #region Disconnected Code - Please Keep for reference

        private void OnClick_ChangeBackground(object sender, EventArgs e)
        {
            var dr = colorDialog1.ShowDialog();
            if (dr == DialogResult.OK)
            {
                DisplayHost.BackColor = colorDialog1.Color;
            }
        }

        private Brush ColorToBrush(Color colour)
        {
            var hex = $"#{colour.A:X2}{colour.R:X2}{colour.G:X2}{colour.B:X2}";
            var converter = new BrushConverter();
            return (Brush)converter.ConvertFromString(hex);
        }

        private void OnCheckedChanged_ShowCarbons(object sender, EventArgs e)
        {
            var module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                if (Display.Chemistry is Model model)
                {
                    var copy = model.Copy();
                    copy.Refresh();
                    Debug.WriteLine($"Old Model: ({model.MinX}, {model.MinY}):({model.MaxX}, {model.MaxY})");
                    Debug.WriteLine($"New Model: ({copy.MinX}, {copy.MinY}):({copy.MaxX}, {copy.MaxY})");
                    Display.Chemistry = copy;
                }
            }
            catch (Exception exception)
            {
                _telemetry.Write(module, "Exception", $"Exception: {exception.Message}");
                _telemetry.Write(module, "Exception(Data)", $"Exception: {exception}");
                MessageBox.Show(exception.StackTrace, exception.Message);
            }
        }

        private void OnClick_RemoveAtom(object sender, EventArgs e)
        {
            var module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                if (Display.Chemistry is Model model)
                {
                    var allAtoms = model.GetAllAtoms();
                    if (model.GetAllAtoms().Any())
                    {
                        var modelMolecule =
                            model.GetAllMolecules().FirstOrDefault(m => allAtoms.Any() && m.Atoms.Count > 0);
                        var atom = modelMolecule.Atoms.Values.First();
                        var bondList = atom.Bonds.ToList();
                        foreach (var neighbouringBond in bondList)
                        {
                            modelMolecule.RemoveBond(neighbouringBond);
                            neighbouringBond.OtherAtom(atom).UpdateVisual();
                            foreach (var bond in neighbouringBond.OtherAtom(atom).Bonds)
                            {
                                bond.UpdateVisual();
                            }
                        }

                        modelMolecule.RemoveAtom(atom);
                    }

                    model.Refresh();
                    Information1.Text = $"Formula: {model.ConciseFormula} ({model.UnicodeFormula})";
                    Information2.Text = $"BondLength: {model.MeanBondLength.ToString("#,##0.00")}";
                }
            }
            catch (Exception exception)
            {
                _telemetry.Write(module, "Exception", $"Exception: {exception.Message}");
                _telemetry.Write(module, "Exception(Data)", $"Exception: {exception}");
                MessageBox.Show(exception.StackTrace, exception.Message);
            }
        }

        private void OnClick_RandomElement(object sender, EventArgs e)
        {
            var module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                if (Display.Chemistry is Model model)
                {
                    var allAtoms = model.GetAllAtoms();
                    if (allAtoms.Any())
                    {
                        var rnd = new Random(DateTime.UtcNow.Millisecond);

                        var maxAtoms = allAtoms.Count;
                        var targetAtom = rnd.Next(0, maxAtoms);

                        var elements = ModelGlobals.PeriodicTable.Elements;
                        var newElement = rnd.Next(0, elements.Values.Max(v => v.AtomicNumber));
                        var x = elements.Values.FirstOrDefault(v => v.AtomicNumber == newElement);

                        if (x == null)
                        {
                            Debugger.Break();
                        }

                        allAtoms[targetAtom].Element = x;
                        if (x.Symbol.Equals("C"))
                        {
                            //allAtoms[targetAtom].ShowSymbol = ExplicitC.Checked
                        }

                        allAtoms[targetAtom].UpdateVisual();

                        foreach (var b in allAtoms[targetAtom].Bonds)
                        {
                            b.UpdateVisual();
                        }

                        model.Refresh();
                        Information1.Text = $"Formula: {model.ConciseFormula} ({model.UnicodeFormula})";
                        Information2.Text = $"BondLength: {model.MeanBondLength.ToString("#,##0.00")}";
                    }
                }
            }
            catch (Exception exception)
            {
                _telemetry.Write(module, "Exception", $"Exception: {exception.Message}");
                _telemetry.Write(module, "Exception(Data)", $"Exception: {exception}");
                MessageBox.Show(exception.StackTrace, exception.Message);
            }
        }

        #endregion Disconnected Code - Please Keep for reference

        private void OnClick_EditLabels(object sender, EventArgs e)
        {
#if !DEBUG
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
#endif

            if (!string.IsNullOrEmpty(_lastCml))
            {
                using (var editorHost = new EditorHost(_lastCml, "LABELS", DefaultBondLength))
                {
                    editorHost.ShowDialog(this);
                    if (editorHost.DialogResult == DialogResult.OK)
                    {
                        HandleChangedCml(editorHost.OutputCml, "Labels Editor result");
                    }
                }
                TopMost = true;
                TopMost = false;
                Activate();
            }
#if !DEBUG
            }
            catch (Exception exception)
            {
                _telemetry.Write(module, "Exception", $"Exception: {exception.Message}");
                _telemetry.Write(module, "Exception(Data)", $"Exception: {exception}");
                MessageBox.Show(exception.StackTrace, exception.Message);
            }
#endif
        }

        private void OnClick_EditWithAcme(object sender, EventArgs e)
        {
#if !DEBUG
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
#endif
            using (var editorHost = new EditorHost(_lastCml, "ACME", DefaultBondLength))
            {
                editorHost.ShowDialog(this);
                if (editorHost.DialogResult == DialogResult.OK)
                {
                    HandleChangedCml(editorHost.OutputCml, "ACME result");
                }
            }
            TopMost = true;
            TopMost = false;
            Activate();
#if !DEBUG
            }
            catch (Exception exception)
            {
                _telemetry.Write(module, "Exception", $"Exception: {exception.Message}");
                _telemetry.Write(module, "Exception(Data)", $"Exception: {exception}");
                MessageBox.Show(exception.StackTrace, exception.Message);
            }
#endif
        }

        private void ShowChemistry(string filename, Model model)
        {
            Display.Clear();

            if (model != null)
            {
                if (model.AllErrors.Any())
                {
                    var lines = new List<string>();

                    if (model.AllErrors.Any())
                    {
                        lines.Add("All Error(s)");
                        lines.AddRange(model.AllErrors);
                    }

                    MessageBox.Show(string.Join(Environment.NewLine, lines));
                }
                else
                {
                    if (!string.IsNullOrEmpty(filename))
                    {
                        Text = filename;
                    }

                    var statistics1 = model.GetBondLengthStatistics();
                    var statistics2 = model.GetBondLengthStatistics(false);

                    Information1.Text = $"Formula: {model.ConciseFormula} ({model.UnicodeFormula})";

                    var stringBuilder = new StringBuilder();

                    stringBuilder.Append($"BL+H: Mean {SafeDouble.AsString(statistics1.Mean)} ");
                    stringBuilder.Append($"Mode {SafeDouble.AsString(statistics1.Mode)} ");
                    stringBuilder.Append($"Median {SafeDouble.AsString(statistics1.Median)} ");

                    stringBuilder.Append($"BL-H: Mean {SafeDouble.AsString(statistics2.Mean)} ");
                    stringBuilder.Append($"Mode {SafeDouble.AsString(statistics2.Mode)} ");
                    stringBuilder.Append($"Median {SafeDouble.AsString(statistics2.Median)} ");

                    Information2.Text = stringBuilder.ToString();

                    Display.Chemistry = model;

                    Debug.WriteLine($"FlexForm is displaying {model.ConciseFormula}");

                    EnableNormalButtons();
                    EnableUndoRedoButtonsAndShowStacks();
                }
            }
        }

        private void EnableNormalButtons()
        {
            EditWithAcme.Enabled = true;
            EditLabels.Enabled = true;
            EditCml.Enabled = true;
            CalculateProperties.Enabled = true;

            ShowCml.Enabled = true;
            ClearChemistry.Enabled = true;
            SaveStructure.Enabled = true;
            LayoutStructure.Enabled = true;
            RenderOoXml.Enabled = true;

            var renderer = new Renderer();
            ChangeOoXmlSettings.Enabled = renderer.HasSettings;

            ListStacks();
        }

        private List<Controller> StackToList(Stack<Model> stack)
        {
            var list = new List<Controller>();
            foreach (var item in stack)
            {
                var model = item.Copy();
                list.Add(new Controller(model));
            }

            return list;
        }

        private void EnableUndoRedoButtonsAndShowStacks()
        {
            Redo.Enabled = _redoStack.Count > 0;
            Undo.Enabled = _undoStack.Count > 0;
            UndoStack.ListOfDisplays.ItemsSource = StackToList(_undoStack);
            RedoStack.ListOfDisplays.ItemsSource = StackToList(_redoStack);
        }

        private void OnClick_Undo(object sender, EventArgs e)
        {
            var module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                var model = _undoStack.Pop();
                Debug.WriteLine(
                    $"Popped F: {model.ConciseFormula} BL: {SafeDouble.AsString0(model.MeanBondLength)} from Undo Stack");

                if (!string.IsNullOrEmpty(_lastCml))
                {
                    var cc = new CMLConverter();
                    var copy = cc.Import(_lastCml);
                    _lastCml = cc.Export(model);

                    Debug.WriteLine(
                        $"Pushing F: {copy.ConciseFormula} BL: {SafeDouble.AsString0(copy.MeanBondLength)} onto Redo Stack");
                    _redoStack.Push(copy);
                }

                SetDisplayOptions();
                var helper = new FormulaHelperV2(model);
                ShowChemistry($"Undo -> {helper.Concise()}", model);
            }
            catch (Exception exception)
            {
                _telemetry.Write(module, "Exception", $"Exception: {exception.Message}");
                _telemetry.Write(module, "Exception(Data)", $"Exception: {exception}");
                MessageBox.Show(exception.StackTrace, exception.Message);
            }
        }

        private void OnClick_Redo(object sender, EventArgs e)
        {
            var module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                var model = _redoStack.Pop();
                Debug.WriteLine(
                    $"Popped F: {model.ConciseFormula} BL: {SafeDouble.AsString0(model.MeanBondLength)} from Redo Stack");

                if (!string.IsNullOrEmpty(_lastCml))
                {
                    var cc = new CMLConverter();
                    var clone = cc.Import(_lastCml);
                    _lastCml = cc.Export(model);

                    Debug.WriteLine(
                        $"Pushing F: {clone.ConciseFormula} BL: {SafeDouble.AsString0(clone.MeanBondLength)} onto Undo Stack");
                    _undoStack.Push(clone);
                }

                SetDisplayOptions();
                var helper = new FormulaHelperV2(model);
                ShowChemistry($"Redo -> {helper.Concise()}", model);
            }
            catch (Exception exception)
            {
                _telemetry.Write(module, "Exception", $"Exception: {exception.Message}");
                _telemetry.Write(module, "Exception(Data)", $"Exception: {exception}");
                MessageBox.Show(exception.StackTrace, exception.Message);
            }
        }

        private void ListStacks()
        {
            if (_undoStack.Any())
            {
                Debug.WriteLine("Undo Stack");
                foreach (var model in _undoStack)
                {
                    Debug.WriteLine(
                        $"{model.ConciseFormula} [{model.GetHashCode()}] {model.MeanBondLength.ToString("#,##0.00")}");
                }
            }

            if (_redoStack.Any())
            {
                Debug.WriteLine("Redo Stack");
                foreach (var model in _redoStack)
                {
                    Debug.WriteLine(
                        $"{model.ConciseFormula} [{model.GetHashCode()}] {model.MeanBondLength.ToString("#,##0.00")}");
                }
            }
        }

        private void OnClick_EditCml(object sender, EventArgs e)
        {
            var module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            try
            {
                if (!string.IsNullOrEmpty(_lastCml))
                {
                    using (var editorHost = new EditorHost(_lastCml, "CML", DefaultBondLength))
                    {
                        editorHost.ShowDialog(this);
                        if (editorHost.DialogResult == DialogResult.OK)
                        {
                            HandleChangedCml(editorHost.OutputCml, "CML Editor result");
                        }
                    }
                    TopMost = true;
                    TopMost = false;
                    Activate();
                }
            }
            catch (Exception exception)
            {
                _telemetry.Write(module, "Exception", $"Exception: {exception.Message}");
                _telemetry.Write(module, "Exception(Data)", $"Exception: {exception}");
                MessageBox.Show(exception.StackTrace, exception.Message);
            }
        }

        private void OnClick_ShowCml(object sender, EventArgs e)
        {
            var module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                if (!string.IsNullOrEmpty(_lastCml))
                {
                    using (var f = new ShowCml { Cml = _lastCml })
                    {
                        f.ShowDialog(this);
                    }
                }
            }
            catch (Exception exception)
            {
                _telemetry.Write(module, "Exception", $"Exception: {exception.Message}");
                _telemetry.Write(module, "Exception(Data)", $"Exception: {exception}");
                MessageBox.Show(exception.StackTrace, exception.Message);
            }
        }

        private void SaveStructure_Click(object sender, EventArgs e)
        {
            var module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                var cmlConverter = new CMLConverter();
                var m = cmlConverter.Import(_lastCml);
                m.CustomXmlPartGuid = "";

                var sb = new StringBuilder();
                sb.Append("CML molecule files (*.cml, *.xml)|*.cml;*.xml");
                sb.Append("|MDL molecule files (*.mol, *.sdf)|*.mol;*.sdf");
                sb.Append("|ChemDoodle Web json files (*.json)|*.json");
                sb.Append("|Protocol Buffers (*.pbuff)|*.pbuff");
                sb.Append("|SketchEl (*.el)|*.el");

                using (var sfd = new SaveFileDialog { Filter = sb.ToString() })
                {
                    sfd.AddExtension = true;
                    var dr = sfd.ShowDialog();
                    if (dr == DialogResult.OK)
                    {
                        var fi = new FileInfo(sfd.FileName);
                        _telemetry.Write(module, "Information", $"Exporting to '{fi.Name}'");
                        var fileType = Path.GetExtension(sfd.FileName).ToLower();
                        switch (fileType)
                        {
                            case ".cml":
                            case ".xml":
                                File.WriteAllText(sfd.FileName, XmlHelper.AddHeader(cmlConverter.Export(m)));
                                break;

                            case ".mol":
                            case ".sdf":
                                // https://www.chemaxon.com/marvin-archive/6.0.2/marvin/help/formats/mol-csmol-doc.html
                                var before = m.MeanBondLength;
                                // Set bond length to 1.54 angstroms (Å)
                                m.ScaleToAverageBondLength(1.54);
                                var after = m.MeanBondLength;
                                _telemetry.Write(module, "Information", $"Structure rescaled from {before.ToString("#0.00")} to {after.ToString("#0.00")}");
                                var sdFileConverter = new SdFileConverter();
                                File.WriteAllText(sfd.FileName, sdFileConverter.Export(m));
                                break;

                            case ".el":
                                var sketchElConverter = new SketchElConverter();
                                File.WriteAllText(sfd.FileName, sketchElConverter.Export(m));
                                break;

                            case ".json":
                                var jsonConverter = new JSONConverter();
                                File.WriteAllText(sfd.FileName, jsonConverter.Export(m));
                                break;

                            case ".pbuff":
                                WritePBuffFile(sfd.FileName, m);
                                break;
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                _telemetry.Write(module, "Exception", $"Exception: {exception.Message}");
                _telemetry.Write(module, "Exception(Data)", $"Exception: {exception}");
                MessageBox.Show(exception.StackTrace, exception.Message);
            }
        }

        private void WritePBuffFile(string sfdFileName, Model model)
        {
            var pbc = new ProtocolBufferConverter();
            var bytes = pbc.Export(model);
            File.WriteAllBytes(sfdFileName, bytes);
        }

        private void OnClick_ClearChemistry(object sender, EventArgs e)
        {
            var cc = new CMLConverter();
            _undoStack.Push(cc.Import(_lastCml));
            _lastCml = string.Empty;

            Display.Clear();
            EnableUndoRedoButtonsAndShowStacks();
        }

        private void OnLoad_FlexForm(object sender, EventArgs e)
        {
            SetDisplayOptions();
            Display.HighlightActive = false;

            RedoStack = new StackViewer();
            RedoHost.Child = RedoStack;
            UndoStack = new StackViewer();
            UndoHost.Child = UndoStack;

            // ToDo: [MAW] Check if we still need the config watcher
            var location = Assembly.GetExecutingAssembly().Location;
            var path = Path.GetDirectoryName(location);
            _configWatcher = new ConfigWatcher(path);
        }

        private void OptionsChanged()
        {
            // Allow time for FileSystemWatcher to fire
            Thread.Sleep(250);

            _renderOptions = new OoXmlV4Options(null);
            UpdateControls();
        }

        private void OnClick_ChangeOoXmlSettings(object sender, EventArgs e)
        {
            var settings = new OoXmlV4Settings();
            settings.Telemetry = _telemetry;
            settings.TopLeft = new Point(Left + 24, Top + 24);

            var tempOptions = _renderOptions.Clone();
            settings.RendererOptions = tempOptions;

            var dr = settings.ShowDialog();
            if (dr == DialogResult.OK)
            {
                _renderOptions = tempOptions.Clone();
                OptionsChanged();
            }

            settings.Close();
        }

        private void UpdateControls()
        {
            SetDisplayOptions();
            Display.Chemistry = _lastCml;
            //RedoStack.SetOptions(_editorOptions);
            //UndoStack.SetOptions(_editorOptions);
            UndoStack.ListOfDisplays.ItemsSource = StackToList(_undoStack);
            RedoStack.ListOfDisplays.ItemsSource = StackToList(_redoStack);
        }

        private void SetDisplayOptions()
        {
            // Remove
        }

        private void OnClick_LayoutStructure(object sender, EventArgs e)
        {
            LayoutUsingCheblClean();
        }

        private void LayoutUsingCheblClean()
        {
            var module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            var cc = new CMLConverter();
            var model = cc.Import(_lastCml);

            if (model.TotalMoleculesCount == 1
                && !model.HasNestedMolecules
                && !model.HasFunctionalGroups)
            {
                var bondLength = model.MeanBondLength;
                var marvin = cc.Export(model, true, CmlFormat.MarvinJs);

                // Replace double quote with single quote
                marvin = marvin.Replace("\"", "'");

                try
                {
                    using (var httpClient = new HttpClient())
                    {
                        httpClient.Timeout = TimeSpan.FromSeconds(15);
                        httpClient.DefaultRequestHeaders.Add("user-agent", "Chem4Word");

                        try
                        {
                            var request = new HttpRequestMessage(HttpMethod.Post, "https://www.ebi.ac.uk/chembl/api/utils/clean");
                            request.Headers.Add("User-Agent", "Chem4Word");

                            var body = JsonConvert.SerializeObject(new { structure = $"{marvin}", parameters = new { dim = 2, opts = "s" } });
                            request.Content = new StringContent(body, Encoding.UTF8, "text/plain");

                            var response = httpClient.SendAsync(request).Result;

                            if (!response.IsSuccessStatusCode)
                            {
                                // Handle Error
                                Debug.WriteLine($"{response.StatusCode} - {response.RequestMessage}");
                            }

                            var answer = response.Content.ReadAsStringAsync();
                            Debug.WriteLine(answer.Result);

                            model = cc.Import(answer.Result);
                            model.EnsureBondLength(bondLength, false);
                            if (string.IsNullOrEmpty(model.CustomXmlPartGuid))
                            {
                                model.CustomXmlPartGuid = Guid.NewGuid().ToString("N");
                            }

                            var clone = cc.Import(_lastCml);
                            _undoStack.Push(clone);

                            _lastCml = cc.Export(model);
                            ShowChemistry("ChEMBL clean", model);
                        }
                        catch (Exception innerException)
                        {
                            _telemetry.Write(module, "Exception", innerException.Message);
                            _telemetry.Write(module, "Exception", innerException.ToString());
                            Debug.WriteLine(innerException.Message);
                        }
                    }
                }
                catch (Exception exception)
                {
                    _telemetry.Write(module, "Exception", exception.Message);
                    _telemetry.Write(module, "Exception", exception.ToString());
                    Debug.WriteLine(exception.Message);
                }
            }
            else
            {
                MessageBox.Show("Clean only handles single molecules without any functional groups (at the moment)", "Test Harness");
            }
        }

        private void OnClick_RenderOoXml(object sender, EventArgs e)
        {
            var renderer = new Renderer
            {
                Telemetry = _telemetry,
                TopLeft = new Point(Left + 24, Top + 24),
                Cml = _lastCml,
                Properties = new Dictionary<string, string>
                                            {
                                                {
                                                    "Guid", Guid.NewGuid().ToString("N")
                                                }
                                            }
            };
            var tempFileName = renderer.Render();
            if (string.IsNullOrEmpty(tempFileName))
            {
                MessageBox.Show("Something went wrong!", "Error");
            }
            else
            {
                Debug.WriteLine($"File {tempFileName} was created, opening it in Word");
                // Start word in quiet mode [/q] without any add ins loaded [/a]
                Process.Start(OfficeHelper.GetWinWordPath(), $" {tempFileName}");
            }
        }

        private void OnClick_SearchChEBI(object sender, EventArgs e)
        {
            using (var searcher = new SearchChEBI())
            {
                searcher.Telemetry = _telemetry;
                searcher.UserOptions = new ChEBIOptions();
                searcher.TopLeft = new Point(Left + 24, Top + 24);

                var result = searcher.ShowDialog(this);
                if (result == DialogResult.OK)
                {
                    HandleChangedCml(searcher.Cml, "ChEBI Search result");
                }
            }

            TopMost = true;
            TopMost = false;
            Activate();
        }

        private void OnClick_SearchPubChem(object sender, EventArgs e)
        {
            using (var searcher = new SearchPubChem())
            {
                searcher.Telemetry = _telemetry;
                searcher.UserOptions = new PubChemOptions();
                searcher.TopLeft = new Point(Left + 24, Top + 24);

                var result = searcher.ShowDialog(this);
                if (result == DialogResult.OK)
                {
                    HandleChangedCml(searcher.Cml, "PubChem Search result");
                }
            }

            TopMost = true;
            TopMost = false;
            Activate();
        }

        private void OnClick_SearchOpsin(object sender, EventArgs e)
        {
            using (var searcher = new SearchOpsin())
            {
                searcher.Telemetry = _telemetry;
                searcher.UserOptions = new SearcherOptions();
                searcher.TopLeft = new Point(Left + 24, Top + 24);

                var result = searcher.ShowDialog(this);
                if (result == DialogResult.OK)
                {
                    HandleChangedCml(searcher.Cml, "ChEBI Search result");
                }
            }

            TopMost = true;
            TopMost = false;
            Activate();
        }

        private void HandleChangedCml(string cml, string captionPrefix)
        {
            var cc = new CMLConverter();
            if (!string.IsNullOrEmpty(_lastCml))
            {
                var clone = cc.Import(_lastCml);
                Debug.WriteLine(
                    $"Pushing F: {clone.ConciseFormula} BL: {SafeDouble.AsString0(clone.MeanBondLength)} onto Stack");
                _undoStack.Push(clone);
            }

            var model = cc.Import(cml);
            if (model.AllErrors.Count == 0 && model.AllWarnings.Count == 0)
            {
                model.Relabel(true);
                model.EnsureBondLength(DefaultBondLength, false);
                _lastCml = cc.Export(model);

                SetDisplayOptions();

                var helper = new FormulaHelperV2(model);
                ShowChemistry($"{captionPrefix} {helper.Concise()}", model);
            }
            else
            {
                var errors = model.AllWarnings;
                errors.AddRange(model.AllErrors);

                MessageBox.Show(string.Join(Environment.NewLine, errors), "Model has errors or warnings!");
            }
        }

        private void OnClick_CalculateProperties(object sender, EventArgs e)
        {
            var cc = new CMLConverter();
            if (!string.IsNullOrEmpty(_lastCml))
            {
                var clone = cc.Import(_lastCml);
                Debug.WriteLine(
                    $"Pushing F: {clone.ConciseFormula} BL: {clone.MeanBondLength.ToString("#,##0.00")} onto Stack");
                _undoStack.Push(clone);
            }

            var model = cc.Import(_lastCml);

            var assembly = Assembly.GetExecutingAssembly();
            var version = assembly.GetName().Version;
            var pc = new PropertyCalculator(_telemetry, new Point(Left, Top), version.ToString());

            var stopwatch = new Stopwatch();
            stopwatch.Start();

            model.CreatorGuid = $"TH:{Guid.NewGuid():N}";
            var changedProperties = pc.CalculateProperties(model);

            stopwatch.Stop();
            Debug.WriteLine($"Calculating {changedProperties} changed properties took {stopwatch.Elapsed}");

            _lastCml = cc.Export(model);

            SetDisplayOptions();

            var helper = new FormulaHelperV2(model);
            ShowChemistry($"{changedProperties} changed properties; {helper.Concise()}", model);
        }
    }
}
