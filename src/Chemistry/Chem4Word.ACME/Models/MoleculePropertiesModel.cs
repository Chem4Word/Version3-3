// ---------------------------------------------------------------------------
//  Copyright (c) 2025, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.ACME.Entities;
using Chem4Word.Model2;
using Chem4Word.Model2.Enums;
using System.Collections.Generic;

namespace Chem4Word.ACME.Models
{
    public class MoleculePropertiesModel : BaseDialogModel
    {
        public Model Data { get; set; }

        public List<string> Used1DProperties { get; set; }

        private bool? _showMoleculeBrackets;

        public bool? ShowMoleculeBrackets
        {
            get => _showMoleculeBrackets;
            set
            {
                _showMoleculeBrackets = value;
                OnPropertyChanged();
            }
        }

        private bool? _explicitC;

        public bool? ExplicitC
        {
            get => _explicitC;
            set
            {
                _explicitC = value;
                OnPropertyChanged();
            }
        }

        private int? _spinMultiplicity;

        public int? SpinMultiplicity
        {
            get => _spinMultiplicity;
            set
            {
                _spinMultiplicity = value;
                OnPropertyChanged();
            }
        }

        private int? _count;

        public int? Count
        {
            get => _count;
            set
            {
                _count = value;
                OnPropertyChanged();
            }
        }

        private int? _charge;

        public int? Charge
        {
            get => _charge;
            set
            {
                _charge = value;
                OnPropertyChanged();
            }
        }

        public HydrogenLabels? ExplicitH { get; set; }

        public List<ChargeValue> MultiplicityValues
        {
            get
            {
                var values = new List<ChargeValue>
                             {
                                 new ChargeValue { Value = 0, Label = "(none)" },
                                 new ChargeValue { Value = 2, Label = "•" },
                                 new ChargeValue { Value = 3, Label = "• •" }
                             };

                return values;
            }
        }

        public List<ChargeValue> Charges
        {
            get
            {
                var charges = new List<ChargeValue>();
                for (var charge = ACMEGlobals.MinMoleculeCharge; charge <= ACMEGlobals.MaxMoleculeCharge; charge++)
                {
                    charges.Add(new ChargeValue { Value = charge, Label = charge.ToString() });
                }

                return charges;
            }
        }
    }
}
