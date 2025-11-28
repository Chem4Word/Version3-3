// ---------------------------------------------------------------------------
//  Copyright (c) 2026, The .NET Foundation.
//  This software is released under the Apache Licence, Version 2.0.
//  The licence and further copyright text can be found in the file LICENCE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------
using Chem4Word.Model2;
using Xunit;

namespace Chem4WordTests
{
    public class PathTests
    {
        [Theory]
        [InlineData("/m1")]
        [InlineData("/m1/m2")]
        public void ParseIntoMolecule(string path)
        {
            // Arrange
            Model multiLevel = ModelHelper.ConstructMultiLevelMolecule();

            // Act
            Molecule molecule = multiLevel.GetByPath(path) as Molecule;

            // Assert
            Assert.NotNull(molecule);
            Assert.Equal(path, molecule.Path);
        }

        [Theory]
        [InlineData("/m1/m2/m3/a1")]
        [InlineData("/m1/m2/m3/a2")]
        public void ParseIntoAtoms(string path)
        {
            // Arrange
            Model multiLevel = ModelHelper.ConstructMultiLevelMolecule();
            // Act
            Atom atom = multiLevel.GetByPath(path) as Atom;

            // Assert
            Assert.NotNull(atom);
            Assert.Equal(path, atom.Path);
        }

        [Theory]
        [InlineData("/m1/m2/a1")]
        [InlineData("/m1/m2/a4/a2")]
        public void ParseIntoAtomsFails(string path)
        {
            // Arrange
            Model multiLevel = ModelHelper.ConstructMultiLevelMolecule();

            // Act
            Atom atom = multiLevel.GetByPath(path) as Atom;
            // Assert
            Assert.Null(atom);
        }

        [Theory]
        [InlineData("/m1/m2/m3/b1")]
        public void ParseIntoBonds(string path)
        {
            // Arrange
            Model multiLevel = ModelHelper.ConstructMultiLevelMolecule();

            // Act
            Bond bond = multiLevel.GetByPath(path) as Bond;
            //assert

            // Assert
            Assert.NotNull(bond);
            Assert.Equal(bond.Path, path);
        }

        [Theory]
        [InlineData("/m1/m2/m3/b2")]
        [InlineData("/m1/b1")]
        public void ParseIntoBondsFails(string path)
        {
            // Arrange
            Model multiLevel = ModelHelper.ConstructMultiLevelMolecule();

            // Act
            Bond bond = multiLevel.GetByPath(path) as Bond;

            // Assert
            Assert.Null(bond);
        }

        [Theory]
        [InlineData("/m5")]
        [InlineData("/m1/m4")]
        public void ParseFails(string path)
        {
            // Arrange
            Model multiLevel = ModelHelper.ConstructMultiLevelMolecule();

            // Act
            Molecule molecule = multiLevel.GetByPath(path) as Molecule;

            // Assert
            Assert.Null(molecule);
        }

        [Theory]
        [InlineData("/m1/m2/m3/a1", "/m1/m2/m3", "a1")]
        [InlineData("/m1/m2/m3/b1", "/m1/m2/m3", "b1")]
        public void AbsVsRelative(string absolutePath, string moleculePath, string relativePath)
        {
            // Arrange
            Model multiLevel = ModelHelper.ConstructMultiLevelMolecule();

            // Act
            StructuralObject obj = multiLevel.GetByPath(absolutePath);
            Molecule mol = multiLevel.GetByPath(moleculePath) as Molecule;
            StructuralObject child = mol.GetByPath(relativePath);

            // Assert
            Assert.True(obj.Path == child.Path);
        }

        [Theory]
        [InlineData("/m1/m2/m3/a1", "/m1/m2", "a1")]
        [InlineData("/m1/m2/m3/b1", "/m1/m2/m3", "b2")]
        public void AbsVsRelativeFails(string absolutePath, string moleculePath, string relativePath)
        {
            // Arrange
            Model multiLevel = ModelHelper.ConstructMultiLevelMolecule();

            // Act
            StructuralObject obj = multiLevel.GetByPath(absolutePath);
            Molecule mol = multiLevel.GetByPath(moleculePath) as Molecule;
            StructuralObject child = mol.GetByPath(relativePath);

            // Assert
            Assert.False(child == obj);
        }
    }
}
