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
using Chem4Word.Model2.Converters.ProtocolBuffers;
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
        private const double WidthOfAtomMode = 620;
        private const double WidthOfFunctionalGroupMode = 575;

        private bool _closedByUser;
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

            if (!DesignerProperties.GetIsInDesignMode(this))
            {
                AtomPropertiesModel = model;
                DataContext = _atomPropertiesModel;
                AtomPath.Text = _atomPropertiesModel.Path;
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

            ShowPreview();

            IsDirty = false;
            _isLoading = false;
        }

        private void SetupHydrogenLabelsDropDown()
        {
            List<Atom> atoms = _atomPropertiesModel.MicroModel.GetAllAtoms();
            Atom atom = atoms[0];

            ImplicitHydrogenMode.Items.Clear();

            TextBlock inherited = new TextBlock
            {
                Text = "Inherited from parent",
                FontStyle = FontStyles.Italic,
                Foreground = new SolidColorBrush(Colors.Gray)
            };

            ComboBoxItem notSet = new ComboBoxItem
            {
                Content = inherited,
                Tag = null
            };

            ImplicitHydrogenMode.Items.Add(notSet);
            if (!atom.ExplicitC.HasValue)
            {
                ImplicitHydrogenMode.SelectedItem = notSet;
            }

            foreach (KeyValuePair<HydrogenLabels, string> keyValuePair in EnumHelper.GetEnumValuesWithDescriptions<HydrogenLabels>())
            {
                ComboBoxItem cbi = new ComboBoxItem
                {
                    Content = keyValuePair.Value,
                    Tag = keyValuePair.Key
                };
                ImplicitHydrogenMode.Items.Add(cbi);

                if (atom.ExplicitH.HasValue
                    && atom.ExplicitH == keyValuePair.Key)
                {
                    ImplicitHydrogenMode.SelectedItem = cbi;
                }
            }
        }

        private void OnContentRendered_AtomPropertyEditor(object sender, EventArgs e)
        {
            // This moves the window to the correct position
            Point point = UIUtils.GetOnScreenCentrePoint(_atomPropertiesModel.Centre, ActualWidth, ActualHeight);
            Left = point.X;
            Top = point.Y;

            IsDirty = false;

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

            Compass1.SelectedCompassPoint = _atomPropertiesModel.ExplicitHydrogenPlacement;
            Compass2.SelectedElectronValues = _atomPropertiesModel.ExplicitElectronPlacements;
            Compass3.SelectedCompassPoint = _atomPropertiesModel.ExplicitFunctionalGroupPlacement;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void OnClick_Close(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void OnClick_Save(object sender, RoutedEventArgs e)
        {
            _atomPropertiesModel.Save = true;
            _closedByUser = true;
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
                if (option.Element is Element el
                    && el == selElement)
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
                ShowPreview();
            }
        }

        private void OnSelectionChanged_AtomPicker(object sender, SelectionChangedEventArgs e)
        {
            AtomOption option = AtomPicker.SelectedItem as AtomOption;
            _atomPropertiesModel.AddedElement = option?.Element;
            if (!_inhibitEvents)
            {
                ShowPreview();
            }
        }

        private void OnSelectionChanged_FunctionalGroupPicker(object sender, SelectionChangedEventArgs e)
        {
            AtomOption option = FunctionalGroupPicker.SelectedItem as AtomOption;
            _atomPropertiesModel.AddedElement = option?.Element;
            if (!_inhibitEvents)
            {
                ShowPreview();
            }
        }

        private void OnSelectionChanged_ChargeCombo(object sender, SelectionChangedEventArgs e)
        {
            HandleIsotopeOrChargeChange();
        }

        private void OnSelectionChanged_IsotopePicker(object sender, SelectionChangedEventArgs e)
        {
            HandleIsotopeOrChargeChange();
        }

        private void HandleIsotopeOrChargeChange()
        {
            SetStateOfExplicitCarbonCheckbox();
            if (!_inhibitEvents)
            {
                ShowPreview();
            }
        }

        private void SetStateOfExplicitCarbonCheckbox()
        {
            List<Atom> atoms = _atomPropertiesModel.MicroModel.GetAllAtoms();
            Atom atom = atoms[0];
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

        private void OnClick_ExplicitCheckBox(object sender, RoutedEventArgs e)
        {
            if (!_inhibitEvents)
            {
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

            if (_atomPropertiesModel.Element is FunctionalGroup functionalGroup
                && functionalGroup.ShowInDropDown)
            {
                FunctionalGroupPicker.SelectedItem = new AtomOption(functionalGroup);
            }
        }

        private void ShowPreview()
        {
            if (!_inhibitEvents)
            {
                _inhibitEvents = true;

                List<Atom> atoms = _atomPropertiesModel.MicroModel.GetAllAtoms();
                Atom atom = atoms[0];

                _atomPropertiesModel.ShowCompass = false;

                atom.Element = _atomPropertiesModel.Element;

                if (_atomPropertiesModel.IsElement)
                {
                    atom.FormalCharge = _atomPropertiesModel.Charge;
                    atom.ExplicitC = _atomPropertiesModel.ExplicitC;
                    atom.ExplicitH = _atomPropertiesModel.ExplicitH;
                    atom.ExplicitHPlacement = _atomPropertiesModel.ExplicitHydrogenPlacement;

                    foreach (Electron electron in atom.Electrons.Values.ToList())
                    {
                        atom.RemoveElectron(electron);
                    }

                    foreach (KeyValuePair<CompassPoints, ElectronType> pair in _atomPropertiesModel.ExplicitElectronPlacements)
                    {
                        Electron electron = new Electron
                        {
                            Count = pair.Value == ElectronType.Radical ? 1 : 2,
                            ExplicitPlacement = pair.Key,
                            Parent = atom,
                            Type = pair.Value
                        };
                        atom.AddElectron(electron);
                    }

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
                    atom.ExplicitFunctionalGroupPlacement = _atomPropertiesModel.ExplicitFunctionalGroupPlacement;
                    atom.FormalCharge = null;
                    atom.ExplicitC = null;
                    atom.ExplicitH = null;
                    atom.IsotopeNumber = null;

                    if (_atomPropertiesModel.Element is FunctionalGroup fg)
                    {
                        _atomPropertiesModel.ShowHydrogenLabels = false;
                        _atomPropertiesModel.ShowCompass = fg.Flippable;
                    }
                }

                Preview.Chemistry = _atomPropertiesModel.MicroModel.Copy();
                IsDirty = true;

                _inhibitEvents = false;
            }
        }

        private void OnClosing_AtomPropertyEditor(object sender, CancelEventArgs e)
        {
            if (!_closedByUser && IsDirty)
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

        private void OnClick_ChangeToElement(object sender, RoutedEventArgs e)
        {
            MinWidth = WidthOfAtomMode;
            Width = WidthOfAtomMode;

            List<Atom> atoms = _atomPropertiesModel.MicroModel.GetAllAtoms();
            Atom atom = atoms[0];

            if (_atomPropertiesModel.IsElement
                && _atomPropertiesModel.Element is Element)
            {
                // ToDo: Where should this data come from ???
                _atomPropertiesModel.ExplicitElectronPlacements = new Dictionary<CompassPoints, ElectronType>
                                                                  {
                                                                      {
                                                                          CompassPoints.NorthEast, ElectronType.LonePair
                                                                      }
                                                                  };
                _atomPropertiesModel.ExplicitHydrogenPlacement = null;
            }

            Compass1.CompassControlType = CompassControlType.Hydrogens;
            Compass2.CompassControlType = CompassControlType.Electrons;

            Compass1.SelectedCompassPoint = _atomPropertiesModel.ExplicitHydrogenPlacement;
            Compass2.SelectedElectronValues = _atomPropertiesModel.ExplicitElectronPlacements;

            SetVisibilityFlags(atom);
        }

        private void OnClick_ChangeToFunctionalGroup(object sender, RoutedEventArgs e)
        {
            MinWidth = WidthOfFunctionalGroupMode;
            Width = WidthOfFunctionalGroupMode;

            _atomPropertiesModel.ShowCompass = false;

            if (_atomPropertiesModel.IsFunctionalGroup
                && _atomPropertiesModel.Element is FunctionalGroup fg)
            {
                _atomPropertiesModel.ShowCompass = fg.Flippable;

                // ToDo: Where should this data come from ???
                _atomPropertiesModel.ExplicitFunctionalGroupPlacement = null;
            }

            Compass3.CompassControlType = CompassControlType.FunctionalGroups;

            Compass3.SelectedCompassPoint = _atomPropertiesModel.ExplicitFunctionalGroupPlacement;
        }

        private void SetVisibilityFlags(Atom atom)
        {
            ExplicitCheckBox.IsEnabled = !atom.IsSingleton;

            bool canShowHydrogen = ModelGlobals.PeriodicTable.ImplicitHydrogenTargets.Contains($"|{atom.Element.Symbol}|");

            _atomPropertiesModel.ShowHydrogenLabels = canShowHydrogen
                                                      || atom.IsHetero
                                                      || atom.IsTerminal;

            //bool showHydrogenLabels = atom.ShowImplicitHydrogenCharacters;
            //string atomSymbol = atom.AtomSymbol;

            //_atomPropertiesModel.ShowCompass = showHydrogenLabels && atom.ImplicitHydrogenCount > 0 && atomSymbol != "";
            // ToDo: Need to work out what the new rules are ...
            _atomPropertiesModel.ShowCompass = true;
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
                        List<Atom> atoms = _atomPropertiesModel.MicroModel.GetAllAtoms();
                        Atom atom = atoms[0];
                        if (atom.IsCarbon && hydrogenLabels == HydrogenLabels.All)
                        {
                            ExplicitCheckBox.IsEnabled = false;
                        }
                    }
                }

                if (!_inhibitEvents)
                {
                    ShowPreview();
                }

                IsDirty = true;
            }
        }

        private void OnCompassValueChanged_HydrogensCompass(object sender, WpfEventArgs e)
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
                        }
                        IsDirty = true;
                        break;
                }
            }
        }

        private void OnCompassValueChanged_FunctionalGroupsCompass(object sender, WpfEventArgs e)
        {
            if (sender is Compass compass)
            {
                switch (compass.CompassControlType)
                {
                    case CompassControlType.FunctionalGroups:
                        _atomPropertiesModel.ExplicitFunctionalGroupPlacement = compass.SelectedCompassPoint;
                        if (!_inhibitEvents)
                        {
                            ShowPreview();
                        }
                        IsDirty = true;
                        break;
                }
            }
        }

        private void OnCompassValueChanged_ElectronsCompass(object sender, WpfEventArgs e)
        {
            if (sender is Compass compass)
            {
                switch (compass.CompassControlType)
                {
                    case CompassControlType.Electrons:
                        // No need to update the model here as data binding has done it for us
                        foreach (KeyValuePair<CompassPoints, ElectronType> pair in _atomPropertiesModel.ExplicitElectronPlacements)
                        {
                            Debug.WriteLine($"{pair.Key} {pair.Value}");
                        }
                        if (!_inhibitEvents)
                        {
                            ShowPreview();
                        }
                        IsDirty = true;
                        break;
                }
            }
        }
    }
}
