// ---------------------------------------------------------------------------
//  Copyright (c) 2023, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.Core.Helpers;
using Chem4Word.Model2.Converters.CML;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;

namespace Chem4Word.Model2
{
    public class FunctionalGroups
    {
        private static List<FunctionalGroup> _shortcutList;

        /// <summary>
        /// ShortcutList represent text as a user might type in a superatom,
        /// actual values control how they are rendered
        /// </summary>
        public static List<FunctionalGroup> ShortcutList
        {
            get
            {
                if (_shortcutList == null)
                {
                    LoadFromResource();
                }
                return _shortcutList;
            }
        }

        private static void LoadFromResource()
        {
            _shortcutList = new List<FunctionalGroup>();

            var xml = ResourceHelper.GetStringResource(Assembly.GetExecutingAssembly(), "FunctionalGroups.xml");
            if (!string.IsNullOrEmpty(xml))
            {
                var groupsDoc = XDocument.Parse(xml);
                foreach (var groupElement in groupsDoc.Descendants().Where(x => x.Name.LocalName == "functionalGroup"))
                {
                    // Copy the values one by one to prevent recursive issue found during unit tests

                    var newGroup = new FunctionalGroup
                    {
                        Comment = groupElement.Element("comment")?.Value,
                        Name = groupElement.Element("name")?.Value,
                        Symbol = groupElement.Element("symbol")?.Value,
                        Colour = groupElement.Element("colour")?.Value,
                        ShowAsSymbol = bool.Parse((string)groupElement.Element("showAsSymbol") ?? "false"),
                        IsSuperAtom = bool.Parse((string)groupElement.Element("isSuperAtom") ?? "false"),
                        Flippable = bool.Parse((string)groupElement.Element("flippable") ?? "false"),
                        Internal = bool.Parse((string)groupElement.Element("internal") ?? "false"),
                        Expansion = groupElement.Element("expansion")?.Value,
                        Components = new List<Group>()
                    };

                    foreach (var compElement in groupElement.Descendants().Where(x => x.Name.LocalName == "component"))
                    {
                        var groupValue = compElement.Element("group")?.Value;
                        var groupCount = compElement.Element("count")?.Value;
                        if (!string.IsNullOrEmpty(groupValue) && !string.IsNullOrEmpty(groupCount))
                        {
                            newGroup.Components.Add(new Group(groupValue, int.Parse(groupCount)));
                        }
                    }

                    _shortcutList.Add(newGroup);
                }
            }
        }

        public static string ExportAsXml()
        {
            var xDocument = new XDocument();

            var root = new XElement("functionalGroups");
            root.Add(new XAttribute(XNamespace.Xmlns + CMLConstants.TagCml, CMLNamespaces.cml));
            xDocument.Add(root);

            foreach (var functionalGroup in ShortcutList)
            {
                var functionalGroupElement = new XElement("functionalGroup");

                if (!string.IsNullOrEmpty(functionalGroup.Comment))
                {
                    functionalGroupElement.Add(new XElement("comment", functionalGroup.Comment));
                }

                functionalGroupElement.Add(new XElement("name", functionalGroup.Name));
                functionalGroupElement.Add(new XElement("symbol", functionalGroup.Symbol));

                if (functionalGroup.ShowAsSymbol)
                {
                    functionalGroupElement.Add(new XElement("showAsSymbol", functionalGroup.ShowAsSymbol));
                }
                if (!string.IsNullOrEmpty(functionalGroup.Colour))
                {
                    functionalGroupElement.Add(new XElement("colour", functionalGroup.Colour));
                }
                if (functionalGroup.Internal)
                {
                    functionalGroupElement.Add(new XElement("internal", functionalGroup.Internal));
                }
                if (functionalGroup.Flippable)
                {
                    functionalGroupElement.Add(new XElement("flippable", functionalGroup.Flippable));
                }
                if (functionalGroup.IsSuperAtom)
                {
                    functionalGroupElement.Add(new XElement("canBeExpanded", functionalGroup.IsSuperAtom));
                }

                var componentsElement = new XElement("components");
                functionalGroupElement.Add(componentsElement);

                foreach (var component in functionalGroup.Components)
                {
                    var componentElement = new XElement("component");
                    componentsElement.Add(componentElement);

                    componentElement.Add(new XElement("group", component.Component));
                    componentElement.Add(new XElement("count", component.Count));
                }

                if (!string.IsNullOrEmpty(functionalGroup.Expansion))
                {
                    functionalGroupElement.Add(new XElement("expansion", functionalGroup.Expansion));
                }

                root.Add(functionalGroupElement);
            }

            return xDocument.ToString();
        }
    }
}