// ---------------------------------------------------------------------------
//  Copyright (c) 2026, The .NET Foundation.
//  This software is released under the Apache Licence, Version 2.0.
//  The licence and further copyright text can be found in the file LICENCE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.Core.Enums;
using Chem4Word.Model2;
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
                ? $"E: {DefaultDirection} - {ElectronValue}"
                : $"H: {DefaultDirection} - {CompassValue}";
        }

        // INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        public static readonly DependencyProperty IsCheckedProperty =
            DependencyProperty.Register(nameof(IsChecked), typeof(bool?),
                                        typeof(CompassButton), new PropertyMetadata(null, OnAnyPropertyChanged));

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
                new PropertyMetadata(CompassPoints.North, OnAnyPropertyChanged));

        public CompassPoints DefaultDirection
        {
            get => (CompassPoints)GetValue(DefaultDirectionProperty);
            set => SetValue(DefaultDirectionProperty, value);
        }

        // IsElectronsMode
        public static readonly DependencyProperty IsElectronsModeProperty = DependencyProperty.Register(
            nameof(IsElectronsMode), typeof(bool), typeof(CompassButton),
                new PropertyMetadata(false, OnAnyPropertyChanged));

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

        // ElectronValue
        public static readonly DependencyProperty ElectronValueProperty = DependencyProperty.Register(
            nameof(ElectronValue), typeof(ElectronType?), typeof(CompassButton),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnValueChanged));

        public ElectronType? ElectronValue
        {
            get => (ElectronType?)GetValue(ElectronValueProperty);
            set => SetValue(ElectronValueProperty, value);
        }

        // ButtonContent
        public static readonly DependencyProperty ButtonContentProperty = DependencyProperty.Register(
            nameof(ButtonContent), typeof(object), typeof(CompassButton), new PropertyMetadata(null, OnAnyPropertyChanged));

        public object ButtonContent
        {
            get => GetValue(ButtonContentProperty);
            set => SetValue(ButtonContentProperty, value);
        }

        private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            CompassButton control = (CompassButton)d;
            control.RaiseEvent(new RoutedEventArgs(ValueChangedEvent));
            control.OnPropertyChanged(nameof(DebugInfo));
        }

        private static void OnAnyPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            CompassButton control = (CompassButton)d;
            control.OnPropertyChanged(nameof(DebugInfo));
        }

        private void OnInternalClick(object sender, RoutedEventArgs e)
        {
            if (IsElectronsMode)
            {
                // ToDo: Implement null (off) -> lone pair -> radical -> carbenoid singlet -> null (off)
                ElectronType[] values = (ElectronType[])Enum.GetValues(typeof(ElectronType));
                if (ElectronValue == null)
                {
                    ElectronValue = values.First();
                }
                else
                {
                    int index = Array.IndexOf(values, ElectronValue.Value);
                    ElectronValue = index == values.Length - 1 ? null : (ElectronType?)values[index + 1];
                }
            }
            else
            {
                CompassValue = CompassValue == null ? DefaultDirection : (CompassPoints?)null;
            }
        }

        // DebugInfo
        public string DebugInfo =>
            IsElectronsMode
                ? $"Mode=Toggle, Value={CompassValue?.ToString() ?? "null"}"
                : $"Mode=Electrons, Value={ElectronValue?.ToString() ?? "null"}";
    }
}
