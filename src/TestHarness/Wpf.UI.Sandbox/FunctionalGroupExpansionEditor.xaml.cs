// ---------------------------------------------------------------------------
//  Copyright (c) 2023, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.ACME;
using Chem4Word.Core.Helpers;
using Chem4Word.Model2;
using Chem4Word.Model2.Converters.CML;
using Chem4Word.Model2.Enums;
using Chem4Word.Model2.Helpers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Wpf.UI.Sandbox.Models;

namespace Wpf.UI.Sandbox
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class FunctionalGroupExpansionEditor : Window
    {
        private string _lastFunctionalGroup;

        public FunctionalGroupExpansionEditor()
        {
            InitializeComponent();
        }

        private void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            Editor.EditorOptions = new AcmeOptions(null);

            var model = new List<FgItem>();

            var groupsToShow = Globals.FunctionalGroupsList
                                      .Where(g => g.GroupType == GroupType.SuperAtom)
                                      .OrderBy(g => g.AtomicWeight)
                                      .ThenBy(g => g.Name);

            foreach (var functionalGroup in groupsToShow)
            {
                var item = new FgItem
                {
                    Name = functionalGroup.Name,
                    Group = functionalGroup
                };
                model.Add(item);
            }

            Groups.ItemsSource = model;

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
            if (Groups.SelectedItem is FgItem item)
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

                _lastFunctionalGroup = item.Name;

                var fg = Globals.FunctionalGroupsList.FirstOrDefault(f => f.Name.Equals(item.Name));

                if (fg == null || string.IsNullOrEmpty(fg.Expansion))
                {
                    var model = new Model();
                    Editor.SetModel(model);
                }
                else
                {
                    var model = cmlConverter.Import(fg.Expansion);
                    Debug.WriteLine($"Formula for '{fg.Name}' is {model.ConciseFormula}");
                    Editor.SetModel(model);
                }

                Title = $"Functional Group Expansion Editor - {item.Name}";

                Groups.Focus();
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