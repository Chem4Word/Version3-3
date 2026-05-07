// ---------------------------------------------------------------------------
//  Copyright (c) 2026, The .NET Foundation.
//  This software is released under the Apache Licence, Version 2.0.
//  The licence and further copyright text can be found in the file LICENCE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.ACME.Controls;
using Chem4Word.ACME.Enums;
using Chem4Word.ACME.Models;
using Chem4Word.Core.Enums;
using Chem4Word.Core.UI.Wpf;
using Chem4Word.Model2;
using Chem4Word.Model2.Enums;
using Chem4Word.Model2.Helpers;
using System;
using System.Linq;
using System.Windows;

namespace Wpf.UI.Sandbox.Forms
{
    /// <summary>
    /// Interaction logic for CompassTesting.xaml
    /// </summary>
    public partial class CompassTesting : Window
    {
        private Model Model1 { get; set; }
        private Atom Atom1 { get; set; }
        private Model Model2 { get; set; }
        private Atom Atom2 { get; set; }
        private Model Model3 { get; set; }
        private Atom Atom3 { get; set; }

        public CompassTesting()
        {
            InitializeComponent();

            Model1 = CreateModel("N");
            Atom1 = Model1.GetAllAtoms().First();

            Model2 = CreateModel("CH2OH");
            Atom2 = Model2.GetAllAtoms().First();

            Model3 = CreateModel("O");
            Atom3 = Model3.GetAllAtoms().First();

            AutomaticElectronsControlModel model = new AutomaticElectronsControlModel
            {
                ParentAtom = Atom3
            };

            Electron electron1 = ElectronHelper.MakeElectron(Atom3, 1, ElectronType.Radical);
            Atom3.AddElectron(electron1);

            Electron electron2 = ElectronHelper.MakeElectron(Atom3, 1, ElectronType.Radical, CompassPoints.East);
            Atom3.AddElectron(electron2);

            Atom3.UpdateElectronPlacements();

            ElectronsControl.Model = new ElectronsControlModel(Atom3, ElectronsControl);
        }

        private void OnCompassValueChanged_Compass(object sender, WpfEventArgs e)
        {
            if (sender is Compass compass)
            {
                switch (compass.CompassControlType)
                {
                    case CompassControlType.Hydrogens:
                        Atom1.ExplicitHPlacement = compass.SelectedCompassPoint;
                        Display.Chemistry = Model1.Copy();
                        break;

                    case CompassControlType.FunctionalGroups:
                        Atom2.ExplicitFunctionalGroupPlacement = compass.SelectedCompassPoint;
                        Display.Chemistry = Model2.Copy();
                        break;
                }
            }
        }

        private void OnValueChanged_Electrons(object sender, WpfEventArgs e)
        {
            Atom3 = ElectronsControl.Model.ParentAtom;
            Atom3.UpdateElectronPlacements();
            Display.Chemistry = Model3.Copy();
        }

        private Model CreateModel(string typeOfAtom)
        {
            Model model = new Model();

            Molecule molecule = new Molecule { Id = "m1" };
            model.AddMolecule(molecule);
            molecule.Parent = model;

            AtomHelpers.TryParse(typeOfAtom, true, out ElementBase element);

            Atom atom1 = AddAtomToMolecule(molecule, "a1");
            atom1.Element = element;
            atom1.Position = new Point(0, 0);
            atom1.Parent = molecule;

            Atom atom2 = AddAtomToMolecule(molecule, "a2");
            atom2.Element = ModelGlobals.PeriodicTable.C;
            atom2.Position = new Point(-20, 20);
            atom2.ExplicitH = HydrogenLabels.None;
            atom2.Parent = molecule;

            Bond bond = new Bond(atom1, atom2)
            {
                Order = "1"
            };
            molecule.AddBond(bond);
            bond.Parent = molecule;

            model.Relabel(true);

            return model;
        }

        private void CompassTesting_OnContentRendered(object sender, EventArgs e)
        {
            Display.Clear();
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
    }
}
