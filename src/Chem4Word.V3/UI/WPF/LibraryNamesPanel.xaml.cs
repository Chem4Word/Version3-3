// ---------------------------------------------------------------------------
//  Copyright (c) 2023, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.ACME.Controls;
using IChem4Word.Contracts.Dto;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

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
            DependencyProperty.Register("NamesList", typeof(List<ChemistryNameDataObject>), typeof(LibraryNamesPanel),
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
            DependencyProperty.Register("CaptionsList", typeof(List<ChemistryNameDataObject>), typeof(LibraryNamesPanel),
                                        new PropertyMetadata(null, new PropertyChangedCallback(CaptionsListChanged)));

        #endregion Properties

        private void OnTreeViewCanCopy(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private void OnTreeViewCopy(object sender, ExecutedRoutedEventArgs e)
        {
            if (sender is TreeView treeView)
            {
                if (treeView.SelectedItem is TreeViewItem item)
                {
                    if (!item.HasItems)
                    {
                        Clipboard.SetText(item.Header.ToString());
                    }
                }

                if (treeView.SelectedItem is TextBlock block)
                {
                    Clipboard.SetText(block.Tag.ToString());
                }
            }
        }

        private static void CaptionsListChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var dataObjects = (List<ChemistryNameDataObject>)e.NewValue;
            ReloadCaptionsList(dataObjects, d);
        }

        private static void ReloadCaptionsList(List<ChemistryNameDataObject> captions, DependencyObject d)
        {
            var namesTree = ((LibraryNamesPanel)d).NamesTreeView;

            if (namesTree.Items.Count > 2)
            {
                var captionNode = namesTree.Items[2] as TreeViewItem;

                LoadNames(captionNode, captions);
            }
        }

        private static void FormulaeListChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var dataObjects = (List<ChemistryNameDataObject>)e.NewValue;
            ReloadFormulaeList(dataObjects, d);
        }

        private static void ReloadFormulaeList(List<ChemistryNameDataObject> formulae, DependencyObject d)
        {
            var namesTree = ((LibraryNamesPanel)d).NamesTreeView;
            if (namesTree.Items.Count > 1)
            {
                var formulaNode = namesTree.Items[1] as TreeViewItem;

                LoadNames(formulaNode, formulae);
            }
        }

        private static void NamesListChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var dataObjects = (List<ChemistryNameDataObject>)e.NewValue;
            ReloadNamesList(dataObjects, d);
        }

        private static void ReloadNamesList(List<ChemistryNameDataObject> names, DependencyObject d)
        {
            var namesTree = ((LibraryNamesPanel)d).NamesTreeView;
            if (namesTree.Items.Count > 0)
            {
                var nameNode = namesTree.Items[0] as TreeViewItem;
                LoadNames(nameNode, names);
            }
        }

        private static void LoadNames(TreeViewItem nameNode, List<ChemistryNameDataObject> listParam)
        {
            nameNode.Items.Clear();
            if (listParam != null)
            {
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
                    var nsNode = new TreeViewItem
                    {
                        Header = grouping.NS,
                        Tag = grouping,
                        Template = nameNode.Template,
                        Style = nameNode.Style
                    };
                    nameNode.Items.Add(nsNode);

                    foreach (var tag in grouping.Tags)
                    {
                        var tagChildNode = new TreeViewItem
                        {
                            Header = tag.Tag,
                            Tag = tag,
                            Template = nsNode.Template,
                            Style = nsNode.Style
                        };
                        nsNode.Items.Add(tagChildNode);

                        foreach (var dataObject in tag.Names)
                        {
                            var node = new TreeViewItem
                            {
                                Tag = dataObject.Name,
                                Template = tagChildNode.Template,
                                FontSize = tagChildNode.FontSize
                            };

                            if (tagChildNode.Tag.ToString().Contains("Formula")
                                && !dataObject.Name.ToLower().Equals("not found")
                                && !dataObject.Name.ToLower().Equals("not requested")
                                && !dataObject.Name.ToLower().Equals("unable to calculate"))
                            {
                                node.Header = TextBlockHelper.FromFormula(dataObject.Name);
                            }
                            else
                            {
                                node.Header = dataObject.Name;
                            }
                            tagChildNode.Items.Add(node);
                        }
                    }
                }
            }
        }
    }
}