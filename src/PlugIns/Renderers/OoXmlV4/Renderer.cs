﻿// ---------------------------------------------------------------------------
//  Copyright (c) 2025, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.Core;
using Chem4Word.Core.Helpers;
using Chem4Word.Core.UI.Forms;
using Chem4Word.Renderer.OoXmlV4.OOXML;
using IChem4Word.Contracts;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Forms;

namespace Chem4Word.Renderer.OoXmlV4
{
    public class Renderer : IChem4WordRenderer
    {
        private static string _product = Assembly.GetExecutingAssembly().FullName.Split(',')[0];
        private static string _class = MethodBase.GetCurrentMethod().DeclaringType?.Name;

        public string Name => Constants.DefaultRendererPlugIn;
        public string Description => "This is the standard renderer for Chem4Word 2025";
        public bool HasSettings => true;

        public Point TopLeft { get; set; }
        public IChem4WordTelemetry Telemetry { get; set; }
        public string SettingsPath { get; set; }

        public string Cml { get; set; }
        public Dictionary<string, string> Properties { get; set; }

        private OoXmlV4Options _rendererOptions;

        public Renderer()
        {
            // Nothing to do here
        }

        public bool ChangeSettings(Point topLeft)
        {
            var module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            try
            {
                Telemetry.Write(module, "Verbose", "Called");
                _rendererOptions = new OoXmlV4Options(SettingsPath);

                var settings = new OoXmlV4Settings();
                settings.Telemetry = Telemetry;
                settings.TopLeft = topLeft;

                var tempOptions = _rendererOptions.Clone();
                settings.SettingsPath = SettingsPath;
                settings.RendererOptions = tempOptions;

                var dr = settings.ShowDialog();
                if (dr == DialogResult.OK)
                {
                    _rendererOptions = tempOptions.Clone();
                }
                settings.Close();
            }
            catch (Exception ex)
            {
                new ReportError(Telemetry, TopLeft, module, ex).ShowDialog();
            }

            return true;
        }

        public string Render()
        {
            var module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            string result = null;

            try
            {
                Telemetry.Write(module, "Verbose", "Called");
                _rendererOptions = new OoXmlV4Options(SettingsPath);

                var guid = Properties["Guid"];
                result = OoXmlFile.CreateFromCml(Cml, guid, _rendererOptions, Telemetry, TopLeft);
                if (!File.Exists(result))
                {
                    Telemetry.Write(module, "Exception", "Structure could not be rendered.");
                    Telemetry.Write(module, "Exception(Data)", Cml);
                    UserInteractions.WarnUser("Sorry this structure could not be rendered.");
                }
                // Deliberate crash to test Error Reporting
                //int ii = 2;
                //int dd = 0;
                //int bang = ii / dd;
            }
            catch (Exception ex)
            {
                new ReportError(Telemetry, TopLeft, module, ex).ShowDialog();
            }

            return result;
        }
    }
}