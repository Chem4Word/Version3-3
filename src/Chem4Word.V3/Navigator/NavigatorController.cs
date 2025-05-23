// ---------------------------------------------------------------------------
//  Copyright (c) 2025, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.ACME.Models;
using Chem4Word.Core.UI.Forms;
using Chem4Word.Helpers;
using Chem4Word.Model2.Converters.CML;
using Microsoft.Office.Core;
using Microsoft.Office.Interop.Word;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using ContentControl = Microsoft.Office.Interop.Word.ContentControl;

namespace Chem4Word.Navigator
{
    public class NavigatorController
    {
        private static string _product = Assembly.GetExecutingAssembly().FullName.Split(',')[0];
        private static string _class = MethodBase.GetCurrentMethod().DeclaringType?.Name;

        public ObservableCollection<ChemistryObject> NavigatorItems { get; }

        //references the custom XML parts in the document
        private CustomXMLParts Parts { get; }

        //local reference to the active document
        private readonly Document _doc;

        public NavigatorController()
        {
            NavigatorItems = new ObservableCollection<ChemistryObject>();
        }

        public NavigatorController(Document doc) : this()
        {
            var module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            //get a reference to the document
            _doc = doc;

            Globals.Chem4WordV3.Telemetry.Write(module, "Information", $"NavigatorController({doc.DocID})");
            Parts = _doc.CustomXMLParts.SelectByNamespace(CMLNamespaces.cml.NamespaceName);

            Parts.PartAfterLoad -= OnAfterLoad_CustomXmlPart;
            Parts.PartAfterLoad += OnAfterLoad_CustomXmlPart;
            Parts.PartBeforeDelete -= OnBeforeDelete_CustomXmlPart;
            Parts.PartBeforeDelete += OnBeforeDelete_CustomXmlPart;

            LoadModel();
        }

        /// <summary>
        /// Loads up the model initially from the document
        /// </summary>
        private void LoadModel()
        {
            var module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod()?.Name}()";

            try
            {
                var converter = new CMLConverter();
                if (NavigatorItems.Any())
                {
                    NavigatorItems.Clear();
                }

                if (_doc != null)
                {
                    var added = new Dictionary<string, int>();

                    var items = from ContentControl ccs in _doc.ContentControls
                                join CustomXMLPart part in Parts
                                    on CustomXmlPartHelper.GuidFromTag(ccs?.Tag) equals CustomXmlPartHelper.GetCmlId(part)
                                orderby ccs.Range.Start
                                let chemModel = converter.Import(part.XML)
                                select new ChemistryObject
                                {
                                    CustomControlTag = CustomXmlPartHelper.GuidFromTag(ccs?.Tag),
                                    Chemistry = part.XML,
                                    Formula = chemModel.ConciseFormula
                                };

                    var chemistryObjects = items.ToList();

                    Globals.Chem4WordV3.Telemetry.Write(module, "Information", $"Found {chemistryObjects.Count} XmlParts in document [{_doc.DocID}]");

                    foreach (var chemistryObject in chemistryObjects)
                    {
                        if (!string.IsNullOrEmpty(chemistryObject.CustomControlTag)
                            && !added.ContainsKey(chemistryObject.CustomControlTag))
                        {
                            NavigatorItems.Add(chemistryObject);
                            added.Add(chemistryObject.CustomControlTag, 1);
                        }
                    }

                    Globals.Chem4WordV3.Telemetry.Write(module, "Information", $"Number of Navigator items loaded = {NavigatorItems.Count}");
                }
            }
            catch (Exception ex)
            {
                using (var form = new ReportError(Globals.Chem4WordV3.Telemetry, Globals.Chem4WordV3.WordTopLeft, module, ex))
                {
                    form.ShowDialog();
                }
            }
        }

        /// <summary>
        /// handles deletion of an XML Part...removes the corresponding navigator item
        /// </summary>
        /// <param name="oldXmlPart">The custom XML part that gets deleted</param>
        private void OnBeforeDelete_CustomXmlPart(CustomXMLPart oldXmlPart)
        {
            var module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod()?.Name}()";
            try
            {
                Globals.Chem4WordV3.Telemetry.Write(module, "Information", $"XmlPart Id:{oldXmlPart.Id} has been removed from document [{_doc.DocID}]");
                var chemistryObject = NavigatorItems.FirstOrDefault(ni
                                                                => CustomXmlPartHelper.GuidFromTag(ni.CustomControlTag) == CustomXmlPartHelper.GetCmlId(oldXmlPart));
                if (chemistryObject != null)
                {
                    Globals.Chem4WordV3.Telemetry.Write(module, "Information", $"Removing Tag {chemistryObject.CustomControlTag} from items in navigator for document [{_doc.DocID}]");
                    NavigatorItems.Remove(chemistryObject);
                }
            }
            catch (Exception ex)
            {
                using (var form = new ReportError(Globals.Chem4WordV3.Telemetry, Globals.Chem4WordV3.WordTopLeft, module, ex))
                {
                    form.ShowDialog();
                }
            }
        }

        /// <summary>
        /// Occurs after a new custom XMl part is loaded into the document
        /// Useful for updating the Navigator
        /// </summary>
        /// <param name="newXmlPart"></param>
        private void OnAfterLoad_CustomXmlPart(CustomXMLPart newXmlPart)
        {
            var module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod()?.Name}()";
            try
            {
                var cmlId = CustomXmlPartHelper.GetCmlId(newXmlPart);
                Globals.Chem4WordV3.Telemetry.Write(module, "Information", $"XmlPart Id:{newXmlPart.Id} Tag {cmlId} has been added to document [{_doc.DocID}]");
                var converter = new CMLConverter();

                //get the chemistry
                var chemModel = converter.Import(newXmlPart.XML);

                //find out which content control matches the custom XML part
                try
                {
                    // ReSharper disable once InconsistentNaming
                    var matchingCC = (from ContentControl cc in _doc.ContentControls
                                      orderby cc.Range.Start
                                      where CustomXmlPartHelper.GuidFromTag(cc.Tag) == cmlId
                                      select cc).First();
                    Globals.Chem4WordV3.Telemetry.Write(module, "Information", $"Found Content Control {matchingCC.ID} with Tag {matchingCC.Tag}");

                    //get the ordinal position of the content control
                    var start = 0;
                    foreach (ContentControl cc in _doc.ContentControls)
                    {
                        if (cc.ID == matchingCC.ID)
                        {
                            break;
                        }

                        start += 1;
                    }

                    //insert the new navigator item at the ordinal position
                    var newNavItem = new ChemistryObject
                    {
                        CustomControlTag = matchingCC.Tag,
                        Chemistry = newXmlPart.XML,
                        Formula = chemModel.ConciseFormula
                    };
                    try
                    {
                        Globals.Chem4WordV3.Telemetry.Write(module, "Information", $"Adding Tag {newNavItem.CustomControlTag} into navigator at position {start} for document [{_doc.DocID}]");
                        NavigatorItems.Insert(start, newNavItem);
                    }
                    catch (ArgumentOutOfRangeException) //can happen when there are more content controls than navigator items
                    {
                        //so simply insert the new navigator item at the end
                        Globals.Chem4WordV3.Telemetry.Write(module, "Information", $"Adding Tag {newNavItem.CustomControlTag} into navigator at end for document [{_doc.DocID}]");
                        NavigatorItems.Add(newNavItem);
                    }
                }
                catch (InvalidOperationException)
                {
                    //sequence contains no elements - thrown on close
                    //just ignore
                }
            }
            catch (Exception ex)
            {
                using (var form = new ReportError(Globals.Chem4WordV3.Telemetry, Globals.Chem4WordV3.WordTopLeft, module, ex))
                {
                    form.ShowDialog();
                }
            }
        }
    }
}