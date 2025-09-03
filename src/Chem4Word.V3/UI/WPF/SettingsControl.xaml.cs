// ---------------------------------------------------------------------------
//  Copyright (c) 2025, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.Core;
using Chem4Word.Core.Helpers;
using Chem4Word.Core.UI.Forms;
using Chem4Word.Core.UI.Wpf;
using Chem4Word.Helpers;
using Chem4Word.Model2.Enums;
using Chem4Word.Models;
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
using System.Xml.Linq;
using System.Xml.XPath;

using Forms = System.Windows.Forms;

using Point = System.Windows.Point;
using TextBox = System.Windows.Controls.TextBox;
using UserControl = System.Windows.Controls.UserControl;

namespace Chem4Word.UI.WPF
{
    /// <summary>
    /// Interaction logic for SettingsControl.xaml
    /// </summary>
    public partial class SettingsControl : UserControl
    {
        private static string _product = Assembly.GetExecutingAssembly().FullName.Split(',')[0];
        private static string _class = MethodBase.GetCurrentMethod()?.DeclaringType?.Name;

        public event EventHandler OnButtonClick;

        public Chem4WordOptions SystemOptions { get; set; }
        public Point TopLeft { get; set; }
        public string ActiveTab { get; set; }
        public bool Dirty { get; set; }

        private bool _loading;
        private string _selectedLibrary;

        public SettingsControl()
        {
            _loading = true;

            InitializeComponent();
        }

        #region Form Load

        private void OnLoaded_SettingsControl(object sender, RoutedEventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod()?.Name}()";

            ImplicitHydrogenMode.Items.Clear();
            foreach (var keyValuePair in EnumHelper.GetEnumValuesWithDescriptions<HydrogenLabels>())
            {
                var cbi = new ComboBoxItem
                {
                    Content = keyValuePair.Value,
                    Tag = keyValuePair.Key
                };
                ImplicitHydrogenMode.Items.Add(cbi);

                if (SystemOptions.ExplicitH == keyValuePair.Key)
                {
                    ImplicitHydrogenMode.SelectedItem = cbi;
                }
            }

            #region Set Current Values

            if (SystemOptions != null)
            {
                LoadSettings();
            }

            if (!string.IsNullOrEmpty(ActiveTab))
            {
                var tab = TabControl.Items
                                    .Cast<TabItem>()
                                    .FirstOrDefault(i => i.Name.Equals(ActiveTab));
                if (tab != null)
                {
                    tab.IsSelected = true;
                }
            }

            #endregion Set Current Values

            _loading = false;

            EnableButtons();
        }

        #endregion Form Load

        #region TabControl

        private void OnSelectionChanged_TabControl(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0
                && e.AddedItems[0] is TabItem item)
            {
                if (item.Name.Equals("Libraries")
                    && LibrariesList.Items.Count == 0)
                {
                    LoadLibrariesListTab();
                }

                SetLibraryTabButtons();
            }
        }

        #endregion TabControl

        #region Bottom Buttons

        private void OnClick_OkButton(object sender, RoutedEventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod()?.Name}()";
            Globals.Chem4WordV3.Telemetry.Write(module, "Action", "Triggered");

            WpfEventArgs args = new WpfEventArgs();
            args.Button = "Ok";

            OnButtonClick?.Invoke(this, args);
        }

        private void OnClick_CancelButton(object sender, RoutedEventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod()?.Name}()";
            Globals.Chem4WordV3.Telemetry.Write(module, "Action", "Triggered");

            WpfEventArgs args = new WpfEventArgs();
            args.Button = "Cancel";

            OnButtonClick?.Invoke(this, args);
        }

        private void OnClick_DefaultsButton(object sender, RoutedEventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod()?.Name}()";
            Globals.Chem4WordV3.Telemetry.Write(module, "Action", "Triggered");

            try
            {
                Forms.DialogResult dr = UserInteractions.AskUserOkCancel("Restore default settings");
                if (dr == Forms.DialogResult.OK)
                {
                    _loading = true;
                    SystemOptions.RestoreDefaults();
                    LoadSettings();
                    _loading = false;
                    Dirty = true;
                    EnableButtons();
                }
            }
            catch (Exception ex)
            {
                new ReportError(Globals.Chem4WordV3.Telemetry, TopLeft, module, ex).ShowDialog();
            }
        }

        #endregion Bottom Buttons

        #region Plug-Ins Tab Events

        private void OnClick_SelectedEditorSettings(object sender, RoutedEventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod()?.Name}()";
            Globals.Chem4WordV3.Telemetry.Write(module, "Action", "Triggered");

            var editor = Globals.Chem4WordV3.GetEditorPlugIn(SelectEditorPlugIn.SelectedItem.ToString());
            editor.SettingsPath = Globals.Chem4WordV3.AddInInfo.ProductAppDataPath;
            editor.ChangeSettings(new Point(SystemOptions.WordTopLeft.X + CoreConstants.TopLeftOffset * 2, SystemOptions.WordTopLeft.Y + CoreConstants.TopLeftOffset * 2));
        }

        private void OnClick_SelectedRendererSettings(object sender, RoutedEventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod()?.Name}()";
            Globals.Chem4WordV3.Telemetry.Write(module, "Action", "Triggered");

            var renderer = Globals.Chem4WordV3.GetRendererPlugIn(SelectRendererPlugIn.SelectedItem.ToString());
            renderer.SettingsPath = Globals.Chem4WordV3.AddInInfo.ProductAppDataPath;
            renderer.ChangeSettings(new Point(SystemOptions.WordTopLeft.X + CoreConstants.TopLeftOffset * 2, SystemOptions.WordTopLeft.Y + CoreConstants.TopLeftOffset * 2));
        }

        private void OnClick_SelectedSearcherSettings(object sender, RoutedEventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod()?.Name}()";
            Globals.Chem4WordV3.Telemetry.Write(module, "Action", "Triggered");

            var searcher = Globals.Chem4WordV3.GetSearcherPlugIn(SelectSearcherPlugIn.SelectedItem.ToString());
            searcher.SettingsPath = Globals.Chem4WordV3.AddInInfo.ProductAppDataPath;
            searcher.ChangeSettings(new Point(SystemOptions.WordTopLeft.X + CoreConstants.TopLeftOffset * 2, SystemOptions.WordTopLeft.Y + CoreConstants.TopLeftOffset * 2));
        }

        private void OnSelectionChanged_SelectEditorPlugIn(object sender, SelectionChangedEventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod()?.Name}()";
            if (!_loading)
            {
                Globals.Chem4WordV3.Telemetry.Write(module, "Action", "Triggered");

                var pci = SelectEditorPlugIn.SelectedItem as PlugInComboItem;
                SystemOptions.SelectedEditorPlugIn = pci?.Name;
                SelectedEditorPlugInDescription.Text = pci?.Description;
                var editor = Globals.Chem4WordV3.GetEditorPlugIn(pci.Name);
                SelectedEditorSettings.IsEnabled = editor.HasSettings;

                Dirty = true;
                EnableButtons();
            }
        }

        private void OnSelectionChanged_SelectRenderer(object sender, SelectionChangedEventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod()?.Name}()";
            if (!_loading)
            {
                Globals.Chem4WordV3.Telemetry.Write(module, "Action", "Triggered");

                var pci = SelectRendererPlugIn.SelectedItem as PlugInComboItem;
                SystemOptions.SelectedRendererPlugIn = pci?.Name;
                SelectedRendererDescription.Text = pci?.Description;
                var renderer = Globals.Chem4WordV3.GetRendererPlugIn(pci.Name);
                SelectedRendererSettings.IsEnabled = renderer.HasSettings;

                Dirty = true;
                EnableButtons();
            }
        }

        private void OnSelectionChanged_SelectSearcher(object sender, SelectionChangedEventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod()?.Name}()";
            if (!_loading)
            {
                Globals.Chem4WordV3.Telemetry.Write(module, "Action", "Triggered");

                var pci = SelectSearcherPlugIn.SelectedItem as PlugInComboItem;
                SelectedSearcherDescription.Text = pci?.Description;
                var searcher = Globals.Chem4WordV3.GetSearcherPlugIn(pci.Name);
                SelectedSearcherSettings.IsEnabled = searcher.HasSettings;
            }
        }

        #endregion Plug-Ins Tab Events

        #region General Tab Events

        private void OnSelectionChanged_BondLength(object sender, SelectionChangedEventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod()?.Name}()";
            if (!_loading)
            {
                Globals.Chem4WordV3.Telemetry.Write(module, "Action", "Triggered");

                if (DefaultBondLength.SelectedItem is ComboBoxItem cbi)
                {
                    SystemOptions.BondLength = int.Parse((string)cbi.Tag);

                    Dirty = true;
                    EnableButtons();
                }
            }
        }

        private void OnClick_RemoveExplicitOnImportFile(object sender, RoutedEventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod()?.Name}()";
            Globals.Chem4WordV3.Telemetry.Write(module, "Action", "Triggered");
            SystemOptions.RemoveExplicitHydrogensOnImportFromFile = RemoveExplicitOnImportFile.IsChecked.Value;
            Dirty = true;
            EnableButtons();
        }

        private void OnClick_RemoveExplicitOnImportSearch(object sender, RoutedEventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod()?.Name}()";
            Globals.Chem4WordV3.Telemetry.Write(module, "Action", "Triggered");
            SystemOptions.RemoveExplicitHydrogensOnImportFromSearch = RemoveExplicitOnImportSearch.IsChecked.Value;
            Dirty = true;
            EnableButtons();
        }

        private void OnClick_RemoveExplicitOnImportLibrary(object sender, RoutedEventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod()?.Name}()";
            Globals.Chem4WordV3.Telemetry.Write(module, "Action", "Triggered");
            SystemOptions.RemoveExplicitHydrogensOnImportFromLibrary = RemoveExplicitOnImportLibrary.IsChecked.Value;
            Dirty = true;
            EnableButtons();
        }

        private void OnClick_ApplyDefaultOnImportFile(object sender, RoutedEventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod()?.Name}()";
            Globals.Chem4WordV3.Telemetry.Write(module, "Action", "Triggered");
            SystemOptions.SetBondLengthOnImportFromFile = ApplyDefaultOnImportFile.IsChecked.Value;
            Dirty = true;
            EnableButtons();
        }

        private void OnClick_ApplyDefaultOnImportSearch(object sender, RoutedEventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod()?.Name}()";
            Globals.Chem4WordV3.Telemetry.Write(module, "Action", "Triggered");
            SystemOptions.SetBondLengthOnImportFromSearch = ApplyDefaultOnImportSearch.IsChecked.Value;
            Dirty = true;
            EnableButtons();
        }

        private void OnClick_ApplyDefaultOnImportLibrary(object sender, RoutedEventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod()?.Name}()";
            Globals.Chem4WordV3.Telemetry.Write(module, "Action", "Triggered");
            SystemOptions.SetBondLengthOnImportFromLibrary = ApplyDefaultOnImportLibrary.IsChecked.Value;
            Dirty = true;
            EnableButtons();
        }

        private void OnClick_ApplyShowAtomsInColour(object sender, RoutedEventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod()?.Name}()";
            Globals.Chem4WordV3.Telemetry.Write(module, "Action", "Triggered");
            SystemOptions.ShowColouredAtoms = ShowAtomsInColour.IsChecked.Value;
            Dirty = true;
            EnableButtons();
        }

        private void OnClick_ApplyShowAllCarbonAtoms(object sender, RoutedEventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod()?.Name}()";
            Globals.Chem4WordV3.Telemetry.Write(module, "Action", "Triggered");
            SystemOptions.ExplicitC = ShowAllCarbonAtoms.IsChecked.Value;
            Dirty = true;
            EnableButtons();
        }

        private void OnSelectionChanged_ImplicitHydrogenMode(object sender, SelectionChangedEventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod()?.Name}()";

            if (!_loading)
            {
                Globals.Chem4WordV3.Telemetry.Write(module, "Action", "Triggered");

                if (ImplicitHydrogenMode.SelectedItem is ComboBoxItem cbi)
                {
                    if (Enum.TryParse(cbi.Tag.ToString(), out HydrogenLabels hydrogenLabels))
                    {
                        SystemOptions.ExplicitH = hydrogenLabels;
                    }

                    Dirty = true;
                    EnableButtons();
                }
            }
        }

        private void OnClick_ShowGroupingOfMolecules(object sender, RoutedEventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod()?.Name}()";
            Globals.Chem4WordV3.Telemetry.Write(module, "Action", "Triggered");
            SystemOptions.ShowMoleculeGrouping = ShowGroupingOfMolecules.IsChecked.Value;
            Dirty = true;
            EnableButtons();
        }

        private void OnClick_ShowMolecularWeight(object sender, RoutedEventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod()?.Name}()";
            Globals.Chem4WordV3.Telemetry.Write(module, "Action", "Triggered");
            SystemOptions.ShowMolecularWeight = ShowMolecularWeight.IsChecked.Value;
            Dirty = true;
            EnableButtons();
        }

        private void OnClick_ShowMoleculeCaptions(object sender, RoutedEventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod()?.Name}()";
            Globals.Chem4WordV3.Telemetry.Write(module, "Action", "Triggered");
            SystemOptions.ShowMoleculeCaptions = ShowMoleculeCaptions.IsChecked.Value;
            Dirty = true;
            EnableButtons();
        }

        #endregion General Tab Events

        #region Privacy Tab Events

        private void OnClick_EnableTelemetry(object sender, RoutedEventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod()?.Name}()";
            if (!_loading)
            {
                Globals.Chem4WordV3.Telemetry.Write(module, "Action", "Triggered");
                SystemOptions.TelemetryEnabled = TelemetryEnabled.IsChecked.Value;
                Dirty = true;
                EnableButtons();
            }
        }

        #endregion Privacy Tab Events

        #region Library Tab Events

        private void OnClick_SelectDefaultLibraryLocation(object sender, RoutedEventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod()?.Name}()";

            Globals.Chem4WordV3.Telemetry.Write(module, "Action", "Triggered");

            var browser = new VistaFolderBrowserDialog();
            browser.Description = "Select a folder to set as your default location";
            browser.UseDescriptionForTitle = true;
            browser.RootFolder = Environment.SpecialFolder.Desktop;
            browser.ShowNewFolderButton = true;
            browser.SelectedPath = Globals.Chem4WordV3.ListOfDetectedLibraries.DefaultLocation;

            var result = browser.ShowDialog();
            if (result == Forms.DialogResult.OK
                && Directory.Exists(browser.SelectedPath))
            {
                Globals.Chem4WordV3.Telemetry.Write(module, "Action", $"Changing default library path to '{browser.SelectedPath}'");

                DefaultLocation.Text = browser.SelectedPath;
                var listOfDetectedLibraries = Globals.Chem4WordV3.ListOfDetectedLibraries;
                listOfDetectedLibraries.DefaultLocation = browser.SelectedPath;
                new LibraryFileHelper(Globals.Chem4WordV3.Telemetry, Globals.Chem4WordV3.AddInInfo.ProgramDataPath)
                    .SaveSettingsFile(listOfDetectedLibraries);
                ReloadGlobalListOfLibraries();

                SetLibraryTabButtons();
            }
        }

        private void OnGotFocus_LibraryName(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                SetSelectedLibrary(textBox.Text);
            }
        }

        private bool IsValidFileName(string suggestedName)
        {
            var result = true;

            // Check for reserved characters
            var invalidFileNameChars = Path.GetInvalidFileNameChars();
            foreach (var c in invalidFileNameChars)
            {
                if (suggestedName.Contains(c))
                {
                    result = false;
                    break;
                }
            }

            if (result)
            {
                // Check for reserved words
                switch (suggestedName.ToUpper())
                {
                    case "CON":
                    case "PRN":
                    case "AUX":
                    case "NUL":
                    case "COM1":
                    case "COM2":
                    case "COM3":
                    case "COM4":
                    case "COM5":
                    case "COM6":
                    case "COM7":
                    case "COM8":
                    case "COM9":
                    case "LPT1":
                    case "LPT2":
                    case "LPT3":
                    case "LPT4":
                    case "LPT5":
                    case "LPT6":
                    case "LPT7":
                    case "LPT8":
                    case "LPT9":
                        result = false;
                        break;
                }
            }

            if (result)
            {
                // Must not end with a dot, '.db' or '.lic'
                if (suggestedName.EndsWith(".")
                    || suggestedName.EndsWith(".db")
                    || suggestedName.EndsWith(".lic"))
                {
                    result = false;
                }
            }

            return result;
        }

        private void OnLostFocus_LibraryName(object sender, RoutedEventArgs e)
        {
            var module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod()?.Name}()";

            try
            {
                if (sender is TextBox textBox)
                {
                    textBox.Background = SystemColors.WindowBrush;
                    var trimmed = textBox.Text.Trim();
                    if (IsValidFileName(trimmed))
                    {
                        var listOfDetectedLibraries = Globals.Chem4WordV3.ListOfDetectedLibraries;
                        if (listOfDetectedLibraries != null)
                        {
                            DatabaseDetails database = null;

                            var oldFileName = string.Empty;
                            var newFileName = $"{trimmed}.db";

                            if (textBox.DataContext is LibrariesSettingsGridSource source)
                            {
                                database = listOfDetectedLibraries.AvailableDatabases
                                                                  .FirstOrDefault(n => n.ShortFileName.Equals(source.FileName));
                                if (database != null)
                                {
                                    oldFileName = database.ShortFileName;
                                }
                            }

                            if (!string.IsNullOrEmpty(oldFileName)
                                && !string.IsNullOrEmpty(newFileName)
                                && !newFileName.Equals(oldFileName)
                                && database != null)
                            {
                                var fileInfo = new FileInfo(database.Connection);

                                var canRename = !File.Exists(Path.Combine(fileInfo.DirectoryName, newFileName));
                                var isUnique = listOfDetectedLibraries.AvailableDatabases.FirstOrDefault(n => n.DisplayName.Equals(newFileName.Replace(".db", "")));

                                if (canRename && isUnique == null)
                                {
                                    Globals.Chem4WordV3.Telemetry.Write(module, "Information", $"Renaming {oldFileName} to {newFileName}");

                                    File.Move(Path.Combine(fileInfo.DirectoryName, oldFileName), Path.Combine(fileInfo.DirectoryName, newFileName));
                                    if (File.Exists(Path.Combine(fileInfo.DirectoryName, oldFileName.Replace(".db", ".lic"))))
                                    {
                                        Globals.Chem4WordV3.Telemetry.Write(module, "Information", $"Renaming {oldFileName.Replace(".db", ".lic")} to {newFileName.Replace(".db", ".lic")}");
                                        File.Move(Path.Combine(fileInfo.DirectoryName, oldFileName.Replace(".db", ".lic")), Path.Combine(fileInfo.DirectoryName, newFileName.Replace(".db", ".lic")));
                                    }

                                    var selected = listOfDetectedLibraries.AvailableDatabases.FirstOrDefault(n => n.DisplayName.Equals(_selectedLibrary));
                                    if (selected != null && selected.ShortFileName.Equals(oldFileName))
                                    {
                                        listOfDetectedLibraries.SelectedLibrary = newFileName.Replace(".db", "");
                                    }

                                    database.ShortFileName = newFileName;
                                    database.DisplayName = newFileName.Replace(".db", "");
                                    database.Connection = Path.Combine(fileInfo.DirectoryName, newFileName);

                                    new LibraryFileHelper(Globals.Chem4WordV3.Telemetry, Globals.Chem4WordV3.AddInInfo.ProgramDataPath)
                                        .SaveSettingsFile(listOfDetectedLibraries);
                                }
                                else
                                {
                                    var message = $"Name clash; Can't rename '{oldFileName}' to '{newFileName}' in '{fileInfo.DirectoryName}'";
                                    Globals.Chem4WordV3.Telemetry.Write(module, "Warning", message);
                                    UserInteractions.WarnUser(message);
                                }

                                ReloadGlobalListOfLibraries();
                                LoadLibrariesListTab();
                            }
                        }
                    }
                    else
                    {
                        textBox.Background = Brushes.Salmon;
                        UserInteractions.WarnUser($"{textBox.Text} is an invalid library name!");
                    }
                }
            }
            catch (Exception exception)
            {
                Globals.Chem4WordV3.Telemetry.Write(module, "Exception", exception.Message);
                Globals.Chem4WordV3.Telemetry.Write(module, "Exception", exception.StackTrace);
            }
        }

        private void OnClick_AddExistingLibrary(object sender, RoutedEventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod()?.Name}()";

            Globals.Chem4WordV3.Telemetry.Write(module, "Action", "Triggered");

            var browser = new Forms.OpenFileDialog();
            browser.InitialDirectory = Globals.Chem4WordV3.ListOfDetectedLibraries.DefaultLocation;
            browser.AddExtension = true;
            browser.Filter = "database files (*.db)|*.db";
            browser.ShowHelp = false;

            var result = browser.ShowDialog();
            if (result == Forms.DialogResult.OK)
            {
                var fileInfo = new FileInfo(browser.FileName);
                if (Directory.Exists(fileInfo.DirectoryName)
                    && File.Exists(browser.FileName))
                {
                    var driver = (IChem4WordLibraryReader)Globals.Chem4WordV3.GetDriverPlugIn(CoreConstants.SQLiteStandardDriver);
                    if (driver != null)
                    {
                        var details = new DatabaseDetails
                        {
                            Driver = CoreConstants.SQLiteStandardDriver,
                            DisplayName = fileInfo.Name.Replace(fileInfo.Extension, ""),
                            Connection = browser.FileName,
                            ShortFileName = fileInfo.Name
                        };

                        var info = driver.GetDatabaseFileProperties(details.Connection);
                        if (!info.IsSqliteDatabase)
                        {
                            UserInteractions.WarnUser("Couldn't add this database, as it is not a SQLite database");
                        }
                        else if (info.IsChem4Word && !info.IsReadOnly
                            || info.IsChem4Word && info.IsReadOnly && !info.RequiresPatching)
                        {
                            details.Driver = GetDriverFromLicense(browser.FileName);

                            var listOfDetectedLibraries = Globals.Chem4WordV3.ListOfDetectedLibraries;
                            var unique = listOfDetectedLibraries.AvailableDatabases
                                                                .FirstOrDefault(n => n.DisplayName.Equals(details.DisplayName));

                            // Prevent add if there is a name clash
                            if (unique == null)
                            {
                                Globals.Chem4WordV3.Telemetry.Write(module, "Action", $"Added existing library {details.DisplayName}");
                                listOfDetectedLibraries.AvailableDatabases.Add(details);

                                new LibraryFileHelper(Globals.Chem4WordV3.Telemetry, Globals.Chem4WordV3.AddInInfo.ProgramDataPath)
                                    .SaveSettingsFile(listOfDetectedLibraries);
                                ReloadGlobalListOfLibraries();
                                LoadLibrariesListTab();
                            }
                            else
                            {
                                UserInteractions.WarnUser("Couldn't add this database, due to display name clash");
                            }
                        }
                        else
                        {
                            if (!info.IsChem4Word)
                            {
                                UserInteractions.WarnUser("Couldn't add this database, as it is not a Chem4Word library");
                            }

                            if (info.IsReadOnly && info.RequiresPatching)
                            {
                                UserInteractions.WarnUser("Couldn't add this Chem4Word library database, because it is read only and requires patching");
                            }
                        }
                    }
                }
            }
        }

        private void OnClick_CreateNewLibrary(object sender, RoutedEventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod()?.Name}()";

            Globals.Chem4WordV3.Telemetry.Write(module, "Action", "Triggered");

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
                if (Directory.Exists(fileInfo.DirectoryName))
                {
                    var displayName = fileInfo.Name.Replace(fileInfo.Extension, "");
                    var listOfDetectedLibraries = Globals.Chem4WordV3.ListOfDetectedLibraries;
                    var existing = listOfDetectedLibraries.AvailableDatabases
                                                          .FirstOrDefault(n => n.DisplayName.Equals(displayName));

                    if (existing == null)
                    {
                        var driver = (IChem4WordLibraryWriter)Globals.Chem4WordV3.GetDriverPlugIn(CoreConstants.SQLiteStandardDriver);
                        if (driver != null)
                        {
                            var details = new DatabaseDetails
                            {
                                Driver = CoreConstants.SQLiteStandardDriver,
                                DisplayName = displayName,
                                Connection = fileName,
                                ShortFileName = fileInfo.Name
                            };

                            Globals.Chem4WordV3.Telemetry.Write(module, "Action", $"Created new library {details.DisplayName}");
                            driver.CreateNewDatabase(details.Connection);

                            listOfDetectedLibraries.AvailableDatabases.Add(details);
                            new LibraryFileHelper(Globals.Chem4WordV3.Telemetry, Globals.Chem4WordV3.AddInInfo.ProgramDataPath)
                                .SaveSettingsFile(listOfDetectedLibraries);
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
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod()?.Name}()";

            Globals.Chem4WordV3.Telemetry.Write(module, "Action", "Triggered");

            var host = new LibraryDownloadHost();
            host.TopLeft = new Point(TopLeft.X + CoreConstants.TopLeftOffset, TopLeft.Y + CoreConstants.TopLeftOffset);
            host.ShowDialog();

            ReloadGlobalListOfLibraries();
            LoadLibrariesListTab();
        }

        private void OnClick_RemoveLibrary(object sender, RoutedEventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod()?.Name}()";

            Globals.Chem4WordV3.Telemetry.Write(module, "Action", "Triggered");

            if (LibrariesList.SelectedItem is LibrariesSettingsGridSource library
                && !library.Name.Equals(_selectedLibrary))
            {
                Globals.Chem4WordV3.Telemetry.Write(module, "Action", $"Removing library '{library.Name}'");

                var listOfDetectedLibraries = Globals.Chem4WordV3.ListOfDetectedLibraries;
                var item = listOfDetectedLibraries.AvailableDatabases.FirstOrDefault(r => r.DisplayName.Equals(library.Name));
                listOfDetectedLibraries.AvailableDatabases.Remove(item);
                new LibraryFileHelper(Globals.Chem4WordV3.Telemetry, Globals.Chem4WordV3.AddInInfo.ProgramDataPath)
                    .SaveSettingsFile(listOfDetectedLibraries);
                ReloadGlobalListOfLibraries();
                LoadLibrariesListTab();
            }
        }

        private void OnClick_EditLibrary(object sender, RoutedEventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod()?.Name}()";

            if (LibrariesList.SelectedItem is LibrariesSettingsGridSource library
                && !library.Locked.Equals("Yes"))
            {
                Globals.Chem4WordV3.Telemetry.Write(module, "Action", $"Editing library '{library.Name}'");

                var editor = new LibraryEditorHost();
                editor.TopLeft = new Point(TopLeft.X + CoreConstants.TopLeftOffset, TopLeft.Y + CoreConstants.TopLeftOffset);
                editor.Telemetry = Globals.Chem4WordV3.Telemetry;
                editor.SelectedDatabase = library.Name;
                editor.ShowDialog();

                ReloadGlobalListOfLibraries();
                LoadLibrariesListTab();
            }
        }

        private string GetUserFromLicense(string filename)
        {
            // http://xpather.com/
            var result = "Required";

            var customerName = string.Empty;
            var customerEmail = string.Empty;

            // Read driver name from license file if present
            var licenseFile = filename.Replace(".db", ".lic");
            if (File.Exists(licenseFile))
            {
                var xDocument = XDocument.Parse(File.ReadAllText(licenseFile));
                var nameElement = xDocument.XPathSelectElements("//Customer/Name").FirstOrDefault();
                if (nameElement != null)
                {
                    customerName = nameElement.Value.Trim();
                }
                var emailElement = xDocument.XPathSelectElements("//Customer/Email").FirstOrDefault();
                if (emailElement != null)
                {
                    customerEmail = emailElement.Value.Trim();
                }
            }

            if (!string.IsNullOrEmpty(customerName) && !string.IsNullOrEmpty(customerEmail))
            {
                result = $"{customerName} <{customerEmail}>";
            }
            return result;
        }

        private string GetDriverFromLicense(string filename)
        {
            // // http://xpather.com/
            var result = CoreConstants.SQLiteStandardDriver;

            // Read driver name from license file if present
            var licenseFile = filename.Replace(".db", ".lic");
            if (File.Exists(licenseFile))
            {
                var xDocument = XDocument.Parse(File.ReadAllText(licenseFile));
                var attributes = xDocument.XPathSelectElements("//Attribute").ToList();
                if (attributes.Any())
                {
                    foreach (var element in attributes)
                    {
                        if (element.FirstAttribute.Value.Equals("Driver"))
                        {
                            result = element.Value.Trim();
                            break;
                        }
                    }
                }
            }

            return result;
        }

        private void SetLibraryTabButtons()
        {
            var hasPermission = FileSystemHelper.UserHasWritePermission(Globals.Chem4WordV3.ListOfDetectedLibraries.DefaultLocation);
            CreateNewLibrary.IsEnabled = hasPermission;
            DownloadLibrary.IsEnabled = hasPermission;
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
                // Don't allow default database to be removed
                RemoveLibrary.IsEnabled = !source.IsDefault;

                // Don't allow locked database to be edited
                EditLibrary.IsEnabled = source.Locked.Equals("No");
            }
        }

        #endregion Library Tab Events

        #region Maintenance Tab Events

        private void OnClick_ExploreSettingsFolder(object sender, RoutedEventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod()?.Name}()";
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

        private void OnClick_ExploreCommonFolder(object sender, RoutedEventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod()?.Name}()";
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

        #endregion Maintenance Tab Events

        #region Private methods

        private void LoadSettings()
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod()?.Name}()";

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
                if (item is ComboBoxItem cbi)
                {
                    if (int.Parse(cbi.Tag.ToString()) == SystemOptions.BondLength)
                    {
                        DefaultBondLength.SelectedItem = item;
                        break;
                    }
                }
            }

            ShowAtomsInColour.IsChecked = SystemOptions.ShowColouredAtoms;
            ShowAllCarbonAtoms.IsChecked = SystemOptions.ExplicitC;

            foreach (var item in ImplicitHydrogenMode.Items)
            {
                if (item is ComboBoxItem cbi)
                {
                    var tag = cbi.Tag.ToString();
                    if (Enum.TryParse(tag, out HydrogenLabels hydrogenLabels))
                    {
                        if (hydrogenLabels == SystemOptions.ExplicitH)
                        {
                            ImplicitHydrogenMode.SelectedItem = item;
                            break;
                        }
                    }
                }
            }

            ShowGroupingOfMolecules.IsChecked = SystemOptions.ShowMoleculeGrouping;
            ShowMolecularWeight.IsChecked = SystemOptions.ShowMolecularWeight;
            ShowMoleculeCaptions.IsChecked = SystemOptions.ShowMoleculeCaptions;

            #endregion General Tab
        }

        private void EnableButtons()
        {
            Ok.IsEnabled = Dirty;

            Defaults.IsEnabled = !SystemOptions.AreDefault();
        }

        private void LoadLibrariesListTab()
        {
            if (Globals.Chem4WordV3.ListOfDetectedLibraries == null)
            {
                // Belt and braces just in case someone clicks too early
                ReloadGlobalListOfLibraries();
            }

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
                        License = GetPropertyValue(database, "Type", "Free").Equals("Free") ? "N/A" : GetUserFromLicense(database.Connection),
                        IsDefault = isDefault
                    };
                    obj.Locked = database.IsLocked() ? "Yes" : "No";

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
                    .GetListOfLibraries(silent: true);
        }

        private string GetPropertyValue(DatabaseDetails database, string key, string defaultValue)
        {
            string result = defaultValue;

            if (database != null)
            {
                if (database.Properties.ContainsKey(key))
                {
                    result = database.Properties[key];
                }
            }

            return result;
        }

        #endregion Private methods
    }
}