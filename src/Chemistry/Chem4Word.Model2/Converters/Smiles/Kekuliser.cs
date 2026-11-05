// ---------------------------------------------------------------------------
//  Copyright (c) 2026, The .NET Foundation.
//  This software is released under the Apache Licence, Version 2.0.
//  The licence and further copyright text can be found in the file LICENCE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;

namespace Chem4Word.Model2.Converters.Smiles
{
    /// <summary>
    /// Turns aromatic bonds into alternating single and double bonds: atoms that still need a double bond
    /// are paired up across aromatic bonds by a backtracking perfect matching.
    /// </summary>
    internal static class Kekuliser
    {
        private const int MaximumSteps = 200000;

        public static void Run(ParsedSmiles parsed, List<string> warnings)
        {
            List<SmilesBond> bonds = parsed.Bonds;

            // An implicit bond between aromatic atoms of different rings (biphenyl written c1ccccc1c1ccccc1) is single.
            for (int b = 0; b < bonds.Count; b++)
            {
                if (bonds[b].Aromatic && parsed.IsBridge[b])
                {
                    bonds[b].Aromatic = false;
                }
            }

            if (!bonds.Any(b => b.Aromatic))
            {
                return;
            }

            int n = parsed.Atoms.Count;
            List<(int neighbour, int bond)>[] adj = parsed.Adjacency();
            bool[] needs = Enumerable.Range(0, n).Select(i => NeedsDoubleBond(parsed, adj, i)).ToArray();

            List<int>[] options = Enumerable.Range(0, n)
                                            .Select(i => adj[i].Where(e => bonds[e.bond].Aromatic && needs[e.neighbour])
                                                               .Select(e => e.bond)
                                                               .ToList())
                                            .ToArray();
            int[] mate = Enumerable.Repeat(-1, n).ToArray();
            int steps = 0;

            bool solved = needs.Count(x => x) % 2 == 0 && Solve();
            if (!solved)
            {
                warnings.Add("Could not assign alternating bonds to the aromatic system; aromatic bonds kept");
                return;
            }

            for (int b = 0; b < bonds.Count; b++)
            {
                if (bonds[b].Aromatic)
                {
                    bonds[b].Aromatic = false;
                    bonds[b].Order = mate[bonds[b].From] == b ? 2 : 1;
                }
            }

            // Local Function
            bool Solve()
            {
                if (++steps > MaximumSteps)
                {
                    return false;
                }

                int atom = -1, fewest = int.MaxValue;
                for (int i = 0; i < n; i++)
                {
                    if (needs[i] && mate[i] < 0)
                    {
                        int free = options[i].Count(b => mate[Other(b, i)] < 0);
                        if (free < fewest)
                        {
                            fewest = free;
                            atom = i;
                        }
                    }
                }

                if (atom < 0)
                {
                    return true;
                }

                foreach (int bond in options[atom])
                {
                    int other = Other(bond, atom);
                    if (mate[other] >= 0)
                    {
                        continue;
                    }

                    mate[atom] = mate[other] = bond;
                    if (Solve())
                    {
                        return true;
                    }
                    mate[atom] = mate[other] = -1;
                }
                return false;
            }

            // Local Function
            int Other(int bond, int atom) => bonds[bond].From == atom ? bonds[bond].To : bonds[bond].From;
        }

        private static bool NeedsDoubleBond(ParsedSmiles parsed, List<(int neighbour, int bond)>[] adj, int i)
        {
            if (!adj[i].Any(e => parsed.Bonds[e.bond].Aromatic))
            {
                return false;
            }

            int used = 0;
            foreach ((int _, int b) in adj[i])
            {
                SmilesBond bond = parsed.Bonds[b];
                if (!bond.Aromatic && bond.Order >= 2)
                {
                    // Already has its pi bond, e.g. the C=O of a pyridone
                    return false;
                }
                used += bond.Aromatic ? 1 : bond.Order;
            }

            SmilesAtom atom = parsed.Atoms[i];
            int valence = AromaticValence(atom.Symbol, atom.Charge);
            int hydrogens = atom.Bracket ? atom.HydrogenCount : 0;
            return valence > 0 && used + hydrogens + 1 <= valence;
        }

        // Lowest normal valence, adjusted for charge so that e.g. N+ behaves like C and C- like N.
        private static int AromaticValence(string symbol, int charge)
        {
            switch (symbol)
            {
                case "B":
                    return 3 - charge;

                case "C":
                case "Si":
                case "Ge":
                    return 4 - Math.Abs(charge);

                case "N":
                case "P":
                case "As":
                    return 3 + charge;

                case "O":
                case "S":
                case "Se":
                case "Te":
                    return 2 + charge;

                default:
                    return -1;
            }
        }
    }
}
