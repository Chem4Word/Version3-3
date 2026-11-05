// ---------------------------------------------------------------------------
//  Copyright (c) 2026, The .NET Foundation.
//  This software is released under the Apache Licence, Version 2.0.
//  The licence and further copyright text can be found in the file LICENCE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.Model2.Enums;
using Chem4Word.Model2.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace Chem4Word.Model2.Converters.Smiles
{
    /// <summary>
    /// Imports SMILES (Daylight / OpenSMILES) into a Chem4Word model, using CleanerV2 for the 2D coordinates.
    ///
    /// Supported: organic subset and bracket atoms (isotope, chirality, H count, charge, atom class),
    /// bonds - = # $ : / \ and '.', branches, ring closures (0-9, %nn, %(nnn)), aromatic atoms (kekulised),
    /// tetrahedral stereo (@, @@, @TH1, @TH2) drawn as wedge/hatch and double bond geometry from / and \.
    /// Not supported: reaction SMILES, @AL/@SP/@TB/@OH stereo (ignored with a warning).
    ///
    /// Each non-empty line is one molecule; anything after the first whitespace is used as its name.
    /// Disconnected parts of one SMILES become child molecules, as SdFileConverter does.
    /// </summary>
    public class SmilesConverter
    {
        public string Description => "SMILES";
        public string[] Extensions => new[] { "*.smi", "*.smiles" };

        public const int ImplicitNeighbour = -1;
        public const int PendingRingBond = -2;

        public static readonly HashSet<string> PseudoElements = new HashSet<string> { "Du", "M", "R", "X", "D", "T" };

        public Model Import(object data) => Import(data, null);

        /// <param name="data">SMILES text; one structure per line</param>
        /// <param name="bondLength">Bond length to lay out with; defaults to CleanerV2.DefaultBondLength</param>
        public Model Import(object data, double? bondLength)
        {
            Model model = new Model();

            if (data is string text)
            {
                List<Record> records = new List<Record>();

                foreach (string line in text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    string smiles = line.Trim();
                    if (smiles.Length == 0)
                    {
                        continue;
                    }

                    Molecule molecule = new Molecule();
                    try
                    {
                        ParsedSmiles parsed = Parser.Parse(smiles, molecule.Warnings);
                        Kekuliser.Run(parsed, molecule.Warnings);
                        records.Add(Build(parsed, molecule));
                    }
                    catch (FormatException ex)
                    {
                        molecule.Errors.Add($"SMILES '{smiles}' - {ex.Message}");
                    }

                    model.AddMolecule(molecule);
                    molecule.Parent = model;
                }

                if (records.Count > 0)
                {
                    double length = Cleaners.V2.Cleaner.Clean(model, bondLength);
                    foreach (Record record in records)
                    {
                        FixDoubleBondGeometry(record);
                        AssignWedges(record);
                    }
                    SpreadMolecules(records, length);
                }

                model.Relabel(true);
                model.Refresh();
            }

            return model;
        }

        #region Building the model

        private static Record Build(ParsedSmiles parsed, Molecule root)
        {
            int n = parsed.Atoms.Count;
            Record record = new Record
            {
                Parsed = parsed,
                Molecule = root,
                Atoms = new Atom[n],
                Bonds = new Bond[parsed.Bonds.Count],
                IsBridge = parsed.IsBridge
            };

            List<int[]> components = parsed.Components();
            Molecule[] owner = new Molecule[n];
            foreach (int[] component in components)
            {
                Molecule target = root;
                if (components.Count > 1)
                {
                    target = new Molecule { Parent = root };
                    root.AddMolecule(target);
                }

                foreach (int i in component)
                {
                    SmilesAtom source = parsed.Atoms[i];
                    Atom atom = new Atom
                    {
                        Element = ModelGlobals.PeriodicTable.Elements[source.Symbol == "*" ? "R" : source.Symbol],
                        FormalCharge = source.Charge == 0 ? (int?)null : source.Charge,
                        IsotopeNumber = source.Isotope,
                        Position = new Point(0, 0)
                    };
                    target.AddAtom(atom);
                    atom.Parent = target;
                    record.Atoms[i] = atom;
                    owner[i] = target;
                }
            }

            for (int b = 0; b < parsed.Bonds.Count; b++)
            {
                SmilesBond source = parsed.Bonds[b];
                Bond bond = new Bond(record.Atoms[source.From], record.Atoms[source.To])
                {
                    Order = ChemOrder(source, root.Warnings)
                };
                owner[source.From].AddBond(bond);
                bond.Parent = owner[source.From];
                record.Bonds[b] = bond;
            }

            int electrons = 0;
            for (int i = 0; i < n; i++)
            {
                if (parsed.Atoms[i].Bracket && parsed.Atoms[i].Symbol != "*")
                {
                    MatchHydrogenCount(record, i, owner[i], ref electrons);
                }
            }

            return record;
        }

        private static string ChemOrder(SmilesBond bond, List<string> warnings)
        {
            if (bond.Aromatic)
            {
                return ModelConstants.OrderAromatic;
            }

            switch (bond.Order)
            {
                case 2:
                    return ModelConstants.OrderDouble;

                case 3:
                    return ModelConstants.OrderTriple;

                case 4:
                    warnings.Add("Quadruple bond imported with order 'other'");
                    return ModelConstants.OrderOther;

                default:
                    return ModelConstants.OrderSingle;
            }
        }

        // Bracket atoms state their H count; Chem4Word infers one from valence, so add explicit H atoms
        // when it would infer too few, and radical / carbenoid electrons when it would infer too many.
        private static void MatchHydrogenCount(Record record, int index, Molecule owner, ref int electrons)
        {
            Atom atom = record.Atoms[index];
            int wanted = record.Parsed.Atoms[index].HydrogenCount;
            int added = 0;

            while (added + Math.Max(atom.ImplicitHydrogenCount, 0) < wanted)
            {
                Atom hydrogen = new Atom { Element = ModelGlobals.PeriodicTable.H, Position = new Point(0, 0) };
                owner.AddAtom(hydrogen);
                hydrogen.Parent = owner;

                Bond bond = new Bond(atom, hydrogen) { Order = ModelConstants.OrderSingle };
                owner.AddBond(bond);
                bond.Parent = owner;

                if (added++ == 0)
                {
                    record.HydrogenFor[index] = hydrogen;
                }
            }

            int excess = added + Math.Max(atom.ImplicitHydrogenCount, 0) - wanted;
            while (excess > 0)
            {
                ElectronType type = excess >= 2 ? ElectronType.Carbenoid : ElectronType.Radical;
                atom.AddElectron(ElectronHelper.MakeElectron(atom, ++electrons, type));
                excess -= type == ElectronType.Carbenoid ? 2 : 1;
            }
        }

        #endregion Building the model

        #region Stereochemistry after layout

        // CleanerV2 does not know about / and \, so reflect one side of any acyclic double bond drawn the wrong way round.
        private static void FixDoubleBondGeometry(Record record)
        {
            ParsedSmiles parsed = record.Parsed;
            for (int d = 0; d < parsed.Bonds.Count; d++)
            {
                SmilesBond bond = parsed.Bonds[d];
                if (bond.Order != 2 || bond.Aromatic)
                {
                    continue;
                }

                (int sa, int flagA) = DirectionalSubstituent(parsed, bond.From, d);
                (int sb, int flagB) = DirectionalSubstituent(parsed, bond.To, d);
                if (sa < 0 || sb < 0)
                {
                    continue;
                }

                Point a = record.Atoms[bond.From].Position, b = record.Atoms[bond.To].Position;
                Point pa = record.Atoms[sa].Position, pb = record.Atoms[sb].Position;
                bool wantCis = flagA == flagB;
                bool isCis = Cross(a, b, pa) * Cross(a, b, pb) > 0;
                if (wantCis == isCis)
                {
                    continue;
                }

                if (!record.IsBridge[d])
                {
                    record.Molecule.Warnings.Add($"Could not honour the cis/trans geometry of ring double bond {bond.From + 1}={bond.To + 1}");
                    continue;
                }

                List<Atom> sideA = Side(record.Atoms[bond.From], record.Atoms[bond.To]);
                List<Atom> sideB = Side(record.Atoms[bond.To], record.Atoms[bond.From]);
                foreach (Atom atom in sideA.Count < sideB.Count ? sideA : sideB)
                {
                    atom.Position = Reflect(atom.Position, a, b);
                }
            }
        }

        // First neighbour of 'atom' (other than across bond 'exclude') joined by / or \, with its up/down flag relative to 'atom'.
        private static (int neighbour, int flag) DirectionalSubstituent(ParsedSmiles parsed, int atom, int exclude)
        {
            for (int b = 0; b < parsed.Bonds.Count; b++)
            {
                SmilesBond bond = parsed.Bonds[b];
                if (b == exclude || bond.Direction == 0)
                {
                    continue;
                }
                if (bond.From == atom)
                {
                    return (bond.To, bond.Direction);
                }
                if (bond.To == atom)
                {
                    return (bond.From, -bond.Direction);
                }
            }
            return (-1, 0);
        }

        // Atoms reachable from 'start' without passing through 'blocked'.
        private static List<Atom> Side(Atom start, Atom blocked)
        {
            HashSet<Atom> seen = new HashSet<Atom> { start, blocked };
            List<Atom> result = new List<Atom> { start };
            Queue<Atom> queue = new Queue<Atom>(result);
            while (queue.Count > 0)
            {
                foreach (Atom next in queue.Dequeue().Neighbours)
                {
                    if (seen.Add(next))
                    {
                        result.Add(next);
                        queue.Enqueue(next);
                    }
                }
            }
            return result;
        }

        private static double Cross(Point origin, Point along, Point p) =>
            (along.X - origin.X) * (p.Y - origin.Y) - (along.Y - origin.Y) * (p.X - origin.X);

        private static Point Reflect(Point p, Point a, Point b)
        {
            double ux = b.X - a.X, uy = b.Y - a.Y;
            double length = Math.Sqrt(ux * ux + uy * uy);
            ux /= length;
            uy /= length;
            double t = (p.X - a.X) * ux + (p.Y - a.Y) * uy;
            double fx = a.X + t * ux, fy = a.Y + t * uy;
            return new Point(2 * fx - p.X, 2 * fy - p.Y);
        }

        private static void AssignWedges(Record record)
        {
            ParsedSmiles parsed = record.Parsed;
            for (int i = 0; i < parsed.Atoms.Count; i++)
            {
                SmilesAtom source = parsed.Atoms[i];
                if (source.Chirality == 0)
                {
                    continue;
                }

                List<int> order = new List<int>(source.Neighbours);
                if (order.Count == 3 && !order.Contains(ImplicitNeighbour))
                {
                    // A lone pair (e.g. sulfoxides) sits where an implicit H would.
                    order.Insert(source.HasPrevious ? 1 : 0, ImplicitNeighbour);
                }
                if (order.Count != 4 || order.Count(k => k == ImplicitNeighbour) > 1)
                {
                    record.Molecule.Warnings.Add($"Stereo on atom {i + 1} ignored as it does not have four neighbours");
                    continue;
                }

                Atom centre = record.Atoms[i];
                Atom[] neighbours = order.Select(k => k == ImplicitNeighbour
                                                     ? (record.HydrogenFor.TryGetValue(i, out Atom h) ? h : null)
                                                     : record.Atoms[k]).ToArray();

                Atom chosen = null;
                Bond wedge = null;
                int bestScore = int.MinValue;
                foreach (Atom neighbour in neighbours.Where(a => a != null))
                {
                    (Bond bond, int index) = BondBetween(record, centre, neighbour);
                    if (bond == null || bond.Order != ModelConstants.OrderSingle || bond.Stereo != BondStereo.None)
                    {
                        continue;
                    }

                    int k = Array.IndexOf(record.Atoms, neighbour);
                    // Avoid wedging towards another stereocentre or a double bond, whose geometry the wedge would obscure.
                    int score = (k >= 0 && parsed.Atoms[k].Chirality != 0 ? 0 : 16)
                                + (neighbour.Bonds.Any(o => o.Order == ModelConstants.OrderDouble) ? 0 : 8)
                                + (index < 0 || record.IsBridge[index] ? 4 : 0)
                                + (neighbour.Degree == 1 ? 2 : 0)
                                + (neighbour.IsHydrogen ? 1 : 0);
                    if (score > bestScore)
                    {
                        bestScore = score;
                        chosen = neighbour;
                        wedge = bond;
                    }
                }

                if (wedge == null)
                {
                    record.Molecule.Warnings.Add($"Stereo on atom {i + 1} ignored as no bond could carry a wedge");
                    continue;
                }

                double volume = SignedVolume(centre, neighbours, chosen);
                if (Math.Abs(volume) < 1e-6)
                {
                    record.Molecule.Warnings.Add($"Stereo on atom {i + 1} ignored as its layout is ambiguous");
                    continue;
                }

                wedge.StartAtomInternalId = centre.InternalId;
                wedge.EndAtomInternalId = chosen.InternalId;

                // @ is anticlockwise, which is a negative volume in a right handed frame.
                bool anticlockwise = volume < 0;
                wedge.Stereo = anticlockwise == (source.Chirality == 1) ? BondStereo.Wedge : BondStereo.Hatch;
            }
        }

        private static (Bond bond, int index) BondBetween(Record record, Atom a, Atom b)
        {
            for (int k = 0; k < record.Bonds.Length; k++)
            {
                Bond bond = record.Bonds[k];
                if (bond.StartAtom == a && bond.EndAtom == b || bond.StartAtom == b && bond.EndAtom == a)
                {
                    return (bond, k);
                }
            }
            return (a.BondBetween(b), -1);
        }

        // Volume of the four neighbour directions with 'raised' coming out of the page; a null neighbour is the implicit H / lone pair.
        private static double SignedVolume(Atom centre, Atom[] neighbours, Atom raised)
        {
            double[][] v = new double[4][];
            for (int k = 0; k < 4; k++)
            {
                if (neighbours[k] != null)
                {
                    // Chem4Word has Y pointing down; flip it to get a right handed frame with Z towards the viewer.
                    double dx = neighbours[k].Position.X - centre.Position.X;
                    double dy = centre.Position.Y - neighbours[k].Position.Y;
                    double length = Math.Sqrt(dx * dx + dy * dy);
                    length = length < 1e-9 ? 1 : length;
                    double dz = neighbours[k] == raised ? length : 0;
                    v[k] = new[] { dx / length, dy / length, dz / length };
                }
            }

            int missing = Array.IndexOf(neighbours, null);
            if (missing >= 0)
            {
                v[missing] = new double[3];
                for (int k = 0; k < 4; k++)
                {
                    if (k != missing)
                    {
                        for (int c = 0; c < 3; c++)
                        {
                            v[missing][c] -= v[k][c];
                        }
                    }
                }
            }

            double[] a = Minus(v[1], v[0]), b = Minus(v[2], v[0]), d = Minus(v[3], v[0]);
            return a[0] * (b[1] * d[2] - b[2] * d[1]) - a[1] * (b[0] * d[2] - b[2] * d[0]) + a[2] * (b[0] * d[1] - b[1] * d[0]);

            // Local Function
            double[] Minus(double[] p, double[] q) => new[] { p[0] - q[0], p[1] - q[1], p[2] - q[2] };
        }

        // CleanerV2 centres every molecule on the origin, so line separate SMILES records up left to right.
        private static void SpreadMolecules(List<Record> records, double bondLength)
        {
            double cursor = double.NaN;
            foreach (Record record in records)
            {
                List<Atom> atoms = new List<Atom>();
                record.Molecule.BuildAtomList(atoms);
                double minX = atoms.Min(a => a.Position.X), maxX = atoms.Max(a => a.Position.X);
                double shift = double.IsNaN(cursor) ? 0 : cursor - minX;
                if (shift != 0)
                {
                    foreach (Atom atom in atoms)
                    {
                        atom.Position = new Point(atom.Position.X + shift, atom.Position.Y);
                    }
                }
                cursor = maxX + shift + 2 * bondLength;
            }
        }

        #endregion Stereochemistry after layout
    }
}
