// ---------------------------------------------------------------------------
//  Copyright (c) 2025, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.Model2.Annotations;
using Chem4Word.Model2.Converters.CML;
using Chem4Word.Model2.Converters.ProtocolBuffers;
using IChem4Word.Contracts;
using IChem4Word.Contracts.Dto;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace Chem4Word.ACME.Models
{
    public class ChemistryObject : INotifyPropertyChanged
    {
        private static string _product = Assembly.GetExecutingAssembly().FullName.Split(',')[0];
        private static string _class = MethodBase.GetCurrentMethod().DeclaringType?.Name;

        private IChem4WordLibraryWriter _driver;

        public bool Initializing { get; set; }

        public ChemistryObject()
        {
            // Required for WPF XAML Designer
            Initializing = true;
        }

        public void SetDriver(IChem4WordLibraryWriter driver)
        {
            _driver = driver;
        }

        /// <summary>
        /// List of Chemical Names for the structure (Library mode)
        /// </summary>
        public List<string> ChemicalNames { get; set; } = new List<string>();

        public List<ChemistryNameDataObject> Names { get; set; }
        public List<ChemistryNameDataObject> Formulae { get; set; }
        public List<ChemistryNameDataObject> Captions { get; set; }

        public string CmlFromChemistry()
        {
            if (_chemistry is string cml)
            {
                return cml;
            }

            if (_chemistry is byte[] bytes)
            {
                var pbc = new ProtocolBufferConverter();
                var cc = new CMLConverter();
                return cc.Export(pbc.Import(bytes));
            }

            return null;
        }

        private object _chemistry;

        public object Chemistry
        {
            get => _chemistry;
            set
            {
                _chemistry = value;
                OnPropertyChanged();
            }
        }

        public ChemistryDataObject ConvertToDto()
        {
            var isByte = _chemistry is byte[];

            var chem = new ChemistryDataObject
            {
                Id = Id,
                DataType = isByte ? "pbuff" : "cml",
                Chemistry = isByte ? (byte[])_chemistry : Encoding.UTF8.GetBytes((string)_chemistry),
                Name = Name,
                Formula = Formula,
                MolWeight = MolecularWeight,
                Names = Names,
                Formulae = Formulae,
                Captions = Captions
            };

            return chem;
        }

        private void Save()
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                if (_driver != null)
                {
                    _driver.UpdateChemistry(ConvertToDto());
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                Debugger.Break();
            }
        }

        /// <summary>
        /// Formula of the structure
        /// </summary>
        public string Formula { get; set; }

        private string _name;

        /// <summary>
        /// Name of the structure
        /// </summary>
        public string Name
        {
            get => _name;
            set
            {
                // Ensure that only strings are accepted here
                var type = value.GetType().ToString();
                if (type.Equals("System.String"))
                {
                    _name = value;
                    if (!Initializing)
                    {
                        Save();
                    }
                    OnPropertyChanged();
                }
            }
        }

        private bool _isChecked;

        /// <summary>
        /// True if selected (Catalogue mode)
        /// </summary>
        public bool IsChecked
        {
            get => _isChecked;
            set
            {
                _isChecked = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// The CustomControl Tag (Navigator mode)
        /// </summary>
        public string CustomControlTag { get; set; }

        /// <summary>
        /// The Library Database Id of the structure (Library and Catalogue mode)
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// The calculated Molecular Weight
        /// </summary>
        public double MolecularWeight { get; set; }

        /// <summary>
        /// Used in LibraryEditorControl.xaml
        /// </summary>
        public string MolecularWeightAsString => $"{MolecularWeight:N3}";

        private List<string> _tags = new List<string>();

        /// <summary>
        /// List of Tags
        /// </summary>
        public List<string> Tags
        {
            get
            {
                return _tags;
            }
            set
            {
                _tags = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            if (PropertyChanged != null)
            {
                Debug.WriteLine($"OnPropertyChanged invoked for {propertyName} from {this}");
            }
        }
    }
}