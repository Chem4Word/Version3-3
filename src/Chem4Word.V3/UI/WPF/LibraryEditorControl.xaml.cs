// ---------------------------------------------------------------------------
//  Copyright (c) 2022, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.ACME;
using Chem4Word.ACME.Annotations;
using Chem4Word.ACME.Controls;
using Chem4Word.ACME.Models;
using Chem4Word.ACME.Models.Chem4Word.Controls.TagControl;
using Chem4Word.ACME.Utils;
using Chem4Word.Core;
using Chem4Word.Core.Helpers;
using Chem4Word.Core.UI.Forms;
using Chem4Word.Core.UI.Wpf;
using Chem4Word.Model2;
using Chem4Word.Model2.Converters.CML;
using Chem4Word.Model2.Converters.MDL;
using IChem4Word.Contracts;
using IChem4Word.Contracts.Dto;
using Newtonsoft.Json;
using Ookii.Dialogs.WinForms;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;
using Forms = System.Windows.Forms;
using Size = System.Windows.Size;

namespace Chem4Word.UI.WPF
{
    /// <summary>
    /// Interaction logic for CatalogueControl.xaml
    /// </summary>
    public partial class LibraryEditorControl : UserControl
    {
        private static string _product = Assembly.GetExecutingAssembly().FullName.Split(',')[0];
        private static string _class = MethodBase.GetCurrentMethod().DeclaringType?.Name;

        private IChem4WordTelemetry _telemetry;
        private IChem4WordDriver _driver;
        private AcmeOptions _acmeOptions;

        private int _filteredItems;
        private int _checkedItems;

        private List<string> _lastTags = new List<string>();

        private const string UserTagsFileName = "MyTags.json";
        private SortedDictionary<string, long> _userTags = new SortedDictionary<string, long>();

        public Point TopLeft { get; set; }

        public LibraryEditorControl()
        {
            InitializeComponent();
        }

        public bool ShowAllCarbonAtoms => _acmeOptions.ShowCarbons;
        public bool ShowImplicitHydrogens => _acmeOptions.ShowHydrogens;
        public bool ShowAtomsInColour => _acmeOptions.ColouredAtoms;
        public bool ShowMoleculeGrouping => _acmeOptions.ShowMoleculeGrouping;

        public Size ItemSize
        {
            get { return (Size)GetValue(ItemSizeProperty); }
            set { SetValue(ItemSizeProperty, value); }
        }

        // Using a DependencyProperty as the backing store for MyProperty.  This enables animation, styling, binding, etc...
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

        // Using a DependencyProperty as the backing store for MyProperty.  This enables animation, styling, binding, etc...
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

        // Using a DependencyProperty as the backing store for MyProperty.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty DisplayHeightProperty =
            DependencyProperty.Register("DisplayHeight", typeof(double), typeof(LibraryEditorControl),
                                        new FrameworkPropertyMetadata(double.NaN,
                                                                      FrameworkPropertyMetadataOptions.AffectsRender
                                                                      | FrameworkPropertyMetadataOptions.AffectsArrange
                                                                      | FrameworkPropertyMetadataOptions.AffectsMeasure));

        public void SetOptions(IChem4WordTelemetry telemetry, AcmeOptions acmeOptions, IChem4WordDriver driver)
        {
            _acmeOptions = acmeOptions;
            _telemetry = telemetry;
            _driver = driver;
        }

        private void OnChemistryItem_ButtonClick(object sender, RoutedEventArgs e)
        {
            if (e.OriginalSource is WpfEventArgs source
                && DataContext is LibaryEditorViewModel controller)
            {
                Debug.WriteLine($"{_class} -> {source.Button} {source.OutputValue}");

                if (source.Button.StartsWith("CheckBox"))
                {
                    _checkedItems = controller.ChemistryItems.Count(i => i.IsChecked);

                    TrashButton.IsEnabled = _checkedItems > 0;
                    CheckedFilterButton.IsEnabled = _checkedItems > 0;

                    UpdateStatusBar();
                }

                if (source.Button.StartsWith("DisplayDoubleClick"))
                {
                    var id = long.Parse(source.OutputValue.Split('=')[1]);
                    var item = controller.ChemistryItems.FirstOrDefault(o => o.Id == id);
                    if (item != null)
                    {
                        var topLeft = new Point(TopLeft.X + Constants.TopLeftOffset, TopLeft.Y + Constants.TopLeftOffset);
                        var result = UIUtils.ShowSketcher(_acmeOptions, _telemetry, topLeft, item.Cml);
                        if (result.IsDirty)
                        {
                            var dto = new ChemistryDataObject();
                            dto.Id = id;
                            dto.Chemistry = Encoding.UTF8.GetBytes(result.Cml);
                            dto.DataType = "cml";
                            dto.Name = item.Name;
                            dto.Formula = result.Formua;
                            dto.MolWeight = result.MolecularWeight;
                            _driver.UpdateChemistry(dto);

                            item.Cml = result.Cml;
                            item.Formula = result.Formua;
                            item.MolecularWeight = result.MolecularWeight;

                            controller.SelectedChemistryObject = item;
                        }
                    }
                }
            }
        }

        public void UpdateStatusBar()
        {
            var sb = new StringBuilder();
            if (DataContext is LibaryEditorViewModel controller)
            {
                var items = controller.ChemistryItems.Count;

                if (_filteredItems == 0)
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
                using (var form = new ReportError(Globals.Chem4WordV3.Telemetry, Globals.Chem4WordV3.WordTopLeft, module, ex))
                {
                    form.ShowDialog();
                }
            }
        }

        private void OnSelectionChanged_Selector(object sender, SelectionChangedEventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
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
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
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
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                CheckedFilterButton.IsChecked = false;
                if (!string.IsNullOrWhiteSpace(SearchBox.Text)
                    && DataContext != null)
                {
                    FilterByText();
                    UpdateStatusBar();
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

        private void OnClick_CheckedFilterButton(object sender, RoutedEventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
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
                using (var form = new ReportError(Globals.Chem4WordV3.Telemetry, Globals.Chem4WordV3.WordTopLeft, module, ex))
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
                && source.DataContext is LibaryEditorViewModel context)
            {
                context.SelectedChemistryObject = selected;
                _lastTags = context.SelectedChemistryObject.Tags;

                var args = new WpfEventArgs
                {
                    Button = "CatalogueView|SelectedItemChanged",
                    OutputValue = $"Id={selected.Id}"
                };
                OnSelectionChange?.Invoke(this, args);
            }
        }

        private class ChemistryObjectComparer : IComparer<ChemistryObject>
        {
            public int Compare(ChemistryObject x, ChemistryObject y)
                => string.CompareOrdinal(x?.Name, y?.Name);
        }

        private void ApplySort()
        {
            if (ComboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                var view = (ListCollectionView)CollectionViewSource.GetDefaultView(CatalogueItems.ItemsSource);
                view.SortDescriptions.Clear();

                var propertyName = selectedItem.Content.ToString();
                if (propertyName.Equals("Name"))
                {
                    view.CustomSort = (IComparer)new ChemistryObjectComparer();
                }
                else
                {
                    view.SortDescriptions.Add(new SortDescription(propertyName, ListSortDirection.Ascending));
                }
            }
        }

        private void ClearFilter()
        {
            //get the view from the listbox's source
            var view = (ListCollectionView)CollectionViewSource.GetDefaultView(CatalogueItems.ItemsSource);

            _filteredItems = 0;
            view.Filter = null;
        }

        private void FilterByText()
        {
            //get the view from the listbox's source
            var view = (ListCollectionView)CollectionViewSource.GetDefaultView(CatalogueItems.ItemsSource);

            //then try to match part of either its name or an alternative name to the string typed in
            _filteredItems = 0;
            view.Filter = ci =>
                          {
                              var item = ci as ChemistryObject;
                              var queryString = SearchBox.Text.ToUpper();
                              if (item != null
                                  && (item.Name.ToUpper().Contains(queryString)
                                      || item.OtherNames.Any(n => n.ToUpper().Contains(queryString))
                                      || item.Tags.Any(n => n.ToUpper().Contains(queryString)))
                              )
                              {
                                  _filteredItems++;
                                  return true;
                              }

                              return false;
                          };
        }

        private void FilterByChecked()
        {
            //get the view from the listbox's source
            var view = (ListCollectionView)CollectionViewSource.GetDefaultView(CatalogueItems.ItemsSource);

            //then try to match part of either its name or an alternative name to the string typed in
            _filteredItems = 0;
            view.Filter = ci =>
                          {
                              var item = ci as ChemistryObject;
                              if (item != null
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
            try
            {
                // ToDo: Implement
                Debug.WriteLine($"{_class} -> Add Button Clicked");
            }
            catch (Exception exception)
            {
                new ReportError(Globals.Chem4WordV3.Telemetry, TopLeft, module, exception).ShowDialog();
            }
        }

        private void OnClick_TrashButton(object sender, RoutedEventArgs e)
        {
            var module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod()?.Name}()";
            Globals.Chem4WordV3.Telemetry.Write(module, "Action", "Triggered");

            try
            {
                // ToDo: Should only delete selected structures ...

                var sb = new StringBuilder();
                sb.AppendLine("This will delete all the selected structures from the Library");
                sb.AppendLine("");
                sb.AppendLine("Do you want to proceed?");
                sb.AppendLine("This cannot be undone.");
                var dialogResult = UserInteractions.AskUserYesNo(sb.ToString(), Forms.MessageBoxDefaultButton.Button2);
                if (dialogResult == Forms.DialogResult.Yes)
                {
                    // ToDo: Backup the file
                    _driver.DeleteAllChemistry(); // ToDo: This is currently wrong

                    // Refresh the control's data
                    var controller = new LibaryEditorViewModel(_telemetry, _driver);
                    DataContext = controller;
                    UpdateStatusBar();
                }
            }
            catch (Exception exception)
            {
                new ReportError(Globals.Chem4WordV3.Telemetry, TopLeft, module, exception).ShowDialog();
            }
        }

        private void OnClick_ImportButton(object sender, RoutedEventArgs e)
        {
            var module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod()?.Name}()";
            try
            {
                StringBuilder sb;
                string importFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                if (Directory.Exists(importFolder))
                {
                    // Fix scrolling to selected item by using code from https://social.msdn.microsoft.com/Forums/expression/en-US/1257aebc-22a6-44f6-975b-74f5067728bc/autoposition-showfolder-dialog?forum=vbgeneral

                    var browser = new VistaFolderBrowserDialog();

                    browser.Description = "Select a folder to import cml files from";
                    browser.UseDescriptionForTitle = true;
                    browser.RootFolder = Environment.SpecialFolder.Desktop;
                    browser.ShowNewFolderButton = false;
                    browser.SelectedPath = importFolder;
                    var dr = browser.ShowDialog();

                    if (dr == Forms.DialogResult.OK)
                    {
                        string selectedFolder = browser.SelectedPath;
                        string doneFile = Path.Combine(selectedFolder, "library-import-done.txt");

                        sb = new StringBuilder();
                        sb.AppendLine("Do you want to import the Gallery structures into the Library?");
                        dr = UserInteractions.AskUserYesNo(sb.ToString());
                        if (dr == Forms.DialogResult.Yes
                            && File.Exists(doneFile))
                        {
                            sb = new StringBuilder();
                            sb.AppendLine($"Files have been imported already from '{selectedFolder}'");
                            sb.AppendLine("Do you want to rerun the import?");
                            dr = UserInteractions.AskUserYesNo(sb.ToString());
                            if (dr == Forms.DialogResult.Yes)
                            {
                                File.Delete(doneFile);
                            }
                        }

                        if (dr == Forms.DialogResult.Yes)
                        {
                            int fileCount = 0;

                            // ToDo: Figure out how to implement transactions
                            //_driver.StartTransaction();

                            try
                            {
                                var files = Directory.GetFiles(selectedFolder, "*.cml").ToList();
                                files.AddRange(Directory.GetFiles(selectedFolder, "*.mol"));

                                if (files.Count > 0)
                                {
                                    int progress = 0;
                                    int total = files.Count;

                                    ProgressBar.Maximum = files.Count;

                                    var cmlConverter = new CMLConverter();
                                    var sdfConvertor = new SdFileConverter();
                                    var model = new Model();

                                    foreach (string file in files)
                                    {
                                        progress++;
                                        ShowProgress(progress, $" [{progress}/{total}]");

                                        var contents = File.ReadAllText(file);

                                        if (contents.StartsWith("<"))
                                        {
                                            model = cmlConverter.Import(contents);
                                        }
                                        else
                                        {
                                            model = sdfConvertor.Import(contents);
                                        }

                                        string cml = cmlConverter.Export(model, compressed: true);
                                        var dto = new ChemistryDataObject
                                        {
                                            Chemistry = Encoding.UTF8.GetBytes(cml),
                                            DataType = "cml",
                                            Name = model.QuickName,
                                            Formula = model.ConciseFormula,
                                            MolWeight = model.MolecularWeight
                                        };

                                        _driver.AddChemistry(dto);
                                        Debug.WriteLine($" [{progress}/{total}]");
                                        fileCount++;
                                    }

                                    File.WriteAllText(doneFile, $"{fileCount} cml files imported into library");
                                    FileInfo fi = new FileInfo(doneFile);
                                    fi.Attributes = FileAttributes.Hidden;

                                    // ToDo: Figure out how to implement transactions
                                    //_driver.EndTransaction(false);
                                }
                            }
                            catch (Exception exception)
                            {
                                // ToDo: Figure out how to implement transactions
                                //_driver.EndTransaction(true);
                                new ReportError(Globals.Chem4WordV3.Telemetry, TopLeft, module, exception).ShowDialog();
                            }

                            ClearProgress();

                            // Refresh the control's data
                            var controller = new LibaryEditorViewModel(_telemetry, _driver);
                            DataContext = controller;
                            UpdateStatusBar();
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                new ReportError(Globals.Chem4WordV3.Telemetry, TopLeft, module, exception).ShowDialog();
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
            ProgressBar.Maximum = 0;
            ProgressBarMessage.Text = "";
        }

        private void OnClick_ExportButton(object sender, RoutedEventArgs e)
        {
            var module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod()?.Name}()";
            try
            {
                // Fix scrolling to selected item by using code from https://social.msdn.microsoft.com/Forums/expression/en-US/1257aebc-22a6-44f6-975b-74f5067728bc/autoposition-showfolder-dialog?forum=vbgeneral

                string exportFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                var browser = new VistaFolderBrowserDialog();

                browser.Description = "Select a folder to export your Library's structures as cml files";
                browser.UseDescriptionForTitle = true;
                browser.RootFolder = Environment.SpecialFolder.Desktop;
                browser.ShowNewFolderButton = false;
                browser.SelectedPath = exportFolder;
                var dr = browser.ShowDialog();
                if (dr == Forms.DialogResult.OK)
                {
                    exportFolder = browser.SelectedPath;

                    if (Directory.Exists(exportFolder))
                    {
                        var doExport = Forms.DialogResult.Yes;
                        var existingCmlFiles = Directory.GetFiles(exportFolder, "*.cml");
                        if (existingCmlFiles.Length > 0)
                        {
                            var sb = new StringBuilder();
                            sb.AppendLine($"This folder contains {existingCmlFiles.Length} cml files.");
                            sb.AppendLine("Do you wish to continue?");
                            doExport = UserInteractions.AskUserYesNo(sb.ToString(), Forms.MessageBoxDefaultButton.Button2);
                        }

                        if (doExport == Forms.DialogResult.Yes)
                        {
                            int exported = 0;
                            int progress = 0;

                            var dto = _driver.GetAllChemistry();
                            int total = dto.Count;
                            if (total > 0)
                            {
                                ProgressBar.Maximum = dto.Count;

                                var converter = new CMLConverter();
                                foreach (var obj in dto)
                                {
                                    progress++;
                                    ShowProgress(progress, $"Structure #{obj.Id} [{progress}/{total}]");

                                    var filename = Path.Combine(browser.SelectedPath, $"Chem4Word-{obj.Id:000000000}.cml");
                                    var model = converter.Import(Encoding.UTF8.GetString(obj.Chemistry));
                                    model.EnsureBondLength(Globals.Chem4WordV3.SystemOptions.BondLength, false);

                                    var contents = Constants.XmlFileHeader + Environment.NewLine + converter.Export(model);
                                    File.WriteAllText(filename, contents);
                                    exported++;
                                }

                                ClearProgress();
                            }

                            if (exported > 0)
                            {
                                UserInteractions.InformUser($"Exported {exported} structures to {browser.SelectedPath}");
                            }
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                new ReportError(Globals.Chem4WordV3.Telemetry, TopLeft, module, exception).ShowDialog();
            }
        }

        private void OnTagRemoved_TagControlModel(object sender, WpfEventArgs e)
        {
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

        private void OnSelectedItemChanged(object sender, DataTransferEventArgs e)
        {
            if (DataContext is LibaryEditorViewModel dc
                && dc.SelectedChemistryObject != null)
            {
                // ToDo: Handle null SettingsPath
                var userTagsFile = Path.Combine(_acmeOptions.SettingsPath, UserTagsFileName);
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
            if (!lt.Equals(tt))
            {
                // Save the updated user file
                // ToDo: Handle _acmeOptions.SettingsPath is null
                var userTagsFile = Path.Combine(_acmeOptions.SettingsPath, UserTagsFileName);
                var jsonOut = JsonConvert.SerializeObject(_userTags, Formatting.Indented);
                File.WriteAllText(userTagsFile, jsonOut);

                // Update the database
                var sw = new Stopwatch();
                sw.Start();

                var dc = DataContext as LibaryEditorViewModel;
                _driver.AddTags(dc.SelectedChemistryObject.Id, tags);

                sw.Stop();
                _telemetry.Write(module, "Timing", $"Writing {tags.Count} tags took {SafeDouble.AsString(sw.ElapsedMilliseconds)}ms");

                // Update the grid view
                dc.SelectedChemistryObject.Tags = tags;

                _lastTags = tags;
            }
        }
    }
}