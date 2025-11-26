// ---------------------------------------------------------------------------
//  Copyright (c) 2025, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.ACME.Controls;
using Chem4Word.Model2;
using Chem4Word.Model2.Converters.CML;
using Chem4Word.Model2.Enums;
using Chem4Word.Model2.Formula;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Chem4Word.ACME
{
    /// <summary>
    /// Interaction logic for LabelsEditor.xaml
    /// </summary>
    public partial class LabelsEditor : UserControl, IHostedWpfEditor
    {
        public Point TopLeft { get; set; }
        public List<string> Used1D { get; set; }
        public bool IsInitialised { get; set; }

        public bool IsDirty { get; set; }
        public Model EditedModel { get; private set; }

        private RenderingOptions _modelRenderingOptions;

        private string _cml;

        public LabelsEditor()
        {
            InitializeComponent();
        }

        public bool ShowTopPanel
        {
            get { return (bool)GetValue(ShowTopPanelProperty); }
            set { SetValue(ShowTopPanelProperty, value); }
        }

        public static readonly DependencyProperty ShowTopPanelProperty =
            DependencyProperty.Register("ShowTopPanel", typeof(bool),
                                        typeof(LabelsEditor),
                                        new FrameworkPropertyMetadata(true,
                                                                      FrameworkPropertyMetadataOptions.AffectsArrange
                                                                        | FrameworkPropertyMetadataOptions.AffectsMeasure
                                                                        | FrameworkPropertyMetadataOptions.AffectsRender));

        private void OnLoaded_LabelsEditor(object sender, RoutedEventArgs e)
        {
            if (!DesignerProperties.GetIsInDesignMode(this)
                && !string.IsNullOrEmpty(_cml)
                && !IsInitialised)
            {
                PopulateTreeView(_cml);
                OnSelectedItemChanged_TreeView(null, null);
                IsInitialised = true;
            }
        }

        private void OnSelectedItemChanged_TreeView(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            Display.Clear();

            LoadNamesEditor(NamesGrid, null);
            LoadNamesEditor(FormulaGrid, null);
            LoadNamesEditor(CaptionsGrid, null);

            if (TreeView.SelectedItem is TreeViewItem item)
            {
                switch (item.Tag)
                {
                    case Model rootModel:
                        Display.Chemistry = rootModel.Copy();
                        break;

                    case Molecule thisMolecule:
                        var model = new Model();
                        model.SetUserOptions(_modelRenderingOptions);
                        var copy = thisMolecule.Copy();
                        model.AddMolecule(copy);
                        copy.Parent = model;

                        if (thisMolecule.Molecules.Count == 0)
                        {
                            LoadNamesEditor(NamesGrid, thisMolecule.Names);
                            LoadNamesEditor(FormulaGrid, thisMolecule.Formulas);
                        }

                        LoadNamesEditor(CaptionsGrid, thisMolecule.Captions);

                        Display.Chemistry = model.Copy();
                        break;
                }
            }
        }

        public void PopulateTreeView(string cml)
        {
            _cml = cml;
            var cc = new CMLConverter();
            EditedModel = cc.Import(_cml, Used1D, relabel: false);
            _modelRenderingOptions = new RenderingOptions(EditedModel);
            TreeView.Items.Clear();
            bool initialSelectionMade = false;

            var style = (Style)TreeView.FindResource("Chem4WordTreeViewItemStyle");

            if (EditedModel != null)
            {
                OverallConciseFormulaPanel.Children.Clear();
                var helper = new FormulaHelperV2(EditedModel);
                OverallConciseFormulaPanel.Children.Add(TextBlockHelper.FromUnicode(helper.Unicode()));

                var root = new TreeViewItem
                {
                    Header = "Structure",
                    Tag = EditedModel,
                    Style = style,
                    ItemContainerStyle = style
                };
                TreeView.Items.Add(root);
                root.IsExpanded = true;

                AddNodes(root, EditedModel.Molecules.Values);
            }

            SetupNamesEditor(NamesGrid, "Add Name", OnClick_AddName, "Alternative name(s) for molecule");
            SetupNamesEditor(FormulaGrid, "Add Formula", OnClick_AddFormula, "Alternative formula for molecule");
            SetupNamesEditor(CaptionsGrid, "Add Caption", OnClick_AddLabel, "Molecule Caption(s)");

            TreeView.Focus();

            OnSelectedItemChanged_TreeView(null, null);

            // Local Function to support recursion
            void AddNodes(TreeViewItem parent, IEnumerable<Molecule> molecules)
            {
                foreach (var molecule in molecules)
                {
                    var tvi = new TreeViewItem
                    {
                        Style = style,
                        ItemContainerStyle = style
                    };

                    if (molecule.Atoms.Count == 0)
                    {
                        var stackPanel = new StackPanel
                        {
                            Orientation = Orientation.Horizontal
                        };
                        var helper = new FormulaHelperV2(molecule);
                        stackPanel.Children.Add(TextBlockHelper.FromUnicode(helper.UnicodeOfChildren(), "Group:"));

                        tvi.Header = stackPanel;
                        tvi.Tag = molecule;
                    }
                    else
                    {
                        var stackPanel = new StackPanel
                        {
                            Orientation = Orientation.Horizontal
                        };
                        var helper = new FormulaHelperV2(molecule);
                        stackPanel.Children.Add(TextBlockHelper.FromUnicode(helper.Unicode()));

                        tvi.Header = stackPanel;
                        tvi.Tag = molecule;
                    }

#if DEBUG
                    tvi.ToolTip = molecule.Path;
#endif

                    parent.Items.Add(tvi);
                    tvi.IsExpanded = true;
                    if (!initialSelectionMade)
                    {
                        tvi.IsSelected = true;
                        initialSelectionMade = true;
                    }

                    molecule.Captions.CollectionChanged += OnCollectionChanged;
                    foreach (var property in molecule.Captions)
                    {
                        property.PropertyChanged += OnTextualPropertyChanged;
                    }

                    molecule.Formulas.CollectionChanged += OnCollectionChanged;
                    foreach (var property in molecule.Formulas)
                    {
                        property.PropertyChanged += OnTextualPropertyChanged;
                    }

                    molecule.Names.CollectionChanged += OnCollectionChanged;
                    foreach (var property in molecule.Names)
                    {
                        property.PropertyChanged += OnTextualPropertyChanged;
                    }

                    AddNodes(tvi, molecule.Molecules.Values);
                }
            }
        }

        public void SetCount(int? value)
        {
            if (EditedModel != null)
            {
                var parent = EditedModel.Molecules.First().Value;
                parent.Count = value;
                OnSelectedItemChanged_TreeView(null, null);
            }
        }

        public void SetFormalCharge(int? value)
        {
            if (EditedModel != null)
            {
                var parent = EditedModel.Molecules.First().Value;
                parent.FormalCharge = value;
                OnSelectedItemChanged_TreeView(null, null);
            }
        }

        public void SetMultiplicity(int? value)
        {
            if (EditedModel != null)
            {
                var parent = EditedModel.Molecules.First().Value;
                parent.SpinMultiplicity = value;
                OnSelectedItemChanged_TreeView(null, null);
            }
        }

        public void SetHydrogensMode(HydrogenLabels? value)
        {
            if (EditedModel != null)
            {
                var parent = EditedModel.Molecules.First().Value;
                parent.ExplicitH = value;
                OnSelectedItemChanged_TreeView(null, null);
            }
        }

        public void SetShowCarbons(bool? value)
        {
            if (EditedModel != null)
            {
                var parent = EditedModel.Molecules.First().Value;
                parent.ExplicitC = value;
                OnSelectedItemChanged_TreeView(null, null);
            }
        }

        public void SetShowBrackets(bool? value)
        {
            if (EditedModel != null)
            {
                var parent = EditedModel.Molecules.First().Value;
                parent.ShowMoleculeBrackets = value;
                OnSelectedItemChanged_TreeView(null, null);
            }
        }

        private void OnClick_AddFormula(object sender, RoutedEventArgs e)
        {
            if (TreeView.SelectedItem is TreeViewItem treeViewItem
                && treeViewItem.Tag is Molecule molecule)
            {
                molecule.Formulas.Add(new TextualProperty
                {
                    Id = molecule.GetNextId(molecule.Formulas, "f"),
                    FullType = ModelConstants.ValueChem4WordFormula,
                    Value = "?",
                    CanBeDeleted = true
                });
                FormulaGrid.ScrollViewer.ScrollToEnd();
            }
        }

        private void OnClick_AddName(object sender, RoutedEventArgs e)
        {
            if (TreeView.SelectedItem is TreeViewItem treeViewItem
                && treeViewItem.Tag is Molecule molecule)
            {
                molecule.Names.Add(new TextualProperty
                {
                    Id = molecule.GetNextId(molecule.Names, "n"),
                    FullType = ModelConstants.ValueChem4WordSynonym,
                    Value = "?",
                    CanBeDeleted = true
                });
                NamesGrid.ScrollViewer.ScrollToEnd();
            }
        }

        private void OnClick_AddLabel(object sender, RoutedEventArgs e)
        {
            if (TreeView.SelectedItem is TreeViewItem treeViewItem
                && treeViewItem.Tag is Molecule molecule)
            {
                molecule.Captions.Add(new TextualProperty
                {
                    Id = molecule.GetNextId(molecule.Captions, "l"),
                    FullType = ModelConstants.ValueChem4WordCaption,
                    Value = "?",
                    CanBeDeleted = true
                });
                CaptionsGrid.ScrollViewer.ScrollToEnd();
            }
        }

        private void OnTextualPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            IsDirty = true;
        }

        private void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            IsDirty = true;
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                foreach (TextualProperty item in e.NewItems)
                {
                    item.PropertyChanged += OnTextualPropertyChanged;
                }
            }

            if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                foreach (TextualProperty item in e.OldItems)
                {
                    item.PropertyChanged -= OnTextualPropertyChanged;
                }
            }
        }

        private void SetupNamesEditor(NamesEditor namesEditor, string buttonCaption, RoutedEventHandler routedEventHandler, string toolTip)
        {
            namesEditor.AddButtonCaption.Text = buttonCaption;
            namesEditor.AddButton.ToolTip = toolTip;
            // Remove existing handler if present (NB: -= should never crash)
            namesEditor.AddButton.Click -= routedEventHandler;
            namesEditor.AddButton.Click += routedEventHandler;
            namesEditor.AddButton.IsEnabled = false;
        }

        private void LoadNamesEditor(NamesEditor namesEditor, ObservableCollection<TextualProperty> data)
        {
            namesEditor.AddButton.IsEnabled = data != null;
            namesEditor.NamesModel.ListOfNames = data;
        }
    }
}
