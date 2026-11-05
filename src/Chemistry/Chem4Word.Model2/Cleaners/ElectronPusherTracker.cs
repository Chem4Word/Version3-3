// ---------------------------------------------------------------------------
//  Copyright (c) 2026, The .NET Foundation.
//  This software is released under the Apache Licence, Version 2.0.
//  The licence and further copyright text can be found in the file LICENCE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace Chem4Word.Model2.Cleaners
{
    /// <summary>
    /// Keeps electron pusher control points attached to their chemistry while the cleaners move atoms.
    /// Each control point is stored relative to the pusher's start → end line, so the curve keeps its
    /// shape as the ends are moved, rotated or rescaled, and is flipped if its molecule ends up mirrored.
    /// </summary>
    internal sealed class ElectronPusherTracker
    {
        private readonly List<ElectronPusherSnapshot> _snapshots = new List<ElectronPusherSnapshot>();
        private readonly Dictionary<Molecule, Dictionary<Atom, Point>> _originalPositions = new Dictionary<Molecule, Dictionary<Atom, Point>>();

        public static ElectronPusherTracker Capture(Model model)
        {
            ElectronPusherTracker tracker = new ElectronPusherTracker();
            foreach (ElectronPusher pusher in model.ElectronPushers.Values)
            {
                Point? start = Anchor(pusher.StartChemistry);
                Point? end = EndAnchor(pusher);
                if (!start.HasValue)
                {
                    continue;
                }

                Molecule molecule = MoleculeOf(pusher.StartChemistry);
                if (molecule != null && !tracker._originalPositions.ContainsKey(molecule))
                {
                    List<Atom> atoms = new List<Atom>();
                    molecule.BuildAtomList(atoms);
                    tracker._originalPositions[molecule] = atoms.ToDictionary(a => a, a => a.Position);
                }

                Vector axis = end.HasValue ? end.Value - start.Value : new Vector();
                bool anchoredAtStart = axis.LengthSquared < 1e-12;
                tracker._snapshots.Add(new ElectronPusherSnapshot
                {
                    Pusher = pusher,
                    Molecule = molecule,
                    AnchoredAtStart = anchoredAtStart,
                    First = anchoredAtStart ? pusher.FirstControlPoint - start.Value : ToFrame(pusher.FirstControlPoint - start.Value, axis),
                    Second = anchoredAtStart ? pusher.SecondControlPoint - start.Value : ToFrame(pusher.SecondControlPoint - start.Value, axis)
                });
            }
            return tracker;
        }

        public void Restore()
        {
            Dictionary<Molecule, bool> mirrored = _originalPositions.ToDictionary(p => p.Key, p => IsMirrored(p.Value));

            foreach (ElectronPusherSnapshot snapshot in _snapshots)
            {
                Point? start = Anchor(snapshot.Pusher.StartChemistry);
                Point? end = EndAnchor(snapshot.Pusher);
                if (!start.HasValue)
                {
                    continue;
                }

                Vector axis = end.HasValue ? end.Value - start.Value : new Vector();
                if (snapshot.AnchoredAtStart || axis.LengthSquared < 1e-12)
                {
                    snapshot.Pusher.FirstControlPoint = start.Value + snapshot.First;
                    snapshot.Pusher.SecondControlPoint = start.Value + snapshot.Second;
                    continue;
                }

                bool flip = snapshot.Molecule != null && mirrored[snapshot.Molecule];
                snapshot.Pusher.FirstControlPoint = start.Value + FromFrame(snapshot.First, axis, flip);
                snapshot.Pusher.SecondControlPoint = start.Value + FromFrame(snapshot.Second, axis, flip);
            }
        }

        // (along, across) as fractions of the axis length.
        private static Vector ToFrame(Vector offset, Vector axis)
        {
            Vector normal = new Vector(-axis.Y, axis.X);
            return new Vector(offset * axis / axis.LengthSquared, offset * normal / axis.LengthSquared);
        }

        private static Vector FromFrame(Vector local, Vector axis, bool flip)
        {
            Vector normal = new Vector(-axis.Y, axis.X);
            return local.X * axis + (flip ? -local.Y : local.Y) * normal;
        }

        // Same anchor points as ElectronPusher.StartPoint/EndPoint, except that an electron follows its parent,
        // because Electron.Centroid is only updated when the electron is drawn.
        private static Point? Anchor(StructuralObject chemistry)
        {
            switch (chemistry)
            {
                case Atom atom:
                    return atom.Position;

                case Bond bond:
                    return bond.MidPoint;

                case Electron electron:
                    return Anchor(electron.Parent);

                default:
                    return null;
            }
        }

        private static Point? EndAnchor(ElectronPusher pusher)
        {
            List<StructuralObject> ends = pusher.EndChemistries;
            if (ends.Count == 1)
            {
                return Anchor(ends[0]);
            }
            if (ends.Count == 2 && ends[0] is Atom a1 && ends[1] is Atom a2)
            {
                return new Point((a1.Position.X + a2.Position.X) / 2, (a1.Position.Y + a2.Position.Y) / 2);
            }
            return null;
        }

        private static Molecule MoleculeOf(StructuralObject chemistry)
        {
            switch (chemistry)
            {
                case Atom atom:
                    return atom.Parent?.RootMolecule;

                case Bond bond:
                    return bond.Parent?.RootMolecule;

                case Electron electron:
                    return MoleculeOf(electron.Parent);

                default:
                    return null;
            }
        }

        // True when a reflection fits the old → new atom positions better than a rotation does.
        private static bool IsMirrored(Dictionary<Atom, Point> before)
        {
            if (before.Count < 3)
            {
                return false;
            }

            double oldX = before.Values.Average(p => p.X), oldY = before.Values.Average(p => p.Y);
            double newX = before.Keys.Average(a => a.Position.X), newY = before.Keys.Average(a => a.Position.Y);
            double sxx = 0, sxy = 0, syx = 0, syy = 0;
            foreach (KeyValuePair<Atom, Point> pair in before)
            {
                double ox = pair.Value.X - oldX, oy = pair.Value.Y - oldY;
                double nx = pair.Key.Position.X - newX, ny = pair.Key.Position.Y - newY;
                sxx += ox * nx;
                sxy += ox * ny;
                syx += oy * nx;
                syy += oy * ny;
            }

            double rotation = (sxx + syy) * (sxx + syy) + (sxy - syx) * (sxy - syx);
            double reflection = (sxx - syy) * (sxx - syy) + (sxy + syx) * (sxy + syx);
            return reflection > rotation;
        }
    }
}
