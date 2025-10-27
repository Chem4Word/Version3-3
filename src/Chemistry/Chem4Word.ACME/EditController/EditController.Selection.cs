// ---------------------------------------------------------------------------
//  Copyright (c) 2025, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.ACME.Adorners.Selectors;
using Chem4Word.ACME.Drawing.Visuals;
using Chem4Word.ACME.Enums;
using Chem4Word.Core.Helpers;
using Chem4Word.Model2;
using Chem4Word.Model2.Enums;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Windows.Documents;

namespace Chem4Word.ACME
{
    /// <summary>
    /// Bundles all the selection related methods, variables,objects etc. for the EditController class.
    /// The code is as complex as it needs to be.
    /// For the sake of separation of concerns, tha process of adding and removing various chemical objects
    /// is decoupled from the response of the EditEontroller to those changes.
    /// We do this by using an ObservableCollection to hold the selected objects, and sinking the events.
    /// </summary>
    public partial class EditController
    {
        #region Fields

        private readonly Dictionary<object, Adorner> SelectionAdorners = new Dictionary<object, Adorner>();
        private MultiAtomBondAdorner _multiAdorner;
        private readonly ReadOnlyObservableCollection<object> _selectedItemsWrapper;
        private readonly ObservableCollection<object> _selectedItems;

        private ElementBase _selectedElement;
        private ReactionType? _selectedReactionType;
        private ReactionVisual _selReactionVisual;

        #endregion Fields

        #region Properties

        /// <summary>
        /// The current MultiAtomBondAdorner, if any
        /// </summary>
        public MultiAtomBondAdorner MultiAdorner
        {
            get
            {
                return _multiAdorner;
            }
            private set
            {
                _multiAdorner = value;
            }
        }

        /// <summary>
        /// Wraps the collection of objects that make up the selection
        /// </summary>
        public ReadOnlyObservableCollection<object> SelectedItems
        {
            get
            {
                return _selectedItemsWrapper;
            }
        }

        /// <summary>
        /// Flag enumeration that precisely describes the makeup of the current selection
        /// </summary>
        public SelectionTypeCode SelectionType
        {
            get
            {
                SelectionTypeCode result = SelectionTypeCode.None;

                if (SelectedItems.OfType<Atom>().Any())
                {
                    result |= SelectionTypeCode.Atom;
                }

                if (SelectedItems.OfType<Bond>().Any())
                {
                    result |= SelectionTypeCode.Bond;
                }

                if (SelectedItems.OfType<Molecule>().Any())
                {
                    result |= SelectionTypeCode.Molecule;
                }

                if (SelectedItems.OfType<Reaction>().Any())
                {
                    result |= SelectionTypeCode.Reaction;
                }

                if (SelectedItems.OfType<Annotation>().Any())
                {
                    result |= SelectionTypeCode.Annotation;
                }

                return result;
            }
        }

        /// <summary>
        /// Used as a binding source for the Element dropdown on the toolbar.
        /// Will default to the last selected element if multiple or no elements are selected
        /// </summary>
        public ElementBase SelectedElement
        {
            get
            {
                List<ElementBase> selElements = SelectedElements;

                switch (selElements.Count)
                {
                    // Nothing selected, return last value selected
                    case 0:
                        return _selectedElement;

                    case 1:
                        return selElements[0];
                    // More than one selected !
                    default:
                        return null;
                }
            }
            set
            {
                _selectedElement = value;

                List<Atom> selAtoms = SelectedItems.OfType<Atom>().ToList();
                if (value != null)
                {
                    SetElement(value, selAtoms);
                }

                OnPropertyChanged();
            }
        }

        /// <summary>
        /// returns a distinct list of selected elements
        /// </summary>
        private List<ElementBase> SelectedElements
        {
            get
            {
                IEnumerable<Molecule> singletons = from Molecule m in SelectedItems.OfType<Molecule>()
                                                   where m.Atoms.Count == 1
                                                   select m;

                IEnumerable<Atom> allSelAtoms = (from Atom a in SelectedItems.OfType<Atom>()
                                                 select a).Union(
                    from Molecule m in singletons
                    from Atom a1 in m.Atoms.Values
                    select a1);
                IEnumerable<ElementBase> elements = (from selAtom in allSelAtoms
                                                     select selAtom.Element).Distinct();

                return elements.ToList();
            }
        }

        /// <summary>
        /// Have we got a single molecule selected?
        /// </summary>
        public bool SingleMolSelected
        {
            get { return SelectedItems.Count == 1 && SelectedItems[0] is Molecule; }
        }

        #endregion Properties

        #region Methods

        /// <summary>
        /// Updates the command status.  Should generally only be called
        /// after the active selection is changed
        /// </summary>
        private void UpdateCommandStatuses()
        {
            CopyCommand.RaiseCanExecChanged();
            GroupCommand.RaiseCanExecChanged();
            UnGroupCommand.RaiseCanExecChanged();
            CutCommand.RaiseCanExecChanged();
            FlipHorizontalCommand.RaiseCanExecChanged();
            FlipVerticalCommand.RaiseCanExecChanged();
            AddHydrogensCommand.RaiseCanExecChanged();
            RemoveHydrogensCommand.RaiseCanExecChanged();

            AlignBottomsCommand.RaiseCanExecChanged();
            AlignMiddlesCommand.RaiseCanExecChanged();
            AlignTopsCommand.RaiseCanExecChanged();

            AlignLeftsCommand.RaiseCanExecChanged();
            AlignCentresCommand.RaiseCanExecChanged();
            AlignRightsCommand.RaiseCanExecChanged();

            EditReagentsCommand.RaiseCanExecChanged();
            EditConditionsCommand.RaiseCanExecChanged();

            AssignReactionRolesCommand.RaiseCanExecChanged();
            ClearReactionRolesCommand.RaiseCanExecChanged();

            EditSelectionPropertiesCommand.RaiseCanExecChanged();

            EditActiveBondPropertiesCommand.RaiseCanExecChanged();
            EditActiveBondPropertiesCommand.RaiseCanExecChanged();
            FlipBondStereoCommand.RaiseCanExecChanged();
            PropertiesCommand.RaiseCanExecChanged();
        }

        /// <summary>
        /// Removes all atom and bond adorners for a given molecule and its children.
        /// Also disconnects event handlers.
        /// Called before the molecule itself is selected
        /// </summary>
        /// <param name="selectedMolecule"></param>
        private void RemoveAtomBondAdorners(Molecule selectedMolecule)
        {
            AdornerLayer layer = AdornerLayer.GetAdornerLayer(EditingCanvas);
            foreach (Bond bond in selectedMolecule.Bonds)
            {
                if (SelectionAdorners.ContainsKey(bond))
                {
                    Adorner selectionAdorner = SelectionAdorners[bond];
                    selectionAdorner.MouseLeftButtonDown -= OnMouseLeftButtonDown_SelAdorner;
                    layer.Remove(selectionAdorner);
                    SelectionAdorners.Remove(bond);
                }
            }

            foreach (Atom atom in selectedMolecule.Atoms.Values)
            {
                if (SelectionAdorners.ContainsKey(atom))
                {
                    Adorner selectionAdorner = SelectionAdorners[atom];
                    selectionAdorner.MouseLeftButtonDown -= OnMouseLeftButtonDown_SelAdorner;
                    layer.Remove(selectionAdorner);
                    SelectionAdorners.Remove(atom);
                }
            }

            foreach (Molecule mol in selectedMolecule.Molecules.Values)
            {
                RemoveAtomBondAdorners(mol);
            }
        }

        /// <summary>
        /// Clears all adorners from the editing canvas
        /// </summary>
        public void RemoveAllAdorners()
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            try
            {
                AdornerLayer layer = AdornerLayer.GetAdornerLayer(EditingCanvas);
                if (layer != null)
                {
                    Adorner[] adornerList = layer.GetAdorners(EditingCanvas);
                    if (adornerList != null)
                    {
                        foreach (Adorner adorner in adornerList)
                        {
                            layer.Remove(adorner);
                        }
                    }
                }

                SelectionAdorners.Clear();
            }
            catch (Exception exception)
            {
                WriteTelemetryException(module, exception);
            }
        }

        /// <summary>
        /// Removes the current atom/bond multi-adorner (if any)
        /// then creates a new one for the current selection (if any)
        /// </summary>
        public void UpdateAtomBondAdorners()
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                if (MultiAdorner != null)
                {
                    MultiAdorner.MouseLeftButtonDown -= OnMouseLeftButtonDown_SelAdorner;
                    AdornerLayer layer = AdornerLayer.GetAdornerLayer(EditingCanvas);
                    layer.Remove(MultiAdorner);
                    MultiAdorner = null;
                }

                List<BaseObject> selAtomBonds = (from BaseObject sel in _selectedItems
                                                 where sel is Atom || sel is Bond
                                                 select sel).ToList();

                if (selAtomBonds.Any())
                {
                    MultiAdorner = new MultiAtomBondAdorner(EditingCanvas, selAtomBonds);
                    MultiAdorner.MouseLeftButtonDown += OnMouseLeftButtonDown_SelAdorner;
                }
            }
            catch (Exception exception)
            {
                WriteTelemetryException(module, exception);
            }
        }

        /// <summary>
        /// Removes adorners for a list of objects.
        /// </summary>
        /// <param name="oldObjects"></param>
        private void RemoveSelectionAdorners(IList oldObjects)
        {
            AdornerLayer layer = AdornerLayer.GetAdornerLayer(EditingCanvas);
            foreach (object oldObject in oldObjects)
            {
                if (SelectionAdorners.ContainsKey(oldObject))
                {
                    Adorner selectionAdorner = SelectionAdorners[oldObject];
                    if (selectionAdorner is MoleculeSelectionAdorner msAdorner)
                    {
                        msAdorner.DragIsCompleted -= OnDragCompleted_MolAdorner;
                        msAdorner.MouseLeftButtonDown -= OnMouseLeftButtonDown_SelAdorner;
                        msAdorner.DragIsCompleted -= OnDragCompleted_MolAdorner;
                    }

                    layer.Remove(selectionAdorner);
                    SelectionAdorners.Remove(oldObject);
                }
            }
        }

        /// <summary>
        /// Adds adorners for a list of objects.  Should only be called from
        /// events on the _selectedItems collection AFTER the collection
        /// has been updated
        /// </summary>
        /// <param name="newObjects"></param>
        private void AddSelectionAdorners(IList newObjects)
        {
            List<Molecule> singleAtomMols = (from m in newObjects.OfType<Molecule>().Union(SelectedItems.OfType<Molecule>())
                                             where m.Atoms.Count == 1
                                             select m).ToList();
            List<Molecule> groupMols = (from m in newObjects.OfType<Molecule>().Union(SelectedItems.OfType<Molecule>())
                                        where m.IsGrouped
                                        select m).ToList();
            List<Molecule> allMolecules = (from m in newObjects.OfType<Molecule>().Union(SelectedItems.OfType<Molecule>())
                                           select m).ToList();
            List<Reaction> allReactions = (from r in newObjects.OfType<Reaction>().Union(SelectedItems.OfType<Reaction>())
                                           select r).ToList();

            List<Annotation> allAnnotations = (from r in newObjects.OfType<Annotation>().Union(SelectedItems.OfType<Annotation>())
                                               select r).ToList();

            bool allSingletons = singleAtomMols.Count == allMolecules.Count && singleAtomMols.Any();
            bool allGroups = allMolecules.Count == groupMols.Count && groupMols.Any();

            if (allSingletons) //all single objects
            {
                RemoveAllAdorners();
                SingleObjectSelectionAdorner atomAdorner =
                    new SingleObjectSelectionAdorner(EditingCanvas, singleAtomMols);
                foreach (Molecule mol in singleAtomMols)
                {
                    SelectionAdorners[mol] = atomAdorner;
                }

                atomAdorner.MouseLeftButtonDown += OnMouseLeftButtonDown_SelAdorner;
                atomAdorner.DragIsCompleted += OnDragCompleted_AtomAdorner;
            }
            else if (allGroups)
            {
                if (!(allReactions.Any() || allAnnotations.Any())) //no reactions selected
                {
                    RemoveAllAdorners();
                    GroupSelectionAdorner groupAdorner = new GroupSelectionAdorner(EditingCanvas,
                                                                 groupMols.Cast<BaseObject>().ToList());
                    foreach (Molecule mol in groupMols)
                    {
                        SelectionAdorners[mol] = groupAdorner;
                    }

                    groupAdorner.MouseLeftButtonDown += OnMouseLeftButtonDown_SelAdorner;
                    groupAdorner.DragIsCompleted += OnDragCompleted_AtomAdorner;
                }
                else //some reactions & groups
                {
                    RemoveAllAdorners();
                    AddMixed(allMolecules,
                             allReactions.Union(allAnnotations.Cast<BaseObject>()).ToList());
                }
            }
            else if (allMolecules.Any())
            {
                if (!(allReactions.Any() || allAnnotations.Any())) //no reactions
                {
                    RemoveAllAdorners();
                    MoleculeSelectionAdorner molAdorner = new MoleculeSelectionAdorner(EditingCanvas,
                                                                  allMolecules.Cast<BaseObject>().ToList());
                    foreach (Molecule mol in allMolecules)
                    {
                        SelectionAdorners[mol] = molAdorner;
                    }

                    molAdorner.MouseLeftButtonDown += OnMouseLeftButtonDown_SelAdorner;
                    molAdorner.DragIsCompleted += OnDragCompleted_AtomAdorner;
                }
                else //some reactions & molecules
                {
                    RemoveAllAdorners();
                    AddMixed(allMolecules,
                             allReactions.Union(allAnnotations.Cast<BaseObject>()).ToList());
                }
            }
            else //just reactions or annotations
            {
                RemoveAllAdorners();
                if (allReactions.Count + allAnnotations.Count > 1)
                {
                    AddMixed(allMolecules,
                             allReactions.Union(allAnnotations.Cast<BaseObject>()).ToList());
                }
                else
                {
                    if (allReactions.Any())
                    {
                        Reaction r = allReactions.First();
                        ReactionSelectionAdorner reactionAdorner =
                            new ReactionSelectionAdorner(EditingCanvas,
                                                         EditingCanvas.ChemicalVisuals[r] as ReactionVisual);
                        SelectionAdorners[r] = reactionAdorner;
                        reactionAdorner.MouseLeftButtonDown += OnMouseLeftButtonDown_SelAdorner;
                    }

                    if (allAnnotations.Any())
                    {
                        Annotation a = allAnnotations.First();
                        SingleObjectSelectionAdorner annotationAdorner =
                            new SingleObjectSelectionAdorner(EditingCanvas, new List<BaseObject> { a });
                        SelectionAdorners[a] = annotationAdorner;
                        annotationAdorner.MouseLeftButtonDown += OnMouseLeftButtonDown_SelAdorner;
                    }
                }
            }

            //local function
            void AddMixed(List<Molecule> mols, List<BaseObject> selObjects)
            {
                List<BaseObject> objects = mols.Cast<BaseObject>().ToList();
                objects = objects.Union(selObjects).ToList();
                MoleculeSelectionAdorner selector;
                if (mols.Any(m => m.IsGrouped))
                {
                    selector = new GroupSelectionAdorner(EditingCanvas, objects);
                }
                else
                {
                    selector = new MoleculeSelectionAdorner(EditingCanvas, objects);
                }

                foreach (object o in selObjects)
                {
                    SelectionAdorners[o] = selector;
                }
            }
        }

        /// <summary>
        /// Have we got a single object (atom, bond or molecule) selected?
        /// </summary>
        public bool SingleObjectSelected
        {
            get
            {
                return SingleMolSelected || SingleAtomSelected || SingleBondSelected;
            }
        }

        /// <summary>
        /// Have we got a single bond selected?
        /// </summary>
        public bool SingleBondSelected
        {
            get
            {
                return SelectionType == SelectionTypeCode.Bond && SelectedItems.Count == 1 && SelectedItems[0] is Bond;
            }
        }

        /// <summary>
        /// Have we got a single atom selected?
        /// </summary>
        public bool SingleAtomSelected
        {
            get
            {
                return SelectionType == SelectionTypeCode.Atom && SelectedItems.Count == 1 && SelectedItems[0] is Atom;
            }
        }

        /// <summary>
        /// Adds a chemical object to the current selection
        /// </summary>
        /// <param name="thingToAdd"></param>
        public void AddToSelection(BaseObject thingToAdd)
        {
            Molecule parent = (thingToAdd as Atom)?.Parent ?? (thingToAdd as Bond)?.Parent;

            List<BaseObject> thingsToAdd = new List<BaseObject> { thingToAdd };
            if (parent != null)
            {
                if (!SelectedItems.Contains(parent))
                {
                    AddObjectListToSelection(thingsToAdd);
                }
            }
            else
            {
                if (SelectedItems.Contains(thingsToAdd))
                {
                    RemoveFromSelection(thingsToAdd);
                }

                AddObjectListToSelection(thingsToAdd);
            }
        }

        /// <summary>
        /// Adds a list of chemical objects to the current selection.
        /// Groups them up from the bottom upwards as needed:
        /// Atoms into Molecules, Molecules into Groups
        /// </summary>
        /// <param name="thingsToAdd"></param>
        public void AddObjectListToSelection(List<BaseObject> thingsToAdd)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            try
            {
                Stopwatch sw = new Stopwatch();
                sw.Start();

                DebugHelper.WriteLine($"Started at {SafeDate.ToShortTime(DateTime.UtcNow)}");

                //take a snapshot of the current selection
                List<object> currentSelection = SelectedItems.ToList();
                //add all the new items to the existing selection
                List<object> allItems = currentSelection.Union(thingsToAdd).ToList();

                DebugHelper.WriteLine($"Timing: {sw.ElapsedMilliseconds}ms");

                //phase one - group atoms into the molecules
                //grab all parent molecules for selected atoms
                var allParents = (from a in allItems.OfType<Atom>()
                                  group a by a.Parent
                                  into parent
                                  select new { Parent = parent.Key, Count = parent.Count() }).ToList();

                //and grab all of those that have all atoms selected
                List<Molecule> fullParents = (from m in allParents
                                              where m.Count == m.Parent.AtomCount
                                              select m.Parent).ToList();

                DebugHelper.WriteLine($"Timing: {sw.ElapsedMilliseconds}ms");

                //now add all the molecules that haven't been selected
                //first clear out the atoms
                foreach (Molecule fullMolecule in fullParents)
                {
                    foreach (Atom atom in fullMolecule.Atoms.Values)
                    {
                        _selectedItems.Remove(atom);
                        thingsToAdd.Remove(atom);
                    }

                    foreach (Bond bond in fullMolecule.Bonds)
                    {
                        _selectedItems.Remove(bond);
                        thingsToAdd.Remove(bond);
                    }

                    //and add in the selected parent
                    if (!_selectedItems.Contains(fullMolecule.RootMolecule))
                    {
                        _selectedItems.Add(fullMolecule.RootMolecule);
                    }
                }

                DebugHelper.WriteLine($"Timing: {sw.ElapsedMilliseconds}ms");

                List<Molecule> newMols = thingsToAdd.OfType<Molecule>().ToList();
                foreach (Molecule molecule in newMols)
                {
                    if (_selectedItems.Contains(molecule.RootMolecule))
                    {
                        _selectedItems.Remove(molecule.RootMolecule);
                    }

                    _selectedItems.Add(molecule.RootMolecule);
                    thingsToAdd.Remove(molecule);
                }

                DebugHelper.WriteLine($"Timing: {sw.ElapsedMilliseconds}ms");

                //now we need to process remaining individual atoms
                List<Atom> newAtoms = thingsToAdd.OfType<Atom>().ToList();
                foreach (Atom newAtom in newAtoms)
                {
                    if (!_selectedItems.Contains(newAtom))
                    {
                        _selectedItems.Add(newAtom);
                        thingsToAdd.Remove(newAtom);
                        //add in the bonds between this atom and any other selected atoms
                        foreach (Bond bond in newAtom.Bonds)
                        {
                            if (!(_selectedItems.Contains(bond)) && _selectedItems.Contains(bond.OtherAtom(newAtom)))
                            {
                                _selectedItems.Add(bond);
                                if (thingsToAdd.Contains(bond))
                                {
                                    thingsToAdd.Remove(bond);
                                }
                            }
                        }
                    }
                }

                DebugHelper.WriteLine($"Timing: {sw.ElapsedMilliseconds}ms");

                //now add in any remaining bonds
                List<Bond> newBonds = thingsToAdd.OfType<Bond>().ToList();

                foreach (Bond newBond in newBonds)
                {
                    if (!(_selectedItems.Contains(newBond)
                          || _selectedItems.Contains(newBond.Parent.RootMolecule)))
                    {
                        _selectedItems.Add(newBond);
                        if (thingsToAdd.Contains(newBond))
                        {
                            thingsToAdd.Remove(newBond);
                        }
                    }
                }

                if (EditingCanvas != null)
                {
                    UpdateAtomBondAdorners();
                }

                //now do the reactions
                List<Reaction> newReactions = thingsToAdd.OfType<Reaction>().ToList();

                foreach (Reaction newReaction in newReactions)
                {
                    if (!_selectedItems.Contains(newReaction))
                    {
                        _selectedItems.Add(newReaction);
                        if (thingsToAdd.Contains(newReaction))
                        {
                            thingsToAdd.Remove(newReaction);
                        }
                    }
                }

                //finally the annotations
                List<Annotation> newAnnotations = thingsToAdd.OfType<Annotation>().ToList();

                foreach (Annotation annotation in newAnnotations)
                {
                    if (!_selectedItems.Contains(annotation))
                    {
                        _selectedItems.Add(annotation);
                        if (thingsToAdd.Contains(annotation))
                        {
                            thingsToAdd.Remove(annotation);
                        }
                    }
                }

                DebugHelper.WriteLine($"Timing: {sw.ElapsedMilliseconds}ms");
                DebugHelper.WriteLine($"Finished at {DateTime.UtcNow}");
            }
            catch (Exception exception)
            {
                WriteTelemetryException(module, exception);
            }
        }

        /// <summary>
        /// Wipes the current selection
        /// </summary>
        public void ClearSelection()
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                _selectedItems.Clear();
            }
            catch (Exception exception)
            {
                WriteTelemetryException(module, exception);
            }
        }

        /// <summary>
        /// Removes an object from the current select
        /// </summary>
        /// <param name="thingToRemove"></param>
        public void RemoveFromSelection(object thingToRemove)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            try
            {
                RemoveFromSelection(new List<object> { thingToRemove });
            }
            catch (Exception exception)
            {
                WriteTelemetryException(module, exception);
            }
        }

        /// <summary>
        /// Removes a list of objects from the current selection
        /// </summary>
        /// <param name="thingsToRemove"></param>
        private void RemoveFromSelection(List<object> thingsToRemove)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                // grab all the molecules that contain selected objects
                foreach (object o in thingsToRemove)
                {
                    switch (o)
                    {
                        case Atom atom:
                            {
                                if (atom.Singleton) //it's a single atom molecule
                                {
                                    _selectedItems.Remove(atom.Parent);
                                }

                                if (_selectedItems.Contains(atom))
                                {
                                    _selectedItems.Remove(atom);
                                }

                                break;
                            }

                        case Bond bond:
                            {
                                if (_selectedItems.Contains(bond))
                                {
                                    _selectedItems.Remove(bond);
                                }

                                break;
                            }

                        case Molecule mol:
                            {
                                if (_selectedItems.Contains(mol))
                                {
                                    _selectedItems.Remove(mol);
                                }

                                break;
                            }
                        case Reaction r:
                            {
                                _selectedItems.Remove(r);
                            }
                            break;
                    }
                }

                if (EditingCanvas != null)
                {
                    UpdateAtomBondAdorners();
                }
            }
            catch (Exception exception)
            {
                WriteTelemetryException(module, exception);
            }
        }

        /// <summary>
        /// Selects all visible molecules
        /// </summary>
        public void SelectAll()
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                ClearSelection();
                List<BaseObject> selection = new List<BaseObject>();
                foreach (Molecule mol in Model.Molecules.Values)
                {
                    selection.Add(mol);
                }

                foreach (Reaction r in Model.DefaultReactionScheme.Reactions.Values)
                {
                    selection.Add(r);
                }

                foreach (Annotation a in Model.Annotations.Values)
                {
                    selection.Add(a);
                }

                AddObjectListToSelection(selection);
            }
            catch (Exception exception)
            {
                WriteTelemetryException(module, exception);
            }

            SendStatus((DefaultStatusMessage, TotUpMolFormulae(), TotUpSelectedMwt()));
        }

        #endregion Methods

        #region Event Handlers

        /// <summary>
        /// The pivotal routine for handling selection in the EditController
        /// All display for selections *must* go through this routine.  No ifs, no buts
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnChanged_SelectedItems(object sender, NotifyCollectionChangedEventArgs e)
        {
            IList newObjects = e.NewItems;
            IList oldObjects = e.OldItems;

            if (newObjects != null)
            {
                AddSelectionAdorners(newObjects);
            }

            if (oldObjects != null)
            {
                RemoveSelectionAdorners(oldObjects);
            }

            if (e.Action == NotifyCollectionChangedAction.Reset)
            {
                RemoveAllAdorners();
            }

            OnPropertyChanged(nameof(SelectedElement));
            OnPropertyChanged(nameof(SelectedBondOptionId));
            OnPropertyChanged(nameof(SelectionType));
            OnPropertyChanged(nameof(SelectedReactionType));
            //tell the editor what commands are allowable
            UpdateCommandStatuses();
        }

        #endregion Event Handlers
    }
}
