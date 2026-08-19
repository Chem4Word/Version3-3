// ---------------------------------------------------------------------------
//  Copyright (c) 2026, The .NET Foundation.
//  This software is released under the Apache Licence, Version 2.0.
//  The licence and further copyright text can be found in the file LICENCE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.ACME.Annotations;
using Chem4Word.ACME.Controls;
using Chem4Word.ACME.Models;
using Chem4Word.ACME.Models.Chem4Word.Controls.TagControl;
using Chem4Word.Core;
using Chem4Word.Core.Helpers;
using Chem4Word.Core.UI;
using Chem4Word.Core.UI.Forms;
using Chem4Word.Core.UI.Wpf;
using Chem4Word.Helpers;
using Chem4Word.Model2;
using Chem4Word.Model2.Converters.CML;
using Chem4Word.Model2.Converters.MDL;
using Chem4Word.Model2.Converters.ProtocolBuffers;
using Chem4Word.WebServices;
using IChem4Word.Contracts;
using IChem4Word.Contracts.Dto;
using Newtonsoft.Json;
using Ookii.Dialogs.WinForms;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Threading;
using DataFormats = System.Windows.DataFormats;
using DataObject = System.Windows.DataObject;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using ListBox = System.Windows.Controls.ListBox;
using Model = Chem4Word.Model2.Model;
using Size = System.Windows.Size;
using TextBox = System.Windows.Controls.TextBox;
using UserControl = System.Windows.Controls.UserControl;

namespace Chem4Word.UI.WPF
{
    /// <summary>
    /// Interaction logic for CatalogueControl.xaml
    /// </summary>
    public partial class LibraryEditorControl : UserControl
    {
        private static string _product = Assembly.GetExecutingAssembly().FullName.Split(',')[0];
        private static string _class = MethodBase.GetCurrentMethod()?.DeclaringType?.Name;

        private IChem4WordTelemetry _telemetry;
        private IChem4WordLibraryWriter _driver;

        private int _filteredItems;
        private int _checkedItems;
        private bool _isLoading;
        private bool _cancelRequested = false;

        private List<string> _lastTags = new List<string>();

        private const string UserTagsFileName = "MyTags.json";
        private SortedDictionary<string, long> _userTags = new SortedDictionary<string, long>();

        public Point TopLeft { get; set; }

        public int DefaultBondLength { get; set; }

        public LibraryEditorControl(IChem4WordTelemetry telemetry)
        {
            _telemetry = telemetry;
            _isLoading = true;
            InitializeComponent();

            DataObject.AddPastingHandler(SubstanceName, OnPasteSubstanceName);
            DataObject.AddPastingHandler(SearchBox, OnPasteSearchBox);
        }

        public Size ItemSize
        {
            get { return (Size)GetValue(ItemSizeProperty); }
            set { SetValue(ItemSizeProperty, value); }
        }

        public static readonly DependencyProperty ItemSizeProperty =
            DependencyProperty.Register("ItemSize", typeof(Size), typeof(LibraryEditorControl),
                                        new FrameworkPropertyMetadata(new Size(180.0, 180.0),
                                                                      FrameworkPropertyMetadataOptions.AffectsRender
                                                                      | FrameworkPropertyMetadataOptions.AffectsArrange
                                                                      | FrameworkPropertyMetadataOptions.AffectsMeasure));

        public double DisplayWidth
        {
            get { return (double)GetValue(DisplayWidthProperty); }
            set { SetValue(DisplayWidthProperty, value); }
        }

        public static readonly DependencyProperty DisplayWidthProperty =
            DependencyProperty.Register("DisplayWidth", typeof(double), typeof(LibraryEditorControl),
                                        new FrameworkPropertyMetadata(180.0,
                                                                      FrameworkPropertyMetadataOptions.AffectsRender
                                                                      | FrameworkPropertyMetadataOptions.AffectsArrange
                                                                      | FrameworkPropertyMetadataOptions.AffectsMeasure));

        public double DisplayHeight
        {
            get { return (double)GetValue(DisplayHeightProperty); }
            set { SetValue(DisplayHeightProperty, value); }
        }

        public static readonly DependencyProperty DisplayHeightProperty =
            DependencyProperty.Register("DisplayHeight", typeof(double), typeof(LibraryEditorControl),
                                        new FrameworkPropertyMetadata(180.0,
                                                                      FrameworkPropertyMetadataOptions.AffectsRender
                                                                      | FrameworkPropertyMetadataOptions.AffectsArrange
                                                                      | FrameworkPropertyMetadataOptions.AffectsMeasure));

        public void SetDriver(IChem4WordLibraryWriter driver)
        {
            _driver = driver;
        }

        private void OnLoaded_LibraryEditorControl(object sender, RoutedEventArgs e)
        {
            ApplySort();
            Cancel.Visibility = Visibility.Collapsed;
            _isLoading = false;
        }

        private void OnClick_ChemistryItem(object sender, RoutedEventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod()?.Name}()";
            _telemetry.Write(module, LoggingLevel.Action, "Triggered");

            if (e.OriginalSource is WpfEventArgs source
                && DataContext is LibraryEditorViewModel viewModel)
            {
                Debug.WriteLine($"{_class} -> {source.Button} {source.ButtonDetails}");

                if (source.Button.StartsWith("CheckBox"))
                {
                    _checkedItems = viewModel.ChemistryItems.Count(i => i.IsChecked);

                    SetButtonStates(true);

                    UpdateStatusBar();
                }

                if (source.Button.StartsWith("DisplayDoubleClick"))
                {
                    long id = long.Parse(source.ButtonDetails.Split('=')[1]);
                    ChemistryObject item = viewModel.ChemistryItems.FirstOrDefault(o => o.Id == id);
                    if (item != null)
                    {
                        PerformEdit(item);
                        UpdateStatusBar();
                    }
                }
            }
        }

        private void PerformEdit(ChemistryObject item)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod()?.Name}()";
            _telemetry.Write(module, LoggingLevel.Action, "Triggered");

            IChem4WordEditor editor = Globals.Chem4WordV3.GetEditorPlugIn(Globals.Chem4WordV3.SystemOptions.SelectedEditorPlugIn);
            if (editor != null)
            {
                if (editor.CanEditReactions && editor.CanEditFunctionalGroups && editor.CanEditNestedMolecules)
                {
                    Point topLeft = new Point(TopLeft.X + CoreConstants.TopLeftOffset, TopLeft.Y + CoreConstants.TopLeftOffset);
                    editor.TopLeft = topLeft;
                    string beforeCml = item.CmlFromChemistry();
                    editor.Cml = beforeCml;
                    Model model = new CMLConverter().Import(beforeCml);
                    string beforeFormula = model.ConciseFormula;
                    string beforeSubstanceName = item.SubstanceName;
                    UpdateModelProperties(model, item);
                    if (string.IsNullOrEmpty(beforeCml))
                    {
                        RenderingOptions renderingOptions = new RenderingOptions
                        {
                            ExplicitC = Globals.Chem4WordV3.SystemOptions.ExplicitC,
                            ExplicitH = Globals.Chem4WordV3.SystemOptions.ExplicitH,
                            ShowColouredAtoms = Globals.Chem4WordV3.SystemOptions.ShowColouredAtoms,
                            ShowMoleculeGrouping = Globals.Chem4WordV3.SystemOptions.ShowMoleculeGrouping,
                            ShowMolecularWeight = Globals.Chem4WordV3.SystemOptions.ShowMolecularWeight,
                            ShowMoleculeCaptions = Globals.Chem4WordV3.SystemOptions.ShowMoleculeCaptions,
                            DefaultBondLength = CoreConstants.StandardBondLength
                        };
                        editor.DefaultRenderingOptions = renderingOptions.ToJson();
                    }
                    else
                    {
                        editor.DefaultRenderingOptions = new RenderingOptions(model).ToJson();
                    }

                    // Perform the edit
                    DialogResult chemEditorResult = editor.Edit();
                    if (chemEditorResult == DialogResult.OK)
                    {
                        CMLConverter cmlConverter = new CMLConverter();

                        Model afterModel = cmlConverter.Import(editor.Cml);
                        string afterCml = editor.Cml;
                        string afterFormula = afterModel.ConciseFormula;

                        PropertyCalculator pc = new WebServices.PropertyCalculator(_telemetry, topLeft, Globals.Chem4WordV3.AddInInfo.AssemblyVersionNumber);
                        if (!string.IsNullOrEmpty(Globals.Chem4WordV3.Helper.MachineId))
                        {
                            afterModel.CreatorGuid = Globals.Chem4WordV3.Helper.MachineId;
                        }
                        else
                        {
                            afterModel.CreatorGuid = CoreConstants.DummyMachineGuid;
                        }
                        pc.CalculateProperties(afterModel);

                        afterModel.SetAnyMissingNameIds();
                        afterModel.ReLabelGuids();
                        afterModel.Relabel(true);

                        using (EditLabelsHost editLabelsHost = new EditLabelsHost())
                        {
                            editLabelsHost.TopLeft = topLeft;
                            editLabelsHost.Cml = cmlConverter.Export(afterModel);

                            // Show Label Editor
                            DialogResult dr = editLabelsHost.ShowDialog();
                            if (dr == DialogResult.OK)
                            {
                                afterModel = cmlConverter.Import(editLabelsHost.Cml);
                                afterCml = cmlConverter.Export(afterModel);
                                afterFormula = afterModel.ConciseFormula;
                            }
                            editLabelsHost.Close();
                        }

                        long edited = 0;
                        ChemistryDataTransferObject dto = DtoHelper.CreateFromModel(afterModel, CoreConstants.DefaultSaveFormat);
                        UpdateModelProperties(model, dto);

                        if (item.Id == 0)
                        {
                            dto.SubstanceName = afterModel.QuickName;

                            // Add New Chemistry
                            long newId = _driver.AddChemistry(dto);

                            LibraryEditorViewModel viewModel = new LibraryEditorViewModel(_telemetry, _driver);
                            DataContext = viewModel;

                            edited = newId;
                        }
                        else
                        {
                            dto.Id = item.Id;

                            dto.SubstanceName = string.IsNullOrEmpty(beforeSubstanceName)
                                ? afterModel.QuickName
                                : beforeSubstanceName;

                            // Update Existing Chemistry
                            _driver.UpdateChemistry(dto);

                            item.Chemistry = afterCml;
                            item.ConciseFormula = afterModel.ConciseFormula;
                            item.UnicodeFormula = afterModel.UnicodeFormula;
                            item.MolecularWeight = afterModel.MolecularWeight;

                            RefreshItemProperties(item, dto);

                            edited = item.Id;
                        }

                        ApplySort();
                        SelectChangedItem(edited);
                    }
                }
                else
                {
                    UserInteractions.WarnUser($"Selected Editor Plug-In '{editor.Name}' is not allowed here");
                }
            }
            else
            {
                UserInteractions.WarnUser($"Unable to find an Editor Plug-In [{Globals.Chem4WordV3.SystemOptions.SelectedEditorPlugIn}]");
            }
        }

        private void SelectChangedItem(long itemId)
        {
            int idx = -1;

            foreach (ChemistryObject catalogueItem in CatalogueItems.Items)
            {
                idx++;
                if (catalogueItem.Id == itemId)
                {
                    Dispatcher.BeginInvoke(
                        DispatcherPriority.Loaded,
                        new Action(() =>
                                   {
                                       // Force generation of containers
                                       CatalogueItems.UpdateLayout();

                                       // Now select
                                       CatalogueItems.SelectedIndex = idx;
                                       CatalogueItems.ScrollIntoView(CatalogueItems.SelectedItem);
                                   }));
                    break;
                }
            }
        }

        private static void RefreshItemProperties(ChemistryObject item, ChemistryDataTransferObject dto)
        {
            // Refresh other properties of the item
            item.Chemistry = dto.Chemistry;
            item.ChemicalNames = dto.Names.Select(n => n.Name).Distinct().ToList();
            item.Names = dto.Names;
            item.Formulae = dto.Formulae;
            item.Captions = dto.Captions;
        }

        private void OnValueChanged_Slider(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod()?.Name}()";
            if (!_isLoading)
            {
                _telemetry.Write(module, LoggingLevel.Action, $"Display size changed to {Slider.Value}");
            }
            ItemSize = new Size(Slider.Value, Slider.Value + 65);
            DisplayWidth = Slider.Value - 20;
            DisplayHeight = Slider.Value - 20;
        }

        private void OnKeyDown_SearchBox(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                if (string.IsNullOrWhiteSpace(SearchBox.Text))
                {
                    OnClick_ClearButton(null, null);
                }
                else
                {
                    OnClick_SearchButton(null, null);
                }
            }
        }

        private void OnTextChanged_SearchBox(object sender, TextChangedEventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod()?.Name}()";
            try
            {
                if (string.IsNullOrWhiteSpace(SearchBox.Text))
                {
                    SearchButton.IsEnabled = false;
                    ClearButton.IsEnabled = false;
                }
                else
                {
                    SearchButton.IsEnabled = true;
                    ClearButton.IsEnabled = true;
                }
            }
            catch (Exception ex)
            {
                using (ReportError form = new ReportError(_telemetry, Globals.Chem4WordV3.WordTopLeft, module, ex))
                {
                    form.ShowDialog();
                }
            }
        }

        private void OnSelectionChanged_SortBy(object sender, SelectionChangedEventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod()?.Name}()";
            try
            {
                if (!_isLoading)
                {
                    _telemetry.Write(module, LoggingLevel.Action, "Triggered");
                }
                if (DataContext != null)
                {
                    ApplySort();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"{module} {ex.Message}");
            }
        }

        private void OnClick_ClearButton(object sender, RoutedEventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod()?.Name}()";
            _telemetry.Write(module, LoggingLevel.Action, "Triggered");
            try
            {
                CheckedFilterButton.IsChecked = false;
                SearchBox.Clear();

                if (DataContext != null)
                {
                    ClearFilter();
                    UpdateStatusBar();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"{module} {ex.Message}");
            }
        }

        private void OnClick_SearchButton(object sender, RoutedEventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod()?.Name}()";
            _telemetry.Write(module, LoggingLevel.Action, "Triggered");
            try
            {
                CheckedFilterButton.IsChecked = false;
                string searchFor = TextHelper.StripAsciiControlCharacters(SearchBox.Text).Trim();
                if (!string.IsNullOrWhiteSpace(searchFor)
                    && DataContext != null)
                {
                    _telemetry.Write(module, LoggingLevel.Information, $"Filter library by '{searchFor}'");
                    FilterByText(searchFor);
                    UpdateStatusBar();
                }
            }
            catch (Exception ex)
            {
                using (ReportError form = new ReportError(_telemetry, Globals.Chem4WordV3.WordTopLeft, module, ex))
                {
                    form.ShowDialog();
                }
            }
        }

        private void OnClick_CheckedFilterButton(object sender, RoutedEventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod()?.Name}()";
            _telemetry.Write(module, LoggingLevel.Action, "Triggered");

            try
            {
                if (DataContext != null)
                {
                    if (CheckedFilterButton.IsChecked != null && CheckedFilterButton.IsChecked.Value)
                    {
                        if (_checkedItems > 0)
                        {
                            SearchBox.Clear();
                            FilterByChecked();
                        }
                        else
                        {
                            CheckedFilterButton.IsChecked = false;
                        }
                    }
                    else
                    {
                        SearchBox.Clear();
                        ClearFilter();
                    }

                    UpdateStatusBar();
                }
            }
            catch (Exception ex)
            {
                using (ReportError form = new ReportError(_telemetry, Globals.Chem4WordV3.WordTopLeft, module, ex))
                {
                    form.ShowDialog();
                }
            }
        }

        public event EventHandler<WpfEventArgs> OnSelectionChange;

        private void OnSelectionItemChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.OriginalSource is ListBox source
                && source.SelectedItem is ChemistryObject selected
                && source.DataContext is LibraryEditorViewModel context)
            {
                context.SelectedChemistryObject = selected;
                _lastTags = context.SelectedChemistryObject.Tags;

                WpfEventArgs args = new WpfEventArgs
                {
                    Button = "CatalogueView|SelectedItemChanged",
                    ButtonDetails = $"Id={selected.Id}"
                };
                OnSelectionChange?.Invoke(this, args);
            }
        }

        private void ApplySort()
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod()?.Name}()";

            if (ComboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                ListCollectionView view = (ListCollectionView)CollectionViewSource.GetDefaultView(CatalogueItems.ItemsSource);
                if (view != null)
                {
                    view.SortDescriptions.Clear();

                    string propertyName = selectedItem.Content.ToString();
                    if (!_isLoading)
                    {
                        _telemetry.Write(module, LoggingLevel.Action, $"Sorting structures by {propertyName}");
                    }
                    if (propertyName.Equals("Name"))
                    {
                        // Sort the list of structures by lower case name
                        view.CustomSort = new ChemistryObjectComparer();
                    }
                    else
                    {
                        view.SortDescriptions.Add(new SortDescription(propertyName, ListSortDirection.Ascending));
                    }
                }
            }
        }

        private void ClearSelectedChemistryItem()
        {
            CatalogueItems.SelectedItem = null;
            if (CatalogueItems.DataContext is LibraryEditorViewModel lvm)
            {
                lvm.SelectedChemistryObject = null;
            }
        }

        private void ClearFilter()
        {
            ClearSelectedChemistryItem();

            //get the view from the listbox's source
            ListCollectionView view = (ListCollectionView)CollectionViewSource.GetDefaultView(CatalogueItems.ItemsSource);
            if (view != null)
            {
                _filteredItems = 0;
                view.Filter = null;
            }
        }

        private void FilterByText(string searchFor)
        {
            ClearSelectedChemistryItem();

            //get the view from the listbox's source
            ListCollectionView view = (ListCollectionView)CollectionViewSource.GetDefaultView(CatalogueItems.ItemsSource);
            if (view != null)
            {
                //then try to match part of either its name or an alternative name to the string typed in
                _filteredItems = 0;
                view.Filter = ci =>
                              {
                                  if (ci is ChemistryObject item)
                                  {
                                      string queryString = searchFor.ToUpper();
                                      if (item.SubstanceName.ToUpper().Contains(queryString)
                                          || item.ChemicalNames.Any(n => n.ToUpper().Contains(queryString))
                                          || item.Tags.Any(n => n.ToUpper().Contains(queryString)))
                                      {
                                          _filteredItems++;
                                          return true;
                                      }
                                  }

                                  return false;
                              };
            }
        }

        private void FilterByChecked()
        {
            ClearSelectedChemistryItem();

            //get the view from the listbox's source
            ListCollectionView view = (ListCollectionView)CollectionViewSource.GetDefaultView(CatalogueItems.ItemsSource);

            //then try to match part of either its name or an alternative name to the string typed in
            _filteredItems = 0;
            view.Filter = ci =>
                          {
                              if (ci is ChemistryObject item
                                  && item.IsChecked)
                              {
                                  _filteredItems++;
                                  return true;
                              }

                              return false;
                          };
        }

        private void OnClick_AddNewButton(object sender, RoutedEventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod()?.Name}()";
            _telemetry.Write(module, LoggingLevel.Action, "Adding new structure");
            try
            {
                PerformEdit(new ChemistryObject());
                RefreshGrid();
            }
            catch (Exception exception)
            {
                new ReportError(_telemetry, TopLeft, module, exception).ShowDialog();
            }
        }

        private void OnClick_MetadataButton(object sender, RoutedEventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod()?.Name}()";
            try
            {
                _telemetry.Write(module, LoggingLevel.Action, "Editing structure meta data");
                Point topLeft = new Point(TopLeft.X + CoreConstants.TopLeftOffset, TopLeft.Y + CoreConstants.TopLeftOffset);

                using (EditLabelsHost editLabelsHost =
                       new EditLabelsHost())
                {
                    if (CatalogueItems.SelectedItem is ChemistryObject item)
                    {
                        editLabelsHost.TopLeft = topLeft;
                        editLabelsHost.Cml = item.CmlFromChemistry();

                        // Show Label Editor
                        DialogResult dr = editLabelsHost.ShowDialog();
                        if (dr == DialogResult.OK)
                        {
                            CMLConverter cmlConverter = new CMLConverter();
                            Model afterModel = cmlConverter.Import(editLabelsHost.Cml);

                            ChemistryDataTransferObject dto = DtoHelper.CreateFromModel(afterModel, CoreConstants.DefaultSaveFormat);
                            dto.Id = item.Id;
                            UpdateModelProperties(afterModel, dto);

                            // Save to database
                            _driver.UpdateChemistry(dto);

                            // Fetch the item we have just written so that Id's are correct
                            dto = _driver.GetChemistryById(dto.Id);

                            RefreshItemProperties(item, dto);
                        }
                        editLabelsHost.Close();
                    }
                }
            }
            catch (Exception exception)
            {
                new ReportError(_telemetry, TopLeft, module, exception).ShowDialog();
            }
        }

        private void OnClick_DeleteButton(object sender, RoutedEventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod()?.Name}()";
            _telemetry.Write(module, LoggingLevel.Action, "Triggered");

            try
            {
                SetButtonStates(false);

                StringBuilder sb = new StringBuilder();
                sb.AppendLine("This will delete the selected structures from the Library");
                sb.AppendLine("");
                sb.AppendLine("Do you want to proceed?");
                sb.AppendLine("This cannot be undone.");
                DialogResult dialogResult = UserInteractions.AskUserYesNo(sb.ToString(), MessageBoxDefaultButton.Button2);
                if (dialogResult == DialogResult.Yes)
                {
                    _driver.StartTransaction();
                    int progress = 0;

                    Stopwatch sw = new Stopwatch();
                    sw.Start();

                    try
                    {
                        CatalogueItems.SelectedItem = null;

                        if (DataContext is LibraryEditorViewModel controller)
                        {
                            ProgressBar.Maximum = _checkedItems;

                            List<ChemistryObject> items = controller.ChemistryItems.Where(i => i.IsChecked).ToList();
                            foreach (ChemistryObject item in items)
                            {
                                progress++;
                                ShowProgress(progress, $" [{progress}/{_checkedItems}]");

                                _driver.DeleteChemistryById(item.Id);
                            }
                        }

                        _driver.CommitTransaction();

                        sw.Stop();
                        _telemetry.Write(module, LoggingLevel.Timing, $"Delete of {progress} structures from '{_driver.FileName}' took {SafeDouble.AsString0(sw.ElapsedMilliseconds)}ms");

                        _checkedItems = 0;
                        DeleteButton.IsEnabled = false;
                        CheckedFilterButton.IsEnabled = false;
                    }
                    catch (Exception exception)
                    {
                        _driver.RollbackTransaction();
                        new ReportError(_telemetry, TopLeft, module, exception).ShowDialog();
                    }
                }
            }
            catch (Exception exception)
            {
                new ReportError(_telemetry, TopLeft, module, exception).ShowDialog();
            }
            finally
            {
                ClearProgress();
                RefreshGrid();
                SetButtonStates(true);
            }
        }

        private void OnClick_ImportButton(object sender, RoutedEventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod()?.Name}()";
            _telemetry.Write(module, LoggingLevel.Action, "Triggered");
            try
            {
                SetButtonStates(false);

                StringBuilder sb;
                string importFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                if (Directory.Exists(importFolder))
                {
                    VistaFolderBrowserDialog browser = new VistaFolderBrowserDialog();

                    browser.Description = "Select a folder to import cml files from";
                    browser.UseDescriptionForTitle = true;
                    browser.RootFolder = Environment.SpecialFolder.Desktop;
                    browser.ShowNewFolderButton = false;
                    browser.SelectedPath = importFolder;
                    DialogResult dr = browser.ShowDialog();

                    if (dr == DialogResult.OK)
                    {
                        string selectedFolder = browser.SelectedPath;
                        string doneFile = Path.Combine(selectedFolder, "library-import-done.txt");

                        sb = new StringBuilder();
                        sb.AppendLine("Do you want to import these structures into the Library?");
                        dr = UserInteractions.AskUserYesNo(sb.ToString());
                        if (dr == DialogResult.Yes
                            && File.Exists(doneFile))
                        {
                            sb = new StringBuilder();
                            sb.AppendLine($"Files have been imported already from '{selectedFolder}'");
                            sb.AppendLine("Do you want to rerun the import?");
                            dr = UserInteractions.AskUserYesNo(sb.ToString());
                            if (dr == DialogResult.Yes)
                            {
                                File.Delete(doneFile);
                            }
                        }

                        if (dr == DialogResult.Yes)
                        {
                            int fileCount = 0;

                            Stopwatch sw = new Stopwatch();
                            sw.Start();

                            _driver.StartTransaction();

                            int progress = 0;

                            try
                            {
                                List<string> files = Directory.GetFiles(selectedFolder, "*.cml").ToList();
                                files.AddRange(Directory.GetFiles(selectedFolder, "*.mol"));

                                if (files.Count > 0)
                                {
                                    int total = files.Count;

                                    ProgressBar.Maximum = files.Count;

                                    CMLConverter cmlConverter = new CMLConverter();
                                    SdFileConverter sdfConvertor = new SdFileConverter();

                                    foreach (string file in files)
                                    {
                                        progress++;
                                        ShowProgress(progress, $" [{progress}/{total}]");

                                        string contents = File.ReadAllText(file);

                                        Model model;

                                        if (contents.StartsWith("<"))
                                        {
                                            model = cmlConverter.Import(contents);
                                        }
                                        else
                                        {
                                            model = sdfConvertor.Import(contents);
                                        }

                                        if (model.TotalAtomsCount > 0)
                                        {
                                            // Tidy Up the structures
                                            if (Globals.Chem4WordV3.SystemOptions.RemoveExplicitHydrogensOnImportFromFile)
                                            {
                                                if (!model.HasElectronPushers)
                                                {
                                                    model.RemoveExplicitHydrogens();
                                                }
                                            }

                                            model.EnsureBondLength(Globals.Chem4WordV3.SystemOptions.BondLength,
                                                                   Globals.Chem4WordV3.SystemOptions.SetBondLengthOnImportFromFile);

                                            model.Relabel(true);

                                            ChemistryDataTransferObject dto = DtoHelper.CreateFromModel(model, CoreConstants.DefaultSaveFormat);
                                            _driver.AddChemistry(dto);
                                        }

                                        fileCount++;
                                    }

                                    File.WriteAllText(doneFile, $"{fileCount} cml files imported into library");
                                    FileInfo fi = new FileInfo(doneFile);
                                    fi.Attributes = FileAttributes.Hidden;
                                    _telemetry.Write(module, LoggingLevel.Information, $"Imported {fileCount} structures into '{_driver.FileName}'");
                                    _driver.CommitTransaction();
                                }
                            }
                            catch (Exception exception)
                            {
                                _driver.RollbackTransaction();
                                new ReportError(_telemetry, TopLeft, module, exception).ShowDialog();
                            }

                            sw.Stop();

                            _telemetry.Write(module, LoggingLevel.Timing, $"Import of {progress} files took {SafeDouble.AsString0(sw.ElapsedMilliseconds)}ms");
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                new ReportError(_telemetry, TopLeft, module, exception).ShowDialog();
            }
            finally
            {
                ClearProgress();
                RefreshGrid();
                SetButtonStates(true);
            }
        }

        private void ShowProgress(int value, string message)
        {
            ProgressBar.Dispatcher?.Invoke(() => ProgressBar.Value = value, DispatcherPriority.Background);
            ProgressBarMessage.Dispatcher?.Invoke(() => ProgressBarMessage.Text = message, DispatcherPriority.Background);
        }

        private void ClearProgress()
        {
            ProgressBar.Value = 0;
            ProgressBar.Maximum = 100;
            ProgressBarMessage.Text = "";
        }

        private void SetButtonStates(bool state)
        {
            AddButton.IsEnabled = state;
            MetadataButton.IsEnabled = state;
            ImportButton.IsEnabled = state;
            ExportButton.IsEnabled = state;
            CalculateButton.IsEnabled = state;
            Slider.IsEnabled = state;
            ComboBox.IsEnabled = state;

            SearchBox.IsEnabled = state;
            SearchButton.IsEnabled = state;
            ClearButton.IsEnabled = state;

            SubstanceName.IsEnabled = state;
            TaggingControl.IsEnabled = state;
            NamesPanel.IsEnabled = state;

            if (state)
            {
                DeleteButton.IsEnabled = _checkedItems > 0;
                CheckedFilterButton.IsEnabled = _checkedItems > 0;
            }
            else
            {
                DeleteButton.IsEnabled = false;
                CheckedFilterButton.IsEnabled = false;
            }
        }

        private void OnClick_CalculateButton(object sender, RoutedEventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod()?.Name}()";
            _telemetry.Write(module, LoggingLevel.Action, "Triggered");

            int updated = 0;
            int progress = 0;

            try
            {
                SetButtonStates(false);
                Cancel.Visibility = Visibility.Visible;
                Cancel.IsEnabled = true;

                List<ChemistryDataTransferObject> dto = _driver.GetAllChemistry();
                int total = dto.Count;
                if (total > 0)
                {
                    ProgressBar.Maximum = dto.Count;

                    CMLConverter cmlConverter = new CMLConverter();
                    PropertyCalculator pc = new WebServices.PropertyCalculator(Globals.Chem4WordV3.Telemetry,
                                                                               Globals.Chem4WordV3.WordTopLeft,
                                                                               Globals.Chem4WordV3.AddInInfo.AssemblyVersionNumber);

                    ProtocolBufferConverter protocolBufferConverter = new ProtocolBufferConverter();
                    _driver.StartTransaction();

                    foreach (ChemistryDataTransferObject obj in dto)
                    {
                        progress++;
                        ShowProgress(progress, $"Structure #{obj.Id} [{progress}/{total}]");

                        Model model;
                        if (obj.DataType.Equals("cml"))
                        {
                            model = cmlConverter.Import(Encoding.UTF8.GetString(obj.Chemistry));
                        }
                        else
                        {
                            model = protocolBufferConverter.Import(obj.Chemistry);
                        }

                        UpdateModelProperties(model, obj);

                        if (!string.IsNullOrEmpty(Globals.Chem4WordV3.Helper.MachineId))
                        {
                            model.CreatorGuid = Globals.Chem4WordV3.Helper.MachineId;
                        }
                        else
                        {
                            model.CreatorGuid = CoreConstants.DummyMachineGuid;
                        }

                        int changed = pc.CalculateProperties(model, showProgress: false);

                        if (changed > 0)
                        {
                            model.SetAnyMissingNameIds();
                            model.ReLabelGuids();
                            model.Relabel(true);

                            ChemistryDataTransferObject chemistryDataTransferObject = DtoHelper.CreateFromModel(model, obj.DataType);
                            chemistryDataTransferObject.Id = obj.Id;

                            foreach (ChemistryNameDataObject formula in chemistryDataTransferObject.Formulae)
                            {
                                formula.ChemistryId = obj.Id;
                            }

                            foreach (ChemistryNameDataObject name in chemistryDataTransferObject.Names)
                            {
                                name.ChemistryId = obj.Id;
                            }

                            foreach (ChemistryNameDataObject caption in chemistryDataTransferObject.Captions)
                            {
                                caption.ChemistryId = obj.Id;
                            }

                            _driver.UpdateChemistry(chemistryDataTransferObject);

                            updated++;
                        }

                        if (_cancelRequested)
                        {
                            break;
                        }
                    }

                    _driver.CommitTransaction();

                    Cancel.IsEnabled = false;
                    Cancel.Visibility = Visibility.Collapsed;
                    _cancelRequested = false;

                    _telemetry.Write(module, LoggingLevel.Information, $"Updated properties for {updated}/{total} structures");
                }
            }
            catch (Exception exception)
            {
                _driver.RollbackTransaction();
                new ReportError(_telemetry, TopLeft, module, exception).ShowDialog();
            }
            finally
            {
                ClearProgress();
                RefreshGrid();
                SetButtonStates(true);
            }
        }

        private void RefreshGrid()
        {
            using (new WaitCursor())
            {
                // Refresh the control's data
                LibraryEditorViewModel controller = new LibraryEditorViewModel(_telemetry, _driver);
                DataContext = controller;
                ApplySort();
                UpdateStatusBar();
            }
        }

        private void OnClick_ExportButton(object sender, RoutedEventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod()?.Name}()";
            _telemetry.Write(module, LoggingLevel.Action, "Triggered");
            try
            {
                SetButtonStates(false);

                string exportFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                VistaFolderBrowserDialog browser = new VistaFolderBrowserDialog();

                browser.Description = "Select a folder to export your Library's structures as cml files";
                browser.UseDescriptionForTitle = true;
                browser.RootFolder = Environment.SpecialFolder.Desktop;
                browser.ShowNewFolderButton = false;
                browser.SelectedPath = exportFolder;
                DialogResult dr = browser.ShowDialog();
                if (dr == DialogResult.OK)
                {
                    exportFolder = browser.SelectedPath;

                    if (Directory.Exists(exportFolder))
                    {
                        DialogResult doExport = DialogResult.Yes;
                        string[] existingCmlFiles = Directory.GetFiles(exportFolder, "*.cml");
                        if (existingCmlFiles.Length > 0)
                        {
                            StringBuilder sb = new StringBuilder();
                            sb.AppendLine($"This folder contains {existingCmlFiles.Length} cml files.");
                            sb.AppendLine("Do you wish to continue?");
                            doExport = UserInteractions.AskUserYesNo(sb.ToString(), MessageBoxDefaultButton.Button2);
                        }

                        if (doExport == DialogResult.Yes)
                        {
                            int exported = 0;
                            int progress = 0;

                            List<ChemistryDataTransferObject> dto = _driver.GetAllChemistry();
                            int total = dto.Count;
                            if (total > 0)
                            {
                                ProgressBar.Maximum = dto.Count;

                                CMLConverter cmlConverter = new CMLConverter();
                                ProtocolBufferConverter protocolBufferConverter = new ProtocolBufferConverter();

                                foreach (ChemistryDataTransferObject obj in dto)
                                {
                                    progress++;
                                    ShowProgress(progress, $"Structure #{obj.Id} [{progress}/{total}]");

                                    Model model;
                                    if (obj.DataType.Equals("cml"))
                                    {
                                        model = cmlConverter.Import(Encoding.UTF8.GetString(obj.Chemistry));
                                    }
                                    else
                                    {
                                        model = protocolBufferConverter.Import(obj.Chemistry);
                                    }

                                    UpdateModelProperties(model, obj);

                                    string filename = Path.Combine(browser.SelectedPath,
                                                                   FileHelper.SuggestedFileName(
                                                                       new Dictionary<string, string>
                                                                       {
                                                                           { "Id", $"{obj.Id:000000000}" },
                                                                           { "Formula", model.ConciseFormula },
                                                                           { "QuickName", model.SubstanceName },
                                                                       }))
                                                      + ".cml";

                                    model.EnsureBondLength(Globals.Chem4WordV3.SystemOptions.BondLength, false);

                                    string contents = CoreConstants.XmlFileHeader + Environment.NewLine
                                                                           + cmlConverter.Export(model);
                                    File.WriteAllText(filename, contents);
                                    exported++;
                                }
                            }

                            if (exported > 0)
                            {
                                UserInteractions.InformUser($"Exported {exported} structures to {browser.SelectedPath}");
                                _telemetry.Write(module, LoggingLevel.Information, $"Exported {exported} structures from '{_driver.FileName}'");
                            }
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                new ReportError(_telemetry, TopLeft, module, exception).ShowDialog();
            }
            finally
            {
                ClearProgress();
                SetButtonStates(true);
            }
        }

        private void OnTagRemoved_TagControlModel(object sender, WpfEventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod()?.Name}()";
            _telemetry.Write(module, LoggingLevel.Action, "Triggered");
            if (sender is TagControlModel tcm)
            {
                UpdateTags(tcm);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void OnPasteSearchBox(object sender, DataObjectPastingEventArgs e)
        {
            // Stop WPF from doing synchronous UI updates
            e.Handled = true;

            if (sender is TextBox textBox)
            {
                HandlePaste(e, textBox);
            }
        }

        private void OnPasteSubstanceName(object sender, DataObjectPastingEventArgs e)
        {
            // Stop WPF from doing synchronous UI updates
            e.Handled = true;

            if (sender is TextBox textBox)
            {
                HandlePaste(e, textBox);
            }
        }

        private void HandlePaste(DataObjectPastingEventArgs e, TextBox textBox)
        {
            // Extract pasted text
            string pastedText = e.DataObject.GetData(DataFormats.UnicodeText) as string;
            pastedText = pastedText?.Trim();

            if (!string.IsNullOrEmpty(pastedText))
            {
                // Defer the update to avoid dispatcher suspension crash
                Dispatcher.BeginInvoke(
                    DispatcherPriority.Background,
                    new Action(() =>
                               {
                                   textBox.Text = pastedText;
                                   textBox.SelectionLength = pastedText.Length;
                               }));
            }
        }

        private void OnPreviewTextInput_SubstanceName(object sender, TextCompositionEventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod()?.Name}()";

            try
            {
                if (!Regex.IsMatch(e.Text, @"^[\s'a-zA-Z0-9()\[\]/+-.,]*$"))
                {
                    e.Handled = true;
                }
            }
            catch (Exception ex)
            {
                _telemetry.Write(module, LoggingLevel.Exception, ex.Message);
                _telemetry.Write(module, LoggingLevel.ExceptionData, ex.StackTrace);
                new ReportError(_telemetry, TopLeft, "PasteHandler", ex).ShowDialog();
            }
        }

        private void OnSelectedItemChanged(object sender, DataTransferEventArgs e)
        {
            if (DataContext is LibraryEditorViewModel editorViewModel
                && editorViewModel.SelectedChemistryObject != null
                && Globals.Chem4WordV3.SystemOptions != null
                && !string.IsNullOrEmpty(Globals.Chem4WordV3.SystemOptions.SettingsPath))
            {
                string userTagsFile = Path.Combine(Globals.Chem4WordV3.SystemOptions.SettingsPath, UserTagsFileName);
                if (File.Exists(userTagsFile))
                {
                    try
                    {
                        string json = File.ReadAllText(userTagsFile);
                        _userTags = JsonConvert.DeserializeObject<SortedDictionary<string, long>>(json);
                    }
                    catch
                    {
                        _userTags = new SortedDictionary<string, long>();
                    }
                }

                List<string> userTags = _userTags.Select(t => t.Key).ToList();
                List<LibraryTagDataObject> databaseTagsDto = _driver.GetAllTags();
                List<string> databaseTags = databaseTagsDto.Select(t => t.Text).ToList();

                List<string> structureTags = editorViewModel.SelectedChemistryObject.Tags;
                _lastTags = structureTags;
                List<string> availableTags = databaseTags.Union(userTags).Except(structureTags).ToList();

                TaggingControl.TagControlModel = new TagControlModel(new ObservableCollection<string>(availableTags),
                                                                     new ObservableCollection<string>(structureTags),
                                                                     _userTags);
                TaggingControl.DataContext = TaggingControl.TagControlModel;

                TaggingControl.TagControlModel.OnTagRemoved -= OnTagRemoved_TagControlModel;
                TaggingControl.TagControlModel.OnTagRemoved += OnTagRemoved_TagControlModel;
            }
        }

        private void OnLostFocus_TaggingControl(object sender, RoutedEventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod()?.Name}()";

            try
            {
                if (sender is TaggingControl tc
                    && tc.DataContext is TagControlModel tcm)
                {
                    UpdateTags(tcm);
                }
            }
            catch (Exception ex)
            {
                _telemetry.Write(module, LoggingLevel.Exception, ex.Message);
                _telemetry.Write(module, LoggingLevel.ExceptionData, ex.StackTrace);
                new ReportError(_telemetry, TopLeft, "TaggingControlUpdate", ex).ShowDialog();
            }
        }

        // Save the Tags to the database and update the used frequencies
        private void UpdateTags(TagControlModel tcm)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod()?.Name}()";

            // Collate new tags for database
            List<string> tags = new List<string>();
            foreach (TagItem tag in tcm.CurrentTags.OfType<TagItem>())
            {
                tags.Add(tag.ItemLabel.Content as string);
            }

            if (tags.Count > 0)
            {
                // Prevent the files being written any more than necessary
                string lastTags = string.Join(",", _lastTags);
                string currentTags = string.Join(",", tags);

                if (!lastTags.Equals(currentTags)
                    && Globals.Chem4WordV3.SystemOptions != null
                    && !string.IsNullOrEmpty(Globals.Chem4WordV3.SystemOptions.SettingsPath))
                {
                    // Save the updated user file
                    string userTagsFile = Path.Combine(Globals.Chem4WordV3.SystemOptions.SettingsPath, UserTagsFileName);
                    string jsonOut = JsonConvert.SerializeObject(_userTags, Formatting.Indented);
                    File.WriteAllText(userTagsFile, jsonOut);
                }
            }

            if (DataContext is LibraryEditorViewModel editorViewModel)
            {
                // Update the database
                Stopwatch sw = new Stopwatch();
                sw.Start();

                _driver.UpsertTags(editorViewModel.SelectedChemistryObject.Id, tags);

                sw.Stop();
                _telemetry.Write(module, LoggingLevel.Timing, $"Updating {tags.Count} tags for Id: {editorViewModel.SelectedChemistryObject.Id} took {SafeDouble.AsString0(sw.ElapsedMilliseconds)}ms");

                // Update the grid view
                editorViewModel.SelectedChemistryObject.Tags = tags;
            }

            _lastTags = tags;
        }

        private void OnClick_CancelButton(object sender, RoutedEventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod()?.Name}()";

            _cancelRequested = true;

            _telemetry.Write(module, LoggingLevel.Information, "Initiating cancel of Property Calculations");
        }

        public void UpdateStatusBar()
        {
            StringBuilder sb = new StringBuilder();
            if (DataContext is LibraryEditorViewModel controller)
            {
                int items = controller.ChemistryItems.Count;

                if (SearchBox.Text.Length == 0)
                {
                    sb.Append($"Showing all {items} items");
                }
                else
                {
                    sb.Append($"Showing {_filteredItems} from {items}");
                }

                if (_checkedItems > 0)
                {
                    sb.Append($" ({_checkedItems} checked)");
                }
                StatusBar.Text = sb.ToString();
            }
            else
            {
                StatusBar.Text = string.Empty;
            }
        }

        private void UpdateModelProperties(Model model, ChemistryObject item)
        {
            model.SubstanceName = item.SubstanceName;

            foreach (string tag in item.Tags)
            {
                model.SubstanceTags.Add(tag);
            }
        }

        private void UpdateModelProperties(Model model, ChemistryDataTransferObject dto)
        {
            model.SubstanceName = dto.SubstanceName;

            foreach (ChemistryTagDataObject tag in dto.Tags)
            {
                model.SubstanceTags.Add(tag.Text);
            }
        }
    }
}
