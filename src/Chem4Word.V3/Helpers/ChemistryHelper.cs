// ---------------------------------------------------------------------------
//  Copyright (c) 2026, The .NET Foundation.
//  This software is released under the Apache Licence, Version 2.0.
//  The licence and further copyright text can be found in the file LICENCE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.Core;
using Chem4Word.Core.Helpers;
using Chem4Word.Model2;
using Chem4Word.Model2.Converters.CML;
using Microsoft.Office.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Word = Microsoft.Office.Interop.Word;

namespace Chem4Word.Helpers
{
    public static class ChemistryHelper
    {
        private static readonly string Product = Assembly.GetExecutingAssembly().FullName.Split(',')[0];
        private static readonly string Class = MethodBase.GetCurrentMethod()?.DeclaringType?.Name;

        private static object _missing = Type.Missing;

        public static Word.ContentControl Insert2DChemistry(Word.Document document, string cml, bool isCopy)
        {
            var module = $"{Product}.{Class}.{MethodBase.GetCurrentMethod()?.Name}()";

            if (Globals.Chem4WordV3.SystemOptions == null)
            {
                Globals.Chem4WordV3.LoadOptions();
            }

            // Calling routine should check that Globals.Chem4WordV3.ChemistryAllowed = true

            Word.ContentControl cc = null;
            var app = document.Application;

            var wordSettings = new WordSettings(app);

            if (Globals.Chem4WordV3.SystemOptions != null)
            {
                var renderer =
                    Globals.Chem4WordV3.GetRendererPlugIn(
                        Globals.Chem4WordV3.SystemOptions.SelectedRendererPlugIn);

                if (renderer != null)
                {
                    try
                    {
                        app.ScreenUpdating = false;
                        Globals.Chem4WordV3.DisableContentControlEvents();

                        var converter = new CMLConverter();
                        var model = converter.Import(cml);
                        var modified = false;

                        if (isCopy)
                        {
                            // Always generate new Guid on Import
                            model.CustomXmlPartGuid = Guid.NewGuid().ToString("N");
                            modified = true;
                        }

                        if (modified)
                        {
                            // Re-export as the CustomXmlPartGuid or Bond Length has been changed
                            cml = converter.Export(model);
                        }

                        var guid = model.CustomXmlPartGuid;

                        renderer.Properties = new Dictionary<string, string> { { "Guid", guid } };
                        renderer.Cml = cml;

                        // Generate temp file which can be inserted into a content control
                        var tempFileName = renderer.Render();
                        if (File.Exists(tempFileName))
                        {
                            cc = document.ContentControls.Add(Word.WdContentControlType.wdContentControlRichText, ref _missing);
                            Insert2D(document, cc.ID, tempFileName, guid);

                            Globals.Chem4WordV3.Telemetry.Write(module, "Information", $"ContentControl inserted at position {cc.Range.Start}");

                            if (isCopy)
                            {
                                document.CustomXMLParts.Add(XmlHelper.AddHeader(cml));
                            }

                            try
                            {
                                // Delete the temporary file now we are finished with it
#if DEBUG
#else
                                File.Delete(tempFileName);
#endif
                            }
                            catch
                            {
                                // Not much we can do here
                            }

#if DEBUG
                            var listChemistryControls = CustomXmlPartHelper.ListChemistryControls(document);
                            Globals.Chem4WordV3.Telemetry.Write(module, "Information", string.Join(Environment.NewLine, listChemistryControls));
#endif
                        }
                    }
                    catch (Exception ex)
                    {
                        Globals.Chem4WordV3.Telemetry.Write(module, "Exception", ex.Message);
                        Globals.Chem4WordV3.Telemetry.Write(module, "Exception", ex.StackTrace);
                    }
                    finally
                    {
                        app.ScreenUpdating = true;
                        Globals.Chem4WordV3.EnableContentControlEvents();
                    }
                }
            }

            wordSettings.RestoreSettings(app);

            return cc;
        }

        public static Word.ContentControl Insert1DChemistry(Word.Document document, string text, bool isFormula, string tag)
        {
            var module = $"{Product}.{Class}.{MethodBase.GetCurrentMethod()?.Name}()";

            if (Globals.Chem4WordV3.SystemOptions == null)
            {
                Globals.Chem4WordV3.LoadOptions();
            }

            var app = Globals.Chem4WordV3.Application;

            var wordSettings = new WordSettings(app);

            var cc = document.ContentControls.Add(Word.WdContentControlType.wdContentControlRichText, ref _missing);

            SetRichText(document, cc.ID, text);

            Globals.Chem4WordV3.Telemetry.Write(module, "Information", $"ContentControl inserted at position {cc.Range.Start}");

            wordSettings.RestoreSettings(app);

            cc.Tag = tag;
            cc.Title = CoreConstants.ContentControlTitle;
            cc.LockContents = true;

            return cc;
        }

        public static void RefreshAllStructures(Word.Document document)
        {
            var module = $"{Product}.{Class}.{MethodBase.GetCurrentMethod()?.Name}()";

            if (Globals.Chem4WordV3.SystemOptions == null)
            {
                Globals.Chem4WordV3.LoadOptions();
            }

            var renderer =
                Globals.Chem4WordV3.GetRendererPlugIn(
                    Globals.Chem4WordV3.SystemOptions.SelectedRendererPlugIn);

            if (renderer != null)
            {
                foreach (CustomXMLPart xmlPart in document.CustomXMLParts)
                {
                    var cml = xmlPart.XML;

                    var cxmlId = CustomXmlPartHelper.GetCmlId(xmlPart);
                    var cc = new CMLConverter();
                    var model = cc.Import(cml);

                    renderer.Properties = new Dictionary<string, string> { { "Guid", cxmlId } };
                    renderer.Cml = cml;

                    var tempFileName = renderer.Render();
                    if (File.Exists(tempFileName))
                    {
                        UpdateThisStructure(document, model, cxmlId, tempFileName);
                    }
                }
            }
        }

        public static void UpdateThisStructure(Word.Document document, Model model, string cxmlId, string tempFilename)
        {
            var module = $"{Product}.{Class}.{MethodBase.GetCurrentMethod()?.Name}()";

            // Use LINQ to get a list of all our ContentControls
            // Using $"{}" to coerce null to empty string
            var targets = (from Word.ContentControl ccs in document.ContentControls
                           orderby ccs.Range.Start
                           where $"{ccs.Title}" == CoreConstants.ContentControlTitle
                                 && $"{ccs.Tag}".Contains(cxmlId)
                           select new KeyValuePair<string, string>(ccs.ID, ccs.Tag)).ToList();

            foreach (var target in targets)
            {
                var ccTag = target.Value;
                var prefix = CustomXmlPartHelper.PrefixFromTag(target.Value);

                if (ccTag != null && ccTag.Equals(cxmlId))
                {
                    // Only 2D Structures if filename supplied
                    if (!string.IsNullOrEmpty(tempFilename))
                    {
                        Update2D(document, target.Key, tempFilename, cxmlId);
                    }
                }
                else
                {
                    switch (prefix)
                    {
                        // 1D Structures
                        case "c0":
                            Update1D(document, target.Key, model.ConciseFormula, $"{prefix}:{cxmlId}");
                            break;

                        case "w0":
                            Update1D(document, target.Key, SafeDouble.AsCMLString(model.MolecularWeight), $"{prefix}:{cxmlId}");
                            break;

                        default:
                            {
                                var text = GetInlineText(model, prefix, out _);
                                Update1D(document, target.Key, text, $"{prefix}:{cxmlId}");
                                break;
                            }
                    }
                }
            }
        }

        public static void Insert2D(Word.Document document, string ccId, string tempfileName, string guid)
        {
            var module = $"{Product}.{Class}.{MethodBase.GetCurrentMethod()?.Name}()";

            Globals.Chem4WordV3.Telemetry.Write(module, "Information", $"Inserting 2D structure in ContentControl {ccId} Tag {guid} in document [{document.DocID}]");

            var application = Globals.Chem4WordV3.Application;

            var wordSettings = new WordSettings(application);

            var contentControl = GetContentControl(document, ccId);

            var bookmarkName = CoreConstants.OoXmlBookmarkPrefix + guid;

            contentControl.Range.InsertFile(tempfileName, bookmarkName);
            if (document.Bookmarks.Exists(bookmarkName))
            {
                document.Bookmarks[bookmarkName].Delete();
            }

            wordSettings.RestoreSettings(application);

            contentControl.Tag = guid;
            contentControl.Title = CoreConstants.ContentControlTitle;
            contentControl.LockContents = true;
        }

        private static void Update2D(Word.Document document, string ccId, string tempfileName, string guid)
        {
            var module = $"{Product}.{Class}.{MethodBase.GetCurrentMethod()?.Name}()";

            Globals.Chem4WordV3.Telemetry.Write(module, "Information", $"Updating 2D structure in ContentControl {ccId} Tag {guid} in document [{document.DocID}]");

            var cc = GetContentControl(document, ccId);
            if (cc != null)
            {
                var app = Globals.Chem4WordV3.Application;
                var wordSettings = new WordSettings(app);

                cc.LockContents = false;
                if (cc.Type == Word.WdContentControlType.wdContentControlPicture)
                {
                    // Handle old Word 2007 style
                    var range = cc.Range;
                    cc.Delete();
                    cc = document.ContentControls.Add(Word.WdContentControlType.wdContentControlRichText, range);
                    cc.Tag = guid;
                    cc.Title = CoreConstants.ContentControlTitle;
                    cc.Range.Delete();
                }
                else
                {
                    cc.Range.Delete();
                }

                var bookmarkName = CoreConstants.OoXmlBookmarkPrefix + guid;
                cc.Range.InsertFile(tempfileName, bookmarkName);
                if (document.Bookmarks.Exists(bookmarkName))
                {
                    document.Bookmarks[bookmarkName].Delete();
                }

                wordSettings.RestoreSettings(app);

                cc.Tag = guid;
                cc.Title = CoreConstants.ContentControlTitle;
                cc.LockContents = true;
                Globals.Chem4WordV3.Telemetry.Write(module, "Information", $"ContentControl updated at position {cc.Range.Start}");
            }
            else
            {
                Globals.Chem4WordV3.Telemetry.Write(module, "Warning", $"Unable to find ContentControl with Id of {ccId}");
            }
        }

        public static void Insert1D(Word.Document document, string ccId, string text, string tag)
        {
            var module = $"{Product}.{Class}.{MethodBase.GetCurrentMethod()?.Name}()";

            Globals.Chem4WordV3.Telemetry.Write(module, "Information", $"Inserting 1D label in ContentControl {ccId} Tag {tag} in document [{document.DocID}]");

            var cc = GetContentControl(document, ccId);
            if (cc != null)
            {
                var app = Globals.Chem4WordV3.Application;
                var wordSettings = new WordSettings(app);

                SetRichText(document, cc.ID, text);

                Globals.Chem4WordV3.Telemetry.Write(module, "Information", $"ContentControl updated at position {cc.Range.Start}");

                wordSettings.RestoreSettings(app);

                cc.Tag = tag;
                cc.Title = CoreConstants.ContentControlTitle;
                cc.LockContents = true;
            }
        }

        private static void Update1D(Word.Document document, string ccId, string text, string tag)
        {
            var module = $"{Product}.{Class}.{MethodBase.GetCurrentMethod()?.Name}()";

            Globals.Chem4WordV3.Telemetry.Write(module, "Information", $"Updating 1D label in ContentControl {ccId} Tag {tag} in document [{document.DocID}]");

            var cc = GetContentControl(document, ccId);
            if (cc != null)
            {
                var app = Globals.Chem4WordV3.Application;
                var wordSettings = new WordSettings(app);

                cc.LockContents = false;
                cc.Range.Delete();

                SetRichText(document, cc.ID, text);

                Globals.Chem4WordV3.Telemetry.Write(module, "Information", $"ContentControl updated at position {cc.Range.Start}");

                wordSettings.RestoreSettings(app);

                cc.Tag = tag;
                cc.Title = CoreConstants.ContentControlTitle;
                cc.LockContents = true;
            }
        }

        public static string GetInlineText(Model model, string prefix, out string source)
        {
            var module = $"{Product}.{Class}.{MethodBase.GetCurrentMethod()?.Name}()";

            source = null;
            string text;

            var tp = model.GetTextPropertyById(prefix);
            if (tp != null)
            {
                text = tp.Value;
                if (tp.Id.EndsWith("f0"))
                {
                    source = "UnicodeFormula";
                }
                else if (tp.Id.EndsWith("w0"))
                {
                    source = "MolecularWeight";
                }
                else
                {
                    source = tp.FullType;
                }
            }
            else
            {
                text = $"Unable to find formula or name with id of '{prefix}'";
            }

            return text;
        }

        public static Word.ContentControl GetContentControl(Word.Document document, string id)
        {
            Word.ContentControl result = null;

            foreach (Word.ContentControl contentControl in document.ContentControls)
            {
                if (contentControl.ID.Equals(id))
                {
                    result = contentControl;
                    break;
                }
            }

            return result;
        }

        private static void SetRichText(Word.Document document, string ccId, string text)
        {
            var module = $"{Product}.{Class}.{MethodBase.GetCurrentMethod()?.Name}()";

            var cc = GetContentControl(document, ccId);
            if (cc != null)
            {
                cc.Range.Text = text;
            }
        }

        public static List<string> GetUsed1D(Word.Document document, string guidString)
        {
            var module = $"{Product}.{Class}.{MethodBase.GetCurrentMethod()?.Name}()";

            // Using $"{}" to coerce null to empty string
            var targets = (from Word.ContentControl ccs in document.ContentControls
                           orderby ccs.Range.Start
                           where $"{ccs.Title}" == CoreConstants.ContentControlTitle
                                 && $"{ccs.Tag}".Contains(guidString)
                                 && !$"{ccs.Tag}".Equals(guidString)
                           select ccs.Tag).Distinct().ToList();

            return targets;
        }

        public static List<string> GetUsed2D(Word.Document document, string guidString)
        {
            var module = $"{Product}.{Class}.{MethodBase.GetCurrentMethod()?.Name}()";

            // Using $"{}" to coerce null to empty string
            var targets = (from Word.ContentControl ccs in document.ContentControls
                           orderby ccs.Range.Start
                           where $"{ccs.Title}" == CoreConstants.ContentControlTitle
                                 && $"{ccs.Tag}".Equals(guidString)
                           select ccs.Tag).Distinct().ToList();

            return targets;
        }
    }
}
