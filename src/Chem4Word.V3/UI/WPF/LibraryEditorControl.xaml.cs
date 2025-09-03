// ---------------------------------------------------------------------------
//  Copyright (c) 2025, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
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
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using ListBox = System.Windows.Controls.ListBox;
using Size = System.Windows.Size;
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
        }

        public Size ItemSize
        {
            get { return (Size)GetValue(ItemSizeProperty); }
            set { SetValue(ItemSizeProperty, value); }
        }

        public static readonly DependencyProperty ItemSizeProperty =
            DependencyProperty.Register("ItemSize", typeof(Size), typeof(LibraryEditorControl),
                                        new FrameworkPropertyMetadata(Size.Empty,
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
                                        new FrameworkPropertyMetadata(double.NaN,
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
                                        new FrameworkPropertyMetadata(double.NaN,
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
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            _telemetry.Write(module, "Action", "Triggered");

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
                    var id = long.Parse(source.ButtonDetails.Split('=')[1]);
                    var item = viewModel.ChemistryItems.FirstOrDefault(o => o.Id == id);
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
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            _telemetry.Write(module, "Action", "Triggered");

            var editor = Globals.Chem4WordV3.GetEditorPlugIn(Globals.Chem4WordV3.SystemOptions.SelectedEditorPlugIn);
            if (editor != null)
            {
                if (editor.CanEditReactions && editor.CanEditFunctionalGroups && editor.CanEditNestedMolecules)
                {
                    var topLeft = new Point(TopLeft.X + CoreConstants.TopLeftOffset, TopLeft.Y + CoreConstants.TopLeftOffset);
                    editor.TopLeft = topLeft;
                    var beforeCml = item.CmlFromChemistry();
                    editor.Cml = beforeCml;
                    var model = new CMLConverter().Import(beforeCml);
                    var beforeFormula = model.ConciseFormula;
                    if (string.IsNullOrEmpty(beforeCml))
                    {
                        var renderingOptions = new RenderingOptions
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
                    var chemEditorResult = editor.Edit();
                    if (chemEditorResult == DialogResult.OK)
                    {
                        var cmlConverter = new CMLConverter();

                        var afterModel = cmlConverter.Import(editor.Cml);
                        var afterCml = editor.Cml;
                        var afterFormula = afterModel.ConciseFormula;

                        var pc = new WebServices.PropertyCalculator(_telemetry, topLeft, Globals.Chem4WordV3.AddInInfo.AssemblyVersionNumber);
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

                        using (var editLabelsHost =
                               new EditLabelsHost())
                        {
                            editLabelsHost.TopLeft = topLeft;
                            editLabelsHost.Cml = cmlConverter.Export(afterModel);

                            // Show Label Editor
                            var dr = editLabelsHost.ShowDialog();
                            if (dr == DialogResult.OK)
                            {
                                afterModel = cmlConverter.Import(editLabelsHost.Cml);
                                afterCml = cmlConverter.Export(afterModel);
                                afterFormula = afterModel.ConciseFormula;
                            }
                            editLabelsHost.Close();
                        }

                        var dto = DtoHelper.CreateFromModel(afterModel, CoreConstants.DefaultSaveFormat);
                        if (item.Id == 0)
                        {
                            dto.Name = afterModel.QuickName;

                            // Add New Chemistry
                            var newId = _driver.AddChemistry(dto);

                            var viewModel = new LibraryEditorViewModel(_telemetry, _driver);
                            DataContext = viewModel;

                            SelectChangedItem(newId);
                        }
                        else
                        {
                            dto.Id = item.Id;

                            if (afterFormula.Equals(beforeFormula))
                            {
                                dto.Name = item.Name;
                            }
                            else
                            {
                                dto.Name = afterModel.QuickName;
                            }

                            // Update Existing Chemistry
                            _driver.UpdateChemistry(dto);

                            item.Chemistry = afterCml;
                            item.Formula = afterModel.ConciseFormula;
                            item.MolecularWeight = afterModel.MolecularWeight;

                            RefreshItemProperties(item, dto);

                            SelectChangedItem(item.Id);
                        }

                        ApplySort();
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

            // Local Function
            void SelectChangedItem(long itemId)
            {
                var idx = 0;
                if (DataContext is LibraryEditorViewModel viewModel)
                {
                    foreach (var catalogueItem in viewModel.ChemistryItems)
                    {
                        if (catalogueItem.Id == itemId)
                        {
                            CatalogueItems.SelectedIndex = idx;
                            CatalogueItems.ScrollIntoView(catalogueItem);
                            break;
                        }

                        idx++;
                    }
                }
            }
        }

        private static void RefreshItemProperties(ChemistryObject item, ChemistryDataObject dto)
        {
            // Refresh other properties of the item
            item.Chemistry = dto.Chemistry;
            item.ChemicalNames = dto.Names.Select(n => n.Name).Distinct().ToList();
            item.Names = dto.Names;
            item.Formulae = dto.Formulae;
            item.Captions = dto.Captions;
        }

        public void UpdateStatusBar()
        {
            var sb = new StringBuilder();
            if (DataContext is LibraryEditorViewModel controller)
            {
                var items = controller.ChemistryItems.Count;

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

        private void OnValueChanged_Slider(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            var module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod()?.Name}()";
            if (!_isLoading)
            {
                _telemetry.Write(module, "Action", $"Display size changed to {Slider.Value}");
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
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
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
                using (var form = new ReportError(_telemetry, Globals.Chem4WordV3.WordTopLeft, module, ex))
                {
                    form.ShowDialog();
                }
            }
        }

        private void OnSelectionChanged_SortBy(object sender, SelectionChangedEventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                if (!_isLoading)
                {
                    _telemetry.Write(module, "Action", "Triggered");
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
            _telemetry.Write(module, "Action", "Triggered");
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
            _telemetry.Write(module, "Action", "Triggered");
            try
            {
                CheckedFilterButton.IsChecked = false;
                var searchFor = TextHelper.StripControlCharacters(SearchBox.Text).Trim();
                if (!string.IsNullOrWhiteSpace(searchFor)
                    && DataContext != null)
                {
                    _telemetry.Write(module, "Information", $"Filter library by '{searchFor}'");
                    FilterByText(searchFor);
                    UpdateStatusBar();
                }
            }
            catch (Exception ex)
            {
                using (var form = new ReportError(_telemetry, Globals.Chem4WordV3.WordTopLeft, module, ex))
                {
                    form.ShowDialog();
                }
            }
        }

        private void OnClick_CheckedFilterButton(object sender, RoutedEventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod()?.Name}()";
            _telemetry.Write(module, "Action", "Triggered");

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
                using (var form = new ReportError(_telemetry, Globals.Chem4WordV3.WordTopLeft, module, ex))
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

                var args = new WpfEventArgs
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
                var view = (ListCollectionView)CollectionViewSource.GetDefaultView(CatalogueItems.ItemsSource);
                if (view != null)
                {
                    view.SortDescriptions.Clear();

                    var propertyName = selectedItem.Content.ToString();
                    if (!_isLoading)
                    {
                        _telemetry.Write(module, "Action", $"Sorting structures by {propertyName}");
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
            var view = (ListCollectionView)CollectionViewSource.GetDefaultView(CatalogueItems.ItemsSource);
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
            var view = (ListCollectionView)CollectionViewSource.GetDefaultView(CatalogueItems.ItemsSource);
            if (view != null)
            {
                //then try to match part of either its name or an alternative name to the string typed in
                _filteredItems = 0;
                view.Filter = ci =>
                              {
                                  if (ci is ChemistryObject item)
                                  {
                                      var queryString = searchFor.ToUpper();
                                      if (item.Name.ToUpper().Contains(queryString)
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
            var view = (ListCollectionView)CollectionViewSource.GetDefaultView(CatalogueItems.ItemsSource);

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
            var module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod()?.Name}()";
            _telemetry.Write(module, "Action", "Adding new structure");
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
            var module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod()?.Name}()";
            try
            {
                _telemetry.Write(module, "Action", "Editing structure meta data");
                var topLeft = new Point(TopLeft.X + CoreConstants.TopLeftOffset, TopLeft.Y + CoreConstants.TopLeftOffset);

                using (var editLabelsHost =
                       new EditLabelsHost())
                {
                    if (CatalogueItems.SelectedItem is ChemistryObject item)
                    {
                        editLabelsHost.TopLeft = topLeft;
                        editLabelsHost.Cml = item.CmlFromChemistry();

                        // Show Label Editor
                        var dr = editLabelsHost.ShowDialog();
                        if (dr == DialogResult.OK)
                        {
                            var cmlConverter = new CMLConverter();
                            var afterModel = cmlConverter.Import(editLabelsHost.Cml);

                            var dto = DtoHelper.CreateFromModel(afterModel, CoreConstants.DefaultSaveFormat);
                            dto.Id = item.Id;
                            dto.Name = item.Name;

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
            var module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod()?.Name}()";
            _telemetry.Write(module, "Action", "Triggered");

            try
            {
                SetButtonStates(false);

                var sb = new StringBuilder();
                sb.AppendLine("This will delete the selected structures from the Library");
                sb.AppendLine("");
                sb.AppendLine("Do you want to proceed?");
                sb.AppendLine("This cannot be undone.");
                var dialogResult = UserInteractions.AskUserYesNo(sb.ToString(), MessageBoxDefaultButton.Button2);
                if (dialogResult == DialogResult.Yes)
                {
                    _driver.StartTransaction();
                    int progress = 0;

                    var sw = new Stopwatch();
                    sw.Start();

                    try
                    {
                        CatalogueItems.SelectedItem = null;

                        if (DataContext is LibraryEditorViewModel controller)
                        {
                            ProgressBar.Maximum = _checkedItems;

                            var items = controller.ChemistryItems.Where(i => i.IsChecked).ToList();
                            foreach (var item in items)
                            {
                                progress++;
                                ShowProgress(progress, $" [{progress}/{_checkedItems}]");

                                _driver.DeleteChemistryById(item.Id);
                            }
                        }

                        _driver.CommitTransaction();

                        sw.Stop();
                        _telemetry.Write(module, "Timing", $"Delete of {progress} structures from '{_driver.FileName}' took {SafeDouble.AsString0(sw.ElapsedMilliseconds)}ms");

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
            var module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod()?.Name}()";
            _telemetry.Write(module, "Action", "Triggered");
            try
            {
                SetButtonStates(false);

                StringBuilder sb;
                string importFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                if (Directory.Exists(importFolder))
                {
                    var browser = new VistaFolderBrowserDialog();

                    browser.Description = "Select a folder to import cml files from";
                    browser.UseDescriptionForTitle = true;
                    browser.RootFolder = Environment.SpecialFolder.Desktop;
                    browser.ShowNewFolderButton = false;
                    browser.SelectedPath = importFolder;
                    var dr = browser.ShowDialog();

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

                            var sw = new Stopwatch();
                            sw.Start();

                            _driver.StartTransaction();

                            int progress = 0;

                            try
                            {
                                var files = Directory.GetFiles(selectedFolder, "*.cml").ToList();
                                files.AddRange(Directory.GetFiles(selectedFolder, "*.mol"));

                                if (files.Count > 0)
                                {
                                    int total = files.Count;

                                    ProgressBar.Maximum = files.Count;

                                    var cmlConverter = new CMLConverter();
                                    var sdfConvertor = new SdFileConverter();

                                    foreach (string file in files)
                                    {
                                        progress++;
                                        ShowProgress(progress, $" [{progress}/{total}]");

                                        var contents = File.ReadAllText(file);

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
                                                model.RemoveExplicitHydrogens();
                                            }

                                            model.EnsureBondLength(Globals.Chem4WordV3.SystemOptions.BondLength,
                                                                   Globals.Chem4WordV3.SystemOptions.SetBondLengthOnImportFromFile);

                                            model.Relabel(true);

                                            var dto = DtoHelper.CreateFromModel(model, CoreConstants.DefaultSaveFormat);
                                            _driver.AddChemistry(dto);
                                        }

                                        fileCount++;
                                    }

                                    File.WriteAllText(doneFile, $"{fileCount} cml files imported into library");
                                    FileInfo fi = new FileInfo(doneFile);
                                    fi.Attributes = FileAttributes.Hidden;
                                    _telemetry.Write(module, "Information", $"Imported {fileCount} structures into '{_driver.FileName}'");
                                    _driver.CommitTransaction();
                                }
                            }
                            catch (Exception exception)
                            {
                                _driver.RollbackTransaction();
                                new ReportError(_telemetry, TopLeft, module, exception).ShowDialog();
                            }

                            sw.Stop();

                            _telemetry.Write(module, "Timing", $"Import of {progress} files took {SafeDouble.AsString0(sw.ElapsedMilliseconds)}ms");
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
            var module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod()?.Name}()";
            _telemetry.Write(module, "Action", "Triggered");

            int updated = 0;
            int progress = 0;

            try
            {
                SetButtonStates(false);
                Cancel.Visibility = Visibility.Visible;
                Cancel.IsEnabled = true;

                var dto = _driver.GetAllChemistry();
                int total = dto.Count;
                if (total > 0)
                {
                    ProgressBar.Maximum = dto.Count;

                    var cmlConverter = new CMLConverter();
                    var pc = new WebServices.PropertyCalculator(Globals.Chem4WordV3.Telemetry,
                                                                Globals.Chem4WordV3.WordTopLeft,
                                                                Globals.Chem4WordV3.AddInInfo.AssemblyVersionNumber);

                    var protocolBufferConverter = new ProtocolBufferConverter();
                    _driver.StartTransaction();

                    foreach (var obj in dto)
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

                        if (!string.IsNullOrEmpty(Globals.Chem4WordV3.Helper.MachineId))
                        {
                            model.CreatorGuid = Globals.Chem4WordV3.Helper.MachineId;
                        }
                        else
                        {
                            model.CreatorGuid = CoreConstants.DummyMachineGuid;
                        }

                        var changed = pc.CalculateProperties(model, showProgress: false);

                        if (changed > 0)
                        {
                            model.SetAnyMissingNameIds();
                            model.ReLabelGuids();
                            model.Relabel(true);

                            var chemistryDataObject = DtoHelper.CreateFromModel(model, obj.DataType);
                            chemistryDataObject.Id = obj.Id;

                            foreach (var formula in chemistryDataObject.Formulae)
                            {
                                formula.ChemistryId = obj.Id;
                            }

                            foreach (var name in chemistryDataObject.Names)
                            {
                                name.ChemistryId = obj.Id;
                            }

                            foreach (var caption in chemistryDataObject.Captions)
                            {
                                caption.ChemistryId = obj.Id;
                            }

                            _driver.UpdateChemistry(chemistryDataObject);

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

                    _telemetry.Write(module, "Information", $"Updated properties for {updated}/{total} structures");
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
                var controller = new LibraryEditorViewModel(_telemetry, _driver);
                DataContext = controller;
                ApplySort();
                UpdateStatusBar();
            }
        }

        private void OnClick_ExportButton(object sender, RoutedEventArgs e)
        {
            var module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod()?.Name}()";
            _telemetry.Write(module, "Action", "Triggered");
            try
            {
                SetButtonStates(false);

                string exportFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                var browser = new VistaFolderBrowserDialog();

                browser.Description = "Select a folder to export your Library's structures as cml files";
                browser.UseDescriptionForTitle = true;
                browser.RootFolder = Environment.SpecialFolder.Desktop;
                browser.ShowNewFolderButton = false;
                browser.SelectedPath = exportFolder;
                var dr = browser.ShowDialog();
                if (dr == DialogResult.OK)
                {
                    exportFolder = browser.SelectedPath;

                    if (Directory.Exists(exportFolder))
                    {
                        var doExport = DialogResult.Yes;
                        var existingCmlFiles = Directory.GetFiles(exportFolder, "*.cml");
                        if (existingCmlFiles.Length > 0)
                        {
                            var sb = new StringBuilder();
                            sb.AppendLine($"This folder contains {existingCmlFiles.Length} cml files.");
                            sb.AppendLine("Do you wish to continue?");
                            doExport = UserInteractions.AskUserYesNo(sb.ToString(), MessageBoxDefaultButton.Button2);
                        }

                        if (doExport == DialogResult.Yes)
                        {
                            int exported = 0;
                            int progress = 0;

                            var dto = _driver.GetAllChemistry();
                            int total = dto.Count;
                            if (total > 0)
                            {
                                ProgressBar.Maximum = dto.Count;

                                var cmlConverter = new CMLConverter();
                                var protocolBufferConverter = new ProtocolBufferConverter();

                                foreach (var obj in dto)
                                {
                                    progress++;
                                    ShowProgress(progress, $"Structure #{obj.Id} [{progress}/{total}]");

                                    var filename = Path.Combine(browser.SelectedPath, $"Chem4Word-{obj.Id:000000000}.cml");
                                    Model model;
                                    if (obj.DataType.Equals("cml"))
                                    {
                                        model = cmlConverter.Import(Encoding.UTF8.GetString(obj.Chemistry));
                                    }
                                    else
                                    {
                                        model = protocolBufferConverter.Import(obj.Chemistry);
                                    }

                                    model.EnsureBondLength(Globals.Chem4WordV3.SystemOptions.BondLength, false);

                                    var contents = CoreConstants.XmlFileHeader + Environment.NewLine
                                                                           + cmlConverter.Export(model);
                                    File.WriteAllText(filename, contents);
                                    exported++;
                                }
                            }

                            if (exported > 0)
                            {
                                UserInteractions.InformUser($"Exported {exported} structures to {browser.SelectedPath}");
                                _telemetry.Write(module, "Information", $"Exported {exported} structures from '{_driver.FileName}'");
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
            _telemetry.Write(module, "Action", "Triggered");
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

        private void OnPreviewTextInput_SubstanceName(object sender, TextCompositionEventArgs e)
        {
            if (!Regex.IsMatch(e.Text, @"^[\s'a-zA-Z0-9()\[\]/+-.,]*$"))
            {
                e.Handled = true;
            }
        }

        private void OnSelectedItemChanged(object sender, DataTransferEventArgs e)
        {
            if (DataContext is LibraryEditorViewModel dc
                && dc.SelectedChemistryObject != null
                && Globals.Chem4WordV3.SystemOptions != null
                && !string.IsNullOrEmpty(Globals.Chem4WordV3.SystemOptions.SettingsPath))
            {
                var userTagsFile = Path.Combine(Globals.Chem4WordV3.SystemOptions.SettingsPath, UserTagsFileName);
                if (File.Exists(userTagsFile))
                {
                    try
                    {
                        var json = File.ReadAllText(userTagsFile);
                        _userTags = JsonConvert.DeserializeObject<SortedDictionary<string, long>>(json);
                    }
                    catch
                    {
                        _userTags = new SortedDictionary<string, long>();
                    }
                }

                var userTags = _userTags.Select(t => t.Key).ToList();
                var databaseTagsDto = _driver.GetAllTags();
                var databaseTags = databaseTagsDto.Select(t => t.Text).ToList();

                var structureTags = dc.SelectedChemistryObject.Tags;
                _lastTags = structureTags;
                var availableTags = databaseTags.Union(userTags).Except(structureTags).ToList();

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
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            if (sender is TaggingControl tc
                && tc.DataContext is TagControlModel tcm)
            {
                UpdateTags(tcm);
            }
        }

        // Save the Tags to the database and update the used frequencies
        private void UpdateTags(TagControlModel tcm)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            // Collate new tags for database
            List<string> tags = new List<string>();
            foreach (var tag in tcm.CurrentTags.OfType<TagItem>())
            {
                tags.Add(tag.ItemLabel.Content as string);
            }

            // Prevent the database and files being written any more than necessary
            string lt = string.Join(",", _lastTags);
            string tt = string.Join(",", tags);
            if (!lt.Equals(tt)
                && Globals.Chem4WordV3.SystemOptions != null
                && !string.IsNullOrEmpty(Globals.Chem4WordV3.SystemOptions.SettingsPath))
            {
                // Save the updated user file
                var userTagsFile = Path.Combine(Globals.Chem4WordV3.SystemOptions.SettingsPath, UserTagsFileName);
                var jsonOut = JsonConvert.SerializeObject(_userTags, Formatting.Indented);
                File.WriteAllText(userTagsFile, jsonOut);

                // Update the database
                var sw = new Stopwatch();
                sw.Start();

                var dc = DataContext as LibraryEditorViewModel;
                _driver.AddTags(dc.SelectedChemistryObject.Id, tags);

                sw.Stop();
                _telemetry.Write(module, "Timing", $"Writing {tags.Count} tags took {SafeDouble.AsString0(sw.ElapsedMilliseconds)}ms");

                // Update the grid view
                dc.SelectedChemistryObject.Tags = tags;

                _lastTags = tags;
            }
        }

        private bool _cancelRequested = false;

        private void OnClick_CancelButton(object sender, RoutedEventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            _cancelRequested = true;

            _telemetry.Write(module, "Information", "Initiating cancel of Property Calculations");
        }
    }
}