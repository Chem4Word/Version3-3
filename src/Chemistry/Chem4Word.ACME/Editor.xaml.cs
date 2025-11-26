// ---------------------------------------------------------------------------
//  Copyright (c) 2025, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.ACME.Behaviors;
using Chem4Word.ACME.Utils;
using Chem4Word.Core;
using Chem4Word.Core.UI.Wpf;
using Chem4Word.Model2;
using Chem4Word.Model2.Annotations;
using Chem4Word.Model2.Converters.CML;
using IChem4Word.Contracts;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Chem4Word.ACME
{
    /// <summary>
    /// Interaction logic for Editor.xaml
    /// </summary>
    public partial class Editor : UserControl, INotifyPropertyChanged, IHostedWpfEditor
    {
        private EditController _activeController;

        public EditController ActiveController
        {
            get { return _activeController; }
            set
            {
                //clear the current behaviour on the old controller
                if (_activeController != null)
                {
                    _activeController.ActiveBehavior = null;
                }
                //now set the new controller
                _activeController = value;

                OnPropertyChanged();
            }
        }

        public Point TopLeft { get; set; }

        private double _defaultBondLength;

        private Model _model;

        private List<string> _used1DProperties;

        private RenderingOptions _userDefaultOptions;
        private Bond _lastBond;

        public IChem4WordTelemetry Telemetry
        {
            get { return ACMEGlobals.Telemetry; }
            set { ACMEGlobals.Telemetry = value; }
        }

        public bool ShowFeedback
        {
            get { return (bool)GetValue(ShowFeedbackProperty); }
            set { SetValue(ShowFeedbackProperty, value); }
        }

        public static readonly DependencyProperty ShowFeedbackProperty =
            DependencyProperty.Register("ShowFeedback", typeof(bool), typeof(Editor), new PropertyMetadata(true));

        public static readonly DependencyProperty SliderVisibilityProperty =
            DependencyProperty.Register("SliderVisibility", typeof(Visibility), typeof(Editor),
                                        new PropertyMetadata(default(Visibility)));

        public Editor()
        {
            EnsureApplicationResources();
            InitializeComponent();
        }

        private void OnLoaded_ACMEControl(object sender, RoutedEventArgs e)
        {
            InitialiseEditor();
        }

        private void InitialiseEditor()
        {
            if (_model != null)
            {
                _model.RescaleForXaml(false, _defaultBondLength);

                ActiveController = new EditController(_model, ChemCanvas, HostingCanvas, ReactionBoxEditor, _used1DProperties, Telemetry)
                {
                    EditorControl = this
                };
                ActiveController.Model.CentreInCanvas(new Size(ChemCanvas.ActualWidth, ChemCanvas.ActualHeight));

                ChemCanvas.Controller = ActiveController;

                ActiveController.Loading = true;

                if (ActiveController.Model.TotalBondsCount == 0)
                {
                    ActiveController.CurrentBondLength = _defaultBondLength;
                }
                else
                {
                    var mean = ActiveController.Model.MeanBondLength / ModelConstants.ScaleFactorForXaml;
                    var average = Math.Round(mean / 5.0) * 5;
                    ActiveController.CurrentBondLength = average;
                }

                ActiveController.Loading = false;

                ScrollIntoView();
                BindControls(ActiveController);

                ActiveController.OnFeedbackChange += OnFeedbackChangeActiveController;

                //refresh the ring button
                SetCurrentRing(BenzeneButton);
                //refresh the selection button
                SetSelectionMode(LassoButton);

                //HACK: [DCD] Need to do this to put the editor into the right mode after refreshing the ring button
                DrawButton.IsChecked = true;
                OnChecked_Mode(DrawButton, new RoutedEventArgs());
            }
        }

        public void SetProperties(string cml, List<string> used1DProperties, RenderingOptions userDefaultOptions)
        {
            if (string.IsNullOrEmpty(cml))
            {
                _model = new Model();
                _model.SetUserOptions(userDefaultOptions);
            }
            else
            {
                _model = new CMLConverter().Import(cml);
            }

            _userDefaultOptions = userDefaultOptions;
            _used1DProperties = used1DProperties;
            _defaultBondLength = userDefaultOptions.DefaultBondLength;

            InitialiseEditor();
        }

        public event EventHandler<WpfEventArgs> OnFeedbackChange;

        private void OnFeedbackChangeActiveController(object sender, WpfEventArgs wpfEventArgs)
        {
            if (ShowFeedback)
            {
                // ToDo: Figure this out ...
            }
            OnFeedbackChange?.Invoke(this, wpfEventArgs);
        }

        public bool IsDirty
        {
            get
            {
                if (ActiveController == null)
                {
                    return false;
                }

                return ActiveController.IsDirty || ActiveController.HasChangedSettings;
            }
        }

        /// <summary>
        /// This model is ONLY for exporting the results of the editing session
        /// </summary>
        public Model EditedModel
        {
            get
            {
                if (ActiveController == null)
                {
                    return null;
                }

                var model = ActiveController.Model.Copy();
                model.RescaleForCml();
                return model;
            }
        }

        //see http://drwpf.com/blog/2007/10/05/managing-application-resources-when-wpf-is-hosted/
        private void EnsureApplicationResources()
        {
            if (Application.Current == null)
            {
                // create the Application object
                try
                {
                    new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
                }
                catch //just in case the application already exists
                {
                    //no action required
                }
            }

            //check to make sure we managed to initialize a
            //new application before adding in resources
            if (Application.Current != null)
            {
                // Merge in your application resources
                // We need to do this for controls hosted in Winforms
                Application.Current.Resources.MergedDictionaries.Add(
                    Application.LoadComponent(
                        new Uri("Chem4Word.ACME;component/Resources/ACMEResources.xaml",
                                UriKind.Relative)) as ResourceDictionary);

                Application.Current.Resources.MergedDictionaries.Add(
                    Application.LoadComponent(
                        new Uri("Chem4Word.ACME;component/Resources/Brushes.xaml",
                                UriKind.Relative)) as ResourceDictionary);
                Application.Current.Resources.MergedDictionaries.Add(
                    Application.LoadComponent(
                        new Uri("Chem4Word.ACME;component/Resources/ControlStyles.xaml",
                                UriKind.Relative)) as ResourceDictionary);
                Application.Current.Resources.MergedDictionaries.Add(
                    Application.LoadComponent(
                        new Uri("Chem4Word.ACME;component/Resources/ZoomBox.xaml",
                                UriKind.Relative)) as ResourceDictionary);
            }
        }

        public AtomOption SelectedAtomOption
        {
            get { return (AtomOption)GetValue(SelectedAtomOptionProperty); }
            set { SetValue(SelectedAtomOptionProperty, value); }
        }

        public static readonly DependencyProperty SelectedAtomOptionProperty =
            DependencyProperty.Register("SelectedAtomOption", typeof(AtomOption), typeof(Editor),
                                        new PropertyMetadata(default(AtomOption)));

        public Visibility SliderVisibility
        {
            get { return (Visibility)GetValue(SliderVisibilityProperty); }
            set { SetValue(SliderVisibilityProperty, value); }
        }

        public double HorizontalOffset
        {
            get => DrawingArea.HorizontalOffset;
        }

        public double VerticalOffset
        {
            get => DrawingArea.VerticalOffset;
        }

        public double ViewportWidth
        {
            get => DrawingArea.ViewportWidth;
        }

        public double ViewportHeight
        {
            get => DrawingArea.ViewportHeight;
        }

        public Point TranslateToScreen(Point p)
        {
            return DrawingArea.TranslatePoint(p, ChemCanvas);
        }

        private void OnClick_RingDropdown(object sender, RoutedEventArgs e)
        {
            RingPopup.IsOpen = true;
            RingPopup.Closed += (senderClosed, eClosed) => { };
        }

        private void OnClick_RingSelect(object sender, RoutedEventArgs e)
        {
            SetCurrentRing(sender);
            OnChecked_Mode(RingButton, null);
            RingButton.IsChecked = true;
            RingPopup.IsOpen = false;
        }

        private void SetCurrentRing(object sender)
        {
            if (sender is Button button)
            {
                var currentFace = new VisualBrush { AutoLayoutContent = true, Stretch = Stretch.Uniform, Visual = button.Content as Visual };

                RingPanel.Background = currentFace;
                RingButton.Tag = button.Tag;
            }
        }

        /// <summary>
        /// Sets up data bindings between the dropdowns
        /// and the view model
        /// </summary>
        /// <param name="controller">EditController for ACME</param>
        private void BindControls(EditController controller)
        {
            controller.EditingCanvas = ChemCanvas;
        }

        /// <summary>
        /// Scrolls drawing into view
        /// </summary>
        private void ScrollIntoView()
        {
            DrawingArea.ScrollToHorizontalOffset((DrawingArea.ExtentWidth - DrawingArea.ViewportWidth) / 2);
            DrawingArea.ScrollToVerticalOffset((DrawingArea.ExtentHeight - DrawingArea.ViewportHeight) / 2);
        }

        private void OnClick_Settings(object sender, RoutedEventArgs e)
        {
            Point locationFromScreen = AcmeControl.PointToScreen(new Point(0, 0));
            Point dialogueTopLeft = new Point(locationFromScreen.X + CoreConstants.TopLeftOffset,
                                              locationFromScreen.Y + CoreConstants.TopLeftOffset);

            var currentOptions = ActiveController.Model.GetCurrentOptions();

            var newOptions = UIUtils.ShowAcmeSettings(ChemCanvas, currentOptions.Copy(), _userDefaultOptions, Telemetry, dialogueTopLeft);

            if (ActiveController != null
                && newOptions != null
                && !currentOptions.IsEqualTo(newOptions))
            {
                ActiveController.Model.ExplicitC = newOptions.ExplicitC;
                ActiveController.Model.ExplicitH = newOptions.ExplicitH;
                ActiveController.Model.ShowColouredAtoms = newOptions.ShowColouredAtoms;
                ActiveController.Model.ShowMoleculeGrouping = newOptions.ShowMoleculeGrouping;
                ActiveController.Model.ShowMolecularWeight = newOptions.ShowMolecularWeight;
                ActiveController.Model.ShowMoleculeCaptions = newOptions.ShowMoleculeCaptions;

                ActiveController.HasChangedSettings = true;

                ChemCanvas.RepaintCanvas();
            }
        }

        /// <summary>
        /// Sets the current behaviour of the editor to the
        /// behavior specified in the button's tag property
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnChecked_Mode(object sender, RoutedEventArgs e)
        {
            ActiveController.ActiveBehavior = null;

            if (ActiveController != null)
            {
                var radioButton = (RadioButton)sender;

                if (radioButton.Tag is BaseEditBehavior bh)
                {
                    ActiveController.ActiveBehavior = bh;
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void OnPreviewKeyDown_Editor(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete)
            {
                ActiveController.DeleteSelection();
            }
            else if (e.Key == Key.A && KeyboardUtils.HoldingDownControl())
            {
                ActiveController.SelectAll();
            }
        }

        /// <summary>
        /// detects whether the popup has been clicked
        /// and sets the mode accordingly
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnClick_SelectionPopup(object sender, RoutedEventArgs e)
        {
            SelectionButton.IsChecked = true;
        }

        private void OnClick_SelectionDropdown(object sender, RoutedEventArgs e)
        {
            SelectionPopup.IsOpen = true;
            SelectionPopup.Closed += (senderClosed, eClosed) => { };
        }

        private void OnClick_Selection(object sender, RoutedEventArgs e)
        {
            SetSelectionMode(sender);
            OnChecked_Mode(SelectionButton, null);
            SelectionButton.IsChecked = true;
            SelectionPopup.IsOpen = false;
        }

        private void SetSelectionMode(object sender)
        {
            Button selButton = sender as Button;
            var currentFace = new VisualBrush
            {
                AutoLayoutContent = true,
                Stretch = Stretch.Uniform,
                Visual = selButton.Content as Visual
            };
            SelectionPanel.Background = currentFace;
            //set the behaviour of the button to that of
            //the selected mode in the dropdown
            SelectionButton.Tag = selButton.Tag;
        }

        private void OnClick_SymbolDropdown(object sender, RoutedEventArgs e)
        {
            SymbolPopup.IsOpen = true;
            SymbolPopup.Closed += (senderClosed, eClosed) => { };
        }

        private void OnClick_SymbolSelect(object sender, RoutedEventArgs e)
        {
            SetSymbol(sender);
            SymbolPopup.IsOpen = false;
        }

        private void SetSymbol(object sender)
        {
            Button panelButton = sender as Button;
            SymbolButton.Tag = panelButton.Tag;
            SymbolButton.Content = panelButton.Tag;
        }

        private void OnClick_PasteMenuItem(object sender, RoutedEventArgs e)
        {
            ActiveController.PasteCommand.Execute(Mouse.GetPosition(ChemCanvas));
        }

        private void OnClick_MoleculeRadicalMenu(object sender, RoutedEventArgs e)
        {
            MenuItem radicalMI = (MenuItem)sender;
            int? spin = ToNullableInt((string)radicalMI.Tag);
            ActiveController.SetSelectedMoleculeRadical(spin);
        }

        private int? ToNullableInt(string s)
        {
            if (int.TryParse(s, out int i)) return i;
            return null;
        }

        private void OnClick_BondTypeMenu(object sender, RoutedEventArgs e)
        {
            MenuItem cmi = (MenuItem)sender;

            if (cmi.Tag != null)
            {
                ActiveController.SetBondOption((int)cmi.Tag, new[] { _lastBond });
            }
        }

        private void OnContextMenuOpening_BondContextMenu(object sender, ContextMenuEventArgs e)
        {
            _lastBond = ActiveController.EditingCanvas.ActiveBondVisual?.ParentBond;
        }
    }
}
;
