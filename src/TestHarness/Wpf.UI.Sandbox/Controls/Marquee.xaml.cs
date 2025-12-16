// ---------------------------------------------------------------------------
//  Copyright (c) 2026, The .NET Foundation.
//  This software is released under the Apache Licence, Version 2.0.
//  The licence and further copyright text can be found in the file LICENCE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.Core.UI.Wpf;
using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace Wpf.UI.Sandbox.Controls
{
    /// <summary>
    /// Interaction logic for Marquee.xaml
    /// </summary>
    public partial class Marquee : UserControl
    {
        public event EventHandler<WpfEventArgs> OnAnimationCompleted;

        public Marquee()
        {
            InitializeComponent();
        }

        private void OnLoaded_Marquee(object sender, RoutedEventArgs e)
        {
            if (DesignerProperties.GetIsInDesignMode(this))
            {
                Start("This control is running in design mode!");
            }
        }

        public void Start(string text)
        {
            TextToScroll.Text = text;
            DoubleAnimation doubleAnimation = new DoubleAnimation
            {
                From = -TextToScroll.ActualWidth,
                To = ScrollingRegion.ActualWidth,
                Duration = new Duration(TimeSpan.Parse("0:0:15"))
            };

            if (DesignerProperties.GetIsInDesignMode(this))
            {
                doubleAnimation.RepeatBehavior = RepeatBehavior.Forever;
            }
            doubleAnimation.Completed += OnCompleted_DoubleAnimation;
            TextToScroll.BeginAnimation(Canvas.RightProperty, doubleAnimation);
        }

        private void OnCompleted_DoubleAnimation(object sender, EventArgs e)
        {
            WpfEventArgs args = new WpfEventArgs();
            OnAnimationCompleted?.Invoke(this, args);
        }
    }
}
