// ---------------------------------------------------------------------------
//  Copyright (c) 2023, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.Model2.Enums;
using Chem4Word.Model2.Helpers;
using IChem4Word.Contracts.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace Chem4Word.UI.WPF
{
    /// <summary>
    /// Interaction logic for LibraryNamesPanel.xaml
    /// </summary>
    public partial class LibraryNamesPanel : UserControl
    {
        public LibraryNamesPanel()
        {
            InitializeComponent();
        }

        #region Properties

        public List<ChemistryNameDataObject> NamesList
        {
            get { return (List<ChemistryNameDataObject>)GetValue(NamesListProperty); }
            set { SetValue(NamesListProperty, value); }
        }

        // Using a DependencyProperty as the backing store for NamesList.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty NamesListProperty =
            DependencyProperty.Register("NamesList", typeof(List<ChemistryNameDataObject>),
                                        typeof(LibraryNamesPanel),
                                        new PropertyMetadata(null, new PropertyChangedCallback(NamesListChanged)));

        public List<ChemistryNameDataObject> FormulaeList
        {
            get { return (List<ChemistryNameDataObject>)GetValue(FormulaeListProperty); }
            set { SetValue(FormulaeListProperty, value); }
        }

        // Using a DependencyProperty as the backing store for FormulaeList.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty FormulaeListProperty =
            DependencyProperty.Register("FormulaeList", typeof(List<ChemistryNameDataObject>), typeof(LibraryNamesPanel),
                                        new PropertyMetadata(null, new PropertyChangedCallback(FormulaeListChanged)));

        public List<ChemistryNameDataObject> CaptionsList
        {
            get { return (List<ChemistryNameDataObject>)GetValue(CaptionsListProperty); }
            set { SetValue(CaptionsListProperty, value); }
        }

        // Using a DependencyProperty as the backing store for CaptionsList.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty CaptionsListProperty =
            DependencyProperty.Register("CaptionsList", typeof(List<ChemistryNameDataObject>), typeof(LibraryNamesPanel), new PropertyMetadata(null, new PropertyChangedCallback(CaptionsListChanged)));

        #endregion Properties

        private static void CaptionsListChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            List<ChemistryNameDataObject> dataObjects = (List<ChemistryNameDataObject>)e.NewValue;
            ReloadCaptionsList(dataObjects, d);
        }

        private static void ReloadCaptionsList(List<ChemistryNameDataObject> listParam, DependencyObject d)
        {
            TreeView namesTree = ((LibraryNamesPanel)d).NamesTreeView;

            var captionNode = namesTree.Items[2] as TreeViewItem;

            LoadNames(captionNode, listParam);
        }

        private static void FormulaeListChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            List<ChemistryNameDataObject> dataObjects = (List<ChemistryNameDataObject>)e.NewValue;
            ReloadFormulaeList(dataObjects, d);
        }

        private static void ReloadFormulaeList(List<ChemistryNameDataObject> listParam, DependencyObject d)
        {
            TreeView namesTree = ((LibraryNamesPanel)d).NamesTreeView;
            var formulaNode = namesTree.Items[1] as TreeViewItem;

            LoadNames(formulaNode, listParam);
        }

        private static void NamesListChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            List<ChemistryNameDataObject> dataObjects = (List<ChemistryNameDataObject>)e.NewValue;
            ReloadNamesList(dataObjects, d);
        }

        private static void ReloadNamesList(List<ChemistryNameDataObject> listParam, DependencyObject d)
        {
            TreeView namesTree = ((LibraryNamesPanel)d).NamesTreeView;
            TreeViewItem nameNode = namesTree.Items[0] as TreeViewItem;
            LoadNames(nameNode, listParam);
        }

        private static void LoadNames(TreeViewItem nameNode, List<ChemistryNameDataObject> listParam)
        {
            nameNode.Items.Clear();
            if (listParam != null)
            {
                Style catHeaderStyle = (Style)nameNode.FindResource("CatSubHeader");

                var namesByNamespace = from name in listParam
                                       group name by name.NameSpace
                                       into ns
                                       select new
                                       {
                                           NS = ns.Key,
                                           Tags = from name2 in ns
                                                  group name2 by name2.Tag
                                                         into taggednames

                                                  select new
                                                  {
                                                      Tag = taggednames.Key,
                                                      Names = taggednames.ToList()
                                                  }
                                       };

                //load the main name headings
                foreach (var grouping in namesByNamespace)
                {
                    var nsNode = new TreeViewItem { Header = grouping.NS, Tag = grouping };
                    nameNode.Items.Add(nsNode);

                    foreach (var tag in grouping.Tags)
                    {
                        var tagChildNode = new TreeViewItem { Header = tag.Tag, Tag = tag };
                        nsNode.Items.Add(tagChildNode);
                        tagChildNode.Style = catHeaderStyle;

                        foreach (var name in tag.Names)
                        {
                            var nameChildNode = new TreeViewItem { Header = name.Name, Tag = name.Name };
                            if (tagChildNode.Tag.ToString().Contains("Formula"))
                            {
                                var tb = TextBlockFromFormula(name.Name);
                                tb.Foreground = new SolidColorBrush(Colors.Black);
                                tagChildNode.Items.Add(tb);
                            }
                            else
                            {
                                tagChildNode.Items.Add(nameChildNode);
                            }
                        }
                    }
                }
            }
        }

        // ToDo: Refactor; This is a near duplicate of $\src\Chemistry\Chem4Word.ACME\Controls\FormulaBlock.cs
        // Ought to be made into common routine
        // Refactor into common code [MAW] ...
        private static TextBlock TextBlockFromFormula(string formula, string prefix = null)
        {
            var textBlock = new TextBlock();

            if (!string.IsNullOrEmpty(prefix))
            {
                // Add in the new element
                var run = new Run($"{prefix} ");
                textBlock.Inlines.Add(run);
            }

            var parts = FormulaHelper.ParseFormulaIntoParts(formula);
            foreach (var formulaPart in parts)
            {
                // Add in the new element
                switch (formulaPart.PartType)
                {
                    case FormulaPartType.Multiplier:
                    case FormulaPartType.Separator:
                        var run1 = new Run(formulaPart.Text);
                        textBlock.Inlines.Add(run1);
                        break;

                    case FormulaPartType.Element:
                        var run2 = new Run(formulaPart.Text);
                        textBlock.Inlines.Add(run2);
                        if (formulaPart.Count > 1)
                        {
                            var subscript = new Run($"{formulaPart.Count}")
                            {
                                BaselineAlignment = BaselineAlignment.Subscript
                            };
                            subscript.FontSize -= 2;
                            textBlock.Inlines.Add(subscript);
                        }

                        break;

                    case FormulaPartType.Charge:
                        var absCharge = Math.Abs(formulaPart.Count);
                        if (absCharge > 1)
                        {
                            var superscript1 = new Run($"{absCharge}{formulaPart.Text}")
                            {
                                BaselineAlignment = BaselineAlignment.Top
                            };
                            superscript1.FontSize -= 3;
                            textBlock.Inlines.Add(superscript1);
                        }
                        else
                        {
                            var superscript2 = new Run($"{formulaPart.Text}")
                            {
                                BaselineAlignment = BaselineAlignment.Top
                            };
                            superscript2.FontSize -= 3;
                            textBlock.Inlines.Add(superscript2);
                        }
                        break;
                }
            }

            return textBlock;
        }
    }
}