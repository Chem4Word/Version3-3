﻿// ---------------------------------------------------------------------------
//  Copyright (c) 2025, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.Model2.Enums;
using Chem4Word.Model2.Helpers;
using System.Collections.Generic;
using System.Diagnostics;

namespace Chem4Word.Model2
{
    public class FunctionalGroup : ElementBase
    {
        private double? _atomicWeight;
        private Dictionary<string, int> _formulaParts;

        public string Comment { get; set; }

        public override string Colour { get; set; }

        public override string Name { get; set; }

        /// <summary>
        /// Determines whether the functional group can be flipped about the pivot
        /// </summary>
        public bool Flippable { get; set; }

        /// <summary>
        /// Symbol to be rendered i.e. 'Ph', 'Bz' 'R{5}' etc
        /// Any text between '{' and '}' characters should be super scripted
        /// Symbol can also be of the form CH3, CF3, C2H5 etc
        /// </summary>
        public override string Symbol { get; set; }

        /// <summary>
        /// True: FunctionalGroup.Symbol should be used as is.
        /// False: FunctionalGroup should be expanded before rendering it.
        /// </summary>
        public bool ShowAsSymbol { get; set; }

        public GroupType GroupType { get; set; } = GroupType.SuperAtom;

        public bool ShowInDropDown => GroupType != GroupType.Legacy
                                      && GroupType != GroupType.Internal
                                      && GroupType != GroupType.Unknown;

        /// <summary>
        /// Defines the constituents of the "SuperAtom"
        /// The 'pivot' atom that bonds to the fragment appears FIRST in the list
        /// so CH3 can appear as H3C
        /// Ths property can be null, which means that the symbol gets rendered
        /// </summary>
        public List<Group> Components { get; set; }

        /// <summary>
        /// CML describing the "SuperAtom"
        /// </summary>
        public string Expansion { get; set; }

        /// <summary>
        /// Overall Atomic Weight of the Functional Group
        /// </summary>
        public override double AtomicWeight
        {
            get
            {
                if (_atomicWeight == null)
                {
                    var atomicWeight = 0.0d;
                    if (Components != null)
                    {
                        //add up the atoms' atomic weights times their multiplicity
                        foreach (var component in Components)
                        {
                            atomicWeight += component.AtomicWeight * component.Count;
                        }
                    }
                    _atomicWeight = atomicWeight;
                }

                return _atomicWeight.Value;
            }
            set => _atomicWeight = value;
        }

        /// <summary>
        /// List of atoms with frequency of use
        /// </summary>
        public Dictionary<string, int> FormulaParts
        {
            get
            {
                if (_formulaParts == null)
                {
                    _formulaParts = new Dictionary<string, int>();

                    foreach (var component in Components)
                    {
                        var pp = component.FormulaParts;
                        foreach (var p in pp)
                        {
                            if (_formulaParts.ContainsKey(p.Key))
                            {
                                _formulaParts[p.Key] += p.Value * component.Count;
                            }
                            else
                            {
                                _formulaParts.Add(p.Key, p.Value * component.Count);
                            }
                        }
                    }
                }

                return _formulaParts;
            }
        }

        /// <summary>
        /// Expand the Functional Group into a flattened list of terms
        /// </summary>
        /// <param name="reverse"></param>
        /// <param name="consolidate"></param>
        /// <returns></returns>
        public List<FunctionalGroupTerm> ExpandIntoTerms(bool reverse = false, bool consolidate = true)
        {
            var result = new List<FunctionalGroupTerm>();

            if (ShowAsSymbol)
            {
                var term = new FunctionalGroupTerm
                {
                    IsAnchor = true,
                    Parts = ExpandSymbol(Symbol)
                };

                result.Add(term);
            }
            else
            {
                if (Components == null)
                {
                    // This is an Error; Find out why the Components are null ?
                    Debugger.Break();
                }

                if (Components != null)
                {
                    for (var i = 0; i < Components.Count; i++)
                    {
                        var term = new FunctionalGroupTerm
                        {
                            IsAnchor = i == 0,
                            Parts = ExpandGroupV2(Components[i])
                        };

                        result.Add(term);
                    }
                }
            }

            // Consolidate parts of each term
            if (consolidate)
            {
                foreach (var term in result)
                {
                    if (term.Parts.Count > 1)
                    {
                        var newParts = new List<FunctionalGroupPart>();

                        var newPart = term.Parts[0];
                        newParts.Add(newPart);

                        for (var i = 1; i < term.Parts.Count; i++)
                        {
                            if (term.Parts[i].Type == term.Parts[i - 1].Type)
                            {
                                newPart.Text += term.Parts[i].Text;
                            }
                            else
                            {
                                newPart = term.Parts[i];
                                newParts.Add(newPart);
                            }
                        }

                        term.Parts = newParts;
                    }
                }
            }

            if (Flippable && reverse)
            {
                result.Reverse();
            }

            return result;

            // Local Functions

            // Ensure that Symbols such as "R{1}" and "{i}Pr" are expanded into parts
            List<FunctionalGroupPart> ExpandSymbol(string symbol)
            {
                var expanded = new List<FunctionalGroupPart>();

                if (symbol.Contains("{") || symbol.Contains("}"))
                {
                    var part = new FunctionalGroupPart();
                    foreach (var c in symbol)
                    {
                        switch (c)
                        {
                            case '{':
                                if (!string.IsNullOrEmpty(part.Text))
                                {
                                    expanded.Add(part);
                                }

                                part = new FunctionalGroupPart
                                {
                                    Type = FunctionalGroupPartType.Superscript
                                };
                                break;

                            case '}':
                                expanded.Add(part);
                                part = new FunctionalGroupPart();
                                break;

                            default:
                                part.Text += c;
                                break;
                        }
                    }

                    // Ensure that trailing characters are not lost
                    if (!string.IsNullOrEmpty(part.Text))
                    {
                        expanded.Add(part);
                    }
                }
                else
                {
                    expanded.Add(new FunctionalGroupPart
                    {
                        Text = symbol
                    });
                }

                return expanded;
            }

            List<FunctionalGroupPart> ExpandGroupV2(Group componentGroup, bool flipped = false)
            {
                var expanded = new List<FunctionalGroupPart>();

                ElementBase elementBase;
                if (AtomHelpers.TryParse(componentGroup.Component, false, out elementBase))
                {
                    if (elementBase is Element element)
                    {
                        expanded.Add(new FunctionalGroupPart
                        {
                            Text = element.Symbol
                        });

                        if (componentGroup.Count != 1)
                        {
                            var part = new FunctionalGroupPart
                            {
                                Type = FunctionalGroupPartType.Subscript,
                                Text = $"{componentGroup.Count}"
                            };
                            expanded.Add(part);
                        }
                    }

                    if (elementBase is FunctionalGroup functionalGroup)
                    {
                        var part = new FunctionalGroupPart();

                        if (componentGroup.Count != 1)
                        {
                            part.Text += "(";
                            expanded.Add(part);
                            part = new FunctionalGroupPart();
                        }

                        if (functionalGroup.ShowAsSymbol)
                        {
                            if (!string.IsNullOrEmpty(part.Text))
                            {
                                expanded.Add(part);
                            }

                            expanded.AddRange(ExpandSymbol(functionalGroup.Symbol));
                            part = new FunctionalGroupPart();
                        }
                        else
                        {
                            if (functionalGroup.Flippable && flipped)
                            {
                                for (var ii = functionalGroup.Components.Count - 1; ii >= 0; ii--)
                                {
                                    expanded.AddRange(ExpandGroupV2(functionalGroup.Components[ii]));
                                }
                            }
                            else
                            {
                                foreach (var fgc in functionalGroup.Components)
                                {
                                    expanded.AddRange(ExpandGroupV2(fgc));
                                }
                            }
                        }

                        if (componentGroup.Count != 1)
                        {
                            part.Text += ")";
                            expanded.Add(part);

                            part = new FunctionalGroupPart
                            {
                                Type = FunctionalGroupPartType.Subscript,
                                Text = $"{componentGroup.Count}"
                            };
                            expanded.Add(part);

                            part = new FunctionalGroupPart();
                        }

                        // Ensure that trailing characters are not lost
                        if (!string.IsNullOrEmpty(part.Text))
                        {
                            expanded.Add(part);
                        }
                    }
                }

                return expanded;
            }
        }

        public override string ToString()
        {
            return $"{Symbol}";
        }
    }
}