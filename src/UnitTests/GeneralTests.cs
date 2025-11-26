// ---------------------------------------------------------------------------
//  Copyright (c) 2025, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.Core.Helpers;
using Chem4Word.Model2;
using Chem4Word.Model2.Converters.CML;
using System;
using System.Linq;
using System.Windows;
using Xunit;

namespace Chem4WordTests
{
    public class GeneralTests
    {
        [Fact]
        public void CheckClone()
        {
            var model = new Model();

            var molecule = new Molecule();
            molecule.Id = "m1";
            model.AddMolecule(molecule);
            molecule.Parent = model;

            var startAtom = new Atom();
            startAtom.Id = "a1";
            startAtom.Element = ModelGlobals.PeriodicTable.C;
            startAtom.Position = new Point(5, 5);
            molecule.AddAtom(startAtom);
            startAtom.Parent = molecule;

            var endAtom = new Atom();
            endAtom.Id = "a2";
            endAtom.Element = ModelGlobals.PeriodicTable.C;
            endAtom.Position = new Point(10, 10);
            molecule.AddAtom(endAtom);
            endAtom.Parent = molecule;

            var bond = new Bond(startAtom, endAtom);
            bond.Id = "b1";
            bond.Order = ModelConstants.OrderSingle;
            molecule.AddBond(bond);
            bond.Parent = molecule;

            Assert.True(model.Molecules.Count == 1, $"Expected 1 Molecule; Got {model.Molecules.Count}");

            var a1 = model.Molecules.Values.First().Atoms.Values.First();
            Assert.True(Math.Abs(a1.Position.X - 5.0) < 0.001, $"Expected a1.X = 5; Got {a1.Position.X}");
            Assert.True(Math.Abs(a1.Position.Y - 5.0) < 0.001, $"Expected a1.Y = 5; Got {a1.Position.Y}");

            var clone = model.Copy();

            Assert.True(model.Molecules.Count == 1, $"Expected 1 Molecule; Got {model.Molecules.Count}");
            Assert.True(clone.Molecules.Count == 1, $"Expected 1 Molecule; Got {clone.Molecules.Count}");

            var a2 = clone.Molecules.Values.First().Atoms.Values.First();
            Assert.True(Math.Abs(a2.Position.X - 5.0) < 0.001, $"Expected a2.X = 5; Got {a2.Position.X}");
            Assert.True(Math.Abs(a2.Position.Y - 5.0) < 0.001, $"Expected a2.Y = 5; Got {a2.Position.Y}");

            clone.ScaleToAverageBondLength(5);

            var a3 = model.Molecules.Values.First().Atoms.Values.First();
            Assert.True(Math.Abs(a3.Position.X - 5.0) < 0.001, $"Expected a3.X = 5; Got {a3.Position.X}");
            Assert.True(Math.Abs(a3.Position.Y - 5.0) < 0.001, $"Expected a3.Y = 5; Got {a3.Position.Y}");

            var a4 = clone.Molecules.Values.First().Atoms.Values.First();
            Assert.True(Math.Abs(a4.Position.X - 3.535) < 0.001, $"Expected a4.X = 3.535; Got {a4.Position.X}");
            Assert.True(Math.Abs(a4.Position.Y - 3.535) < 0.001, $"Expected a4.Y = 3.535; Got {a4.Position.Y}");
        }

        [Theory]
        [InlineData("O", 0, false)]
        [InlineData("S", 1, true)]
        [InlineData("N", 2, true)]
        public void CheckAtomRings(string element, int ringCount, bool isInRing)
        {
            var mc = new CMLConverter();
            var m = mc.Import(ResourceHelper.GetStringResource("Two-Rings.xml"));

            // Basic sanity checks
            Assert.True(m.Molecules.Count == 1, $"Expected 1 Molecule; Got {m.Molecules.Count}");
            var molecule = m.Molecules.Values.First();
            Assert.True(molecule.Rings.Count == 2, $"Expected 2 Rings; Got {molecule.Rings.Count}");

            // Get atom to test
            var atoms = molecule.Atoms.Values.Where(a => a.SymbolText == element).ToList();
            Assert.True(atoms.Count == 1, "Expected only one atom");
            var atom = atoms.FirstOrDefault();
            Assert.NotNull(atom);

            Assert.True(atom.RingCount == ringCount, $"Expected RingCount: {ringCount}; Got {atom.RingCount}");
            Assert.True(atom.IsInRing == isInRing, $"Expected IsInRing: {isInRing}; Got {atom.IsInRing}");
        }

        [Theory]
        [InlineData("BalancingVectorCheckTwoBonds.xml")]
        [InlineData("BalancingVectorCheckThreeBonds.xml")]
        public void BalancingVectorIsCorrect(string cmlFile)
        {
            var cmlConverter = new CMLConverter();
            var model = cmlConverter.Import(ResourceHelper.GetStringResource(cmlFile));

            for (var i = 5; i < 200; i += 5)
            {
                model.ScaleToAverageBondLength(i);

                var molecule = model.Molecules.Values.First();
                Assert.Equal(60, molecule.Bonds[0].Angle, 4);
                Assert.Equal(0, molecule.Bonds[1].Angle, 4);

                var atom = molecule.Atoms.Values.First(a => a.Id.Equals("a2"));
                var angle = Vector.AngleBetween(GeometryTool.ScreenNorth, atom.BalancingVector());
                Assert.Equal(120.0, angle, 4);
            }
        }

        [Fact]
        public void CheckFormulasCanBeAdded()
        {
            var model = new Model();

            var molecule = new Molecule();
            molecule.Id = "m1";
            model.AddMolecule(molecule);
            molecule.Parent = model;

            var formula = new TextualProperty();
            formula.FullType = "";
            formula.Value = "";
            molecule.Formulas.Add(formula);

            Assert.True(molecule.Formulas.Count == 1, "Expected count to be 1");
            Assert.Equal("", molecule.Formulas[0].FullType);

            molecule.Formulas[0].FullType = "convention";
            Assert.Equal("convention", molecule.Formulas[0].FullType);
        }

        [Fact]
        public void CheckNamesCanBeAdded()
        {
            var model = new Model();

            var molecule = new Molecule();
            molecule.Id = "m1";
            model.AddMolecule(molecule);
            molecule.Parent = model;

            var name = new TextualProperty();
            name.FullType = "";
            name.Value = "";
            molecule.Names.Add(name);

            Assert.True(molecule.Names.Count == 1, "Expected count to be 1");
            Assert.Equal("", molecule.Names[0].FullType);

            molecule.Names[0].FullType = "dictref";
            Assert.Equal("dictref", molecule.Names[0].FullType);
        }

        [Theory]
        // Single Valent Element
        [InlineData("O", 0, 0, 2, false)]
        [InlineData("O", 0, 1, 1, false)]
        [InlineData("O", 0, 2, 0, false)]
        [InlineData("O", 1, 2, 1, false)]
        [InlineData("O", 1, 3, 0, false)]
        [InlineData("O", 0, 3, 0, true)]
        // Multi Valent Element: Valencies 1,3,5,7
        [InlineData("Cl", 0, 0, 1, false)]
        [InlineData("Cl", 0, 1, 0, false)]
        [InlineData("Cl", 0, 2, 1, false)]
        // Multi Valent Element: Valencies 3,5
        [InlineData("N", 0, 0, 3, false)]
        [InlineData("N", 0, 1, 2, false)]
        [InlineData("N", 0, 5, 0, false)]
        [InlineData("N", 0, 6, 0, true)]
        public void CheckValencyCalculations(string element, int charge, int bonds,
                      int expectedImplicitHCount, bool expectedOverbonding)
        {
            // Arrange
            var model = new Model();

            var molecule = new Molecule();
            model.AddMolecule(molecule);
            molecule.Parent = model;

            var atom = new Atom();
            molecule.AddAtom(atom);
            atom.Parent = molecule;

            atom.Element = ModelGlobals.PeriodicTable.Elements[element];
            atom.FormalCharge = charge;

            TestHelpers.AddHBonds(molecule, atom, bonds);

            var bondOrders = (int)Math.Truncate(atom.BondOrders);

            // Act
            var implicitHydrogen = atom.ImplicitHydrogenCount;
            var over = atom.Overbonded;

            // Assert
            Assert.Equal(bondOrders, bonds);
            Assert.Equal(expectedImplicitHCount, implicitHydrogen);
            Assert.Equal(expectedOverbonding, over);
        }
    }
}
