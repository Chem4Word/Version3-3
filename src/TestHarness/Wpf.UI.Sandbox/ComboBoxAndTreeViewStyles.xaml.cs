// ---------------------------------------------------------------------------
//  Copyright (c) 2025, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System;
using System.Windows;
using System.Windows.Controls;

namespace Wpf.UI.Sandbox
{
    /// <summary>
    /// Interaction logic for ComboBoxAndTreeViewStyles.xaml
    /// </summary>
    public partial class ComboBoxAndTreeViewStyles : Window
    {
        public ComboBoxAndTreeViewStyles()
        {
            InitializeComponent();
        }

        private void OnClick_ButtonBase(object sender, RoutedEventArgs e)
        {
            if (sender is Button button)
            {
                switch (button.Tag.ToString())
                {
                    case "Ootb":
                        AddChildNode((TreeViewItem)Ootb.SelectedItem);
                        break;

                    case "Chem4Word":
                        AddChildNode((TreeViewItem)Chem4Word.SelectedItem);
                        break;

                    case "OotbTop":
                        AddTopLevelNode(Ootb);
                        break;

                    case "Chem4WordTop":
                        AddTopLevelNode(Chem4Word);
                        break;
                }
            }
        }

        private void AddTopLevelNode(TreeView treeView)
        {
            var style = (Style)treeView.FindResource("Chem4WordTreeViewItemStyle");
            var template = (ControlTemplate)treeView.FindResource("Chem4WordTreeViewItemTemplate");
            var node = new TreeViewItem
            {
                Header = $"{DateTime.UtcNow:HH:mm:ss.fff}",
                Template = template,
                // Don't add style if Black Item is required
                Style = style,
                //FontSize = 14
            };
            treeView.Items.Add(node);
        }

        private void AddChildNode(TreeViewItem item)
        {
            if (item != null)
            {
                var node = new TreeViewItem
                {
                    Header = $"{DateTime.UtcNow:HH:mm:ss.fff}",
                    Template = item.Template,
                    // Don't add style if Black Item is required
                    Style = item.Style,
                    //FontSize = item.FontSize
                };
                item.Items.Add(node);
            }
        }
    }
}