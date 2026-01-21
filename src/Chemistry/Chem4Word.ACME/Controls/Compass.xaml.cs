// ---------------------------------------------------------------------------
//  Copyright (c) 2026, The .NET Foundation.
//  This software is released under the Apache Licence, Version 2.0.
//  The licence and further copyright text can be found in the file LICENCE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.ACME.Enums;
using Chem4Word.Core.Enums;
using Chem4Word.Core.UI.Wpf;
using Chem4Word.Model2;
using Chem4Word.Model2.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace Chem4Word.ACME.Controls
{
    /// <summary>
    /// Interaction logic for Compass.xaml
    /// </summary>
    public partial class Compass : UserControl
    {
        #region Dependency Properties

        public static readonly DependencyProperty CompassControlTypeProperty = DependencyProperty.Register(
            nameof(CompassControlType), typeof(CompassControlType), typeof(Compass),
            new FrameworkPropertyMetadata(CompassControlType.Hydrogens,
                                          FrameworkPropertyMetadataOptions.AffectsRender
                                               | FrameworkPropertyMetadataOptions.AffectsArrange
                                               | FrameworkPropertyMetadataOptions.AffectsMeasure));

        public CompassControlType CompassControlType
        {
            get { return (CompassControlType)GetValue(CompassControlTypeProperty); }
            set { SetValue(CompassControlTypeProperty, value); }
        }

        public static readonly DependencyProperty ExplicitHydrogenPlacementProperty = DependencyProperty.Register(
            nameof(ExplicitHydrogenPlacement), typeof(bool?), typeof(Compass),
                new PropertyMetadata(null));

        public bool? ExplicitHydrogenPlacement
        {
            get => (bool?)GetValue(ExplicitHydrogenPlacementProperty);
            set => SetValue(ExplicitHydrogenPlacementProperty, value);
        }

        public static readonly DependencyProperty SelectedCompassPointProperty = DependencyProperty.Register(
            nameof(SelectedCompassPoint), typeof(CompassPoints?), typeof(Compass),
                new PropertyMetadata(null));

        public CompassPoints? SelectedCompassPoint
        {
            get { return (CompassPoints?)GetValue(SelectedCompassPointProperty); }
            set { SetValue(SelectedCompassPointProperty, value); }
        }

        public static readonly DependencyProperty SelectedElectronDictionaryProperty = DependencyProperty.Register(
            nameof(SelectedElectronDictionary), typeof(Dictionary<CompassPoints, ElectronType>), typeof(Compass),
            new PropertyMetadata(new Dictionary<CompassPoints, ElectronType>()));

        public Dictionary<CompassPoints, ElectronType> SelectedElectronDictionary
        {
            get { return (Dictionary<CompassPoints, ElectronType>)GetValue(SelectedElectronDictionaryProperty); }
            set { SetValue(SelectedElectronDictionaryProperty, value); }
        }

        #endregion Dependency Properties

        public List<Electron> SelectedElectrons { get; set; } = new List<Electron>();
        public Atom Atom { get; set; }

        private bool _inhibitEvents;

        public Compass()
        {
            InitializeComponent();

            DataContext = this;
        }

        public event EventHandler<WpfEventArgs> CompassValueChanged;

        public bool ElectronsMode
        {
            get
            {
                return CompassControlType == CompassControlType.Electrons;
            }
        }

        public void Refresh()
        {
            _inhibitEvents = true;

            CollapseAllButtons();

            switch (CompassControlType)
            {
                case CompassControlType.Hydrogens:
                    InitializeHydrogensMode();
                    ClearHydrogensOrFunctionalGroupsMode();
                    break;

                case CompassControlType.FunctionalGroups:
                    InitializeFunctionalGroupsMode();
                    ClearHydrogensOrFunctionalGroupsMode();
                    break;

                case CompassControlType.Electrons:
                    InitializeElectronsMode();
                    ClearElectronsMode();
                    break;
            }

            SetButtonStates();

            _inhibitEvents = false;
        }

        public void DisableEvents()
        {
            _inhibitEvents = true;
        }

        public void EnableEvents()
        {
            _inhibitEvents = false;
        }

        public void SetButtonStates()
        {
            if (ElectronsMode)
            {
                List<CompassButton> buttons = GetVisibleButtons();
                foreach (CompassButton button in buttons)
                {
                    if (button.Name != "Centre")
                    {
                        ElectronType? electronType = GetElectronType(button);
                        button.ElectronTypeValue = electronType;
                        button.ButtonContent = CreateElectronsModeCanvas(electronType, button);
                        button.IsElectronsMode = true;
                    }
                }
            }
            else
            {
                if (SelectedCompassPoint != null)
                {
                    if (CompassControlType == CompassControlType.Hydrogens)
                    {
                        switch (SelectedCompassPoint)
                        {
                            case CompassPoints.North:
                                North.CompassValue = SelectedCompassPoint;
                                break;

                            case CompassPoints.East:
                                East.CompassValue = SelectedCompassPoint;
                                break;

                            case CompassPoints.South:
                                South.CompassValue = SelectedCompassPoint;
                                break;

                            case CompassPoints.West:
                                West.CompassValue = SelectedCompassPoint;
                                break;
                        }
                    }

                    if (CompassControlType == CompassControlType.FunctionalGroups)
                    {
                        switch (SelectedCompassPoint)
                        {
                            case CompassPoints.East:
                                Right.CompassValue = SelectedCompassPoint;
                                break;

                            case CompassPoints.West:
                                Left.CompassValue = SelectedCompassPoint;
                                break;
                        }
                    }
                }
                else
                {
                    // just needs to be set to anything other than null
                    Centre.CompassValue = CompassPoints.North;
                    Centre.IsElectronsMode = false;

                    Middle.CompassValue = CompassPoints.North;
                    Middle.IsElectronsMode = false;
                }
            }
        }

        private void ClearElectronsMode()
        {
            North.ButtonContent = CreateElectronsModeCanvas(null, North);
            NorthEast.ButtonContent = CreateElectronsModeCanvas(null, NorthEast);
            East.ButtonContent = CreateElectronsModeCanvas(null, East);
            SouthEast.ButtonContent = CreateElectronsModeCanvas(null, SouthEast);
            South.ButtonContent = CreateElectronsModeCanvas(null, South);
            SouthWest.ButtonContent = CreateElectronsModeCanvas(null, SouthWest);
            West.ButtonContent = CreateElectronsModeCanvas(null, West);
            NorthWest.ButtonContent = CreateElectronsModeCanvas(null, NorthEast);
        }

        private void ClearHydrogensOrFunctionalGroupsMode()
        {
            // Hydrogens Mode
            North.CompassValue = null;
            East.CompassValue = null;
            South.CompassValue = null;
            West.CompassValue = null;

            Centre.CompassValue = null;

            // FunctionalGroups Mode
            Left.CompassValue = null;
            Right.CompassValue = null;

            Middle.CompassValue = null;
        }

        private ElectronType? GetElectronType(CompassButton button)
        {
            ElectronType? result = null;

            if (SelectedElectronDictionary.ContainsKey(button.DefaultDirection))
            {
                SelectedElectronDictionary.TryGetValue(button.DefaultDirection, out ElectronType value);
                result = value;
            }

            return result;
        }

        private void CollapseAllButtons()
        {
            List<CompassButton> buttons = GetAllButtons();
            foreach (CompassButton button in buttons)
            {
                button.Visibility = Visibility.Collapsed;
            }
        }

        private void ClearAllVisibleButtons()
        {
            List<CompassButton> buttons = GetVisibleButtons();
            foreach (CompassButton button in buttons)
            {
                button.CompassValue = null;
            }
        }

        private void InitializeElectronsMode()
        {
            North.IsElectronsMode = true;
            North.ButtonContent = CreateElectronsModeCanvas(null, North);

            NorthEast.IsElectronsMode = true;
            NorthEast.ButtonContent = CreateElectronsModeCanvas(null, NorthEast);

            East.IsElectronsMode = true;
            East.ButtonContent = CreateElectronsModeCanvas(null, East);

            SouthEast.IsElectronsMode = true;
            SouthEast.ButtonContent = CreateElectronsModeCanvas(null, SouthEast);

            South.IsElectronsMode = true;
            South.ButtonContent = CreateElectronsModeCanvas(null, South);

            SouthWest.IsElectronsMode = true;
            SouthWest.ButtonContent = CreateElectronsModeCanvas(null, SouthWest);

            West.IsElectronsMode = true;
            West.ButtonContent = CreateElectronsModeCanvas(null, West);

            NorthWest.IsElectronsMode = true;
            NorthWest.ButtonContent = CreateElectronsModeCanvas(null, NorthWest);

            // Centre Button (C for Clear)
            Centre.IsElectronsMode = true;
            Centre.ButtonContent = CreateCanvasWithSingleLetter(Centre, true);
        }

        private void InitializeFunctionalGroupsMode()
        {
            Left.ButtonContent = CreateCanvasWithSingleLetter(Left);
            Left.IsElectronsMode = false;
            Right.ButtonContent = CreateCanvasWithSingleLetter(Right);
            Right.IsElectronsMode = false;

            Middle.ButtonContent = CreateCanvasWithSingleLetter(Middle);
            Middle.IsElectronsMode = false;
        }

        private void InitializeHydrogensMode()
        {
            North.ButtonContent = CreateCanvasWithSingleLetter(North);
            North.IsElectronsMode = false;
            East.ButtonContent = CreateCanvasWithSingleLetter(East);
            East.IsElectronsMode = false;
            South.ButtonContent = CreateCanvasWithSingleLetter(South);
            South.IsElectronsMode = false;
            West.ButtonContent = CreateCanvasWithSingleLetter(West);
            West.IsElectronsMode = false;

            Centre.ButtonContent = CreateCanvasWithSingleLetter(Centre);
            Centre.IsElectronsMode = false;
        }

        private void OnIsVisibleChanged_Grid1(object sender, DependencyPropertyChangedEventArgs e)
        {
            Grid1.InvalidateArrange();
            Grid1.UpdateLayout();

            // Schedule async work without blocking
            Application.Current.Dispatcher.BeginInvoke(
                DispatcherPriority.ApplicationIdle,
                new Action(() =>
                           {
                               Refresh();
                           }));
        }

        private void OnIsVisibleChanged_Grid2(object sender, DependencyPropertyChangedEventArgs e)
        {
            Grid2.InvalidateArrange();
            Grid2.UpdateLayout();

            // Schedule async work without blocking
            Application.Current.Dispatcher.BeginInvoke(
                DispatcherPriority.ApplicationIdle,
                new Action(() =>
                           {
                               Refresh();
                           }));
        }

        private void OnLoaded_Compass(object sender, RoutedEventArgs e)
        {
            if (!DesignerProperties.GetIsInDesignMode(this))
            {
                Refresh();
            }
        }

        private void OnValueChanged_CompassButton(object sender, RoutedEventArgs e)
        {
            if (sender is CompassButton button
                && !_inhibitEvents)
            {
                _inhibitEvents = true;

                if (ElectronsMode)
                {
                    if (button.Name == "Centre")
                    {
                        SelectedElectronDictionary.Clear();
                        button.ElectronTypeValue = null;
                    }
                    else
                    {
                        if (button.ElectronTypeValue.HasValue)
                        {
                            SelectedElectronDictionary[button.DefaultDirection] = button.ElectronTypeValue.Value;
                        }
                        else
                        {
                            SelectedElectronDictionary.Remove(button.DefaultDirection);
                        }
                    }

                    SetButtonStates();
                }
                else
                {
                    CompassPoints? compassValue = button.CompassValue;

                    ClearAllVisibleButtons();

                    // Centre button special case
                    if (button.Name == "Centre" || button.Name == "Middle")
                    {
                        SelectedCompassPoint = null;
                    }
                    else
                    {
                        SelectedCompassPoint = compassValue;
                        button.CompassValue = SelectedCompassPoint;
                    }

                    // Auto select Centre or Middle button when no other buttons selected
                    List<CompassButton> buttons = GetVisibleButtons();
                    int emptyButtons = buttons.Count(b => b.CompassValue != null);
                    if (emptyButtons == 0)
                    {
                        if (CompassControlType == CompassControlType.Hydrogens)
                        {
                            Centre.CompassValue = CompassPoints.North;
                        }
                        else
                        {
                            Middle.CompassValue = CompassPoints.North;
                        }
                    }
                }

                WpfEventArgs args = new WpfEventArgs
                {
                    Button = button.Name
                };

                CompassValueChanged?.Invoke(this, args);

                _inhibitEvents = false;
            }
        }

        public string ListCompassElectrons()
        {
            StringBuilder stringBuilder = new StringBuilder();

            stringBuilder.AppendLine($"{SelectedElectronDictionary.Count} Electron Values selected");
            foreach (KeyValuePair<CompassPoints, ElectronType> keyValuePair in SelectedElectronDictionary)
            {
                Electron electron = new Electron
                {
                    ExplicitPlacement = keyValuePair.Key,
                    TypeOfElectron = keyValuePair.Value,
                    Count = keyValuePair.Value == ElectronType.Radical ? 1 : 2
                };
                stringBuilder.AppendLine($"  {electron}");
            }

            return stringBuilder.ToString();
        }

        private List<CompassButton> GetAllButtons()
        {
            List<CompassButton> buttons = FindVisualChildren<CompassButton>(this)
                .ToList();
            return buttons;
        }

        private List<CompassButton> GetVisibleButtons()
        {
            List<CompassButton> buttons = FindVisualChildren<CompassButton>(this)
                                          .Where(b => b.Visibility == Visibility.Visible)
                .ToList();
            return buttons;
        }

        private static IEnumerable<T> FindVisualChildren<T>(DependencyObject parent) where T : DependencyObject
        {
            if (parent == null)
            {
                yield break;
            }

            int count = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < count; i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(parent, i);

                if (child is T typedChild)
                {
                    yield return typedChild;
                }

                foreach (T descendant in FindVisualChildren<T>(child))
                {
                    yield return descendant;
                }
            }
        }

        private object CreateCanvasWithSingleLetter(CompassButton button, bool grey = false)
        {
            button.Visibility = Visibility.Visible;

            if (button.ActualWidth > 0 && button.ActualHeight > 0)
            {
                double width = button.ActualWidth;
                double height = button.ActualHeight;

                // Button has Padding of 2 and Border of 2
                double padding = 4.0;

                // Create a canvas which does not overlap the button's borders
                Canvas canvas = new Canvas
                {
                    Width = width - padding,
                    Height = height - padding,
                    Background = grey ? Brushes.LightGray : Brushes.Transparent
                };

                // Clip the canvas so that the (orange or gray) border is not mangled
                canvas.Clip = new RectangleGeometry
                {
                    Rect = new Rect(0, 0, canvas.Width, canvas.Height),
                    RadiusX = 3,
                    RadiusY = 3
                };

                Grid grid = new Grid
                {
                    Width = canvas.Width,
                    Height = canvas.Height
                };

                string character = "?";

                // Handle Centre and Middle Buttons
                if (button.Tag is string)
                {
                    switch (button.Name)
                    {
                        case "Centre":
                            character = button.IsElectronsMode ? "C" : "A";
                            break;

                        case "Middle":
                            character = "A";
                            break;
                    }
                }

                // Handle other buttons
                if (button.Tag is CompassPoints)
                {
                    if (CompassControlType == CompassControlType.Hydrogens)
                    {
                        switch (button.DefaultDirection)
                        {
                            case CompassPoints.North:
                                character = "N";
                                break;

                            case CompassPoints.East:
                                character = "E";
                                break;

                            case CompassPoints.South:
                                character = "S";
                                break;

                            case CompassPoints.West:
                                character = "W";
                                break;
                        }
                    }

                    if (CompassControlType == CompassControlType.FunctionalGroups)
                    {
                        switch (button.DefaultDirection)
                        {
                            case CompassPoints.East:
                                character = "E";
                                break;

                            case CompassPoints.West:
                                character = "W";
                                break;
                        }
                    }
                }

                TextBlock textBlock = new TextBlock
                {
                    Text = character,
                    FontSize = width / 2.0,
                    Foreground = Brushes.Black,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };

                grid.Children.Add(textBlock);
                canvas.Children.Add(grid);

                return canvas;
            }

            return null;
        }

        private object CreateElectronsModeCanvas(ElectronType? electronType, CompassButton button)
        {
            button.Visibility = Visibility.Visible;

            if (button.ActualWidth > 0 && button.ActualHeight > 0)
            {
                double width = button.ActualWidth;
                double height = button.ActualHeight;

                double halfWidth = width / 2.0;
                double halfHeight = height / 2.0;

                double offset = halfWidth / 6.0;

                // Button has Padding of 2 and Border of 2
                double padding = 4.0;

                // Create a canvas which does not overlap the button's borders
                Canvas canvas = new Canvas
                {
                    Width = width - (padding * 2),
                    Height = height - (padding * 2),
                    Background = Brushes.Transparent
                };

                if (electronType != null)
                {
                    switch (electronType)
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

                    if (Debugger.IsAttached)
                    {
                        ShowEnumValue();
                    }
                }

                return canvas;

                // Local Functions

                void ShowEnumValue()
                {
                    TextBlock textBlock = new TextBlock
                    {
                        Text = $"E:{(int)electronType}",
                        FontSize = 10,
                        Foreground = Brushes.Blue
                    };

                    Canvas.SetLeft(textBlock, 0.0);
                    Canvas.SetTop(textBlock, 0.0);

                    canvas.Children.Add(textBlock);
                }

                void AddLine()
                {
                    CreatePoints(out Point start, out Point end);

                    // Create a Line
                    Line line = new Line
                    {
                        X1 = start.X - padding,
                        Y1 = start.Y - padding,
                        X2 = end.X - padding,
                        Y2 = end.Y - padding,
                        Stroke = Brushes.Red,
                        StrokeThickness = offset / 2.0
                    };

                    string colour = Atom.Element.Colour;
                    line.Stroke = new SolidColorBrush((Color)ColorConverter.ConvertFromString(colour ?? AcmeConstants.DefaultTextColor));

                    canvas.Children.Add(line);
                }

                void AddDot(double x, double y)
                {
                    // Create a dot
                    Ellipse ellipse = new Ellipse
                    {
                        Width = offset,
                        Height = offset,
                        Stroke = Brushes.Red,
                        StrokeThickness = 1.0,
                        Fill = Brushes.Red
                    };

                    string colour = Atom.Element.Colour;
                    ellipse.Stroke = new SolidColorBrush((Color)ColorConverter.ConvertFromString(colour ?? AcmeConstants.DefaultTextColor));
                    ellipse.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString(colour ?? AcmeConstants.DefaultTextColor));

                    // Position ellipse on canvas
                    Canvas.SetLeft(ellipse, x - padding - 2.0);
                    Canvas.SetTop(ellipse, y - padding - 2.0);
                    canvas.Children.Add(ellipse);
                }

                void CreatePoints(out Point p1, out Point p2)
                {
                    p1 = new Point();
                    p2 = new Point();

                    switch (button.DefaultDirection)
                    {
                        case CompassPoints.North:
                        case CompassPoints.South:
                            p1 = new Point(halfWidth - offset, halfHeight);
                            p2 = new Point(halfWidth + offset, halfHeight);
                            break;

                        case CompassPoints.East:
                        case CompassPoints.West:
                            p1 = new Point(halfWidth, halfHeight - offset);
                            p2 = new Point(halfWidth, halfHeight + offset);
                            break;

                        case CompassPoints.NorthEast:
                        case CompassPoints.SouthWest:
                            p1 = new Point(halfWidth - offset, halfHeight - offset);
                            p2 = new Point(halfWidth + offset, halfHeight + offset);
                            break;

                        case CompassPoints.NorthWest:
                        case CompassPoints.SouthEast:
                            p1 = new Point(halfWidth + offset, halfHeight - offset);
                            p2 = new Point(halfWidth - offset, halfHeight + offset);
                            break;
                    }
                }

                void AddPairOfDots()
                {
                    CreatePoints(out Point p1, out Point p2);

                    AddDot(p1.X, p1.Y);
                    AddDot(p2.X, p2.Y);
                }
            }

            return null;
        }
    }
}
