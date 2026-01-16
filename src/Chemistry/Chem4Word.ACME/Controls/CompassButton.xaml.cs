// ---------------------------------------------------------------------------
//  Copyright (c) 2026, The .NET Foundation.
//  This software is released under the Apache Licence, Version 2.0.
//  The licence and further copyright text can be found in the file LICENCE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.Core.Enums;
using Chem4Word.Model2.Enums;
using System;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Chem4Word.ACME.Controls
{
    public partial class CompassButton : UserControl, INotifyPropertyChanged
    {
        public CompassButton()
        {
            InitializeComponent();
        }

        public override string ToString()
        {
            return IsElectronsMode
                ? $"E: {Name} - {DefaultDirection} - {ElectronTypeValue}"
                : $"H: {Name} - {DefaultDirection} - {CompassValue}";
        }

        // INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        public static readonly DependencyProperty IsCheckedProperty =
            DependencyProperty.Register(nameof(IsChecked), typeof(bool?),
                                        typeof(CompassButton), new PropertyMetadata(null, null));

        public bool? IsChecked
        {
            get => (bool?)GetValue(IsCheckedProperty);
            set => SetValue(IsCheckedProperty, value);
        }

        // RoutedEvent
        public static readonly RoutedEvent ValueChangedEvent =
            EventManager.RegisterRoutedEvent(nameof(ValueChanged),
                                             RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(CompassButton));

        public event RoutedEventHandler ValueChanged
        {
            add => AddHandler(ValueChangedEvent, value);
            remove => RemoveHandler(ValueChangedEvent, value);
        }

        // DefaultDirection
        public static readonly DependencyProperty DefaultDirectionProperty = DependencyProperty.Register(
            nameof(DefaultDirection), typeof(CompassPoints), typeof(CompassButton),
                new PropertyMetadata(CompassPoints.North, null));

        public CompassPoints DefaultDirection
        {
            get => (CompassPoints)GetValue(DefaultDirectionProperty);
            set => SetValue(DefaultDirectionProperty, value);
        }

        // IsElectronsMode
        public static readonly DependencyProperty IsElectronsModeProperty = DependencyProperty.Register(
            nameof(IsElectronsMode), typeof(bool), typeof(CompassButton),
                new PropertyMetadata(false, null));

        public bool IsElectronsMode
        {
            get => (bool)GetValue(IsElectronsModeProperty);
            set => SetValue(IsElectronsModeProperty, value);
        }

        // CompassValue
        public static readonly DependencyProperty CompassValueProperty = DependencyProperty.Register(
            nameof(CompassValue), typeof(CompassPoints?), typeof(CompassButton),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnValueChanged));

        public CompassPoints? CompassValue
        {
            get => (CompassPoints?)GetValue(CompassValueProperty);
            set => SetValue(CompassValueProperty, value);
        }

        // ElectronTypeValue
        public static readonly DependencyProperty ElectronTypeValueProperty = DependencyProperty.Register(
            nameof(ElectronTypeValue), typeof(ElectronType?), typeof(CompassButton),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnValueChanged));

        public ElectronType? ElectronTypeValue
        {
            get => (ElectronType?)GetValue(ElectronTypeValueProperty);
            set => SetValue(ElectronTypeValueProperty, value);
        }

        // ButtonContent
        public static readonly DependencyProperty ButtonContentProperty = DependencyProperty.Register(
            nameof(ButtonContent), typeof(object), typeof(CompassButton), new PropertyMetadata(null, null));

        public object ButtonContent
        {
            get => GetValue(ButtonContentProperty);
            set => SetValue(ButtonContentProperty, value);
        }

        private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            CompassButton control = (CompassButton)d;
            control.RaiseEvent(new RoutedEventArgs(ValueChangedEvent));
        }

        private void OnInternalClick(object sender, RoutedEventArgs e)
        {
            if (IsElectronsMode)
            {
                // ToDo: Implement null (off) -> lone pair -> radical -> carbenoid singlet -> null (off)
                ElectronType[] values = (ElectronType[])Enum.GetValues(typeof(ElectronType));
                if (ElectronTypeValue == null)
                {
                    ElectronTypeValue = values.First();
                }
                else
                {
                    int index = Array.IndexOf(values, ElectronTypeValue.Value);
                    ElectronTypeValue = index == values.Length - 1 ? null : (ElectronType?)values[index + 1];
                }
            }
            else
            {
                CompassValue = CompassValue == null ? DefaultDirection : (CompassPoints?)null;
            }
        }
    }
}
