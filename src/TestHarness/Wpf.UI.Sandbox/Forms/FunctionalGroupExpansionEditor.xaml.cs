// ---------------------------------------------------------------------------
//  Copyright (c) 2026, The .NET Foundation.
//  This software is released under the Apache Licence, Version 2.0.
//  The licence and further copyright text can be found in the file LICENCE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.Core.Helpers;
using Chem4Word.Model2;
using Chem4Word.Model2.Converters.CML;
using Chem4Word.Model2.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Wpf.UI.Sandbox.Models;

namespace Wpf.UI.Sandbox.Forms
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

        private void OnLoaded_MainWindow(object sender, RoutedEventArgs e)
        {
            var model = new List<FgItem>();

            var groupsToShow = ModelGlobals.FunctionalGroupsList
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

        private void OnContentRendered_MainWindow(object sender, EventArgs e)
        {
            InvalidateArrange();
        }

        private void OnLocationChanged_MainWindow(object sender, EventArgs e)
        {
            Editor.TopLeft = new Point(Left, Top);
        }

        private void OnSelectionChanged_Groups(object sender, SelectionChangedEventArgs e)
        {
            if (Groups.SelectedItem is FgItem item)
            {
                var cmlConverter = new CMLConverter();

                if (!string.IsNullOrEmpty(_lastFunctionalGroup))
                {
                    if (Editor.IsDirty)
                    {
                        var fge = ModelGlobals.FunctionalGroupsList.FirstOrDefault(f => f.Name.Equals(_lastFunctionalGroup));
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

                var fg = ModelGlobals.FunctionalGroupsList.FirstOrDefault(f => f.Name.Equals(item.Name));

                if (fg == null || string.IsNullOrEmpty(fg.Expansion))
                {
                    var model = new Model();
                    Editor.SetProperties(new CMLConverter().Export(model), null, new RenderingOptions());
                }
                else
                {
                    var model = cmlConverter.Import(fg.Expansion);
                    Debug.WriteLine($"Formula for '{fg.Name}' is {model.ConciseFormula}");
                    Editor.SetProperties(new CMLConverter().Export(model), null, new RenderingOptions());
                }

                Title = $"Functional Group Expansion Editor - {item.Name}";

                Groups.Focus();
            }
        }

        private void OnClosing_MainWindow(object sender, CancelEventArgs e)
        {
            if (Editor.IsDirty)
            {
                var fg = ModelGlobals.FunctionalGroupsList.FirstOrDefault(f => f.Name.Equals(_lastFunctionalGroup));
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

            // Seems a bit of an overkill, but allows the exported list to be sorted by a) GroupType, b) AtomicWeight, c) Comment, d) Name

            ModelGlobals.FunctionalGroupsList.Sort(delegate (FunctionalGroup x, FunctionalGroup y)
                                              {
                                                  var xType = (int)x.GroupType;
                                                  var yType = (int)y.GroupType;

                                                  if (xType != yType)
                                                  {
                                                      return xType.CompareTo(yType);
                                                  }

                                                  var xWeight = x.AtomicWeight;
                                                  var yWeight = y.AtomicWeight;
                                                  if (xWeight != yWeight)
                                                  {
                                                      return xWeight.CompareTo(yWeight);
                                                  }

                                                  var xComment = $"{x.Comment}";
                                                  var yComment = $"{y.Comment}";
                                                  if (xComment != yComment)
                                                  {
                                                      return string.Compare(xComment, yComment, StringComparison.InvariantCultureIgnoreCase);
                                                  }

                                                  var xName = x.Name;
                                                  var yName = y.Name;

                                                  if (xName.StartsWith("R"))
                                                  {
                                                      xName = ConvertToJustNumber(xName);
                                                  }
                                                  if (yName.StartsWith("R"))
                                                  {
                                                      yName = ConvertToJustNumber(yName);
                                                  }

                                                  string ConvertToJustNumber(string name)
                                                  {
                                                      if (name.EndsWith("#"))
                                                      {
                                                          return "R000";
                                                      }
                                                      if (name.EndsWith("′"))
                                                      {
                                                          return "R001";
                                                      }
                                                      if (name.EndsWith("″"))
                                                      {
                                                          return "R002";
                                                      }

                                                      var number = int.Parse(name.Substring(1)) + 100;
                                                      return $"R{number:000}";
                                                  }

                                                  return string.Compare(xName, yName, StringComparison.InvariantCultureIgnoreCase);
                                              });

            var xml = FunctionalGroups.ExportAsXml();
            Clipboard.SetText(XmlHelper.AddHeader(xml));
            MessageBox.Show($@"Please replace $\src\Chemistry\Chem4Word.Model2\Resources\FunctionalGroups.xml{Environment.NewLine}with the results on the clipboard!", "Data on Clipboard");
        }
    }
}
