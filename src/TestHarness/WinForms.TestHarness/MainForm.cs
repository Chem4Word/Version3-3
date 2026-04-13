// ---------------------------------------------------------------------------
//  Copyright (c) 2026, The .NET Foundation.
//  This software is released under the Apache Licence, Version 2.0.
//  The licence and further copyright text can be found in the file LICENCE.md
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
using System.Threading.Tasks;
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

        private Model _currentModel;

        private OoXmlV4Options _renderOptions;
        private ConfigWatcher _configWatcher;

        public MainForm()
        {
            InitializeComponent();

            SetButtonStates(FormState.Disabled);

            _helper = new SystemHelper();
            _telemetry = new TelemetryWriter(true, true, _helper);
            timer1.Enabled = true;

            string location = Assembly.GetExecutingAssembly().Location;
            string path = Path.GetDirectoryName(location);

            // Use either path or null below
            _renderOptions = new OoXmlV4Options(null);
        }

        private void SetButtonStates(FormState state)
        {
            switch (state)
            {
                case FormState.Disabled:
                    LoadStructure.Enabled = false;
                    EditWithAcme.Enabled = false;
                    Undo.Enabled = false;
                    Redo.Enabled = false;
                    EditCml.Enabled = false;
                    ShowCml.Enabled = false;
                    SaveStructure.Enabled = false;
                    ClearChemistry.Enabled = false;
                    EditLabels.Enabled = false;
                    LayoutStructure.Enabled = false;
                    RenderOoXml.Enabled = false;
                    ChangeOoXmlSettings.Enabled = false;
                    SearchChEBI.Enabled = false;
                    SearchOpsin.Enabled = false;
                    SearchPubChem.Enabled = false;
                    CalculateProperties.Enabled = false;
                    break;

                case FormState.OpenOrCreate:
                    LoadStructure.Enabled = true;
                    EditWithAcme.Enabled = true;
                    SearchChEBI.Enabled = true;
                    SearchOpsin.Enabled = true;
                    SearchPubChem.Enabled = true;
                    break;

                case FormState.Edit:
                    EnableNormalButtons();
                    EnableUndoRedoButtonsAndShowStacks();
                    break;
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

            Renderer renderer = new Renderer();
            ChangeOoXmlSettings.Enabled = renderer.HasSettings;

            ListStacks();
        }

        private void EnableUndoRedoButtonsAndShowStacks()
        {
            Redo.Enabled = _redoStack.Count > 0;
            Undo.Enabled = _undoStack.Count > 0;

            UndoStackViewer.Reload(_undoStack);
            RedoStackViewer.Reload(_redoStack);
        }

        private void OnClick_LoadStructure(object sender, EventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                Model model = null;

                StringBuilder sb = new StringBuilder();
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

                DialogResult dr = openFileDialog1.ShowDialog();

                if (dr == DialogResult.OK)
                {
                    string fileType = Path.GetExtension(openFileDialog1.FileName).ToLower();
                    string filename = Path.GetFileName(openFileDialog1.FileName);
                    string mol = File.ReadAllText(openFileDialog1.FileName);

                    CMLConverter cmlConverter = new CMLConverter();
                    SdFileConverter fileConverter = new SdFileConverter();

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
                            JSONConverter jsonConvertor = new JSONConverter();
                            model = jsonConvertor.Import(mol);
                            stopwatch.Stop();
                            elapsed1 = stopwatch.Elapsed;
                            break;

                        case ".el":
                            stopwatch = new Stopwatch();
                            stopwatch.Start();
                            SketchElConverter sketchElConverter = new SketchElConverter();
                            model = sketchElConverter.Import(mol);
                            stopwatch.Stop();
                            elapsed1 = stopwatch.Elapsed;
                            break;

                        case ".pbuff":
                            ProtocolBufferConverter protocolBufferConverter = new ProtocolBufferConverter();

                            stopwatch = new Stopwatch();
                            stopwatch.Start();

                            byte[] inputBytes = File.ReadAllBytes(openFileDialog1.FileName);
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
                            double originalBondLength = model.MeanBondLength;
                            model.EnsureBondLength(DefaultBondLength, false);

                            if (string.IsNullOrEmpty(model.CustomXmlPartGuid))
                            {
                                model.CustomXmlPartGuid = Guid.NewGuid().ToString("N");
                            }

                            if (_currentModel != null)
                            {
                                _undoStack.Push(_currentModel);
                            }

                            stopwatch = new Stopwatch();
                            stopwatch.Start();
                            _currentModel = model;
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
            DialogResult dr = colorDialog1.ShowDialog();
            if (dr == DialogResult.OK)
            {
                DisplayHost.BackColor = colorDialog1.Color;
            }
        }

        private Brush ColorToBrush(Color colour)
        {
            string hex = $"#{colour.A:X2}{colour.R:X2}{colour.G:X2}{colour.B:X2}";
            BrushConverter converter = new BrushConverter();
            return (Brush)converter.ConvertFromString(hex);
        }

        #endregion Disconnected Code - Please Keep for reference

        private void OnClick_EditLabels(object sender, EventArgs e)
        {
#if !DEBUG
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
#endif

            if (_currentModel != null)
            {
                CMLConverter cmlConverter = new CMLConverter();
                using (EditorHost editorHost = new EditorHost(cmlConverter.Export(_currentModel), "LABELS", DefaultBondLength))
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
            Model clone = new Model();

            if (_currentModel != null)
            {
                clone = _currentModel.Copy();
            }

            CMLConverter cmlConvertor = new CMLConverter();
            using (EditorHost editorHost = new EditorHost(cmlConvertor.Export(clone), "ACME", DefaultBondLength))
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
                if (model.MeanBondLength < 4 || model.MeanBondLength > 96)
                {
                    Debugger.Break();
                }

                if (model.AllErrors.Any())
                {
                    List<string> lines = new List<string>();

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

                    Information1.Text = $"Formula: {model.ConciseFormula} Unicode: {model.UnicodeFormula}";

                    StringBuilder stringBuilder = new StringBuilder();
                    stringBuilder.Append($"Mean Bond Length: {SafeDouble.AsString(model.MeanBondLength)} ");
                    stringBuilder.Append($"Molecular Weight: {SafeDouble.AsString4(model.MolecularWeight)}");
                    Information2.Text = stringBuilder.ToString();

                    // MUST Use a copy as the display modifies BondLength
                    Display.Chemistry = model.Copy();

                    Debug.WriteLine($"FlexForm is displaying {model.ConciseFormula}");

                    SetButtonStates(FormState.Edit);
                }
            }
        }

        private void OnClick_Undo(object sender, EventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                Model poppedModel = _undoStack.Pop();
                Debug.WriteLine(
                    $"Popped F: {poppedModel.ConciseFormula} BL: {SafeDouble.AsString0(poppedModel.MeanBondLength)} from Undo Stack");

                if (_currentModel != null)
                {
                    Debug.WriteLine(
                        $"Pushing F: {_currentModel.ConciseFormula} BL: {SafeDouble.AsString0(_currentModel.MeanBondLength)} onto Redo Stack");
                    _redoStack.Push(_currentModel);
                }

                _currentModel = poppedModel;
                ShowChemistry($"Undo -> {_currentModel.ConciseFormula}", _currentModel);
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
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                Model poppedModel = _redoStack.Pop();
                Debug.WriteLine(
                    $"Popped F: {poppedModel.ConciseFormula} BL: {SafeDouble.AsString0(poppedModel.MeanBondLength)} from Redo Stack");

                if (_currentModel != null)
                {
                    Debug.WriteLine(
                        $"Pushing F: {_currentModel.ConciseFormula} BL: {SafeDouble.AsString0(_currentModel.MeanBondLength)} onto Undo Stack");
                    _undoStack.Push(_currentModel);
                }

                _currentModel = poppedModel;
                ShowChemistry($"Redo -> {_currentModel.ConciseFormula}", _currentModel);
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
                foreach (Model model in _undoStack)
                {
                    Debug.WriteLine(
                        $"{model.ConciseFormula} [{model.GetHashCode()}] {model.MeanBondLength.ToString("#,##0.00")}");
                }
            }

            if (_redoStack.Any())
            {
                Debug.WriteLine("Redo Stack");
                foreach (Model model in _redoStack)
                {
                    Debug.WriteLine(
                        $"{model.ConciseFormula} [{model.GetHashCode()}] {model.MeanBondLength.ToString("#,##0.00")}");
                }
            }
        }

        private void OnClick_EditCml(object sender, EventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            try
            {
                if (_currentModel != null)
                {
                    CMLConverter cmlConverter = new CMLConverter();
                    string cml = cmlConverter.Export(_currentModel);

                    using (EditorHost editorHost = new EditorHost(cml, "CML", DefaultBondLength))
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
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                if (_currentModel != null)
                {
                    CMLConverter cmlConverter = new CMLConverter();
                    string cml = cmlConverter.Export(_currentModel);

                    using (ShowCml f = new ShowCml { Cml = cml })
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
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                if (_currentModel != null)
                {
                    Model copy = _currentModel.Copy();
                    CMLConverter cmlConverter = new CMLConverter();

                    copy.CustomXmlPartGuid = "";

                    StringBuilder sb = new StringBuilder();
                    sb.Append("CML molecule files (*.cml, *.xml)|*.cml;*.xml");
                    sb.Append("|MDL molecule files (*.mol, *.sdf)|*.mol;*.sdf");
                    sb.Append("|ChemDoodle Web json files (*.json)|*.json");
                    sb.Append("|Protocol Buffers (*.pbuff)|*.pbuff");
                    sb.Append("|SketchEl (*.el)|*.el");

                    using (SaveFileDialog sfd = new SaveFileDialog { Filter = sb.ToString() })
                    {
                        sfd.AddExtension = true;
                        DialogResult dr = sfd.ShowDialog();
                        if (dr == DialogResult.OK)
                        {
                            FileInfo fi = new FileInfo(sfd.FileName);
                            _telemetry.Write(module, "Information", $"Exporting to '{fi.Name}'");
                            string fileType = Path.GetExtension(sfd.FileName).ToLower();
                            switch (fileType)
                            {
                                case ".cml":
                                case ".xml":
                                    File.WriteAllText(sfd.FileName, XmlHelper.AddHeader(cmlConverter.Export(copy)));
                                    break;

                                case ".mol":
                                case ".sdf":
                                    // https://www.chemaxon.com/marvin-archive/6.0.2/marvin/help/formats/mol-csmol-doc.html
                                    double before = copy.MeanBondLength;
                                    // Set bond length to 1.54 angstroms (Å)
                                    copy.ScaleToAverageBondLength(1.54);
                                    double after = copy.MeanBondLength;
                                    _telemetry.Write(module, "Information", $"Structure rescaled from {SafeDouble.AsString(before)} to {SafeDouble.AsString(after)}");
                                    SdFileConverter sdFileConverter = new SdFileConverter();
                                    File.WriteAllText(sfd.FileName, sdFileConverter.Export(copy));
                                    break;

                                case ".el":
                                    SketchElConverter sketchElConverter = new SketchElConverter();
                                    File.WriteAllText(sfd.FileName, sketchElConverter.Export(copy));
                                    break;

                                case ".json":
                                    JSONConverter jsonConverter = new JSONConverter();
                                    File.WriteAllText(sfd.FileName, jsonConverter.Export(copy));
                                    break;

                                case ".pbuff":
                                    WritePBuffFile(sfd.FileName, copy);
                                    break;
                            }
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
            ProtocolBufferConverter pbc = new ProtocolBufferConverter();
            byte[] bytes = pbc.Export(model);
            File.WriteAllBytes(sfdFileName, bytes);
        }

        private void OnClick_ClearChemistry(object sender, EventArgs e)
        {
            CMLConverter cc = new CMLConverter();
            _undoStack.Push(_currentModel);
            _currentModel = null;

            Display.Clear();
            SetButtonStates(FormState.Edit);
        }

        private void OnLoad_FlexForm(object sender, EventArgs e)
        {
            Display.HighlightActive = false;

            // ToDo: [MAW] Check if we still need the config watcher
            string location = Assembly.GetExecutingAssembly().Location;
            string path = Path.GetDirectoryName(location);
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
            OoXmlV4Settings settings = new OoXmlV4Settings();
            settings.Telemetry = _telemetry;
            settings.TopLeft = new Point(Left + 24, Top + 24);

            OoXmlV4Options tempOptions = _renderOptions.Clone();
            settings.RendererOptions = tempOptions;

            DialogResult dr = settings.ShowDialog();
            if (dr == DialogResult.OK)
            {
                _renderOptions = tempOptions.Clone();
                OptionsChanged();
            }

            settings.Close();
        }

        private void UpdateControls()
        {
            // MUST Use a copy as the display modifies BondLength
            Display.Chemistry = _currentModel.Copy();

            UndoStackViewer.Reload(_undoStack);
            RedoStackViewer.Reload(_redoStack);
        }

        private void OnClick_LayoutStructure(object sender, EventArgs e)
        {
            // No Longer working ...
            // LayoutUsingChemblClean()
        }

        private void LayoutUsingChemblClean()
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            CMLConverter cc = new CMLConverter();
            Model copy = _currentModel.Copy();

            if (copy.TotalMoleculesCount == 1
                && !copy.HasNestedMolecules
                && !copy.HasFunctionalGroups)
            {
                string marvin = cc.Export(copy, true, CmlFormat.MarvinJs);

                // Replace double quote with single quote
                marvin = marvin.Replace("\"", "'");

                try
                {
                    using (HttpClient httpClient = new HttpClient())
                    {
                        httpClient.Timeout = TimeSpan.FromSeconds(15);
                        httpClient.DefaultRequestHeaders.Add("user-agent", "Chem4Word");

                        try
                        {
                            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, "https://www.ebi.ac.uk/chembl/api/utils/clean");
                            request.Headers.Add("User-Agent", "Chem4Word");

                            string body = JsonConvert.SerializeObject(new { structure = $"{marvin}", parameters = new { dim = 2, opts = "s" } });
                            request.Content = new StringContent(body, Encoding.UTF8, "text/plain");

                            HttpResponseMessage response = httpClient.SendAsync(request).Result;

                            if (!response.IsSuccessStatusCode)
                            {
                                // Handle Error
                                Debug.WriteLine($"{response.StatusCode} - {response.RequestMessage}");
                            }

                            Task<string> answer = response.Content.ReadAsStringAsync();
                            Debug.WriteLine(answer.Result);

                            copy = cc.Import(answer.Result);
                            copy.EnsureBondLength(DefaultBondLength, false);
                            if (string.IsNullOrEmpty(copy.CustomXmlPartGuid))
                            {
                                copy.CustomXmlPartGuid = Guid.NewGuid().ToString("N");
                            }

                            _undoStack.Push(_currentModel);
                            _currentModel = copy;

                            ShowChemistry("ChEMBL clean", copy);
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
            CMLConverter cmlConverter = new CMLConverter();

            Renderer renderer = new Renderer
            {
                Telemetry = _telemetry,
                TopLeft = new Point(Left + 24, Top + 24),
                Cml = cmlConverter.Export(_currentModel),
                Properties = new Dictionary<string, string>
                                                 {
                                                     {
                                                         "Guid", Guid.NewGuid().ToString("N")
                                                     }
                                                 }
            };
            string tempFileName = renderer.Render();
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
            using (SearchChEBI searcher = new SearchChEBI())
            {
                searcher.Telemetry = _telemetry;
                searcher.UserOptions = new ChEBIOptions();
                searcher.TopLeft = new Point(Left + 24, Top + 24);

                DialogResult result = searcher.ShowDialog(this);
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
            using (SearchPubChem searcher = new SearchPubChem())
            {
                searcher.Telemetry = _telemetry;
                searcher.UserOptions = new PubChemOptions();
                searcher.TopLeft = new Point(Left + 24, Top + 24);

                DialogResult result = searcher.ShowDialog(this);
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
            using (SearchOpsin searcher = new SearchOpsin())
            {
                searcher.Telemetry = _telemetry;
                searcher.UserOptions = new SearcherOptions();
                searcher.TopLeft = new Point(Left + 24, Top + 24);

                DialogResult result = searcher.ShowDialog(this);
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
            CMLConverter cc = new CMLConverter();
            if (_currentModel != null)
            {
                Model copy = _currentModel.Copy();
                Debug.WriteLine(
                    $"Pushing F: {copy.ConciseFormula} BL: {SafeDouble.AsString(copy.MeanBondLength)} onto Stack");
                _undoStack.Push(copy);
            }

            Model model = cc.Import(cml);
            if (model.AllErrors.Count == 0 && model.AllWarnings.Count == 0)
            {
                model.Relabel(true);
                model.EnsureBondLength(DefaultBondLength, false);
                _currentModel = model;

                ShowChemistry($"{captionPrefix} {model.ConciseFormula}", _currentModel);
            }
            else
            {
                List<string> errors = model.AllWarnings;
                errors.AddRange(model.AllErrors);

                MessageBox.Show(string.Join(Environment.NewLine, errors), "Model has errors or warnings!");
            }
        }

        private void OnClick_CalculateProperties(object sender, EventArgs e)
        {
            CMLConverter cc = new CMLConverter();

            if (_currentModel != null)
            {
                Model copy = _currentModel.Copy();
                Debug.WriteLine(
                    $"Pushing F: {copy.ConciseFormula} BL: {SafeDouble.AsString(copy.MeanBondLength)} onto Stack");
                _undoStack.Push(copy);

                Assembly assembly = Assembly.GetExecutingAssembly();
                Version version = assembly.GetName().Version;
                PropertyCalculator pc = new PropertyCalculator(_telemetry, new Point(Left, Top), version.ToString());

                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();

                copy.CreatorGuid = $"TH:{Guid.NewGuid():N}";
                int changedProperties = pc.CalculateProperties(copy);

                stopwatch.Stop();
                Debug.WriteLine($"Calculating {changedProperties} changed properties took {stopwatch.Elapsed}");

                _currentModel = copy;

                ShowChemistry($"{changedProperties} changed properties; {copy.ConciseFormula}", copy);
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (_helper.GitStatusObtained)
            {
                timer1.Enabled = false;
                Thread.Sleep(250);
                SetButtonStates(FormState.OpenOrCreate);
            }
        }
    }
}
