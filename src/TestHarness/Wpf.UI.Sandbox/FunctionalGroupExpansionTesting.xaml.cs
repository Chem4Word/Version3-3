using Chem4Word.ACME;
using Chem4Word.Core.Helpers;
using Chem4Word.Model2;
using Chem4Word.Model2.Converters.CML;
using Microsoft.Win32;
using System.Diagnostics;
using System.IO;
using System.Windows;

namespace Wpf.UI.Sandbox
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

        private void FunctionalGroupExpansion_OnLoaded(object sender, RoutedEventArgs e)
        {
            Editor.EditorOptions = new AcmeOptions(null);

            var model = new Model();
            Editor.SetModel(model);
        }

        private void OnClick_ExpandButton(object sender, RoutedEventArgs e)
        {
            var model = Editor.EditedModel.Copy();

            var before = model.FunctionalGroupsCount;

            var expanded = model.ExpandAllFunctionalGroups();

            // Use .Copy() as display scales it's model to have bond length of 40, therefore corrupting it
            Display.Chemistry = model.Copy();

            Debug.WriteLine($"Expanded {expanded} Functional Groups from {before}; remaining {model.FunctionalGroupsCount}");

            var converter = new CMLConverter();
            Clipboard.SetText(XmlHelper.AddHeader(converter.Export(model)));
            Debug.WriteLine("New CML copied to clipboard");
        }

        private void OnClick_LoadButton(object sender, RoutedEventArgs e)
        {
            var converter = new CMLConverter();

            var openFileDialog = new OpenFileDialog
            {
                Filter = "*.cml|*.cml"
            };
            if (openFileDialog.ShowDialog() == true)
            {
                var model = converter.Import(File.ReadAllText(openFileDialog.FileName));
                if (model != null)
                {
                    Editor.SetModel(model);
                }
            }
        }
    }
}