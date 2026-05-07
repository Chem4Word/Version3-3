// ---------------------------------------------------------------------------
//  Copyright (c) 2026, The .NET Foundation.
//  This software is released under the Apache Licence, Version 2.0.
//  The licence and further copyright text can be found in the file LICENCE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.Model2;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Chem4Word.ACME.Models
{
    public class AutomaticElectronsControlModel : INotifyPropertyChanged
    {
        public Atom ParentAtom { get; set; }

        private ObservableCollection<AutomaticElectronItem> _automaticElectronItems = new ObservableCollection<AutomaticElectronItem>();

        public ObservableCollection<AutomaticElectronItem> AutomaticElectronItems
        {
            get { return _automaticElectronItems; }
            set
            {
                _automaticElectronItems = value;
                OnPropertyChanged();
            }
        }

        private Dictionary<string, AutomaticElectronItem> _automaticElectronPlacements =
            new Dictionary<string, AutomaticElectronItem>();

        public Dictionary<string, AutomaticElectronItem> AutomaticElectronPlacements
        {
            get => _automaticElectronPlacements;
            set
            {
                _automaticElectronPlacements = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
