﻿// ---------------------------------------------------------------------------
//  Copyright (c) 2025, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.Core;
using Chem4Word.Core.Helpers;
using Chem4Word.Model2;
using Chem4Word.Model2.Converters.CML;
using Chem4Word.Model2.Converters.ProtocolBuffers;
using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Chem4Word.ACME
{
    /// <summary>
    /// Interaction logic for Display.xaml
    /// </summary>
    public partial class Display : UserControl
    {
        public Display()
        {
            InitializeComponent();
        }

        #region Public Properties

        public Controller CurrentController { get; set; }

        #region Chemistry (DependencyProperty)

        public object Chemistry
        {
            get { return (object)GetValue(ChemistryProperty); }
            set { SetValue(ChemistryProperty, value); }
        }

        public static readonly DependencyProperty ChemistryProperty =
            DependencyProperty.Register("Chemistry", typeof(object), typeof(Display),
                                        new FrameworkPropertyMetadata(null,
                                                                      FrameworkPropertyMetadataOptions.AffectsRender
                                                                      | FrameworkPropertyMetadataOptions.AffectsArrange
                                                                      | FrameworkPropertyMetadataOptions.AffectsMeasure,
                                                                      ChemistryChanged));

        #endregion Chemistry (DependencyProperty)

        public Brush BackgroundColor
        {
            get { return (Brush)GetValue(BackgroundColorProperty); }
            set { SetValue(BackgroundColorProperty, value); }
        }

        public static readonly DependencyProperty BackgroundColorProperty =
            DependencyProperty.Register("BackgroundColor", typeof(Brush), typeof(Display),
                                        new FrameworkPropertyMetadata(SystemColors.WindowBrush,
                                            FrameworkPropertyMetadataOptions.AffectsRender));

        public bool HighlightActive
        {
            get { return (bool)GetValue(HighlightActiveProperty); }
            set { SetValue(HighlightActiveProperty, value); }
        }

        public static readonly DependencyProperty HighlightActiveProperty =
            DependencyProperty.Register("HighlightActive", typeof(bool), typeof(Display),
                                        new FrameworkPropertyMetadata(true,
                                            FrameworkPropertyMetadataOptions.AffectsRender
                                            | FrameworkPropertyMetadataOptions.AffectsArrange
                                            | FrameworkPropertyMetadataOptions.AffectsMeasure));

        public bool ShowOverbondedAtoms
        {
            get { return (bool)GetValue(ShowOverbondedAtomsProperty); }
            set { SetValue(ShowOverbondedAtomsProperty, value); }
        }

        public static readonly DependencyProperty ShowOverbondedAtomsProperty =
            DependencyProperty.Register("ShowOverbondedAtoms", typeof(bool), typeof(Display), new PropertyMetadata(default(bool)));

        #endregion Public Properties

        #region Public Methods

        public void Clear()
        {
            var model = new Model();
            CurrentController = new Controller(model);
            DrawChemistry(CurrentController);
        }

        #endregion Public Methods

        #region Private Methods

        private static void ChemistryChanged(DependencyObject source, DependencyPropertyChangedEventArgs args)
        {
            if (source is Display display)
            {
                display.HandleDataContextChanged();
            }
        }

        private void HandleDataContextChanged()
        {
            var sw = new Stopwatch();
            sw.Start();

            var cmlConverter = new CMLConverter();

            Model chemistryModel;

            if (Chemistry != null)
            {
                switch (Chemistry)
                {
                    case string data:
                        if (data.StartsWith("<"))
                        {
                            chemistryModel = cmlConverter.Import(data);
                            sw.Stop();
                            Debug.WriteLine($"Cml converter took {SafeDouble.AsString0(sw.ElapsedMilliseconds)}ms, {sw.ElapsedTicks} ticks");
                        }
                        else
                        {
                            var error = ResourceHelper.GetStringResource(Assembly.GetExecutingAssembly(), "Error.xml");
                            chemistryModel = cmlConverter.Import(error);
                        }
                        break;

                    case byte[] pbuff:
                        var protocolBufferConverter = new ProtocolBufferConverter();
                        chemistryModel = protocolBufferConverter.Import(pbuff);
                        sw.Stop();
                        Debug.WriteLine($"Protocol Buffer converter took {SafeDouble.AsString0(sw.ElapsedMilliseconds)}ms, {sw.ElapsedTicks} ticks");
                        break;

                    case Model model:
                        Debug.WriteLine("Using model as is");
                        chemistryModel = model;
                        break;

                    default:
                        var resource = ResourceHelper.GetStringResource(Assembly.GetExecutingAssembly(), "Error.xml");
                        chemistryModel = cmlConverter.Import(resource);
                        break;
                }

                //assuming we've got this far, we should have something we can draw
                if (chemistryModel != null)
                {
                    sw.Reset();
                    sw.Start();

                    chemistryModel.EnsureBondLength(20, false);
                    chemistryModel.RescaleForXaml(true, Constants.StandardBondLength);

                    CurrentController = new Controller(chemistryModel);
                    CurrentController.SetTextParams(chemistryModel.XamlBondLength);

                    DrawChemistry(CurrentController);

                    sw.Stop();
                    Debug.WriteLine($"Draw Chemistry took {SafeDouble.AsString0(sw.ElapsedMilliseconds)}ms, {sw.ElapsedTicks} ticks");
                }
            }
        }

        private void DrawChemistry(Controller currentController)
        {
            ChemCanvas.Controller = currentController;
        }

        #endregion Private Methods

        #region Private EventHandlers

        /// <summary>
        /// Add this to the OnMouseLeftButtonDown attribute
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UIElementOnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            dynamic clobberedElement = sender;
            UserInteractions.InformUser(clobberedElement.ID);
        }

        #endregion Private EventHandlers
    }
}