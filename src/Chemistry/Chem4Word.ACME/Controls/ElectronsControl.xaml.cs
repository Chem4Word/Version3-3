// ---------------------------------------------------------------------------
//  Copyright (c) 2026, The .NET Foundation.
//  This software is released under the Apache Licence, Version 2.0.
//  The licence and further copyright text can be found in the file LICENCE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.ACME.Models;
using Chem4Word.Core.UI.Wpf;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace Chem4Word.ACME.Controls
{
    /// <summary>
    /// Interaction logic for ElectronsControl.xaml
    /// </summary>
    public partial class ElectronsControl : UserControl
    {
        private bool _inhibitEvents;

        private ElectronsControlModel _model;

        public ElectronsControlModel Model
        {
            get => _model;
            set
            {
                _model = value;
                _inhibitEvents = true;

                if (_model.InAutomaticMode)
                {
                    ShowAutomaticControls();
                }
                else
                {
                    ShowManualControls();
                }

                _inhibitEvents = false;
            }
        }

        public ElectronsControl()
        {
            InitializeComponent();

            if (!DesignerProperties.GetIsInDesignMode(this))
            {
                //
            }
        }

        public event EventHandler<WpfEventArgs> ValueChanged;

        private void OnSwitchToAutomatic(object sender, RoutedEventArgs e)
        {
            if (!_inhibitEvents)
            {
                _inhibitEvents = true;

                if (_model != null)
                {
                    _model.ConvertModelToAutomatic();
                    ShowAutomaticControls();

                    WpfEventArgs args = new WpfEventArgs
                    {
                        Message = "OnSwitchToAutomatic"
                    };

                    ValueChanged?.Invoke(this, args);
                }

                _inhibitEvents = false;
            }
        }

        private void OnSwitchToManual(object sender, RoutedEventArgs e)
        {
            if (!_inhibitEvents)
            {
                _inhibitEvents = true;

                if (_model != null)
                {
                    _model.ConvertModelToManual();
                    ShowManualControls();

                    WpfEventArgs args = new WpfEventArgs
                    {
                        Message = "OnSwitchToManual"
                    };

                    ValueChanged?.Invoke(this, args);
                }

                _inhibitEvents = false;
            }
        }

        public void ShowAutomaticControls()
        {
            ElectronsModeSwitch.Content = "Switch to Manual";
            ElectronsModeSwitch.IsChecked = true;

            ManualElectrons.Visibility = Visibility.Collapsed;
            AutomaticElectrons.Visibility = Visibility.Visible;

            Model.InAutomaticMode = true;
            AutomaticElectrons.EnableAddAutomaticElectronButton();
        }

        private void ShowManualControls()
        {
            ElectronsModeSwitch.Content = "Switch to Automatic";
            ElectronsModeSwitch.IsChecked = false;

            ManualElectrons.Visibility = Visibility.Visible;
            AutomaticElectrons.Visibility = Visibility.Collapsed;

            Model.InAutomaticMode = false;
            ManualElectrons.Refresh();
        }

        private void OnValueChanged_ManualElectrons(object sender, WpfEventArgs e)
        {
            if (_model != null && !_inhibitEvents)
            {
                _model.UpdateParentAtom();

                WpfEventArgs args = new WpfEventArgs
                {
                    Message = $"ValueChanged_ManualElectrons - {e.Message}"
                };

                ValueChanged?.Invoke(this, args);
            }
        }

        private void OnValueChanged_AutomaticElectrons(object sender, WpfEventArgs e)
        {
            if (_model != null && !_inhibitEvents)
            {
                _model.UpdateParentAtom();

                WpfEventArgs args = new WpfEventArgs
                {
                    Message = $"ValueChanged_AutomaticElectrons - {e.Message}"
                };

                ValueChanged?.Invoke(this, args);
            }
        }
    }
}
