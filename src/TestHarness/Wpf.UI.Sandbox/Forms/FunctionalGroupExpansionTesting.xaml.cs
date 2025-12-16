// ---------------------------------------------------------------------------
//  Copyright (c) 2026, The .NET Foundation.
//  This software is released under the Apache Licence, Version 2.0.
//  The licence and further copyright text can be found in the file LICENCE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.Core.Helpers;
using Chem4Word.Model2;
using Chem4Word.Model2.Converters.CML;
using Microsoft.Win32;
using System.Diagnostics;
using System.IO;
using System.Windows;

namespace Wpf.UI.Sandbox.Forms
{
    /// <summary>
    /// Interaction logic for FunctionalGroupExpansionTesting.xaml
    /// </summary>
    public partial class FunctionalGroupExpansionTesting : Window
    {
        public FunctionalGroupExpansionTesting()
        {
            InitializeComponent();
        }

        private void OnLoaded_FunctionalGroupExpansion(object sender, RoutedEventArgs e)
        {
            Model model = new Model();
            Editor.SetProperties(new CMLConverter().Export(model), null, new RenderingOptions());
        }

        private void OnClick_ExpandButton(object sender, RoutedEventArgs e)
        {
            Model model = Editor.EditedModel.Copy();

            int before = model.FunctionalGroupsCount;

            int expanded = model.ExpandAllFunctionalGroups();

            // Use .Copy() as display scales it's model to have bond length of 40, therefore corrupting it
            Display.Chemistry = model.Copy();

            Debug.WriteLine($"Expanded {expanded} Functional Groups from {before}; remaining {model.FunctionalGroupsCount}");

            CMLConverter converter = new CMLConverter();
            Clipboard.SetText(XmlHelper.AddHeader(converter.Export(model)));
            Debug.WriteLine("New CML copied to clipboard");
        }

        private void OnClick_LoadButton(object sender, RoutedEventArgs e)
        {
            CMLConverter converter = new CMLConverter();

            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "*.cml|*.cml"
            };
            if (openFileDialog.ShowDialog() == true)
            {
                Model model = converter.Import(File.ReadAllText(openFileDialog.FileName));
                if (model != null)
                {
                    Editor.SetProperties(new CMLConverter().Export(model), null, new RenderingOptions());
                }
            }
        }
    }
}
