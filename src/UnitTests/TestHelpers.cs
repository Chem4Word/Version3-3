// ---------------------------------------------------------------------------
//  Copyright (c) 2026, The .NET Foundation.
//  This software is released under the Apache Licence, Version 2.0.
//  The licence and further copyright text can be found in the file LICENCE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.Model2;
using System.Windows;

namespace Chem4WordTests
{
    public static class TestHelpers
    {
        #region Support functions

        public static Model CreateDoubleNestedMolecule()
        {
            var model = new Model();

            var atom1 = new Atom
            {
                Id = "a1",
                Position = new Point(0, 0),
                Element = ModelGlobals.PeriodicTable.P
            };

            var atom2 = new Atom
            {
                Id = "a2",
                Position = new Point(10, 10),
                Element = ModelGlobals.PeriodicTable.O
            };

            var atom3 = new Atom
            {
                Id = "a3",
                Position = new Point(20, 20),
                Element = ModelGlobals.PeriodicTable.N
            };

            var atom4 = new Atom
            {
                Id = "a4",
                Position = new Point(30, 30),
                Element = ModelGlobals.PeriodicTable.Y
            };

            var molecule1 = new Molecule
            {
                Id = "m1"
            };
            var molecule2 = new Molecule
            {
                Id = "m2"
            };
            var molecule3 = new Molecule
            {
                Id = "m3"
            };
            var molecule4 = new Molecule
            {
                Id = "m4"
            };
            var molecule5 = new Molecule
            {
                Id = "m5"
            };
            var molecule6 = new Molecule
            {
                Id = "m6"
            };
            var molecule7 = new Molecule
            {
                Id = "m7"
            };

            molecule3.AddAtom(atom1);
            atom1.Parent = molecule3;

            molecule4.AddAtom(atom2);
            atom2.Parent = molecule4;

            molecule6.AddAtom(atom3);
            atom3.Parent = molecule6;

            molecule7.AddAtom(atom4);
            atom4.Parent = molecule7;

            molecule2.AddMolecule(molecule3);
            molecule3.Parent = molecule2;
            molecule2.AddMolecule(molecule4);
            molecule4.Parent = molecule2;

            molecule5.AddMolecule(molecule6);
            molecule6.Parent = molecule5;
            molecule5.AddMolecule(molecule7);
            molecule7.Parent = molecule5;

            molecule1.AddMolecule(molecule2);
            molecule2.Parent = molecule1;
            molecule1.AddMolecule(molecule5);
            molecule5.Parent = molecule1;

            model.AddMolecule(molecule1);
            molecule1.Parent = model;

            return model;
        }

        public static Model CreateNestedMolecule()
        {
            var model = new Model();

            var atom1 = new Atom
            {
                Id = "a1",
                Position = new Point(0, 0),
                Element = ModelGlobals.PeriodicTable.C
            };

            var atom2 = new Atom
            {
                Id = "a2",
                Position = new Point(10, 10),
                Element = ModelGlobals.PeriodicTable.C
            };

            var atom3 = new Atom
            {
                Id = "a3",
                Position = new Point(20, 20),
                Element = ModelGlobals.PeriodicTable.F
            };

            var molecule1 = new Molecule
            {
                Id = "m1"
            };
            var molecule2 = new Molecule
            {
                Id = "m2"
            };
            var molecule3 = new Molecule
            {
                Id = "m3"
            };

            molecule2.AddAtom(atom1);
            atom1.Parent = molecule2;
            molecule2.AddAtom(atom2);
            atom2.Parent = molecule2;

            molecule3.AddAtom(atom3);
            atom3.Parent = molecule3;

            var bond1 = new Bond(atom1, atom2)
            {
                Id = "b1",
                Order = ModelConstants.OrderSingle
            };
            molecule2.AddBond(bond1);
            bond1.Parent = molecule2;

            model.AddMolecule(molecule1);
            molecule1.Parent = model;

            molecule1.AddMolecule(molecule2);
            molecule2.Parent = molecule1;

            molecule1.AddMolecule(molecule3);
            molecule3.Parent = molecule1;

            return model;
        }

        public static Model CreateSimpleMolecule()
        {
            var model = new Model();

            var atom1 = new Atom
            {
                Id = "a1",
                Position = new Point(0, 0),
                Element = ModelGlobals.PeriodicTable.C
            };

            var atom2 = new Atom
            {
                Id = "a2",
                Position = new Point(10, 10),
                Element = ModelGlobals.PeriodicTable.C
            };

            var atom3 = new Atom
            {
                Id = "a3",
                Position = new Point(20, 20),
                Element = ModelGlobals.PeriodicTable.F
            };

            var molecule = new Molecule
            {
                Id = "m1"
            };
            molecule.AddAtom(atom1);
            atom1.Parent = molecule;
            molecule.AddAtom(atom2);
            atom2.Parent = molecule;
            molecule.AddAtom(atom3);
            atom3.Parent = molecule;

            var bond1 = new Bond(atom1, atom2)
            {
                Id = "b1",
                Order = ModelConstants.OrderSingle
            };
            molecule.AddBond(bond1);
            bond1.Parent = molecule;
            var bond2 = new Bond(atom2, atom3)
            {
                Id = "b2",
                Order = ModelConstants.OrderSingle
            };
            molecule.AddBond(bond2);
            bond2.Parent = molecule;

            model.AddMolecule(molecule);
            molecule.Parent = model;

            return model;
        }

        public static void AddHBonds(Molecule molecule, Atom atom, int bonds)
        {
            for (var i = 0; i < bonds; i++)
            {
                var h = new Atom();
                h.Element = ModelGlobals.PeriodicTable.H;

                molecule.AddAtom(h);
                h.Parent = molecule;

                var bond = new Bond(atom, h);
                bond.Order = "S";
                molecule.AddBond(bond);
                bond.Parent = molecule;
            }
        }

        #endregion Support functions
    }
}
