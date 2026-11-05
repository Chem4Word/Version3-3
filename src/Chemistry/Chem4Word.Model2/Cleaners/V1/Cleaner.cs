// ---------------------------------------------------------------------------
//  Copyright (c) 2026, The .NET Foundation.
//  This software is released under the Apache Licence, Version 2.0.
//  The licence and further copyright text can be found in the file LICENCE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace Chem4Word.Model2.Cleaners.V1
{
    /// <summary>
    /// Generates fresh 2D coordinates for a Chem4Word model from its connection table alone:
    ///
    /// 1. classical MDS on topological distances for a reasonable starting shape,
    /// 2. stress majorization to untangle it,
    /// 3. a small distance-based force field (bond lengths, ideal angles via 1-3 distances,
    ///    regular ring polygons, non-bonded repulsion),
    /// 4. orientation tidy-up and exact rescaling so the median bond equals the target length.
    /// </summary>
    public static class Cleaner
    {
        private const int MaxRingSize = 24;

        // Default bond length, used when the model has no usable bond length (e.g. every atom at the origin).
        public const double DefaultBondLength = 20;

        private const double MinimumUsableBondLength = 1e-3;

        /// <summary>
        /// Re-lays out every molecule in the model, keeping each one centred on its original centroid.
        /// </summary>
        /// <param name="model">The input model</param>
        /// <param name="bondLength">Target bond length; defaults to the model's current mean bond length.</param>
        /// <returns>The bond length that was used.</returns>
        public static double Clean(Model model, double? bondLength = null)
        {
            if (bondLength.HasValue && bondLength.Value <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(bondLength), "Bond length must be positive.");
            }

            double length = bondLength ?? model.MeanBondLength;
            if (double.IsNaN(length) || length < MinimumUsableBondLength)
            {
                length = DefaultBondLength;
            }

            ElectronPusherTracker pushers = ElectronPusherTracker.Capture(model);
            foreach (Molecule molecule in model.Molecules.Values.ToList())
            {
                CleanMolecule(molecule, length);
            }
            pushers.Restore();

            return length;
        }

        private static void CleanMolecule(Molecule molecule, double bondLength)
        {
            // Include child molecules, which is how SdFileConverter stores disconnected fragments.
            List<Atom> atoms = new List<Atom>();
            molecule.BuildAtomList(atoms);
            if (atoms.Count == 0)
            {
                return;
            }

            List<Bond> bonds = new List<Bond>();
            molecule.BuildBondList(bonds);

            Point centroid = molecule.Centroid;
            Point[] layout = Layout(atoms, bonds, bondLength);

            double centreX = (layout.Min(p => p.X) + layout.Max(p => p.X)) / 2;
            double centreY = (layout.Min(p => p.Y) + layout.Max(p => p.Y)) / 2;
            for (int i = 0; i < atoms.Count; i++)
            {
                atoms[i].Position = new Point(layout[i].X - centreX + centroid.X,
                                              layout[i].Y - centreY + centroid.Y);
            }
        }

        // New positions (Chem4Word orientation, Y down) for the given atoms, in the same order.
        private static Point[] Layout(List<Atom> atoms, List<Bond> bonds, double bondLength)
        {
            int n = atoms.Count;
            Dictionary<Atom, int> index = new Dictionary<Atom, int>();
            for (int i = 0; i < n; i++)
            {
                index[atoms[i]] = i;
            }

            List<int>[] adj = Enumerable.Range(0, n).Select(_ => new List<int>()).ToArray();
            Dictionary<(int, int), double> order = new Dictionary<(int, int), double>();

            foreach (Bond bond in bonds)
            {
                if (!index.TryGetValue(bond.StartAtom, out int a) || !index.TryGetValue(bond.EndAtom, out int b) || a == b)
                {
                    continue;
                }

                (int, int) key = (Math.Min(a, b), Math.Max(a, b));
                if (order.ContainsKey(key))
                {
                    continue;
                }

                order[key] = bond.OrderValue ?? 1;
                adj[a].Add(b);
                adj[b].Add(a);
            }

            // Lay out each fragment separately, then place them side by side.
            Point[] result = new Point[n];
            double offsetX = 0;
            foreach (int[] comp in Components(adj))
            {
                (double[] x, double[] y) = LayoutComponent(comp, adj, order, bondLength);

                double minX = x.Min(), minY = y.Min(), maxX = x.Max();
                for (int k = 0; k < comp.Length; k++)
                {
                    // The layout is computed Y up; Chem4Word stores Y pointing down.
                    result[comp[k]] = new Point(x[k] - minX + offsetX, -(y[k] - minY));
                }

                offsetX += maxX - minX + 2 * bondLength;
            }

            return result;
        }

        private static (double[] x, double[] y) LayoutComponent(
            int[] atoms, List<int>[] globalAdj, Dictionary<(int, int), double> order, double l)
        {
            int m = atoms.Length;
            double[] x = new double[m];
            double[] y = new double[m];
            if (m == 1)
            {
                return (x, y);
            }
            if (m == 2)
            {
                x[1] = l;
                return (x, y);
            }

            Dictionary<int, int> local = new Dictionary<int, int>();
            for (int k = 0; k < m; k++)
            {
                local[atoms[k]] = k;
            }

            int[][] adj = atoms.Select(a => globalAdj[a].Select(g => local[g]).ToArray()).ToArray();

            int[,] topo = AllPairsShortestPaths(adj);
            double[,] target = new double[m, m];
            for (int i = 0; i < m; i++)
            {
                for (int j = 0; j < m; j++)
                {
                    target[i, j] = l * ZigZagDistance(topo[i, j]);
                }
            }

            ClassicalMds(target, x, y);
            StressMajorization(target, x, y, 300);
            ScaleToMedianBond(adj, x, y, l);

            List<Term> terms = BuildTerms(adj, topo, BondOrder, l, out List<int[]> rings);
            Refine(terms, x, y, 6000);

            // Rings trapped in a folded shape are a local minimum; snap them back to a polygon and re-relax.
            for (int attempt = 0; attempt < 5; attempt++)
            {
                List<int[]> folded = rings.Where(r => !IsConvex(r, x, y)).ToList();
                if (folded.Count == 0)
                {
                    break;
                }

                foreach (int[] r in folded)
                {
                    SnapToPolygon(r, x, y, l);
                }
                Refine(terms, x, y, 6000);
            }

            TidyOrientation(adj, x, y, rings);
            ScaleToMedianBond(adj, x, y, l);
            return (x, y);

            // Local function
            double BondOrder(int i, int j)
            {
                return order[(Math.Min(atoms[i], atoms[j]), Math.Max(atoms[i], atoms[j]))];
            }
        }

        // Distance between atoms k bonds apart on an ideal 120° zig-zag chain, in bond-length units.
        private static double ZigZagDistance(int k)
        {
            if (k <= 0)
            {
                return 0;
            }

            double h = k * Math.Sqrt(3) / 2;
            return k % 2 == 0 ? h : Math.Sqrt(h * h + 0.25);
        }

        private static List<Term> BuildTerms(int[][] adj, int[,] topo, Func<int, int, double> bondOrder, double l, out List<int[]> ringList)
        {
            int m = adj.Length;
            List<Term> terms = new List<Term>();

            for (int i = 0; i < m; i++)
            {
                foreach (int j in adj[i])
                {
                    if (i < j)
                    {
                        terms.Add(new Term(i, j, l, 4.0, false));
                    }
                }
            }

            Dictionary<string, int[]> rings = new Dictionary<string, int[]>();
            for (int b = 0; b < m; b++)
            {
                foreach ((int a, int c, double angle, bool lowerBound) in IdealAngles(b, adj, bondOrder, rings))
                {
                    terms.Add(new Term(a, c, 2 * l * Math.Sin(angle / 2), 0.5, lowerBound));
                }
            }

            // Every pair across a perceived ring gets its regular-polygon chord length, which stops rings
            // settling into folded "bow-tie" shapes that satisfy bond and angle terms alone.
            foreach (int[] ring in rings.Values)
            {
                int n = ring.Length;
                double radius = l / (2 * Math.Sin(Math.PI / n));
                for (int p = 0; p < n; p++)
                {
                    for (int q = p + 2; q < n; q++)
                    {
                        int k = Math.Min(q - p, n - (q - p));
                        if (k >= 2)
                        {
                            terms.Add(new Term(ring[p], ring[q], 2 * radius * Math.Sin(Math.PI * k / n), 0.3, false));
                        }
                    }
                }
            }

            for (int i = 0; i < m; i++)
            {
                for (int j = i + 1; j < m; j++)
                {
                    if (topo[i, j] >= 3)
                    {
                        terms.Add(new Term(i, j, l, 0.3, true));
                    }
                }
            }

            ringList = rings.Values.ToList();
            return terms;
        }

        /// <summary>
        /// Ideal angles (radians) between pairs of neighbours of atom b. Pairs that close a small ring get the
        /// regular-polygon interior angle; the remaining pairs share what is left of 360°.
        /// </summary>
        private static IEnumerable<(int a, int c, double angle, bool lowerBound)> IdealAngles(
            int b, int[][] adj, Func<int, int, double> bondOrder, Dictionary<string, int[]> rings)
        {
            int[] nb = adj[b];
            int deg = nb.Length;
            if (deg < 2)
            {
                yield break;
            }

            List<(int a, int c, int ring)> pairs = new List<(int a, int c, int ring)>();
            Dictionary<(int, int), int[]> ringPaths = new Dictionary<(int, int), int[]>();
            for (int p = 0; p < deg; p++)
            {
                for (int q = p + 1; q < deg; q++)
                {
                    int[] path = SmallestRingThrough(nb[p], b, nb[q], adj);
                    if (path != null)
                    {
                        ringPaths[(nb[p], nb[q])] = path;
                    }
                    pairs.Add((nb[p], nb[q], path?.Length ?? 0));
                }
            }

            // Pick the smallest rings first; each neighbour may sit in at most two chosen ring angles,
            // and "envelope" rings around fused systems (e.g. naphthalene's 10-ring) are skipped.
            Dictionary<(int, int), double> chosen = new Dictionary<(int, int), double>();
            Dictionary<int, int> usage = nb.ToDictionary(v => v, v => 0);
            foreach ((int a, int c, int ring) pr in pairs.Where(p => p.ring > 0).OrderBy(p => p.ring))
            {
                if (deg == 3 && chosen.Count == 2)
                {
                    break;
                }
                if (usage[pr.a] >= 2 || usage[pr.c] >= 2)
                {
                    continue;
                }
                if (nb.Any(d => IsChosen(chosen, pr.a, d) && IsChosen(chosen, d, pr.c)))
                {
                    continue;
                }

                chosen[(pr.a, pr.c)] = Math.PI * (pr.ring - 2) / pr.ring;
                int[] ring = ringPaths[(pr.a, pr.c)];
                string key = string.Join(",", ring.OrderBy(v => v));
                if (!rings.ContainsKey(key))
                {
                    rings[key] = ring;
                }
                usage[pr.a]++;
                usage[pr.c]++;
            }

            if (deg == 2)
            {
                (int a, int c, _) = pairs[0];
                double angle = chosen.TryGetValue((a, c), out double r) ? r
                    : IsLinear(b, a, c, bondOrder) ? Math.PI
                    : 2 * Math.PI / 3;
                yield return (a, c, angle, false);
                yield break;
            }

            if (deg == 3)
            {
                List<(int a, int c, int ring)> rest = pairs.Where(p => !chosen.ContainsKey((p.a, p.c))).ToList();
                double leftover = rest.Count == 0 ? 0 : (2 * Math.PI - chosen.Values.Sum()) / rest.Count;
                leftover = Math.Max(leftover, Math.PI / 3);
                foreach ((int a, int c, int ring) p in pairs)
                {
                    yield return (p.a, p.c, chosen.TryGetValue((p.a, p.c), out double r) ? r : leftover, false);
                }
                yield break;
            }

            // Four or more neighbours: keep ring angles exact, just keep the others apart.
            double minAngle = 2 * Math.PI / deg;
            foreach ((int a, int c, int ring) p in pairs)
            {
                yield return chosen.TryGetValue((p.a, p.c), out double r)
                    ? (p.a, p.c, r, false)
                    : (p.a, p.c, minAngle, true);
            }
        }

        private static bool IsChosen(Dictionary<(int, int), double> chosen, int u, int v) =>
            chosen.ContainsKey((u, v)) || chosen.ContainsKey((v, u));

        private static bool IsLinear(int b, int a, int c, Func<int, int, double> bondOrder)
        {
            double o1 = bondOrder(a, b), o2 = bondOrder(b, c);
            return o1 >= 3 || o2 >= 3 || (o1 == 2 && o2 == 2);
        }

        // Atoms of the smallest ring containing a-b-c in ring order, or null if none (BFS from a to c avoiding b).
        private static int[] SmallestRingThrough(int a, int b, int c, int[][] adj)
        {
            Dictionary<int, int> parent = new Dictionary<int, int> { [a] = -1 };
            Dictionary<int, int> depth = new Dictionary<int, int> { [a] = 0 };
            Queue<int> queue = new Queue<int>();
            queue.Enqueue(a);
            while (queue.Count > 0)
            {
                int u = queue.Dequeue();
                if (depth[u] + 3 > MaxRingSize)
                {
                    break;
                }

                foreach (int v in adj[u])
                {
                    if (v == b || parent.ContainsKey(v))
                    {
                        continue;
                    }

                    parent[v] = u;
                    depth[v] = depth[u] + 1;
                    if (v == c)
                    {
                        List<int> ring = new List<int> { b };
                        for (int w = c; w != -1; w = parent[w])
                        {
                            ring.Add(w);
                        }
                        return ring.ToArray();
                    }
                    queue.Enqueue(v);
                }
            }
            return null;
        }

        private static void ClassicalMds(double[,] d, double[] x, double[] y)
        {
            int m = x.Length;
            double[,] B = new double[m, m];
            double[] rowMean = new double[m];
            double grand = 0;
            for (int i = 0; i < m; i++)
            {
                for (int j = 0; j < m; j++)
                {
                    rowMean[i] += d[i, j] * d[i, j];
                }
                grand += rowMean[i];
                rowMean[i] /= m;
            }
            grand /= (double)m * m;

            for (int i = 0; i < m; i++)
            {
                for (int j = 0; j < m; j++)
                {
                    B[i, j] = -0.5 * (d[i, j] * d[i, j] - rowMean[i] - rowMean[j] + grand);
                }
            }

            double[] v1 = TopEigenvector(B, out double l1, null);
            double[] v2 = TopEigenvector(B, out double l2, v1);
            double s1 = Math.Sqrt(Math.Max(l1, 1e-6)), s2 = Math.Sqrt(Math.Max(l2, 1e-6));
            for (int i = 0; i < m; i++)
            {
                // Tiny deterministic jitter so nothing starts exactly on top of anything else.
                x[i] = v1[i] * s1 + 1e-3 * Math.Sin(i * 2.3 + 0.4);
                y[i] = v2[i] * s2 + 1e-3 * Math.Cos(i * 1.7 + 0.9);
            }
        }

        // Power iteration on a shifted matrix (so the most positive eigenvalue dominates), optionally orthogonal to 'exclude'.
        private static double[] TopEigenvector(double[,] b, out double eigenvalue, double[] exclude)
        {
            int m = b.GetLength(0);
            double shift = 0;
            for (int i = 0; i < m; i++)
            {
                double row = 0;
                for (int j = 0; j < m; j++)
                {
                    row += Math.Abs(b[i, j]);
                }
                shift = Math.Max(shift, row);
            }

            double[] v = Enumerable.Range(0, m).Select(i => Math.Sin(i * 1.31 + 0.7) + 0.01 * i).ToArray();
            double[] w = new double[m];
            for (int iter = 0; iter < 1000; iter++)
            {
                if (exclude != null)
                {
                    Orthogonalize(v, exclude);
                }
                Normalize(v);
                for (int i = 0; i < m; i++)
                {
                    double s = shift * v[i];
                    for (int j = 0; j < m; j++)
                    {
                        s += b[i, j] * v[j];
                    }
                    w[i] = s;
                }
                (v, w) = (w, v);
            }
            if (exclude != null)
            {
                Orthogonalize(v, exclude);
            }
            Normalize(v);

            eigenvalue = 0;
            for (int i = 0; i < m; i++)
            {
                for (int j = 0; j < m; j++)
                {
                    eigenvalue += v[i] * b[i, j] * v[j];
                }
            }
            return v;
        }

        private static void Orthogonalize(double[] v, double[] u)
        {
            double dot = 0;
            for (int i = 0; i < v.Length; i++)
            {
                dot += v[i] * u[i];
            }
            for (int i = 0; i < v.Length; i++)
            {
                v[i] -= dot * u[i];
            }
        }

        private static void Normalize(double[] v)
        {
            double norm = Math.Sqrt(v.Sum(t => t * t));
            if (norm < 1e-12)
            {
                return;
            }
            for (int i = 0; i < v.Length; i++)
            {
                v[i] /= norm;
            }
        }

        // Localized stress majorization (Gansner, Koren & North) with weights 1/d².
        private static void StressMajorization(double[,] d, double[] x, double[] y, int sweeps)
        {
            int m = x.Length;
            for (int s = 0; s < sweeps; s++)
            {
                for (int i = 0; i < m; i++)
                {
                    double nx = 0, ny = 0, wsum = 0;
                    for (int j = 0; j < m; j++)
                    {
                        if (i == j || d[i, j] <= 0)
                        {
                            continue;
                        }

                        double w = 1.0 / (d[i, j] * d[i, j]);
                        double dx = x[i] - x[j], dy = y[i] - y[j];
                        double r = Math.Max(Math.Sqrt(dx * dx + dy * dy), 1e-9);
                        nx += w * (x[j] + d[i, j] * dx / r);
                        ny += w * (y[j] + d[i, j] * dy / r);
                        wsum += w;
                    }
                    if (wsum > 0)
                    {
                        x[i] = nx / wsum;
                        y[i] = ny / wsum;
                    }
                }
            }
        }

        // Gradient descent on E = Σ w (r - target)², lower-bound terms only active when r < target.
        private static void Refine(List<Term> terms, double[] x, double[] y, int iterations)
        {
            int m = x.Length;
            double[] gx = new double[m];
            double[] gy = new double[m];
            const double step = 0.02;

            for (int iter = 0; iter < iterations; iter++)
            {
                Array.Clear(gx, 0, m);
                Array.Clear(gy, 0, m);
                foreach (Term t in terms)
                {
                    double dx = x[t.I] - x[t.J], dy = y[t.I] - y[t.J];
                    double r = Math.Sqrt(dx * dx + dy * dy);
                    if (r < 1e-9)
                    {
                        dx = 1e-3;
                        dy = 1e-3 * (t.I - t.J);
                        r = Math.Sqrt(dx * dx + dy * dy);
                    }

                    double diff = r - t.Target;
                    if (t.LowerBoundOnly && diff >= 0)
                    {
                        continue;
                    }

                    double f = 2 * t.Weight * diff / r;
                    gx[t.I] += f * dx;
                    gy[t.I] += f * dy;
                    gx[t.J] -= f * dx;
                    gy[t.J] -= f * dy;
                }

                double maxMove = 0;
                for (int i = 0; i < m; i++)
                {
                    x[i] -= step * gx[i];
                    y[i] -= step * gy[i];
                    maxMove = Math.Max(maxMove, step * (Math.Abs(gx[i]) + Math.Abs(gy[i])));
                }
                if (maxMove < 1e-8)
                {
                    break;
                }
            }
        }

        private static bool IsConvex(int[] ring, double[] x, double[] y)
        {
            int n = ring.Length, sign = 0;
            for (int k = 0; k < n; k++)
            {
                int a = ring[k], b = ring[(k + 1) % n], c = ring[(k + 2) % n];
                double cross = (x[b] - x[a]) * (y[c] - y[b]) - (y[b] - y[a]) * (x[c] - x[b]);
                int sg = Math.Sign(Math.Round(cross, 6));
                if (sg == 0)
                {
                    continue;
                }
                if (sign == 0)
                {
                    sign = sg;
                }
                else if (sg != sign)
                {
                    return false;
                }
            }
            return true;
        }

        private static void SnapToPolygon(int[] ring, double[] x, double[] y, double l)
        {
            int n = ring.Length;
            double cx = ring.Average(v => x[v]), cy = ring.Average(v => y[v]);
            double area = 0;
            for (int k = 0; k < n; k++)
            {
                int a = ring[k], b = ring[(k + 1) % n];
                area += x[a] * y[b] - x[b] * y[a];
            }

            double dir = area < 0 ? -1 : 1;
            double radius = l / (2 * Math.Sin(Math.PI / n));
            double start = Math.Atan2(y[ring[0]] - cy, x[ring[0]] - cx);
            for (int k = 0; k < n; k++)
            {
                double t = start + dir * 2 * Math.PI * k / n;
                x[ring[k]] = cx + radius * Math.Cos(t);
                y[ring[k]] = cy + radius * Math.Sin(t);
            }
        }

        // Rotate so the long axis is horizontal, then put six-membered rings on Set 1/Set 2 if there are any,
        // otherwise nudge so bonds sit on the 30° grid where possible.
        private static void TidyOrientation(int[][] adj, double[] x, double[] y, List<int[]> rings)
        {
            int m = x.Length;
            double cx = x.Average(), cy = y.Average();
            double sxx = 0, syy = 0, sxy = 0;
            for (int i = 0; i < m; i++)
            {
                double dx = x[i] - cx, dy = y[i] - cy;
                sxx += dx * dx;
                syy += dy * dy;
                sxy += dx * dy;
            }
            Rotate(x, y, cx, cy, -0.5 * Math.Atan2(2 * sxy, sxx - syy));

            double? hexagonRotation = HexagonRotation(rings, x, y);
            if (hexagonRotation.HasValue)
            {
                Rotate(x, y, cx, cy, hexagonRotation.Value);
                return;
            }

            const double grid = Math.PI / 6;
            List<double> bondAngles = new List<double>();
            for (int i = 0; i < m; i++)
            {
                foreach (int j in adj[i])
                {
                    if (i < j)
                    {
                        bondAngles.Add(Math.Atan2(y[j] - y[i], x[j] - x[i]));
                    }
                }
            }
            if (bondAngles.Count == 0)
            {
                return;
            }

            double best = 0, bestCost = double.MaxValue;
            foreach (double candidate in bondAngles.Select(a => -Residual(a)))
            {
                double cost = bondAngles.Sum(a => Math.Pow(Residual(a + candidate), 2));
                if (cost < bestCost)
                {
                    bestCost = cost;
                    best = candidate;
                }
            }
            Rotate(x, y, cx, cy, best);

            // Local Function
            double Residual(double a)
            {
                return a - grid * Math.Round(a / grid);
            }
        }

        // Bond directions for six-membered rings, as offsets of a 60° grid in order of preference:
        // Set 1 = 0°, ±60°, ±120°, 180° and Set 2 = ±30°, ±90°, ±150°.
        private static readonly double[] HexagonGridOffsets = { 0, Math.PI / 6 };

        private const double HexagonGrid = Math.PI / 3;
        private const double HexagonTolerance = Math.PI / 18;

        /// <summary>
        /// Rotation putting the bonds of the six-membered rings on Set 1, or on Set 2 only when that lines up
        /// more of the rings. Returns null when there are no six-membered rings.
        /// </summary>
        private static double? HexagonRotation(List<int[]> rings, double[] x, double[] y)
        {
            List<double[]> hexagons = rings.Where(r => r.Length == 6)
                                           .Select(r => Enumerable.Range(0, 6)
                                                                  .Select(k => Math.Atan2(y[r[(k + 1) % 6]] - y[r[k]], x[r[(k + 1) % 6]] - x[r[k]]))
                                                                  .ToArray())
                                           .ToList();
            if (hexagons.Count == 0)
            {
                return null;
            }

            double best = 0;
            int bestAligned = -1;
            foreach (double offset in HexagonGridOffsets)
            {
                // Candidates: the least-squares fit to every hexagon at once, then to each hexagon on its own.
                IEnumerable<double[]> fits = hexagons.Prepend(hexagons.SelectMany(h => h).ToArray());
                (double rotation, int aligned) = fits.Select(bonds => GridFit(bonds, offset))
                                                     .Select(r => (rotation: r, aligned: Aligned(r, offset), cost: Cost(r, offset)))
                                                     .OrderByDescending(c => c.aligned)
                                                     .ThenBy(c => c.cost)
                                                     .Select(c => (c.rotation, c.aligned))
                                                     .First();

                // A later set is only used if it lines up strictly more rings, so Set 1 wins any tie.
                if (aligned > bestAligned)
                {
                    bestAligned = aligned;
                    best = rotation;
                }
            }
            return best;

            // Local Function
            int Aligned(double rotation, double offset)
            {
                return hexagons.Count(h => h.All(a => Math.Abs(Residual(a + rotation - offset)) < HexagonTolerance));
            }

            // Local Function
            double Cost(double rotation, double offset)
            {
                return hexagons.SelectMany(h => h).Sum(a => Math.Pow(Residual(a + rotation - offset), 2));
            }

            // Local Function
            double Residual(double a)
            {
                return a - HexagonGrid * Math.Round(a / HexagonGrid);
            }
        }

        // Smallest rotation (within ±30°) that best fits the bond angles to the 60° grid starting at 'offset'.
        private static double GridFit(double[] bondAngles, double offset)
        {
            double s = 0, c = 0;
            foreach (double a in bondAngles)
            {
                s += Math.Sin(6 * (a - offset));
                c += Math.Cos(6 * (a - offset));
            }
            return Math.Abs(s) < 1e-12 && Math.Abs(c) < 1e-12 ? 0 : -Math.Atan2(s, c) / 6;
        }

        private static void Rotate(double[] x, double[] y, double cx, double cy, double theta)
        {
            double c = Math.Cos(theta), s = Math.Sin(theta);
            for (int i = 0; i < x.Length; i++)
            {
                double dx = x[i] - cx, dy = y[i] - cy;
                x[i] = cx + c * dx - s * dy;
                y[i] = cy + s * dx + c * dy;
            }
        }

        private static void ScaleToMedianBond(int[][] adj, double[] x, double[] y, double l)
        {
            List<double> lengths = new List<double>();
            for (int i = 0; i < adj.Length; i++)
            {
                foreach (int j in adj[i])
                {
                    if (i < j)
                    {
                        lengths.Add(Math.Sqrt(Math.Pow(x[i] - x[j], 2) + Math.Pow(y[i] - y[j], 2)));
                    }
                }
            }
            if (lengths.Count == 0)
            {
                return;
            }

            lengths.Sort();
            double median = lengths[lengths.Count / 2];
            if (median < 1e-9)
            {
                return;
            }

            double k = l / median;
            for (int i = 0; i < x.Length; i++)
            {
                x[i] *= k;
                y[i] *= k;
            }
        }

        private static int[,] AllPairsShortestPaths(int[][] adj)
        {
            int m = adj.Length;
            int[,] d = new int[m, m];
            for (int s = 0; s < m; s++)
            {
                for (int t = 0; t < m; t++)
                {
                    d[s, t] = -1;
                }
                d[s, s] = 0;

                Queue<int> q = new Queue<int>();
                q.Enqueue(s);
                while (q.Count > 0)
                {
                    int u = q.Dequeue();
                    foreach (int v in adj[u])
                    {
                        if (d[s, v] < 0)
                        {
                            d[s, v] = d[s, u] + 1;
                            q.Enqueue(v);
                        }
                    }
                }
            }
            return d;
        }

        private static List<int[]> Components(List<int>[] adj)
        {
            int n = adj.Length;
            bool[] seen = new bool[n];
            List<int[]> result = new List<int[]>();
            for (int s = 0; s < n; s++)
            {
                if (seen[s])
                {
                    continue;
                }

                List<int> comp = new List<int>();
                Stack<int> stack = new Stack<int>();
                stack.Push(s);
                seen[s] = true;
                while (stack.Count > 0)
                {
                    int u = stack.Pop();
                    comp.Add(u);
                    foreach (int v in adj[u])
                    {
                        if (!seen[v])
                        {
                            seen[v] = true;
                            stack.Push(v);
                        }
                    }
                }
                comp.Sort();
                result.Add(comp.ToArray());
            }
            return result;
        }
    }
}
