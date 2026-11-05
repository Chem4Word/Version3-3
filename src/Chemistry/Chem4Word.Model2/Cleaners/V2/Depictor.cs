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
    /// <summary>
    /// Lays out one connected fragment of heavy atoms in units of one bond length (Y up).
    /// </summary>
    internal sealed class Depictor
    {
        private const int SampleCount = 100;
        private const int MaxRefinePasses = 20;
        private const int MaxThrowbackRounds = 3;
        private const int MaxStretchPasses = 20;
        private const double CloseContact = 0.5;
        private const int MacrocycleSize = 12;

        private static readonly HashSet<string> ChainElements = new HashSet<string> { "C", "N", "O", "S" };
        private static readonly HashSet<string> Halogens = new HashSet<string> { "F", "Cl", "Br", "I" };
        private static readonly double[] RotationSteps = { 10, -10, 20, -20, 30, -30, 45, -45, 60, -60, 0 };
        private static readonly double[] Stretches = { 1, 1.15, 1.3 };

        private readonly Graph _g;
        private readonly int _n;
        private readonly bool[,] _bonded;
        private readonly List<(int a, int b)> _bonds = new List<(int a, int b)>();
        private readonly bool[][] _ringEdge;
        private readonly int[] _blockOf;
        private readonly List<RingBlock> _blocks = new List<RingBlock>();
        private readonly List<StereoBond> _stereo = new List<StereoBond>();
        private readonly Random _random = new Random(2006);
        private readonly int _root;

        public Depictor(Graph g)
        {
            _g = g;
            _n = g.Count;
            _bonded = new bool[_n, _n];
            for (int a = 0; a < _n; a++)
            {
                foreach (int b in g.Adj[a])
                {
                    _bonded[a, b] = true;
                    if (a < b)
                    {
                        _bonds.Add((a, b));
                    }
                }
            }

            _ringEdge = Enumerable.Range(0, _n).Select(a => g.Adj[a].Select(b => IsRingBond(a, b)).ToArray()).ToArray();
            _blockOf = Enumerable.Repeat(-1, _n).ToArray();
            PerceiveRingBlocks();
            PerceiveStereo();
            _root = ChooseRoot();
        }

        public List<int[]> Rings => _blocks.SelectMany(b => b.Rings).ToList();

        public (double[] x, double[] y) Run()
        {
            if (_n == 1)
            {
                return (new double[1], new double[1]);
            }

            HashSet<int> throwback = new HashSet<int>();
            Candidate best = Sample(BuildConstraints(throwback));

            // Atoms on the shortest path between each clash are re-sampled with more freedom.
            for (int round = 0; round < MaxThrowbackRounds && best.Clashes.Count > 0; round++)
            {
                int before = throwback.Count;
                foreach ((int i, int j) in best.Clashes)
                {
                    foreach (int atom in ShortestPath(i, j).Where(CanThrowBack))
                    {
                        throwback.Add(atom);
                    }
                }
                if (throwback.Count == before)
                {
                    break;
                }

                Candidate retry = Sample(BuildConstraints(throwback));
                if (retry.IsBetterThan(best))
                {
                    best = retry;
                }
            }

            StretchAndRotate(best);
            return (best.X, best.Y);
        }

        private bool CanThrowBack(int atom) =>
            _blockOf[atom] < 0 && _g.Degree(atom) >= 2 && !_stereo.Any(s => s.A == atom || s.B == atom);

        #region Partitioning

        private bool IsRingBond(int a, int b)
        {
            bool[] seen = new bool[_n];
            Queue<int> queue = new Queue<int>();
            queue.Enqueue(a);
            seen[a] = true;
            while (queue.Count > 0)
            {
                int u = queue.Dequeue();
                foreach (int v in _g.Adj[u])
                {
                    if (u == a && v == b || seen[v])
                    {
                        continue;
                    }
                    if (v == b)
                    {
                        return true;
                    }
                    seen[v] = true;
                    queue.Enqueue(v);
                }
            }
            return false;
        }

        private bool IsRingBondBetween(int a, int b) => _ringEdge[a][_g.Slot(a, b)];

        private void PerceiveRingBlocks()
        {
            for (int s = 0; s < _n; s++)
            {
                if (_blockOf[s] >= 0 || !_ringEdge[s].Any(r => r))
                {
                    continue;
                }

                RingBlock block = new RingBlock { Id = _blocks.Count };
                List<int> atoms = new List<int>();
                Queue<int> queue = new Queue<int>();
                queue.Enqueue(s);
                _blockOf[s] = block.Id;
                while (queue.Count > 0)
                {
                    int u = queue.Dequeue();
                    atoms.Add(u);
                    for (int k = 0; k < _g.Adj[u].Length; k++)
                    {
                        int v = _g.Adj[u][k];
                        if (_ringEdge[u][k] && _blockOf[v] < 0)
                        {
                            _blockOf[v] = block.Id;
                            queue.Enqueue(v);
                        }
                    }
                }

                atoms.Sort();
                block.Atoms = atoms.ToArray();
                _blocks.Add(block);
                LayoutRingBlock(block);
            }
        }

        // Acyclic double bonds keep the cis/trans arrangement they had in the original drawing, when it had one.
        private void PerceiveStereo()
        {
            foreach ((int a, int b) in _bonds)
            {
                int slot = _g.Slot(a, b);
                if (_g.Order[a][slot] != 2 || _ringEdge[a][slot] || _g.Stereo[a][slot] == BondStereo.Indeterminate
                    || _g.Degree(a) < 2 || _g.Degree(b) < 2 || _blockOf[a] >= 0 || _blockOf[b] >= 0)
                {
                    continue;
                }

                int s = _g.Adj[a].First(v => v != b);
                int t = _g.Adj[b].First(v => v != a);
                double sideS = Side(_g.Original[a], _g.Original[b], _g.Original[s]);
                double sideT = Side(_g.Original[a], _g.Original[b], _g.Original[t]);
                if (Math.Abs(sideS) > 0.05 && Math.Abs(sideT) > 0.05)
                {
                    _stereo.Add(new StereoBond { A = a, B = b, S = s, T = t, Cis = sideS * sideT > 0 });
                }
            }
        }

        // Sine of the angle at 'from' between 'to' and 'point', or 0 if the geometry is degenerate.
        private static double Side(Point from, Point to, Point point)
        {
            Vector u = to - from, v = point - from;
            double scale = u.Length * v.Length;
            return scale < 1e-9 ? 0 : Vector.CrossProduct(u, v) / scale;
        }

        private int ChooseRoot()
        {
            if (_blocks.Count > 0)
            {
                return _blocks.OrderByDescending(b => b.Atoms.Length).First().Atoms[0];
            }

            return Enumerable.Range(0, _n).OrderBy(a => Distances(a).Max()).First();
        }

        private List<Constraint> BuildConstraints(HashSet<int> throwback)
        {
            List<Constraint> constraints = new List<Constraint>();
            bool[] assigned = new bool[_n];

            foreach (RingBlock block in _blocks)
            {
                constraints.Add(BlockConstraint(block));
                foreach (int atom in block.Atoms)
                {
                    assigned[atom] = true;
                }
            }

            for (int a = 0; a < _n; a++)
            {
                if (_g.Degree(a) <= 1)
                {
                    assigned[a] = true;
                }
            }

            foreach (StereoBond stereo in _stereo.Where(s => !assigned[s.A] && !assigned[s.B]))
            {
                constraints.Add(PairConstraint(stereo.A, stereo.B, stereo));
                assigned[stereo.A] = assigned[stereo.B] = true;
            }

            foreach (int[] chain in FindChains(assigned, throwback))
            {
                constraints.Add(ChainConstraint(chain));
                foreach (int atom in chain)
                {
                    assigned[atom] = true;
                }
            }

            foreach ((int a, int b) in MatchPairs(assigned, throwback))
            {
                constraints.Add(PairConstraint(a, b, null));
                assigned[a] = assigned[b] = true;
            }

            for (int a = 0; a < _n; a++)
            {
                if (!assigned[a])
                {
                    Constraint single = new Constraint();
                    single.Choices.AddRange(AtomChoices(a, throwback));
                    constraints.Add(single);
                }
            }

            return constraints;
        }

        private static Constraint BlockConstraint(RingBlock block)
        {
            Constraint constraint = new Constraint { Block = block };
            foreach (int mirror in new[] { 1, -1 })
            {
                Choice choice = new Choice { Weight = 1, Mirror = mirror };
                foreach (int atom in block.Atoms)
                {
                    choice.Angles.Add((atom, block.Angles[atom].Select(t => mirror * t).ToArray()));
                }
                constraint.Choices.Add(choice);
            }
            return constraint;
        }

        /// <summary>
        /// Locally ideal arrangements of an atom's neighbours (angles in the atom's own frame), covering every
        /// distinct ordering and both mirror images. Throwback atoms also get variants with the bonds towards
        /// other throwback atoms bent by 15° or 25°.
        /// </summary>
        private List<Choice> AtomChoices(int atom, HashSet<int> throwback)
        {
            int degree = _g.Degree(atom);
            List<Choice> result = new List<Choice>();
            HashSet<string> seen = new HashSet<string>();

            foreach ((double[] geometry, double weight) in Geometries(atom))
            {
                IEnumerable<int[]> orders = degree <= 4
                    ? Permutations(degree)
                    : new[] { Enumerable.Range(0, degree).ToArray() };
                foreach (int[] order in orders)
                {
                    double[] angles = order.Select(p => geometry[p] * GeometryHelper.Degrees).ToArray();
                    Add(angles, weight);
                    if (degree > 4)
                    {
                        Add(angles.Select(t => -t).ToArray(), weight);
                    }
                }
            }

            return throwback.Contains(atom) ? Flex(atom, result, throwback) : result;

            // Local function
            void Add(double[] angles, double weight)
            {
                string key = string.Join(",", angles.Select(t => Math.Round(GeometryHelper.NormalizeAngle(t - angles[0]) / GeometryHelper.Degrees) % 360));
                if (seen.Add(key))
                {
                    Choice choice = new Choice { Weight = weight };
                    choice.Angles.Add((atom, angles));
                    result.Add(choice);
                }
            }
        }

        private IEnumerable<(double[] geometry, double weight)> Geometries(int atom)
        {
            int degree = _g.Degree(atom);
            double[] orders = _g.Order[atom];
            switch (degree)
            {
                case 2:
                    bool linear = orders.Any(o => o >= 3) || orders.Count(o => o == 2) >= 2;
                    yield return (linear ? new double[] { 0, 180 } : new double[] { 0, 120 }, 1);
                    break;

                case 3:
                    yield return (new double[] { 0, 120, 240 }, 1);
                    break;

                case 4:
                    yield return (new double[] { 0, 90, 180, 270 }, 1);
                    yield return (new double[] { 0, 120, 200, 280 }, 0.5);
                    break;

                default:
                    yield return (Enumerable.Range(0, degree).Select(k => 360.0 * k / degree).ToArray(), 1);
                    break;
            }
        }

        private List<Choice> Flex(int atom, List<Choice> choices, HashSet<int> throwback)
        {
            int[] flexible = Enumerable.Range(0, _g.Degree(atom)).Where(k => throwback.Contains(_g.Adj[atom][k])).ToArray();
            if (flexible.Length == 0)
            {
                return choices;
            }

            double[] deltas = { 0, 15, -15, 25, -25 };
            bool everyCombination = Math.Pow(deltas.Length, flexible.Length) * choices.Count <= 600;
            List<Choice> result = new List<Choice>();

            foreach (Choice choice in choices)
            {
                double[] baseAngles = choice.Angles[0].angles;
                IEnumerable<int[]> variants = everyCombination
                    ? Combinations(flexible.Length, deltas.Length)
                    : Enumerable.Range(0, flexible.Length)
                                .SelectMany(f => Enumerable.Range(1, deltas.Length - 1)
                                                           .Select(d => Enumerable.Range(0, flexible.Length).Select(i => i == f ? d : 0).ToArray()))
                                .Prepend(new int[flexible.Length]);

                foreach (int[] variant in variants)
                {
                    double[] angles = (double[])baseAngles.Clone();
                    double weight = choice.Weight;
                    for (int i = 0; i < flexible.Length; i++)
                    {
                        double delta = deltas[variant[i]];
                        angles[flexible[i]] += delta * GeometryHelper.Degrees;
                        weight *= delta == 0 ? 1 : Math.Abs(delta) < 20 ? 0.5 : 0.25;
                    }

                    Choice flexed = new Choice { Weight = weight };
                    flexed.Angles.Add((atom, angles));
                    result.Add(flexed);
                }
            }
            return result;
        }

        /// <summary>
        /// Every combination of the two atoms' local choices. A stereo double bond keeps only those matching
        /// its cis/trans arrangement; two disubstituted atoms prefer the zigzag (trans) arrangement.
        /// </summary>
        private Constraint PairConstraint(int a, int b, StereoBond stereo)
        {
            HashSet<int> none = new HashSet<int>();
            List<Choice> choicesA = AtomChoices(a, none);
            List<Choice> choicesB = AtomChoices(b, none);
            int ab = _g.Slot(a, b), ba = _g.Slot(b, a);
            bool zigzag = stereo == null && _g.Degree(a) == 2 && _g.Degree(b) == 2;
            int s = stereo?.S ?? _g.Adj[a].First(v => v != b);
            int t = stereo?.T ?? _g.Adj[b].First(v => v != a);

            Constraint constraint = new Constraint();
            foreach (Choice x in choicesA)
            {
                foreach (Choice y in choicesB)
                {
                    double[] anglesA = x.Angles[0].angles, anglesB = y.Angles[0].angles;
                    double weight = x.Weight * y.Weight;

                    if (stereo != null || zigzag)
                    {
                        double sideS = Math.Sin(anglesA[_g.Slot(a, s)] - anglesA[ab]);
                        double sideT = -Math.Sin(anglesB[_g.Slot(b, t)] - anglesB[ba]);
                        if (Math.Abs(sideS) > 1e-6 && Math.Abs(sideT) > 1e-6)
                        {
                            bool cis = sideS * sideT > 0;
                            if (stereo != null && cis != stereo.Cis)
                            {
                                continue;
                            }
                            if (zigzag && cis)
                            {
                                weight *= 0.3;
                            }
                        }
                    }

                    Choice choice = new Choice { Weight = weight };
                    choice.Angles.Add((a, anglesA));
                    choice.Angles.Add((b, anglesB));
                    constraint.Choices.Add(choice);
                }
            }
            return constraint;
        }

        // Maximum matching of the remaining atoms; they are acyclic, so repeatedly pairing a leaf is optimal.
        private List<(int a, int b)> MatchPairs(bool[] assigned, HashSet<int> throwback)
        {
            bool[] available = Enumerable.Range(0, _n).Select(a => !assigned[a] && !throwback.Contains(a)).ToArray();
            List<(int a, int b)> pairs = new List<(int a, int b)>();
            bool changed = true;
            while (changed)
            {
                changed = false;
                for (int a = 0; a < _n; a++)
                {
                    if (!available[a])
                    {
                        continue;
                    }

                    int[] neighbours = _g.Adj[a].Where(v => available[v]).ToArray();
                    if (neighbours.Length == 0)
                    {
                        available[a] = false;
                    }
                    else if (neighbours.Length == 1)
                    {
                        pairs.Add((a, neighbours[0]));
                        available[a] = available[neighbours[0]] = false;
                        changed = true;
                    }
                }
            }
            return pairs;
        }

        // Repeatedly takes the longest path of chain-eligible atoms, while it has at least four atoms.
        private List<int[]> FindChains(bool[] assigned, HashSet<int> throwback)
        {
            bool[] eligible = Enumerable.Range(0, _n).Select(a => !assigned[a] && !throwback.Contains(a) && IsChainAtom(a)).ToArray();
            List<int[]> chains = new List<int[]>();
            while (true)
            {
                int[] longest = LongestPath(eligible);
                if (longest.Length < 4)
                {
                    break;
                }

                chains.Add(longest);
                foreach (int atom in longest)
                {
                    eligible[atom] = false;
                }
            }
            return chains;
        }

        private bool IsChainAtom(int atom)
        {
            if (!ChainElements.Contains(_g.Symbol[atom]) || _g.Charge[atom] != 0 || _g.Order[atom].Any(o => o != 1))
            {
                return false;
            }

            int degree = _g.Degree(atom);
            return degree == 2 || degree == 3 || (degree == 4 && _g.Symbol[atom] == "C" && TerminalHalogens(atom).Count() >= 2);
        }

        private IEnumerable<int> TerminalHalogens(int atom) =>
            _g.Adj[atom].Where(v => Halogens.Contains(_g.Symbol[v]) && _g.Degree(v) == 1);

        private int[] LongestPath(bool[] eligible)
        {
            bool[] seen = new bool[_n];
            int[] best = new int[0];
            for (int s = 0; s < _n; s++)
            {
                if (!eligible[s] || seen[s])
                {
                    continue;
                }

                // Tree diameter: farthest from anywhere, then farthest from that.
                (int end, _) = Farthest(s, eligible, seen);
                (int other, int[] parent) = Farthest(end, eligible, null);

                List<int> path = new List<int>();
                for (int v = other; v >= 0; v = parent[v])
                {
                    path.Add(v);
                }
                if (path.Count > best.Length)
                {
                    best = path.ToArray();
                }
            }
            return best;
        }

        private (int farthest, int[] parent) Farthest(int start, bool[] eligible, bool[] seen)
        {
            int[] parent = Enumerable.Repeat(-2, _n).ToArray();
            parent[start] = -1;
            Queue<int> queue = new Queue<int>();
            queue.Enqueue(start);
            int last = start;
            while (queue.Count > 0)
            {
                int u = queue.Dequeue();
                last = u;
                if (seen != null)
                {
                    seen[u] = true;
                }
                foreach (int v in _g.Adj[u])
                {
                    if (eligible[v] && parent[v] == -2)
                    {
                        parent[v] = u;
                        queue.Enqueue(v);
                    }
                }
            }
            return (last, parent);
        }

        /// <summary>
        /// Chains zigzag with 120° angles, a third substituent taking the remaining interstice; a carbon carrying
        /// two terminal halogens continues the chain straight through with the halogens either side at 90°.
        /// The two choices are mirror images.
        /// </summary>
        private Constraint ChainConstraint(int[] chain)
        {
            Constraint constraint = new Constraint();
            foreach (int mirror in new[] { 1, -1 })
            {
                Choice choice = new Choice { Weight = 1 };
                for (int i = 0; i < chain.Length; i++)
                {
                    choice.Angles.Add((chain[i], ChainAngles(chain, i, i % 2 == 0 ? mirror : -mirror)));
                }
                constraint.Choices.Add(choice);
            }
            return constraint;
        }

        private double[] ChainAngles(int[] chain, int i, int side)
        {
            int atom = chain[i];
            int next = i + 1 < chain.Length ? chain[i + 1] : -1;
            int prev = i > 0 ? chain[i - 1] : -1;
            List<int> others = _g.Adj[atom].Where(v => v != next && v != prev).ToList();
            List<int> across = new List<int>();

            bool halocarbon = _g.Degree(atom) == 4;
            if (halocarbon)
            {
                across = TerminalHalogens(atom).Take(2).ToList();
                others = others.Except(across).ToList();
            }

            // Chain ends carry the line on through an outside neighbour.
            if (next < 0 && others.Count > 0)
            {
                next = others[0];
                others.RemoveAt(0);
            }
            if (prev < 0 && others.Count > 0)
            {
                prev = others[0];
                others.RemoveAt(0);
            }

            double[] angles = new double[_g.Degree(atom)];
            angles[_g.Slot(atom, next)] = 0;
            if (halocarbon)
            {
                angles[_g.Slot(atom, prev)] = Math.PI;
                angles[_g.Slot(atom, across[0])] = Math.PI / 2;
                angles[_g.Slot(atom, across[1])] = -Math.PI / 2;
            }
            else
            {
                if (prev >= 0)
                {
                    angles[_g.Slot(atom, prev)] = side * 120 * GeometryHelper.Degrees;
                }
                if (others.Count > 0)
                {
                    angles[_g.Slot(atom, others[0])] = -side * 120 * GeometryHelper.Degrees;
                }
            }
            return angles;
        }

        #endregion Partitioning

        #region Sampling

        /// <summary>
        /// Picks the least congested of a set of weighted random combinations, then improves it by changing one
        /// constraint at a time until a full pass makes no improvement.
        /// </summary>
        private Candidate Sample(List<Constraint> constraints)
        {
            int count = constraints.Count;
            int[] best = new int[count];
            double bestCongestion = double.MaxValue;
            bool anyChoice = constraints.Any(c => c.Choices.Count > 1);

            for (int s = 0; s < (anyChoice ? SampleCount : 1); s++)
            {
                int[] selection = constraints.Select(PickWeighted).ToArray();
                (double[] x, double[] y) = Assemble(constraints, selection);
                double congestion = Congestion(x, y);
                if (congestion < bestCongestion)
                {
                    bestCongestion = congestion;
                    best = selection;
                }
            }

            bool improved = anyChoice;
            for (int pass = 0; improved && pass < MaxRefinePasses; pass++)
            {
                improved = false;
                for (int i = 0; i < count; i++)
                {
                    int keep = best[i];
                    for (int k = 0; k < constraints[i].Choices.Count; k++)
                    {
                        if (k == keep)
                        {
                            continue;
                        }

                        best[i] = k;
                        (double[] x, double[] y) = Assemble(constraints, best);
                        double congestion = Congestion(x, y);
                        if (congestion < bestCongestion - 1e-9)
                        {
                            bestCongestion = congestion;
                            keep = k;
                            improved = true;
                        }
                    }
                    best[i] = keep;
                }
            }

            (double[] bx, double[] by) = Assemble(constraints, best);
            return new Candidate { X = bx, Y = by, Congestion = bestCongestion, Clashes = Clashes(bx, by) };
        }

        private int PickWeighted(Constraint constraint)
        {
            double total = constraint.Choices.Sum(c => c.Weight);
            double r = _random.NextDouble() * total;
            for (int k = 0; k < constraint.Choices.Count; k++)
            {
                r -= constraint.Choices[k].Weight;
                if (r <= 0)
                {
                    return k;
                }
            }
            return constraint.Choices.Count - 1;
        }

        // Converts the chosen local geometries into Cartesian coordinates by walking out from the root.
        private (double[] x, double[] y) Assemble(List<Constraint> constraints, int[] selection)
        {
            double[][] angles = new double[_n][];
            double[] mirror = Enumerable.Repeat(1.0, _blocks.Count).ToArray();
            for (int i = 0; i < constraints.Count; i++)
            {
                Choice choice = constraints[i].Choices[selection[i]];
                foreach ((int atom, double[] a) in choice.Angles)
                {
                    angles[atom] = a;
                }
                if (constraints[i].Block != null)
                {
                    mirror[constraints[i].Block.Id] = choice.Mirror;
                }
            }
            for (int a = 0; a < _n; a++)
            {
                if (angles[a] == null)
                {
                    int degree = _g.Degree(a);
                    angles[a] = Enumerable.Range(0, degree).Select(k => 2 * Math.PI * k / degree).ToArray();
                }
            }

            double[] x = new double[_n], y = new double[_n], rotation = new double[_n];
            bool[] placed = new bool[_n];
            Queue<int> queue = new Queue<int>();
            PlaceAt(_root, 0, 0, 0);

            while (queue.Count > 0)
            {
                int a = queue.Dequeue();
                for (int k = 0; k < _g.Adj[a].Length; k++)
                {
                    int b = _g.Adj[a][k];
                    if (placed[b])
                    {
                        continue;
                    }

                    double direction = rotation[a] + angles[a][k];
                    double back = angles[b][_g.Slot(b, a)];
                    PlaceAt(b, x[a] + Math.Cos(direction), y[a] + Math.Sin(direction), direction + Math.PI - back);
                }
            }

            return (x, y);

            // Local function: ring blocks are placed whole, as a rigid body.
            void PlaceAt(int atom, double px, double py, double rot)
            {
                int id = _blockOf[atom];
                IEnumerable<int> members = id < 0 ? new[] { atom } : (IEnumerable<int>)_blocks[id].Atoms;
                Point anchor = id < 0 ? new Point() : _blocks[id].Coords[atom];
                double m = id < 0 ? 1 : mirror[id];
                double c = Math.Cos(rot), s = Math.Sin(rot);

                foreach (int member in members)
                {
                    double dx = 0, dy = 0;
                    if (id >= 0)
                    {
                        dx = _blocks[id].Coords[member].X - anchor.X;
                        dy = m * (_blocks[id].Coords[member].Y - anchor.Y);
                    }
                    x[member] = px + c * dx - s * dy;
                    y[member] = py + s * dx + c * dy;
                    rotation[member] = rot;
                    placed[member] = true;
                    queue.Enqueue(member);
                }
            }
        }

        // Σ 1/d² over non-bonded pairs.
        private double Congestion(double[] x, double[] y)
        {
            double sum = 0;
            for (int i = 0; i < _n; i++)
            {
                for (int j = i + 1; j < _n; j++)
                {
                    if (!_bonded[i, j])
                    {
                        double dx = x[i] - x[j], dy = y[i] - y[j];
                        sum += 1 / Math.Max(dx * dx + dy * dy, 1e-4);
                    }
                }
            }
            return sum;
        }

        // Atom pairs that are too close, or that represent crossing bonds; clashes inside one ring block are its own business.
        private List<(int, int)> Clashes(double[] x, double[] y)
        {
            List<(int, int)> clashes = new List<(int, int)>();
            for (int i = 0; i < _n; i++)
            {
                for (int j = i + 1; j < _n; j++)
                {
                    if (!_bonded[i, j] && !SameBlock(i, j)
                        && (x[i] - x[j]) * (x[i] - x[j]) + (y[i] - y[j]) * (y[i] - y[j]) < CloseContact * CloseContact)
                    {
                        clashes.Add((i, j));
                    }
                }
            }

            for (int p = 0; p < _bonds.Count; p++)
            {
                (int a, int b) = _bonds[p];
                for (int q = p + 1; q < _bonds.Count; q++)
                {
                    (int c, int d) = _bonds[q];
                    if (a == c || a == d || b == c || b == d || SameBlock(a, b) && SameBlock(a, c) && SameBlock(a, d))
                    {
                        continue;
                    }
                    if (SegmentsCross(x, y, a, b, c, d))
                    {
                        clashes.Add((a, c));
                    }
                }
            }
            return clashes;
        }

        private bool SameBlock(int a, int b) => _blockOf[a] >= 0 && _blockOf[a] == _blockOf[b];

        #endregion Sampling

        #region Fixing clashes

        /// <summary>
        /// Last resort for remaining clashes: rotate and/or stretch an acyclic bond on the path between the clashing
        /// atoms, moving the smaller side, keeping whichever single change helps most.
        /// </summary>
        private void StretchAndRotate(Candidate candidate)
        {
            double[] x = candidate.X, y = candidate.Y;
            for (int pass = 0; pass < MaxStretchPasses; pass++)
            {
                List<(int, int)> clashes = Clashes(x, y);
                if (clashes.Count == 0)
                {
                    break;
                }

                int bestCount = clashes.Count;
                double bestCongestion = Congestion(x, y);
                double[] bestX = null, bestY = null;

                int[] path = ShortestPath(clashes[0].Item1, clashes[0].Item2);
                for (int p = 0; p + 1 < path.Length; p++)
                {
                    int pivot = path[p], moving = path[p + 1];
                    if (IsRingBondBetween(pivot, moving))
                    {
                        continue;
                    }

                    List<int> side = SideOf(moving, pivot);
                    if (side.Count * 2 > _n)
                    {
                        (pivot, moving) = (moving, pivot);
                        side = SideOf(moving, pivot);
                    }

                    foreach (double angle in RotationSteps)
                    {
                        foreach (double stretch in Stretches)
                        {
                            if (angle == 0 && stretch == 1)
                            {
                                continue;
                            }

                            double[] tx = (double[])x.Clone(), ty = (double[])y.Clone();
                            MoveSide(tx, ty, side, pivot, moving, angle * GeometryHelper.Degrees, stretch);
                            int count = Clashes(tx, ty).Count;
                            double congestion = Congestion(tx, ty);
                            if (count < bestCount || (count == bestCount && congestion < bestCongestion - 1e-9))
                            {
                                bestCount = count;
                                bestCongestion = congestion;
                                bestX = tx;
                                bestY = ty;
                            }
                        }
                    }
                }

                if (bestX == null)
                {
                    break;
                }
                Array.Copy(bestX, x, _n);
                Array.Copy(bestY, y, _n);
            }

            candidate.Clashes = Clashes(x, y);
            candidate.Congestion = Congestion(x, y);
        }

        private static void MoveSide(double[] x, double[] y, List<int> side, int pivot, int moving, double angle, double stretch)
        {
            double c = Math.Cos(angle), s = Math.Sin(angle);
            foreach (int a in side)
            {
                double dx = x[a] - x[pivot], dy = y[a] - y[pivot];
                x[a] = x[pivot] + c * dx - s * dy;
                y[a] = y[pivot] + s * dx + c * dy;
            }

            double bx = x[moving] - x[pivot], by = y[moving] - y[pivot];
            foreach (int a in side)
            {
                x[a] += (stretch - 1) * bx;
                y[a] += (stretch - 1) * by;
            }
        }

        // Atoms reachable from 'start' without passing through 'blocked'.
        private List<int> SideOf(int start, int blocked)
        {
            bool[] seen = new bool[_n];
            seen[start] = seen[blocked] = true;
            List<int> side = new List<int> { start };
            for (int i = 0; i < side.Count; i++)
            {
                foreach (int v in _g.Adj[side[i]])
                {
                    if (!seen[v])
                    {
                        seen[v] = true;
                        side.Add(v);
                    }
                }
            }
            return side;
        }

        private int[] ShortestPath(int from, int to)
        {
            int[] parent = Enumerable.Repeat(-2, _n).ToArray();
            parent[from] = -1;
            Queue<int> queue = new Queue<int>();
            queue.Enqueue(from);
            while (queue.Count > 0 && parent[to] == -2)
            {
                int u = queue.Dequeue();
                foreach (int v in _g.Adj[u])
                {
                    if (parent[v] == -2)
                    {
                        parent[v] = u;
                        queue.Enqueue(v);
                    }
                }
            }

            List<int> path = new List<int>();
            for (int v = to; v >= 0; v = parent[v])
            {
                path.Add(v);
            }
            path.Reverse();
            return path.ToArray();
        }

        // Breadth-first bond counts from 'start' (-1 where unreachable).
        private int[] Distances(int start)
        {
            int[] d = Enumerable.Repeat(-1, _n).ToArray();
            d[start] = 0;
            Queue<int> queue = new Queue<int>();
            queue.Enqueue(start);
            while (queue.Count > 0)
            {
                int u = queue.Dequeue();
                foreach (int v in _g.Adj[u])
                {
                    if (d[v] < 0)
                    {
                        d[v] = d[u] + 1;
                        queue.Enqueue(v);
                    }
                }
            }
            return d;
        }

        #endregion Fixing clashes

        private static bool SegmentsCross(double[] x, double[] y, int a, int b, int c, int d)
        {
            double d1 = Orient(x[c], y[c], x[d], y[d], x[a], y[a]);
            double d2 = Orient(x[c], y[c], x[d], y[d], x[b], y[b]);
            double d3 = Orient(x[a], y[a], x[b], y[b], x[c], y[c]);
            double d4 = Orient(x[a], y[a], x[b], y[b], x[d], y[d]);
            const double eps = 1e-9;
            return (d1 > eps && d2 < -eps || d1 < -eps && d2 > eps) && (d3 > eps && d4 < -eps || d3 < -eps && d4 > eps);
        }

        private static double Orient(double ax, double ay, double bx, double by, double cx, double cy) =>
            (bx - ax) * (cy - ay) - (by - ay) * (cx - ax);

        private static IEnumerable<int[]> Permutations(int n)
        {
            if (n == 0)
            {
                yield return new int[0];
                yield break;
            }

            foreach (int[] rest in Permutations(n - 1))
            {
                for (int position = 0; position < n; position++)
                {
                    List<int> p = rest.ToList();
                    p.Insert(position, n - 1);
                    yield return p.ToArray();
                }
            }
        }

        // Every length-'length' tuple of values in 0..radix-1.
        private static IEnumerable<int[]> Combinations(int length, int radix)
        {
            int[] digits = new int[length];
            while (true)
            {
                yield return (int[])digits.Clone();
                int i = 0;
                while (i < length && ++digits[i] == radix)
                {
                    digits[i++] = 0;
                }
                if (i == length)
                {
                    yield break;
                }
            }
        }

        /// <summary>
        /// Rings of 12 or more atoms go straight to the macrocycle method. Otherwise terminal rings are peeled off,
        /// the core is drawn as a polygon or embedded (2D, or 3D and projected for cages and failed 2D layouts),
        /// and the peeled rings are fused back on. Finally the exocyclic bond directions are chosen.
        /// </summary>
        private void LayoutRingBlock(RingBlock block)
        {
            block.Rings = SmallestSetOfSmallestRings(block.Atoms);
            if (block.Rings.Any(r => r.Length >= MacrocycleSize))
            {
                block.Coords = EmbedMacrocycle(block);

                // Heavily fused macrocycles (e.g. porphyrins) can trap a ring on the wrong side; a plain embedding may do better.
                int problems = CoreProblems(block.Coords);
                if (problems > 0 && block.Rings.Count > 1)
                {
                    Dictionary<int, Point> flat = Embed2D(block.Atoms, block.Rings);
                    if (CoreProblems(flat) < problems)
                    {
                        block.Coords = flat;
                    }
                }
            }
            else
            {
                block.Coords = PeelAndEmbed(block);
            }
            PlaceRingSubstituents(block);
        }

        #region Ring perception

        // Horton's candidate cycles, accepted shortest first while they are independent over GF(2).
        private List<int[]> SmallestSetOfSmallestRings(int[] atoms)
        {
            HashSet<int> inBlock = new HashSet<int>(atoms);
            Dictionary<(int, int), int> edgeIndex = new Dictionary<(int, int), int>();
            foreach (int a in atoms)
            {
                foreach (int b in _g.Adj[a])
                {
                    if (a < b && inBlock.Contains(b))
                    {
                        edgeIndex[(a, b)] = edgeIndex.Count;
                    }
                }
            }

            int needed = edgeIndex.Count - atoms.Length + 1;
            List<int[]> candidates = new List<int[]>();
            foreach (int root in atoms)
            {
                Dictionary<int, int> parent = new Dictionary<int, int> { [root] = -1 };
                Queue<int> queue = new Queue<int>();
                queue.Enqueue(root);
                while (queue.Count > 0)
                {
                    int u = queue.Dequeue();
                    foreach (int v in _g.Adj[u].Where(inBlock.Contains))
                    {
                        if (!parent.ContainsKey(v))
                        {
                            parent[v] = u;
                            queue.Enqueue(v);
                        }
                    }
                }

                foreach ((int x, int y) in edgeIndex.Keys)
                {
                    if (parent[x] == y || parent[y] == x)
                    {
                        continue;
                    }

                    List<int> toX = PathToRoot(x, parent);
                    List<int> toY = PathToRoot(y, parent);
                    HashSet<int> onX = new HashSet<int>(toX);
                    if (toY.Take(toY.Count - 1).Any(onX.Contains))
                    {
                        continue;
                    }

                    toX.Reverse();
                    candidates.Add(toX.Concat(toY.Take(toY.Count - 1)).ToArray());
                }
            }

            int words = (edgeIndex.Count + 63) / 64;
            List<(ulong[] bits, int pivot)> basis = new List<(ulong[] bits, int pivot)>();
            List<int[]> rings = new List<int[]>();
            foreach (int[] cycle in candidates.OrderBy(c => c.Length))
            {
                if (rings.Count >= needed)
                {
                    break;
                }

                ulong[] bits = new ulong[words];
                for (int k = 0; k < cycle.Length; k++)
                {
                    int a = cycle[k], b = cycle[(k + 1) % cycle.Length];
                    int e = edgeIndex[(Math.Min(a, b), Math.Max(a, b))];
                    bits[e / 64] ^= 1UL << (e % 64);
                }

                foreach ((ulong[] row, int pivot) in basis)
                {
                    if ((bits[pivot / 64] & (1UL << (pivot % 64))) != 0)
                    {
                        for (int w = 0; w < words; w++)
                        {
                            bits[w] ^= row[w];
                        }
                    }
                }

                int lead = FirstSetBit(bits);
                if (lead >= 0)
                {
                    basis.Add((bits, lead));
                    rings.Add(cycle);
                }
            }
            return rings;
        }

        private static List<int> PathToRoot(int atom, Dictionary<int, int> parent)
        {
            List<int> path = new List<int>();
            for (int v = atom; v >= 0; v = parent[v])
            {
                path.Add(v);
            }
            return path;
        }

        private static int FirstSetBit(ulong[] bits)
        {
            for (int w = 0; w < bits.Length; w++)
            {
                if (bits[w] != 0)
                {
                    for (int b = 0; b < 64; b++)
                    {
                        if ((bits[w] & (1UL << b)) != 0)
                        {
                            return w * 64 + b;
                        }
                    }
                }
            }
            return -1;
        }

        #endregion Ring perception

        #region Peeling and fusion

        private Dictionary<int, Point> PeelAndEmbed(RingBlock block)
        {
            // Repeatedly remove a ring that shares exactly one bond with the rest.
            List<int[]> remaining = new List<int[]>(block.Rings);
            Stack<(int[] ring, int u, int v)> peeled = new Stack<(int[] ring, int u, int v)>();
            bool progress = true;
            while (remaining.Count > 1 && progress)
            {
                progress = false;
                for (int i = 0; i < remaining.Count; i++)
                {
                    HashSet<int> others = new HashSet<int>(remaining.Where((r, k) => k != i).SelectMany(r => r));
                    int[] shared = remaining[i].Where(others.Contains).ToArray();
                    if (shared.Length == 2 && _bonded[shared[0], shared[1]])
                    {
                        peeled.Push((remaining[i], shared[0], shared[1]));
                        remaining.RemoveAt(i);
                        progress = true;
                        break;
                    }
                }
            }

            Dictionary<int, Point> coords = remaining.Count == 1
                ? Polygon(remaining[0])
                : EmbedCore(remaining.SelectMany(r => r).Distinct().ToArray(), remaining);

            while (peeled.Count > 0)
            {
                (int[] ring, int u, int v) = peeled.Pop();
                FuseRing(coords, ring, u, v);
            }
            return coords;
        }

        private static Dictionary<int, Point> Polygon(int[] ring)
        {
            int n = ring.Length;
            double radius = 1 / (2 * Math.Sin(Math.PI / n));
            Dictionary<int, Point> coords = new Dictionary<int, Point>();
            for (int k = 0; k < n; k++)
            {
                coords[ring[k]] = GeometryHelper.Offset(new Point(), 2 * Math.PI * k / n, radius);
            }
            return coords;
        }

        // Regular polygon on whichever side of the shared bond u-v is less congested.
        private static void FuseRing(Dictionary<int, Point> coords, int[] ring, int u, int v)
        {
            int n = ring.Length;
            int start = Array.IndexOf(ring, u);
            int step = ring[(start + 1) % n] == v ? 1 : n - 1;
            int[] sequence = Enumerable.Range(0, n).Select(k => ring[(start + k * step) % n]).ToArray();

            double length = (coords[v] - coords[u]).Length;
            Dictionary<int, Point> best = null;
            double bestCongestion = double.MaxValue;
            foreach (int turn in new[] { 1, -1 })
            {
                Dictionary<int, Point> trial = new Dictionary<int, Point>();
                Point p = coords[v];
                double heading = GeometryHelper.Angle(coords[u], coords[v]);
                for (int k = 2; k < n; k++)
                {
                    heading += turn * 2 * Math.PI / n;
                    p = GeometryHelper.Offset(p, heading, length);
                    trial[sequence[k]] = p;
                }

                double congestion = trial.Values.Sum(t => coords.Values.Sum(c => 1 / Math.Max((t - c).LengthSquared, 1e-4)));
                if (congestion < bestCongestion)
                {
                    bestCongestion = congestion;
                    best = trial;
                }
            }

            foreach (KeyValuePair<int, Point> p in best)
            {
                coords[p.Key] = p.Value;
            }
        }

        #endregion Peeling and fusion

        #region Embedding

        private Dictionary<int, Point> EmbedCore(int[] core, List<int[]> rings)
        {
            HashSet<int> inCore = new HashSet<int>(core);
            bool cage = core.All(a => _g.Adj[a].Count(inCore.Contains) >= 3);

            Dictionary<int, Point> flat = cage ? null : Embed2D(core, rings);
            if (flat != null && CoreProblems(flat) == 0)
            {
                return flat;
            }

            Dictionary<int, Point> projected = Embed3D(core);
            return flat == null || CoreProblems(projected) < CoreProblems(flat) ? projected : flat;
        }

        /// <summary>
        /// Distance geometry: MDS on topological distances, then minimise Σ C(|xi-xj|² - dij²)² over bonds (C=3),
        /// ring 1-3 distances of the regular polygon and para distances in six-membered rings (C=1).
        /// </summary>
        private Dictionary<int, Point> Embed2D(int[] core, List<int[]> rings)
        {
            Dictionary<int, int> local = LocalIndex(core);
            double[][] p = EmbeddingHelper.ClassicalMds(TopologicalDistances(core), 2);

            List<DistanceTerm> terms = BondTerms(core, local);
            foreach (int[] ring in rings)
            {
                AddPolygonTerms(terms, ring, local);
            }

            EmbeddingHelper.MinimizeDistances(p, terms);
            EmbeddingHelper.ScaleToUnitBonds(p, terms);
            return ToPoints(core, p);
        }

        /// <summary>
        /// For cages: embed in 3D with ideal hybridisation angles, then project onto the plane that keeps the
        /// substituted atoms on the outside, tilting slightly to reduce overlaps and crossings.
        /// </summary>
        private Dictionary<int, Point> Embed3D(int[] core)
        {
            Dictionary<int, int> local = LocalIndex(core);
            HashSet<int> inCore = new HashSet<int>(core);
            double[][] p = EmbeddingHelper.ClassicalMds(TopologicalDistances(core), 3);

            List<DistanceTerm> terms = BondTerms(core, local);
            foreach (int centre in core)
            {
                int[] neighbours = _g.Adj[centre].Where(inCore.Contains).ToArray();
                double d2 = 2 - 2 * Math.Cos(IdealAngle(centre));
                for (int i = 0; i < neighbours.Length; i++)
                {
                    for (int k = i + 1; k < neighbours.Length; k++)
                    {
                        terms.Add(new DistanceTerm(local[neighbours[i]], local[neighbours[k]], d2, 1, false));
                    }
                }
            }
            EmbeddingHelper.MinimizeDistances(p, terms);

            double[] weight = core.Select(a => _g.Adj[a].Any(v => !inCore.Contains(v)) ? 5.0 : 1.0).ToArray();
            double[] centroid = new double[3];
            for (int i = 0; i < core.Length; i++)
            {
                for (int d = 0; d < 3; d++)
                {
                    centroid[d] += weight[i] * p[i][d] / weight.Sum();
                }
            }

            double[,] covariance = new double[3, 3];
            for (int i = 0; i < core.Length; i++)
            {
                for (int r = 0; r < 3; r++)
                {
                    for (int c = 0; c < 3; c++)
                    {
                        covariance[r, c] += weight[i] * (p[i][r] - centroid[r]) * (p[i][c] - centroid[c]);
                    }
                }
            }

            double[] e1 = EmbeddingHelper.TopEigenvector(covariance, out _, new List<double[]>());
            double[] e2 = EmbeddingHelper.TopEigenvector(covariance, out _, new List<double[]> { e1 });

            Dictionary<int, Point> best = null;
            (int problems, double spread) bestScore = (int.MaxValue, 0);
            double[] tilts = { 0, 10, -10, 20, -20 };
            foreach (double alpha in tilts)
            {
                foreach (double beta in tilts)
                {
                    double[] v = EmbeddingHelper.RotateAbout(e2, e1, alpha * GeometryHelper.Degrees);
                    double[] u = EmbeddingHelper.RotateAbout(e1, v, beta * GeometryHelper.Degrees);

                    double[][] flat = p.Select(q => new[] { EmbeddingHelper.Dot(EmbeddingHelper.Subtract(q, centroid), u), EmbeddingHelper.Dot(EmbeddingHelper.Subtract(q, centroid), v) }).ToArray();
                    double spread = Enumerable.Range(0, core.Length).Sum(i => weight[i] * (flat[i][0] * flat[i][0] + flat[i][1] * flat[i][1]));
                    EmbeddingHelper.ScaleToUnitBonds(flat, terms);

                    Dictionary<int, Point> coords = ToPoints(core, flat);
                    int problems = CoreProblems(coords, 0.35);
                    if (problems < bestScore.problems || (problems == bestScore.problems && spread > bestScore.spread + 1e-9))
                    {
                        bestScore = (problems, spread);
                        best = coords;
                    }
                }
            }
            return best;
        }

        private double IdealAngle(int atom)
        {
            double[] orders = _g.Order[atom];
            if (orders.Any(o => o >= 3) || orders.Count(o => o == 2) >= 2)
            {
                return Math.PI;
            }
            return orders.Any(o => o >= 1.5) ? 120 * GeometryHelper.Degrees : 109.47 * GeometryHelper.Degrees;
        }

        /// <summary>
        /// The macrocycle is mapped onto the outline of a cluster of hexagons, rotated so substituted atoms sit on
        /// the outside and heteroatoms on the inside. Other rings are embedded, attached at their shared atoms, and
        /// the whole block refined with 120° angles along the macrocycle and spacing terms that stop rings folding over it.
        /// </summary>
        private Dictionary<int, Point> EmbedMacrocycle(RingBlock block)
        {
            int[] macro = block.Rings.OrderByDescending(r => r.Length).First();
            HashSet<int> inMacro = new HashSet<int>(macro);
            int n = macro.Length;
            List<Point> outline = HoneycombOutline(n);
            Point centre = GeometryHelper.Centroid(outline);

            int bestOffset = 0, bestDirection = 1;
            double bestScore = double.MinValue;
            foreach (int direction in new[] { 1, -1 })
            {
                for (int offset = 0; offset < n; offset++)
                {
                    double score = 0;
                    for (int k = 0; k < n; k++)
                    {
                        int atom = macro[((offset + direction * k) % n + n) % n];
                        double r = (outline[k] - centre).Length;
                        if (_g.Adj[atom].Any(v => !inMacro.Contains(v)))
                        {
                            score += r;
                        }
                        if (_g.Symbol[atom] != "C")
                        {
                            score -= r;
                        }
                    }
                    if (score > bestScore + 1e-9)
                    {
                        bestScore = score;
                        bestOffset = offset;
                        bestDirection = direction;
                    }
                }
            }

            Dictionary<int, Point> coords = new Dictionary<int, Point>();
            for (int k = 0; k < n; k++)
            {
                coords[macro[((bestOffset + bestDirection * k) % n + n) % n]] = outline[k];
            }

            foreach (int[] group in GroupsOutside(block.Atoms.Where(a => !inMacro.Contains(a)).ToArray()))
            {
                AttachGroup(coords, group, macro, centre);
            }

            Dictionary<int, int> local = LocalIndex(block.Atoms);
            List<DistanceTerm> terms = BondTerms(block.Atoms, local);
            for (int k = 0; k < n; k++)
            {
                terms.Add(new DistanceTerm(local[macro[k]], local[macro[(k + 2) % n]], 3, 1, false));
            }
            foreach (int[] ring in block.Rings.Where(r => r != macro && r.Length < MacrocycleSize))
            {
                AddPolygonTerms(terms, ring, local);
            }

            double[,] topo = TopologicalDistances(block.Atoms);
            for (int i = 0; i < block.Atoms.Length; i++)
            {
                for (int j = i + 1; j < block.Atoms.Length; j++)
                {
                    if (topo[i, j] >= 3)
                    {
                        terms.Add(new DistanceTerm(i, j, 1, 1, true));
                    }
                }
            }

            double[][] p = block.Atoms.Select(a => new[] { coords[a].X, coords[a].Y }).ToArray();
            EmbeddingHelper.MinimizeDistances(p, terms);
            EmbeddingHelper.ScaleToUnitBonds(p, terms);
            return ToPoints(block.Atoms, p);
        }

        private List<int[]> GroupsOutside(int[] atoms)
        {
            HashSet<int> remaining = new HashSet<int>(atoms);
            List<int[]> groups = new List<int[]>();
            foreach (int start in atoms)
            {
                if (!remaining.Remove(start))
                {
                    continue;
                }

                List<int> group = new List<int> { start };
                for (int i = 0; i < group.Count; i++)
                {
                    foreach (int v in _g.Adj[group[i]].Where(remaining.Contains).ToList())
                    {
                        remaining.Remove(v);
                        group.Add(v);
                    }
                }
                groups.Add(group.ToArray());
            }
            return groups;
        }

        // Embed a group of non-macrocycle ring atoms with its attachment atoms, then fit it onto them on the less crowded side.
        private void AttachGroup(Dictionary<int, Point> coords, int[] group, int[] macro, Point centre)
        {
            int[] attach = macro.Where(m => _g.Adj[m].Any(group.Contains)).ToArray();
            int[] atoms = group.Concat(attach).ToArray();
            double[][] p = EmbeddingHelper.ClassicalMds(TopologicalDistances(atoms), 2);
            EmbeddingHelper.ScaleToUnitBonds(p, BondTerms(atoms, LocalIndex(atoms)));

            Point[] source = p.Select(q => new Point(q[0], q[1])).ToArray();
            Dictionary<int, Point> best = null;
            double bestCongestion = double.MaxValue;
            foreach (int reflect in new[] { 1, -1 })
            {
                Point[] s = source.Select(q => new Point(q.X, reflect * q.Y)).ToArray();
                Func<Point, Point> transform;
                if (attach.Length >= 2)
                {
                    transform = Procrustes(s.Skip(group.Length).ToArray(), attach.Select(a => coords[a]).ToArray());
                }
                else
                {
                    Point anchor = s[group.Length];
                    Point groupCentre = GeometryHelper.Centroid(s.Take(group.Length));
                    double turn = GeometryHelper.Angle(centre, coords[attach[0]]) - GeometryHelper.Angle(anchor, groupCentre);
                    Point target = coords[attach[0]];
                    transform = q => GeometryHelper.Offset(target, GeometryHelper.Angle(anchor, q) + turn, (q - anchor).Length);
                }

                Dictionary<int, Point> placed = new Dictionary<int, Point>();
                for (int k = 0; k < group.Length; k++)
                {
                    placed[group[k]] = transform(s[k]);
                }

                double congestion = placed.Values.Sum(q => coords.Values.Sum(c => 1 / Math.Max((q - c).LengthSquared, 1e-4)));
                if (congestion < bestCongestion)
                {
                    bestCongestion = congestion;
                    best = placed;
                }
            }

            foreach (KeyValuePair<int, Point> q in best)
            {
                coords[q.Key] = q.Value;
            }
        }

        // Rigid rotation + translation taking 'source' as close as possible to 'target' (least squares).
        private static Func<Point, Point> Procrustes(Point[] source, Point[] target)
        {
            Point cs = GeometryHelper.Centroid(source), ct = GeometryHelper.Centroid(target);
            double a = 0, b = 0;
            for (int i = 0; i < source.Length; i++)
            {
                Vector s = source[i] - cs, t = target[i] - ct;
                a += s.X * t.X + s.Y * t.Y;
                b += s.X * t.Y - s.Y * t.X;
            }

            double theta = Math.Atan2(b, a), c = Math.Cos(theta), sn = Math.Sin(theta);
            return q => new Point(ct.X + c * (q.X - cs.X) - sn * (q.Y - cs.Y),
                                  ct.Y + sn * (q.X - cs.X) + c * (q.Y - cs.Y));
        }

        /// <summary>
        /// Perimeter (unit edges, in order) of a compact cluster of hexagons with 'size' vertices; grown one hexagon at
        /// a time, and trimmed when no cluster has exactly that perimeter (odd sizes in particular).
        /// </summary>
        private static List<Point> HoneycombOutline(int size)
        {
            int[,] around = { { 1, 0 }, { 1, -1 }, { 0, -1 }, { -1, 0 }, { -1, 1 }, { 0, 1 } };
            HashSet<(int q, int r)> cells = new HashSet<(int q, int r)> { (0, 0) };
            int perimeter = 6;

            while (perimeter < size)
            {
                (int q, int r) best = (0, 0);
                int bestGain = int.MaxValue;
                double bestDistance = double.MaxValue;
                double cx = cells.Average(c => HexCentre(c).X), cy = cells.Average(c => HexCentre(c).Y);

                foreach ((int q, int r) cell in cells)
                {
                    for (int k = 0; k < 6; k++)
                    {
                        (int q, int r) candidate = (cell.q + around[k, 0], cell.r + around[k, 1]);
                        if (cells.Contains(candidate))
                        {
                            continue;
                        }

                        bool[] touching = Enumerable.Range(0, 6).Select(j => cells.Contains((candidate.q + around[j, 0], candidate.r + around[j, 1]))).ToArray();
                        int runs = Enumerable.Range(0, 6).Count(j => touching[j] && !touching[(j + 5) % 6]);
                        if (runs != 1)
                        {
                            continue;
                        }

                        int gain = 6 - 2 * touching.Count(t => t);
                        if (gain <= 0)
                        {
                            continue;
                        }

                        // Prefer not to overshoot, then the most compact addition.
                        int rank = (gain > size - perimeter ? 100 : 0) + gain;
                        Point centre = HexCentre(candidate);
                        double distance = (centre.X - cx) * (centre.X - cx) + (centre.Y - cy) * (centre.Y - cy);
                        if (rank < bestGain || (rank == bestGain && distance < bestDistance - 1e-9))
                        {
                            bestGain = rank;
                            bestDistance = distance;
                            best = candidate;
                        }
                    }
                }

                cells.Add(best);
                perimeter += bestGain % 100;
            }

            // Edges belonging to exactly one hexagon form the outline.
            Dictionary<(long, long), int> edgeCount = new Dictionary<(long, long), int>();
            Dictionary<long, Point> vertex = new Dictionary<long, Point>();
            foreach ((int q, int r) cell in cells)
            {
                Point c = HexCentre(cell);
                long[] keys = new long[6];
                for (int k = 0; k < 6; k++)
                {
                    Point v = GeometryHelper.Offset(c, (30 + 60 * k) * GeometryHelper.Degrees, 1);
                    keys[k] = (long)Math.Round(v.X * 1000) * 1000003 + (long)Math.Round(v.Y * 1000);
                    vertex[keys[k]] = v;
                }
                for (int k = 0; k < 6; k++)
                {
                    (long, long) edge = (Math.Min(keys[k], keys[(k + 1) % 6]), Math.Max(keys[k], keys[(k + 1) % 6]));
                    edgeCount[edge] = edgeCount.TryGetValue(edge, out int count) ? count + 1 : 1;
                }
            }

            Dictionary<long, List<long>> boundary = new Dictionary<long, List<long>>();
            foreach (KeyValuePair<(long, long), int> e in edgeCount.Where(e => e.Value == 1))
            {
                AddLink(e.Key.Item1, e.Key.Item2);
                AddLink(e.Key.Item2, e.Key.Item1);
            }

            List<Point> outline = new List<Point>();
            long first = boundary.Keys.First(), previous = long.MinValue, current = first;
            do
            {
                outline.Add(vertex[current]);
                long next = boundary[current].First(k => k != previous);
                previous = current;
                current = next;
            }
            while (current != first);

            return outline.Take(size).ToList();

            // Local function
            void AddLink(long from, long to)
            {
                if (!boundary.TryGetValue(from, out List<long> list))
                {
                    boundary[from] = list = new List<long>();
                }
                list.Add(to);
            }
        }

        private static Point HexCentre((int q, int r) cell) => new Point(Math.Sqrt(3) * (cell.q + cell.r / 2.0), 1.5 * cell.r);

        #endregion Embedding

        #region Substituents

        // Exocyclic bonds go into the gaps outside the rings; larger branches take the less crowded slots.
        private void PlaceRingSubstituents(RingBlock block)
        {
            Dictionary<int, Point> coords = block.Coords;
            List<(int[] ring, Point centre)> centres = block.Rings.Select(r => (r, GeometryHelper.Centroid(r.Select(a => coords[a])))).ToList();

            foreach (int atom in block.Atoms)
            {
                int[] neighbours = _g.Adj[atom];
                double[] angles = new double[neighbours.Length];
                List<int> outside = new List<int>();
                for (int k = 0; k < neighbours.Length; k++)
                {
                    if (coords.ContainsKey(neighbours[k]))
                    {
                        angles[k] = GeometryHelper.Angle(coords[atom], coords[neighbours[k]]);
                    }
                    else
                    {
                        outside.Add(k);
                    }
                }

                if (outside.Count > 0)
                {
                    double[] taken = Enumerable.Range(0, neighbours.Length).Where(k => !outside.Contains(k)).Select(k => angles[k]).ToArray();
                    IEnumerable<double> inside = centres.Where(c => c.ring.Contains(atom)).Select(c => GeometryHelper.Angle(coords[atom], c.centre));
                    double[] slots = GeometryHelper.SpreadIntoGaps(taken, outside.Count, inside);

                    List<int> bySize = outside.OrderByDescending(k => SideOf(neighbours[k], atom).Count).ToList();
                    List<double> byRoom = slots.OrderBy(t => SlotCongestion(coords, atom, t)).ToList();
                    for (int i = 0; i < bySize.Count; i++)
                    {
                        angles[bySize[i]] = byRoom[i];
                    }
                }

                block.Angles[atom] = angles;
            }
        }

        private static double SlotCongestion(Dictionary<int, Point> coords, int atom, double angle)
        {
            Point p = GeometryHelper.Offset(coords[atom], angle, 1);
            return coords.Where(c => c.Key != atom).Sum(c => 1 / Math.Max((p - c.Value).LengthSquared, 1e-4));
        }

        #endregion Substituents

        #region Helpers

        // Bond lengths off by more than 25%, atoms closer than 'minimum', and crossing bonds.
        private int CoreProblems(Dictionary<int, Point> coords, double minimum = CloseContact)
        {
            int[] atoms = coords.Keys.ToArray();
            List<(int a, int b)> bonds = _bonds.Where(b => coords.ContainsKey(b.a) && coords.ContainsKey(b.b)).ToList();
            int problems = bonds.Count(b => Math.Abs((coords[b.a] - coords[b.b]).Length - 1) > 0.25);

            for (int i = 0; i < atoms.Length; i++)
            {
                for (int j = i + 1; j < atoms.Length; j++)
                {
                    if (!_bonded[atoms[i], atoms[j]] && (coords[atoms[i]] - coords[atoms[j]]).Length < minimum)
                    {
                        problems++;
                    }
                }
            }

            double[] x = new double[_n], y = new double[_n];
            foreach (KeyValuePair<int, Point> p in coords)
            {
                x[p.Key] = p.Value.X;
                y[p.Key] = p.Value.Y;
            }
            for (int p = 0; p < bonds.Count; p++)
            {
                for (int q = p + 1; q < bonds.Count; q++)
                {
                    (int a, int b) = bonds[p];
                    (int c, int d) = bonds[q];
                    if (a != c && a != d && b != c && b != d && SegmentsCross(x, y, a, b, c, d))
                    {
                        problems++;
                    }
                }
            }
            return problems;
        }

        private static Dictionary<int, int> LocalIndex(int[] atoms)
        {
            Dictionary<int, int> local = new Dictionary<int, int>();
            for (int k = 0; k < atoms.Length; k++)
            {
                local[atoms[k]] = k;
            }
            return local;
        }

        private static Dictionary<int, Point> ToPoints(int[] atoms, double[][] p)
        {
            Dictionary<int, Point> coords = new Dictionary<int, Point>();
            for (int k = 0; k < atoms.Length; k++)
            {
                coords[atoms[k]] = new Point(p[k][0], p[k][1]);
            }
            return coords;
        }

        private List<DistanceTerm> BondTerms(int[] atoms, Dictionary<int, int> local)
        {
            List<DistanceTerm> terms = new List<DistanceTerm>();
            foreach (int a in atoms)
            {
                foreach (int b in _g.Adj[a])
                {
                    if (a < b && local.ContainsKey(b))
                    {
                        terms.Add(new DistanceTerm(local[a], local[b], 1, 3, false, true));
                    }
                }
            }
            return terms;
        }

        // 1-3 distances of a regular polygon, plus the para distance in six-membered rings.
        private static void AddPolygonTerms(List<DistanceTerm> terms, int[] ring, Dictionary<int, int> local)
        {
            int n = ring.Length;
            double d2 = 2 + 2 * Math.Cos(2 * Math.PI / n);
            for (int k = 0; k < n; k++)
            {
                terms.Add(new DistanceTerm(local[ring[k]], local[ring[(k + 2) % n]], d2, 1, false));
            }
            if (n == 6)
            {
                for (int k = 0; k < 3; k++)
                {
                    terms.Add(new DistanceTerm(local[ring[k]], local[ring[k + 3]], 4, 1, false));
                }
            }
        }

        // Bond counts between the given atoms, through those atoms only.
        private double[,] TopologicalDistances(int[] atoms)
        {
            Dictionary<int, int> local = LocalIndex(atoms);
            int m = atoms.Length;
            double[,] d = new double[m, m];
            for (int s = 0; s < m; s++)
            {
                int[] dist = Enumerable.Repeat(-1, m).ToArray();
                dist[s] = 0;
                Queue<int> queue = new Queue<int>();
                queue.Enqueue(s);
                while (queue.Count > 0)
                {
                    int u = queue.Dequeue();
                    foreach (int v in _g.Adj[atoms[u]])
                    {
                        if (local.TryGetValue(v, out int lv) && dist[lv] < 0)
                        {
                            dist[lv] = dist[u] + 1;
                            queue.Enqueue(lv);
                        }
                    }
                }
                for (int t = 0; t < m; t++)
                {
                    d[s, t] = dist[t] < 0 ? m : dist[t];
                }
            }
            return d;
        }

        #endregion Helpers
    }
}
