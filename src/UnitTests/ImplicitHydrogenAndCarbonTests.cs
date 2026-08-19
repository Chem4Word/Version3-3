// ---------------------------------------------------------------------------
//  Copyright (c) 2026, The .NET Foundation.
//  This software is released under the Apache Licence, Version 2.0.
//  The licence and further copyright text can be found in the file LICENCE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.Core.Helpers;
using Chem4Word.Model2.Converters.CML;
using Chem4Word.Model2.Enums;
using System.Linq;
using Xunit;

namespace Chem4WordUnitTests
{
    public class ImplicitHydrogenAndCarbonTests
    {
        [Fact]
        public void SingleMolecule_ExplicitC_NotSet()
        {
            // Arrange
            var model = ModelHelper.ConstructSingleMolecule();
            var atom = model.GetAllAtoms().First(a => a.Id.Equals("a1"));

            // Act
            var effective = atom.InheritedC;

            // Assert
            Assert.False(effective, "Expected effective to be false");
        }

        [Fact]
        public void SingleMolecule_ExplicitC_SetAtAtom()
        {
            // Arrange
            var model = ModelHelper.ConstructSingleMolecule();
            var atom = model.GetAllAtoms().First(a => a.Id.Equals("a1"));

            // Act
            atom.ExplicitC = true;
            var effective = atom.InheritedC;

            var cmlConvertor = new CMLConverter();
            var exported = cmlConvertor.Export(model);
            var imported = cmlConvertor.Import(XmlHelper.AddHeader(exported));

            var a1 = imported.GetAllAtoms().First(a => a.Id.Equals("a1"));

            // Assert
            Assert.True(effective, "Expected effective to be true");
            Assert.True(a1.ExplicitC);
            Assert.False(imported.ExplicitC, "Expected model.ExplicitC to be false");
            Assert.True(imported.ExplicitH.Equals(HydrogenLabels.HeteroAndTerminal), $"Expected imported model.ExplicitH to be {HydrogenLabels.HeteroAndTerminal}, but got {imported.ExplicitH}");
        }

        [Fact]
        public void SingleMolecule_ExplicitC_SetAtMolecule()
        {
            // Arrange
            var model = ModelHelper.ConstructSingleMolecule();
            var molecule = model.GetAllMolecules().First(a => a.Id.Equals("m1"));

            // Act
            molecule.ExplicitC = true;
            var atom = model.GetAllAtoms().First(a => a.Id.Equals("a1"));
            var effective = atom.InheritedC;

            var cmlConvertor = new CMLConverter();
            var exported = cmlConvertor.Export(model);
            var imported = cmlConvertor.Import(XmlHelper.AddHeader(exported));

            var a1 = imported.GetAllAtoms().First(a => a.Id.Equals("a1"));

            // Assert
            Assert.True(effective, "Expected effective to be true");
            Assert.Null(a1.ExplicitC);
            Assert.False(imported.ExplicitC, "Expected model.ExplicitC to be false");
            Assert.True(imported.ExplicitH.Equals(HydrogenLabels.HeteroAndTerminal), $"Expected imported model.ExplicitH to be {HydrogenLabels.HeteroAndTerminal}, but got {imported.ExplicitH}");
        }

        [Fact]
        public void SingleMolecule_ExplicitC_SetAtModel()
        {
            // Arrange
            var model = ModelHelper.ConstructSingleMolecule();

            // Act
            model.ExplicitC = true;
            var atom = model.GetAllAtoms().First(a => a.Id.Equals("a1"));
            var effective = atom.InheritedC;

            var cmlConvertor = new CMLConverter();
            var exported = cmlConvertor.Export(model);
            var imported = cmlConvertor.Import(XmlHelper.AddHeader(exported));

            var a1 = imported.GetAllAtoms().First(a => a.Id.Equals("a1"));

            // Assert
            Assert.True(effective, "Expected effective to be true");
            Assert.Null(a1.ExplicitC);
            Assert.True(imported.ExplicitC, "Expected model.ExplicitC to be true");
            Assert.True(imported.ExplicitH.Equals(HydrogenLabels.HeteroAndTerminal), $"Expected imported model.ExplicitH to be {HydrogenLabels.HeteroAndTerminal}, but got {imported.ExplicitH}");
        }

        [Fact]
        public void MultiLevelMolecule_ExplicitC_NotSet()
        {
            // Arrange
            var model = ModelHelper.ConstructMultiLevelMolecule();
            var atom = model.GetAllAtoms().First(a => a.Id.Equals("a1"));

            // Act
            var effective = atom.InheritedC;

            // Assert
            Assert.False(effective, "Expected effective to be false");
        }

        [Fact]
        public void MultiLevelMolecule_ExplicitC_SetAtAtom()
        {
            // Arrange
            var model = ModelHelper.ConstructMultiLevelMolecule();
            var atom = model.GetAllAtoms().First(a => a.Id.Equals("a1"));

            // Act
            atom.ExplicitC = true;
            var effective = atom.InheritedC;

            // Assert
            Assert.True(effective, "Expected effective to be true");
        }

        [Fact]
        public void MultiLevelMolecule_ExplicitC_SetAtMolecule()
        {
            // Arrange
            var model = ModelHelper.ConstructMultiLevelMolecule();
            var molecule = model.GetAllMolecules().First(a => a.Id.Equals("m1"));

            // Act
            molecule.ExplicitC = true;
            var atom = model.GetAllAtoms().First(a => a.Id.Equals("a1"));
            var effective = atom.InheritedC;

            // Assert
            Assert.True(effective, "Expected effective to be true");
        }

        [Fact]
        public void MultiLevelMolecule_ExplicitC_SetAtModel()
        {
            // Arrange
            var model = ModelHelper.ConstructMultiLevelMolecule();

            // Act
            model.ExplicitC = true;
            var atom = model.GetAllAtoms().First(a => a.Id.Equals("a1"));
            var effective = atom.InheritedC;

            // Assert
            Assert.True(effective, "Expected effective to be true");
        }

        [Fact]
        public void SingleMolecule_ExplicitH_NotSet()
        {
            // Arrange
            var model = ModelHelper.ConstructSingleMolecule();
            var atom = model.GetAllAtoms().First(a => a.Id.Equals("a1"));

            // Act
            var effective = atom.InheritedHydrogenLabels;

            // Assert
            Assert.True(effective.Equals(HydrogenLabels.HeteroAndTerminal), $"Expected effective to be {HydrogenLabels.HeteroAndTerminal}, but got {effective}");
        }

        [Fact]
        public void SingleMolecule_ExplicitH_SetAtAtom()
        {
            // Arrange
            var model = ModelHelper.ConstructSingleMolecule();
            var atom = model.GetAllAtoms().First(a => a.Id.Equals("a1"));

            // Act
            atom.ExplicitH = HydrogenLabels.All;
            var effective = atom.InheritedHydrogenLabels;

            var cmlConvertor = new CMLConverter();
            var exported = cmlConvertor.Export(model);
            var imported = cmlConvertor.Import(exported);

            var a1 = imported.GetAllAtoms().First(a => a.Id.Equals("a1"));

            // Assert
            Assert.True(effective.Equals(HydrogenLabels.All), $"Expected effective to be {HydrogenLabels.All}, but got {effective}");
            Assert.True(a1.ExplicitH.Equals(HydrogenLabels.All), $"Expected imported model.ExplicitH to be {HydrogenLabels.All}, but got {a1.ExplicitH}");
            Assert.False(imported.ExplicitC, "Expected model.ExplicitC to be false");
            Assert.True(imported.ExplicitH.Equals(HydrogenLabels.HeteroAndTerminal), $"Expected imported model.ExplicitH to be {HydrogenLabels.HeteroAndTerminal}, but got {imported.ExplicitH}");
        }

        [Fact]
        public void SingleMolecule_ExplicitH_SetAtMolecule()
        {
            // Arrange
            var model = ModelHelper.ConstructSingleMolecule();
            var molecule = model.GetAllMolecules().First(a => a.Id.Equals("m1"));

            // Act
            molecule.ExplicitH = HydrogenLabels.Hetero;
            var atom = model.GetAllAtoms().First(a => a.Id.Equals("a1"));
            var effective = atom.InheritedHydrogenLabels;

            var cmlConvertor = new CMLConverter();
            var exported = cmlConvertor.Export(model);
            var imported = cmlConvertor.Import(XmlHelper.AddHeader(exported));

            var a1 = imported.GetAllAtoms().First(a => a.Id.Equals("a1"));

            // Assert
            Assert.True(effective.Equals(HydrogenLabels.Hetero), $"Expected effective to be {HydrogenLabels.Hetero}, but got {effective}");
            Assert.Null(a1.ExplicitH);
            Assert.False(imported.ExplicitC, "Expected model.ExplicitC to be false");
            Assert.True(imported.ExplicitH.Equals(HydrogenLabels.HeteroAndTerminal), $"Expected imported model.ExplicitH to be {HydrogenLabels.HeteroAndTerminal}, but got {imported.ExplicitH}");
        }

        [Fact]
        public void SingleMolecule_ExplicitH_SetAtModel()
        {
            // Arrange
            var model = ModelHelper.ConstructSingleMolecule();

            // Act
            model.ExplicitH = HydrogenLabels.None;
            var atom = model.GetAllAtoms().First(a => a.Id.Equals("a1"));
            var effective = atom.InheritedHydrogenLabels;

            var cmlConvertor = new CMLConverter();
            var exported = cmlConvertor.Export(model);
            var imported = cmlConvertor.Import(XmlHelper.AddHeader(exported));

            var a1 = imported.GetAllAtoms().First(a => a.Id.Equals("a1"));

            // Assert
            Assert.True(effective.Equals(HydrogenLabels.None), $"Expected effective to be {HydrogenLabels.None}, but got {effective}");
            Assert.Null(a1.ExplicitH);
            Assert.False(imported.ExplicitC, "Expected model.ExplicitC to be false");
            Assert.True(imported.ExplicitH.Equals(HydrogenLabels.None), $"Expected imported model.ExplicitH to be {HydrogenLabels.None}, but got {imported.ExplicitH}");
        }

        [Fact]
        public void MultiLevelMolecule_ExplicitH_NotSet()
        {
            // Arrange
            var model = ModelHelper.ConstructMultiLevelMolecule();
            var atom = model.GetAllAtoms().First(a => a.Id.Equals("a1"));

            // Act
            var effective = atom.InheritedHydrogenLabels;

            // Assert
            Assert.True(effective.Equals(HydrogenLabels.HeteroAndTerminal), $"Expected effective to be {HydrogenLabels.HeteroAndTerminal}, but got {effective}");
        }

        [Fact]
        public void MultiLevelMolecule_ExplicitH_SetAtAtom()
        {
            // Arrange
            var model = ModelHelper.ConstructMultiLevelMolecule();
            var atom = model.GetAllAtoms().First(a => a.Id.Equals("a1"));

            // Act
            atom.ExplicitH = HydrogenLabels.All;
            var effective = atom.InheritedHydrogenLabels;

            // Assert
            Assert.True(effective.Equals(HydrogenLabels.All), $"Expected effective to be {HydrogenLabels.All}, but got {effective}");
        }

        [Fact]
        public void MultiLevelMolecule_ExplicitH_SetAtMolecule()
        {
            // Arrange
            var model = ModelHelper.ConstructMultiLevelMolecule();
            var molecule = model.GetAllMolecules().First(a => a.Id.Equals("m1"));

            // Act
            molecule.ExplicitH = HydrogenLabels.Hetero;
            var atom = model.GetAllAtoms().First(a => a.Id.Equals("a1"));
            var effective = atom.InheritedHydrogenLabels;

            // Assert
            Assert.True(effective.Equals(HydrogenLabels.Hetero), $"Expected effective to be {HydrogenLabels.Hetero}, but got {effective}");
        }

        [Fact]
        public void MultiLevelMolecule_ExplicitH_SetAtModel()
        {
            // Arrange
            var model = ModelHelper.ConstructMultiLevelMolecule();

            // Act
            model.ExplicitH = HydrogenLabels.None;
            var atom = model.GetAllAtoms().First(a => a.Id.Equals("a1"));
            var effective = atom.InheritedHydrogenLabels;

            // Assert
            Assert.True(effective.Equals(HydrogenLabels.None), $"Expected effective to be {HydrogenLabels.None}, but got {effective}");
        }
    }
}
