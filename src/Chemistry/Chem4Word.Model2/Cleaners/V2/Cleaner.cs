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

namespace Chem4Word.Model2.Cleaners.V2
{
    /// <summary>
    /// Generates fresh 2D coordinates for a Chem4Word model from its connection table, following
    /// Clark, Labute & Santavy, "2D Structure Depiction", J. Chem. Inf. Model. 2006, 46, 1107-1123:
    ///
    /// 1. partition each fragment into ring blocks, chains, atom pairs and single atoms,
    /// 2. give each partition a few weighted, locally ideal geometries,
    /// 3. sample combinations, keep the least congested and refine it one partition at a time,
    /// 4. re-sample clashing regions with extra angular freedom ("throwback"), then stretch/rotate bonds if still needed,
    /// 5. orient each fragment, arrange the fragments and finally place explicit hydrogens.
    /// The paper's database of ring templates is not included, so those ring cores use the embedding methods instead.
    /// </summary>
    public static class Cleaner
    {
        // Default bond length, used when the model has no usable bond length (e.g. every atom at the origin).
        public const double DefaultBondLength = 20;

        private const double MinimumUsableBondLength = 1e-3;

        // Gap between separate fragments, in bond lengths.
        private const double FragmentSpacing = 1.5;

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
            Graph graph = Graph.Build(atoms, bonds);

            List<Fragment> fragments = Components(graph.Adj).Select(c => LayoutFragment(graph, c)).ToList();
            ArrangeFragments(graph, fragments);

            Point[] result = new Point[atoms.Count];
            foreach (Fragment fragment in fragments)
            {
                foreach (KeyValuePair<int, Point> p in fragment.Positions)
                {
                    // The layout is computed Y up in units of one bond; Chem4Word stores Y pointing down.
                    result[p.Key] = new Point(p.Value.X * bondLength, -p.Value.Y * bondLength);
                }
            }

            return result;
        }

        private static Fragment LayoutFragment(Graph graph, int[] atoms)
        {
            // Hydrogens are left until the end, as in the paper.
            int[] heavy = atoms.Where(a => !IsPlaceableHydrogen(graph, a)).ToArray();
            Graph g = graph.Subgraph(heavy);

            Depictor depictor = new Depictor(g);
            (double[] x, double[] y) = depictor.Run();
            List<int[]> localRings = depictor.Rings;
            TidyOrientation(g.Adj, x, y, localRings);

            Fragment fragment = new Fragment();
            for (int k = 0; k < heavy.Length; k++)
            {
                fragment.Positions[heavy[k]] = new Point(x[k], y[k]);
            }

            List<int[]> rings = localRings.Select(r => r.Select(k => heavy[k]).ToArray()).ToList();
            PlaceHydrogens(graph, fragment.Positions, rings);
            return fragment;
        }

        private static bool IsPlaceableHydrogen(Graph graph, int atom) =>
            graph.Symbol[atom] == "H" && graph.Adj[atom].Length == 1 && graph.Symbol[graph.Adj[atom][0]] != "H";

        // Fill the remaining gaps around each parent atom, never pointing into one of its rings.
        private static void PlaceHydrogens(Graph graph, Dictionary<int, Point> positions, List<int[]> rings)
        {
            foreach (int parent in positions.Keys.ToList())
            {
                int[] hydrogens = graph.Adj[parent].Where(h => IsPlaceableHydrogen(graph, h)).ToArray();
                if (hydrogens.Length == 0)
                {
                    continue;
                }

                Point origin = positions[parent];
                double[] taken = graph.Adj[parent].Where(positions.ContainsKey).Select(nb => GeometryHelper.Angle(origin, positions[nb])).ToArray();
                IEnumerable<double> inside = rings.Where(r => r.Contains(parent))
                                                  .Select(r => GeometryHelper.Angle(origin, GeometryHelper.Centroid(r.Select(a => positions[a]))));

                double[] slots = GeometryHelper.SpreadIntoGaps(taken, hydrogens.Length, inside);
                for (int k = 0; k < hydrogens.Length; k++)
                {
                    positions[hydrogens[k]] = GeometryHelper.Offset(origin, slots[k], 1);
                }
            }
        }

        /// <summary>
        /// Polyatomic fragments are placed largest first with the rest in rows beneath; single-atom ions
        /// sit next to an atom of opposite charge where there is one.
        /// </summary>
        private static void ArrangeFragments(Graph graph, List<Fragment> fragments)
        {
            if (fragments.Count < 2)
            {
                return;
            }

            List<Fragment> placed = new List<Fragment>();
            HashSet<int> partnered = new HashSet<int>();
            double rowWidth = 0, cursorX = 0, cursorTop = 0, rowHeight = 0;

            foreach (Fragment fragment in fragments.Where(f => f.Positions.Count > 1).OrderByDescending(f => f.Positions.Count))
            {
                PlaceInRow(fragment);
            }

            foreach (Fragment ion in fragments.Where(f => f.Positions.Count == 1))
            {
                if (!PlaceNextToCounterion(ion))
                {
                    PlaceInRow(ion);
                }
            }

            // Local function
            void PlaceInRow(Fragment fragment)
            {
                Rect box = fragment.Bounds();
                if (placed.Count == 0)
                {
                    fragment.Translate(-box.Left, -box.Bottom);
                    rowWidth = Math.Max(box.Width, 8);
                    cursorTop = -box.Height - FragmentSpacing;
                }
                else
                {
                    if (cursorX > 0 && cursorX + box.Width > rowWidth)
                    {
                        cursorTop -= rowHeight + FragmentSpacing;
                        cursorX = 0;
                        rowHeight = 0;
                    }

                    fragment.Translate(cursorX - box.Left, cursorTop - box.Bottom);
                    cursorX += box.Width + FragmentSpacing;
                    rowHeight = Math.Max(rowHeight, box.Height);
                }

                placed.Add(fragment);
            }

            // Local function
            bool PlaceNextToCounterion(Fragment ion)
            {
                int atom = ion.Positions.Keys.First();
                int charge = graph.Charge[atom];
                if (charge == 0)
                {
                    return false;
                }

                List<KeyValuePair<int, Point>> all = placed.SelectMany(f => f.Positions).ToList();
                int partner = all.Select(p => p.Key)
                                 .Where(a => Math.Sign(graph.Charge[a]) == -Math.Sign(charge) && !partnered.Contains(a))
                                 .DefaultIfEmpty(-1)
                                 .First();
                if (partner < 0)
                {
                    return false;
                }

                Point centre = all.First(p => p.Key == partner).Value;
                Point best = Enumerable.Range(0, 12)
                                       .Select(k => GeometryHelper.Offset(centre, k * 30 * GeometryHelper.Degrees, FragmentSpacing))
                                       .OrderBy(p => all.Sum(q => 1 / Math.Max((p - q.Value).LengthSquared, 1e-4)))
                                       .First();

                ion.Positions[atom] = best;
                partnered.Add(partner);
                placed.Add(ion);
                return true;
            }
        }

        // Rotate so the long axis is horizontal, then put six-membered rings on Set 1/Set 2 if there are any,
        // otherwise rotate by the smallest amount that lines up the most bonds on 15° increments.
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

            const double grid = 15 * GeometryHelper.Degrees;
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

            double best = 0;
            int bestCount = -1;
            foreach (double candidate in bondAngles.Select(a => -Residual(a)).Prepend(0))
            {
                int count = bondAngles.Count(a => Math.Abs(Residual(a + candidate)) < 0.5 * GeometryHelper.Degrees);
                if (count > bestCount || (count == bestCount && Math.Abs(candidate) < Math.Abs(best)))
                {
                    bestCount = count;
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

        private static List<int[]> Components(int[][] adj)
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
