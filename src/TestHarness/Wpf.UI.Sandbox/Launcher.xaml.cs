// ---------------------------------------------------------------------------
//  Copyright (c) 2023, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System.Windows;

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

        private void OnClick_Button1(object sender, RoutedEventArgs e)
        {
            var window = new ShapesUI();
            window.ShowDialog();
        }

        private void OnClick_Button2(object sender, RoutedEventArgs e)
        {
            var window = new StylesUI();
            window.ShowDialog();
        }
    }
}