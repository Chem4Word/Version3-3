﻿// ---------------------------------------------------------------------------
//  Copyright (c) 2025, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.Core.Helpers;
using Chem4Word.Model2.Converters.CML;
using IChem4Word.Contracts;
using System;
using System.IO;
using System.Net;
using System.Reflection;
using System.Windows.Forms;
using System.Windows.Media;

namespace Chem4Word.Searcher.OpsinPlugIn
{
    public partial class SearchOpsin : Form
    {
        private static string _product = Assembly.GetExecutingAssembly().FullName.Split(',')[0];
        private static string _class = MethodBase.GetCurrentMethod().DeclaringType?.Name;

        public System.Windows.Point TopLeft { get; set; }
        public IChem4WordTelemetry Telemetry { get; set; }
        public string SettingsPath { get; set; }

        public string Cml { get; set; }

        public SearcherOptions UserOptions { get; set; }

        public SearchOpsin()
        {
            InitializeComponent();
        }

        private void OnTextChanged_SearchFor(object sender, EventArgs e)
        {
            SearchButton.Enabled = TextHelper.IsValidSearchString(SearchFor.Text);
        }

        private void OnClick_SearchButton(object sender, EventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            var searchFor = TextHelper.StripControlCharacters(SearchFor.Text).Trim();
            Telemetry.Write(module, "Information", $"User searched for '{searchFor}'");

            display1.Chemistry = null;
            display1.Clear();

            Cursor = Cursors.WaitCursor;

            var securityProtocol = ServicePointManager.SecurityProtocol;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            UriBuilder builder = new UriBuilder(UserOptions.OpsinWebServiceUri + searchFor);
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(builder.Uri);
            request.Timeout = 30000;
            request.Accept = "chemical/x-cml";
            request.UserAgent = "Chem4Word";

            HttpWebResponse response;
            try
            {
                response = (HttpWebResponse)request.GetResponse();
                if (response.StatusCode.Equals(HttpStatusCode.OK))
                {
                    ProcessResponse(response);
                }
                else
                {
                    Telemetry.Write(module, "Warning", $"Status code {response.StatusCode} was returned by the server");
                    ShowFailureMessage($"An unexpected status code {response.StatusCode} was returned by the server");
                }
            }
            catch (WebException ex)
            {
                HttpWebResponse webResponse = (HttpWebResponse)ex.Response;
                switch (webResponse.StatusCode)
                {
                    case HttpStatusCode.NotFound:
                        ShowFailureMessage($"No valid representation of the name '{searchFor}' has been found");
                        break;

                    case HttpStatusCode.RequestTimeout:
                        ShowFailureMessage("Please try again later - the service has timed out");
                        break;

                    default:
                        Telemetry.Write(module, "Warning", $"Status code: {webResponse.StatusCode}  was returned by the server");
                        break;
                }
            }
            catch (Exception ex)
            {
                Telemetry.Write(module, "Exception", ex.Message);
                Telemetry.Write(module, "Exception", ex.StackTrace);
                ShowFailureMessage($"An unexpected error has occurred: {ex.Message}");
            }
            finally
            {
                ServicePointManager.SecurityProtocol = securityProtocol;
                Cursor = Cursors.Default;
            }
        }

        private void ShowFailureMessage(string message)
        {
            LabelInfo.Text = message;
            display1.Chemistry = "";
            ImportButton.Enabled = false;
        }

        private void ProcessResponse(HttpWebResponse response)
        {
            LabelInfo.Text = "";
            // read data via the response stream
            using (Stream resStream = response.GetResponseStream())
            {
                if (resStream != null)
                {
                    StreamReader sr = new StreamReader(resStream);
                    string temp = sr.ReadToEnd();

                    CMLConverter cmlConverter = new CMLConverter();
                    var model = cmlConverter.Import(temp);
                    if (model.MeanBondLength < Core.Helpers.Constants.MinimumBondLength - Core.Helpers.Constants.BondLengthTolerance
                        || model.MeanBondLength > Core.Helpers.Constants.MaximumBondLength + Core.Helpers.Constants.BondLengthTolerance)
                    {
                        model.ScaleToAverageBondLength(Core.Helpers.Constants.StandardBondLength);
                    }

                    Cml = cmlConverter.Export(model);

                    display1.Chemistry = Cml;
                    ImportButton.Enabled = true;
                }
            }
        }

        private void OnClick_ImportButton(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
            Hide();
        }

        private void OnLoad_SearchOpsin(object sender, EventArgs e)
        {
            if (!PointHelper.PointIsEmpty(TopLeft))
            {
                Left = (int)TopLeft.X;
                Top = (int)TopLeft.Y;
                var screen = Screen.FromControl(this);
                var sensible = PointHelper.SensibleTopLeft(TopLeft, screen, Width, Height);
                Left = (int)sensible.X;
                Top = (int)sensible.Y;
            }

            display1.Background = Brushes.White;
            display1.HighlightActive = false;

            ImportButton.Enabled = false;
            SearchButton.Enabled = false;

            LabelInfo.Text = "";
            AcceptButton = SearchButton;
        }
    }
}