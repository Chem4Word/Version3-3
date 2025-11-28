// ---------------------------------------------------------------------------
//  Copyright (c) 2026, The .NET Foundation.
//  This software is released under the Apache Licence, Version 2.0.
//  The licence and further copyright text can be found in the file LICENCE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.Model2.Enums;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Chem4Word.Model2.Formula
{
    public class FormulaHelper
    {
        private readonly Molecule _molecule;
        private readonly string _conciseRaw = string.Empty;
        private readonly string _unicodeRaw = string.Empty;

        public FormulaHelper(Model model)
        {
            List<Molecule> molecules = model.Molecules.Values.ToList();

            if (molecules.Any())
            {
                ChildHelpers = new List<FormulaHelper>();

                foreach (Molecule molecule in molecules)
                {
                    FormulaHelper helper = new FormulaHelper(molecule);
                    ChildHelpers.Add(helper);
                }
            }
        }

        public FormulaHelper(Molecule molecule)
        {
            _molecule = molecule;

            List<Molecule> molecules = molecule.Molecules.Values.ToList();
            if (molecules.Any())
            {
                ChildHelpers = new List<FormulaHelper>();

                foreach (Molecule childMolecule in molecules)
                {
                    FormulaHelper helper = new FormulaHelper(childMolecule);
                    ChildHelpers.Add(helper);
                }
            }

            List<FormulaPart> parts = GenerateFormulaParts();

            _conciseRaw = CreateConciseFormula(parts);
            _unicodeRaw = CreateUnicodeFormula(parts);
        }

        private List<FormulaHelper> ChildHelpers { get; set; }

        public static string ToUnicode(string formula)
        {
            List<FormulaPart> parts = ParseFormulaIntoParts(formula);

            string result = parts.Count == 0
                ? formula
                : CreateUnicodeFormula(parts);

            return result;
        }

        public string Concise(bool compact = false)
        {
            return GetFormula(FormulaStyle.Concise, addBrackets: true, compact);
        }

        public override string ToString()
        {
            return $"{_molecule.Path} - '{_conciseRaw}'";
        }

        public string Unicode()
        {
            return GetFormula(FormulaStyle.Unicode, addBrackets: true, compact: false);
        }

        private static string CreateConciseFormula(List<FormulaPart> parts)
        {
            List<string> values = new List<string>();

            if (parts.Any())
            {
                List<FormulaPart> atomParts = parts
                                                .Where(p => p.PartType == FormulaPartType.Element)
                                                .ToList();
                foreach (FormulaPart part in atomParts)
                {
                    values.Add(part.Value);
                }

                List<FormulaPart> chargeParts = parts
                                                  .Where(p => p.PartType == FormulaPartType.Charge)
                                                  .ToList();
                foreach (FormulaPart part in chargeParts)
                {
                    values.Add(part.Value);
                }
            }

            return string.Join(" ", values);
        }

        private static string CreateUnicodeFormula(List<FormulaPart> parts)
        {
            string result = string.Empty;

            if (parts.Any())
            {
                foreach (FormulaPart part in parts)
                {
                    switch (part.PartType)
                    {
                        case FormulaPartType.Separator:
                        case FormulaPartType.Multiplier:
                            if (!string.IsNullOrEmpty(part.Text))
                            {
                                result += part.Text;
                            }
                            break;

                        case FormulaPartType.Element:
                            switch (part.Count)
                            {
                                case 1: // No Subscript
                                    if (!string.IsNullOrEmpty(part.Text))
                                    {
                                        result += part.Text;
                                    }
                                    break;

                                default: // With Subscript
                                    if (!string.IsNullOrEmpty(part.Text))
                                    {
                                        result += part.Text;
                                    }

                                    if (part.Count > 0)
                                    {
                                        result += string.Concat($"{part.Count}".Select(c => FormulaConstants.SubScriptNumbers[c - 48]));
                                    }
                                    break;
                            }
                            break;

                        case FormulaPartType.Charge:
                            int absCharge = Math.Abs(part.Count);
                            if (absCharge > 0)
                            {
                                if (absCharge > 1)
                                {
                                    result += string.Concat($"{absCharge}".Select(c => FormulaConstants.SuperScriptNumbers[c - 48]));
                                }

                                if (part.Count > 0)
                                {
                                    result += FormulaConstants.SuperScriptPlus;
                                }

                                if (part.Count < 0)
                                {
                                    result += FormulaConstants.SuperScriptMinus;
                                }
                            }
                            break;
                    }
                }
            }

            return result;
        }

        private static List<FormulaPart> ParseFormulaIntoParts(string input)
        {
            List<FormulaPart> allParts = new List<FormulaPart>();

            if (!string.IsNullOrEmpty(input))
            {
                List<string> elements = ModelGlobals.PeriodicTable.ValidElements.Split('|').ToList();

                // Add charge characters and special characters so we can detect them
                elements.AddRange(new[] { "+", "-", "[", "]", "." });

                // Sort elements by length descending this enables accurate detection of two character, then one character elements
                elements.Sort((b, a) => a.Length.CompareTo(b.Length));

                string[] chunks = SplitString(input);

                foreach (string chunk in chunks)
                {
                    List<FormulaPart> parsed = ParseString(elements, chunk);
                    allParts.AddRange(parsed);
                }
            }

            // Detect if we found any elements
            int c1 = allParts.Count(w => w.PartType == FormulaPartType.Element);

            // Return a List if at least one element was found
            return c1 > 0
                ? allParts
                : new List<FormulaPart>();
        }

        private static List<FormulaPart> ParseString(List<string> elements, string formula)
        {
            SortedDictionary<int, FormulaPart> parts = new SortedDictionary<int, FormulaPart>();

            #region Detect each type of element, charge or separator

            foreach (string element in elements)
            {
                if (formula.Contains(element))
                {
                    int idx = formula.IndexOf(element, StringComparison.InvariantCulture);

                    FormulaPartType type = FormulaPartType.Element;
                    switch (element)
                    {
                        case "+":
                        case "-":
                            type = FormulaPartType.Charge;
                            break;

                        case "[":
                        case ".":
                        case "]":
                            type = FormulaPartType.Separator;
                            break;
                    }

                    FormulaPart info = new FormulaPart(type, idx, element, 0);

                    // Convert dot to a Bullet
                    if (info.PartType == FormulaPartType.Separator && element.Equals("."))
                    {
                        info.Text = $" {FormulaConstants.BulletSeparator} ";
                    }

                    // Prevent insertion of element with fewer characters at same index
                    if (!parts.ContainsKey(idx))
                    {
                        parts.Add(idx, info);
                    }
                }
            }

            #endregion Detect each type of element, charge or separator

            // Convert SortedDictionary to a list to make it easier to process
            List<FormulaPart> list = parts.Values.ToList();

            // Handle Multiplier
            if (list.Count > 0 && list[0].Index > 0)
            {
                string multiplier = formula.Substring(0, list[0].Index);
                parts.Add(0, new FormulaPart(FormulaPartType.Multiplier, multiplier, 0));
            }

            #region Detect counts

            for (int i = 0; i < list.Count; i++)
            {
                int start = list[i].Index;

                // Extract chunk from first character of element to first character of next chunk or end of target
                string chunk;
                if (i < list.Count - 1)
                {
                    int length = list[i + 1].Index - start;
                    chunk = formula.Substring(start, length);
                }
                else
                {
                    chunk = formula.Substring(start);
                }

                string symbol = list[i].Text;

                if (list[i].PartType == FormulaPartType.Element
                    || list[i].PartType == FormulaPartType.Charge)
                {
                    // Remove its symbol from the chunk to leave behind the numeric portion
                    string digits = chunk.Replace(symbol, "");

                    if (string.IsNullOrEmpty(digits))
                    {
                        // Assume 1 if it's an empty string
                        list[i].Count = 1;
                    }
                    else
                    {
                        list[i].Count = int.TryParse(digits, out int number)
                            ? number
                            : 999999;
                    }

                    // If this is a negative charge invert the value
                    if (list[i].PartType == FormulaPartType.Charge
                        && symbol.Equals("-"))
                    {
                        list[i].Count = 0 - list[i].Count;
                    }
                }
            }

            #endregion Detect counts

            // Detect counts which mark invalid parsing
            int c1 = parts.Values.Count(c => c.Count == 999999);
            int c2 = parts.Values.Count(c => c.Count == -999999);

            // Return a List if everything is valid
            return c1 + c2 == 0
                ? parts.Values.ToList()
                : new List<FormulaPart>();
        }

        private static string[] SplitString(string value)
        {
            List<string> result = new List<string>();

            // Remove all spaces
            string temp = value.Replace(" ", "");

            // Replace any Bullet characters with dot
            temp = temp.Replace($"{FormulaConstants.BulletSeparator}", ".");

            string chunk = "";

            foreach (char character in temp)
            {
                if (character == FormulaConstants.BracketStart
                    || character == '.'
                    || character == FormulaConstants.BracketEnd)
                {
                    // Got a match
                    if (!string.IsNullOrEmpty(chunk))
                    {
                        result.Add(chunk);
                    }
                    result.Add(character.ToString());
                    chunk = "";
                }
                else
                {
                    chunk += character;
                }
            }

            // Add in what's left over
            if (!string.IsNullOrEmpty(chunk))
            {
                result.Add(chunk);
            }

            return result.ToArray();
        }

        private List<FormulaPart> GenerateFormulaParts()
        {
            List<FormulaPart> result = new List<FormulaPart>();

            FormulaPart cPart = new FormulaPart(FormulaPartType.Element, "C", 0);
            FormulaPart hPart = new FormulaPart(FormulaPartType.Element, "H", 0);
            FormulaPart chargePart = new FormulaPart(FormulaPartType.Charge, "", 0);

            Dictionary<string, FormulaPart> otherAtomParts = new Dictionary<string, FormulaPart>();

            foreach (Atom atom in _molecule.Atoms.Values)
            {
                switch (atom.Element)
                {
                    // Add this element
                    case Element e:
                        {
                            // Obtain sum of charge on all atoms as we go round the loop
                            if (atom.FormalCharge != null)
                            {
                                chargePart.Count += atom.FormalCharge.Value;
                            }

                            string symbol = e.Symbol;

                            switch (symbol)
                            {
                                case "C":
                                    cPart.Count++;
                                    break;

                                case "H":
                                    hPart.Count++;
                                    break;

                                default:
                                    if (otherAtomParts.ContainsKey(symbol))
                                    {
                                        otherAtomParts[symbol].Count++;
                                    }
                                    else
                                    {
                                        otherAtomParts.Add(symbol, new FormulaPart(FormulaPartType.Element, symbol, 1));
                                    }

                                    break;
                            }

                            int hCount = atom.ImplicitHydrogenCount;
                            if (hCount > 0)
                            {
                                hPart.Count += hCount;
                            }

                            break;
                        }

                    // Expand functional group
                    case FunctionalGroup fg:
                        {
                            Dictionary<string, int> parts = fg.FormulaParts;
                            foreach (KeyValuePair<string, int> part in parts)
                            {
                                switch (part.Key)
                                {
                                    case "C":
                                        cPart.Count += part.Value;
                                        break;

                                    case "H":
                                        hPart.Count += part.Value;
                                        break;

                                    case "R":
                                        // Ignore pseudo Element(s)
                                        break;

                                    default:
                                        if (otherAtomParts.ContainsKey(part.Key))
                                        {
                                            otherAtomParts[part.Key].Count += part.Value;
                                        }
                                        else
                                        {
                                            otherAtomParts.Add(part.Key, new FormulaPart(FormulaPartType.Element, part.Key, part.Value));
                                        }
                                        break;
                                }
                            }

                            break;
                        }
                }
            }

            // Now add the parts in the correct order (Hill Notation) C then H then the rest in alphabetical order then the charge
            if (cPart.Count > 0)
            {
                result.Add(cPart);
            }

            if (hPart.Count > 0)
            {
                result.Add(hPart);
            }

            if (otherAtomParts.Any())
            {
                result.AddRange(otherAtomParts.Values);
            }

            // Get charge for the molecule
            if (_molecule.FormalCharge != null)
            {
                // Add the molecule's charge to what's been calculated from the atoms
                chargePart.Count += _molecule.FormalCharge.Value;
            }

            if (chargePart.Count != 0)
            {
                result.Add(chargePart);
            }

            return result;
        }

        private string GetFormula(FormulaStyle type, bool addBrackets, bool compact)
        {
            List<string> result = new List<string>();

            if (ChildHelpers != null && ChildHelpers.Any())
            {
                int childHelpersCount = ChildHelpers.Count;
                bool isMolecule = _molecule != null;
                int childMolecules = isMolecule ? _molecule.Molecules.Count : 0;

                Dictionary<string, int> dictionary = new Dictionary<string, int>();

                for (int i = 0; i < childHelpersCount; i++)
                {
                    switch (type)
                    {
                        case FormulaStyle.Concise:
                            string concise = ChildHelpers[i].Concise(compact);
                            UpsertCount(dictionary, concise);
                            break;

                        case FormulaStyle.Unicode:
                            string unicode = ChildHelpers[i].Unicode();
                            UpsertCount(dictionary, unicode);
                            break;
                    }
                }

                if (addBrackets
                    && childMolecules > 1
                    && dictionary.Count > 1)
                {
                    result.Add($"{FormulaConstants.BracketStart}");
                }

                List<string> main = new List<string>();

                foreach (KeyValuePair<string, int> kvp in dictionary)
                {
                    if (kvp.Value == 1)
                    {
                        main.Add($"{kvp.Key}");
                    }
                    else
                    {
                        main.Add(compact ? $"{kvp.Value}{kvp.Key}" : $"{kvp.Value} {kvp.Key}");
                    }
                }

                result.Add(compact
                               ? string.Join($"{FormulaConstants.BulletSeparator}", main)
                               : string.Join($" {FormulaConstants.BulletSeparator} ", main));

                if (addBrackets
                    && childMolecules > 1
                    && dictionary.Count > 1)
                {
                    result.Add($"{FormulaConstants.BracketEnd}");
                }
            }

            switch (type)
            {
                case FormulaStyle.Concise:
                    if (!string.IsNullOrEmpty(_conciseRaw))
                    {
                        result.Add(compact ? _conciseRaw.Replace(" ", "") : _conciseRaw);
                    }
                    break;

                case FormulaStyle.Unicode:
                    if (!string.IsNullOrEmpty(_unicodeRaw))
                    {
                        result.Add(_unicodeRaw);
                    }
                    break;
            }

            return string.Join(compact ? "" : " ", result);

            void UpsertCount(Dictionary<string, int> dictionary, string formula)
            {
                if (dictionary.ContainsKey(formula))
                {
                    dictionary[formula]++;
                }
                else
                {
                    dictionary.Add(formula, 1);
                }
            }
        }
    }
}
