// ---------------------------------------------------------------------------
//  Copyright (c) 2026, The .NET Foundation.
//  This software is released under the Apache Licence, Version 2.0.
//  The licence and further copyright text can be found in the file LICENCE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.ACME.Controls;
using Chem4Word.ACME.Enums;
using Chem4Word.Core.Enums;
using Chem4Word.Core.UI.Wpf;
using Chem4Word.Model2;
using Chem4Word.Model2.Enums;
using System;
using System.Collections.Generic;
using System.Windows;

namespace Wpf.UI.Sandbox.Forms
{
    /// <summary>
    /// Interaction logic for CompassTesting.xaml
    /// </summary>
    public partial class CompassTesting : Window
    {
        public Atom Atom { get; set; }

        public bool IsDirty { get; set; }

        public CompassTesting()
        {
            InitializeComponent();

            // Set some initial values for H and FG compasses
            Compass1.SelectedCompassPoint = CompassPoints.East;
            Compass2.SelectedCompassPoint = CompassPoints.West;

            // Set some values for Electron Compass (Manual Placement) and ListView (Auto Placement)
            Model model = new Model();

            Molecule molecule1 = new Molecule
            {
                Id = "m1"
            };
            model.AddMolecule(molecule1);
            molecule1.Parent = model;

            Atom atom = AddAtomToMolecule(molecule1, "a1");

            Compass3.SelectedElectronDictionary = new Dictionary<CompassPoints, ElectronType>();
            Compass3.SelectedElectrons = new List<Electron>();

            Electron electron = new Electron { Parent = atom, TypeOfElectron = ElectronType.Radical, Count = 1 };
            atom.AddElectron(electron);
            Compass3.SelectedElectronDictionary.Add(CompassPoints.North, electron.TypeOfElectron);
            electron.ExplicitPlacement = CompassPoints.North;
            Compass3.SelectedElectrons.Add(electron);

            electron = new Electron { Parent = atom, TypeOfElectron = ElectronType.LonePair, Count = 2 };
            atom.AddElectron(electron);
            Compass3.SelectedElectronDictionary.Add(CompassPoints.NorthEast, electron.TypeOfElectron);
            electron.ExplicitPlacement = CompassPoints.NorthEast;
            Compass3.SelectedElectrons.Add(electron);

            electron = new Electron { Parent = atom, TypeOfElectron = ElectronType.Carbenoid, Count = 2 };
            atom.AddElectron(electron);
            Compass3.SelectedElectronDictionary.Add(CompassPoints.East, electron.TypeOfElectron);
            electron.ExplicitPlacement = CompassPoints.East;
            Compass3.SelectedElectrons.Add(electron);

            model.Relabel(true);

            Atom = atom;
            ElectronsView.Atom = atom;
        }

        private void CompassTesting_OnContentRendered(object sender, EventArgs e)
        {
            IsDirty = false;

            Status3.Text = Compass3.ListCompassElectrons();
            Status4.Text = ElectronsView.ListElectrons();
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

        private void OnCompassValueChanged_Compass(object sender, WpfEventArgs e)
        {
            if (sender is Compass compass)
            {
                switch (compass.CompassControlType)
                {
                    case CompassControlType.Hydrogens:
                        Status1.Text = $"Selected direction is {compass.SelectedCompassPoint}";
                        break;

                    case CompassControlType.FunctionalGroups:
                        Status2.Text = $"Selected direction is {compass.SelectedCompassPoint}";
                        break;

                    case CompassControlType.Electrons:
                        Status3.Text = Compass3.ListCompassElectrons();
                        break;
                }
            }
        }

        private void ElectronsView_OnElectronsValueChanged(object sender, WpfEventArgs e)
        {
            Status4.Text = ElectronsView.ListElectrons();
        }
    }
}
