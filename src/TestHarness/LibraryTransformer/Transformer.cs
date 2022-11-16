// ---------------------------------------------------------------------------
//  Copyright (c) 2022, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.Driver.Open;
using Chem4Word.Model2.Converters.CML;
using Chem4Word.Model2.Converters.ProtocolBuffers;
using IChem4Word.Contracts;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace LibraryTransformer
{
    public partial class Transformer : Form
    {
        private ListOfLibraries _listOfLibraries;

        public Transformer()
        {
            InitializeComponent();
        }

        private string _settingsFile = @"C:\ProgramData\Chem4Word.V3\Libraries.json";

        private CMLConverter _cmlConverter = new CMLConverter();
        private ProtocolBufferConverter _protocolBufferConverter = new ProtocolBufferConverter();

        private void Transformer_Load(object sender, EventArgs e)
        {
            if (File.Exists(_settingsFile))
            {
                listView1.Items.Clear();

                var driver = new OpenDriver();
                var data = File.ReadAllText(_settingsFile);
                _listOfLibraries = JsonConvert.DeserializeObject<ListOfLibraries>(data);
                foreach (var details in _listOfLibraries.AvailableDatabases)
                {
                    driver.DatabaseDetails = details;
                    details.Properties = driver.GetProperties();
                    var lvi = new ListViewItem(details.DisplayName);
                    lvi.SubItems.Add(details.Connection);
                    listView1.Items.Add(lvi);
                }
            }

            label1.Text = "";
        }

        private ListViewItem GetSelectedListViewItem()
        {
            if (listView1.SelectedIndices.Count == 1)
            {
                return listView1.SelectedItems[0];
            }

            return null;
        }

        private byte[] ConvertToCml(byte[] source)
        {
            var model = _protocolBufferConverter.Import(source);
            var cml = _cmlConverter.Export(model, compressed: true);
            return Encoding.UTF8.GetBytes(cml);
        }

        private byte[] ConvertToPbuff(byte[] source)
        {
            var cml = Encoding.UTF8.GetString(source);
            var model = _cmlConverter.Import(cml);
            return _protocolBufferConverter.Export(model);
        }

        private void ToCml_Click(object sender, EventArgs e)
        {
            BulkConvert("cml");
        }

        private void ToPb_Click(object sender, EventArgs e)
        {
            BulkConvert("pbuff");
        }

        private void BulkConvert(string target)
        {
            var lvi = GetSelectedListViewItem();
            if (lvi != null)
            {
                label1.Text = "Fetching data ...";
                Application.DoEvents();

                var driver = new OpenDriver();
                driver.DatabaseDetails = _listOfLibraries.AvailableDatabases.FirstOrDefault(n => n.DisplayName.Equals(lvi.Text));
                var objects = driver.GetAllChemistry();

                progressBar1.Maximum = objects.Count;
                label1.Text = $"Updating {objects.Count} objects ...";
                Application.DoEvents();

                driver.StartTransaction();

                foreach (var chemistryDataObject in objects)
                {
                    progressBar1.Value++;
                    switch (target)
                    {
                        case "cml":
                            switch (chemistryDataObject.DataType)
                            {
                                case "pbuff":
                                    chemistryDataObject.Chemistry = ConvertToCml(chemistryDataObject.Chemistry);
                                    chemistryDataObject.DataType = target;
                                    driver.UpdateChemistry(chemistryDataObject);
                                    break;
                            }
                            break;

                        case "pbuff":
                            switch (chemistryDataObject.DataType)
                            {
                                case "cml":
                                    chemistryDataObject.Chemistry = ConvertToPbuff(chemistryDataObject.Chemistry);
                                    chemistryDataObject.DataType = target;
                                    driver.UpdateChemistry(chemistryDataObject);
                                    break;
                            }
                            break;
                    }

                    Application.DoEvents();
                }

                driver.CommitTransaction();

                progressBar1.Value = 0;
                progressBar1.Maximum = 0;
                label1.Text = "Done";
            }
        }
    }
}