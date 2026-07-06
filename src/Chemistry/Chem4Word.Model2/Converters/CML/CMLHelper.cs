// ---------------------------------------------------------------------------
//  Copyright (c) 2026, The .NET Foundation.
//  This software is released under the Apache Licence, Version 2.0.
//  The licence and further copyright text can be found in the file LICENCE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.Core.Enums;
using Chem4Word.Core.Helpers;
using Chem4Word.Model2.Enums;
using Chem4Word.Model2.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Xml.Linq;

namespace Chem4Word.Model2.Converters.CML
{
    // ReSharper disable once InconsistentNaming
    public static class CMLHelper
    {
        public static int? GetIsotopeNumber(XElement cmlElement)
        {
            if (int.TryParse(cmlElement.Attribute(ModelConstants.AttributeIsotopeNumber)?.Value, out int isotopeNumber))
            {
                return isotopeNumber;
            }
            else
            {
                return null;
            }
        }

        internal static ElementBase GetElementOrFunctionalGroup(XElement cmlElement, out string message)
        {
            message = "";
            XAttribute xa = cmlElement.Attribute(ModelConstants.AttributeElementType);
            if (xa != null)
            {
                string symbol = xa.Value;
                AtomHelpers.TryParse(symbol, true, out ElementBase eb);

                if (eb is Element || eb is FunctionalGroup)
                {
                    return eb;
                }

                //if we got here then it went very wrong
                message = $"Unrecognised element '{symbol}' in {cmlElement}";
            }
            else
            {
                message = $"cml attribute 'elementType' missing from {cmlElement}";
            }

            return null;
        }

        internal static XElement GetExplicitCarbonTag(XElement doc)
        {
            IEnumerable<XElement> id1 = from XElement xe in doc.Elements(ModelConstants.TagExplicitC) select xe;
            IEnumerable<XElement> id2 = from XElement xe in doc.Elements(CMLNamespaces.c4w + ModelConstants.TagExplicitC) select xe;
            return id1.Union(id2).FirstOrDefault();
        }

        internal static XElement GetExplicitHydrogenTag(XElement doc)
        {
            IEnumerable<XElement> id1 = from XElement xe in doc.Elements(ModelConstants.TagExplicitH) select xe;
            IEnumerable<XElement> id2 = from XElement xe in doc.Elements(CMLNamespaces.c4w + ModelConstants.TagExplicitH) select xe;
            return id1.Union(id2).FirstOrDefault();
        }

        internal static XElement GetColouredAtomsTag(XElement doc)
        {
            IEnumerable<XElement> id1 = from XElement xe in doc.Elements(ModelConstants.TagShowColouredAtoms) select xe;
            IEnumerable<XElement> id2 = from XElement xe in doc.Elements(CMLNamespaces.c4w + ModelConstants.TagShowColouredAtoms) select xe;
            return id1.Union(id2).FirstOrDefault();
        }

        internal static XElement GetShowMoleculeGroupingTag(XElement doc)
        {
            IEnumerable<XElement> id1 = from XElement xe in doc.Elements(ModelConstants.TagShowMoleculeGrouping) select xe;
            IEnumerable<XElement> id2 = from XElement xe in doc.Elements(CMLNamespaces.c4w + ModelConstants.TagShowMoleculeGrouping) select xe;
            return id1.Union(id2).FirstOrDefault();
        }

        internal static XElement GetShowMolecularWeightTag(XElement doc)
        {
            IEnumerable<XElement> id1 = from XElement xe in doc.Elements(ModelConstants.TagShowMolecularWeight) select xe;
            IEnumerable<XElement> id2 = from XElement xe in doc.Elements(CMLNamespaces.c4w + ModelConstants.TagShowMolecularWeight) select xe;
            return id1.Union(id2).FirstOrDefault();
        }

        internal static XElement GetShowMoleculeCaptionsTag(XElement doc)
        {
            IEnumerable<XElement> id1 = from XElement xe in doc.Elements(ModelConstants.TagShowMoleculeCaptions) select xe;
            IEnumerable<XElement> id2 = from XElement xe in doc.Elements(CMLNamespaces.c4w + ModelConstants.TagShowMoleculeCaptions) select xe;
            return id1.Union(id2).FirstOrDefault();
        }

        internal static XElement GetCustomXmlPartGuid(XElement doc)
        {
            IEnumerable<XElement> id1 = from XElement xe in doc.Elements(ModelConstants.TagXmlPartGuid) select xe;
            IEnumerable<XElement> id2 = from XElement xe in doc.Elements(CMLNamespaces.c4w + ModelConstants.TagXmlPartGuid) select xe;
            return id1.Union(id2).FirstOrDefault();
        }

        // ReSharper disable once InconsistentNaming
        internal static List<XElement> GetMolecules(XElement doc)
        {
            List<XElement> result = new List<XElement>();

            // Task 736 - Handle ChemDraw 19.1 cml variant
            if (doc.Document.Root.Name.LocalName == ModelConstants.TagMolecule)
            {
                result.Add(doc);
            }
            else
            {
                IEnumerable<XElement> mols = from XElement xe in doc.Elements(ModelConstants.TagMolecule) select xe;
                IEnumerable<XElement> mols2 = from XElement xe2 in doc.Elements(CMLNamespaces.cml + ModelConstants.TagMolecule) select xe2;
                result = mols.Union(mols2).ToList();
            }

            return result;
        }

        internal static List<XElement> GetAtoms(XElement mol)
        {
            // Task 336
            IEnumerable<XElement> aa1 = from a in mol.Elements(ModelConstants.TagAtomArray) select a;
            IEnumerable<XElement> aa2 = from a in mol.Elements(CMLNamespaces.cml + ModelConstants.TagAtomArray) select a;
            IEnumerable<XElement> aa = aa1.Union(aa2);

            if (aa.Count() == 0)
            {
                // Bare Atoms without AtomArray
                IEnumerable<XElement> atoms1 = from a in mol.Elements(ModelConstants.TagAtom) select a;
                IEnumerable<XElement> atoms2 = from a in mol.Elements(CMLNamespaces.cml + ModelConstants.TagAtom) select a;
                return atoms1.Union(atoms2).ToList();
            }
            else
            {
                // Atoms inside AtomArray
                IEnumerable<XElement> atoms1 = from a in aa.Elements(ModelConstants.TagAtom) select a;
                IEnumerable<XElement> atoms2 = from a in aa.Elements(CMLNamespaces.cml + ModelConstants.TagAtom) select a;
                return atoms1.Union(atoms2).ToList();
            }
        }

        internal static List<XElement> GetBonds(XElement mol)
        {
            // Task 336
            IEnumerable<XElement> ba1 = from b in mol.Elements(ModelConstants.TagBondArray) select b;
            IEnumerable<XElement> ba2 = from b in mol.Elements(CMLNamespaces.cml + ModelConstants.TagBondArray) select b;
            IEnumerable<XElement> ba = ba1.Union(ba2);

            if (ba.Count() == 0)
            {
                // Bare bonds without BondArray
                IEnumerable<XElement> bonds1 = from b in mol.Elements(ModelConstants.TagBond) select b;
                IEnumerable<XElement> bonds2 = from b in mol.Elements(CMLNamespaces.cml + ModelConstants.TagBond) select b;
                return bonds1.Union(bonds2).ToList();
            }
            else
            {
                // Bonds inside BondArray
                IEnumerable<XElement> bonds1 = from b in ba.Elements(ModelConstants.TagBond) select b;
                IEnumerable<XElement> bonds2 = from b in ba.Elements(CMLNamespaces.cml + ModelConstants.TagBond) select b;
                return bonds1.Union(bonds2).ToList();
            }
        }

        internal static List<XElement> GetStereo(XElement bond)
        {
            IEnumerable<XElement> stereo = from s in bond.Elements(ModelConstants.TagBondStereo) select s;
            IEnumerable<XElement> stereo2 = from s in bond.Elements(CMLNamespaces.cml + ModelConstants.TagBondStereo) select s;
            return stereo.Union(stereo2).ToList();
        }

        internal static List<XElement> GetNames(XElement mol)
        {
            IEnumerable<XElement> names1 = from n1 in mol.Elements(ModelConstants.TagName) select n1;
            IEnumerable<XElement> names2 = from n2 in mol.Elements(CMLNamespaces.cml + ModelConstants.TagName) select n2;
            return names1.Union(names2).ToList();
        }

        internal static List<XElement> GetFormulas(XElement mol)
        {
            IEnumerable<XElement> formulae1 = from f1 in mol.Elements(ModelConstants.TagFormula) select f1;
            IEnumerable<XElement> formulae2 = from f2 in mol.Elements(CMLNamespaces.cml + ModelConstants.TagFormula) select f2;
            return formulae1.Union(formulae2).ToList();
        }

        internal static List<XElement> GetMoleculeLabels(XElement mol)
        {
            IEnumerable<XElement> labels1 = from f1 in mol.Elements(ModelConstants.TagLabel) select f1;
            IEnumerable<XElement> labels2 = from f2 in mol.Elements(CMLNamespaces.cml + ModelConstants.TagLabel) select f2;
            return labels1.Union(labels2).ToList();
        }

        internal static List<XElement> GetReactionSchemes(XElement doc)
        {
            List<XElement> result = new List<XElement>();

            IEnumerable<XElement> rschemes = from XElement xe in doc.Elements(ModelConstants.TagReactionScheme) select xe;
            IEnumerable<XElement> rschemes2 = from XElement xe2 in doc.Elements(CMLNamespaces.cml + ModelConstants.TagReactionScheme) select xe2;
            result = rschemes.Union(rschemes2).ToList();

            return result;
        }

        internal static List<XElement> GetAnnotations(XElement doc)
        {
            List<XElement> result = new List<XElement>();

            IEnumerable<XElement> ann = from XElement xe in doc.Elements(ModelConstants.TagAnnotation) select xe;
            IEnumerable<XElement> ann2 = from XElement xe2 in doc.Elements(CMLNamespaces.c4w + ModelConstants.TagAnnotation) select xe2;
            result = ann.Union(ann2).ToList();

            return result;
        }

        internal static List<XElement> GetElectronPushers(XElement doc)
        {
            List<XElement> result = new List<XElement>();

            IEnumerable<XElement> ann = from XElement xe in doc.Elements(ModelConstants.TagElectronPusher) select xe;
            IEnumerable<XElement> ann2 = from XElement xe2 in doc.Elements(CMLNamespaces.c4w + ModelConstants.TagElectronPusher) select xe2;
            result = ann.Union(ann2).ToList();

            return result;
        }

        internal static int? GetFormalCharge(XElement cmlElement)
        {
            if (int.TryParse(cmlElement.Attribute(ModelConstants.AttributeFormalCharge)?.Value, out int formalCharge))
            {
                return formalCharge;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Gets the cmlElement position from the CML
        /// </summary>
        /// <param name="cmlElement">XElement representing the cmlElement CML</param>
        /// <returns>Point containing the cmlElement coordinates</returns>
        internal static Point GetPosition(XElement cmlElement, out string message)
        {
            message = "";
            string symbol = cmlElement.Attribute(ModelConstants.AttributeElementType)?.Value;
            string id = cmlElement.Attribute(ModelConstants.AttributeId)?.Value;

            Point result = new Point();
            bool found = false;

            // Try first with 2D Co-ordinate scheme
            if (cmlElement.Attribute(ModelConstants.AttributeX2) != null
                && cmlElement.Attribute(ModelConstants.AttributeY2) != null)
            {
                result = new Point(
                    SafeDouble.Parse(cmlElement.Attribute(ModelConstants.AttributeX2).Value),
                    SafeDouble.Parse(cmlElement.Attribute(ModelConstants.AttributeY2).Value));
                found = true;
            }

            if (!found)
            {
                // Try again with 3D Co-ordinate scheme
                if (cmlElement.Attribute(ModelConstants.AttributeX3) != null
                    && cmlElement.Attribute(ModelConstants.AttributeY3) != null)
                {
                    result = new Point(
                        SafeDouble.Parse(cmlElement.Attribute(ModelConstants.AttributeX3).Value),
                        SafeDouble.Parse(cmlElement.Attribute(ModelConstants.AttributeY3).Value));
                    found = true;
                }
            }

            if (!found)
            {
                message = $"No co-ordinates found for object '{symbol}' with id of '{id}'.";
            }
            return result;
        }

        public static bool? GetExplicit(XElement cmlElement)
        {
            if (bool.TryParse(cmlElement.Attribute(CMLNamespaces.c4w + ModelConstants.AttributeExplicit)?.Value, out bool explicitC))
            {
                return explicitC;
            }

            if (bool.TryParse(cmlElement.Attribute(CMLNamespaces.c4w + ModelConstants.AttributeExplicitC)?.Value, out explicitC))
            {
                return explicitC;
            }

            return null;
        }

        public static HydrogenLabels? GetExplicitH(XElement cmlElement)
        {
            if (Enum.TryParse(cmlElement.Attribute(CMLNamespaces.c4w + ModelConstants.AttributeExplicitH)?.Value, out HydrogenLabels explicitH))
            {
                return explicitH;
            }

            return null;
        }

        public static CompassPoints? GetExplicitHPlacement(XElement cmlElement)
        {
            if (Enum.TryParse(cmlElement.Attribute(CMLNamespaces.c4w + ModelConstants.AttributeHydrogenPlacement)?.Value, out CompassPoints explicitHPlacement))
            {
                return explicitHPlacement;
            }

            return null;
        }

        public static CompassPoints? GetExplicitGroupPlacement(XElement cmlElement)
        {
            if (Enum.TryParse(cmlElement.Attribute(CMLNamespaces.c4w + ModelConstants.AttributeFunctionalGroupPlacement)?.Value, out CompassPoints explicitGroupPlacement))
            {
                return explicitGroupPlacement;
            }

            return null;
        }

        public static List<XElement> GetReactions(XElement doc)
        {
            List<XElement> result = new List<XElement>();
            IEnumerable<XElement> reacts = from XElement xe in doc.Elements(ModelConstants.TagReaction) select xe;
            IEnumerable<XElement> reacts2 = from XElement xe2 in doc.Elements(CMLNamespaces.cml + ModelConstants.TagReaction) select xe2;
            result = reacts.Union(reacts2).ToList();
            return result;
        }

        public static bool BondOrderIsValid(string order) =>
            "0|1|2|3|S|D|T|hbond|partial01|partial12|partial23|A|".Contains($"|{order}|");

        public static int GetElectronCount(XElement electronElement)
        {
            if (int.TryParse(electronElement.Attribute(CMLNamespaces.cml + ModelConstants.AttributeElectronCount)?.Value, out int electronCount))
            {
                return electronCount;
            }
            if (int.TryParse(electronElement.Attribute(ModelConstants.AttributeElectronCount)?.Value, out electronCount))
            {
                return electronCount;
            }

            return 0;
        }

        public static string GetId(XElement structElement)
        {
            string label = structElement.Attribute(ModelConstants.AttributeId)?.Value;
            return label;
        }

        public static CompassPoints? GetElectronPlacement(XElement electronElement)
        {
            if (Enum.TryParse(electronElement.Attribute(CMLNamespaces.c4w + ModelConstants.AttributeElectronPlacement)?.Value, out CompassPoints electronPlacement))
            {
                return electronPlacement;
            }

            if (Enum.TryParse(electronElement.Attribute(ModelConstants.AttributeElectronPlacement)?.Value, out electronPlacement))
            {
                return electronPlacement;
            }
            return null;
        }

        public static ElectronType GetElectronType(XElement electronElement)
        {
            if (Enum.TryParse(electronElement.Attribute(CMLNamespaces.c4w + ModelConstants.AttributeElectronType)?.Value, out ElectronType type))
            {
                return type;
            }
            if (Enum.TryParse(electronElement.Attribute(ModelConstants.AttributeElectronType)?.Value, out type))
            {
                return type;
            }

            return ElectronType.LonePair;
        }

        public static ElectronPusherType GetElectronPusherType(XElement cmlElement)
        {
            if (Enum.TryParse(cmlElement.Attribute(CMLNamespaces.c4w + ModelConstants.AttrElectronPusherType)?.Value, out ElectronPusherType type))
            {
                return type;
            }
            if (Enum.TryParse(cmlElement.Attribute(ModelConstants.AttrElectronPusherType)?.Value, out type))
            {
                return type;
            }

            return ElectronPusherType.CurlyArrow;
        }

        internal static XElement GetSubstanceName(XElement doc)
        {
            IEnumerable<XElement> id1 = from XElement xe in doc.Elements(ModelConstants.TagSubstanceName) select xe;
            IEnumerable<XElement> id2 = from XElement xe in doc.Elements(CMLNamespaces.c4w + ModelConstants.TagSubstanceName) select xe;
            return id1.Union(id2).FirstOrDefault();
        }

        internal static List<XElement> GetSubstanceTags(XElement doc)
        {
            List<XElement> result = new List<XElement>();

            IEnumerable<XElement> outer1 = from a in doc.Elements(ModelConstants.TagSubstanceTags) select a;
            IEnumerable<XElement> outer2 = from a in doc.Elements(CMLNamespaces.c4w + ModelConstants.TagSubstanceTags) select a;
            List<XElement> union = outer1.Union(outer2).ToList();

            if (union.Any())
            {
                IEnumerable<XElement> inner1 = from a in union.Elements(ModelConstants.TagSubstanceTag) select a;
                IEnumerable<XElement> inner2 = from a in union.Elements(CMLNamespaces.c4w + ModelConstants.TagSubstanceTag) select a;
                result = inner1.Union(inner2).ToList();
            }

            return result;
        }
    }
}
