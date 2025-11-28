// ---------------------------------------------------------------------------
//  Copyright (c) 2026, The .NET Foundation.
//  This software is released under the Apache Licence, Version 2.0.
//  The licence and further copyright text can be found in the file LICENCE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.Core.UI.Wpf;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using Wpf.UI.Sandbox.Models;

namespace Wpf.UI.Sandbox.Forms
{
    /// <summary>
    /// Interaction logic for Ticker.xaml
    /// </summary>
    public partial class Ticker : Window
    {
        private int _index = 0;

        private List<TickerItem> _tickerItems = new List<TickerItem>
                                                {
                                                    new TickerItem { Text = "#1 This is news item #1", Url = "https://item1" },
                                                    new TickerItem { Text = "#2 This is news item #2", Url = "https://item2" },
                                                    new TickerItem { Text = "#3 This is news item #3", Url = "https://item3" },
                                                    new TickerItem { Text = "#4 This is news item #4", Url = "https://item4" },
                                                    new TickerItem { Text = "#5 This is news item #5", Url = "https://item5" }
                                                };

        public Ticker()
        {
            InitializeComponent();
        }

        private void OnMouseDown_UIElement(object sender, MouseButtonEventArgs e)
        {
            Debug.WriteLine($"User Clicked {_tickerItems[_index].Url}");
        }

        private void OnLoaded_Ticker(object sender, RoutedEventArgs e)
        {
            Marquee.OnAnimationCompleted += OnAnimationCompleted_Marquee;
            Marquee.Start(_tickerItems[_index].Text);
        }

        private void OnAnimationCompleted_Marquee(object sender, WpfEventArgs e)
        {
            _index++;
            if (_index >= _tickerItems.Count)
            {
                _index = 0;
            }
            Marquee.Start(_tickerItems[_index].Text);
        }
    }
}
