// ---------------------------------------------------------------------------
//  Copyright (c) 2026, The .NET Foundation.
//  This software is released under the Apache Licence, Version 2.0.
//  The licence and further copyright text can be found in the file LICENCE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.ACME.Models;
using Chem4Word.Core.UI.Forms;
using Chem4Word.Helpers;
using Chem4Word.Model2;
using Chem4Word.Model2.Converters.CML;
using IChem4Word.Contracts;
using IChem4Word.Contracts.Dto;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Windows;

namespace Chem4Word.Library
{
    public class LibraryController : DependencyObject
    {
        private static string _product = Assembly.GetExecutingAssembly().FullName.Split(',')[0];
        private static string _class = MethodBase.GetCurrentMethod().DeclaringType?.Name;

        //used for XAML data binding
        public ObservableCollection<ChemistryObject> ChemistryItems { get; }

        private readonly IChem4WordTelemetry _telemetry;
        private IChem4WordLibraryReader _driver;

        public LibraryController(IChem4WordTelemetry telemetry)
        {
            _telemetry = telemetry;

            var details = Globals.Chem4WordV3.GetSelectedDatabaseDetails();
            if (details != null)
            {
                _driver = (IChem4WordLibraryReader)Globals.Chem4WordV3.GetDriverPlugIn(details.Driver);
                if (_driver != null)
                {
                    _driver.FileName = details.Connection;

                    ChemistryItems = new ObservableCollection<ChemistryObject>();

                    LoadChemistryItems();
                }
            }
        }

        private void LoadChemistryItems()
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                ChemistryItems.Clear();

                List<long> idsToUpdate = new List<long>();
                List<ChemistryDataTransferObject> dataObjects = _driver.GetAllChemistry();

                // Search for structures with missing MolWeights or Unicode Formula
                foreach (ChemistryDataTransferObject chemistryDto in dataObjects)
                {
                    if (chemistryDto.MolWeight == 0.0
                        || string.IsNullOrEmpty(chemistryDto.UnicodeFormula))
                    {
                        idsToUpdate.Add(chemistryDto.Id);
                    }
                }

                // Did we find any?
                if (idsToUpdate.Any())
                {
                    IChem4WordLibraryWriter fixer = (IChem4WordLibraryWriter)Globals.Chem4WordV3.GetDriverPlugIn("SQLite Standard");
                    fixer.FileName = _driver.FileName;
                    fixer.StartTransaction();

                    // If so update the MolWeight of them
                    CMLConverter cmlConverter = new CMLConverter();
                    foreach (long id in idsToUpdate)
                    {
                        ChemistryDataTransferObject dto = dataObjects.FirstOrDefault(d => d.Id == id);
                        if (dto != null)
                        {
                            ChemistryObject obj = DtoHelper.CreateFromDto(dto);
                            string cml = obj.CmlFromChemistry();
                            Model model = cmlConverter.Import(cml);
                            dto.MolWeight = model.MolecularWeight;
                            dto.UnicodeFormula = model.UnicodeFormula;
                            fixer.UpdateMetaData(dto);
                        }
                    }

                    fixer.CommitTransaction();

                    dataObjects = _driver.GetAllChemistry();
                }

                foreach (ChemistryDataTransferObject dto in dataObjects)
                {
                    ChemistryObject obj = DtoHelper.CreateFromDto(dto);
                    ChemistryItems.Add(obj);
                }
            }
            catch (Exception ex)
            {
                using (ReportError form = new ReportError(_telemetry, Globals.Chem4WordV3.WordTopLeft, module, ex))
                {
                    form.ShowDialog();
                }
            }
        }
    }
}
