﻿// ---------------------------------------------------------------------------
//  Copyright (c) 2025, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.Core.UI.Forms;
using System;
using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;

namespace Chem4Word.UI.WPF
{
    /// <summary>
    /// Interaction logic for AboutControl.xaml
    /// </summary>
    public partial class AboutControl : UserControl
    {
        private static string _product = Assembly.GetExecutingAssembly().FullName.Split(',')[0];
        private static string _class = MethodBase.GetCurrentMethod().DeclaringType?.Name;

        public string VersionString { get; set; }
        public Point TopLeft { get; set; }
        public bool AutoClose { get; set; }

        public event EventHandler OnPreloadComplete;

        public AboutControl()
        {
            InitializeComponent();
        }

        private void OnLoaded_AboutControl(object sender, RoutedEventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                if (AutoClose)
                {
                    OnPreloadComplete?.Invoke(this, null);
                }
                else
                {
                    Chem4WordVersion.Text = VersionString;
                }
            }
            catch (Exception ex)
            {
                using (var form = new ReportError(Globals.Chem4WordV3.Telemetry, TopLeft, module, ex))
                {
                    form.ShowDialog();
                }
            }
        }

        #region Hyperlink Methods

        private void OnClick_ChemDoodle(object sender, RoutedEventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            Globals.Chem4WordV3.Telemetry.Write(module, "Action", "Triggered");

            try
            {
                Process.Start("http://web.chemdoodle.com");
            }
            catch (Exception ex)
            {
                using (var form = new ReportError(Globals.Chem4WordV3.Telemetry, TopLeft, module, ex))
                {
                    form.ShowDialog();
                }
            }
        }

        private void OnClick_MainWebsite(object sender, RoutedEventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            Globals.Chem4WordV3.Telemetry.Write(module, "Action", "Triggered");

            try
            {
                Process.Start("https://www.chem4word.co.uk");
            }
            catch (Exception ex)
            {
                using (var form = new ReportError(Globals.Chem4WordV3.Telemetry, TopLeft, module, ex))
                {
                    form.ShowDialog();
                }
            }
        }

        private void OnClick_Facebook(object sender, RoutedEventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            Globals.Chem4WordV3.Telemetry.Write(module, "Action", "Triggered");

            try
            {
                Process.Start("https://www.facebook.com/groups/chem4word");
            }
            catch (Exception ex)
            {
                using (var form = new ReportError(Globals.Chem4WordV3.Telemetry, TopLeft, module, ex))
                {
                    form.ShowDialog();
                }
            }
        }

        private void OnClick_Cambridge(object sender, RoutedEventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            Globals.Chem4WordV3.Telemetry.Write(module, "Action", "Triggered");

            try
            {
                Process.Start("http://www.ch.cam.ac.uk/");
            }
            catch (Exception ex)
            {
                using (var form = new ReportError(Globals.Chem4WordV3.Telemetry, TopLeft, module, ex))
                {
                    form.ShowDialog();
                }
            }
        }

        private void OnClick_Sponsor(object sender, RoutedEventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            Globals.Chem4WordV3.Telemetry.Write(module, "Action", "Triggered");

            try
            {
                Process.Start("https://dotnetfoundation.org/projects");
            }
            catch (Exception ex)
            {
                using (var form = new ReportError(Globals.Chem4WordV3.Telemetry, TopLeft, module, ex))
                {
                    form.ShowDialog();
                }
            }
        }

        private void OnClick_SourceCode(object sender, RoutedEventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            Globals.Chem4WordV3.Telemetry.Write(module, "Action", "Triggered");

            try
            {
                Process.Start("https://github.com/Chem4Word/Version3-3");
            }
            catch (Exception ex)
            {
                using (var form = new ReportError(Globals.Chem4WordV3.Telemetry, TopLeft, module, ex))
                {
                    form.ShowDialog();
                }
            }
        }

        private void OnClick_YouTube(object sender, RoutedEventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            Globals.Chem4WordV3.Telemetry.Write(module, "Action", "Triggered");

            try
            {
                Process.Start("https://www.youtube.com/@chem4word");
            }
            catch (Exception ex)
            {
                using (var form = new ReportError(Globals.Chem4WordV3.Telemetry, TopLeft, module, ex))
                {
                    form.ShowDialog();
                }
            }
        }

        private void OnClick_Donate(object sender, RoutedEventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            Globals.Chem4WordV3.Telemetry.Write(module, "Action", "Triggered");

            try
            {
                Process.Start("https://buymeacoffee.com/chem4word");
            }
            catch (Exception ex)
            {
                using (var form = new ReportError(Globals.Chem4WordV3.Telemetry, TopLeft, module, ex))
                {
                    form.ShowDialog();
                }
            }
        }

        #endregion Hyperlink Methods
    }
}