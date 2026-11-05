using Chem4Word.Model2;
using Chem4Word.Model2.Converters.MDL;
using Chem4Word.Model2.Enums;
using Chem4Word.Model2.Helpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;

namespace WinForms.TestHarness.Claude
{
    public static class Driver
    {
        private static readonly (string name, string smiles)[] Tests =
        {
            ("hexane", "CCCCCC"),
            ("ibuprofen", "CC(C)Cc1ccc(cc1)C(C)C(=O)O"),
            ("naphthalene", "c1ccc2ccccc2c1"),
            ("phenanthrene", "c1ccc2c(c1)ccc1ccccc12"),
            ("pyrene", "c1cc2ccc3cccc4ccc(c1)c2c34"),
            ("steroid", "CC(C)CCCC(C)C1CCC2C1(CCC3C2CC=C4C3(CCC(C4)O)C)C"),
            ("adamantane", "C1C2CC3CC1CC(C2)C3"),
            ("cubane", "C12C3C4C1C5C2C3C45"),
            ("norbornane", "C1CC2CCC1C2"),
            ("18-crown-6", "C1COCCOCCOCCOCCOCCO1"),
            ("benzo-macrocycle", "O=C1CCCCCCCCCCc2ccccc2N1"),
            ("spiro", "C1CCC2(CC1)CCCC2"),
            ("biphenyl", "c1ccc(cc1)-c1ccccc1"),
            ("caffeine", "Cn1cnc2c1c(=O)n(C)c(=O)n2C"),
            ("perfluorohexane", "FC(F)(F)C(F)(F)C(F)(F)C(F)(F)C(F)(F)C(F)(F)F"),
            ("sodium acetate", "CC(=O)[O-].[Na+]"),
            ("tetraphenylmethane", "c1ccc(cc1)C(c1ccccc1)(c1ccccc1)c1ccccc1"),
            ("hexaphenylbenzene", "c1ccc(cc1)-c1c(-c2ccccc2)c(-c2ccccc2)c(-c2ccccc2)c(-c2ccccc2)c1-c1ccccc1"),
            ("morphine", "CN1CCC23C4C1CC5=C2C(=C(C=C5)O)OC3C(C=C4)O"), 
            ("tBu-ester", "CC(C)(C)OC(=O)CCC(=O)OC(C)(C)C"),
            ("acetonitrile-chain", "N#CCCCC#N"), 
            ("explicit H ethanol", "[H]C([H])([H])C([H])([H])O[H]"),
            ("cyclohexane H", "[H]C1([H])CCCCC1"), 
            ("ammonium chloride", "[NH4+].[Cl-]"),
            ("dodecahedrane-ish", "C12C3C4C5C1C6C7C2C8C3C9C4C%10C5C6C%11C7C8C9C%10%11"),
            ("tetra-tBu-methane", "CC(C)(C)C(C(C)(C)C)(C(C)(C)C)C(C)(C)C"),
            ("tri-tBu-phenol", "CC(C)(C)c1cc(C(C)(C)C)c(O)c(C(C)(C)C)c1"),
            ("Ph3PO", "O=P(c1ccccc1)(c1ccccc1)c1ccccc1"),
            ("porphine", "c1cc2cc3ccc(cc4ccc(cc5ccc(cc1n2)[nH]5)n4)[nH]3"),
            ("erythromycin", "CCC1C(C(C(C(=O)C(CC(C(C(C(C(C(=O)O1)C)OC2CC(C(C(O2)C)O)(C)OC)C)OC3C(C(CC(O3)C)N(C)C)O)(C)O)C)C)O)(C)O"),
            ("paclitaxel", "CC1=C2C(C(=O)C3(C(CC4C(C3C(C(C2(C)C)(CC1OC(=O)C(C(C5=CC=CC=C5)NC(=O)C6=CC=CC=C6)O)O)OC(=O)C7=CC=CC=C7)(CO4)OC(=O)C)O)C)OC(=O)C"),
            ("dendrimer-ish", "CC(C)(C)C(CC(C)(C)C)(CC(C)(C)C)CC(CC(C)(C)C)(CC(C)(C)C)CC(C)(C)C"),
            ("C200 chain", new string('C', 200)),
        };

        public static void TestDrive(string[] args)
        {
            string outDir = args[0];
            StringBuilder html = new StringBuilder("<html><body style='font-family:sans-serif'><div style='display:flex;flex-wrap:wrap'>");
            foreach ((string name, string smiles) in Tests)
            {
                try
                {
                    string molfile = SmilesToMolfile(smiles);
                    Model model = new SdFileConverter().Import(molfile);
                    Stopwatch sw = Stopwatch.StartNew();
                    double l = Chem4Word.Model2.Cleaners.V2.Cleaner.Clean(model);
                    sw.Stop();

                    List<Atom> atoms = new List<Atom>();
                    List<Bond> bonds = new List<Bond>();
                    foreach (Molecule m in model.Molecules.Values)
                    {
                        m.BuildAtomList(atoms);
                        m.BuildBondList(bonds);
                    }

                    double maxDev = bonds.Count == 0
                        ? 0
                        : bonds.Max(b => Math.Abs((b.StartAtom.Position - b.EndAtom.Position).Length / l - 1));
                    HashSet<(Atom, Atom)> bonded = new HashSet<(Atom, Atom)>(
                        bonds.SelectMany(b => new[] { (b.StartAtom, b.EndAtom), (b.EndAtom, b.StartAtom) }));
                    double minNb = double.MaxValue;
                    for (int i = 0; i < atoms.Count; i++)
                        for (int j = i + 1; j < atoms.Count; j++)
                            if (!bonded.Contains((atoms[i], atoms[j])))
                                minNb = Math.Min(minNb, (atoms[i].Position - atoms[j].Position).Length / l);
                    int crossings = 0;
                    for (int p = 0; p < bonds.Count; p++)
                        for (int q = p + 1; q < bonds.Count; q++)
                            if (Crosses(bonds[p], bonds[q]))
                                crossings++;
                    bool nan = atoms.Any(a => double.IsNaN(a.Position.X) || double.IsNaN(a.Position.Y));

                    Console.WriteLine($"{name,-22} atoms={atoms.Count,3} maxBondDev={maxDev,6:F3} minNonBonded={(minNb == double.MaxValue ? 0 : minNb),6:F3} crossings={crossings} nan={nan} {sw.ElapsedMilliseconds}ms");
                    html.Append($"<div style='margin:8px;border:1px solid #ccc'><div>{name}</div>{Svg(atoms, bonds)}</div>");
                    Cells.Add((name, atoms, bonds));
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"{name,-22} FAILED {ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}");
                }
            }

            html.Append("</div></body></html>");
            File.WriteAllText(Path.Combine(outDir, "layouts.html"), html.ToString());
            Png(Path.Combine(outDir, "layouts.png"));
        }

        private static readonly List<(string name, List<Atom> atoms, List<Bond> bonds)> Cells =
            new List<(string, List<Atom>, List<Bond>)>();

        private static void Png(string path)
        {
            const int cell = 260, cols = 5;
            int rows = (Cells.Count + cols - 1) / cols;
            using (var bmp = new System.Drawing.Bitmap(cell * cols, cell * rows))
            using (var g = System.Drawing.Graphics.FromImage(bmp))
            using (var font = new System.Drawing.Font("Arial", 9))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                g.Clear(System.Drawing.Color.White);
                for (int c = 0; c < Cells.Count; c++)
                {
                    var (name, atoms, bonds) = Cells[c];
                    float ox = (c % cols) * cell, oy = (c / cols) * cell;
                    g.DrawRectangle(System.Drawing.Pens.LightGray, ox, oy, cell - 1, cell - 1);
                    g.DrawString(name, font, System.Drawing.Brushes.Black, ox + 3, oy + 3);
                    double minX = atoms.Min(a => a.Position.X), maxX = atoms.Max(a => a.Position.X);
                    double minY = atoms.Min(a => a.Position.Y), maxY = atoms.Max(a => a.Position.Y);
                    double s = Math.Min(1.4, (cell - 40) / Math.Max(1, Math.Max(maxX - minX, maxY - minY)));
                    Func<Point, System.Drawing.PointF> map = p => new System.Drawing.PointF(
                                                                 (float)(ox + cell / 2 + (p.X - (minX + maxX) / 2) * s),
                                                                 (float)(oy + cell / 2 + 8 +
                                                                         (p.Y - (minY + maxY) / 2) * s));
                    foreach (Bond b in bonds)
                    {
                        var color = b.OrderValue >= 2 ? System.Drawing.Color.Blue :
                            b.OrderValue == 1.5 ? System.Drawing.Color.Green : System.Drawing.Color.Black;
                        using (var pen = new System.Drawing.Pen(color, b.OrderValue >= 2 ? 2.5f : 1.3f))
                            g.DrawLine(pen, map(b.StartAtom.Position), map(b.EndAtom.Position));
                    }

                    foreach (Atom a in atoms)
                    {
                        string sym = a.Element?.Symbol ?? "?";
                        if (sym == "C") continue;
                        var p = map(a.Position);
                        g.FillEllipse(System.Drawing.Brushes.White, p.X - 6, p.Y - 6, 12, 12);
                        g.DrawString(sym, font, System.Drawing.Brushes.Red, p.X - 6, p.Y - 7);
                    }
                }

                bmp.Save(path, System.Drawing.Imaging.ImageFormat.Png);
            }
        }

        private static bool Crosses(Bond a, Bond b)
        {
            if (a.StartAtom == b.StartAtom || a.StartAtom == b.EndAtom || a.EndAtom == b.StartAtom ||
                a.EndAtom == b.EndAtom) return false;
            Point p1 = a.StartAtom.Position,
                  p2 = a.EndAtom.Position,
                  p3 = b.StartAtom.Position,
                  p4 = b.EndAtom.Position;
            double d1 = Cross(p3, p4, p1), d2 = Cross(p3, p4, p2), d3 = Cross(p1, p2, p3), d4 = Cross(p1, p2, p4);
            return d1 * d2 < -1e-6 && d3 * d4 < -1e-6;
        }

        private static double Cross(Point a, Point b, Point c) => (b.X - a.X) * (c.Y - a.Y) - (b.Y - a.Y) * (c.X - a.X);

        private static string Svg(List<Atom> atoms, List<Bond> bonds)
        {
            double minX = atoms.Min(a => a.Position.X) - 20, minY = atoms.Min(a => a.Position.Y) - 20;
            double w = atoms.Max(a => a.Position.X) - minX + 20, h = atoms.Max(a => a.Position.Y) - minY + 20;
            StringBuilder sb = new StringBuilder();
            double scale = Math.Min(1.5, 300 / Math.Max(w, h));
            sb.Append(
                $"<svg width='{w * scale:F0}' height='{h * scale:F0}' viewBox='{F(minX)} {F(minY)} {F(w)} {F(h)}'>");
            foreach (Bond b in bonds)
            {
                Point s = b.StartAtom.Position, e = b.EndAtom.Position;
                string stroke = b.OrderValue >= 2 ? "#06c" : b.OrderValue == 1.5 ? "#090" : "#000";
                sb.Append($"<line x1='{F(s.X)}' y1='{F(s.Y)}' x2='{F(e.X)}' y2='{F(e.Y)}' stroke='{stroke}' stroke-width='{(b.OrderValue >= 2 ? 3 : 1.5)}'/>");
            }

            foreach (Atom a in atoms)
            {
                string sym = a.Element?.Symbol ?? "?";
                if (sym != "C")
                    sb.Append($"<text x='{F(a.Position.X)}' y='{F(a.Position.Y + 4)}' font-size='11' text-anchor='middle' fill='#c00' stroke='white' stroke-width='3' paint-order='stroke'>{sym}{(a.FormalCharge > 0 ? "+" : a.FormalCharge < 0 ? "-" : "")}</text>");
            }

            sb.Append("</svg>");
            return sb.ToString();
        }

        private static string F(double v) => v.ToString("F2", CultureInfo.InvariantCulture);

        // Minimal SMILES reader: organic subset, brackets with H count and charge, bonds - = # :, branches, ring closures, dots.
        public static string SmilesToMolfile(string smiles)
        {
            List<(string el, int charge)> atoms = new List<(string, int)>();
            List<(int a, int b, int order)> bonds = new List<(int, int, int)>();
            Dictionary<int, (int atom, int order)> rings = new Dictionary<int, (int, int)>();
            Stack<int> branch = new Stack<int>();
            HashSet<int> aromatic = new HashSet<int>();
            int prev = -1, pendingOrder = 0;
            int i = 0;
            while (i < smiles.Length)
            {
                char c = smiles[i];
                if (c == '(')
                {
                    branch.Push(prev);
                    i++;
                    continue;
                }

                if (c == ')')
                {
                    prev = branch.Pop();
                    i++;
                    continue;
                }

                if (c == '.')
                {
                    prev = -1;
                    i++;
                    continue;
                }

                if (c == '-')
                {
                    pendingOrder = 1;
                    i++;
                    continue;
                }

                if (c == '=')
                {
                    pendingOrder = 2;
                    i++;
                    continue;
                }

                if (c == '#')
                {
                    pendingOrder = 3;
                    i++;
                    continue;
                }

                if (c == ':')
                {
                    pendingOrder = 4;
                    i++;
                    continue;
                }

                if (char.IsDigit(c) || c == '%')
                {
                    int num;
                    if (c == '%')
                    {
                        num = int.Parse(smiles.Substring(i + 1, 2));
                        i += 3;
                    }
                    else
                    {
                        num = c - '0';
                        i++;
                    }

                    if (rings.TryGetValue(num, out var open))
                    {
                        int order = pendingOrder != 0 ? pendingOrder :
                            open.order != 0 ? open.order :
                            (aromatic.Contains(prev) && aromatic.Contains(open.atom) ? 4 : 1);
                        bonds.Add((open.atom, prev, order));
                        rings.Remove(num);
                    }
                    else rings[num] = (prev, pendingOrder);

                    pendingOrder = 0;
                    continue;
                }

                string el;
                int charge = 0;
                int hCount = 0;
                bool arom = false;
                if (c == '[')
                {
                    int end = smiles.IndexOf(']', i);
                    string inner = smiles.Substring(i + 1, end - i - 1);
                    int k = 0;
                    el = char.IsUpper(inner[0]) && inner.Length > 1 && char.IsLower(inner[1]) && inner[1] != 'h'
                        ? inner.Substring(0, 2)
                        : inner.Substring(0, 1);
                    k = el.Length;
                    if (char.IsLower(el[0]))
                    {
                        arom = true;
                        el = el.ToUpper();
                    }

                    if (k < inner.Length && inner[k] == 'H')
                    {
                        k++;
                        hCount = 1;
                        if (k < inner.Length && char.IsDigit(inner[k])) hCount = inner[k++] - '0';
                    }

                    while (k < inner.Length)
                    {
                        if (inner[k] == '+') charge++;
                        else if (inner[k] == '-') charge--;
                        else if (char.IsDigit(inner[k])) charge = charge * (inner[k] - '0');
                        k++;
                    }

                    i = end + 1;
                }
                else
                {
                    if (i + 1 < smiles.Length && (smiles.Substring(i, 2) == "Cl" || smiles.Substring(i, 2) == "Br"))
                    {
                        el = smiles.Substring(i, 2);
                        i += 2;
                    }
                    else
                    {
                        el = c.ToString();
                        i++;
                        if (char.IsLower(c))
                        {
                            arom = true;
                            el = el.ToUpper();
                        }
                    }
                }

                int idx = atoms.Count;
                atoms.Add((el, charge));
                if (arom) aromatic.Add(idx);
                if (prev >= 0)
                {
                    int order = pendingOrder != 0 ? pendingOrder : (arom && aromatic.Contains(prev) ? 4 : 1);
                    bonds.Add((prev, idx, order));
                }

                pendingOrder = 0;
                prev = idx;
            }

            StringBuilder sb = new StringBuilder();
            sb.Append("\n  Driver\n\n");
            sb.Append($"{atoms.Count,3}{bonds.Count,3}  0  0  0  0  0  0  0  0999 V2000\n");
            foreach (var a in atoms)
                sb.Append($"    0.0000    0.0000    0.0000 {a.el,-3} 0  0  0  0  0  0  0  0  0  0  0  0\n");
            foreach (var b in bonds)
                sb.Append($"{b.a + 1,3}{b.b + 1,3}{b.order,3}  0\n");
            var charged = atoms.Select((a, k) => (a, k)).Where(t => t.a.charge != 0).ToList();
            if (charged.Count > 0)
            {
                sb.Append($"M  CHG{charged.Count,3}");
                foreach (var t in charged) sb.Append($"{t.k + 1,4}{t.a.charge,4}");
                sb.Append("\n");
            }

            sb.Append("M  END\n$$$$\n");
            return sb.ToString();
        }

        /// <summary>
        /// Converts SMILES to a Chem4Word model, with every atom at the origin, ready for cleaning.
        /// Aromatic (lower case) atoms are kekulised, bracket hydrogen counts are honoured, and double-bond stereo
        /// written with / and \ becomes Bond.Stereo of Cis or Trans, relative to the first other neighbour at each end.
        /// Syntax errors are reported in the model's errors. A tetrahedral bracket atom with a hydrogen, such as [C@H],
        /// gets it as an explicit H on a wedge bond. Pass 'clean' (e.g. m => CleanerV2.Clean(m)) to lay the model out
        /// here, after which each such bond is made a wedge or a hatch so the drawing shows the @ or @@ written.
        /// </summary>
        public static Model SmilesToModel(string smiles, Action<Model> clean = null)
        {
            SmilesGraph graph;
            try
            {
                graph = SmilesGraph.Parse(smiles ?? "");
            }
            catch (FormatException ex)
            {
                Model failed = new Model();
                Molecule empty = new Molecule();
                empty.Errors.Add(ex.Message);
                failed.AddMolecule(empty);
                empty.Parent = failed;
                return failed;
            }

            List<string> warnings = new List<string>();
            graph.MakeNonRingAromaticBondsSingle();
            graph.Kekulise(warnings);
            List<StereoCentre> centres = new List<StereoCentre>();
            Model model = BuildModel(graph, graph.DoubleBondStereo(), warnings, centres);

            if (clean != null)
            {
                clean(model);
                foreach (StereoCentre centre in centres)
                {
                    centre.ShowConfiguration();
                }
            }

            return model;
        }

        // One molecule per connected fragment; bonds are added in SMILES order, which fixes each stereo reference atom.
        private static Model BuildModel(SmilesGraph graph, Dictionary<SmilesBond, bool> isCis, List<string> warnings,
                                        List<StereoCentre> centres)
        {
            List<SmilesAtom> atoms = graph.Atoms;
            List<SmilesBond> bonds = graph.Bonds;
            int[] fragment = Enumerable.Range(0, atoms.Count).ToArray();
            int Find(int a) => fragment[a] == a ? a : fragment[a] = Find(fragment[a]);
            foreach (SmilesBond b in bonds)
            {
                fragment[Find(b.A)] = Find(b.B);
            }

            string[] orders =
            {
                ModelConstants.OrderSingle, ModelConstants.OrderSingle, ModelConstants.OrderDouble,
                ModelConstants.OrderTriple, ModelConstants.OrderAromatic
            };
            Model model = new Model();
            bool first = true;
            foreach (IGrouping<int, int> group in Enumerable.Range(0, atoms.Count).GroupBy(Find))
            {
                Molecule molecule = new Molecule();
                if (first)
                {
                    molecule.Warnings.AddRange(warnings);
                    first = false;
                }

                Dictionary<int, Atom> made = new Dictionary<int, Atom>();
                foreach (int k in group)
                {
                    AtomHelpers.TryParse(atoms[k].Symbol, true, out ElementBase element);
                    Atom atom = new Atom
                    {
                        Element = element,
                        FormalCharge = atoms[k].Charge != 0 ? atoms[k].Charge : (int?)null,
                        IsotopeNumber = atoms[k].Isotope,
                        Position = new Point(0, 0)
                    };
                    molecule.AddAtom(atom);
                    atom.Parent = molecule;
                    made[k] = atom;
                }

                foreach (SmilesBond b in bonds.Where(b => Find(b.A) == group.Key))
                {
                    AddBond(molecule, made[b.A], made[b.B], orders[b.Order],
                            !isCis.TryGetValue(b, out bool cis) ? BondStereo.None :
                            cis ? BondStereo.Cis : BondStereo.Trans);
                }

                foreach (int k in group.Where(k => atoms[k].HydrogenCount.HasValue))
                {
                    int hydrogens = atoms[k].HydrogenCount.Value;
                    if (atoms[k].Anticlockwise.HasValue && hydrogens > 0)
                    {
                        Atom hydrogen = AddHydrogen(molecule);
                        Bond wedge = AddBond(molecule, made[k], hydrogen, ModelConstants.OrderSingle, BondStereo.Wedge);
                        centres.Add(new StereoCentre
                        {
                            Centre = made[k],
                            Neighbours =
                                            atoms[k].Neighbours
                                                    .Select(n => n == SmilesAtom.ImplicitHydrogen ? hydrogen : made[n])
                                                    .ToArray(),
                            Wedge = wedge,
                            Anticlockwise = atoms[k].Anticlockwise.Value
                        });
                        hydrogens--;
                    }

                    MatchHydrogenCount(molecule, made[k], hydrogens);
                }

                model.AddMolecule(molecule);
                molecule.Parent = model;
            }

            model.Relabel(true);
            model.Refresh();
            return model;
        }

        private static Bond AddBond(Molecule molecule, Atom start, Atom end, string order,
                                    BondStereo stereo = BondStereo.None)
        {
            Bond bond = new Bond
            {
                StartAtomInternalId = start.InternalId,
                EndAtomInternalId = end.InternalId,
                Order = order,
                Stereo = stereo
            };
            molecule.AddBond(bond);
            bond.Parent = molecule;
            return bond;
        }

        // A bracket atom has exactly the hydrogens written. Chem4Word works out implicit hydrogens from its own
        // valence model, so add explicit H atoms where it would show too few, and radical or carbenoid electrons
        // where it would show too many (e.g. [CH3] or [CH2]).
        private static void MatchHydrogenCount(Molecule molecule, Atom atom, int hydrogens)
        {
            if (Math.Max(atom.ImplicitHydrogenCount, 0) < hydrogens)
            {
                for (int h = 0; h < hydrogens; h++)
                {
                    AddBond(molecule, atom, AddHydrogen(molecule), ModelConstants.OrderSingle);
                }

                hydrogens = 0;
            }

            int extra = Math.Max(atom.ImplicitHydrogenCount, 0) - hydrogens;
            for (; extra >= 2; extra -= 2)
            {
                atom.AddElectron(new Electron { TypeOfElectron = ElectronType.Carbenoid, Parent = atom });
            }

            if (extra == 1)
            {
                atom.AddElectron(new Electron { TypeOfElectron = ElectronType.Radical, Parent = atom });
            }
        }

        private static Atom AddHydrogen(Molecule molecule)
        {
            AtomHelpers.TryParse("H", true, out ElementBase hydrogen);
            Atom atom = new Atom { Element = hydrogen, Position = new Point(0, 0) };
            molecule.AddAtom(atom);
            atom.Parent = molecule;
            return atom;
        }

        private sealed class StereoCentre
        {
            public Atom Centre;
            public Atom[] Neighbours;  // In SMILES order.
            public Bond Wedge;         // From the centre to its hydrogen.
            public bool Anticlockwise; // @ rather than @@.

            // Looking from the first neighbour, @ has the other three anticlockwise. With the hydrogen out of the
            // page that is a negative signed volume, so use a hatch instead wherever the layout gives the opposite.
            public void ShowConfiguration()
            {
                if (Neighbours.Length != 4)
                {
                    return;
                }

                Atom hydrogen = Wedge.OtherAtom(Centre);
                double depth = Neighbours.Where(n => n != hydrogen).Average(n => (n.Position - Centre.Position).Length);
                // Chem4Word's Y axis points down, so flip it to get a right-handed frame with +Z out of the page.
                double[][] p = Neighbours.Select(n => new[] { n.Position.X, -n.Position.Y, n == hydrogen ? depth : 0 })
                                         .ToArray();
                double[][] v = Enumerable.Range(1, 3)
                                         .Select(i => Enumerable.Range(0, 3).Select(d => p[i][d] - p[0][d]).ToArray())
                                         .ToArray();
                double volume = v[0][0] * (v[1][1] * v[2][2] - v[1][2] * v[2][1])
                                - v[0][1] * (v[1][0] * v[2][2] - v[1][2] * v[2][0])
                                + v[0][2] * (v[1][0] * v[2][1] - v[1][1] * v[2][0]);
                if (Math.Abs(volume) < 1e-6 * depth * depth * depth)
                {
                    return;
                }

                Wedge.Stereo = (volume < 0) == Anticlockwise ? BondStereo.Wedge : BondStereo.Hatch;
            }
        }

        public sealed class SmilesAtom
        {
            public const int ImplicitHydrogen = -1;

            public string Symbol;
            public int Charge;
            public int? Isotope;
            public int? HydrogenCount; // Only set for bracket atoms.
            public bool Aromatic;
            public bool? Anticlockwise; // Tetrahedral @ (true) or @@ (false).

            public readonly List<int>
                Neighbours = new List<int>(); // In SMILES order; ImplicitHydrogen for a bracket H.
        }

        public sealed class SmilesBond
        {
            public int A;
            public int B;
            public int Order;      // 1, 2, 3, or 4 for aromatic.
            public char Direction; // '/' or '\' as written going from A to B, or '\0'.
        }

        private sealed class SmilesGraph
        {
            private static readonly string[] ChiralClasses = { "TH", "AL", "SP", "TB", "OH" };

            public readonly List<SmilesAtom> Atoms = new List<SmilesAtom>();
            public readonly List<SmilesBond> Bonds = new List<SmilesBond>();

            private readonly string _smiles;
            private int _i;

            private SmilesGraph(string smiles)
            {
                _smiles = smiles;
            }

            public static SmilesGraph Parse(string smiles)
            {
                SmilesGraph graph = new SmilesGraph(smiles);
                graph.Read();
                return graph;
            }

            private void Read()
            {
                Dictionary<int, (int atom, int? order, char dir, int slot)> rings =
                    new Dictionary<int, (int, int?, char, int)>();
                Stack<int> branches = new Stack<int>();
                int prev = -1;
                int? pendingOrder = null;
                char pendingDir = '\0';

                while (_i < _smiles.Length)
                {
                    char c = _smiles[_i];
                    if (char.IsWhiteSpace(c))
                    {
                        break; // Anything after whitespace is a name or comment.
                    }

                    switch (c)
                    {
                        case '(':
                            if (prev < 0 || pendingOrder.HasValue)
                            {
                                throw Error(prev < 0
                                                ? "a branch must follow an atom"
                                                : "the bond symbol must be inside the parentheses");
                            }

                            branches.Push(prev);
                            _i++;
                            continue;

                        case ')':
                            if (branches.Count == 0)
                            {
                                throw Error("')' has no matching '('");
                            }

                            NoPendingBond(pendingOrder);
                            prev = branches.Pop();
                            _i++;
                            continue;

                        case '.':
                            NoPendingBond(pendingOrder);
                            prev = -1;
                            _i++;
                            continue;

                        case '-':
                        case '=':
                        case '#':
                        case '$':
                        case ':':
                        case '/':
                        case '\\':
                            if (prev < 0 || pendingOrder.HasValue)
                            {
                                throw Error(prev < 0 ? "a bond must follow an atom" : "two bond symbols in a row");
                            }

                            if (c == '$')
                            {
                                throw Error("quadruple bonds ($) can't be represented in Chem4Word");
                            }

                            pendingOrder = c == '=' ? 2 : c == '#' ? 3 : c == ':' ? 4 : 1;
                            pendingDir = c == '/' || c == '\\' ? c : '\0';
                            _i++;
                            continue;
                    }

                    if (char.IsDigit(c) || c == '%')
                    {
                        if (prev < 0)
                        {
                            throw Error("a ring closure must follow an atom");
                        }

                        int number = ReadRingNumber();
                        if (rings.TryGetValue(number, out var open))
                        {
                            if (open.order.HasValue && pendingOrder.HasValue && open.order != pendingOrder)
                            {
                                throw Error($"ring closure {number} has conflicting bond types");
                            }

                            if (open.atom == prev ||
                                Bonds.Any(b => b.A == open.atom && b.B == prev || b.A == prev && b.B == open.atom))
                            {
                                throw Error($"ring closure {number} joins atoms that are already bonded");
                            }

                            int order = pendingOrder ?? open.order ?? DefaultOrder(open.atom, prev);
                            if (open.dir != '\0' || pendingDir == '\0')
                            {
                                Bonds.Add(new SmilesBond
                                {
                                    A = open.atom,
                                    B = prev,
                                    Order = order,
                                    Direction = open.dir
                                });
                            }
                            else
                            {
                                Bonds.Add(new SmilesBond
                                {
                                    A = prev,
                                    B = open.atom,
                                    Order = order,
                                    Direction = pendingDir
                                });
                            }

                            Atoms[open.atom].Neighbours[open.slot] = prev;
                            Atoms[prev].Neighbours.Add(open.atom);
                            rings.Remove(number);
                        }
                        else
                        {
                            // The ring partner's place in the neighbour order is where the digit is written.
                            rings[number] = (prev, pendingOrder, pendingDir, Atoms[prev].Neighbours.Count);
                            Atoms[prev].Neighbours.Add(int.MinValue);
                        }

                        pendingOrder = null;
                        pendingDir = '\0';
                        continue;
                    }

                    int atom = c == '[' ? ReadBracketAtom() : ReadOrganicAtom();
                    if (prev >= 0)
                    {
                        Bonds.Add(new SmilesBond
                        {
                            A = prev,
                            B = atom,
                            Order = pendingOrder ?? DefaultOrder(prev, atom),
                            Direction = pendingDir
                        });
                        Atoms[prev].Neighbours.Add(atom);
                        Atoms[atom].Neighbours.Insert(0, prev);
                    }

                    pendingOrder = null;
                    pendingDir = '\0';
                    prev = atom;
                }

                NoPendingBond(pendingOrder);
                if (branches.Count > 0)
                {
                    throw Error("'(' has no matching ')'");
                }

                if (rings.Count > 0)
                {
                    throw Error($"ring {string.Join(", ", rings.Keys)} is never closed");
                }
            }

            // Bonds between two aromatic atoms are aromatic unless written otherwise.
            private int DefaultOrder(int a, int b) => Atoms[a].Aromatic && Atoms[b].Aromatic ? 4 : 1;

            private int ReadRingNumber()
            {
                if (_smiles[_i] != '%')
                {
                    return _smiles[_i++] - '0';
                }

                if (_i + 2 >= _smiles.Length || !char.IsDigit(_smiles[_i + 1]) || !char.IsDigit(_smiles[_i + 2]))
                {
                    throw Error("'%' must be followed by two digits");
                }

                int number = int.Parse(_smiles.Substring(_i + 1, 2));
                _i += 3;
                return number;
            }

            // Outside brackets only the organic subset is allowed: B C N O P S F Cl Br I, or aromatic b c n o p s.
            private int ReadOrganicAtom()
            {
                string symbol;
                if (_i + 1 < _smiles.Length && (_smiles.Substring(_i, 2) == "Cl" || _smiles.Substring(_i, 2) == "Br"))
                {
                    symbol = _smiles.Substring(_i, 2);
                }
                else if ("BCNOPSFIbcnops".IndexOf(_smiles[_i]) >= 0)
                {
                    symbol = _smiles.Substring(_i, 1);
                }
                else
                {
                    throw Error($"unexpected '{_smiles[_i]}'; atoms outside the organic subset must be in brackets");
                }

                _i += symbol.Length;
                return AddAtom(symbol, 0, null, null);
            }

            // [isotope? symbol chirality? hcount? charge? class?]
            private int ReadBracketAtom()
            {
                int end = _smiles.IndexOf(']', _i);
                if (end < 0)
                {
                    throw Error("'[' has no matching ']'");
                }

                string inner = _smiles.Substring(_i + 1, end - _i - 1);
                int k = 0;

                int? isotope = null;
                while (k < inner.Length && char.IsDigit(inner[k]))
                {
                    k++;
                }

                if (k > 0)
                {
                    isotope = int.Parse(inner.Substring(0, k));
                }

                string symbol;
                if (k < inner.Length && char.IsUpper(inner[k]))
                {
                    symbol = k + 1 < inner.Length && char.IsLower(inner[k + 1])
                        ? inner.Substring(k, 2)
                        : inner.Substring(k, 1);
                }
                else if (k < inner.Length && char.IsLower(inner[k]))
                {
                    string two = k + 1 < inner.Length ? inner.Substring(k, 2) : "";
                    symbol = two == "se" || two == "as" || two == "te" ? two : inner.Substring(k, 1);
                    if ("bcnops".IndexOf(symbol[0]) < 0 && symbol.Length == 1)
                    {
                        throw Error($"'{symbol}' is not an aromatic element");
                    }
                }
                else
                {
                    throw Error($"missing element symbol in '[{inner}]'");
                }

                k += symbol.Length;
                string element = char.ToUpper(symbol[0]) + symbol.Substring(1);
                if (!ModelGlobals.PeriodicTable.HasElement(element))
                {
                    throw Error($"unknown element '{element}'");
                }

                // Only tetrahedral centres are kept: @ is @TH1 and @@ is @TH2.
                bool? anticlockwise = null;
                if (k < inner.Length && inner[k] == '@')
                {
                    k++;
                    string chiralClass = "TH1";
                    if (k < inner.Length && inner[k] == '@')
                    {
                        k++;
                        chiralClass = "TH2";
                    }
                    else if (k + 1 < inner.Length && ChiralClasses.Contains(inner.Substring(k, 2)))
                    {
                        int start = k;
                        k += 2;
                        while (k < inner.Length && char.IsDigit(inner[k]))
                        {
                            k++;
                        }

                        chiralClass = inner.Substring(start, k - start);
                    }

                    anticlockwise = chiralClass == "TH1" ? true : chiralClass == "TH2" ? false : (bool?)null;
                }

                int hydrogens = 0;
                if (k < inner.Length && inner[k] == 'H')
                {
                    k++;
                    hydrogens = 1;
                    if (k < inner.Length && char.IsDigit(inner[k]))
                    {
                        hydrogens = inner[k++] - '0';
                    }
                }

                int charge = 0;
                if (k < inner.Length && (inner[k] == '+' || inner[k] == '-'))
                {
                    char sign = inner[k];
                    int start = k++;
                    while (k < inner.Length && char.IsDigit(inner[k]))
                    {
                        k++;
                    }

                    if (k > start + 1)
                    {
                        charge = int.Parse(inner.Substring(start + 1, k - start - 1));
                    }
                    else
                    {
                        charge = 1;
                        while (k < inner.Length && inner[k] == sign)
                        {
                            charge++;
                            k++;
                        }
                    }

                    charge = sign == '+' ? charge : -charge;
                }

                if (k < inner.Length && inner[k] == ':')
                {
                    k++;
                    while (k < inner.Length && char.IsDigit(inner[k]))
                    {
                        k++;
                    }
                }

                if (k != inner.Length)
                {
                    throw Error($"can't read '[{inner}]'");
                }

                _i = end + 1;
                return AddAtom(symbol, charge, isotope, hydrogens, anticlockwise);
            }

            private int AddAtom(string symbol, int charge, int? isotope, int? hydrogens, bool? anticlockwise = null)
            {
                bool aromatic = char.IsLower(symbol[0]);
                SmilesAtom atom = new SmilesAtom
                {
                    Symbol = aromatic ? char.ToUpper(symbol[0]) + symbol.Substring(1) : symbol,
                    Charge = charge,
                    Isotope = isotope,
                    HydrogenCount = hydrogens,
                    Aromatic = aromatic,
                    Anticlockwise = anticlockwise
                };
                // A bracket H comes straight after the preceding atom, or first if there isn't one.
                if (hydrogens > 0)
                {
                    atom.Neighbours.Add(SmilesAtom.ImplicitHydrogen);
                }

                Atoms.Add(atom);
                return Atoms.Count - 1;
            }

            private void NoPendingBond(int? pendingOrder)
            {
                if (pendingOrder.HasValue)
                {
                    throw Error("a bond symbol must be followed by an atom or ring closure");
                }
            }

            private FormatException Error(string message)
            {
                return new FormatException($"Invalid SMILES at position {_i + 1}: {message}.");
            }

            // Aromatic bonds outside rings, e.g. between the rings in c1ccccc1c2ccccc2, are single bonds.
            public void MakeNonRingAromaticBondsSingle()
            {
                foreach (SmilesBond bond in Bonds.Where(b => b.Order == 4 && !InRing(b)).ToList())
                {
                    bond.Order = 1;
                }
            }

            private bool InRing(SmilesBond bond)
            {
                HashSet<int> seen = new HashSet<int> { bond.A };
                Stack<int> pending = new Stack<int>(seen);
                while (pending.Count > 0)
                {
                    int atom = pending.Pop();
                    foreach (SmilesBond b in Bonds.Where(b => b != bond && (b.A == atom || b.B == atom)))
                    {
                        int next = b.A == atom ? b.B : b.A;
                        if (next == bond.B)
                        {
                            return true;
                        }

                        if (seen.Add(next))
                        {
                            pending.Push(next);
                        }
                    }
                }

                return false;
            }

            // Replace aromatic bonds with alternating single and double bonds. Each aromatic atom that still has a
            // spare valence (counting aromatic bonds as single) must receive exactly one double bond.
            public void Kekulise(List<string> warnings)
            {
                List<SmilesBond> aromaticBonds = Bonds.Where(b => b.Order == 4).ToList();
                if (aromaticBonds.Count == 0)
                {
                    return;
                }

                bool[] needsDouble = Enumerable.Range(0, Atoms.Count)
                                               .Select(a => Atoms[a].Aromatic && SpareValence(a) >= 1).ToArray();
                int[] mate = Enumerable.Repeat(-1, Atoms.Count).ToArray();
                List<int>[] options = Enumerable.Range(0, Atoms.Count).Select(_ => new List<int>()).ToArray();
                foreach (SmilesBond b in aromaticBonds.Where(b => needsDouble[b.A] && needsDouble[b.B]))
                {
                    options[b.A].Add(b.B);
                    options[b.B].Add(b.A);
                }

                if (!Match())
                {
                    warnings.Add(
                        "The aromatic system could not be kekulised, so its bonds have been left as aromatic.");
                    return;
                }

                foreach (SmilesBond b in aromaticBonds)
                {
                    b.Order = mate[b.A] == b.B ? 2 : 1;
                }

                // Local Function
                // Backtracking matching, always extending the atom with the fewest choices left.
                bool Match()
                {
                    int best = -1, bestCount = int.MaxValue;
                    for (int a = 0; a < Atoms.Count; a++)
                    {
                        if (needsDouble[a] && mate[a] < 0)
                        {
                            int count = options[a].Count(o => mate[o] < 0);
                            if (count == 0)
                            {
                                return false;
                            }

                            if (count < bestCount)
                            {
                                best = a;
                                bestCount = count;
                            }
                        }
                    }

                    if (best < 0)
                    {
                        return true;
                    }

                    foreach (int other in options[best].Where(o => mate[o] < 0).ToList())
                    {
                        mate[best] = other;
                        mate[other] = best;
                        if (Match())
                        {
                            return true;
                        }

                        mate[best] = mate[other] = -1;
                    }

                    return false;
                }
            }

            // Spare valence of an aromatic atom, counting its aromatic bonds as single and any bracket hydrogens.
            private int SpareValence(int atom)
            {
                int used = Bonds.Where(b => b.A == atom || b.B == atom).Sum(b => b.Order == 4 ? 1 : b.Order)
                           + (Atoms[atom].HydrogenCount ?? 0);
                int charge = Atoms[atom].Charge;
                int[] valences;
                switch (Atoms[atom].Symbol)
                {
                    case "B": valences = new[] { 3 - charge }; break;
                    case "C": valences = new[] { 4 - Math.Abs(charge) }; break;
                    case "N":
                    case "P":
                    case "As": valences = new[] { 3 + charge, 5 + charge }; break;
                    case "O": valences = new[] { 2 + charge }; break;
                    case "S":
                    case "Se":
                    case "Te": valences = new[] { 2 + charge, 4 + charge, 6 + charge }; break;
                    default: return 0;
                }

                int target = valences.Where(v => v >= used).DefaultIfEmpty(used).Min();
                return target - used;
            }

            // Cis (true) or trans (false) for each double bond with a directional bond at both ends,
            // relative to the first other neighbour at each end.
            public Dictionary<SmilesBond, bool> DoubleBondStereo()
            {
                Dictionary<SmilesBond, bool> result = new Dictionary<SmilesBond, bool>();
                foreach (SmilesBond bond in Bonds.Where(b => b.Order == 2))
                {
                    int? startSide = Side(bond.A, bond);
                    int? endSide = Side(bond.B, bond);
                    if (startSide.HasValue && endSide.HasValue)
                    {
                        result[bond] = startSide.Value == endSide.Value;
                    }
                }

                return result;

                // Local Function
                // Which side of the double bond the first other neighbour of 'centre' is on (+1 above, -1 below).
                int? Side(int centre, SmilesBond doubleBond)
                {
                    List<SmilesBond> others = Bonds.Where(b => b != doubleBond && (b.A == centre || b.B == centre))
                                                   .ToList();
                    if (others.Count == 0)
                    {
                        return null;
                    }

                    int first = Other(others[0], centre);
                    SmilesBond marked = others.FirstOrDefault(b => b.Direction != '\0');
                    if (marked == null)
                    {
                        return null;
                    }

                    // "N/C" puts N below C; "C/N" puts N above C. A backslash reverses both.
                    int side = (marked.A == centre) == (marked.Direction == '/') ? 1 : -1;
                    // Any other substituent on the same end sits on the opposite side.
                    return Other(marked, centre) == first ? side : -side;
                }

                // Local Function
                int Other(SmilesBond b, int atom)
                {
                    return b.A == atom ? b.B : b.A;
                }
            }
        }
    }
}
