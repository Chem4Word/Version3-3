// ---------------------------------------------------------------------------
//  Copyright (c) 2023, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.ACME;
using Chem4Word.Model2;
using Chem4Word.Model2.Converters.CML;
using Chem4Word.Model2.Helpers;
using System;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Chem4Word.Core.Helpers;

namespace Wpf.FunctionalGroupEditor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string _lastFunctionalGroup;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            Editor.EditorOptions = new AcmeOptions(null);

            Groups.Items.Clear();
            foreach (var functionalGroup in Globals.FunctionalGroupsList.Where(i => i.IsSuperAtom))
            {
                Groups.Items.Add(functionalGroup);
            }

            Groups.SelectedIndex = 0;
        }

        private void MainWindow_OnContentRendered(object sender, EventArgs e)
        {
            InvalidateArrange();
        }

        private void MainWindow_OnLocationChanged(object sender, EventArgs e)
        {
            Editor.TopLeft = new Point(Left, Top);
        }

        private void Groups_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (Groups.SelectedItem is FunctionalGroup fg)
            {
                var cmlConverter = new CMLConverter();

                if (!string.IsNullOrEmpty(_lastFunctionalGroup))
                {
                    if (Editor.IsDirty)
                    {
                        var fge = Globals.FunctionalGroupsList.FirstOrDefault(f => f.Name.Equals(_lastFunctionalGroup));
                        if (fge != null)
                        {
                            var temp = Editor.ActiveController.Model.Copy();
                            if (temp.TotalAtomsCount > 0)
                            {
                                temp.RescaleForCml();
                                temp.Relabel(true);
                                var cml = cmlConverter.Export(temp, compressed: true, format: CmlFormat.ChemDraw);
                                fge.Expansion = cml;
                            }
                            else
                            {
                                fge.Expansion = string.Empty;
                            }
                        }
                    }
                }

                _lastFunctionalGroup = fg.Name;

                if (string.IsNullOrEmpty(fg.Expansion))
                {
                    var model = new Model();
                    Editor.SetModel(model);
                }
                else
                {
                    var model = cmlConverter.Import(fg.Expansion);
                    Editor.SetModel(model);
                }
            }
        }

        private void MainWindow_OnClosing(object sender, CancelEventArgs e)
        {
            if (Editor.IsDirty)
            {
                var fg = Globals.FunctionalGroupsList.FirstOrDefault(f => f.Name.Equals(_lastFunctionalGroup));
                if (fg != null)
                {
                    var temp = Editor.ActiveController.Model.Copy();
                    if (temp.TotalAtomsCount > 0)
                    {
                        temp.RescaleForCml();
                        temp.Relabel(true);
                        var cmlConverter = new CMLConverter();
                        var cml = cmlConverter.Export(temp, compressed: true, format: CmlFormat.ChemDraw);
                        fg.Expansion = cml;
                    }
                    else
                    {
                        fg.Expansion = string.Empty;
                    }
                }
            }

            var xml = FunctionalGroups.ExportAsXml();
            Clipboard.SetText(XmlHelper.AddHeader(xml));
            MessageBox.Show($@"Please replace $\src\Chemistry\Chem4Word.Model2\Resources\FunctionalGroups.xml{Environment.NewLine}with the results on the clipboard!", "Data on Clipboard");
        }
    }
}