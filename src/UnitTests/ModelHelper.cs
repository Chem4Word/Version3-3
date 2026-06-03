// ---------------------------------------------------------------------------
//  Copyright (c) 2026, The .NET Foundation.
//  This software is released under the Apache Licence, Version 2.0.
//  The licence and further copyright text can be found in the file LICENCE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.Model2;
using System.Windows;

namespace Chem4WordUnitTests
{
    public static class ModelHelper
    {
        // [1]  [4]--[5]
        //  |    |
        // [2]--[3]
        public static Model ConstructChain()
        {
            Model model = new Model();

            Molecule molecule1 = new Molecule
            {
                Id = "m1"
            };
            model.AddMolecule(molecule1);
            molecule1.Parent = model;

            Atom atom1 = AddAtomToMolecule(molecule1, "a1");
            Atom atom2 = AddAtomToMolecule(molecule1, "a2");
            Atom atom3 = AddAtomToMolecule(molecule1, "a3");
            Atom atom4 = AddAtomToMolecule(molecule1, "a4");
            Atom atom5 = AddAtomToMolecule(molecule1, "a5");

            AddBondBetween(molecule1, atom1, atom2, "b1");
            AddBondBetween(molecule1, atom2, atom3, "b2");
            AddBondBetween(molecule1, atom3, atom4, "b3");
            AddBondBetween(molecule1, atom4, atom5, "b4");

            molecule1.RebuildRings(force: true);

            model.Relabel(true);

            return model;
        }

        // [1]--[4]--[5]
        //  |    |
        // [2]--[3]
        public static Model ConstructRing()
        {
            Model model = new Model();

            Molecule molecule1 = new Molecule
            {
                Id = "m1"
            };
            model.AddMolecule(molecule1);
            molecule1.Parent = model;

            Atom atom1 = AddAtomToMolecule(molecule1, "a1");
            Atom atom2 = AddAtomToMolecule(molecule1, "a2");
            Atom atom3 = AddAtomToMolecule(molecule1, "a3");
            Atom atom4 = AddAtomToMolecule(molecule1, "a4");
            Atom atom5 = AddAtomToMolecule(molecule1, "a5");

            AddBondBetween(molecule1, atom1, atom2, "b1");
            AddBondBetween(molecule1, atom2, atom3, "b2");
            AddBondBetween(molecule1, atom3, atom4, "b3");
            AddBondBetween(molecule1, atom4, atom1, "b4");
            AddBondBetween(molecule1, atom4, atom5, "b5");

            molecule1.RebuildRings(force: true);

            model.Relabel(true);

            return model;
        }

        public static Model ConstructSingleMolecule()
        {
            Model model = new Model();

            Molecule molecule1 = new Molecule
            {
                Id = "m1"
            };
            model.AddMolecule(molecule1);
            molecule1.Parent = model;

            Atom atom1 = AddAtomToMolecule(molecule1, "a1");
            Atom atom2 = AddAtomToMolecule(molecule1, "a2");

            AddBondBetween(molecule1, atom1, atom2, "b1");

            model.Relabel(true);

            return model;
        }

        public static Model ConstructMultiLevelMolecule()
        {
            Model model = new Model();

            Molecule molecule0 = new Molecule
            {
                Id = "m0"
            };
            model.AddMolecule(molecule0);
            molecule0.Parent = model;

            Molecule molecule1 = new Molecule
            {
                Id = "m1"
            };
            molecule0.AddMolecule(molecule1);
            molecule1.Parent = molecule0;

            Molecule molecule2 = new Molecule
            {
                Id = "m2"
            };
            molecule1.AddMolecule(molecule2);
            molecule2.Parent = molecule1;

            Atom atom1 = AddAtomToMolecule(molecule2, "a1");
            Atom atom2 = AddAtomToMolecule(molecule2, "a2");

            AddBondBetween(molecule2, atom1, atom2, "b1");

            model.Relabel(true);

            return model;
        }

        public static Model CreateBadModel()
        {
            Model model = new Model();
            Atom atom1 = new Atom
            {
                Position = new Point(0, 0),
                Element = ModelGlobals.PeriodicTable.C
            };

            Atom atom2 = new Atom
            {
                Position = new Point(10, 10),
                Element = ModelGlobals.PeriodicTable.C
            };

            Molecule molecule1 = new Molecule();
            molecule1.AddAtom(atom1);
            atom1.Parent = molecule1;
            molecule1.AddAtom(atom2);
            atom2.Parent = molecule1;

            Bond bond1 = new Bond(atom1, atom2)
            {
                Order = ModelConstants.OrderSingle
            };
            molecule1.AddBond(bond1);
            bond1.Parent = molecule1;

            Bond bond2 = new Bond(atom1, atom2)
            {
                Order = ModelConstants.OrderSingle
            };
            molecule1.AddBond(bond2);
            bond2.Parent = molecule1;

            model.AddMolecule(molecule1);
            molecule1.Parent = model;

            model.Relabel(true);

            return model;
        }

        public static Model CreateSingleMolecule()
        {
            Model model = new Model();
            Atom atom1 = new Atom
            {
                Position = new Point(0, 0),
                Element = ModelGlobals.PeriodicTable.C
            };

            Atom atom2 = new Atom
            {
                Position = new Point(10, 10),
                Element = ModelGlobals.PeriodicTable.C
            };

            Molecule molecule1 = new Molecule();
            molecule1.AddAtom(atom1);
            atom1.Parent = molecule1;
            molecule1.AddAtom(atom2);
            atom2.Parent = molecule1;

            Bond bond1 = new Bond(atom1, atom2)
            {
                Order = ModelConstants.OrderSingle
            };
            molecule1.AddBond(bond1);
            bond1.Parent = molecule1;

            model.AddMolecule(molecule1);
            molecule1.Parent = model;

            model.Relabel(true);

            return model;
        }

        public static Model CreateTwoFlatMolecules()
        {
            Model model = new Model();
            Atom atom1 = new Atom
            {
                Position = new Point(0, 0),
                Element = ModelGlobals.PeriodicTable.C
            };

            Atom atom2 = new Atom
            {
                Position = new Point(10, 10),
                Element = ModelGlobals.PeriodicTable.C
            };

            Bond bond1 = new Bond(atom1, atom2)
            {
                Order = ModelConstants.OrderSingle
            };

            Molecule molecule1 = new Molecule();
            molecule1.AddAtom(atom1);
            atom1.Parent = molecule1;
            molecule1.AddAtom(atom2);
            atom2.Parent = molecule1;
            molecule1.AddBond(bond1);
            bond1.Parent = molecule1;

            Atom atom3 = new Atom
            {
                Position = new Point(20, 20),
                Element = ModelGlobals.PeriodicTable.C
            };

            Atom atom4 = new Atom
            {
                Position = new Point(30, 30),
                Element = ModelGlobals.PeriodicTable.C
            };

            Bond bond2 = new Bond(atom3, atom4)
            {
                Order = ModelConstants.OrderSingle
            };

            Molecule molecule2 = new Molecule();
            molecule2.AddAtom(atom3);
            atom1.Parent = molecule2;
            molecule2.AddAtom(atom4);
            atom2.Parent = molecule2;
            molecule2.AddBond(bond2);
            bond2.Parent = molecule2;

            model.AddMolecule(molecule1);
            molecule1.Parent = model;
            model.AddMolecule(molecule2);
            molecule2.Parent = model;

            model.Relabel(true);

            return model;
        }

        private static Atom AddAtomToMolecule(Molecule molecule, string id)
        {
            Atom atom = new Atom
            {
                Id = id,
                Element = ModelGlobals.PeriodicTable.C
            };
            molecule.AddAtom(atom);
            atom.Parent = molecule;

            return atom;
        }

        private static void AddBondBetween(Molecule molecule, Atom atom1, Atom atom2, string id)
        {
            Bond bond = new Bond(atom1, atom2)
            {
                Id = id,
                Order = ModelConstants.OrderSingle
            };
            molecule.AddBond(bond);
            bond.Parent = molecule;
        }
    }
}
