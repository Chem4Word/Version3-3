// ---------------------------------------------------------------------------
//  Copyright (c) 2022, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.Core;
using Chem4Word.Core.Helpers;
using Chem4Word.Core.UI.Forms;
using Chem4Word.Core.UI.Wpf;
using Chem4Word.Helpers;
using IChem4Word.Contracts;
using Ookii.Dialogs.WinForms;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Forms = System.Windows.Forms;
using Point = System.Windows.Point;
using UserControl = System.Windows.Controls.UserControl;

namespace Chem4Word.UI.WPF
{
    /// <summary>
    /// Interaction logic for SettingsControl.xaml
    /// </summary>
    public partial class SettingsControl : UserControl
    {
        private static string _product = Assembly.GetExecutingAssembly().FullName.Split(',')[0];
        private static string _class = MethodBase.GetCurrentMethod().DeclaringType?.Name;

        public event EventHandler OnButtonClick;

        public Chem4WordOptions SystemOptions { get; set; }
        public Point TopLeft { get; set; }
        public bool Dirty { get; set; }

        private bool _loading;
        private string _selectedLibrary;

        public SettingsControl()
        {
            _loading = true;

            InitializeComponent();
        }

        #region Form Load

        private void SettingsControl_OnLoaded(object sender, RoutedEventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            #region Load Images

            // Tab 1 - Plug Ins
            var imageStream = ResourceHelper.GetBinaryResource(Assembly.GetExecutingAssembly(), "Preferences.png");
            if (imageStream != null)
            {
                var bitmap = CreateImageFromStream(imageStream);

                EditorSettingsButtonImage.Source = bitmap;
                RendererSettingsButtonImage.Source = bitmap;
                SearcherSettingsButtonImage.Source = bitmap;
            }

            // Tab 5 Maintenance
            imageStream = ResourceHelper.GetBinaryResource(Assembly.GetExecutingAssembly(), "File-Open.png");
            if (imageStream != null)
            {
                var bitmap = CreateImageFromStream(imageStream);
                LibraryFolderButtonImage.Source = bitmap;
                SettingsFolderButtonImage.Source = bitmap;
                PlugInsFolderButtonImage.Source = bitmap;
            }

            #endregion Load Images

            #region Set Current Values

            if (SystemOptions != null)
            {
                LoadSettings();
            }

            #endregion Set Current Values

            _loading = false;
        }

        #endregion Form Load

        #region Bottom Buttons

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            Globals.Chem4WordV3.Telemetry.Write(module, "Action", "Triggered");

            WpfEventArgs args = new WpfEventArgs();
            args.Button = "Ok";
            args.OutputValue = "";

            OnButtonClick?.Invoke(this, args);
        }

        private void CancelButton_OnClick(object sender, RoutedEventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            Globals.Chem4WordV3.Telemetry.Write(module, "Action", "Triggered");

            WpfEventArgs args = new WpfEventArgs();
            args.Button = "Cancel";
            args.OutputValue = "";

            OnButtonClick?.Invoke(this, args);
        }

        private void DefaultsButton_OnClick(object sender, RoutedEventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            Globals.Chem4WordV3.Telemetry.Write(module, "Action", "Triggered");

            try
            {
                Forms.DialogResult dr = UserInteractions.AskUserOkCancel("Restore default settings");
                if (dr == Forms.DialogResult.OK)
                {
                    _loading = true;
                    Dirty = true;
                    SystemOptions.RestoreDefaults();
                    LoadSettings();
                    _loading = false;
                }
            }
            catch (Exception ex)
            {
                new ReportError(Globals.Chem4WordV3.Telemetry, TopLeft, module, ex).ShowDialog();
            }
        }

        #endregion Bottom Buttons

        #region Plug-Ins Tab Events

        private void SelectedEditorSettings_OnClick(object sender, RoutedEventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            Globals.Chem4WordV3.Telemetry.Write(module, "Action", "Triggered");

            var editor = Globals.Chem4WordV3.GetEditorPlugIn(SelectEditorPlugIn.SelectedItem.ToString());
            editor.SettingsPath = Globals.Chem4WordV3.AddInInfo.ProductAppDataPath;
            editor.ChangeSettings(new Point(SystemOptions.WordTopLeft.X + Constants.TopLeftOffset * 2, SystemOptions.WordTopLeft.Y + Constants.TopLeftOffset * 2));
        }

        private void SelectedRendererSettings_OnClick(object sender, RoutedEventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            Globals.Chem4WordV3.Telemetry.Write(module, "Action", "Triggered");

            var renderer = Globals.Chem4WordV3.GetRendererPlugIn(SelectRendererPlugIn.SelectedItem.ToString());
            renderer.SettingsPath = Globals.Chem4WordV3.AddInInfo.ProductAppDataPath;
            renderer.ChangeSettings(new Point(SystemOptions.WordTopLeft.X + Constants.TopLeftOffset * 2, SystemOptions.WordTopLeft.Y + Constants.TopLeftOffset * 2));
        }

        private void SelectedSearcherSettings_OnClick(object sender, RoutedEventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            Globals.Chem4WordV3.Telemetry.Write(module, "Action", "Triggered");

            var searcher = Globals.Chem4WordV3.GetSearcherPlugIn(SelectSearcherPlugIn.SelectedItem.ToString());
            searcher.SettingsPath = Globals.Chem4WordV3.AddInInfo.ProductAppDataPath;
            searcher.ChangeSettings(new Point(SystemOptions.WordTopLeft.X + Constants.TopLeftOffset * 2, SystemOptions.WordTopLeft.Y + Constants.TopLeftOffset * 2));
        }

        private void SelectEditorPlugIn_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            if (!_loading)
            {
                Globals.Chem4WordV3.Telemetry.Write(module, "Action", "Triggered");

                var pci = SelectEditorPlugIn.SelectedItem as PlugInComboItem;
                SystemOptions.SelectedEditorPlugIn = pci?.Name;
                SelectedEditorPlugInDescription.Text = pci?.Description;
                var editor = Globals.Chem4WordV3.GetEditorPlugIn(pci.Name);
                SelectedEditorSettings.IsEnabled = editor.HasSettings;

                Dirty = true;
            }
        }

        private void SelectRenderer_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            if (!_loading)
            {
                Globals.Chem4WordV3.Telemetry.Write(module, "Action", "Triggered");

                var pci = SelectRendererPlugIn.SelectedItem as PlugInComboItem;
                SystemOptions.SelectedRendererPlugIn = pci?.Name;
                SelectedRendererDescription.Text = pci?.Description;
                var renderer = Globals.Chem4WordV3.GetRendererPlugIn(pci.Name);
                SelectedRendererSettings.IsEnabled = renderer.HasSettings;

                Dirty = true;
            }
        }

        private void SelectSearcher_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            if (!_loading)
            {
                Globals.Chem4WordV3.Telemetry.Write(module, "Action", "Triggered");

                var pci = SelectSearcherPlugIn.SelectedItem as PlugInComboItem;
                SelectedSearcherDescription.Text = pci?.Description;
                var searcher = Globals.Chem4WordV3.GetSearcherPlugIn(pci.Name);
                SelectedSearcherSettings.IsEnabled = searcher.HasSettings;

                Dirty = true;
            }
        }

        #endregion Plug-Ins Tab Events

        #region General Tab Events

        private void BondLength_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            if (!_loading)
            {
                Globals.Chem4WordV3.Telemetry.Write(module, "Action", "Triggered");

                var cbi = DefaultBondLength.SelectedItem as ComboBoxItem;
                SystemOptions.BondLength = int.Parse((string)cbi.Tag);

                Dirty = true;
            }
        }

        private void RemoveExplicitOnImportFile_OnClick(object sender, RoutedEventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            Globals.Chem4WordV3.Telemetry.Write(module, "Action", "Triggered");
            SystemOptions.RemoveExplicitHydrogensOnImportFromFile = RemoveExplicitOnImportFile.IsChecked.Value;
            Dirty = true;
        }

        private void RemoveExplicitOnImportSearch_OnClick(object sender, RoutedEventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            Globals.Chem4WordV3.Telemetry.Write(module, "Action", "Triggered");
            SystemOptions.RemoveExplicitHydrogensOnImportFromSearch = RemoveExplicitOnImportSearch.IsChecked.Value;
            Dirty = true;
        }

        private void RemoveExplicitOnImportLibrary_OnClick(object sender, RoutedEventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            Globals.Chem4WordV3.Telemetry.Write(module, "Action", "Triggered");
            SystemOptions.RemoveExplicitHydrogensOnImportFromLibrary = RemoveExplicitOnImportLibrary.IsChecked.Value;
            Dirty = true;
        }

        private void ApplyDefaultOnImportFile_OnClick(object sender, RoutedEventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            Globals.Chem4WordV3.Telemetry.Write(module, "Action", "Triggered");
            SystemOptions.SetBondLengthOnImportFromFile = ApplyDefaultOnImportFile.IsChecked.Value;
            Dirty = true;
        }

        private void ApplyDefaultOnImportSearch_OnClick(object sender, RoutedEventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            Globals.Chem4WordV3.Telemetry.Write(module, "Action", "Triggered");
            SystemOptions.SetBondLengthOnImportFromSearch = ApplyDefaultOnImportSearch.IsChecked.Value;
            Dirty = true;
        }

        private void ApplyDefaultOnImportLibrary_OnClick(object sender, RoutedEventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            Globals.Chem4WordV3.Telemetry.Write(module, "Action", "Triggered");
            SystemOptions.SetBondLengthOnImportFromLibrary = ApplyDefaultOnImportLibrary.IsChecked.Value;
            Dirty = true;
        }

        #endregion General Tab Events

        #region Privacy Tab Events

        private void TelemetryEnabled_OnClick(object sender, RoutedEventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            if (!_loading)
            {
                Globals.Chem4WordV3.Telemetry.Write(module, "Action", "Triggered");
                SystemOptions.TelemetryEnabled = TelemetryEnabled.IsChecked.Value;
                Dirty = true;
            }
        }

        #endregion Privacy Tab Events

        #region Library Tab Events

        private void OnClick_BrowseLibraryLocation(object sender, RoutedEventArgs e)
        {
            var browser = new VistaFolderBrowserDialog();
            browser.Description = "Select a folder to set as your default location";
            browser.UseDescriptionForTitle = true;
            browser.RootFolder = Environment.SpecialFolder.Desktop;
            browser.ShowNewFolderButton = true;
            browser.SelectedPath = Globals.Chem4WordV3.ListOfDetectedLibraries.DefaultLocation;

            var result = browser.ShowDialog();
            if (result == Forms.DialogResult.OK)
            {
                if (Directory.Exists(browser.SelectedPath))
                {
                    DefaultLocation.Text = browser.SelectedPath;
                    var listOfDetectedLibraries = Globals.Chem4WordV3.ListOfDetectedLibraries;
                    listOfDetectedLibraries.DefaultLocation = browser.SelectedPath;
                    new LibraryFileHelper(Globals.Chem4WordV3.Telemetry, Globals.Chem4WordV3.AddInInfo.ProgramDataPath)
                        .SaveFile(listOfDetectedLibraries);
                    ReloadGlobalListOfLibraries();
                }
            }
        }

        private void OnClick_AddExistingLibrary(object sender, RoutedEventArgs e)
        {
            var browser = new Forms.OpenFileDialog();
            browser.InitialDirectory = Globals.Chem4WordV3.ListOfDetectedLibraries.DefaultLocation;
            browser.AddExtension = true;
            browser.Filter = "*.db|*.db";
            browser.FileName = "New Library.db";
            browser.ShowHelp = false;

            var result = browser.ShowDialog();
            if (result == Forms.DialogResult.OK)
            {
                var fileInfo = new FileInfo(browser.FileName);
                if (Directory.Exists(fileInfo.DirectoryName)
                    && File.Exists(browser.FileName))
                {
                    var details = new DatabaseDetails
                    {
                        Driver = Constants.SQLiteStandardDriver,
                        DisplayName = fileInfo.Name.Replace(fileInfo.Extension, ""),
                        Connection = browser.FileName,
                        ShortFileName = fileInfo.Name
                    };

                    var listOfDetectedLibraries = Globals.Chem4WordV3.ListOfDetectedLibraries;
                    var existing = listOfDetectedLibraries.AvailableDatabases
                                                          .FirstOrDefault(n => n.DisplayName.Equals(details.DisplayName));
                    // Prevent add if there is a name clash
                    if (existing == null)
                    {
                        listOfDetectedLibraries.AvailableDatabases.Add(details);
                        new LibraryFileHelper(Globals.Chem4WordV3.Telemetry, Globals.Chem4WordV3.AddInInfo.ProgramDataPath)
                            .SaveFile(listOfDetectedLibraries);
                        ReloadGlobalListOfLibraries();
                        LoadLibrariesListTab();
                    }
                    else
                    {
                        UserInteractions.WarnUser("Couldn't add existing database due to display name clash");
                    }
                }
            }
        }

        private void OnClick_CreateNewLibrary(object sender, RoutedEventArgs e)
        {
            var browser = new Forms.SaveFileDialog();
            browser.InitialDirectory = Globals.Chem4WordV3.ListOfDetectedLibraries.DefaultLocation;
            browser.AddExtension = true;
            browser.Filter = "*.db|*.db";
            browser.FileName = "New Library.db";
            browser.ShowHelp = false;

            var result = browser.ShowDialog();
            if (result == Forms.DialogResult.OK)
            {
                var fileName = browser.FileName;
                if (!fileName.EndsWith(".db"))
                {
                    fileName += ".db";
                }
                var fileInfo = new FileInfo(fileName);
                if (Directory.Exists(fileInfo.DirectoryName)
                    && !File.Exists(browser.FileName))
                {
                    var displayName = fileInfo.Name.Replace(fileInfo.Extension, "");
                    var listOfDetectedLibraries = Globals.Chem4WordV3.ListOfDetectedLibraries;
                    var existing = listOfDetectedLibraries.AvailableDatabases
                                                          .FirstOrDefault(n => n.DisplayName.Equals(displayName));

                    if (existing == null)
                    {
                        var driver = Globals.Chem4WordV3.GetDriverPlugIn(Constants.SQLiteStandardDriver);
                        if (driver != null)
                        {
                            var details = new DatabaseDetails
                            {
                                Driver = Constants.SQLiteStandardDriver,
                                DisplayName = displayName,
                                Connection = fileName,
                                ShortFileName = fileInfo.Name
                            };
                            driver.CreateNewDatabase(details);

                            listOfDetectedLibraries.AvailableDatabases.Add(details);
                            new LibraryFileHelper(Globals.Chem4WordV3.Telemetry, Globals.Chem4WordV3.AddInInfo.ProgramDataPath)
                                .SaveFile(listOfDetectedLibraries);
                            ReloadGlobalListOfLibraries();
                            LoadLibrariesListTab();
                        }
                    }
                    else
                    {
                        UserInteractions.WarnUser("Couldn't create new database due to display name clash");
                    }
                }
            }
        }

        private void OnClick_DownloadLibrary(object sender, RoutedEventArgs e)
        {
            Debugger.Break();
        }

        private void OnClick_RemoveLibrary(object sender, RoutedEventArgs e)
        {
            var listOfDetectedLibraries = Globals.Chem4WordV3.ListOfDetectedLibraries;
            var item = listOfDetectedLibraries.AvailableDatabases.FirstOrDefault(r => r.DisplayName.Equals(_selectedLibrary));
            listOfDetectedLibraries.AvailableDatabases.Remove(item);
            new LibraryFileHelper(Globals.Chem4WordV3.Telemetry, Globals.Chem4WordV3.AddInInfo.ProgramDataPath)
                .SaveFile(listOfDetectedLibraries);
            ReloadGlobalListOfLibraries();
            LoadLibrariesListTab();
        }

        private void OnClick_EditLibrary(object sender, RoutedEventArgs e)
        {
            var editor = new LibraryEditorHost();
            editor.TopLeft = new Point(TopLeft.X + Constants.TopLeftOffset, TopLeft.Y + Constants.TopLeftOffset);
            editor.Telemetry = Globals.Chem4WordV3.Telemetry;
            editor.SelectedDatabase = _selectedLibrary;
            editor.ShowDialog();

            ReloadGlobalListOfLibraries();
            LoadLibrariesListTab();
        }

        private void SetSelectedLibrary(string selectedLibrary)
        {
            foreach (var item in LibrariesList.Items)
            {
                if (item is LibrariesSettingsGridSource source
                    && source.Name.Equals(selectedLibrary))
                {
                    LibrariesList.SelectedItem = item;
                    break;
                }
            }
        }

        private void OnSelectionChanged_ListOfLibraries(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0
                && e.AddedItems[0] is LibrariesSettingsGridSource source)
            {
                _selectedLibrary = source.Name;
                var cantRemove = source.IsDefault;
                RemoveLibrary.IsEnabled = !cantRemove;
                RemoveLibraryIcon.Fill = cantRemove ? new SolidColorBrush(Colors.Gray) : new SolidColorBrush(Colors.Red);
            }
        }

        #endregion Library Tab Events

        #region Maintenance Tab Events

        private void OnClick_SettingsFolder(object sender, RoutedEventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            Globals.Chem4WordV3.Telemetry.Write(module, "Action", "Triggered");

            try
            {
                Process.Start(Globals.Chem4WordV3.AddInInfo.ProductAppDataPath);
            }
            catch (Exception ex)
            {
                new ReportError(Globals.Chem4WordV3.Telemetry, TopLeft, module, ex).ShowDialog();
            }
        }

        private void OnClick_LibraryFolder(object sender, RoutedEventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            Globals.Chem4WordV3.Telemetry.Write(module, "Action", "Triggered");

            try
            {
                Process.Start(Globals.Chem4WordV3.AddInInfo.ProgramDataPath);
            }
            catch (Exception ex)
            {
                new ReportError(Globals.Chem4WordV3.Telemetry, TopLeft, module, ex).ShowDialog();
            }
        }

        private void OnClick_PlugInsFolder(object sender, RoutedEventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            Globals.Chem4WordV3.Telemetry.Write(module, "Action", "Triggered");

            try
            {
                Process.Start(Path.Combine(Globals.Chem4WordV3.AddInInfo.DeploymentPath, "PlugIns"));
            }
            catch (Exception ex)
            {
                new ReportError(Globals.Chem4WordV3.Telemetry, TopLeft, module, ex).ShowDialog();
            }
        }

        #endregion Maintenance Tab Events

        #region Private methods

        private void SetButtonState(bool enabled)
        {
            Ok.IsEnabled = enabled;
            Cancel.IsEnabled = enabled;
            Defaults.IsEnabled = enabled;
            TabControl.IsEnabled = enabled;
        }

        private void LoadSettings()
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            #region Plug-Ins Tab

            string selectedEditor = SystemOptions.SelectedEditorPlugIn;
            string selectedRenderer = SystemOptions.SelectedRendererPlugIn;

            SelectEditorPlugIn.Items.Clear();
            SelectRendererPlugIn.Items.Clear();
            SelectSearcherPlugIn.Items.Clear();
            SelectedEditorSettings.IsEnabled = false;
            SelectedRendererSettings.IsEnabled = false;
            SelectedSearcherSettings.IsEnabled = false;

            foreach (IChem4WordEditor editor in Globals.Chem4WordV3.Editors)
            {
                var pci = new PlugInComboItem
                {
                    Name = editor.Name,
                    Description = editor.Description
                };
                int item = SelectEditorPlugIn.Items.Add(pci);

                if (editor.Name.Equals(selectedEditor))
                {
                    SelectedEditorSettings.IsEnabled = editor.HasSettings;
                    SelectedEditorPlugInDescription.Text = editor.Description;
                    SelectEditorPlugIn.SelectedIndex = item;
                }
            }

            foreach (IChem4WordRenderer renderer in Globals.Chem4WordV3.Renderers)
            {
                var pci = new PlugInComboItem
                {
                    Name = renderer.Name,
                    Description = renderer.Description
                };
                int item = SelectRendererPlugIn.Items.Add(pci);
                if (renderer.Name.Equals(selectedRenderer))
                {
                    SelectedRendererSettings.IsEnabled = renderer.HasSettings;
                    SelectedRendererDescription.Text = renderer.Description;
                    SelectRendererPlugIn.SelectedIndex = item;
                }
            }

            foreach (IChem4WordSearcher searcher in Globals.Chem4WordV3.Searchers.OrderBy(s => s.DisplayOrder))
            {
                var pci = new PlugInComboItem
                {
                    Name = searcher.Name,
                    Description = searcher.Description
                };
                int item = SelectSearcherPlugIn.Items.Add(pci);
                if (SelectSearcherPlugIn.Items.Count == 1)
                {
                    SelectedSearcherSettings.IsEnabled = searcher.HasSettings;
                    SelectedSearcherDescription.Text = searcher.Description;
                    SelectSearcherPlugIn.SelectedIndex = item;
                }
            }

            #endregion Plug-Ins Tab

            #region Telemetry Tab

            string betaValue = Globals.Chem4WordV3.ThisVersion.Root?.Element("IsBeta")?.Value;
            bool isBeta = betaValue != null && bool.Parse(betaValue);

            TelemetryEnabled.IsChecked = isBeta || SystemOptions.TelemetryEnabled;
            TelemetryEnabled.IsEnabled = !isBeta;
            if (!isBeta)
            {
                BetaInformation.Visibility = Visibility.Hidden;
            }

            #endregion Telemetry Tab

            #region Libraries Tab

            if (Globals.Chem4WordV3.ListOfDetectedLibraries == null)
            {
                // Belt and braces just in case someone clicks too early
                ReloadGlobalListOfLibraries();
            }

            LoadLibrariesListTab();

            #endregion Libraries Tab

            #region General Tab

            ApplyDefaultOnImportFile.IsChecked = SystemOptions.SetBondLengthOnImportFromFile;
            ApplyDefaultOnImportSearch.IsChecked = SystemOptions.SetBondLengthOnImportFromSearch;
            ApplyDefaultOnImportLibrary.IsChecked = SystemOptions.SetBondLengthOnImportFromLibrary;

            RemoveExplicitOnImportFile.IsChecked = SystemOptions.RemoveExplicitHydrogensOnImportFromFile;
            RemoveExplicitOnImportSearch.IsChecked = SystemOptions.RemoveExplicitHydrogensOnImportFromSearch;
            RemoveExplicitOnImportLibrary.IsChecked = SystemOptions.RemoveExplicitHydrogensOnImportFromLibrary;

            foreach (var item in DefaultBondLength.Items)
            {
                var cbi = item as ComboBoxItem;
                if (int.Parse(cbi.Tag as string) == SystemOptions.BondLength)
                {
                    DefaultBondLength.SelectedItem = item;
                    break;
                }
            }

            #endregion General Tab
        }

        private void LoadLibrariesListTab()
        {
            var libraries = Globals.Chem4WordV3.ListOfDetectedLibraries;
            var data = new List<LibrariesSettingsGridSource>();

            if (libraries != null)
            {
                DefaultLocation.Text = libraries.DefaultLocation;
                foreach (var database in libraries.AvailableDatabases)
                {
                    var isDefault = libraries.SelectedLibrary.Equals(database.DisplayName);
                    var obj = new LibrariesSettingsGridSource
                    {
                        Name = database.DisplayName,
                        FileName = database.ShortFileName,
                        Connection = database.Connection,
                        Count = GetPropertyValue(database, "Count", "?"),
                        Dictionary = false,
                        Locked = GetPropertyValue(database, "Owner", "User").Equals("System") ? "Yes" : "No",
                        License = GetPropertyValue(database, "Type", "Free").Equals("Free") ? "N/A" : "Required",
                        IsDefault = isDefault
                    };
                    data.Add(obj);
                }
                LibrariesList.ItemsSource = data;

                _selectedLibrary = libraries.SelectedLibrary;
                SetSelectedLibrary(_selectedLibrary);
            }
        }

        private void ReloadGlobalListOfLibraries()
        {
            Globals.Chem4WordV3.ListOfDetectedLibraries
                = new LibraryFileHelper(Globals.Chem4WordV3.Telemetry, Globals.Chem4WordV3.AddInInfo.ProgramDataPath)
                    .GetListOfLibraries();
        }

        private BitmapImage CreateImageFromStream(Stream stream)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            var bitmap = new BitmapImage();

            bitmap.BeginInit();
            bitmap.StreamSource = stream;
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.EndInit();
            bitmap.Freeze();

            return bitmap;
        }

        private string GetPropertyValue(DatabaseDetails details, string key, string defaultValue)
        {
            string result = defaultValue;

            if (details.Properties.ContainsKey(key))
            {
                result = details.Properties[key];
            }

            return result;
        }

        #endregion Private methods
    }
}