// ---------------------------------------------------------------------------
//  Copyright (c) 2026, The .NET Foundation.
//  This software is released under the Apache Licence, Version 2.0.
//  The licence and further copyright text can be found in the file LICENCE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.ACME.Annotations;
using Chem4Word.ACME.Entities;
using Chem4Word.ACME.Enums;
using Chem4Word.ACME.Models;
using Chem4Word.ACME.Utils;
using Chem4Word.Core;
using Chem4Word.Core.Enums;
using Chem4Word.Core.Helpers;
using Chem4Word.Core.UI.Wpf;
using Chem4Word.Model2;
using Chem4Word.Model2.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Media;

namespace Chem4Word.ACME.Controls
{
    /// <summary>
    /// Interaction logic for AtomPropertyEditor.xaml
    /// </summary>
    public partial class AtomPropertyEditor : INotifyPropertyChanged
    {
        private const double WidthOfAtomMode = 650;
        private const double WidthOfFunctionalGroupMode = 580;

        private bool _userClickedOk;

        private bool IsDirty { get; set; }

        private bool AutomaticElectronPlacement { get; set; }

        private bool _isLoading;
        private bool _inhibitEvents;

        private AtomPropertiesModel _atomPropertiesModel;

        public AtomPropertiesModel AtomPropertiesModel
        {
            get
            {
                return _atomPropertiesModel;
            }
            set
            {
                _atomPropertiesModel = value;
                OnPropertyChanged();
            }
        }

        public AtomPropertyEditor()
        {
            InitializeComponent();
        }

        public AtomPropertyEditor(AtomPropertiesModel model) : this()
        {
            AtomPath.Visibility = Debugger.IsAttached
                ? Visibility.Visible
                : Visibility.Collapsed;

            _isLoading = true;
            _inhibitEvents = true;

            if (!DesignerProperties.GetIsInDesignMode(this))
            {
                AtomPropertiesModel = model;
                DataContext = _atomPropertiesModel;
                AtomPath.Text = _atomPropertiesModel.Path;

                Atom atom = _atomPropertiesModel.Atom;
                AutomaticElectrons.ParentAtom = atom;

                if (_atomPropertiesModel.AutomaticElectronPlacements.Any())
                {
                    AutomaticElectrons.Model = AutomaticElectrons.Model = new AutomaticElectronsEditorModel();

                    // Setup Automatic Electrons
                    foreach (Electron electron in atom.AllElectrons())
                    {
                        AutomaticElectronItem item = new AutomaticElectronItem
                        {
                            ParentAtom = atom,
                            Id = electron.Id,
                            ElectronType = electron.TypeOfElectron
                        };
                        AutomaticElectrons.Model.AutomaticElectronItems.Add(item);
                    }
                }
            }
        }

        private void OnLoaded_AtomPropertyEditor(object sender, RoutedEventArgs e)
        {
            // This moves the window off-screen while it renders
            Point point = UIUtils.GetOffScreenPoint();
            Left = point.X;
            Top = point.Y;

            SetupHydrogenLabelsDropDown();
            LoadAtomItems();
            LoadFunctionalGroups();

            IsDirty = false;
            _isLoading = false;
        }

        private void SetupHydrogenLabelsDropDown()
        {
            Atom atom = _atomPropertiesModel.Atom;

            if (atom != null)
            {
                ImplicitHydrogenMode.Items.Clear();

                TextBlock inherited = new TextBlock
                {
                    Text = "Inherited from parent",
                    FontStyle = FontStyles.Italic,
                    Foreground = new SolidColorBrush(Colors.Gray)
                };

                ComboBoxItem notSet = new ComboBoxItem { Content = inherited, Tag = null };

                ImplicitHydrogenMode.Items.Add(notSet);
                if (!atom.ExplicitC.HasValue)
                {
                    ImplicitHydrogenMode.SelectedItem = notSet;
                }

                foreach (KeyValuePair<HydrogenLabels, string> keyValuePair in EnumHelper
                             .GetEnumValuesWithDescriptions<HydrogenLabels>())
                {
                    ComboBoxItem cbi = new ComboBoxItem { Content = keyValuePair.Value, Tag = keyValuePair.Key };
                    ImplicitHydrogenMode.Items.Add(cbi);

                    if (atom.ExplicitH.HasValue
                        && atom.ExplicitH == keyValuePair.Key)
                    {
                        ImplicitHydrogenMode.SelectedItem = cbi;
                    }
                }
            }
        }

        private void OnContentRendered_AtomPropertyEditor(object sender, EventArgs e)
        {
            // This moves the window to the correct position
            Point point = UIUtils.GetOnScreenCentrePoint(_atomPropertiesModel.Centre, ActualWidth, ActualHeight);
            Left = point.X;
            Top = point.Y;

            DataContext = _atomPropertiesModel;

            if (_atomPropertiesModel.IsFunctionalGroup)
            {
                MinWidth = WidthOfFunctionalGroupMode;
                Width = WidthOfFunctionalGroupMode;
            }
            else
            {
                MinWidth = WidthOfAtomMode;
                Width = WidthOfAtomMode;
            }

            HydrogensCompass.SelectedCompassPoint = _atomPropertiesModel.ExplicitHydrogenPlacement;

            ManualElectrons.Atom = _atomPropertiesModel.Atom;
            ManualElectrons.SelectedElectronDictionary = _atomPropertiesModel.ManualElectronPlacements;
            ManualElectrons.SelectedElectrons = _atomPropertiesModel.Atom.AllElectrons();

            FunctionalGroupsCompass.SelectedCompassPoint = _atomPropertiesModel.ExplicitFunctionalGroupPlacement;

            IsDirty = false;

            _inhibitEvents = false;
            _isLoading = false;

            ShowPreview();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void OnClick_Close(object sender, RoutedEventArgs e)
        {
            _atomPropertiesModel.Save = false;
            _userClickedOk = true;

            Close();
        }

        private void OnClick_Ok(object sender, RoutedEventArgs e)
        {
            _atomPropertiesModel.Save = true;
            _userClickedOk = true;

            Close();
        }

        private void OnElementSelected_AtomTable(object sender, VisualPeriodicTable.ElementEventArgs e)
        {
            AtomOption newOption = null;
            Element selElement = e.SelectedElement as Element;
            _atomPropertiesModel.Element = selElement;
            PeriodicTableExpander.IsExpanded = false;
            bool found = false;

            foreach (object item in AtomPicker.Items)
            {
                AtomOption option = (AtomOption)item;
                if (option.Element is Element el && el == selElement)
                {
                    found = true;
                    newOption = option;
                    break;
                }

                if (option.Element is FunctionalGroup)
                {
                    // Ignore any Functional Groups in the picker (if present)
                }
            }

            if (!found)
            {
                newOption = new AtomOption(selElement);
                AtomPicker.Items.Add(newOption);
                _atomPropertiesModel.AddedElement = selElement;
            }

            AtomOption atomPickerSelectedItem = newOption;
            AtomPicker.SelectedItem = atomPickerSelectedItem;

            if (!_inhibitEvents)
            {
                IsDirty = true;
                ShowPreview();
            }
        }

        private void OnSelectionChanged_AtomPicker(object sender, SelectionChangedEventArgs e)
        {
            AtomOption option = AtomPicker.SelectedItem as AtomOption;
            _atomPropertiesModel.AddedElement = option?.Element;
            ManualElectrons.Atom = _atomPropertiesModel.Atom;

            if (!_inhibitEvents)
            {
                IsDirty = true;
                ShowPreview();
                ManualElectrons.Refresh();
            }
        }

        private void DisableElectronsControls()
        {
            ElectronPlacementMode.Visibility = Visibility.Visible;
            ManualElectrons.Visibility = Visibility.Visible;
            AutomaticElectrons.Visibility = Visibility.Visible;
            ElectronsLabel.Visibility = Visibility.Visible;
        }

        private void EnableElectronsControls()
        {
            AutomaticElectronPlacement = !_atomPropertiesModel.ManualElectronPlacements.Any();

            Atom atom = _atomPropertiesModel.Atom;
            bool showElectronControls;

            if (atom.IsCarbon)
            {
                showElectronControls = atom.ShowSymbol;
            }
            else
            {
                showElectronControls = true;
            }

            if (showElectronControls)
            {
                ElectronPlacementMode.Visibility = Visibility.Visible;
                ManualElectrons.Visibility = Visibility.Visible;
                AutomaticElectrons.Visibility = Visibility.Visible;
                ElectronsLabel.Visibility = Visibility.Visible;

                ElectronPlacementMode.IsEnabled = true;
                ManualElectrons.IsEnabled = true;
                AutomaticElectrons.IsEnabled = true;

                if (AutomaticElectronPlacement)
                {
                    SetupAutomaticElectronPlacement();
                    AutomaticElectrons.EnableAddAutomaticElectronButton();
                    AutomaticElectrons.UpdateImages();
                }
                else
                {
                    SetupManualElectronPlacement();
                }
            }
            else
            {
                ElectronPlacementMode.Visibility = Visibility.Collapsed;
                ManualElectrons.Visibility = Visibility.Collapsed;
                AutomaticElectrons.Visibility = Visibility.Collapsed;
                ElectronsLabel.Visibility = Visibility.Collapsed;
            }
        }

        private void OnSelectionChanged_FunctionalGroupPicker(object sender, SelectionChangedEventArgs e)
        {
            AtomOption option = FunctionalGroupPicker.SelectedItem as AtomOption;
            _atomPropertiesModel.AddedElement = option?.Element;
            if (!_inhibitEvents)
            {
                IsDirty = true;
                ShowPreview();
            }
        }

        private void OnSelectionChanged_ChargePicker(object sender, SelectionChangedEventArgs e)
        {
            HandleIsotopeOrChargeChange();
        }

        private void OnSelectionChanged_IsotopePicker(object sender, SelectionChangedEventArgs e)
        {
            HandleIsotopeOrChargeChange();
        }

        private void HandleIsotopeOrChargeChange()
        {
            if (!_inhibitEvents)
            {
                IsDirty = true;
                SetStateOfExplicitCarbonCheckbox();
                ShowPreview();
            }
        }

        private void SetStateOfExplicitCarbonCheckbox()
        {
            Atom atom = _atomPropertiesModel.Atom;
            if (atom != null)
            {
                if (atom.Parent.AtomCount == 1)
                {
                    ExplicitCheckBox.IsEnabled = false;
                }
                else
                {
                    ChargeValue chargeValue = ChargeCombo.SelectedItem as ChargeValue;
                    IsotopeValue isotopeValue = IsotopePicker.SelectedItem as IsotopeValue;

                    if (chargeValue?.Value == 0 && isotopeValue?.Mass == null)
                    {
                        ExplicitCheckBox.IsEnabled = true;
                    }
                    else
                    {
                        ExplicitCheckBox.IsEnabled = false;
                    }
                }
            }
        }

        private void OnClick_ExplicitCheckBox(object sender, RoutedEventArgs e)
        {
            if (!_inhibitEvents)
            {
                IsDirty = true;
                ShowPreview();
            }
        }

        private void LoadAtomItems()
        {
            AtomPicker.Items.Clear();
            foreach (string item in AcmeConstants.StandardAtoms)
            {
                AtomPicker.Items.Add(new AtomOption(ModelGlobals.PeriodicTable.Elements[item]));
            }

            if (_atomPropertiesModel.Element is Element el)
            {
                if (!AcmeConstants.StandardAtoms.Contains(el.Symbol))
                {
                    AtomPicker.Items.Add(new AtomOption(ModelGlobals.PeriodicTable.Elements[el.Symbol]));
                }

                AtomPicker.SelectedItem = new AtomOption(_atomPropertiesModel.Element as Element);
            }
        }

        private void LoadFunctionalGroups()
        {
            FunctionalGroupPicker.Items.Clear();
            foreach (FunctionalGroup item in ModelGlobals.FunctionalGroupsList)
            {
                if (item.ShowInDropDown)
                {
                    FunctionalGroupPicker.Items.Add(new AtomOption(item));
                }
            }

            if (_atomPropertiesModel.Element is FunctionalGroup functionalGroup && functionalGroup.ShowInDropDown)
            {
                FunctionalGroupPicker.SelectedItem = new AtomOption(functionalGroup);
            }
        }

        private void ShowPreview()
        {
            if (!_inhibitEvents)
            {
                _inhibitEvents = true;

                Atom atom = _atomPropertiesModel.Atom;

                if (atom != null)
                {
                    atom.Element = _atomPropertiesModel.Element;

                    if (_atomPropertiesModel.IsElement)
                    {
                        atom.FormalCharge = _atomPropertiesModel.Charge;
                        atom.ExplicitC = _atomPropertiesModel.ExplicitC;
                        atom.ExplicitH = _atomPropertiesModel.ExplicitH;
                        atom.ExplicitHPlacement = _atomPropertiesModel.ExplicitHydrogenPlacement;

                        GatherElectrons(atom);
                        EnableElectronsControls();

                        if (string.IsNullOrEmpty(_atomPropertiesModel.Isotope))
                        {
                            atom.IsotopeNumber = null;
                        }
                        else
                        {
                            atom.IsotopeNumber = int.Parse(_atomPropertiesModel.Isotope);
                        }

                        SetVisibilityFlags(atom);
                    }

                    if (_atomPropertiesModel.IsFunctionalGroup)
                    {
                        atom.FormalCharge = null;
                        atom.ExplicitC = null;
                        atom.ExplicitH = null;
                        atom.IsotopeNumber = null;

                        atom.ExplicitFunctionalGroupPlacement = _atomPropertiesModel.ExplicitFunctionalGroupPlacement;

                        if (_atomPropertiesModel.Element is FunctionalGroup fg)
                        {
                            _atomPropertiesModel.ShowHydrogenLabels = false;
                            _atomPropertiesModel.ShowCompass = fg.Flippable;
                        }
                    }

                    Preview.Chemistry = _atomPropertiesModel.MicroModel.Copy();
                }
                else
                {
                    // Clear the display
                    Preview.Chemistry = new Model();
                }

                _inhibitEvents = false;
            }
        }

        private void GatherElectrons(Atom atom)
        {
            atom.ClearElectrons();

            if (AutomaticElectronPlacement)
            {
                foreach (AutomaticElectronItem item in AutomaticElectrons.Model.AutomaticElectronItems)
                {
                    Electron electron = new Electron
                    {
                        Parent = atom,
                        TypeOfElectron = item.ElectronType,
                        Count = item.ElectronType == ElectronType.Radical ? 1 : 2,
                    };
                    atom.AddElectron(electron);
                }
            }
            else
            {
                foreach (KeyValuePair<CompassPoints, ElectronType> pair in _atomPropertiesModel.ManualElectronPlacements)
                {
                    Electron electron = new Electron
                    {
                        Count = pair.Value == ElectronType.Radical ? 1 : 2,
                        ExplicitPlacement = pair.Key,
                        Parent = atom,
                        TypeOfElectron = pair.Value
                    };
                    atom.AddElectron(electron);
                }
            }

            atom.UpdateElectronPlacements();
        }

        private void OnClosing_AtomPropertyEditor(object sender, CancelEventArgs e)
        {
            if (!_userClickedOk && IsDirty)
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("Do you wish to save your changes?");
                sb.AppendLine("  Click 'Yes' to save your changes and exit.");
                sb.AppendLine("  Click 'No' to discard your changes and exit.");
                sb.AppendLine("  Click 'Cancel' to return to the form.");

                DialogResult dr = UserInteractions.AskUserYesNoCancel(sb.ToString());
                switch (dr)
                {
                    case System.Windows.Forms.DialogResult.Yes:
                        _atomPropertiesModel.Save = true;
                        break;

                    case System.Windows.Forms.DialogResult.No:
                        _atomPropertiesModel.Save = false;
                        break;

                    case System.Windows.Forms.DialogResult.Cancel:
                        e.Cancel = true;
                        break;
                }
            }
        }

        private void ClearDownAtomProperties()
        {
            // Clear Atom's properties
            Atom atom = _atomPropertiesModel.Atom;

            // Element properties
            atom.ExplicitC = null;
            atom.ExplicitH = null;
            atom.ExplicitHPlacement = null;

            atom.IsotopeNumber = null;
            atom.FormalCharge = null;

            atom.ClearElectrons();
            atom.UpdateElectronPlacements();

            // Functional Group properties
            atom.ExplicitFunctionalGroupPlacement = null;
        }

        private void ClearDownModelProperties()
        {
            // General properties
            _atomPropertiesModel.ExplicitC = null;
            _atomPropertiesModel.ExplicitH = null;
            _atomPropertiesModel.ExplicitHydrogenPlacement = null;
            _atomPropertiesModel.ExplicitFunctionalGroupPlacement = null;

            // Clear this control's electrons models
            _atomPropertiesModel.ManualElectronPlacements.Clear();
            _atomPropertiesModel.AutomaticElectronPlacements.Clear();
            AutomaticElectrons.Model.AutomaticElectronItems.Clear();

            HydrogensCompass.IsEnabled = false;
            ElectronPlacementMode.IsEnabled = false;
            ManualElectrons.IsEnabled = false;
            AutomaticElectrons.IsEnabled = false;
        }

        private void OnClick_ChangeToElement(object sender, RoutedEventArgs e)
        {
            MinWidth = WidthOfAtomMode;
            Width = WidthOfAtomMode;

            _inhibitEvents = true;

            _atomPropertiesModel.Atom.Element = null;

            AutomaticElectrons.DisableEvents();
            ManualElectrons.DisableEvents();

            AtomPicker.SelectedIndex = -1;
            ChargeCombo.SelectedIndex = -1;

            ClearDownAtomProperties();
            ClearDownModelProperties();

            HydrogensCompass.CompassControlType = CompassControlType.Hydrogens;
            ManualElectrons.CompassControlType = CompassControlType.Electrons;

            HydrogensCompass.SelectedCompassPoint = _atomPropertiesModel.ExplicitHydrogenPlacement;
            ManualElectrons.SelectedElectronDictionary = _atomPropertiesModel.ManualElectronPlacements;

            DisableElectronsControls();
            SetupAutomaticElectronPlacement();
            SetVisibilityFlags(_atomPropertiesModel.Atom);

            // Clear the preview
            Preview.Chemistry = new Model();

            _inhibitEvents = false;
            AutomaticElectrons.EnableEvents();
            ManualElectrons.EnableEvents();
        }

        private void OnClick_ChangeToFunctionalGroup(object sender, RoutedEventArgs e)
        {
            MinWidth = WidthOfFunctionalGroupMode;
            Width = WidthOfFunctionalGroupMode;

            _inhibitEvents = true;
            AutomaticElectrons.DisableEvents();
            ManualElectrons.DisableEvents();

            FunctionalGroupPicker.SelectedIndex = -1;

            ClearDownAtomProperties();
            ClearDownModelProperties();

            _atomPropertiesModel.ShowCompass = false;

            if (_atomPropertiesModel.IsFunctionalGroup
                && _atomPropertiesModel.Element is FunctionalGroup fg)
            {
                _atomPropertiesModel.ShowCompass = fg.Flippable;
            }

            FunctionalGroupsCompass.CompassControlType = CompassControlType.FunctionalGroups;
            DisableElectronsControls();

            // Clear the preview
            Preview.Chemistry = new Model();

            _inhibitEvents = false;
            AutomaticElectrons.EnableEvents();
            ManualElectrons.EnableEvents();
        }

        private void SetVisibilityFlags(Atom atom)
        {
            if (atom != null && atom.Element != null)
            {
                ExplicitCheckBox.IsEnabled = !atom.IsSingleton;

                bool canShowHydrogen = ModelGlobals.PeriodicTable.ImplicitHydrogenTargets.Contains($"|{atom.Element.Symbol}|");

                _atomPropertiesModel.ShowHydrogenLabels = canShowHydrogen
                                                          || atom.IsHetero
                                                          || atom.IsTerminal;

                // ToDo: Need to work out what the new rules are ...
                //bool showHydrogenLabels = atom.ShowImplicitHydrogenCharacters
                //string atomSymbol = atom.AtomSymbol
                //_atomPropertiesModel.ShowCompass = showHydrogenLabels && atom.ImplicitHydrogenCount > 0 && atomSymbol != ""
                _atomPropertiesModel.ShowCompass = true;
            }
        }

        private void OnSelectionChanged_ImplicitHydrogenMode(object sender, SelectionChangedEventArgs e)
        {
            if (!_isLoading
                && ImplicitHydrogenMode.SelectedItem is ComboBoxItem cbi)
            {
                ExplicitCheckBox.IsEnabled = true;
                if (cbi.Tag is null)
                {
                    _atomPropertiesModel.ExplicitH = null;
                }
                else
                {
                    if (Enum.TryParse(cbi.Tag.ToString(), out HydrogenLabels hydrogenLabels))
                    {
                        _atomPropertiesModel.ExplicitH = hydrogenLabels;
                        Atom atom = _atomPropertiesModel.Atom;
                        if (atom != null && atom.IsCarbon && hydrogenLabels == HydrogenLabels.All)
                        {
                            ExplicitCheckBox.IsEnabled = false;
                        }
                    }
                }

                if (!_inhibitEvents)
                {
                    IsDirty = true;
                    ShowPreview();
                }
            }
        }

        private void OnValueChanged_Hydrogens(object sender, WpfEventArgs e)
        {
            if (sender is Compass compass)
            {
                switch (compass.CompassControlType)
                {
                    case CompassControlType.Hydrogens:
                        _atomPropertiesModel.ExplicitHydrogenPlacement = compass.SelectedCompassPoint;

                        if (!_inhibitEvents)
                        {
                            ShowPreview();
                            IsDirty = true;
                        }

                        break;
                }
            }
        }

        private void OnValueChanged_FunctionalGroups(object sender, WpfEventArgs e)
        {
            if (sender is Compass compass)
            {
                switch (compass.CompassControlType)
                {
                    case CompassControlType.FunctionalGroups:
                        _atomPropertiesModel.ExplicitFunctionalGroupPlacement = compass.SelectedCompassPoint;

                        if (!_inhibitEvents)
                        {
                            IsDirty = true;
                            ShowPreview();
                        }

                        break;
                }
            }
        }

        private void OnValueChanged_ManualElectrons(object sender, WpfEventArgs e)
        {
            if (sender is Compass compass)
            {
                switch (compass.CompassControlType)
                {
                    case CompassControlType.Electrons:
                        // No need to update the model here as data binding has done it for us
                        foreach (KeyValuePair<CompassPoints, ElectronType> pair in _atomPropertiesModel.ManualElectronPlacements)
                        {
                            Debug.WriteLine($"{pair.Key} {pair.Value}");
                        }

                        if (!_inhibitEvents)
                        {
                            IsDirty = true;
                            ShowPreview();
                        }

                        break;
                }
            }
        }

        private void OnValueChanged_AutomaticElectrons(object sender, WpfEventArgs e)
        {
            Debug.WriteLine(e.Button);
            if (sender is ElectronsEditor && !_inhibitEvents)
            {
                IsDirty = true;
                _atomPropertiesModel.Atom.UpdateElectronPlacements();
                ShowPreview();
            }
        }

        private void OnSwitchToManual(object sender, RoutedEventArgs e)
        {
            if (!_inhibitEvents && _atomPropertiesModel != null)
            {
                _inhibitEvents = true;

                AutomaticElectrons.DisableEvents();
                ManualElectrons.DisableEvents();

                _atomPropertiesModel.ManualElectronPlacements.Clear();

                Atom atom = _atomPropertiesModel.Atom;
                atom.UpdateElectronPlacements();

                foreach (Electron item in _atomPropertiesModel.Atom.AllElectrons())
                {
                    CompassPoints cp = item.Placement;

                    while (_atomPropertiesModel.ManualElectronPlacements.ContainsKey(cp))
                    {
                        cp = Model2.Helpers.Utils.NextCompassPoint(cp);
                    }

                    _atomPropertiesModel.ManualElectronPlacements.Add(cp, item.TypeOfElectron);
                }

                ManualElectrons.SelectedElectronDictionary = _atomPropertiesModel.ManualElectronPlacements;
                ManualElectrons.SelectedElectrons = _atomPropertiesModel.Atom.AllElectrons();

                AutomaticElectronPlacement = false;

                SetupManualElectronPlacement();

                AutomaticElectrons.Model.AutomaticElectronItems.Clear();

                _inhibitEvents = false;

                ShowPreview();

                AutomaticElectrons.EnableEvents();
                ManualElectrons.EnableEvents();

                IsDirty = true;
            }
        }

        private void OnSwitchToAutomatic(object sender, RoutedEventArgs e)
        {
            if (!_inhibitEvents && _atomPropertiesModel != null)
            {
                _inhibitEvents = true;

                AutomaticElectrons.DisableEvents();
                ManualElectrons.DisableEvents();

                AutomaticElectrons.Model.AutomaticElectronItems.Clear();
                AutomaticElectrons.ParentAtom = _atomPropertiesModel.Atom;

                int index = 0;
                SortedDictionary<CompassPoints, ElectronType> temp = new SortedDictionary<CompassPoints, ElectronType>(_atomPropertiesModel.ManualElectronPlacements);
                foreach (ElectronType electronType in temp.Values)
                {
                    AutomaticElectronItem item = new AutomaticElectronItem
                    {
                        ParentAtom = _atomPropertiesModel.Atom,
                        Id = $"{index++}",
                        ElectronType = electronType
                    };

                    AutomaticElectrons.Model.AutomaticElectronItems.Add(item);
                }

                _atomPropertiesModel.ManualElectronPlacements.Clear();

                AutomaticElectronPlacement = true;

                SetupAutomaticElectronPlacement();
                AutomaticElectrons.UpdateImages();

                _inhibitEvents = false;

                _atomPropertiesModel.MicroModel.AutoPlaceElectrons();

                ShowPreview();

                AutomaticElectrons.EnableEvents();
                ManualElectrons.EnableEvents();

                IsDirty = true;
            }
        }

        private void SetupAutomaticElectronPlacement()
        {
            ElectronPlacementMode.IsChecked = true;
            ElectronPlacementMode.Content = "Automatic Placement";
            ElectronPlacementMode.ToolTip = "Switch to Manual Placement Mode";

            AutomaticElectrons.Visibility = Visibility.Visible;
            ManualElectrons.Visibility = Visibility.Collapsed;

            AutomaticElectronPlacement = true;
            _atomPropertiesModel.Atom.UpdateElectronPlacements();
        }

        private void SetupManualElectronPlacement()
        {
            ElectronPlacementMode.IsChecked = false;
            ElectronPlacementMode.Content = "Manual Placement";
            ElectronPlacementMode.ToolTip = "Switch to Automatic Placement Mode";

            AutomaticElectrons.Visibility = Visibility.Collapsed;
            ManualElectrons.Visibility = Visibility.Visible;

            AutomaticElectronPlacement = false;
        }
    }
}
