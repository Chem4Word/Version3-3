// ---------------------------------------------------------------------------
//  Copyright (c) 2026, The .NET Foundation.
//  This software is released under the Apache Licence, Version 2.0.
//  The licence and further copyright text can be found in the file LICENCE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.ACME.Entities;
using Chem4Word.Core.Enums;
using Chem4Word.Model2;
using Chem4Word.Model2.Enums;
using System.Collections.Generic;

namespace Chem4Word.ACME.Models
{
    public class AtomPropertiesModel : BaseDialogModel
    {
        private ElementBase _element;
        private CompassPoints? _explicitHydrogenPlacement;
        private CompassPoints? _explicitFunctionalGroupPlacement;

        private Dictionary<CompassPoints, ElectronType> _explicitElectronPlacements =
            new Dictionary<CompassPoints, ElectronType>();

        private int? _charge;
        private string _isotope;
        private bool? _explicitC;
        private HydrogenLabels? _explicitH;

        private bool _isFunctionalGroup;
        private bool _isElement;
        private bool _showCompass;
        private bool _showHydrogenLabels;

        public ElementBase AddedElement { get; set; }

        public bool IsFunctionalGroup
        {
            get => _isFunctionalGroup;
            set
            {
                _isFunctionalGroup = value;
                OnPropertyChanged();
            }
        }

        public bool IsElement
        {
            get => _isElement;
            set
            {
                _isElement = value;
                OnPropertyChanged();
            }
        }

        public Model MicroModel { get; set; }

        public bool HasIsotopes
        {
            get { return IsotopeMasses.Count > 1; }
        }

        public Element SelectedElement { get; private set; }

        public ElementBase Element
        {
            get => _element;
            set
            {
                if (value is Element element)
                {
                    SelectedElement = element;
                }
                else
                {
                    SelectedElement = null;
                }
                _element = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsotopeMasses));
                OnPropertyChanged(nameof(HasIsotopes));
            }
        }

        public int? Charge
        {
            get => _charge;
            set
            {
                _charge = value;
                OnPropertyChanged();
            }
        }

        public CompassPoints? ExplicitHydrogenPlacement
        {
            get => _explicitHydrogenPlacement;
            set
            {
                _explicitHydrogenPlacement = value;
                OnPropertyChanged();
            }
        }

        public CompassPoints? ExplicitFunctionalGroupPlacement
        {
            get => _explicitFunctionalGroupPlacement;
            set
            {
                _explicitFunctionalGroupPlacement = value;
                OnPropertyChanged();
            }
        }

        public Dictionary<CompassPoints, ElectronType> ExplicitElectronPlacements
        {
            get => _explicitElectronPlacements;
            set
            {
                _explicitElectronPlacements = value;
                OnPropertyChanged();
            }
        }

        public string Isotope
        {
            get => _isotope;
            set
            {
                _isotope = value;
                OnPropertyChanged();
            }
        }

        public bool? ExplicitC
        {
            get => _explicitC;
            set
            {
                _explicitC = value;
                OnPropertyChanged();
            }
        }

        public HydrogenLabels? ExplicitH
        {
            get => _explicitH;
            set
            {
                _explicitH = value;
                OnPropertyChanged();
            }
        }

        public List<ChargeValue> Charges
        {
            get
            {
                List<ChargeValue> charges = new List<ChargeValue>();
                for (int charge = ACMEGlobals.MinAtomCharge; charge <= ACMEGlobals.MaxAtomCharge; charge++)
                {
                    charges.Add(new ChargeValue { Value = charge, Label = charge.ToString() });
                }

                return charges;
            }
        }

        public List<IsotopeValue> IsotopeMasses
        {
            get
            {
                List<IsotopeValue> temp = new List<IsotopeValue>();

                Element element = Element as Element;
                temp.Add(new IsotopeValue { Label = "", Mass = null });
                if (element != null && element.IsotopeMasses != null)
                {
                    foreach (int mass in element.IsotopeMasses)
                    {
                        temp.Add(new IsotopeValue { Label = mass.ToString(), Mass = mass });
                    }
                }

                return temp;
            }
        }

        public object IsNotSingleton { get; set; }

        public bool ShowCompass
        {
            get
            {
                return _showCompass;
            }
            set
            {
                _showCompass = value;
                OnPropertyChanged();
            }
        }

        public bool ShowHydrogenLabels
        {
            get
            {
                return _showHydrogenLabels;
            }
            set
            {
                _showHydrogenLabels = value;
                OnPropertyChanged();
            }
        }
    }
}
