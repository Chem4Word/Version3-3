// ---------------------------------------------------------------------------
//  Copyright (c) 2024, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.Core.Helpers;
using Chem4Word.Model2.Converters.CML;
using Chem4Word.Model2.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;

namespace Chem4Word.Model2
{
    public class FunctionalGroups
    {
        private static List<FunctionalGroup> _shortcutList;

        private const string ElementNameRoot = "functionalGroups";
        private const string ElementNameFunctionalGroup = "functionalGroup";
        private const string ElementNameComment = "comment";
        private const string ElementNameName = "name";
        private const string ElementNameSymbol = "symbol";
        private const string ElementNameShowAsSymbol = "showAsSymbol";
        private const string ElementNameFlippable = "flippable";
        private const string ElementNameGroupType = "groupType";
        private const string ElementNameColour = "colour";
        private const string ElementNameComponents = "components";
        private const string ElementNameComponent = "component";
        private const string ElementNameGroup = "group";
        private const string ElementNameCount = "count";
        private const string ElementNameExpansion = "expansion";

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
                foreach (var groupElement in groupsDoc.Descendants().Where(x => x.Name.LocalName == ElementNameFunctionalGroup))
                {
                    // Copy the values one by one to prevent recursive issue found during unit tests

                    var newGroup = new FunctionalGroup
                    {
                        Comment = groupElement.Element(ElementNameComment)?.Value,
                        Name = groupElement.Element(ElementNameName)?.Value,
                        Symbol = groupElement.Element(ElementNameSymbol)?.Value,
                        Colour = groupElement.Element(ElementNameColour)?.Value,
                        ShowAsSymbol = bool.Parse((string)groupElement.Element(ElementNameShowAsSymbol) ?? "false"),
                        Flippable = bool.Parse((string)groupElement.Element(ElementNameFlippable) ?? "false"),
                        Expansion = groupElement.Element(ElementNameExpansion)?.Value,
                        Components = new List<Group>()
                    };

                    GroupType groupType;
                    if (Enum.TryParse((string)groupElement.Element(ElementNameGroupType) ?? "SuperAtom", out groupType))
                    {
                        newGroup.GroupType = groupType;
                    }

                    foreach (var compElement in groupElement.Descendants().Where(x => x.Name.LocalName.Equals(ElementNameComponent)))
                    {
                        var groupValue = compElement.Element(ElementNameGroup)?.Value;
                        var groupCount = compElement.Element(ElementNameCount)?.Value;
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

            var root = new XElement(ElementNameRoot);
            root.Add(new XAttribute(XNamespace.Xmlns + CMLConstants.TagCml, CMLNamespaces.cml));
            xDocument.Add(root);

            foreach (var functionalGroup in ShortcutList)
            {
                var functionalGroupElement = new XElement(ElementNameFunctionalGroup);

                if (!string.IsNullOrEmpty(functionalGroup.Comment))
                {
                    functionalGroupElement.Add(new XElement(ElementNameComment, functionalGroup.Comment));
                }

                functionalGroupElement.Add(new XElement(ElementNameName, functionalGroup.Name));
                functionalGroupElement.Add(new XElement(ElementNameSymbol, functionalGroup.Symbol));
                if (functionalGroup.ShowAsSymbol)
                {
                    functionalGroupElement.Add(new XElement(ElementNameShowAsSymbol, functionalGroup.ShowAsSymbol));
                }

                if (!string.IsNullOrEmpty(functionalGroup.Colour))
                {
                    functionalGroupElement.Add(new XElement(ElementNameColour, functionalGroup.Colour));
                }

                functionalGroupElement.Add(new XElement(ElementNameGroupType, functionalGroup.GroupType));

                if (functionalGroup.Flippable)
                {
                    functionalGroupElement.Add(new XElement(ElementNameFlippable, functionalGroup.Flippable));
                }

                var componentsElement = new XElement(ElementNameComponents);
                functionalGroupElement.Add(componentsElement);

                foreach (var component in functionalGroup.Components)
                {
                    var componentElement = new XElement(ElementNameComponent);
                    componentsElement.Add(componentElement);

                    componentElement.Add(new XElement(ElementNameGroup, component.Component));
                    componentElement.Add(new XElement(ElementNameCount, component.Count));
                }

                if (!string.IsNullOrEmpty(functionalGroup.Expansion))
                {
                    functionalGroupElement.Add(new XElement(ElementNameExpansion, functionalGroup.Expansion));
                }

                root.Add(functionalGroupElement);
            }

            return xDocument.ToString();
        }
    }
}