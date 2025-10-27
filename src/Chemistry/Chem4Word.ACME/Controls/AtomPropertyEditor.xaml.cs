// ---------------------------------------------------------------------------
//  Copyright (c) 2025, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.ACME.Annotations;
using Chem4Word.ACME.Entities;
using Chem4Word.ACME.Models;
using Chem4Word.ACME.Utils;
using Chem4Word.Core;
using Chem4Word.Core.Helpers;
using Chem4Word.Model2;
using Chem4Word.Model2.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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
        private bool _closedByUser;
        private bool IsDirty { get; set; }
        private bool _isLoading;

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
#if DEBUG
            AtomPath.Visibility = Visibility.Visible;
#endif

            _isLoading = true;

            if (!DesignerProperties.GetIsInDesignMode(this))
            {
                AtomPropertiesModel = model;
                DataContext = AtomPropertiesModel;
                AtomPath.Text = AtomPropertiesModel.Path;
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
            InvalidateArrange();
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
            AtomPropertiesModel.Element = selElement;
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
                AtomPropertiesModel.AddedElement = selElement;
            }

            AtomOption atomPickerSelectedItem = newOption;
            AtomPicker.SelectedItem = atomPickerSelectedItem;
            ShowPreview();
        }

        private void OnSelectionChanged_AtomPicker(object sender, SelectionChangedEventArgs e)
        {
            AtomOption option = AtomPicker.SelectedItem as AtomOption;
            AtomPropertiesModel.AddedElement = option?.Element;
            ShowPreview();
        }

        private void OnSelectionChanged_FunctionalGroupPicker(object sender, SelectionChangedEventArgs e)
        {
            AtomOption option = FunctionalGroupPicker.SelectedItem as AtomOption;
            AtomPropertiesModel.AddedElement = option?.Element;
            ShowPreview();
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
            ShowPreview();
        }

        private void SetStateOfExplicitCarbonCheckbox()
        {
            List<Atom> atoms = AtomPropertiesModel.MicroModel.GetAllAtoms();
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
            ShowPreview();
        }

        private void LoadAtomItems()
        {
            AtomPicker.Items.Clear();
            foreach (string item in AcmeConstants.StandardAtoms)
            {
                AtomPicker.Items.Add(new AtomOption(ModelGlobals.PeriodicTable.Elements[item]));
            }

            if (AtomPropertiesModel.Element is Element el)
            {
                if (!AcmeConstants.StandardAtoms.Contains(el.Symbol))
                {
                    AtomPicker.Items.Add(new AtomOption(ModelGlobals.PeriodicTable.Elements[el.Symbol]));
                }

                AtomPicker.SelectedItem = new AtomOption(AtomPropertiesModel.Element as Element);
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

            if (AtomPropertiesModel.Element is FunctionalGroup functionalGroup
                && functionalGroup.ShowInDropDown)
            {
                FunctionalGroupPicker.SelectedItem = new AtomOption(functionalGroup);
            }
        }

        private void ShowPreview()
        {
            List<Atom> atoms = AtomPropertiesModel.MicroModel.GetAllAtoms();
            Atom atom = atoms[0];

            AtomPropertiesModel.ShowCompass = false;

            atom.Element = AtomPropertiesModel.Element;

            if (AtomPropertiesModel.IsElement)
            {
                atom.FormalCharge = AtomPropertiesModel.Charge;
                atom.ExplicitC = AtomPropertiesModel.ExplicitC;
                atom.ExplicitH = AtomPropertiesModel.ExplicitH;
                atom.ExplicitHPlacement = AtomPropertiesModel.ExplicitHydrogenPlacement;

                if (string.IsNullOrEmpty(AtomPropertiesModel.Isotope))
                {
                    atom.IsotopeNumber = null;
                }
                else
                {
                    atom.IsotopeNumber = int.Parse(AtomPropertiesModel.Isotope);
                }

                if (atom.Element is Element)
                {
                    SetVisibilityFlags(atom);
                }
            }

            if (AtomPropertiesModel.IsFunctionalGroup)
            {
                atom.ExplicitFunctionalGroupPlacement = AtomPropertiesModel.ExplicitFunctionalGroupPlacement;
                atom.FormalCharge = null;
                atom.ExplicitC = null;
                atom.ExplicitH = null;
                atom.IsotopeNumber = null;

                if (AtomPropertiesModel.Element is FunctionalGroup fg)
                {
                    AtomPropertiesModel.ShowHydrogenLabels = false;
                    AtomPropertiesModel.ShowCompass = fg.Flippable;
                }
            }

            Preview.Chemistry = AtomPropertiesModel.MicroModel.Copy();
            IsDirty = true;
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

        private void OnChecked_PlacementButton(object sender, RoutedEventArgs e)
        {
            ShowPreview();
        }

        private void OnChecked_FGPlacementButton(object sender, RoutedEventArgs e)
        {
            ShowPreview();
        }

        private void OnClick_Element(object sender, RoutedEventArgs e)
        {
            List<Atom> atoms = AtomPropertiesModel.MicroModel.GetAllAtoms();
            Atom atom = atoms[0];

            if (AtomPropertiesModel.IsElement
                && AtomPropertiesModel.Element is Element)
            {
                SetVisibilityFlags(atom);
            }
        }

        private void SetVisibilityFlags(Atom atom)
        {
            ExplicitCheckBox.IsEnabled = !atom.IsSingleton;

            bool canShowHydrogen = ModelGlobals.PeriodicTable.ImplicitHydrogenTargets.Contains($"|{atom.Element.Symbol}|");

            AtomPropertiesModel.ShowHydrogenLabels = canShowHydrogen
                                                     || atom.IsHetero
                                                     || atom.IsTerminal;

            bool showHydrogenLabels = atom.ShowImplicitHydrogenCharacters;
            string atomSymbol = atom.AtomSymbol;

            AtomPropertiesModel.ShowCompass = showHydrogenLabels && atom.ImplicitHydrogenCount > 0 && atomSymbol != "";
        }

        private void OnClick_FunctionalGroup(object sender, RoutedEventArgs e)
        {
            AtomPropertiesModel.ShowCompass = false;

            if (AtomPropertiesModel.IsFunctionalGroup
                && AtomPropertiesModel.Element is FunctionalGroup fg)
            {
                AtomPropertiesModel.ShowCompass = fg.Flippable;
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
                    AtomPropertiesModel.ExplicitH = null;
                }
                else
                {
                    if (Enum.TryParse(cbi.Tag.ToString(), out HydrogenLabels hydrogenLabels))
                    {
                        AtomPropertiesModel.ExplicitH = hydrogenLabels;
                        List<Atom> atoms = AtomPropertiesModel.MicroModel.GetAllAtoms();
                        Atom atom = atoms[0];
                        if (atom.IsCarbon && hydrogenLabels == HydrogenLabels.All)
                        {
                            ExplicitCheckBox.IsEnabled = false;
                        }
                    }
                }
                ShowPreview();
                IsDirty = true;
            }
        }
    }
}
