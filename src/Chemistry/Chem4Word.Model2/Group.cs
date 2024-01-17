// ---------------------------------------------------------------------------
//  Copyright (c) 2024, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.Model2.Helpers;
using System.Collections.Generic;

namespace Chem4Word.Model2
{
    public class Group
    {
        public string Component { get; set; }

        public int Count { get; set; }

        public Group(string e, int c)
        {
            Component = e;
            Count = c;
        }

        public override string ToString()
        {
            return $"{Component} * {Count}";
        }

        /// <summary>
        /// Calculated AtomicWeight
        /// </summary>
        public double AtomicWeight
        {
            get
            {
                double atomicWeight = 0;
                if (AtomHelpers.TryParse(Component, false, out var elementBase))
                {
                    if (elementBase is Element e)
                    {
                        atomicWeight = e.AtomicWeight;
                    }

                    if (elementBase is FunctionalGroup fg)
                    {
                        atomicWeight = fg.AtomicWeight;
                    }
                }

                return atomicWeight;
            }
        }

        public Dictionary<string, int> FormulaParts
        {
            get
            {
                var parts = new Dictionary<string, int>();

                if (AtomHelpers.TryParse(Component, false, out var elementBase))
                {
                    if (elementBase is Element e)
                    {
                        parts.Add(e.Symbol, 1);
                    }

                    if (elementBase is FunctionalGroup fg)
                    {
                        foreach (var component in fg.Components)
                        {
                            var pp = component.FormulaParts;
                            foreach (var p in pp)
                            {
                                if (parts.ContainsKey(p.Key))
                                {
                                    parts[p.Key] += p.Value;
                                }
                                else
                                {
                                    parts.Add(p.Key, p.Value);
                                }
                            }
                        }
                    }
                }

                return parts;
            }
        }
    }
}