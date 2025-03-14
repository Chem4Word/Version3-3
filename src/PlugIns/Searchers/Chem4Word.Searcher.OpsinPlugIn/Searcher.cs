﻿// ---------------------------------------------------------------------------
//  Copyright (c) 2025, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.Core.UI.Forms;
using Chem4Word.Searcher.OpsinPlugIn.Properties;
using IChem4Word.Contracts;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using Point = System.Windows.Point;

namespace Chem4Word.Searcher.OpsinPlugIn
{
    public class Searcher : IChem4WordSearcher
    {
        private static string _product = Assembly.GetExecutingAssembly().FullName.Split(',')[0];
        private static string _class = MethodBase.GetCurrentMethod().DeclaringType?.Name;

        private SearcherOptions _searcherOptions;

        public bool HasSettings => true;

        public string ShortName => "Opsin";
        public string Name => "Opsin Search PlugIn";
        public string Description => "Searches the Opsin public database";
        public Image Image => Resources.Opsin_Logo;

        public int DisplayOrder
        {
            get
            {
                _searcherOptions = new SearcherOptions(SettingsPath);
                return _searcherOptions.DisplayOrder;
            }
        }

        public Point TopLeft { get; set; }
        public IChem4WordTelemetry Telemetry { get; set; }
        public string SettingsPath { get; set; }
        public Dictionary<string, string> Properties { get; set; }

        public string Cml { get; set; }

        public Searcher()
        {
            // Nothing to do here
        }

        public bool ChangeSettings(Point topLeft)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                Telemetry.Write(module, "Verbose", "Called");
                _searcherOptions = new SearcherOptions(SettingsPath);

                Settings settings = new Settings();
                settings.Telemetry = Telemetry;
                settings.TopLeft = topLeft;

                SearcherOptions tempOptions = _searcherOptions.Clone();
                settings.SettingsPath = SettingsPath;
                settings.SearcherOptions = tempOptions;

                DialogResult dr = settings.ShowDialog();
                if (dr == DialogResult.OK)
                {
                    _searcherOptions = tempOptions.Clone();
                }
                settings.Close();
            }
            catch (Exception ex)
            {
                new ReportError(Telemetry, TopLeft, module, ex).ShowDialog();
            }

            return true;
        }

        public DialogResult Search()
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            DialogResult result = DialogResult.Abort;
            try
            {
                _searcherOptions = new SearcherOptions(SettingsPath);

                SearchOpsin searcher = new SearchOpsin();
                searcher.TopLeft = TopLeft;
                searcher.Telemetry = Telemetry;
                searcher.SettingsPath = SettingsPath;
                searcher.UserOptions = _searcherOptions;

                result = searcher.ShowDialog();
                if (result == DialogResult.OK)
                {
                    Properties = new Dictionary<string, string>();
                    Cml = searcher.Cml;
                }
            }
            catch (Exception ex)
            {
                new ReportError(Telemetry, TopLeft, module, ex).ShowDialog();
            }
            return result;
        }
    }
}