// ---------------------------------------------------------------------------
//  Copyright (c) 2025, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.Model2.Enums;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Chem4Word.Model2.Formula
{
    public class FormulaHelperV2
    {
        private readonly char[] _subScriptNumbers = {
                                  '\u2080', '\u2081', '\u2082', '\u2083', '\u2084',
                                  '\u2085', '\u2086', '\u2087', '\u2088', '\u2089'
                              };

        private readonly char[] _superScriptNumbers = {
                                    '\u2070', '\u00B9', '\u00B2', '\u00B3', '\u2074',
                                    '\u2075', '\u2076', '\u2077', '\u2078', '\u2079'
                                };

        private const string Separator = "·";
        private const string BracketStart = "[";
        private const string BracketEnd = "]";

        private List<FormulaPartV2> _parts = new List<FormulaPartV2>();

        private readonly string _conciseRaw = string.Empty;
        private readonly string _unicodeRaw = string.Empty;

        private readonly Molecule _molecule;

        public FormulaHelperV2(Model model)
        {
            List<Molecule> molecules = model.Molecules.Values.ToList();

            if (molecules.Any())
            {
                ChildHelpers = new List<FormulaHelperV2>();

                foreach (Molecule molecule in molecules)
                {
                    FormulaHelperV2 helper = new FormulaHelperV2(molecule);
                    ChildHelpers.Add(helper);
                }
            }
        }

        public FormulaHelperV2(Molecule molecule)
        {
            _molecule = molecule;

            List<Molecule> molecules = molecule.Molecules.Values.ToList();
            if (molecules.Any())
            {
                ChildHelpers = new List<FormulaHelperV2>();

                foreach (Molecule childMolecule in molecules)
                {
                    FormulaHelperV2 helper = new FormulaHelperV2(childMolecule);
                    ChildHelpers.Add(helper);
                }
            }

            GenerateFormulaPartsV2();

            _conciseRaw = CreateConciseFormula();
            _unicodeRaw = CreateUnicodeFormula();

            Debug.WriteLine($"{molecule.Path} = '{_conciseRaw}' '{_unicodeRaw}'");
        }

        private List<FormulaHelperV2> ChildHelpers { get; set; }

        public string Concise(bool compact = false)
        {
            return GetFormula(FormulaStyle.Concise, addBrackets: true, compact);
        }

        public string ConciseOfChildren(bool compact = false)
        {
            return GetFormula(FormulaStyle.Concise, addBrackets: false, compact);
        }

        public string Unicode()
        {
            return GetFormula(FormulaStyle.Unicode, addBrackets: true, compact: false);
        }

        // Replaces Molecule.CalculatedFormulaOfChildren() used in labels editor
        public string UnicodeOfChildren()
        {
            return GetFormula(FormulaStyle.Unicode, addBrackets: false, compact: false);
        }

        private string GetFormula(FormulaStyle type, bool addBrackets, bool compact)
        {
            List<string> result = new List<string>();

            if (ChildHelpers != null && ChildHelpers.Any())
            {
                int childHelpersCount = ChildHelpers.Count;
                int childMolecules = _molecule != null ? _molecule.Molecules.Count : 0;
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
                    result.Add(BracketStart);
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

                result.Add(compact ? string.Join(Separator, main) : string.Join($" {Separator} ", main));

                if (addBrackets
                    && childMolecules > 1
                    && dictionary.Count > 1)
                {
                    result.Add(BracketEnd);
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

        private string CreateConciseFormula()
        {
            List<string> parts = new List<string>();

            if (_parts.Any())
            {
                List<FormulaPartV2> atomParts = _parts
                                                .Where(p => p.PartType == FormulaPartType.Element)
                                                .ToList();
                foreach (FormulaPartV2 part in atomParts)
                {
                    parts.Add(part.Value);
                }

                List<FormulaPartV2> chargeParts = _parts
                                                  .Where(p => p.PartType == FormulaPartType.Charge)
                                                  .ToList();
                foreach (FormulaPartV2 part in chargeParts)
                {
                    parts.Add(part.Value);
                }
            }

            return string.Join(" ", parts);
        }

        private string CreateUnicodeFormula()
        {
            string result = string.Empty;

            foreach (FormulaPartV2 part in _parts)
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
                                    result += string.Concat($"{part.Count}".Select(c => _subScriptNumbers[c - 48]));
                                }
                                break;
                        }
                        break;

                    case FormulaPartType.Charge:
                        int absCharge = Math.Abs(part.SumOfCharges);
                        if (absCharge > 0)
                        {
                            if (absCharge > 1)
                            {
                                result += string.Concat($"{absCharge}".Select(c => _superScriptNumbers[c - 48]));
                            }

                            if (part.SumOfCharges > 0)
                            {
                                result += '\u207a';
                            }

                            if (part.SumOfCharges < 0)
                            {
                                result += '\u207b';
                            }
                        }
                        break;
                }
            }

            return result;
        }

        private void GenerateFormulaPartsV2()
        {
            FormulaPartV2 cPart = new FormulaPartV2(FormulaPartType.Element, "C", 0);
            FormulaPartV2 hPart = new FormulaPartV2(FormulaPartType.Element, "H", 0);
            FormulaPartV2 chargePart = new FormulaPartV2(FormulaPartType.Charge, "", 0);

            Dictionary<string, FormulaPartV2> otherAtomParts = new Dictionary<string, FormulaPartV2>();

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
                                chargePart.SumOfCharges += atom.FormalCharge.Value;
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
                                        otherAtomParts.Add(symbol, new FormulaPartV2(FormulaPartType.Element, symbol, 1));
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
                                            otherAtomParts.Add(part.Key, new FormulaPartV2(FormulaPartType.Element, part.Key, part.Value));
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
                _parts.Add(cPart);
            }

            if (hPart.Count > 0)
            {
                _parts.Add(hPart);
            }

            if (otherAtomParts.Any())
            {
                _parts.AddRange(otherAtomParts.Values);
            }

            // Get charge for the molecule
            if (_molecule.FormalCharge != null)
            {
                // Add the molecule's charge to what's been calculated from the atoms
                chargePart.SumOfCharges += _molecule.FormalCharge.Value;
            }

            if (chargePart.SumOfCharges != 0)
            {
                _parts.Add(chargePart);
            }
        }

        public override string ToString()
        {
            return $"{_molecule.Path} - '{_conciseRaw}' '{_unicodeRaw}'";
        }

        public static List<FormulaPartV2> ParseFormulaIntoParts(string formula)
        {
            //ToDo: Implement this
            return new List<FormulaPartV2>();
        }
    }
}
