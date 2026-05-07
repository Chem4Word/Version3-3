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
                _atomPropertiesModel = model;
                DataContext = _atomPropertiesModel;
                AtomPath.Text = _atomPropertiesModel.Path;

                Atom atom = _atomPropertiesModel.Atom;
                if (_atomPropertiesModel.IsElement)
                {
                    _atomPropertiesModel.ExplicitC = atom.ExplicitC;
                    _atomPropertiesModel.ExplicitH = atom.ExplicitH;
                    _atomPropertiesModel.ExplicitHydrogenPlacement = atom.ExplicitFunctionalGroupPlacement;
                    atom.UpdateElectronPlacements();
                }
                else
                {
                    _atomPropertiesModel.ExplicitFunctionalGroupPlacement = atom.ExplicitFunctionalGroupPlacement;
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

            ElectronsControl.Model = new ElectronsControlModel(_atomPropertiesModel.Atom, ElectronsControl);

            IsDirty = false;
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

            FunctionalGroupsCompass.SelectedCompassPoint = _atomPropertiesModel.ExplicitFunctionalGroupPlacement;

            IsDirty = false;

            _inhibitEvents = false;
            _isLoading = false;

            ShowPreview();
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
                ClearDownAtomProperties();
                ClearDownModelProperties();

                IsDirty = true;
                ShowPreview();
            }
        }

        private void OnSelectionChanged_AtomPicker(object sender, SelectionChangedEventArgs e)
        {
            AtomOption option = AtomPicker.SelectedItem as AtomOption;
            if (!_isLoading && !_inhibitEvents)
            {
                _atomPropertiesModel.AddedElement = option?.Element;

                ClearDownAtomProperties();
                ClearDownModelProperties();

                IsDirty = true;
                ShowPreview();
            }
        }

        private void OnSelectionChanged_FunctionalGroupPicker(object sender, SelectionChangedEventArgs e)
        {
            AtomOption option = FunctionalGroupPicker.SelectedItem as AtomOption;
            _atomPropertiesModel.AddedElement = option?.Element;
            if (!_inhibitEvents)
            {
                ClearDownAtomProperties();
                ClearDownModelProperties();

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
            if (!_isLoading && !_inhibitEvents)
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

                        if (string.IsNullOrEmpty(_atomPropertiesModel.Isotope))
                        {
                            atom.IsotopeNumber = null;
                        }
                        else
                        {
                            atom.IsotopeNumber = int.Parse(_atomPropertiesModel.Isotope);
                        }

                        SetVisibilityFlags(atom);

                        if (ElectronsControl.Model != null)
                        {
                            UpdateElectrons();
                        }

                        if (atom.Electrons.Any() && atom.IsCarbon)
                        {
                            atom.ExplicitC = true;
                        }

                        EnableElectronsControls(atom);
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

        private void EnableElectronsControls(Atom atom)
        {
            bool showElectronControls = !atom.IsCarbon
                                        || atom.CarbonIsShowing
                                        || atom.Electrons.Any()
                                        || EditController.CanAddAnyElectrons(atom);

            if (showElectronControls)
            {
                ElectronsControl.Visibility = Visibility.Visible;
                ElectronsLabel.Visibility = Visibility.Visible;
            }
            else
            {
                ElectronsControl.Visibility = Visibility.Collapsed;
                ElectronsLabel.Visibility = Visibility.Collapsed;
            }
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
            ElectronsControl.Model.ClearElectrons();
            ElectronsControl.ShowAutomaticControls();

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

            // Now the controls
            HydrogensCompass.IsEnabled = false;
            ImplicitHydrogenMode.SelectedIndex = 0;
        }

        private void OnClick_ChangeToElement(object sender, RoutedEventArgs e)
        {
            MinWidth = WidthOfAtomMode;
            Width = WidthOfAtomMode;

            _inhibitEvents = true;

            _atomPropertiesModel.Atom.Element = null;

            AtomPicker.SelectedIndex = -1;
            ChargeCombo.SelectedIndex = -1;

            ClearDownAtomProperties();
            ClearDownModelProperties();

            HydrogensCompass.CompassControlType = CompassControlType.Hydrogens;
            HydrogensCompass.SelectedCompassPoint = _atomPropertiesModel.ExplicitHydrogenPlacement;

            SetVisibilityFlags(_atomPropertiesModel.Atom);

            // Clear the preview
            Preview.Chemistry = new Model();

            _inhibitEvents = false;
        }

        private void OnClick_ChangeToFunctionalGroup(object sender, RoutedEventArgs e)
        {
            MinWidth = WidthOfFunctionalGroupMode;
            Width = WidthOfFunctionalGroupMode;

            _inhibitEvents = true;

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

            // Clear the preview
            Preview.Chemistry = new Model();

            _inhibitEvents = false;
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
                && !_inhibitEvents
                && ImplicitHydrogenMode.SelectedItem is ComboBoxItem cbi)
            {
                ExplicitCheckBox.IsEnabled = true;
                if (cbi.Tag is null)
                {
                    if (!_inhibitEvents)
                    {
                        _atomPropertiesModel.ExplicitH = null;
                    }
                }
                else
                {
                    if (Enum.TryParse(cbi.Tag.ToString(), out HydrogenLabels hydrogenLabels))
                    {
                        if (!_inhibitEvents)
                        {
                            _atomPropertiesModel.ExplicitH = hydrogenLabels;
                        }
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
                        if (!_inhibitEvents)
                        {
                            _atomPropertiesModel.ExplicitHydrogenPlacement = compass.SelectedCompassPoint;

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
                        if (!_inhibitEvents)
                        {
                            _atomPropertiesModel.ExplicitFunctionalGroupPlacement = compass.SelectedCompassPoint;
                            IsDirty = true;
                            ShowPreview();
                        }

                        break;
                }
            }
        }

        private void OnValueChanged_ElectronsControl(object sender, WpfEventArgs e)
        {
            UpdateElectrons();

            if (!_inhibitEvents)
            {
                if (ElectronsControl.Model.ParentAtom.Electrons.Count > 0)
                {
                    _atomPropertiesModel.ExplicitH = HydrogenLabels.All;
                    ImplicitHydrogenMode.SelectedIndex = 4;
                    _atomPropertiesModel.ExplicitC = true;
                }

                IsDirty = true;

                ShowPreview();
            }
        }

        private void UpdateElectrons()
        {
            ElectronsControl.Model.UpdateParentAtom();
            List<Electron> after = ElectronsControl.Model.ParentAtom.AllElectrons();
            _atomPropertiesModel.Atom.ClearElectrons();
            _atomPropertiesModel.Atom.AddRangeOfElectrons(after);
        }
    }
}
