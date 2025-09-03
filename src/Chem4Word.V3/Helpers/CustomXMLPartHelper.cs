// ---------------------------------------------------------------------------
//  Copyright (c) 2025, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Xml;
using Chem4Word.Core;
using Microsoft.Office.Core;
using Word = Microsoft.Office.Interop.Word;

namespace Chem4Word.Helpers
{
    public static class CustomXmlPartHelper
    {
        private static string _product = Assembly.GetExecutingAssembly().FullName.Split(',')[0];
        private static string _class = MethodBase.GetCurrentMethod()?.DeclaringType?.Name;

        private static CustomXMLParts AllChemistryParts(Word.Document document)
            => document.CustomXMLParts.SelectByNamespace("http://www.xml-cml.org/schema");

        public static int ChemistryXmlParts(Word.Document document)
            => AllChemistryParts(document).Count;

        public static CustomXMLPart FindCustomXmlPartInOtherDocuments(string id, int activeDocumentId, ref int foundInDocumentId)
        {
            CustomXMLPart result = null;

            foreach (Word.Document otherDocument in Globals.Chem4WordV3.Application.Documents)
            {
                if (!otherDocument.DocID.Equals(activeDocumentId))
                {
                    foreach (CustomXMLPart customXmlPart in AllChemistryParts(otherDocument))
                    {
                        var molId = GetCmlId(customXmlPart);
                        if (molId.Equals(id))
                        {
                            result = customXmlPart;
                            foundInDocumentId = otherDocument.DocID;
                            break;
                        }
                    }
                }
                if (result != null)
                {
                    break;
                }
            }

            return result;
        }

        public static string GuidFromTag(string tag)
        {
            var guid = string.Empty;

            if (!string.IsNullOrEmpty(tag))
            {
                guid = tag.Contains(":") ? tag.Split(':')[1] : tag;
            }

            return guid;
        }

        public static string PrefixFromTag(string tag)
        {
            var prefix = string.Empty;

            if (!string.IsNullOrEmpty(tag) && tag.Contains(":"))
            {
                prefix = tag.Split(':')[0];
            }

            return prefix;
        }

        public static List<string> ListCustomXmlParts(Word.Document document)
        {
            var parts = new List<string>
                        {
                            $"Chemistry CustomXmlParts in document [{document.DocID}] :-"
                        };

            foreach (CustomXMLPart xmlPart in AllChemistryParts(document))
            {
                var cmlId = GetCmlId(xmlPart);
                parts.Add($"XmlPartId: {xmlPart.Id} customXmlPartGuid: '{cmlId}'");
            }

            return parts;
        }

        public static CustomXMLPart GetCustomXmlPart(Word.Document document, string id)
        {
            CustomXMLPart result = null;

            var guid = GuidFromTag(id);

            if (!string.IsNullOrEmpty(guid))
            {
                foreach (CustomXMLPart xmlPart in AllChemistryParts(document))
                {
                    var cmlId = GetCmlId(xmlPart);
                    if (!string.IsNullOrEmpty(cmlId))
                    {
                        if (cmlId.Equals(guid))
                        {
                            result = xmlPart;
                            break;
                        }
                    }
                }
            }

            return result;
        }

        public static string GetCmlId(CustomXMLPart xmlPart)
        {
            var result = string.Empty;

            var xdoc = new XmlDocument();
            xdoc.LoadXml(xmlPart.XML);
            var nsmgr = new XmlNamespaceManager(xdoc.NameTable);
            nsmgr.AddNamespace("cml", "http://www.xml-cml.org/schema");
            nsmgr.AddNamespace("c4w", "http://www.chem4word.com/cml");

            var node = xdoc.SelectSingleNode("//c4w:customXmlPartGuid", nsmgr);
            if (node != null)
            {
                result = node.InnerText;
            }

            return result;
        }

        public static List<string> ListChemistryControls(Word.Document document)
        {
            var module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod()?.Name}()";

            var parts = new List<string>
                        {
                            $"Chemistry ContentControls in document [{document.DocID}] :-"
                        };

            var chemistryZones = new List<ChemistryContentControl>();

            // Pass 1 collect list of all our content controls
            foreach (Word.ContentControl cc in document.ContentControls)
            {
                if (cc.Title != null && cc.Title.Equals(CoreConstants.ContentControlTitle))
                {
                    chemistryZones.Add(new ChemistryContentControl
                    {
                        Position = cc.Range.Start,
                        Tag = cc.Tag
                    });
                }
            }

            // Pass 2 collect xmlpart ids
            var allChemistryParts = AllChemistryParts(document);

            foreach (var chemistryZone in chemistryZones)
            {
                var guid = GuidFromTag(chemistryZone.Tag);

                foreach (CustomXMLPart customXmlPart in allChemistryParts)
                {
                    var molId = GetCmlId(customXmlPart);
                    if (molId.Equals(guid))
                    {
                        chemistryZone.XmlPartId = customXmlPart.Id;
                    }
                }
            }

            // List results
            foreach (var chemistryZone in chemistryZones)
            {
                parts.Add($"Position: {chemistryZone.Position} Tag: {chemistryZone.Tag} XmlPart: {chemistryZone.XmlPartId}");
            }

            return parts;
        }

        public static int RemoveOrphanedXmlParts(Word.Document document)
        {
            var module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod()?.Name}()";
            var result = 0;

            if (document != null)
            {
                var referencedXmlParts = new Dictionary<string, int>();

                // Pass 1 collect dictionary of our unique content controls
                foreach (Word.ContentControl cc in document.ContentControls)
                {
                    if (cc.Title != null && cc.Title.Equals(CoreConstants.ContentControlTitle))
                    {
                        var guid = GuidFromTag(cc.Tag);

                        if (!string.IsNullOrEmpty(guid))
                        {
                            if (!referencedXmlParts.ContainsKey(guid))
                            {
                                referencedXmlParts.Add(guid, 1);
                            }
                        }
                    }
                }

                var backupFolder = Path.Combine(Globals.Chem4WordV3.AddInInfo.ProductAppDataPath, "Backups");

                var allChemistryParts = AllChemistryParts(document);
                var carryOutPurge = true;

                // Pass 2 save orphans
                foreach (CustomXMLPart customXmlPart in allChemistryParts)
                {
                    var molId = GetCmlId(customXmlPart);
                    if (!referencedXmlParts.ContainsKey(molId))
                    {
                        try
                        {
                            var guid = Guid.NewGuid().ToString("N");
                            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);

                            var fileName = $"Chem4Word-Orphaned-Structure-{timestamp}-{guid}.cml";
                            var filePath = Path.Combine(backupFolder, fileName);
                            var find = "<?xml version=\"1.0\"?>";
                            var replace = "<?xml version=\"1.0\" encoding=\"utf-8\"?>" + Environment.NewLine;
                            var xml = customXmlPart.XML;
                            File.WriteAllText(filePath, xml.Replace(find, replace));
                            Globals.Chem4WordV3.Telemetry.Write(module, "Information, ", $"Saved Orphaned XmlPart Id:{customXmlPart.Id} Tag: {molId} from document [{document.DocID}] as {fileName}");
                        }
                        catch (Exception exception)
                        {
                            carryOutPurge = false;
                            RegistryHelper.StoreException(module, exception);
                        }
                    }
                }

                // Pass 3 purge orphans
                if (carryOutPurge)
                {
                    foreach (CustomXMLPart customXmlPart in allChemistryParts)
                    {
                        var molId = GetCmlId(customXmlPart);
                        if (!referencedXmlParts.ContainsKey(molId))
                        {
                            try
                            {
                                Globals.Chem4WordV3.Telemetry.Write(module, "Information, ", $"Purging Orphaned XmlPart Id:{customXmlPart.Id} Tag: {molId} from document [{document.DocID}]");
                                customXmlPart.Delete();
                                result++;
                            }
                            catch (Exception exception)
                            {
                                RegistryHelper.StoreException(module, exception);
                            }
                        }
                    }
                }
            }

            return result;
        }
    }
}