// ---------------------------------------------------------------------------
//  Copyright (c) 2026, The .NET Foundation.
//  This software is released under the Apache Licence, Version 2.0.
//  The licence and further copyright text can be found in the file LICENCE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.ACME.Controls;
using Chem4Word.Core.Enums;
using Chem4Word.Model2;
using Chem4Word.Model2.Enums;
using Chem4Word.Model2.Helpers;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Chem4Word.ACME.Models
{
    public class ElectronsControlModel : INotifyPropertyChanged
    {
        private readonly AutoElectronsControl _autoControl;
        private readonly Compass _manualControl;

        public bool InAutomaticMode { get; set; }

        public Atom ParentAtom { get; set; }

        public ElectronsControlModel(Atom atom, ElectronsControl electronsControl)
        {
            ParentAtom = atom;

            _autoControl = electronsControl.AutomaticElectrons;
            _autoControl.ParentAtom = atom;
            _autoControl.Model = new AutomaticElectronsControlModel
            {
                ParentAtom = atom
            };

            _manualControl = electronsControl.ManualElectrons;
            _manualControl.Atom = atom;

            SetupChildModels();
        }

        private void SetupChildModels()
        {
            ClearElectrons();

            if (ParentAtom != null)
            {
                InAutomaticMode = true;

                List<Electron> electrons = ParentAtom.AllElectrons();
                if (electrons.Count > 0)
                {
                    // If there are a mixture of Manual placed electrons and Automatically placed electrons
                    //   they will all be converted to Manual
                    int manualPlacementsCount = electrons.Count(e => e.ExplicitPlacement != null);
                    if (manualPlacementsCount > 0)
                    {
                        CompassPoints compassPoint = CompassPoints.North;

                        foreach (Electron electron in electrons)
                        {
                            ElectronType typeOfElectron = electron.TypeOfElectron;

                            if (electron.ExplicitPlacement.HasValue)
                            {
                                // This is a Manual placement - leave alone
                                compassPoint = electron.ExplicitPlacement.Value;
                            }
                            else
                            {
                                // This is an automatic placement
                                //   find the next free compass point and convert to Manual
                                while (_manualControl.SelectedElectronDictionary.ContainsKey(compassPoint))
                                {
                                    compassPoint = Model2.Helpers.Utils.NextCompassPoint(compassPoint);
                                }
                            }

                            // Add to the Manual placements
                            _manualControl.SelectedElectronDictionary.Add(compassPoint, typeOfElectron);
                            _manualControl.SelectedElectrons.Add(electron);
                            _manualControl.Atom.AddElectron(electron);
                        }

                        InAutomaticMode = false;
                    }
                    else
                    {
                        // All electrons are Automatic
                        foreach (Electron electron in electrons)
                        {
                            AutomaticElectronItem item = MakeAutomaticElectronItem(ParentAtom, electron.Id, electron.TypeOfElectron);
                            _autoControl.Model.AutomaticElectronItems.Add(item);
                            _autoControl.Model.AutomaticElectronPlacements.Add(item.Id, item);
                        }

                        InAutomaticMode = true;
                    }
                }
            }
        }

        public void ConvertModelToAutomatic()
        {
            // Clear existing Automatic properties
            _autoControl.Model.AutomaticElectronItems = new ObservableCollection<AutomaticElectronItem>();
            _autoControl.Model.AutomaticElectronPlacements = new Dictionary<string, AutomaticElectronItem>();

            int index = 1;
            foreach (ElectronType electronType in _manualControl.SelectedElectronDictionary.Values)
            {
                AutomaticElectronItem item = MakeAutomaticElectronItem(ParentAtom, $"e{index++}", electronType);

                _autoControl.Model.AutomaticElectronItems.Add(item);
                _autoControl.Model.AutomaticElectronPlacements.Add(item.Id, item);
            }

            InAutomaticMode = true;
            UpdateParentAtom();

            // Finally clear existing Manual properties
            _manualControl.SelectedElectronDictionary = new Dictionary<CompassPoints, ElectronType>();
            _manualControl.SelectedElectrons = new List<Electron>();
        }

        public void ConvertModelToManual()
        {
            // Clear existing Manual Placements
            _manualControl.SelectedElectronDictionary = new Dictionary<CompassPoints, ElectronType>();
            _manualControl.SelectedElectrons = new List<Electron>();

            foreach (Electron item in ParentAtom.AllElectrons())
            {
                CompassPoints cp = item.Placement;

                while (_manualControl.SelectedElectronDictionary.ContainsKey(cp))
                {
                    cp = Model2.Helpers.Utils.NextCompassPoint(cp);
                }

                item.ExplicitPlacement = cp;
                _manualControl.SelectedElectronDictionary.Add(cp, item.TypeOfElectron);
                _manualControl.SelectedElectrons.Add(item);
            }

            InAutomaticMode = false;
            UpdateParentAtom();

            // Finally clear existing Automatic properties
            _autoControl.Model.AutomaticElectronItems = new ObservableCollection<AutomaticElectronItem>();
            _autoControl.Model.AutomaticElectronPlacements = new Dictionary<string, AutomaticElectronItem>();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void UpdateParentAtom()
        {
            List<Electron> electrons;

            if (InAutomaticMode)
            {
                electrons = new List<Electron>();
                int index = 1;
                foreach (AutomaticElectronItem placement in _autoControl.Model.AutomaticElectronItems)
                {
                    ElectronType electronType = placement.ElectronType;
                    Electron electron = ElectronHelper.MakeElectron(ParentAtom, index++, electronType);
                    electrons.Add(electron);
                }
            }
            else
            {
                electrons = _manualControl.SelectedElectrons;
            }

            ParentAtom.ClearElectrons();
            ParentAtom.AddRangeOfElectrons(electrons);
        }

        public void ClearElectrons()
        {
            // Clear existing properties
            _manualControl.SelectedElectronDictionary = new Dictionary<CompassPoints, ElectronType>();
            _manualControl.SelectedElectrons = new List<Electron>();

            _autoControl.Model.AutomaticElectronPlacements = new Dictionary<string, AutomaticElectronItem>();
            _autoControl.Model.AutomaticElectronItems = new ObservableCollection<AutomaticElectronItem>();
        }

        public static AutomaticElectronItem MakeAutomaticElectronItem(Atom parent, string id, ElectronType type)
        {
            AutomaticElectronItem item = new AutomaticElectronItem
            {
                ParentAtom = parent,
                Id = id,
                ElectronType = type
            };

            return item;
        }
    }
}
