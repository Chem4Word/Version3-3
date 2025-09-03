// ---------------------------------------------------------------------------
//  Copyright (c) 2025, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.Model2;

namespace Chem4WordTests
{
    public static class ModelHelper
    {
        // [1]  [4]--[5]
        //  |    |
        // [2]--[3]
        public static Model ConstructChain()
        {
            var model = new Model();

            var molecule1 = new Molecule
            {
                Id = "m1"
            };
            model.AddMolecule(molecule1);
            molecule1.Parent = model;

            var atom1 = AddAtomToMolecule(molecule1, "a1");
            var atom2 = AddAtomToMolecule(molecule1, "a2");
            var atom3 = AddAtomToMolecule(molecule1, "a3");
            var atom4 = AddAtomToMolecule(molecule1, "a4");
            var atom5 = AddAtomToMolecule(molecule1, "a5");

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
            var model = new Model();

            var molecule1 = new Molecule
            {
                Id = "m1"
            };
            model.AddMolecule(molecule1);
            molecule1.Parent = model;

            var atom1 = AddAtomToMolecule(molecule1, "a1");
            var atom2 = AddAtomToMolecule(molecule1, "a2");
            var atom3 = AddAtomToMolecule(molecule1, "a3");
            var atom4 = AddAtomToMolecule(molecule1, "a4");
            var atom5 = AddAtomToMolecule(molecule1, "a5");

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
            var model = new Model();

            var molecule1 = new Molecule
            {
                Id = "m1"
            };
            model.AddMolecule(molecule1);
            molecule1.Parent = model;

            var atom1 = AddAtomToMolecule(molecule1, "a1");
            var atom2 = AddAtomToMolecule(molecule1, "a2");

            AddBondBetween(molecule1, atom1, atom2, "b1");

            model.Relabel(true);

            return model;
        }

        public static Model ConstructMultiLevelMolecule()
        {
            var model = new Model();

            var molecule0 = new Molecule
            {
                Id = "m0"
            };
            model.AddMolecule(molecule0);
            molecule0.Parent = model;

            var molecule1 = new Molecule
            {
                Id = "m1"
            };
            molecule0.AddMolecule(molecule1);
            molecule1.Parent = molecule0;

            var molecule2 = new Molecule
            {
                Id = "m2"
            };
            molecule1.AddMolecule(molecule2);
            molecule2.Parent = molecule1;

            var atom1 = AddAtomToMolecule(molecule2, "a1");
            var atom2 = AddAtomToMolecule(molecule2, "a2");

            AddBondBetween(molecule2, atom1, atom2, "b1");

            model.Relabel(true);

            return model;
        }

        private static Atom AddAtomToMolecule(Molecule molecule, string id)
        {
            var atom = new Atom
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
            var bond = new Bond(atom1, atom2)
            {
                Id = id,
                Order = ModelConstants.OrderSingle
            };
            molecule.AddBond(bond);
            bond.Parent = molecule;
        }
    }
}