// ---------------------------------------------------------------------------
//  Copyright (c) 2025, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.Core;
using Chem4Word.Core.Helpers;
using Chem4Word.Core.UI.Forms;
using Chem4Word.Model2;
using Chem4Word.Model2.Converters.CML;
using Chem4Word.Model2.Converters.MDL;
using IChem4Word.Contracts;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using System.Windows.Media;
using System.Xml.Linq;
using System.Xml.XPath;

namespace Chem4Word.Searcher.PubChemPlugIn
{
    public partial class SearchPubChem : Form
    {
        private static string _product = Assembly.GetExecutingAssembly().FullName.Split(',')[0];
        private static string _class = MethodBase.GetCurrentMethod().DeclaringType?.Name;

        public System.Windows.Point TopLeft { get; set; }
        public IChem4WordTelemetry Telemetry { get; set; }
        public string SettingsPath { get; set; }
        public string PubChemId { get; set; }

        public string Cml { get; set; }

        public PubChemOptions UserOptions { get; set; }

        private int resultsCount;
        private string webEnv;
        private int lastResult;
        private int firstResult = 0;
        private const int numResults = 20;

        private string lastSelected = string.Empty;
        private string lastMolfile = string.Empty;

        public SearchPubChem()
        {
            InitializeComponent();
        }

        private void OnLoad_SearchPubChem(object sender, EventArgs e)
        {
            var module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
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

                NextButton.Enabled = false;
                PreviousButton.Enabled = false;
                ImportButton.Enabled = false;
                SearchButton.Enabled = false;

                Results.Enabled = false;
                AcceptButton = SearchButton;

                Results.Items.Clear();
            }
            catch (Exception ex)
            {
                new ReportError(Telemetry, TopLeft, module, ex).ShowDialog();
            }
        }

        private void OnTextChanged_SearchFor(object sender, EventArgs e)
        {
            SearchButton.Enabled = TextHelper.IsValidSearchString(SearchFor.Text);
        }

        private void OnClick_SearchButton(object sender, EventArgs e)
        {
            var module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                var searchFor = TextHelper.StripControlCharacters(SearchFor.Text).Trim();
                Telemetry.Write(module, "Information", $"User searched for '{searchFor}'");

                ErrorsAndWarnings.Text = "";
                display1.Chemistry = null;
                display1.Clear();

                ExecuteSearch(searchFor, 0);
            }
            catch (Exception ex)
            {
                new ReportError(Telemetry, TopLeft, module, ex).ShowDialog();
            }
        }

        private void OnClick_PreviousButton(object sender, EventArgs e)
        {
            var module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                var searchFor = TextHelper.StripControlCharacters(SearchFor.Text).Trim();
                ExecuteSearch(searchFor, -1);
            }
            catch (Exception ex)
            {
                new ReportError(Telemetry, TopLeft, module, ex).ShowDialog();
            }
        }

        private void OnClick_NextButton(object sender, EventArgs e)
        {
            var module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                var searchFor = TextHelper.StripControlCharacters(SearchFor.Text).Trim();
                ExecuteSearch(searchFor, 1);
            }
            catch (Exception ex)
            {
                new ReportError(Telemetry, TopLeft, module, ex).ShowDialog();
            }
        }

        private void OnClick_ImportButton(object sender, EventArgs e)
        {
            var module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                DialogResult = DialogResult.OK;
                Hide();
            }
            catch (Exception ex)
            {
                new ReportError(Telemetry, TopLeft, module, ex).ShowDialog();
            }
        }

        private void OnSelectedIndexChanged_Results(object sender, EventArgs e)
        {
            var module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                lastSelected = FetchStructure();
            }
            catch (Exception ex)
            {
                new ReportError(Telemetry, TopLeft, module, ex).ShowDialog();
            }
        }

        private void OnDoubleClick_Results(object sender, EventArgs e)
        {
            var module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                Debug.WriteLine("OnDoubleClick_Results");
                DialogResult = DialogResult.OK;
                Hide();
            }
            catch (Exception ex)
            {
                new ReportError(Telemetry, TopLeft, module, ex).ShowDialog();
            }
        }

        private void ExecuteSearch(string searchFor, int direction)
        {
            var module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            Cursor = Cursors.WaitCursor;

            string webCall;
            if (direction == 0)
            {
                webCall = string.Format(CultureInfo.InvariantCulture,
                                        "{0}entrez/eutils/esearch.fcgi?db=pccompound&term={1}&retmode=xml&relevanceorder=on&usehistory=y&retmax={2}",
                                        UserOptions.PubChemWebServiceUri, searchFor, UserOptions.ResultsPerCall);
            }
            else
            {
                if (direction == 1)
                {
                    var startFrom = firstResult + numResults;
                    webCall = string.Format(CultureInfo.InvariantCulture,
                                            "{0}entrez/eutils/esearch.fcgi?db=pccompound&term={1}&retmode=xml&relevanceorder=on&usehistory=y&retmax={2}&WebEnv={3}&RetStart={4}",
                                            UserOptions.PubChemWebServiceUri, searchFor, UserOptions.ResultsPerCall, webEnv, startFrom);
                }
                else
                {
                    var startFrom = firstResult - numResults;
                    webCall = string.Format(CultureInfo.InvariantCulture,
                                            "{0}entrez/eutils/esearch.fcgi?db=pccompound&term={1}&retmode=xml&relevanceorder=on&usehistory=y&retmax={2}&WebEnv={3}&RetStart={4}",
                                            UserOptions.PubChemWebServiceUri, searchFor, UserOptions.ResultsPerCall, webEnv, startFrom);
                }
            }

            var securityProtocol = ServicePointManager.SecurityProtocol;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            var request = (HttpWebRequest)WebRequest.Create(webCall);

            request.Timeout = 30000;
            request.UserAgent = "Chem4Word";

            HttpWebResponse httpWebResponse;
            try
            {
                httpWebResponse = (HttpWebResponse)request.GetResponse();
                if (HttpStatusCode.OK.Equals(httpWebResponse.StatusCode))
                {
                    using (var responseStream = httpWebResponse.GetResponseStream())
                    {
                        if (responseStream != null)
                        {
                            var responseBody = new StreamReader(responseStream).ReadToEnd();
                            try
                            {
                                var resultDocument = XDocument.Parse(responseBody);

                                // Get the count of results
                                var countElement = resultDocument.XPathSelectElement("//Count");
                                if (countElement != null)
                                {
                                    resultsCount = int.Parse(countElement.Value);
                                }

                                // Get current position
                                var retStartElement = resultDocument.XPathSelectElement("//RetStart");
                                if (retStartElement != null)
                                {
                                    resultsCount = int.Parse(retStartElement.Value);
                                }

                                // Get where to start from next time
                                var retMaxElement = resultDocument.XPathSelectElement("//RetMax");
                                if (retMaxElement != null)
                                {
                                    var fetched = int.Parse(retMaxElement.Value);
                                    lastResult = firstResult + fetched;
                                }

                                // Get WebEnv for history
                                var webEnvElement = resultDocument.XPathSelectElement("//WebEnv");
                                if (webEnvElement != null)
                                {
                                    webEnv = webEnvElement.Value;
                                }

                                // Set flags for More/Prev buttons
                                if (lastResult > numResults)
                                {
                                    PreviousButton.Enabled = true;
                                }
                                else
                                {
                                    PreviousButton.Enabled = false;
                                }

                                if (lastResult < resultsCount)
                                {
                                    NextButton.Enabled = true;
                                }
                                else
                                {
                                    NextButton.Enabled = false;
                                }

                                var ids = resultDocument.XPathSelectElements("//Id");
                                var count = ids.Count();
                                Results.Items.Clear();

                                if (count > 0)
                                {
                                    // Set form title
                                    Text = $"Search PubChem - Showing {firstResult + 1} to {lastResult} [of {resultsCount}]";
                                    Refresh();

                                    var sb = new StringBuilder();
                                    for (var i = 0; i < count; i++)
                                    {
                                        var id = ids.ElementAt(i);
                                        if (i > 0)
                                        {
                                            sb.Append(",");
                                        }
                                        sb.Append(id.Value);
                                    }
                                    GetData(sb.ToString());
                                }
                                else
                                {
                                    // Set error box
                                    ErrorsAndWarnings.Text = "Sorry, no results were found.";
                                }
                            }
                            catch (Exception innerException)
                            {
                                Telemetry.Write(module, "Exception", innerException.Message);
                                Telemetry.Write(module, "Exception", innerException.StackTrace);
                                Telemetry.Write(module, "Exception(Data)", responseBody);
                            }
                        }
                    }
                }
                else
                {
                    var sb = new StringBuilder();
                    sb.AppendLine($"Status code {httpWebResponse.StatusCode} was returned by the server");
                    Telemetry.Write(module, "Warning", sb.ToString());
                    UserInteractions.AlertUser(sb.ToString());
                }
            }
            catch (Exception outerException)
            {
                if (outerException.Message.Equals("The operation has timed out"))
                {
                    ErrorsAndWarnings.Text = "Please try again later - the service has timed out";
                }
                else
                {
                    ErrorsAndWarnings.Text = outerException.Message;
                    Telemetry.Write(module, "Exception", outerException.Message);
                    Telemetry.Write(module, "Exception", outerException.StackTrace);
                }
            }
            finally
            {
                ServicePointManager.SecurityProtocol = securityProtocol;
                Cursor = Cursors.Default;
            }
        }

        private void GetData(string idlist)
        {
            var module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            var request = (HttpWebRequest)
                WebRequest.Create(
                    string.Format(CultureInfo.InvariantCulture,
                        "{0}entrez/eutils/esummary.fcgi?db=pccompound&id={1}&retmode=xml",
                        UserOptions.PubChemWebServiceUri, idlist));

            request.Timeout = 30000;
            request.UserAgent = "Chem4Word";

            var securityProtocol = ServicePointManager.SecurityProtocol;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            HttpWebResponse response;
            try
            {
                response = (HttpWebResponse)request.GetResponse();
                if (HttpStatusCode.OK.Equals(response.StatusCode))
                {
                    Results.Enabled = true;

                    // we will read data via the response stream
                    using (var resStream = response.GetResponseStream())
                    {
                        var resultDocument = XDocument.Load(new StreamReader(resStream));
                        var compounds = resultDocument.XPathSelectElements("//DocSum");
                        if (compounds.Any())
                        {
                            foreach (var compound in compounds)
                            {
                                var id = compound.XPathSelectElement("./Id");
                                var name = compound.XPathSelectElement("./Item[@Name='IUPACName']");
                                //var smiles = compound.XPathSelectElement("./Item[@Name='CanonicalSmile']")
                                var formula = compound.XPathSelectElement("./Item[@Name='MolecularFormula']");
                                var lvi = new ListViewItem(id.Value);

                                lvi.SubItems.Add(new ListViewItem.ListViewSubItem(lvi, name.Value));
                                //lvi.SubItems.Add(new ListViewItem.ListViewSubItem(lvi, smiles.ToString()))
                                lvi.SubItems.Add(new ListViewItem.ListViewSubItem(lvi, formula.Value));

                                Results.Items.Add(lvi);
                                // Add to a list view ...
                            }
                        }
                        else
                        {
                            Debug.WriteLine("Something went wrong");
                        }
                    }
                }
                else
                {
                    var sb = new StringBuilder();
                    sb.AppendLine($"Bad request. Status code: {response.StatusCode}");
                    UserInteractions.AlertUser(sb.ToString());
                }
            }
            catch (Exception ex)
            {
                if (ex.Message.Equals("The operation has timed out"))
                {
                    ErrorsAndWarnings.Text = "Please try again later - the service has timed out";
                }
                else
                {
                    ErrorsAndWarnings.Text = ex.Message;
                    Telemetry.Write(module, "Exception", ex.Message);
                    Telemetry.Write(module, "Exception", ex.StackTrace);
                }
            }
            finally
            {
                ServicePointManager.SecurityProtocol = securityProtocol;
            }
        }

        private string FetchStructure()
        {
            var module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            var result = lastSelected;
            ImportButton.Enabled = false;

            var selected = Results.SelectedItems;
            if (selected.Count > 0)
            {
                var item = selected[0];
                var pubchemId = item.Text;
                PubChemId = pubchemId;

                if (!pubchemId.Equals(lastSelected))
                {
                    Cursor = Cursors.WaitCursor;

                    // https://pubchem.ncbi.nlm.nih.gov/rest/pug/compound/cid/241/record/SDF

                    var securityProtocol = ServicePointManager.SecurityProtocol;
                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                    try
                    {
                        var request = (HttpWebRequest)WebRequest.Create(
                            string.Format(CultureInfo.InvariantCulture, "{0}rest/pug/compound/cid/{1}/record/SDF",
                                UserOptions.PubChemRestApiUri, pubchemId));

                        request.Timeout = 30000;
                        request.UserAgent = "Chem4Word";

                        HttpWebResponse response;

                        response = (HttpWebResponse)request.GetResponse();
                        if (HttpStatusCode.OK.Equals(response.StatusCode))
                        {
                            // we will read data via the response stream
                            using (var resStream = response.GetResponseStream())
                            {
                                lastMolfile = new StreamReader(resStream).ReadToEnd();
                                var sdFileConverter = new SdFileConverter();
                                var model = sdFileConverter.Import(lastMolfile);
                                var cmlConverter = new CMLConverter();
                                Cml = cmlConverter.Export(model);

                                model.ScaleToAverageBondLength(Core.Helpers.Constants.StandardBondLength);
                                this.display1.Chemistry = model;

                                if (model.AllWarnings.Count > 0 || model.AllErrors.Count > 0)
                                {
                                    Telemetry.Write(module, "Exception(Data)", lastMolfile);
                                    var lines = new List<string>();
                                    if (model.AllErrors.Count > 0)
                                    {
                                        Telemetry.Write(module, "Exception(Data)", string.Join(Environment.NewLine, model.AllErrors));
                                        lines.Add("Errors(s)");
                                        lines.AddRange(model.AllErrors);
                                    }
                                    if (model.AllWarnings.Count > 0)
                                    {
                                        Telemetry.Write(module, "Exception(Data)", string.Join(Environment.NewLine, model.AllWarnings));
                                        lines.Add("Warnings(s)");
                                        lines.AddRange(model.AllWarnings);
                                    }
                                    ErrorsAndWarnings.Text = string.Join(Environment.NewLine, lines);
                                }
                                else
                                {
                                    ImportButton.Enabled = true;
                                }
                            }
                            result = pubchemId;
                        }
                        else
                        {
                            result = string.Empty;
                            lastMolfile = string.Empty;

                            var sb = new StringBuilder();
                            sb.AppendLine($"Bad request. Status code: {response.StatusCode}");
                            UserInteractions.AlertUser(sb.ToString());
                        }
                    }
                    catch (Exception ex)
                    {
                        if (ex.Message.Equals("The operation has timed out"))
                        {
                            ErrorsAndWarnings.Text = "Please try again later - the service has timed out";
                        }
                        else
                        {
                            ErrorsAndWarnings.Text = ex.Message;
                            Telemetry.Write(module, "Exception", ex.Message);
                            Telemetry.Write(module, "Exception", ex.StackTrace);
                        }
                    }
                    finally
                    {
                        ServicePointManager.SecurityProtocol = securityProtocol;
                        Cursor = Cursors.Default;
                    }
                }
            }

            return result;
        }

        private void OnFormClosing_SearchPubChem(object sender, FormClosingEventArgs e)
        {
            var module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                if (DialogResult != DialogResult.OK)
                {
                    DialogResult = DialogResult.Cancel;
                }
            }
            catch (Exception ex)
            {
                new ReportError(Telemetry, TopLeft, module, ex).ShowDialog();
            }
        }
    }
}