// ---------------------------------------------------------------------------
//  Copyright (c) 2026, The .NET Foundation.
//  This software is released under the Apache Licence, Version 2.0.
//  The licence and further copyright text can be found in the file LICENCE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.Core.Enums;
using Chem4Word.Model2;
using Chem4Word.Model2.Converters.CML;
using Chem4Word.Model2.Enums;
using System.Collections.Specialized;
using Xunit;

namespace Chem4WordTests
{
    public class ElectronTests
    {
        [Fact]
        public void ImportElectrons()
        {
            // Arrange
            var cmlConverter = new CMLConverter();
            var cml = ResourceHelper.GetStringResource("electron placement.xml");

            // Act
            var modelFromCml = cmlConverter.Import(cml);
            var electron = modelFromCml.GetByPath("/m1/a3/e1") as Electron;
            var electron2 = modelFromCml.GetByPath("/m1/a9/e1") as Electron;

            // Assert
            Assert.NotNull(electron);
            Assert.Equal(2, electron.Count);
            Assert.Equal(ElectronType.LonePair, electron.TypeOfElectron);

            Assert.NotNull(electron2);
            Assert.Equal(1, electron2.Count);
            Assert.Equal(ElectronType.Radical, electron2.TypeOfElectron);
            Assert.Equal(CompassPoints.North, electron2.Placement);
        }

        [Fact]
        public void DeleteElectrons()
        {
            // Arrange
            var cmlConverter = new CMLConverter();
            var cml = ResourceHelper.GetStringResource("electron placement.xml");

            // Act
            var modelFromCml = cmlConverter.Import(cml);
            var electron = modelFromCml.GetByPath("/m1/a3/e1") as Electron;
            var electron2 = modelFromCml.GetByPath("/m1/a9/e1") as Electron;

            Atom atom = modelFromCml.GetByPath("/m1/a3") as Atom;
            atom.RemoveElectron(electron);
            electron.Parent = null;

            // Assert
            Assert.NotNull(electron);
            Assert.NotNull(atom);
            Assert.Null(atom.GetByPath("e1"));
        }

        [Fact]
        public void RoundTripElectrons()
        {
            // Arrange
            var cmlConverter = new CMLConverter();
            var cml = ResourceHelper.GetStringResource("electron placement empty.xml");
            var modelFromCml = cmlConverter.Import(cml);

            // Act
            var atom = modelFromCml.GetByPath("/m1/a3") as Atom;
            var electron = new Electron
            {
                Count = 2,
                TypeOfElectron = ElectronType.LonePair,
                ExplicitPlacement = CompassPoints.NorthEast
            };
            atom.AddElectron(electron);
            electron.Parent = atom;
            var xml = cmlConverter.Export(modelFromCml);
            var modelFromCml2 = cmlConverter.Import(xml);
            var importedElectron = modelFromCml2.GetByPath("/m1/a3/e1") as Electron;

            // Assert
            Assert.NotNull(importedElectron);
            Assert.Equal(2, importedElectron.Count);
            Assert.Equal(ElectronType.LonePair, importedElectron.TypeOfElectron);
            Assert.Equal(CompassPoints.NorthEast, electron.Placement);
        }

        [Fact]
        public void BubbleElectronEventToModel()
        {
            // Arrange
            var cmlConverter = new CMLConverter();
            var cml = ResourceHelper.GetStringResource("electron placement empty.xml");
            var modelFromCml = cmlConverter.Import(cml);

            // Act
            var atom = modelFromCml.GetByPath("/m1/a3") as Atom;
            var electron = new Electron
            {
                Count = 2,
                TypeOfElectron = ElectronType.LonePair,
                ExplicitPlacement = CompassPoints.NorthEast
            };
            bool addedOK = false;
            modelFromCml.ElectronsChanged += (s, e) =>
            {
                addedOK = e.Action == NotifyCollectionChangedAction.Add && e.NewItems.Contains(electron);
            };
            bool propOK = false;
            modelFromCml.PropertyChanged += (s, e) =>
            {
                propOK = s is Electron && e.PropertyName == "Placement" && ((Electron)s).Placement == CompassPoints.SouthWest;
            };

            atom.AddElectron(electron);
            electron.Parent = atom;

            // Assert
            Assert.True(addedOK, "Electron addition event did not bubble to Model level");
            Assert.Equal(CompassPoints.NorthEast, electron.Placement);
            electron.ExplicitPlacement = CompassPoints.SouthWest;
            Assert.True(propOK, "Electron property change event did not bubble to Model level");
        }
    }
}
