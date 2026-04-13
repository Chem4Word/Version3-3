// ---------------------------------------------------------------------------
//  Copyright (c) 2026, The .NET Foundation.
//  This software is released under the Apache Licence, Version 2.0.
//  The licence and further copyright text can be found in the file LICENCE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.ACME.Annotations;
using Chem4Word.ACME.Models;
using Chem4Word.Core.Helpers;
using Chem4Word.Model2;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows;

namespace WinForms.TestHarness
{
    public class StackViewModel : DependencyObject, INotifyPropertyChanged
    {
        private static string _product = Assembly.GetExecutingAssembly().FullName.Split(',')[0];
        private static string _class = MethodBase.GetCurrentMethod()?.DeclaringType?.Name;

        public ObservableCollection<ChemistryObject> ChemistryItems { get; set; } =
            new ObservableCollection<ChemistryObject>();

        public event PropertyChangedEventHandler PropertyChanged;

        public void LoadChemistryItems(Stack<Model> stack)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod()?.Name}()";

            try
            {
                ChemistryItems.Clear();

                foreach (Model model in stack)
                {
                    ChemistryObject chemistry = new ChemistryObject();
                    Model clone = model.Copy();

                    chemistry.Formula = clone.UnicodeFormula;
                    chemistry.Name = $"Mean Bond Length: {SafeDouble.AsString(clone.MeanBondLength)}";

                    chemistry.ChemicalNames.Add($"Molecular Weight: {SafeDouble.AsString4(clone.MolecularWeight)}");
                    //chemistry.ChemicalNames.Add($"CustomXml Part Guid: {clone.CustomXmlPartGuid}");
                    chemistry.Chemistry = clone;

                    ChemistryItems.Add(chemistry);
                }

            }
            catch (Exception exception)
            {
                Debug.WriteLine(exception);
                Debugger.Break();
            }
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
