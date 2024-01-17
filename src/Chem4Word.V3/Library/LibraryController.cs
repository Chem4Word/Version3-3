// ---------------------------------------------------------------------------
//  Copyright (c) 2024, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.ACME.Models;
using Chem4Word.Core.UI.Forms;
using Chem4Word.Helpers;
using IChem4Word.Contracts;
using System;
using System.Collections.ObjectModel;
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

                var dataObjects = _driver.GetAllChemistry();

                foreach (var dto in dataObjects)
                {
                    var obj = DtoHelper.CreateFromDto(dto);
                    ChemistryItems.Add(obj);
                }
            }
            catch (Exception ex)
            {
                using (var form = new ReportError(_telemetry, Globals.Chem4WordV3.WordTopLeft, module, ex))
                {
                    form.ShowDialog();
                }
            }
        }
    }
}