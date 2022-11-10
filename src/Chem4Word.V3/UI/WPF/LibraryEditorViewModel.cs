// ---------------------------------------------------------------------------
//  Copyright (c) 2022, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.ACME.Models;
using Chem4Word.Core.UI.Forms;
using Chem4Word.Helpers;
using Chem4Word.Model2.Annotations;
using IChem4Word.Contracts;
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows;

namespace Chem4Word.UI.WPF
{
    public class LibraryEditorViewModel : DependencyObject, INotifyPropertyChanged
    {
        private static string _product = Assembly.GetExecutingAssembly().FullName.Split(',')[0];
        private static string _class = MethodBase.GetCurrentMethod().DeclaringType?.Name;

        private readonly IChem4WordTelemetry _telemetry;
        private readonly IChem4WordDriver _driver;

        public LibraryEditorViewModel(IChem4WordTelemetry telemetry, IChem4WordDriver driver)
        {
            _telemetry = telemetry;
            _driver = driver;

            ChemistryItems = new ObservableCollection<ChemistryObject>();
            LoadChemistryItems();
            ChemistryItems.CollectionChanged += ChemistryItems_CollectionChanged;
        }

        //used for XAML data binding
        public ObservableCollection<ChemistryObject> ChemistryItems { get; }

        private ChemistryObject _selectedChemistryObject;

        public ChemistryObject SelectedChemistryObject
        {
            get
            {
                return _selectedChemistryObject;
            }
            set
            {
                _selectedChemistryObject = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void LoadChemistryItems()
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                ChemistryItems.Clear();

                var dataObjects = _driver.GetAllChemistry();
                foreach (var chemistryDto in dataObjects)
                {
                    var obj = DtoHelper.CreateFromDto(chemistryDto);
                    obj.SetDriver(_driver);
                    ChemistryItems.Add(obj);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"{module} {ex.Message}");
                using (var form = new ReportError(Globals.Chem4WordV3.Telemetry, Globals.Chem4WordV3.WordTopLeft, module, ex))
                {
                    form.ShowDialog();
                }
            }
        }

        private void ChemistryItems_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                switch (e.Action)
                {
                    case NotifyCollectionChangedAction.Add:
                        break;

                    case NotifyCollectionChangedAction.Remove:
                        break;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"{module} {ex.Message}");
                using (var form = new ReportError(Globals.Chem4WordV3.Telemetry, Globals.Chem4WordV3.WordTopLeft, module, ex))
                {
                    form.ShowDialog();
                }
            }
        }
    }
}