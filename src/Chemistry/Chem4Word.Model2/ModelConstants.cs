// ---------------------------------------------------------------------------
//  Copyright (c) 2025, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

namespace Chem4Word.Model2
{
    public static class ModelConstants
    {
        // CML Constants
        public const string NSCML = "cml";

        public const string NSC4W = "c4w";

        public const string AttributeArrowHead = "head";
        public const string AttributeArrowTail = "tail";
        public const string AttributeAtomRefs2 = "atomRefs2";
        public const string AttributeAtomRefs4 = "atomRefs4";
        public const string AttributeConcise = "concise";
        public const string AttributeConvention = "convention";
        public const string AttributeCount = "count";
        public const string AttributeDictRef = "dictRef";
        public const string AttributeElementType = "elementType";
        public const string AttributeExplicit = "explicit";
        public const string AttributeExplicitC = "explicitC";
        public const string AttributeExplicitH = "explicitH";
        public const string AttributeFormalCharge = "formalCharge";
        public const string AttributeFunctionalGroupPlacement = "groupPlacement";
        public const string AttributeHydrogenPlacement = "hydrogenPlacement";
        public const string AttributeId = "id";
        public const string AttributeInline = "inline";
        public const string AttributeIsEditable = "isEditable";
        public const string AttributeIsotopeNumber = "isotopeNumber";
        public const string AttributeNameValue = "value";
        public const string AttributeOrder = "order";
        public const string AttributePlacement = "placement";
        public const string AttributeReactionBias = "reactionBias";
        public const string AttributeReactionType = "reactionType";
        public const string AttributeRef = "ref";
        public const string AttributeRole = "role";
        public const string AttributeShowMoleculeBrackets = "showBrackets";
        public const string AttributeSpinMultiplicity = "spinMultiplicity";
        public const string AttributeSymbolSize = "symbolSize";
        public const string AttributeText = "text";
        public const string AttributeTitle = "title";
        public const string AttributeX2 = "x2";
        public const string AttributeX3 = "x3";
        public const string AttributeY2 = "y2";
        public const string AttributeY3 = "y3";
        public const string AttributeZ3 = "z3";
        public const string AttrValueBiasForward = "forward";
        public const string AttrValueBiasReverse = "reverse";
        public const string AttrValueBlocked = "blocked";
        public const string AttrValueResonance = "resonance";
        public const string AttrValueRetrosynthetic = "retrosynthetic";
        public const string AttrValueReversible = "reversible";
        public const string AttrValueTheoretical = "theoretical";
        public const string TagAnnotation = "annotation";
        public const string TagAtom = "atom";
        public const string TagAtomArray = "atomArray";
        public const string TagBond = "bond";
        public const string TagBondArray = "bondArray";
        public const string TagBondStereo = "bondStereo";
        public const string TagCmlDict = "cmlDict";
        public const string TagConditionsText = "conditions";
        public const string TagConventionMolecular = "convention:molecular";
        public const string TagConventions = "conventions";
        public const string TagExplicitC = "explicitC";
        public const string TagExplicitH = "explicitH";
        public const string TagFormula = "formula";
        public const string TagLabel = "label";
        public const string TagMolecularWeight = "molecularWeight";
        public const string TagMolecule = "molecule";
        public const string TagName = "name";
        public const string TagNameDict = "nameDict";
        public const string TagProduct = "product";
        public const string TagProductList = "productList";
        public const string TagReactant = "reactant";
        public const string TagReactantList = "reactantList";
        public const string TagReaction = "reaction";
        public const string TagReactionScheme = "reactionScheme";
        public const string TagReagentText = "reagents";
        public const string TagShowColouredAtoms = "showColouredAtoms";
        public const string TagShowMolecularWeight = "showMolecularWeight";
        public const string TagShowMoleculeCaptions = "showMoleculeCaptions";
        public const string TagShowMoleculeGrouping = "showMoleculeGrouping";
        public const string TagSubstance = "substance";
        public const string TagSubstanceList = "substanceList";
        public const string TagXmlPartGuid = "customXmlPartGuid";
        public const string ValueChem4WordCaption = "chem4word:Caption";
        public const string ValueChem4WordFormula = "chem4word:Formula";
        public const string ValueChem4WordInchiKeyName = "chem4word:CalculatedInchikey";
        public const string ValueChem4WordInchiName = "chem4word:CalculatedInchi";
        public const string ValueChem4WordResolverFormulaName = "chem4word:ResolvedFormula";
        public const string ValueChem4WordResolverIupacName = "chem4word:ResolvedIupacname";
        public const string ValueChem4WordResolverSmilesName = "chem4word:ResolvedSmiles";
        public const string ValueChem4WordSynonym = "chem4word:Synonym";
        public const string ValueNameDictUnknown = "nameDict:unknown";

        //MDL related constants
        public const string M_CHG = "M  CHG"; // Represents the tag in the MDL Properties Block for Charge

        public const string M_ISO = "M  ISO"; // Represents the tag in the MDL Properties Block for an Isotopes list
        public const string M_RAD = "M  RAD"; // Represents the tag in the MDL Properties Block for an Radical list
        public const string M_END = "M  END"; // Represents the tag in the MDL Properties Block terminating the MolFile
        public const string SDF_END = "$$$$"; // Represents the end of the SD file block

        public const double BondOffsetPercentage = 0.1d;
        public const double DefaultFontSize = 20.0d;
        public const double FontSizePercentageBond = 0.5d;
        public const double ScaleFactorForXaml = 2.0d;
        public const string AllenicCarbonSymbol = "•";
        public const string EnDashSymbol = "\u2013";
        public const string FormatCML = "CML";
        public const string FormatSDFile = "SDFile";
        public const string OrderAromatic = "A";
        public const string OrderDouble = "D";
        public const string OrderOther = "other";
        public const string OrderPartial01 = "partial01";
        public const string OrderPartial12 = "partial12";
        public const string OrderPartial23 = "partial23";
        public const string OrderSingle = "S";
        public const string OrderTriple = "T";
        public const string OrderZero = "hbond";

        public const string MoleculePathSeparator = "/";
    }
}
