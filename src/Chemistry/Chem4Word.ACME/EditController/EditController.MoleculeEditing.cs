// ---------------------------------------------------------------------------
//  Copyright (c) 2025, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.ACME.Adorners.Selectors;
using Chem4Word.ACME.Commands;
using Chem4Word.ACME.Commands.Grouping;
using Chem4Word.ACME.Commands.Layout.Flipping;
using Chem4Word.Core.Helpers;
using Chem4Word.Model2;
using Chem4Word.Model2.Enums;
using Chem4Word.Model2.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace Chem4Word.ACME
{
    public partial class EditController
    {
        #region Fields

        private FuseCommand _fuseCommand;
        private GroupCommand _groupCommand;
        private UnGroupCommand _unGroupCommand;

        private FlipVerticalCommand _flipVerticalCommand;
        private FlipHorizontalCommand _flipHorizontalCommand;

        #endregion Fields

        #region Commands

        /// <summary>
        /// There is currently no fuse button on the toolbar!
        /// </summary>
        public FuseCommand FuseCommand
        {
            get
            {
                return _fuseCommand;
            }
            set
            {
                _fuseCommand = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Groups a set of molecules under a parent molecule
        /// </summary>
        public GroupCommand GroupCommand
        {
            get
            {
                return _groupCommand;
            }
            set
            {
                _groupCommand = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// splits a grouped molecule into its constituents
        /// </summary>
        public UnGroupCommand UnGroupCommand
        {
            get
            {
                return _unGroupCommand;
            }
            set
            {
                _unGroupCommand = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Flips a molecule vertically
        /// </summary>
        public FlipVerticalCommand FlipVerticalCommand
        {
            get
            {
                return _flipVerticalCommand;
            }
            set
            {
                _flipVerticalCommand = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Flips a molecule horizontally
        /// </summary>
        public FlipHorizontalCommand FlipHorizontalCommand
        {
            get
            {
                return _flipHorizontalCommand;
            }
            set
            {
                _flipHorizontalCommand = value;
                OnPropertyChanged();
            }
        }

        #endregion Commands

        #region Methods

        /// <summary>
        /// removes a mol from the selection and deletes it from the model
        /// </summary>
        /// <param name="mol"></param>
        public void DeleteMolecule(Molecule mol)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                WriteTelemetry(module, "Debug", "Called");

                Action redo = () =>
                              {
                                  RemoveFromSelection(mol);
                                  Model.RemoveMolecule(mol);
                                  mol.Parent = null;
                                  SendStatus(("Molecule(s) deleted", TotUpMolFormulae(), TotUpSelectedMwt()));
                              };

                Action undo = () =>
                              {
                                  mol.Parent = Model;
                                  Model.AddMolecule(mol);
                                  AddToSelection(mol);
                              };

                UndoManager.BeginUndoBlock();
                UndoManager.RecordAction(undo, redo);
                UndoManager.EndUndoBlock();
                redo();
            }
            catch (Exception exception)
            {
                WriteTelemetryException(module, exception);
            }

            CheckModelIntegrity(module);
        }

        /// <summary>
        /// Forces the Model to update the Display of the supplied molecules
        /// </summary>
        /// <param name="mols"></param>
        private void RefreshMolecules(List<Molecule> mols)
        {
            foreach (Molecule mol in mols)
            {
                mol.UpdateVisual();
            }
        }

        /// <summary>
        /// flips a molecule along an axis
        /// </summary>
        /// <param name="selMolecule">Molecule to flip</param>
        /// <param name="flipVertically">true to flip vertically, otherwise flips horizontally</param>
        /// <param name="flipStereo">If true, will preserve the stereochemistry of the molecule</param>
        public void FlipMolecule(Molecule selMolecule, bool flipVertically, bool flipStereo)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                WriteTelemetry(module, "Debug", "Called");

                int scaleX = 1;
                int scaleY = 1;

                if (flipVertically)
                {
                    scaleY = -1;
                }
                else
                {
                    scaleX = -1;
                }

                Rect bb = selMolecule.BoundingBox;

                double cx = bb.Left + (bb.Right - bb.Left) / 2;
                double cy = bb.Top + (bb.Bottom - bb.Top) / 2;

                ScaleTransform flipTransform = new ScaleTransform(scaleX, scaleY, cx, cy);

                Action undo = () =>
                              {
                                  Transform(selMolecule, flipTransform);

                                  InvertPlacements(selMolecule);
                                  selMolecule.UpdateVisual();
                                  if (flipStereo)
                                  {
                                      InvertStereo(selMolecule);
                                  }
                              };

                Action redo = () =>
                              {
                                  Transform(selMolecule, flipTransform);

                                  InvertPlacements(selMolecule);
                                  selMolecule.UpdateVisual();
                                  if (flipStereo)
                                  {
                                      InvertStereo(selMolecule);
                                  }
                              };

                UndoManager.BeginUndoBlock();
                UndoManager.RecordAction(undo, redo, flipVertically ? "Flip Vertical" : "Flip Horizontal");
                UndoManager.EndUndoBlock();
                redo();

                //local function
                void InvertStereo(Molecule m)
                {
                    foreach (Bond bond in m.Bonds)
                    {
                        if (bond.Stereo == BondStereo.Wedge)
                        {
                            bond.Stereo = BondStereo.Hatch;
                        }
                        else if (bond.Stereo == BondStereo.Hatch)
                        {
                            bond.Stereo = BondStereo.Wedge;
                        }
                    }
                }

                //local function
                void InvertPlacements(Molecule m)
                {
                    IEnumerable<Bond> ringBonds = from b in m.Bonds
                                                  where b.Rings.Any()
                                                        && b.OrderValue <= 2.5
                                                        && b.OrderValue >= 1.5
                                                  select b;
                    foreach (Bond ringBond in ringBonds)
                    {
                        if (ringBond.ExplicitPlacement != null)
                        {
                            ringBond.ExplicitPlacement = (BondDirection)(-(int)ringBond.ExplicitPlacement);
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                WriteTelemetryException(module, exception);
            }
        }

        /// <summary>
        /// Joins two molecules by creating a bond between the specified atoms
        /// </summary>
        /// <param name="a">First atom to join</param>
        /// <param name="b">Second atom to join</param>
        /// <param name="currentOrder">Current selected bond order</param>
        /// <param name="currentStereo">Current selected bond stereo</param>
        public void JoinMolecules(Atom a, Atom b, string currentOrder, BondStereo currentStereo)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                string startAtomInfo = "{null}";
                if (a != null)
                {
                    if (a.Element == null)
                    {
                        startAtomInfo = $"Null @ {PointHelper.AsString(a.Position)}";
                    }
                    else
                    {
                        startAtomInfo = $"{a.SymbolText} @ {PointHelper.AsString(a.Position)}";
                    }
                }

                string endAtomInfo = "{null}";
                if (b != null)
                {
                    if (b.Element == null)
                    {
                        endAtomInfo = $"Null @ {PointHelper.AsString(b.Position)}";
                    }
                    else
                    {
                        endAtomInfo = $"{b.SymbolText} @ {PointHelper.AsString(b.Position)}";
                    }
                }

                string orderInfo = currentOrder ?? CurrentBondOrder;
                WriteTelemetry(module, "Debug",
                               $"StartAtom: {startAtomInfo}; EndAtom: {endAtomInfo}; BondOrder; {orderInfo}");

                Molecule molA = a.Parent;
                Molecule molB = b.Parent;
                Molecule newMol = null;
                IChemistryContainer parent = molA.Parent;

                Action redo = () =>
                              {
                                  Bond bond = new Bond(a, b) { Order = currentOrder, Stereo = currentStereo };
                                  newMol = Molecule.Join(molA, molB, bond);
                                  newMol.Parent = parent;
                                  parent.AddMolecule(newMol);
                                  parent.RemoveMolecule(molA);
                                  molA.Parent = null;
                                  parent.RemoveMolecule(molB);
                                  molB.Parent = null;
                                  newMol.Model.Relabel(false);
                                  newMol.UpdateVisual();
                              };

                Action undo = () =>
                              {
                                  molA.Parent = parent;
                                  molA.Reparent();
                                  parent.AddMolecule(molA);
                                  molB.Parent = parent;
                                  molB.Reparent();
                                  parent.AddMolecule(molB);
                                  parent.RemoveMolecule(newMol);
                                  newMol.Parent = null;

                                  molA.UpdateVisual();
                                  molB.UpdateVisual();
                              };

                UndoManager.BeginUndoBlock();
                UndoManager.RecordAction(undo, redo);
                UndoManager.EndUndoBlock();
                redo();
            }
            catch (Exception exception)
            {
                WriteTelemetryException(module, exception);
            }

            CheckModelIntegrity(module);
        }

        /// <summary>
        /// copies properties from one molecule to another with undo/redo support
        /// Used by the Molecule Properties dialog: allows easy setting of multiple properties
        /// </summary>
        /// <param name="target"></param>
        /// <param name="source"></param>
        public void UpdateMolecule(Molecule target, Molecule source)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                WriteTelemetry(module, "Debug", "Called");

                bool? showBefore = target.ShowMoleculeBrackets;
                bool? showCarbonBefore = target.ExplicitC;
                HydrogenLabels? hydrogenLabelsBefore = target.ExplicitH;
                int? chargeBefore = target.FormalCharge;
                int? countBefore = target.Count;
                int? spinBefore = target.SpinMultiplicity;

                bool? showAfter = source.ShowMoleculeBrackets;
                bool? showCarbonAfter = source.ExplicitC;
                HydrogenLabels? hydrogenLabelsAfter = source.ExplicitH;
                int? chargeAfter = source.FormalCharge;
                int? countAfter = source.Count;
                int? spinAfter = source.SpinMultiplicity;

                //caches the properties for undo/redo
                Dictionary<string, MoleculePropertyBag> sourceProps = new Dictionary<string, MoleculePropertyBag>();

                Action redo = () =>
                              {
                                  target.ShowMoleculeBrackets = showAfter;
                                  target.ExplicitC = showCarbonAfter;
                                  target.ExplicitH = hydrogenLabelsAfter;
                                  target.FormalCharge = chargeAfter;
                                  target.Count = countAfter;
                                  target.SpinMultiplicity = spinAfter;
                                  target.UpdateVisual();

                                  StashProperties(source, sourceProps);
                                  UnstashProperties(target, sourceProps);
                              };

                Action undo = () =>
                              {
                                  target.ShowMoleculeBrackets = showBefore;
                                  target.ExplicitC = showCarbonBefore;
                                  target.ExplicitH = hydrogenLabelsBefore;
                                  target.FormalCharge = chargeBefore;
                                  target.Count = countBefore;
                                  target.SpinMultiplicity = spinBefore;
                                  target.UpdateVisual();

                                  UnstashProperties(target, sourceProps);
                              };

                UndoManager.BeginUndoBlock();
                UndoManager.RecordAction(undo, redo);
                UndoManager.EndUndoBlock();
                redo();

                //local function
                void StashProperties(Molecule mol, Dictionary<string, MoleculePropertyBag> propertyBags)
                {
                    MoleculePropertyBag bag = new MoleculePropertyBag();
                    bag.Store(mol);
                    propertyBags[mol.Id] = bag;
                    foreach (Molecule child in mol.Molecules.Values)
                    {
                        StashProperties(child, propertyBags);
                    }
                }

                //local function
                void UnstashProperties(Molecule mol, Dictionary<string, MoleculePropertyBag> propertyBags)
                {
                    MoleculePropertyBag bag = propertyBags[mol.Id];
                    bag.Restore(mol);
                    foreach (Molecule child in mol.Molecules.Values)
                    {
                        UnstashProperties(child, propertyBags);
                    }
                }
            }
            catch (Exception exception)
            {
                WriteTelemetryException(module, exception);
            }
        }

        #endregion Methods

        #region Events

        /// <summary>
        /// End of drag event for a molecule adorner.
        /// Handled in the controller as the adorner
        /// needs to be removed from the adorner layer
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnDragCompleted_MolAdorner(object sender, DragCompletedEventArgs e)
        {
            //we've completed the drag operation
            //remove the existing molecule adorner
            MoleculeSelectionAdorner moleculeSelectionAdorner = ((MoleculeSelectionAdorner)sender);

            foreach (Molecule mol in moleculeSelectionAdorner.AdornedMolecules)
            {
                RemoveFromSelection(mol);
                //and add in a new one
                AddToSelection(mol);
            }
        }

        #endregion Events

        #region Methods

        private void DeleteMolecules(IEnumerable<Molecule> mols)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                WriteTelemetry(module, "Debug", "Called");

                UndoManager.BeginUndoBlock();

                foreach (Molecule mol in mols)
                {
                    DeleteMolecule(mol);
                }

                UndoManager.EndUndoBlock();
            }
            catch (Exception exception)
            {
                WriteTelemetryException(module, exception);
            }
        }

        #endregion Methods
    }
}
