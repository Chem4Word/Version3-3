// ---------------------------------------------------------------------------
//  Copyright (c) 2022, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.ACME;
using Chem4Word.ACME.Models;
using Chem4Word.Core.UI.Forms;
using Chem4Word.Core.UI.Wpf;
using Chem4Word.Helpers;
using Chem4Word.UI.WPF;
using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace Chem4Word.Library
{
    /// <summary>
    /// Interaction logic for LibraryViewControl.xaml
    /// </summary>
    public partial class LibraryViewControl : UserControl
    {
        private static string _product = Assembly.GetExecutingAssembly().FullName.Split(',')[0];
        private static string _class = MethodBase.GetCurrentMethod()?.DeclaringType?.Name;

        private AcmeOptions _options;

        public LibraryViewControl()
        {
            InitializeComponent();
        }

        public void SetOptions(AcmeOptions options)
        {
            _options = options;
        }

        public bool ShowAllCarbonAtoms => _options.ShowCarbons;
        public bool ShowImplicitHydrogens => _options.ShowHydrogens;
        public bool ShowAtomsInColour => _options.ColouredAtoms;
        public bool ShowMoleculeGrouping => _options.ShowMoleculeGrouping;
        private string _selectedLibrary = string.Empty;

        private void OnLoaded_LibraryViewControl(object sender, RoutedEventArgs e)
        {
            // Disable the selection changed event while loading the combo box
            LibrarySelector.SelectionChanged -= OnSelectionChanged_LibrarySelector;

            var libraries = Globals.Chem4WordV3.ListOfDetectedLibraries;
            if (libraries != null)
            {
                _selectedLibrary = libraries.SelectedLibrary;

                LibrarySelector.Items.Clear();
                var index = 0;
                foreach (var database in libraries.AvailableDatabases)
                {
                    LibrarySelector.Items.Add(database.DisplayName);
                    if (database.DisplayName.Equals(libraries.SelectedLibrary))
                    {
                        LibrarySelector.SelectedIndex = index;
                    }
                    index++;
                }

                EnableEditThisLibrayButton();

                // Enable the selection changed event
                LibrarySelector.SelectionChanged += OnSelectionChanged_LibrarySelector;
            }

            // Sort the list of structures by lower case name
            var view = (ListCollectionView)CollectionViewSource.GetDefaultView(LibraryList.ItemsSource);
            if (view != null)
            {
                view.CustomSort = new ChemistryObjectComparer();
            }
        }

        private void OnSelectionChanged_LibrarySelector(object sender, SelectionChangedEventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod()?.Name}()";

            if (e.AddedItems.Count > 0)
            {
                if (e.AddedItems[0] is string selected)
                {
                    if (!_selectedLibrary.Equals(selected))
                    {
                        Globals.Chem4WordV3.Telemetry.Write(module, "Action", $"Library changed to '{selected}'");

                        var listOfDetectedLibraries = Globals.Chem4WordV3.ListOfDetectedLibraries;
                        listOfDetectedLibraries.SelectedLibrary = selected;

                        new LibraryFileHelper(Globals.Chem4WordV3.Telemetry, Globals.Chem4WordV3.AddInInfo.ProgramDataPath)
                            .SaveFile(listOfDetectedLibraries);

                        Globals.Chem4WordV3.ListOfDetectedLibraries
                            = new LibraryFileHelper(Globals.Chem4WordV3.Telemetry, Globals.Chem4WordV3.AddInInfo.ProgramDataPath)
                                .GetListOfLibraries();

                        EnableEditThisLibrayButton();

                        _selectedLibrary = selected;

                        var controller = new LibraryController(Globals.Chem4WordV3.Telemetry);
                        DataContext = controller;

                        var doc = Globals.Chem4WordV3.Application.ActiveDocument;
                        var sel = Globals.Chem4WordV3.Application.Selection;
                        Globals.Chem4WordV3.SelectChemistry(doc, sel);
                    }
                }
            }
        }

        private void EnableEditThisLibrayButton()
        {
            var details = Globals.Chem4WordV3.GetSelectedDatabaseDetails();
            if (details != null)
            {
                if (details.Properties.ContainsKey("Owner"))
                {
                    var owner = details.Properties["Owner"];
                    if (owner.Equals("User"))
                    {
                        EditThisLibrary.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        EditThisLibrary.Visibility = Visibility.Collapsed;
                    }
                }
                else
                {
                    EditThisLibrary.Visibility = Visibility.Collapsed;
                }
            }
        }

        private void OnClick_ItemButton(object sender, RoutedEventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod()?.Name}()";

            try
            {
                if (Globals.Chem4WordV3.EventsEnabled
                    && e.OriginalSource is WpfEventArgs source)
                {
                    Globals.Chem4WordV3.Telemetry.Write(module, "Action", $"Source: {source.Button} Data: {source.OutputValue}");

                    var parts = source.OutputValue.Split('=');
                    var item = long.Parse(parts[1]);

                    if (DataContext is LibraryController controller)
                    {
                        var clicked = controller.ChemistryItems.FirstOrDefault(c => c.Id == item);
                        if (clicked != null)
                        {
                            Globals.Chem4WordV3.EventsEnabled = false;
                            var activeDocument = DocumentHelper.GetActiveDocument();

                            if (Globals.Chem4WordV3.Application.Documents.Count > 0
                                && activeDocument?.ActiveWindow?.Selection != null)
                            {
                                switch (source.Button)
                                {
                                    case "Library|InsertCopy":
                                        TaskPaneHelper.InsertChemistry(true, Globals.Chem4WordV3.Application, clicked.CmlFromChemistry(), true);
                                        break;
                                }
                            }

                            Globals.Chem4WordV3.EventsEnabled = true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                using (var form = new ReportError(Globals.Chem4WordV3.Telemetry, Globals.Chem4WordV3.WordTopLeft, module, ex))
                {
                    form.ShowDialog();
                }
            }
            finally
            {
                Globals.Chem4WordV3.EventsEnabled = true;
            }
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

        /// <summary>
        /// Handles filtering of the library list
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnClick_SearchButton(object sender, RoutedEventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                if (!string.IsNullOrWhiteSpace(SearchBox.Text)
                    && DataContext != null)
                {
                    Globals.Chem4WordV3.Telemetry.Write(module, "Information", $"Filter library by '{SearchBox.Text}'");

                    //get the view from the listbox's source
                    ICollectionView view = CollectionViewSource.GetDefaultView(((LibraryController)DataContext).ChemistryItems);
                    //then try to match part of either its name or an alternative name to the string typed in
                    view.Filter = ci =>
                                  {
                                      var item = ci as ChemistryObject;
                                      var queryString = SearchBox.Text.ToUpper();
                                      return item != null
                                             && (item.Name.ToUpper().Contains(queryString)
                                                 || item.ChemicalNames.Any(n => n.ToUpper().Contains(queryString)));
                                  };
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

        private void OnClick_ClearButton(object sender, RoutedEventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                SearchBox.Clear();

                if (DataContext != null)
                {
                    ICollectionView view = CollectionViewSource.GetDefaultView(((LibraryController)DataContext).ChemistryItems);
                    view.Filter = null;
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

        private void OnClick_EditLibrary(object sender, RoutedEventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod()?.Name}()";

            Globals.Chem4WordV3.Telemetry.Write(module, "Action", $"Editing library '{_selectedLibrary}'");

            var editor = new LibraryEditorHost();
            editor.TopLeft = Globals.Chem4WordV3.WordTopLeft;
            editor.Telemetry = Globals.Chem4WordV3.Telemetry;
            editor.SelectedDatabase = _selectedLibrary;
            editor.ShowDialog();

            var controller = new LibraryController(Globals.Chem4WordV3.Telemetry);
            DataContext = controller;

            // Sort the list of structures by lower case name
            var view = (ListCollectionView)CollectionViewSource.GetDefaultView(LibraryList.ItemsSource);
            view.CustomSort = new ChemistryObjectComparer();

            var doc = Globals.Chem4WordV3.Application.ActiveDocument;
            var sel = Globals.Chem4WordV3.Application.Selection;
            Globals.Chem4WordV3.SelectChemistry(doc, sel);
        }

        private DependencyObject GetScrollViewer(DependencyObject o)
        {
            // https://stackoverflow.com/a/50004583/2527555

            // Return the DependencyObject if it is a ScrollViewer
            if (o is ScrollViewer)
            {
                return o;
            }

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(o); i++)
            {
                var child = VisualTreeHelper.GetChild(o, i);

                var result = GetScrollViewer(child);
                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }

        private void HandleScrollSpeed(object sender, MouseWheelEventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            try
            {
                if (sender is DependencyObject dependencyObject
                    && GetScrollViewer(dependencyObject) is ScrollViewer scrollViewer)
                {
                    var items = scrollViewer.ExtentHeight;
                    var current = scrollViewer.VerticalOffset;
                    var amount = Math.Max(Math.Min(scrollViewer.ViewportHeight, 3), 1);

                    // e.Delta is +ve for scroll up and -ve for scroll down
                    if (e.Delta > 0 && current > 0)
                    {
                        scrollViewer.ScrollToVerticalOffset(current - amount);
                    }
                    if (e.Delta < 0 && current < items)
                    {
                        scrollViewer.ScrollToVerticalOffset(current + amount);
                    }

                    e.Handled = true;
                }
            }
            catch (Exception ex)
            {
                Globals.Chem4WordV3.Telemetry.Write(module, "Exception", ex.Message);
                Globals.Chem4WordV3.Telemetry.Write(module, "Exception", ex.StackTrace);
            }
        }
    }
}