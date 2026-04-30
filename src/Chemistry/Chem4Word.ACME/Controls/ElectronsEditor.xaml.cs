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
using System.Linq;
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
        public ElectronType SelectedType = ElectronType.Radical;
        public int ElectronsToBeAdded = 1;
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

            EnableAddAutomaticElectronButton();
            ComboBox.SelectedIndex = 0;
        }

        private void RaiseChangedEvent(string button, string cause)
        {
            WpfEventArgs args = new WpfEventArgs
            {
                Button = button,
                Message = $"ElectronsEditor: {button} {cause}"
            };

            ElectronsValueChanged?.Invoke(this, args);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
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

        private void OnClick_Add(object sender, RoutedEventArgs e)
        {
            DisableEvents();

            AutomaticElectronItem item = new AutomaticElectronItem
            {
                ParentAtom = ParentAtom,
                Id = $"e{_id++}",
                ElectronType = SelectedType
            };

            EnableEvents();

            Model.AutomaticElectronItems.Add(item);

            EnableAddAutomaticElectronButton();
        }

        private void OnDeleteRowClicked(object sender, RoutedEventArgs e)
        {
            if (sender is Button deleteButton && deleteButton.DataContext is AutomaticElectronItem item)
            {
                DisableEvents();
                Model.AutomaticElectronItems.Remove(item);
                EnableAddAutomaticElectronButton();
                RaiseChangedEvent("DeletedElectron",$"Item {item.Id} '{item.ElectronType}' Deleted");
                EnableEvents();
            }
        }

        private void OnSelectionChanged_NewElectronTypePicker(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox combo && combo.SelectedItem is ElectronType type)
            {
                SelectedType = type;
                ElectronsToBeAdded = type == ElectronType.Radical ? 1 : 2;
                EnableAddAutomaticElectronButton();
            }
        }

        private void OnSelectionChanged_ExistingElectronTypePicker(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox combo
                && combo.DataContext is AutomaticElectronItem item
                && combo.SelectedItem is ElectronType type)
            {
                if (!_inhibitEvents)
                {
                    OnPropertyChanged();
                    EnableAddAutomaticElectronButton();

                    item.ElectronType = type;
                    item.ButtonContent = CreateElectronsModeCanvas(item);
                    RaiseChangedEvent("ChangedElectron", $"Setting {item.Id} type to '{type}'");
                }
            }
        }

        public void UpdateImages()
        {
            foreach (AutomaticElectronItem item in _model.AutomaticElectronItems)
            {
                item.ButtonContent = CreateElectronsModeCanvas(item);
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

        public void EnableAddAutomaticElectronButton()
        {
            if (ParentAtom == null)
            {
                AddElectron.IsEnabled = false;
            }
            else
            {
                // #1 Radical(s) and Carbenoid(s)
                // These affect ImplicitH Count
                // Radical count cannot exceed implicit H count
                // Carbenoid count cannot exceed implicit H count /2 (rounded down)

                // #2 Lone Pairs
                // Subtract 14 from the group of the element.
                //   The result gives you the number of lone pairs the atom can support.

                // Or to make it even simpler, don't allow more than 4 radicals, 2 carbenoids, or 4 lone pairs.

                int group = 0;
                if (ParentAtom.Element is Element element)
                {
                    group = element.Group;
                }

                bool enableAdd = false;

                int remaining;
                switch (SelectedType)
                {
                    case ElectronType.Radical:
                        remaining = ParentAtom.ImplicitHydrogenCount;
                        if (remaining > 0)
                        {
                            enableAdd = true;
                        }
                        break;

                    case ElectronType.LonePair:
                        int possible = group - 14;
                        int used = ParentAtom.Electrons.Values.Count(t => t.TypeOfElectron == ElectronType.LonePair);
                        if (used < possible)
                        {
                            enableAdd = true;
                        }
                        break;

                    case ElectronType.Carbenoid:
                        remaining = ParentAtom.ImplicitHydrogenCount / 2;
                        if (remaining > 0)
                        {
                            enableAdd = true;
                        }
                        break;
                }

                AddElectron.IsEnabled = enableAdd;
            }
        }
    }
}
