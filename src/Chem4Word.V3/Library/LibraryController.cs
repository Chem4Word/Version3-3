// ---------------------------------------------------------------------------
//  Copyright (c) 2022, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.ACME.Models;
using Chem4Word.Core.UI.Forms;
using IChem4Word.Contracts;
using IChem4Word.Contracts.Dto;
using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Reflection;
using System.Text;
using System.Windows;

namespace Chem4Word.Library
{
    public class LibraryController : DependencyObject
    {
        private static string _product = Assembly.GetExecutingAssembly().FullName.Split(',')[0];
        private static string _class = MethodBase.GetCurrentMethod().DeclaringType?.Name;

        //used for XAML data binding
        public ObservableCollection<ChemistryObject> ChemistryItems { get; }

        private bool _initializing;
        private readonly IChem4WordTelemetry _telemetry;
        private IChem4WordDriver _driver;

        public LibraryController(IChem4WordTelemetry telemetry)
        {
            _telemetry = telemetry;

            var details = Globals.Chem4WordV3.GetDatabaseDetails();
            _driver = Globals.Chem4WordV3.GetDriverPlugIn(details.Driver);
            _driver.DatabaseDetails = details;

            ChemistryItems = new ObservableCollection<ChemistryObject>();
            ChemistryItems.CollectionChanged += ChemistryItems_CollectionChanged;

            LoadChemistryItems();
        }

        private void LoadChemistryItems()
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                _initializing = true;
                ChemistryItems.Clear();

                var dataObjects = _driver.GetAllChemistry();

                foreach (var dto in dataObjects)
                {
                    var obj = new ChemistryObject
                    {
                        Id = dto.Id,
                        Cml = Encoding.UTF8.GetString(dto.Chemistry),
                        Formula = dto.Formula,
                        Name = dto.Name,
                        MolecularWeight = dto.MolWeight,
                        Names = dto.Names
                    };

                    ChemistryItems.Add(obj);
                }

                _initializing = false;
            }
            catch (Exception ex)
            {
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
                        if (!_initializing)
                        {
                            AddNewChemistry(e.NewItems);
                        }
                        break;

                    case NotifyCollectionChangedAction.Remove:
                        if (!_initializing)
                        {
                            DeleteChemistry(e.OldItems);
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                using (var form = new ReportError(Globals.Chem4WordV3.Telemetry, Globals.Chem4WordV3.WordTopLeft, module, ex))
                {
                    form.ShowDialog();
                }
            }
        }

        private void DeleteChemistry(IList eOldItems)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                if (!_initializing)
                {
                    foreach (ChemistryObject chemistry in eOldItems)
                    {
                        _driver.DeleteChemistryById(chemistry.Id);
                        _telemetry.Write(module, "Information", $"Structure {chemistry.Id} deleted from {_driver.DatabaseDetails.DisplayName}");
                    }
                }
            }
            catch (Exception ex)
            {
                using (var form = new ReportError(Globals.Chem4WordV3.Telemetry, Globals.Chem4WordV3.WordTopLeft, module, ex))
                {
                    form.ShowDialog();
                }
            }
        }

        private void AddNewChemistry(IList eNewItems)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                if (!_initializing)
                {
                    foreach (ChemistryObject chemistry in eNewItems)
                    {
                        var dto = new ChemistryDataObject
                        {
                            Chemistry = Encoding.UTF8.GetBytes(chemistry.Cml),
                            DataType = "cml",
                            Name = chemistry.Name,
                            Formula = chemistry.Formula,
                            MolWeight = chemistry.MolecularWeight,
                            Names = chemistry.Names
                        };

                        // ToDo: [V3.3] Add Tags

                        chemistry.Id = _driver.AddChemistry(dto);
                        _telemetry.Write(module, "Information", $"Structure {chemistry.Id} added to {_driver.DatabaseDetails.DisplayName}");
                    }
                }
            }
            catch (Exception ex)
            {
                using (var form = new ReportError(Globals.Chem4WordV3.Telemetry, Globals.Chem4WordV3.WordTopLeft, module, ex))
                {
                    form.ShowDialog();
                }
            }
        }
    }
}