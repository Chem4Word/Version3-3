// ---------------------------------------------------------------------------
//  Copyright (c) 2026, The .NET Foundation.
//  This software is released under the Apache Licence, Version 2.0.
//  The licence and further copyright text can be found in the file LICENCE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.Core.UI.Wpf;
using Chem4Word.Model2;
using Chem4Word.Model2.Enums;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace Chem4Word.ACME.Controls
{
    /// <summary>
    /// Interaction logic for ElectronsView.xaml
    /// </summary>
    public partial class ElectronsView : UserControl
    {
        public Atom Atom { get; set; }
        public ObservableCollection<Electron> Electrons { get; set; } = new ObservableCollection<Electron>();

        private bool _isLoading;
        private bool _inhibitEvents;
        private bool _inAddOrRemove;

        private ElectronType? _lastChosen;

        public event EventHandler<WpfEventArgs> ElectronsValueChanged;

        public ElectronsView()
        {
            InitializeComponent();

            _inhibitEvents = true;
            _isLoading = true;
        }

        private void OnLoaded_ElectronsView(object sender, RoutedEventArgs e)
        {
            EnableAddRemoveButtons();

            _isLoading = false;
            _inhibitEvents = false;
        }

        private void OnSelectionChanged_ElectronsListView(object sender, SelectionChangedEventArgs e)
        {
            Debug.WriteLine($"ElectronsListView.SelectedIndex: {ElectronsListView.SelectedIndex}");
            EnableAddRemoveButtons();
        }

        private void OnSelectionChanged_Selector(object sender, SelectionChangedEventArgs e)
        {
            //Debug.WriteLine("=== OnSelectionChanged_Selector() called ===");
            //StackTrace stack = new StackTrace(true); // true = include file/line numbers
            //Debug.WriteLine(StackHelper.ShowStack(stack));

            if (!_inhibitEvents && !_inAddOrRemove)
            {
                if (sender is ComboBox combo
                    && combo.DataContext is Electron electron)
                {
                    electron.Count = electron.TypeOfElectron == ElectronType.Radical ? 1 : 2;
                    _lastChosen = electron.TypeOfElectron;
                }

                RaiseChangedEvent();
            }
        }

        private void OnClick_Add(object sender, RoutedEventArgs e)
        {
            if (!_isLoading
                && Electrons.Count < 8)
            {
                _inAddOrRemove = true;
                _isLoading = true;

                ElectronsListView.SelectedItem = null;

                Electron electron = new Electron
                {
                    Parent = Atom,
                    TypeOfElectron = ElectronType.Radical,
                    Count = 1
                };

                if (_lastChosen != null)
                {
                    electron.TypeOfElectron = _lastChosen.Value;
                    electron.Count = electron.TypeOfElectron == ElectronType.Radical ? 1 : 2;
                }

                Electrons.Add(electron);

                ElectronsListView.ItemsSource = null;
                ElectronsListView.ItemsSource = Electrons;

                EnableAddRemoveButtons();

                _isLoading = false;
                _inAddOrRemove = false;

                RaiseChangedEvent();
            }
        }

        private void OnClick_Delete(object sender, RoutedEventArgs e)
        {
            if (!_isLoading
                && ElectronsListView.SelectedItem is Electron electron)
            {
                _inAddOrRemove = true;
                _isLoading = true;

                ElectronsListView.SelectedItem = null;

                Electrons.Remove(electron);
                ElectronsListView.ItemsSource = null;
                ElectronsListView.ItemsSource = Electrons;

                EnableAddRemoveButtons();

                _isLoading = false;
                _inAddOrRemove = false;

                RaiseChangedEvent();
            }
        }

        public void EnableAddRemoveButtons()
        {
            Remove.IsEnabled = ElectronsListView.SelectedIndex >= 0;
            Add.IsEnabled = Electrons.Count < 8;
        }

        public void DisableEvents()
        {
            _inhibitEvents = true;
        }

        public void EnableEvents()
        {
            _inhibitEvents = false;
        }

        private void RaiseChangedEvent()
        {
            WpfEventArgs args = new WpfEventArgs
            {
                Button = "ElectronsView"
            };

            ElectronsValueChanged?.Invoke(this, args);
        }

        public string ListElectrons()
        {
            StringBuilder stringBuilder = new StringBuilder();

            stringBuilder.AppendLine($"{Electrons.Count} Electrons");
            foreach (Electron electron in Electrons)
            {
                stringBuilder.AppendLine($" {electron}");
                Debug.WriteLine($"{electron} - {electron.Path}");
            }

            return stringBuilder.ToString();
        }
    }
}
