// ---------------------------------------------------------------------------
//  Copyright (c) 2024, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System.Windows;
using System.Windows.Controls;

namespace Wpf.UI.Sandbox
{
    /// <summary>
    /// Interaction logic for Launcher.xaml
    /// </summary>
    public partial class Launcher : Window
    {
        public Launcher()
        {
            InitializeComponent();
        }

        private void OnClick_Button(object sender, RoutedEventArgs e)
        {
            if (sender is Button button)
            {
                Window window;

                switch (button.Tag)
                {
                    case "Shapes":
                        window = new ShapesUI();
                        window.ShowDialog();
                        break;

                    case "Styles":
                        window = new StylesUI();
                        window.ShowDialog();
                        break;

                    case "Ticker":
                        var top = Top + Height - 80;
                        window = new Ticker();
                        window.Height = 80;
                        window.Left = Left;
                        window.Top = top;
                        window.ShowDialog();
                        break;

                    case "Animation":
                        window = new Animation();
                        window.ShowDialog();
                        break;

                    case "DropDownStyle":
                        window = new ComboBoxAndTreeViewStyles();
                        window.ShowDialog();
                        break;

                    case "FunctionalGroupExpansionEditor":
                        window = new FunctionalGroupExpansionEditor();
                        window.ShowDialog();
                        break;

                    case "FunctionalGroupExpansionTesting":
                        window = new FunctionalGroupExpansionTesting();
                        window.ShowDialog();
                        break;
                }
            }
        }
    }
}