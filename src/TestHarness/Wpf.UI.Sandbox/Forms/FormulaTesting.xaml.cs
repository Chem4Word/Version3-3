// ---------------------------------------------------------------------------
//  Copyright (c) 2025, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.Core.UI.Wpf;
using Chem4Word.Model2;
using Chem4Word.Model2.Converters.CML;
using Chem4Word.Model2.Formula;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;

namespace Wpf.UI.Sandbox.Forms
{
    /// <summary>
    /// Interaction logic for FormulaTesting.xaml
    /// </summary>
    public partial class FormulaTesting : Window
    {
        private ObservableCollection<FormulaPart> PartsCollection { get; set; }

        public FormulaTesting()
        {
            InitializeComponent();

            PartsCollection = new ObservableCollection<FormulaPart>();
            Concise.Text = "";
            Unicode.Text = "";
        }

        private void OnLoaded_FormulaTesting(object sender, RoutedEventArgs e)
        {
            Model model = new Model();
            Editor.SetProperties(new CMLConverter().Export(model), null, new RenderingOptions());

            Editor.ShowFeedback = true;
            Editor.OnFeedbackChange += OnFeedbackChange_Editor;
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

        private void OnFeedbackChange_Editor(object sender, WpfEventArgs e)
        {
            DisplayFormulaParts();
        }

        private void OnClick_Formula(object sender, RoutedEventArgs e)
        {
            DisplayFormulaParts();
        }

        private void DisplayFormulaParts()
        {
            Model model = Editor.EditedModel;
            FormulaHelper helper = new FormulaHelper(model);

            Concise.Text = model.ConciseFormula;
            ConciseCompact.Text = helper.Concise(compact: true);
            Unicode.Text = model.UnicodeFormula;
        }
    }
}
