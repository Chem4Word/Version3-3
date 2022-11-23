// ---------------------------------------------------------------------------
//  Copyright (c) 2022, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.Driver.Open;
using Chem4Word.Model2;
using Chem4Word.Model2.Converters.CML;
using Chem4Word.Model2.Converters.ProtocolBuffers;
using Chem4Word.Model2.Helpers;
using IChem4Word.Contracts;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Chem4Word.Core.Helpers;
using IChem4Word.Contracts.Dto;

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

                    progressBar1.Value++;
                    Application.DoEvents();
                }

                driver.CommitTransaction();

                progressBar1.Value = 0;
                progressBar1.Maximum = 0;
                label1.Text = "Done";
            }
        }

        private void Export_Click(object sender, EventArgs e)
        {
            var lvi = GetSelectedListViewItem();
            if (lvi != null)
            {
                var folder = new FolderBrowserDialog();
                folder.RootFolder = Environment.SpecialFolder.MyComputer;
                folder.Description = "Export to ...";
                var dr = folder.ShowDialog();
                if (dr == DialogResult.OK)
                {
                    var cmlConverter = new CMLConverter();
                    var protocolBufferConverter = new ProtocolBufferConverter();

                    label1.Text = "Fetching data ...";
                    Application.DoEvents();

                    var driver = new OpenDriver();
                    driver.DatabaseDetails = _listOfLibraries.AvailableDatabases.FirstOrDefault(n => n.DisplayName.Equals(lvi.Text));

                    var objects = driver.GetAllChemistry();

                    progressBar1.Maximum = objects.Count;
                    label1.Text = $"Exporting {objects.Count} objects ...";
                    Application.DoEvents();

                    foreach (var chemistryDataObject in objects)
                    {
                        var filename = Path.Combine(folder.SelectedPath, $"Chem4Word-{chemistryDataObject.Id:000000000}.cml");
                        Model model;
                        if (chemistryDataObject.DataType.Equals("cml"))
                        {
                            model = cmlConverter.Import(Encoding.UTF8.GetString(chemistryDataObject.Chemistry));
                        }
                        else
                        {
                            model = protocolBufferConverter.Import(chemistryDataObject.Chemistry);
                        }
                        model.EnsureBondLength(Constants.StandardBondLength, false);

                        var contents = Constants.XmlFileHeader + Environment.NewLine
                                                               + cmlConverter.Export(model);
                        File.WriteAllText(filename, contents);

                        progressBar1.Value++;
                        Application.DoEvents();
                    }

                    progressBar1.Value = 0;
                    progressBar1.Maximum = 0;
                    label1.Text = "Done";
                }
            }
        }

        private void Import_Click(object sender, EventArgs e)
        {
            var lvi = GetSelectedListViewItem();
            if (lvi != null)
            {
                var folder = new FolderBrowserDialog();
                folder.RootFolder = Environment.SpecialFolder.MyComputer;
                folder.Description = "Import from ...";
                var dr = folder.ShowDialog();
                if (dr == DialogResult.OK)
                {
                    var driver = new OpenDriver();
                    driver.DatabaseDetails = _listOfLibraries.AvailableDatabases.FirstOrDefault(n => n.DisplayName.Equals(lvi.Text));

                    var files = Directory.GetFiles(folder.SelectedPath, "*.cml").ToList();
                    if (files.Count > 0)
                    {
                        label1.Text = $"Importing {files.Count} files ...";
                        Application.DoEvents();

                        progressBar1.Maximum = files.Count;
                        driver.StartTransaction();

                        var cmlConverter = new CMLConverter();

                        foreach (string file in files)
                        {
                            var contents = File.ReadAllText(file);
                            var model = cmlConverter.Import(contents);

                            var dto = DtoFromModel(model);

                            driver.AddChemistry(dto);

                            progressBar1.Value++;
                            Application.DoEvents();
                        }

                        driver.CommitTransaction();

                        progressBar1.Value = 0;
                        progressBar1.Maximum = 0;
                        label1.Text = "Done";
                    }
                }
            }

            // Local Function
            ChemistryDataObject DtoFromModel(Model model)
            {
                var dto = new ChemistryDataObject
                          {
                              DataType = "pbuff",
                              Name = model.QuickName,
                              Formula = model.ConciseFormula,
                              MolWeight = model.MolecularWeight
                          };

                var protocolBufferConverter = new ProtocolBufferConverter();
                dto.Chemistry = protocolBufferConverter.Export(model);

                // Lists of ChemistryNameDataObject for TreeView
                foreach (var property in model.GetAllNames())
                {
                    var chemistryNameDataObject = CreateNamesFromModel(property);
                    dto.Names.Add(chemistryNameDataObject);
                }

                foreach (var property in model.GetAllFormulae())
                {
                    var chemistryNameDataObject = CreateNamesFromModel(property);
                    dto.Formulae.Add(chemistryNameDataObject);
                }

                foreach (var property in model.GetAllCaptions())
                {
                    var chemistryNameDataObject = CreateNamesFromModel(property);
                    dto.Captions.Add(chemistryNameDataObject);
                }

                return dto;
            }

            // Local Function
            ChemistryNameDataObject CreateNamesFromModel(TextualProperty textualProperty)
            {
                var nameDataObject = new ChemistryNameDataObject
                                     {
                                         Name = textualProperty.Value
                                     };

                if (textualProperty.FullType.Contains(":"))
                {
                    var parts = textualProperty.FullType.Split(':');
                    nameDataObject.NameSpace = parts[0];
                    nameDataObject.Tag = parts[1];
                }
                else
                {
                    nameDataObject.NameSpace = "chem4word";
                    nameDataObject.Tag = textualProperty.FullType;
                }

                return nameDataObject;
            }
        }

        private void Erase_Click(object sender, EventArgs e)
        {
            var lvi = GetSelectedListViewItem();
            if (lvi != null)
            {
                var result = MessageBox.Show($"Delete All structures from '{lvi.Text}'", "Library Transformer", MessageBoxButtons.YesNo);
                if (result == DialogResult.Yes)
                {
                    var driver = new OpenDriver();
                    driver.DatabaseDetails = _listOfLibraries.AvailableDatabases.FirstOrDefault(n => n.DisplayName.Equals(lvi.Text));
                    driver.DeleteAllChemistry();
                }
            }
        }
    }
}