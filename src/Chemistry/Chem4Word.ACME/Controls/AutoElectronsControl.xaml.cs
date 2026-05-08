// ---------------------------------------------------------------------------
//  Copyright (c) 2026, The .NET Foundation.
//  This software is released under the Apache Licence, Version 2.0.
//  The licence and further copyright text can be found in the file LICENCE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.ACME.Annotations;
using Chem4Word.ACME.Models;
using Chem4Word.Core.UI.Wpf;
using Chem4Word.Model2;
using Chem4Word.Model2.Enums;
using Chem4Word.Model2.Helpers;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Chem4Word.ACME.Controls
{
    /// <summary>
    /// Interaction logic for AutoElectronsControl.xaml
    /// </summary>
    public partial class AutoElectronsControl : INotifyPropertyChanged
    {
        private AutomaticElectronsControlModel _model;
        private bool _inhibitEvents;
        private ElectronType _selectedType = ElectronType.Radical;

        private Atom _parentAtom;

        public Atom ParentAtom
        {
            get => _parentAtom;
            set
            {
                _parentAtom = value;
                EnableAddAutomaticElectronButton();
            }
        }

        public AutomaticElectronsControlModel Model
        {
            get
            {
                return _model;
            }
            set
            {
                _model = value;
                DataContext = _model;

                _parentAtom = _model.ParentAtom;
                ComboBox.SelectedIndex = 0;
                EnableAddAutomaticElectronButton();

                OnPropertyChanged();
            }
        }

        public AutoElectronsControl()
        {
            InitializeComponent();

            _inhibitEvents = true;
        }

        private void AutoElectronsControl_OnLoaded(object sender, RoutedEventArgs e)
        {
            _inhibitEvents = false;
        }

        public event EventHandler<WpfEventArgs> ValueChanged;

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void RaiseChangedEvent(string button, string cause)
        {
            WpfEventArgs args = new WpfEventArgs
            {
                Button = button,
                Message = $"AutomaticElectronsEditor: {button} {cause}"
            };

            ValueChanged?.Invoke(this, args);
        }

        private void OnSelectionChanged_NewElectronTypePicker(object sender, SelectionChangedEventArgs e)
        {
            if (!_inhibitEvents)
            {
                if (sender is ComboBox combo && combo.SelectedItem is ElectronType type)
                {
                    _selectedType = type;
                    EnableAddAutomaticElectronButton();
                }
            }
        }

        private void OnSelectionChanged_ExistingElectronTypePicker(object sender, SelectionChangedEventArgs e)
        {
            if (!_inhibitEvents)
            {
                if (sender is ComboBox combo
                    && combo.DataContext is AutomaticElectronItem item
                    && combo.SelectedItem is ElectronType type)
                {
                    ApplyAutomaticElectronsToAtom();
                    EnableAddAutomaticElectronButton();

                    item.ElectronType = type;
                    item.ButtonContent = CreateElectronsModeCanvas(item);

                    RaiseChangedEvent("ChangedElectron", $"Electron {item.Id} set to '{type}'");
                    OnPropertyChanged();
                }
            }
        }

        private void OnDeleteRowClicked(object sender, RoutedEventArgs e)
        {
            if (sender is Button deleteButton && deleteButton.DataContext is AutomaticElectronItem item)
            {
                Model.AutomaticElectronItems.Remove(item);
                ApplyAutomaticElectronsToAtom();
                EnableAddAutomaticElectronButton();

                RaiseChangedEvent("DeletedElectron", $"Deleted {item.Id} '{item.ElectronType}'");
            }
        }

        private void OnClick_Add(object sender, RoutedEventArgs e)
        {
            int id = 0;

            foreach (AutomaticElectronItem electronItem in Model.AutomaticElectronItems)
            {
                if (int.TryParse(electronItem.Id.Substring(1), out int value))
                {
                    id = Math.Max(id, value);
                }
            }

            id++;

            AutomaticElectronItem item = ElectronsControlModel.MakeAutomaticElectronItem(ParentAtom, $"e{id}", _selectedType);
            Model.AutomaticElectronItems.Add(item);
            ApplyAutomaticElectronsToAtom();
            EnableAddAutomaticElectronButton();

            // No need to raise event here as OnSelectionChanged_ExistingElectronTypePicker is fired by AutomaticElectronItems.Add(...)
        }

        private void ApplyAutomaticElectronsToAtom()
        {
            _parentAtom.ClearElectrons();
            int index = 1;
            foreach (AutomaticElectronItem item in Model.AutomaticElectronItems)
            {
                Electron electron = ElectronHelper.MakeElectron(_parentAtom, index++, item.ElectronType);
                _parentAtom.AddElectron(electron);
            }
            _parentAtom.UpdateElectronPlacements();
        }

        public void EnableAddAutomaticElectronButton()
        {
            if (ParentAtom == null)
            {
                AddElectron.IsEnabled = false;
            }
            else
            {
                bool enableAdd = false;

                switch (_selectedType)
                {
                    case ElectronType.Radical:
                        enableAdd = EditController.CanAddRadical(ParentAtom);
                        break;

                    case ElectronType.LonePair:
                        enableAdd = EditController.CanAddLonePair(ParentAtom);
                        break;

                    case ElectronType.Carbenoid:
                        enableAdd = EditController.CanAddCarbenoidElectrons(ParentAtom);
                        break;
                }

                AddElectron.IsEnabled = enableAdd;
            }
        }

        private Canvas CreateElectronsModeCanvas(AutomaticElectronItem item)
        {
            double width = 24;
            double height = 24;

            double halfWidth = width / 2.0;
            double halfHeight = height / 2.0;

            double offset = 4.0;
            double spotSize = 3.0;

            // Create a canvas which does not overlap the button's borders
            Canvas canvas = new Canvas
            {
                Width = width,
                Height = height,
                Background = Brushes.LightGray
            };

            switch (item.ElectronType)
            {
                case ElectronType.Radical:
                    AddDot(halfWidth, halfHeight);
                    break;

                case ElectronType.LonePair:
                    AddPairOfDots();
                    break;

                case ElectronType.Carbenoid:
                    AddLine();
                    break;
            }

            return canvas;

            // Local Functions

            void AddLine()
            {
                CreatePoints(out Point start, out Point end);

                // Create a Line
                Line line = new Line
                {
                    X1 = start.X,
                    Y1 = start.Y,
                    X2 = end.X,
                    Y2 = end.Y,
                    StrokeThickness = spotSize / 2.0,
                    StrokeEndLineCap = PenLineCap.Round,
                    StrokeStartLineCap = PenLineCap.Round
                };

                string colour = AcmeConstants.DefaultTextColor;
                if (item.ParentAtom != null)
                {
                    colour = item.ParentAtom.Element.Colour;
                }

                line.Stroke = new SolidColorBrush((Color)ColorConverter.ConvertFromString(colour));

                canvas.Children.Add(line);
            }

            void AddDot(double x, double y)
            {
                // Create a dot
                Ellipse ellipse = new Ellipse
                {
                    Width = spotSize,
                    Height = spotSize,
                    StrokeThickness = 1.0
                };

                string colour = AcmeConstants.DefaultTextColor;
                if (item.ParentAtom != null)
                {
                    colour = item.ParentAtom.Element.Colour;
                }

                ellipse.Stroke = new SolidColorBrush((Color)ColorConverter.ConvertFromString(colour));
                ellipse.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString(colour));

                // Position ellipse on canvas
                Canvas.SetLeft(ellipse, x - 2.0);
                Canvas.SetTop(ellipse, y - 2.0);
                canvas.Children.Add(ellipse);
            }

            void CreatePoints(out Point p1, out Point p2)
            {
                p1 = new Point(halfWidth - offset, halfHeight);
                p2 = new Point(halfWidth + offset, halfHeight);
            }

            void AddPairOfDots()
            {
                CreatePoints(out Point p1, out Point p2);

                AddDot(p1.X, p1.Y);
                AddDot(p2.X, p2.Y);
            }
        }
    }
}
