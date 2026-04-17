// ---------------------------------------------------------------------------
//  Copyright (c) 2026, The .NET Foundation.
//  This software is released under the Apache Licence, Version 2.0.
//  The licence and further copyright text can be found in the file LICENCE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.ACME.Annotations;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Chem4Word.ACME.Models
{
    public class AutomaticElectronsEditorModel : INotifyPropertyChanged
    {
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

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
