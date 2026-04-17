// ---------------------------------------------------------------------------
//  Copyright (c) 2026, The .NET Foundation.
//  This software is released under the Apache Licence, Version 2.0.
//  The licence and further copyright text can be found in the file LICENCE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.ACME.Annotations;
using Chem4Word.ACME.Models;
using Chem4Word.Core.Helpers;
using Chem4Word.Core.UI.Wpf;
using Chem4Word.Model2;
using Chem4Word.Model2.Enums;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Chem4Word.ACME.Controls
{
    /// <summary>
    /// Interaction logic for AutomaticElectrons.xaml
    /// </summary>
    public partial class ElectronsEditor : UserControl, INotifyPropertyChanged
    {
        private AutomaticElectronsEditorModel _model;

        private int _id;

        private bool _inhibitEvents;

        public Atom ParentAtom { get; set; }

        public event EventHandler<WpfEventArgs> ElectronsValueChanged;

        public AutomaticElectronsEditorModel Model
        {
            get
            {
                return _model;
            }
            set
            {
                _model = value;
                DataContext = _model;
                OnPropertyChanged();
            }
        }

        public ElectronsEditor()
        {
            InitializeComponent();
            if (!DesignerProperties.GetIsInDesignMode(this))
            {
                Model = new AutomaticElectronsEditorModel();
            }

            ComboBox.SelectedIndex = 0;
        }

        private void RaiseChangedEvent()
        {
            WpfEventArgs args = new WpfEventArgs
            {
                Button = "ElectronsEditor"
            };

            ElectronsValueChanged?.Invoke(this, args);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void OnClick_Add(object sender, RoutedEventArgs e)
        {
            AutomaticElectronItem item = new AutomaticElectronItem
            {
                ParentAtom = ParentAtom,
                Id = $"e{_id++}",
                ElectronType = _selectedType
            };

            Model.AutomaticElectronItems.Add(item);
            RaiseChangedEvent();
        }

        private void OnDeleteRowClicked(object sender, RoutedEventArgs e)
        {
            if (sender is Button deleteButton && deleteButton.DataContext is AutomaticElectronItem item)
            {
                Model.AutomaticElectronItems.Remove(item);
                RaiseChangedEvent();
            }
        }

        public string ListElectrons()
        {
            StringBuilder stringBuilder = new StringBuilder();

            stringBuilder.AppendLine($"{Model.AutomaticElectronItems.Count} Electrons");
            foreach (AutomaticElectronItem item in Model.AutomaticElectronItems)
            {
                string text = $"{item.Id} - {item.ElectronType.GetDescription()}";
                stringBuilder.AppendLine(text);
            }

            return stringBuilder.ToString();
        }

        private ElectronType _selectedType = ElectronType.Radical;

        private void OnSelectionChanged_ElectronType(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox combo && combo.SelectedItem is ElectronType type)
            {
                _selectedType = type;
            }
        }

        private void OnChanged_ElectronTypeCombo(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox combo
                && combo.DataContext is AutomaticElectronItem item
                && combo.SelectedItem is ElectronType type)
            {
                item.ElectronType = type;
                item.ButtonContent = CreateElectronsModeCanvas(item);
                OnPropertyChanged();
                if (!_inhibitEvents)
                {
                    RaiseChangedEvent();
                }
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

        public void DisableEvents()
        {
            _inhibitEvents = true;
        }

        public void EnableEvents()
        {
            _inhibitEvents = false;
        }
    }
}
