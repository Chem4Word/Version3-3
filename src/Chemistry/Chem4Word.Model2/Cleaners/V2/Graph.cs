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
using System.Windows;

namespace Chem4Word.Model2.Cleaners.V2
{
    // Connection table with per-neighbour bond data held in arrays parallel to Adj.
    internal sealed class Graph
    {
        public int Count;
        public int[][] Adj;
        public double[][] Order;
        public BondStereo[][] Stereo;
        public string[] Symbol;
        public int[] Charge;
        public Point[] Original;

        public int Degree(int atom) => Adj[atom].Length;

        public int Slot(int atom, int neighbour) => Array.IndexOf(Adj[atom], neighbour);

        public static Graph Build(List<Atom> atoms, List<Bond> bonds)
        {
            int n = atoms.Count;
            Dictionary<Atom, int> index = new Dictionary<Atom, int>();
            for (int i = 0; i < n; i++)
            {
                index[atoms[i]] = i;
            }

            List<int>[] adj = Enumerable.Range(0, n).Select(_ => new List<int>()).ToArray();
            List<double>[] order = Enumerable.Range(0, n).Select(_ => new List<double>()).ToArray();
            List<BondStereo>[] stereo = Enumerable.Range(0, n).Select(_ => new List<BondStereo>()).ToArray();
            HashSet<(int, int)> seen = new HashSet<(int, int)>();

            foreach (Bond bond in bonds)
            {
                if (!index.TryGetValue(bond.StartAtom, out int a) || !index.TryGetValue(bond.EndAtom, out int b) || a == b
                    || !seen.Add((Math.Min(a, b), Math.Max(a, b))))
                {
                    continue;
                }

                double value = bond.OrderValue ?? 1;
                adj[a].Add(b);
                adj[b].Add(a);
                order[a].Add(value);
                order[b].Add(value);
                stereo[a].Add(bond.Stereo);
                stereo[b].Add(bond.Stereo);
            }

            return new Graph
            {
                Count = n,
                Adj = adj.Select(l => l.ToArray()).ToArray(),
                Order = order.Select(l => l.ToArray()).ToArray(),
                Stereo = stereo.Select(l => l.ToArray()).ToArray(),
                Symbol = atoms.Select(a => a.Element?.Symbol ?? "").ToArray(),
                Charge = atoms.Select(a => a.FormalCharge ?? 0).ToArray(),
                Original = atoms.Select(a => a.Position).ToArray()
            };
        }

        // The induced subgraph on 'atoms', renumbered 0..atoms.Length-1 in the given order.
        public Graph Subgraph(int[] atoms)
        {
            Dictionary<int, int> local = new Dictionary<int, int>();
            for (int k = 0; k < atoms.Length; k++)
            {
                local[atoms[k]] = k;
            }

            int[][] adj = new int[atoms.Length][];
            double[][] order = new double[atoms.Length][];
            BondStereo[][] stereo = new BondStereo[atoms.Length][];
            for (int k = 0; k < atoms.Length; k++)
            {
                int[] keep = Enumerable.Range(0, Adj[atoms[k]].Length).Where(s => local.ContainsKey(Adj[atoms[k]][s])).ToArray();
                adj[k] = keep.Select(s => local[Adj[atoms[k]][s]]).ToArray();
                order[k] = keep.Select(s => Order[atoms[k]][s]).ToArray();
                stereo[k] = keep.Select(s => Stereo[atoms[k]][s]).ToArray();
            }

            return new Graph
            {
                Count = atoms.Length,
                Adj = adj,
                Order = order,
                Stereo = stereo,
                Symbol = atoms.Select(a => Symbol[a]).ToArray(),
                Charge = atoms.Select(a => Charge[a]).ToArray(),
                Original = atoms.Select(a => Original[a]).ToArray()
            };
        }
    }
}
