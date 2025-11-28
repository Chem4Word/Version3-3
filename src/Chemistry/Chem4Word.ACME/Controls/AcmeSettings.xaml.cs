// ---------------------------------------------------------------------------
//  Copyright (c) 2026, The .NET Foundation.
//  This software is released under the Apache Licence, Version 2.0.
//  The licence and further copyright text can be found in the file LICENCE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.ACME.Models;
using Chem4Word.Core.Helpers;
using Chem4Word.Core.UI.Wpf;
using Chem4Word.Model2;
using Chem4Word.Model2.Annotations;
using Chem4Word.Model2.Enums;
using IChem4Word.Contracts;
using System;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using UserControl = System.Windows.Controls.UserControl;

namespace Chem4Word.ACME.Controls
{
    /// <summary>
    ///     Interaction logic for AcmeSettings.xaml
    /// </summary>
    public partial class AcmeSettings : UserControl, INotifyPropertyChanged
    {
        private static string _product = Assembly.GetExecutingAssembly().FullName.Split(',')[0];
        private static string _class = MethodBase.GetCurrentMethod().DeclaringType?.Name;

        public IChem4WordTelemetry Telemetry { get; set; }

        public Point TopLeft { get; set; }

        public bool Dirty { get; set; }

        public event EventHandler<WpfEventArgs> OnButtonClick;

        private RenderingOptions _currentOptions;
        private RenderingOptions _originalOptions;

        public RenderingOptions UserDefaultOptions { get; set; }

        public RenderingOptions CurrentOptions
        {
            get { return _currentOptions; }
            set
            {
                if (value != null)
                {
                    _currentOptions = value;
                    _originalOptions = value.Copy();

                    var model = new SettingsModel
                    {
                        ShowMoleculeGrouping = _currentOptions.ShowMoleculeGrouping,
                        ExplicitH = _currentOptions.ExplicitH,
                        ShowColouredAtoms = _currentOptions.ShowColouredAtoms,
                        ShowMolecularWeight = _currentOptions.ShowMolecularWeight,
                        ShowMoleculeCaptions = _currentOptions.ShowMoleculeCaptions,
                        ExplicitC = _currentOptions.ExplicitC
                    };

                    SettingsModel = model;

#if !DEBUG
                    DebugTab.Visibility = Visibility.Collapsed;
#endif

                    DataContext = SettingsModel;
                }
            }
        }

        private SettingsModel _model;

        public SettingsModel SettingsModel
        {
            get { return _model; }
            set
            {
                _model = value;
                OnPropertyChanged();
            }
        }

        private bool _loading;

        public AcmeSettings()
        {
            _loading = true;
            InitializeComponent();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void OnLoaded_AcmeSettings(object sender, RoutedEventArgs e)
        {
            ImplicitHydrogenMode.ItemsSource = EnumHelper.GetEnumValuesWithDescriptions<HydrogenLabels>();
            ImplicitHydrogenMode.DisplayMemberPath = "Value";
            ImplicitHydrogenMode.SelectedValuePath = "Key";
            ImplicitHydrogenMode.SetBinding(Selector.SelectedValueProperty, new Binding("ExplicitH") { Mode = BindingMode.TwoWay });

            _loading = false;
            if (CurrentOptions != null)
            {
                Dirty = false;
                EnableButtons();
            }
        }

        private void OnClick_Defaults(object sender, RoutedEventArgs e)
        {
            SettingsModel.ExplicitC = UserDefaultOptions.ExplicitC;
            SettingsModel.ExplicitH = UserDefaultOptions.ExplicitH;
            SettingsModel.ShowColouredAtoms = UserDefaultOptions.ShowColouredAtoms;
            SettingsModel.ShowMoleculeGrouping = UserDefaultOptions.ShowMoleculeGrouping;
            SettingsModel.ShowMolecularWeight = UserDefaultOptions.ShowMolecularWeight;
            SettingsModel.ShowMoleculeCaptions = UserDefaultOptions.ShowMoleculeCaptions;

            Dirty = true;
            EnableButtons();

            OnPropertyChanged(nameof(SettingsModel));
        }

        private void OnClick_Cancel(object sender, RoutedEventArgs e)
        {
            CurrentOptions = _originalOptions.Copy();
            Dirty = false;

            var args = new WpfEventArgs
            {
                Button = "CANCEL"
            };
            OnButtonClick?.Invoke(this, args);
        }

        private void OnClick_Save(object sender, RoutedEventArgs e)
        {
            // Copy current model values to options before saving
            CurrentOptions.ExplicitC = SettingsModel.ExplicitC;
            CurrentOptions.ExplicitH = SettingsModel.ExplicitH;
            CurrentOptions.ShowColouredAtoms = SettingsModel.ShowColouredAtoms;
            CurrentOptions.ShowMoleculeGrouping = SettingsModel.ShowMoleculeGrouping;
            CurrentOptions.ShowMolecularWeight = SettingsModel.ShowMolecularWeight;
            CurrentOptions.ShowMoleculeCaptions = SettingsModel.ShowMoleculeCaptions;

            var args = new WpfEventArgs
            {
                Button = "SAVE"
            };
            OnButtonClick?.Invoke(this, args);
        }

        private void OnSelectionChanged_ImplicitHydrogenMode(object sender, SelectionChangedEventArgs e)
        {
            if (!_loading)
            {
                CurrentOptions.ExplicitH = SettingsModel.ExplicitH;
                Dirty = true;
                EnableButtons();
            }
        }

        private void OnClick_ShowAtomsInColour(object sender, RoutedEventArgs e)
        {
            if (!_loading)
            {
                CurrentOptions.ShowColouredAtoms = SettingsModel.ShowColouredAtoms;
                Dirty = true;
                EnableButtons();
            }
        }

        private void OnClick_ShowAllCarbonAtoms(object sender, RoutedEventArgs e)
        {
            if (!_loading)
            {
                CurrentOptions.ExplicitC = SettingsModel.ExplicitC;
                Dirty = true;
                EnableButtons();
            }
        }

        private void OnClick_ShowMoleculeGroups(object sender, RoutedEventArgs e)
        {
            if (!_loading)
            {
                CurrentOptions.ShowMoleculeGrouping = SettingsModel.ShowMoleculeGrouping;
                Dirty = true;
                EnableButtons();
            }
        }

        private void OnClick_ShowMolecularWeight(object sender, RoutedEventArgs e)
        {
            if (!_loading)
            {
                CurrentOptions.ShowMolecularWeight = SettingsModel.ShowMolecularWeight;
                Dirty = true;
                EnableButtons();
            }
        }

        private void OnClick_ShowMoleculeCaptions(object sender, RoutedEventArgs e)
        {
            if (!_loading)
            {
                CurrentOptions.ShowMoleculeCaptions = SettingsModel.ShowMoleculeCaptions;
                Dirty = true;
                EnableButtons();
            }
        }

        private void EnableButtons()
        {
            Ok.IsEnabled = Dirty;

            var settingsChanged = SettingsModel.ExplicitC != UserDefaultOptions.ExplicitC
                                  || SettingsModel.ExplicitH != UserDefaultOptions.ExplicitH
                                  || SettingsModel.ShowMoleculeGrouping != UserDefaultOptions.ShowMoleculeGrouping
                                  || SettingsModel.ShowMolecularWeight != UserDefaultOptions.ShowMolecularWeight
                                  || SettingsModel.ShowMoleculeCaptions != UserDefaultOptions.ShowMoleculeCaptions
                                  || SettingsModel.ShowColouredAtoms != UserDefaultOptions.ShowColouredAtoms;

            Defaults.IsEnabled = settingsChanged;
        }
    }
}