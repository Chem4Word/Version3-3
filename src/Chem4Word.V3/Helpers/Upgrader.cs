﻿// ---------------------------------------------------------------------------
//  Copyright (c) 2025, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.Core;
using Chem4Word.Core.Helpers;
using Chem4Word.Model2;
using Chem4Word.Model2.Converters.CML;
using Microsoft.Office.Core;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using Word = Microsoft.Office.Interop.Word;

namespace Chem4Word.Helpers
{
    public static class Upgrader
    {
        private static string _product = Assembly.GetExecutingAssembly().FullName.Split(',')[0];
        private static string _class = MethodBase.GetCurrentMethod().DeclaringType?.Name;

        private static object _missing = Type.Missing;

        public static DialogResult UpgradeIsRequired(Word.Document document)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            DialogResult result = DialogResult.Cancel;

            int count = LegacyChemistryCount(document);

            if (count > 0)
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine($"We have detected {count} legacy Chemistry objects in this document.");
                sb.AppendLine("Would you like them converted to the new format?");
                sb.AppendLine("");
                sb.AppendLine("  Click Yes to Upgrade then");
                sb.AppendLine("  Click No to leave them as they are");
                sb.AppendLine("");
                sb.AppendLine("This operation can't be undone.");
                result = UserInteractions.AskUserYesNo(sb.ToString());
                if (Globals.Chem4WordV3.Telemetry != null)
                {
                    Globals.Chem4WordV3.Telemetry.Write(module, "Information", $"Detected {count} legacy chemistry ContentControl(s)");
                }
                else
                {
                    RegistryHelper.StoreMessage(module, $"Detected {count} legacy chemistry ContentControl(s)");
                }
            }

            return result;
        }

        private static string DecodeContentControlType(Word.WdContentControlType? contentControlType)
        {
            // Date from https://msdn.microsoft.com/en-us/library/microsoft.office.interop.word.wdcontentcontroltype(v=office.14).aspx
            string result = "";

            switch (contentControlType)
            {
                case Word.WdContentControlType.wdContentControlRichText:
                    result = "Rich-Text";
                    break;

                case Word.WdContentControlType.wdContentControlText:
                    result = "Text";
                    break;

                case Word.WdContentControlType.wdContentControlBuildingBlockGallery:
                    result = "Picture";
                    break;

                case Word.WdContentControlType.wdContentControlComboBox:
                    result = "ComboBox";
                    break;

                case Word.WdContentControlType.wdContentControlDropdownList:
                    result = "Drop-Down List";
                    break;

                case Word.WdContentControlType.wdContentControlPicture:
                    result = "Building Block Gallery";
                    break;

                case Word.WdContentControlType.wdContentControlDate:
                    result = "Date";
                    break;

                case Word.WdContentControlType.wdContentControlGroup:
                    result = "Group";
                    break;

                case Word.WdContentControlType.wdContentControlCheckBox:
                    result = "CheckBox";
                    break;

                case Word.WdContentControlType.wdContentControlRepeatingSection:
                    result = "Repeating Section";
                    break;

                default:
                    result = contentControlType.ToString();
                    break;
            }

            return result;
        }

        public static int LegacyChemistryCount(Word.Document document)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            int count = 0;

            foreach (Word.ContentControl cc in document.ContentControls)
            {
                Word.WdContentControlType? contentControlType = cc.Type;
                try
                {
                    if (cc.Title != null && cc.Title.Equals(Constants.LegacyContentControlTitle))
                    {
                        count++;
                    }
                }
                catch (Exception ex)
                {
                    Globals.Chem4WordV3.Telemetry.Write(module, "Exception", $"{ex.Message}");
                    Globals.Chem4WordV3.Telemetry.Write(module, "Exception", $"{ex.StackTrace}");
                }
            }

            return count;
        }

        public static void DoUpgrade(Word.Document document)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            int sel = Globals.Chem4WordV3.Application.Selection.Range.Start;
            Globals.Chem4WordV3.DisableContentControlEvents();

            try
            {
                string extension = document.FullName.Split('.').Last();
                string guid = Guid.NewGuid().ToString("N");
                string timestamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
                string destination = Path.Combine(Globals.Chem4WordV3.AddInInfo.ProductAppDataPath, "Backups", $"Chem4Word-{timestamp}-{guid}.{extension}");
                File.Copy(document.FullName, destination);
            }
            catch (Exception ex)
            {
                // Nothing much we can do here :-(
                Debug.WriteLine(ex.Message);
            }

            Dictionary<string, CustomXMLPart> customXmlParts = new Dictionary<string, CustomXMLPart>();
            List<UpgradeTarget> targets = CollectData(document);
            int upgradedCCs = 0;
            int upgradedXml = 0;

            var cmlConverter = new CMLConverter();

            foreach (var target in targets)
            {
                if (target.ContentControls.Count > 0)
                {
                    upgradedXml++;
                    upgradedCCs += target.ContentControls.Count;
                }

                foreach (var cci in target.ContentControls)
                {
                    foreach (Word.ContentControl cc in document.ContentControls)
                    {
                        if (cc.ID.Equals(cci.Id))
                        {
                            int start;
                            bool isFormula;
                            string text;

                            switch (cci.Type)
                            {
                                case "2D":
                                    cc.LockContents = false;
                                    cc.Title = Constants.ContentControlTitle;
                                    cc.Tag = target.Model.CustomXmlPartGuid;
                                    cc.LockContents = true;

                                    target.Model.EnsureBondLength(Globals.Chem4WordV3.SystemOptions.BondLength, false);

                                    // ToDo: [MAW] Regenerate converted 2D structures
                                    break;

                                case "new":
                                    cc.LockContents = false;
                                    cc.Range.Delete();
                                    start = cc.Range.Start;
                                    cc.Delete();

                                    Globals.Chem4WordV3.Application.Selection.SetRange(start - 1, start - 1);
                                    var options = new RenderingOptions
                                    {
                                        ExplicitC = Globals.Chem4WordV3.SystemOptions.ExplicitC,
                                        ExplicitH = Globals.Chem4WordV3.SystemOptions.ExplicitH,
                                        ShowColouredAtoms = Globals.Chem4WordV3.SystemOptions.ShowColouredAtoms,
                                        ShowMoleculeGrouping = Globals.Chem4WordV3.SystemOptions.ShowMoleculeGrouping
                                    };
                                    var model = new Model();
                                    model.SetUserOptions(options);
                                    var molecule = new Molecule();
                                    molecule.Names.Add(new TextualProperty { Id = "m1.n1", Value = cci.Text, FullType = CMLConstants.ValueChem4WordSynonym });
                                    model.AddMolecule(molecule);
                                    molecule.Parent = model;
                                    model.CustomXmlPartGuid = Guid.NewGuid().ToString("N");

                                    document.CustomXMLParts.Add(XmlHelper.AddHeader(cmlConverter.Export(model)));

                                    var ccn = document.ContentControls.Add(Word.WdContentControlType.wdContentControlRichText, ref _missing);
                                    ChemistryHelper.Insert1D(document, ccn.ID, cci.Text, false, $"m1.n1:{model.CustomXmlPartGuid}");
                                    ccn.LockContents = true;
                                    break;

                                default:
                                    cc.LockContents = false;
                                    cc.Range.Delete();
                                    start = cc.Range.Start;
                                    cc.Delete();

                                    Globals.Chem4WordV3.Application.Selection.SetRange(start - 1, start - 1);
                                    isFormula = false;
                                    text = ChemistryHelper.GetInlineText(target.Model, cci.Type, ref isFormula, out _);
                                    var ccr = document.ContentControls.Add(Word.WdContentControlType.wdContentControlRichText, ref _missing);
                                    ChemistryHelper.Insert1D(document, ccr.ID, text, isFormula, $"{cci.Type}:{target.Model.CustomXmlPartGuid}");
                                    ccr.LockContents = true;
                                    break;
                            }
                        }
                    }
                }

                CustomXMLPart cxml = document.CustomXMLParts.SelectByID(target.CxmlPartId);
                if (customXmlParts.ContainsKey(cxml.Id))
                {
                    customXmlParts.Add(cxml.Id, cxml);
                }
                document.CustomXMLParts.Add(XmlHelper.AddHeader(cmlConverter.Export(target.Model)));
            }

            EraseChemistryZones(document);

            foreach (var kvp in customXmlParts.ToList())
            {
                kvp.Value.Delete();
            }

            Globals.Chem4WordV3.EnableContentControlEvents();
            Globals.Chem4WordV3.Application.Selection.SetRange(sel, sel);
            if (upgradedCCs + upgradedXml > 0)
            {
                if (Globals.Chem4WordV3.Telemetry != null)
                {
                    Globals.Chem4WordV3.Telemetry.Write(module, "Information", $"Upgraded {upgradedCCs} Chemistry Objects for {upgradedXml} Structures");
                }
                else
                {
                    RegistryHelper.StoreMessage(module, $"Upgraded {upgradedCCs} Chemistry Objects for {upgradedXml} Structures");
                }
                UserInteractions.AlertUser($"Upgrade Completed{Environment.NewLine}{Environment.NewLine}Upgraded {upgradedCCs} Chemistry Objects for {upgradedXml} Structures");
            }
        }

        private static void EraseChemistryZones(Word.Document doc)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            foreach (CustomXMLPart xmlPart in doc.CustomXMLParts)
            {
                string xml = xmlPart.XML;
                if (xml.Contains("<ChemistryZone"))
                {
                    xmlPart.Delete();
                }
            }
        }

        private static string GetDepictionValue(string cml, string xPath)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            string result = null;

            XDocument xDocument = XDocument.Parse(cml);

            if (xDocument.Root != null)
            {
                var objects = (IEnumerable)xDocument.Root.XPathEvaluate(xPath);
                var xObjects = objects.Cast<XObject>();
                XObject xObject = xObjects.FirstOrDefault();

                XElement xe = xObject as XElement;
                if (xe != null)
                {
                    if (!xe.HasElements)
                    {
                        result = xe.Value;
                    }
                }

                XAttribute xa = xObject as XAttribute;
                if (xa != null)
                {
                    result = xa.Value;
                }
            }

            return result;
        }

        private static List<UpgradeTarget> CollectData(Word.Document document)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            List<UpgradeTarget> targets = new List<UpgradeTarget>();

            Word.Selection sel = Globals.Chem4WordV3.Application.Selection;

            // Step 1 find location of all content controls
            // Step 2 extract Cml
            // Step 3 find all Chemistry Zones

            List<ContentControlInfo> listOfContentControls = new List<ContentControlInfo>();

            for (int i = 1; i <= document.ContentControls.Count; i++)
            {
                Word.ContentControl cc = document.ContentControls[i];
                if (cc.Title != null && cc.Title.Equals(Constants.LegacyContentControlTitle))
                {
                    ContentControlInfo cci = new ContentControlInfo();
                    cci.Id = cc.ID;
                    cci.Index = i;
                    cci.Location = cc.Range.Start;
                    listOfContentControls.Add(cci);
                }
            }

            foreach (CustomXMLPart xmlPart in document.CustomXMLParts)
            {
                string xml = xmlPart.XML;
                if (xml.Contains("<cml"))
                {
                    UpgradeTarget ut = new UpgradeTarget();
                    ut.CxmlPartId = xmlPart.Id;
                    ut.Cml = xmlPart.XML;
                    CMLConverter converter = new CMLConverter();
                    ut.Model = converter.Import(ut.Cml);
                    ut.Model.CustomXmlPartGuid = Guid.NewGuid().ToString("N");
                    targets.Add(ut);
                }
            }

            foreach (CustomXMLPart xmlPart in document.CustomXMLParts)
            {
                string xml = xmlPart.XML;

                if (xml.Contains("<ChemistryZone"))
                {
                    XmlDocument xmlDoc = new XmlDocument();
                    xmlDoc.LoadXml(xml);

                    string ccValue = null;
                    string cmlValue = null;
                    string ddValue = null;

                    XmlNode refNode = xmlDoc.SelectSingleNode("//ref");
                    XmlNode ddNode = xmlDoc.SelectSingleNode("//DocumentDepictionOptionXPath");

                    if (ddNode != null)
                    {
                        ddValue = ddNode.Attributes["value"].Value;
                    }

                    if (refNode != null)
                    {
                        ccValue = refNode.Attributes["cc"].Value;
                        cmlValue = refNode.Attributes["cml"].Value;

                        foreach (UpgradeTarget target in targets)
                        {
                            if (target.CxmlPartId.Equals(cmlValue))
                            {
                                ContentControlInfo cci = new ContentControlInfo();
                                cci.Id = ccValue;

                                string dv = GetDepictionValue(target.Cml, ddValue);

                                if (dv == null)
                                {
                                    cci.Type = "2D";
                                }
                                else
                                {
                                    // Default to flagging that new 1D molecule is to be created
                                    cci.Type = "new";
                                    cci.Text = dv;

                                    #region Find new style 1D code

                                    foreach (var molecule in target.Model.Molecules.Values)
                                    {
                                        if (dv.Equals(molecule.ConciseFormula))
                                        {
                                            cci.Type = $"{molecule.Id}.f0";
                                        }
                                        foreach (var formula in molecule.Formulas)
                                        {
                                            if (formula.Value.Equals(dv))
                                            {
                                                cci.Type = formula.Id;
                                                break;
                                            }
                                        }
                                        foreach (var name in molecule.Names)
                                        {
                                            if (name.Value.Equals(dv))
                                            {
                                                cci.Type = name.Id;
                                                break;
                                            }
                                        }
                                    }

                                    #endregion Find new style 1D code
                                }

                                foreach (var ccii in listOfContentControls)
                                {
                                    if (ccii.Id.Equals(cci.Id))
                                    {
                                        cci.Location = ccii.Location;
                                        cci.Index = ccii.Index;
                                        break;
                                    }
                                }

                                target.ContentControls.Add(cci);
                                break;
                            }
                        }
                    }
                }
            }

            return targets;
        }
    }

    public class UpgradeTarget
    {
        public UpgradeTarget()
        {
            ContentControls = new List<ContentControlInfo>();
        }

        public string CxmlPartId { get; set; }
        public string Cml { get; set; }
        public Model Model { get; set; }
        public List<ContentControlInfo> ContentControls { get; set; }
    }

    public class ContentControlInfo
    {
        public string Id { get; set; }
        public int Index { get; set; }
        public int Location { get; set; }
        public string Type { get; set; }
        public string Text { get; set; }
    }
}