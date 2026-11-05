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
    internal sealed class ParsedSmiles
    {
        public readonly List<SmilesAtom> Atoms = new List<SmilesAtom>();
        public readonly List<SmilesBond> Bonds = new List<SmilesBond>();
        public bool[] IsBridge;

        public List<(int neighbour, int bond)>[] Adjacency()
        {
            List<(int, int)>[] adj = Atoms.Select(_ => new List<(int, int)>()).ToArray();
            for (int b = 0; b < Bonds.Count; b++)
            {
                adj[Bonds[b].From].Add((Bonds[b].To, b));
                adj[Bonds[b].To].Add((Bonds[b].From, b));
            }
            return adj;
        }

        public List<int[]> Components()
        {
            List<(int neighbour, int bond)>[] adj = Adjacency();
            int[] component = Enumerable.Repeat(-1, Atoms.Count).ToArray();
            List<int[]> result = new List<int[]>();
            for (int s = 0; s < Atoms.Count; s++)
            {
                if (component[s] >= 0)
                {
                    continue;
                }

                List<int> members = new List<int>();
                Stack<int> stack = new Stack<int>();
                stack.Push(s);
                component[s] = result.Count;
                while (stack.Count > 0)
                {
                    int u = stack.Pop();
                    members.Add(u);
                    foreach ((int v, int _) in adj[u])
                    {
                        if (component[v] < 0)
                        {
                            component[v] = result.Count;
                            stack.Push(v);
                        }
                    }
                }
                members.Sort();
                result.Add(members.ToArray());
            }
            return result;
        }

        // Bonds that are in no ring, found with an iterative Tarjan bridge search.
        public void FindBridges()
        {
            List<(int neighbour, int bond)>[] adj = Adjacency();
            int n = Atoms.Count;
            int[] discovered = Enumerable.Repeat(-1, n).ToArray();
            int[] low = new int[n];
            IsBridge = new bool[Bonds.Count];
            int time = 0;

            for (int root = 0; root < n; root++)
            {
                if (discovered[root] >= 0)
                {
                    continue;
                }

                Stack<(int atom, int viaBond, int next)> stack = new Stack<(int, int, int)>();
                discovered[root] = low[root] = time++;
                stack.Push((root, -1, 0));
                while (stack.Count > 0)
                {
                    (int u, int via, int next) = stack.Pop();
                    if (next < adj[u].Count)
                    {
                        stack.Push((u, via, next + 1));
                        (int v, int b) = adj[u][next];
                        if (b == via)
                        {
                            continue;
                        }
                        if (discovered[v] < 0)
                        {
                            discovered[v] = low[v] = time++;
                            stack.Push((v, b, 0));
                        }
                        else
                        {
                            low[u] = Math.Min(low[u], discovered[v]);
                        }
                    }
                    else if (via >= 0)
                    {
                        int parent = Bonds[via].From == u ? Bonds[via].To : Bonds[via].From;
                        low[parent] = Math.Min(low[parent], low[u]);
                        if (low[u] > discovered[parent])
                        {
                            IsBridge[via] = true;
                        }
                    }
                }
            }
        }
    }
}
