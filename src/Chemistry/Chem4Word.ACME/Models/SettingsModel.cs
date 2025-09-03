// ---------------------------------------------------------------------------
//  Copyright (c) 2025, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.Model2.Annotations;
using Chem4Word.Model2.Enums;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Chem4Word.ACME.Models
{
    public class SettingsModel : INotifyPropertyChanged
    {
        private HydrogenLabels _explicitH;

        public HydrogenLabels ExplicitH
        {
            get => _explicitH;
            set
            {
                _explicitH = value;
                OnPropertyChanged();
            }
        }

        private bool _showColouredAtoms;

        public bool ShowColouredAtoms
        {
            get => _showColouredAtoms;
            set
            {
                _showColouredAtoms = value;
                OnPropertyChanged();
            }
        }

        private bool _explicitC;

        public bool ExplicitC
        {
            get => _explicitC;
            set
            {
                _explicitC = value;
                OnPropertyChanged();
            }
        }

        private bool _showMoleculeGrouping;

        public bool ShowMoleculeGrouping
        {
            get => _showMoleculeGrouping;
            set
            {
                _showMoleculeGrouping = value;
                OnPropertyChanged();
            }
        }

        private bool _showMolecularWeight;

        public bool ShowMolecularWeight
        {
            get => _showMolecularWeight;
            set
            {
                _showMolecularWeight = value;
                OnPropertyChanged();
            }
        }

        private bool _showMoleculeCaptions;

        public bool ShowMoleculeCaptions
        {
            get => _showMoleculeCaptions;
            set
            {
                _showMoleculeCaptions = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}