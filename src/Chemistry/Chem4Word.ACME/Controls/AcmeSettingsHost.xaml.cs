// ---------------------------------------------------------------------------
//  Copyright (c) 2025, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.ACME.Utils;
using Chem4Word.Core.UI.Wpf;
using Chem4Word.Model2;
using IChem4Word.Contracts;
using System.ComponentModel;
using System.Windows;

namespace Chem4Word.ACME.Controls
{
    /// <summary>
    /// Interaction logic for AcmeSettingsHost.xaml
    /// </summary>
    public partial class AcmeSettingsHost : Window
    {
        private Point _topLeft { get; set; }

        private RenderingOptions _originalOptions;

        public RenderingOptions Result { get; set; }

        public AcmeSettingsHost()
        {
            InitializeComponent();
        }

        public AcmeSettingsHost(RenderingOptions currentOptions, RenderingOptions userDefaults, IChem4WordTelemetry telemetry, Point topLeft) : this()
        {
            if (!DesignerProperties.GetIsInDesignMode(this))
            {
                _originalOptions = currentOptions.Copy();
                UcSettings.CurrentOptions = currentOptions;
                UcSettings.UserDefaultOptions = userDefaults;
                UcSettings.Telemetry = telemetry;
                UcSettings.OnButtonClick += OnButtonClick_UcSettings;
                _topLeft = topLeft;
            }
        }

        private void OnButtonClick_UcSettings(object sender, WpfEventArgs e)
        {
            if (e.Button.Equals("CANCEL"))
            {
                Result = _originalOptions;
                Close();
            }

            if (e.Button.Equals("SAVE"))
            {
                Result = UcSettings.CurrentOptions;
                Close();
            }
        }

        private void OnLoaded_SettingsHost(object sender, RoutedEventArgs e)
        {
            var p1 = new Point(_topLeft.X + Width / 2, _topLeft.Y + Height / 2);
            var p2 = UIUtils.GetOnScreenCentrePoint(p1, Width, Height);
            Left = p2.X;
            Top = p2.Y;
            if (UcSettings != null)
            {
                UcSettings.TopLeft = new Point(p2.X + Core.Helpers.Constants.TopLeftOffset, p2.Y + Core.Helpers.Constants.TopLeftOffset);
            }
        }
    }
}