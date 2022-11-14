// ---------------------------------------------------------------------------
//  Copyright (c) 2022, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using IChem4Word.Contracts.Dto;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

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
                Style defaultNameStyle = (Style)nameNode.FindResource("DefaultNameNode");
                Style catHeaderStyle = (Style)nameNode.FindResource("CatSubHeader");
                bool firstNameNode = true;

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
                            tagChildNode.Items.Add(nameChildNode);
                            if (firstNameNode)
                            {
                                nameChildNode.Style = defaultNameStyle;
                                firstNameNode = false;
                            }
                        }
                    }
                }
            }
        }
    }
}