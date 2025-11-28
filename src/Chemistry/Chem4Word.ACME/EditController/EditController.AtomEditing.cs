// ---------------------------------------------------------------------------
//  Copyright (c) 2026, The .NET Foundation.
//  This software is released under the Apache Licence, Version 2.0.
//  The licence and further copyright text can be found in the file LICENCE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.ACME.Adorners.Selectors;
using Chem4Word.ACME.Commands.Editing;
using Chem4Word.ACME.Commands.Sketching;
using Chem4Word.ACME.Models;
using Chem4Word.Core.Enums;
using Chem4Word.Model2;
using Chem4Word.Model2.Enums;
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

        private AddAtomCommand _addAtomCommand;
        private PickElementCommand _pickElementCommand;

        #endregion Fields

        #region Properties

        public PickElementCommand PickElementCommand
        {
            get
            {
                return _pickElementCommand;
            }
            set
            {
                _pickElementCommand = value;
                OnPropertyChanged();
            }
        }

        #endregion Properties

        #region Methods

        /// <summary>
        /// Converts all implicit hydrogens on atoms of selected molecules (or all atoms if nothing is selected) to explicit hydrogens
        /// </summary>
        public void AddHydrogens()
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            double newBondLength = CurrentBondLength * ModelConstants.ScaleFactorForXaml;
            try
            {
                WriteTelemetry(module, "Debug", "Called");

                List<Atom> targetAtoms = new List<Atom>();
                List<Molecule> mols = SelectedItems.OfType<Molecule>().ToList();
                if (mols.Any())
                {
                    foreach (Molecule mol in mols)
                    {
                        foreach (Atom atom in mol.Atoms.Values)
                        {
                            if (atom.ImplicitHydrogenCount > 0)
                            {
                                targetAtoms.Add(atom);
                            }
                        }
                    }
                }
                else
                {
                    foreach (Atom atom in Model.GetAllAtoms())
                    {
                        if (atom.ImplicitHydrogenCount > 0)
                        {
                            targetAtoms.Add(atom);
                        }
                    }
                }

                if (targetAtoms.Any())
                {
                    List<Atom> newAtoms = new List<Atom>();
                    List<Bond> newBonds = new List<Bond>();
                    Dictionary<Guid, Molecule> parents = new Dictionary<Guid, Molecule>();
                    foreach (Atom atom in targetAtoms)
                    {
                        double separation = 90.0;
                        if (atom.Bonds.Count() > 1)
                        {
                            separation = 30.0;
                        }

                        int hydrogenCount = atom.ImplicitHydrogenCount;
                        Vector vector = atom.BalancingVector();

                        switch (hydrogenCount)
                        {
                            case 1:
                                // Use balancing vector as is
                                break;

                            case 2:
                                Matrix matrix1 = new Matrix();
                                matrix1.Rotate(-separation / 2);
                                vector *= matrix1;
                                break;

                            case 3:
                                Matrix matrix2 = new Matrix();
                                matrix2.Rotate(-separation);
                                vector *= matrix2;
                                break;

                            case 4:
                                // Use default balancing vector (Screen.North) as is
                                break;
                        }

                        Matrix matrix3 = new Matrix();
                        matrix3.Rotate(separation);

                        for (int i = 0; i < hydrogenCount; i++)
                        {
                            if (i > 0)
                            {
                                vector *= matrix3;
                            }

                            Atom aa = new Atom
                            {
                                Element = ModelGlobals.PeriodicTable.H,
                                Position = atom.Position +
                                                     vector * (newBondLength *
                                                               AcmeConstants.ExplicitHydrogenBondPercentage)
                            };
                            newAtoms.Add(aa);
                            if (!parents.ContainsKey(aa.InternalId))
                            {
                                parents.Add(aa.InternalId, atom.Parent);
                            }

                            Bond bb = new Bond
                            {
                                StartAtomInternalId = atom.InternalId,
                                EndAtomInternalId = aa.InternalId,
                                Stereo = BondStereo.None,
                                Order = "S"
                            };
                            newBonds.Add(bb);
                            if (!parents.ContainsKey(bb.InternalId))
                            {
                                parents.Add(bb.InternalId, atom.Parent);
                            }
                        }
                    }

                    Action undoAction = () =>
                                        {
                                            foreach (Bond bond in newBonds)
                                            {
                                                bond.Parent.RemoveBond(bond);
                                            }

                                            foreach (Atom atom in newAtoms)
                                            {
                                                atom.Parent.RemoveAtom(atom);
                                            }

                                            if (mols.Any())
                                            {
                                                RefreshMolecules(mols);
                                            }
                                            else
                                            {
                                                RefreshMolecules(Model.Molecules.Values.ToList());
                                            }

                                            ClearSelection();
                                        };

                    Action redoAction = () =>
                                        {
                                            Model.InhibitEvents = true;

                                            foreach (Atom atom in newAtoms)
                                            {
                                                parents[atom.InternalId].AddAtom(atom);
                                                atom.Parent = parents[atom.InternalId];
                                            }

                                            foreach (Bond bond in newBonds)
                                            {
                                                parents[bond.InternalId].AddBond(bond);
                                                bond.Parent = parents[bond.InternalId];
                                            }

                                            Model.InhibitEvents = false;

                                            if (mols.Any())
                                            {
                                                RefreshMolecules(mols);
                                            }
                                            else
                                            {
                                                RefreshMolecules(Model.Molecules.Values.ToList());
                                            }

                                            ClearSelection();
                                        };

                    UndoManager.BeginUndoBlock();
                    UndoManager.RecordAction(undoAction, redoAction);
                    UndoManager.EndUndoBlock();
                    redoAction();
                }
            }
            catch (Exception exception)
            {
                WriteTelemetryException(module, exception);
            }

            CheckModelIntegrity(module);
        }

        /// <summary>
        /// Strips all explicit hydrogens from selected atoms (or all atoms if nothing is selected)
        /// </summary>
        public void RemoveHydrogens()
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                WriteTelemetry(module, "Debug", "Called");

                HydrogenTargets targets;
                List<Molecule> molecules = SelectedItems.OfType<Molecule>().ToList();
                if (molecules.Any())
                {
                    targets = Model.GetHydrogenTargets(molecules);
                }
                else
                {
                    targets = Model.GetHydrogenTargets();
                }

                if (targets.Atoms.Any())
                {
                    Action undoAction = () =>
                                        {
                                            Model.InhibitEvents = true;

                                            foreach (Atom atom in targets.Atoms)
                                            {
                                                targets.Molecules[atom.InternalId].AddAtom(atom);
                                                atom.Parent = targets.Molecules[atom.InternalId];
                                            }

                                            foreach (Bond bond in targets.Bonds)
                                            {
                                                targets.Molecules[bond.InternalId].AddBond(bond);
                                                bond.Parent = targets.Molecules[bond.InternalId];
                                            }

                                            Model.InhibitEvents = false;

                                            if (molecules.Any())
                                            {
                                                RefreshMolecules(molecules);
                                            }
                                            else
                                            {
                                                RefreshMolecules(Model.Molecules.Values.ToList());
                                            }

                                            ClearSelection();
                                        };

                    Action redoAction = () =>
                                        {
                                            foreach (Bond bond in targets.Bonds)
                                            {
                                                bond.Parent.RemoveBond(bond);
                                            }

                                            foreach (Atom atom in targets.Atoms)
                                            {
                                                atom.Parent.RemoveAtom(atom);
                                            }

                                            if (molecules.Any())
                                            {
                                                RefreshMolecules(molecules);
                                            }
                                            else
                                            {
                                                RefreshMolecules(Model.Molecules.Values.ToList());
                                            }

                                            ClearSelection();
                                        };

                    UndoManager.BeginUndoBlock();
                    UndoManager.RecordAction(undoAction, redoAction);
                    UndoManager.EndUndoBlock();
                    redoAction();
                }
            }
            catch (Exception exception)
            {
                WriteTelemetryException(module, exception);
            }

            CheckModelIntegrity(module);
        }

        public void SetElement(ElementBase value, List<Atom> selAtoms)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                string affectedAtoms = selAtoms == null ? "{null}" : selAtoms.Count.ToString();
                string countString = selAtoms == null ? "{null}" : $"{selAtoms.Count}";
                WriteTelemetry(module, "Debug",
                               $"Atoms: {countString}; Symbol: {value?.Symbol ?? "{null}"}; Affected Atoms {affectedAtoms}");

                if (selAtoms != null && selAtoms.Any())
                {
                    UndoManager.BeginUndoBlock();

                    foreach (Atom selectedAtom in selAtoms)
                    {
                        Atom lastAtom = selectedAtom;
                        if (lastAtom.Element != value)
                        {
                            int? currentIsotope = lastAtom.IsotopeNumber;
                            ElementBase lastElement = lastAtom.Element;

                            Action redo = () =>
                                          {
                                              lastAtom.Element = value;
                                              lastAtom.IsotopeNumber = null;
                                              lastAtom.UpdateVisual();
                                          };

                            Action undo = () =>
                                          {
                                              lastAtom.Element = lastElement;
                                              lastAtom.IsotopeNumber = currentIsotope;
                                              lastAtom.UpdateVisual();
                                          };

                            UndoManager.RecordAction(undo, redo, $"Set Element to {value?.Symbol ?? "null"}");
                            redo();

                            ClearSelection();

                            foreach (Bond bond in lastAtom.Bonds)
                            {
                                bond.UpdateVisual();
                            }
                        }
                    }

                    UndoManager.EndUndoBlock();
                }
            }
            catch (Exception exception)
            {
                WriteTelemetryException(module, exception);
            }

            CheckModelIntegrity(module);
        }

        public void SetExplicitHPlacement(Atom selAtom, CompassPoints? newPlacement)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            WriteTelemetry(module, "Debug", $"{selAtom}; Placement {newPlacement}");
            CompassPoints oldPlacement = selAtom.ImplicitHPlacement;

            Action undo = () =>
                          {
                              selAtom.ExplicitHPlacement = oldPlacement;
                              selAtom.UpdateVisual();
                              foreach (Bond selBond in selAtom.Bonds)
                              {
                                  selBond.UpdateVisual();
                              }
                          };
            Action redo = () =>
                          {
                              ClearSelection();
                              selAtom.ExplicitHPlacement = newPlacement;
                              selAtom.UpdateVisual();
                              foreach (Bond selBond in selAtom.Bonds)
                              {
                                  selBond.UpdateVisual();
                              }
                          };

            UndoManager.BeginUndoBlock();
            UndoManager.RecordAction(undo, redo);
            UndoManager.EndUndoBlock();
            redo();
        }

        /// <summary>
        /// Deletes a list of atoms.  If any are not singletons, then calls DeleteAtomsAndBonds()
        /// </summary>
        /// <param name="atoms"></param>
        public void DeleteAtoms(IEnumerable<Atom> atoms)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                string countString = atoms == null ? "{null}" : $"{atoms.Count()}";
                WriteTelemetry(module, "Debug", $"Atoms: {countString}");

                Atom[] atomList = atoms.ToArray();
                //Add all the selected atoms to a set A
                if (atomList.Length == 1 && atomList[0].Singleton)
                {
                    Atom delAtom = atomList[0];
                    Molecule molecule = delAtom.Parent;
                    Model model = molecule.Model;

                    Action redo = () =>
                                  {
                                      model.RemoveMolecule(molecule);
                                  };
                    Action undo = () =>
                                  {
                                      model.AddMolecule(molecule);
                                  };

                    UndoManager.BeginUndoBlock();
                    UndoManager.RecordAction(undo, redo, $"{nameof(DeleteAtoms)}[Singleton]");
                    UndoManager.EndUndoBlock();
                    redo();
                }
                else
                {
                    DeleteAtomsAndBonds(atomList);
                }
            }
            catch (Exception exception)
            {
                WriteTelemetryException(module, exception);
            }

            CheckModelIntegrity(module);
        }

        /// <summary>
        /// Deletes a list of atoms and bonds, splitting them into separate molecules if required
        /// </summary>
        /// <param name="atomlist"></param>
        /// <param name="bondList"></param>
        ///
        public void DeleteAtomsAndBonds(IEnumerable<Atom> atomlist = null, IEnumerable<Bond> bondList = null)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                string count1 = atomlist == null ? "{null}" : $"{atomlist.Count()}";
                string count2 = bondList == null ? "{null}" : $"{bondList.Count()}";

                WriteTelemetry(module, "Debug", $"Atoms: {count1}; Bonds: {count2}");

                HashSet<Atom> deleteAtoms = new HashSet<Atom>();
                HashSet<Bond> deleteBonds = new HashSet<Bond>();
                HashSet<Atom> neighbours = new HashSet<Atom>();

                if (atomlist != null)
                {
                    //Add all the selected atoms to a set A
                    foreach (Atom atom in atomlist)
                    {
                        deleteAtoms.Add(atom);

                        foreach (Bond bond in atom.Bonds)
                        {
                            //Add all the selected atoms' bonds to B
                            deleteBonds.Add(bond);
                            //Add start and end atoms B1s and B1E to neighbours
                            neighbours.Add(bond.StartAtom);
                            neighbours.Add(bond.EndAtom);
                        }
                    }
                }

                if (bondList != null)
                {
                    foreach (Bond bond in bondList)
                    {
                        //Add all the selected bonds to deleteBonds
                        deleteBonds.Add(bond);
                        //Add start and end atoms B1s and B1E to neighbours
                        neighbours.Add(bond.StartAtom);
                        neighbours.Add(bond.EndAtom);
                    }
                }

                //ignore the atoms we are going to delete anyway
                neighbours.ExceptWith(deleteAtoms);
                HashSet<Atom> updateAtoms = new HashSet<Atom>(neighbours);

                List<HashSet<Atom>> atomGroups = new List<HashSet<Atom>>();
                Molecule molecule = null;

                //now, take groups of connected atoms from the remaining graph ignoring the excluded bonds
                while (neighbours.Count > 0)
                {
                    HashSet<Atom> atomGroup = new HashSet<Atom>();

                    Atom firstAtom = neighbours.First();
                    molecule = firstAtom.Parent;
                    molecule.TraverseBFS(firstAtom, a1 => { atomGroup.Add(a1); }, a2 => !atomGroup.Contains(a2),
                                    deleteBonds);
                    atomGroups.Add(atomGroup);
                    //remove the list of atoms from the atom group
                    neighbours.ExceptWith(atomGroup);
                }

                //now, check to see whether there is a single atomgroup.  If so, then we still have one molecule
                if (atomGroups.Count == 1)
                {
                    MoleculePropertyBag moleculePropertyBag = new MoleculePropertyBag();
                    moleculePropertyBag.Store(molecule);

                    Dictionary<Atom, bool?> explicitFlags = new Dictionary<Atom, bool?>();
                    foreach (Bond deleteBond in deleteBonds)
                    {
                        if (!explicitFlags.ContainsKey(deleteBond.StartAtom))
                        {
                            explicitFlags[deleteBond.StartAtom] = deleteBond.StartAtom.ExplicitC;
                        }

                        if (!explicitFlags.ContainsKey(deleteBond.EndAtom))
                        {
                            explicitFlags[deleteBond.EndAtom] = deleteBond.EndAtom.ExplicitC;
                        }
                    }

                    Action redo = () =>
                                  {
                                      ClearSelection();
                                      int theoreticalRings = molecule.TheoreticalRings;
                                      foreach (Bond deleteBond in deleteBonds)
                                      {
                                          molecule.RemoveBond(deleteBond);
                                          RefreshRingBonds(theoreticalRings, molecule, deleteBond);

                                          deleteBond.StartAtom.ExplicitC = null;
                                          deleteBond.StartAtom.UpdateVisual();

                                          deleteBond.EndAtom.ExplicitC = null;
                                          deleteBond.EndAtom.UpdateVisual();

                                          foreach (Bond atomBond in deleteBond.StartAtom.Bonds)
                                          {
                                              atomBond.UpdateVisual();
                                          }

                                          foreach (Bond atomBond in deleteBond.EndAtom.Bonds)
                                          {
                                              atomBond.UpdateVisual();
                                          }
                                      }

                                      foreach (Atom deleteAtom in deleteAtoms)
                                      {
                                          molecule.RemoveAtom(deleteAtom);
                                      }

                                      molecule.ClearProperties();
                                      RefreshAtomVisuals(updateAtoms);
                                  };

                    Action undo = () =>
                                  {
                                      ClearSelection();
                                      foreach (Atom restoreAtom in deleteAtoms)
                                      {
                                          molecule.AddAtom(restoreAtom);
                                          restoreAtom.UpdateVisual();
                                          AddToSelection(restoreAtom);
                                      }

                                      foreach (Bond restoreBond in deleteBonds)
                                      {
                                          int theoreticalRings = molecule.TheoreticalRings;
                                          molecule.AddBond(restoreBond);

                                          restoreBond.StartAtom.ExplicitC = explicitFlags[restoreBond.StartAtom];
                                          restoreBond.StartAtom.UpdateVisual();
                                          restoreBond.EndAtom.ExplicitC = explicitFlags[restoreBond.EndAtom];
                                          restoreBond.EndAtom.UpdateVisual();

                                          RefreshRingBonds(theoreticalRings, molecule, restoreBond);

                                          foreach (Bond atomBond in restoreBond.StartAtom.Bonds)
                                          {
                                              atomBond.UpdateVisual();
                                          }

                                          foreach (Bond atomBond in restoreBond.EndAtom.Bonds)
                                          {
                                              atomBond.UpdateVisual();
                                          }

                                          restoreBond.UpdateVisual();

                                          AddToSelection(restoreBond);
                                      }

                                      moleculePropertyBag.Restore(molecule);
                                  };

                    UndoManager.BeginUndoBlock();
                    UndoManager.RecordAction(undo, redo, $"{nameof(DeleteAtomsAndBonds)}[SingleAtom]");
                    UndoManager.EndUndoBlock();
                    redo();
                }
                else //we have multiple fragments
                {
                    List<Molecule> newMolecules = new List<Molecule>();
                    List<Molecule> oldMolecules = new List<Molecule>();

                    Dictionary<Atom, bool?> explicitFlags = new Dictionary<Atom, bool?>();

                    //add all the relevant atoms and bonds to a new molecule
                    //grab the model for future reference
                    Model parentModel = null;
                    foreach (HashSet<Atom> atomGroup in atomGroups)
                    {
                        //assume that all atoms share the same parent model & molecule
                        Molecule parent = atomGroup.First().Parent;
                        if (parentModel == null)
                        {
                            parentModel = parent.Model;
                        }

                        if (!oldMolecules.Contains(parent))
                        {
                            oldMolecules.Add(parent);
                        }

                        Molecule newMolecule = new Molecule();

                        foreach (Atom atom in atomGroup)
                        {
                            newMolecule.AddAtom(atom);
                            IEnumerable<Bond> bondsToAdd = from Bond bond in atom.Bonds
                                                           where !newMolecule.Bonds.Contains(bond) && !deleteBonds.Contains(bond)
                                                           select bond;
                            foreach (Bond bond in bondsToAdd)
                            {
                                newMolecule.AddBond(bond);
                            }
                        }

                        newMolecule.Parent = parentModel;
                        newMolecule.Reparent();
                        newMolecules.Add(newMolecule);
                        newMolecule.RebuildRings();

                        // Clear explicit flag on a lone atom
                        if (newMolecule.AtomCount == 1)
                        {
                            Atom loneAtom = newMolecule.Atoms.Values.First();
                            explicitFlags[loneAtom] = loneAtom.ExplicitC;
                            loneAtom.ExplicitC = null;
                        }

                        //add the molecule to the model
                        parentModel.AddMolecule(newMolecule);
                    }

                    foreach (Molecule oldMolecule in oldMolecules)
                    {
                        parentModel.RemoveMolecule(oldMolecule);
                        oldMolecule.Parent = null;
                    }

                    //refresh the neighbouring atoms
                    RefreshAtomVisuals(updateAtoms);

                    Action undo = () =>
                                  {
                                      ClearSelection();
                                      foreach (Molecule oldMol in oldMolecules)
                                      {
                                          oldMol.Reparent();
                                          oldMol.Parent = parentModel;

                                          foreach (Atom atom in oldMol.Atoms.Values)
                                          {
                                              if (explicitFlags.ContainsKey(atom))
                                              {
                                                  atom.ExplicitC = explicitFlags[atom];
                                              }
                                          }

                                          parentModel.AddMolecule(oldMol);

                                          oldMol.UpdateVisual();
                                      }

                                      foreach (Molecule newMol in newMolecules)
                                      {
                                          parentModel.RemoveMolecule(newMol);
                                          newMol.Parent = null;
                                      }

                                      RefreshAtomVisuals(updateAtoms);
                                  };

                    Action redo = () =>
                                  {
                                      ClearSelection();
                                      foreach (Molecule newmol in newMolecules)
                                      {
                                          newmol.Reparent();
                                          newmol.Parent = parentModel;

                                          if (newmol.AtomCount == 1)
                                          {
                                              newmol.Atoms.Values.First().ExplicitC = null;
                                          }

                                          parentModel.AddMolecule(newmol);
                                          newmol.UpdateVisual();
                                      }

                                      foreach (Molecule oldMol in oldMolecules)
                                      {
                                          parentModel.RemoveMolecule(oldMol);
                                          oldMol.Parent = null;
                                      }

                                      RefreshAtomVisuals(updateAtoms);
                                  };

                    UndoManager.BeginUndoBlock();
                    UndoManager.RecordAction(undo, redo, $"{nameof(DeleteAtomsAndBonds)}[MultipleFragments]");
                    UndoManager.EndUndoBlock();
                }
            }
            catch (Exception exception)
            {
                WriteTelemetryException(module, exception);
            }

            CheckModelIntegrity(module);

            SendStatus(("Object(s) deleted", TotUpMolFormulae(), TotUpSelectedMwt()));

            // Local Function
            void RefreshRingBonds(int theoreticalRings, Molecule molecule, Bond deleteBond)
            {
                if (theoreticalRings != molecule.TheoreticalRings)
                {
                    molecule.RebuildRings();
                    foreach (Ring bondRing in deleteBond.Rings)
                    {
                        foreach (Bond bond in bondRing.Bonds)
                        {
                            bond.UpdateVisual();
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Updates an atom's properties from an AtomPropertiesModel
        /// </summary>
        /// <param name="atom"></param>
        /// <param name="atomPropertiesModel"></param>
        public void UpdateAtom(Atom atom, AtomPropertiesModel atomPropertiesModel)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                WriteTelemetry(module, "Debug", "Called");

                ElementBase elementBaseBefore = atom.Element;
                int? chargeBefore = atom.FormalCharge;
                CompassPoints? explicitFGPlacementBefore = atom.ExplicitFunctionalGroupPlacement;
                int? isotopeBefore = atom.IsotopeNumber;
                bool? explicitCBefore = atom.ExplicitC;
                HydrogenLabels? explicitHBefore = atom.ExplicitH;

                CompassPoints? hydrogenPlacementBefore = atom.ExplicitHPlacement;

                ElementBase elementBaseAfter = atomPropertiesModel.Element;
                int? chargeAfter = null;
                int? isotopeAfter = null;
                bool? explicitCAfter = null;
                HydrogenLabels? explictHAfter = null;

                CompassPoints? hydrogenPlacementAfter = null;
                CompassPoints? explicitFGPlacementAfter = null;
                if (elementBaseAfter is FunctionalGroup)
                {
                    explicitFGPlacementAfter = atomPropertiesModel.ExplicitFunctionalGroupPlacement;
                }
                else if (elementBaseAfter is Element)
                {
                    chargeAfter = atomPropertiesModel.Charge;
                    explicitCAfter = atomPropertiesModel.ExplicitC;
                    explictHAfter = atomPropertiesModel.ExplicitH;
                    hydrogenPlacementAfter = atomPropertiesModel.ExplicitHydrogenPlacement;
                    if (!string.IsNullOrEmpty(atomPropertiesModel.Isotope))
                    {
                        isotopeAfter = int.Parse(atomPropertiesModel.Isotope);
                    }
                }

                Action redo = () =>
                              {
                                  atom.Element = elementBaseAfter;
                                  atom.FormalCharge = chargeAfter;
                                  atom.IsotopeNumber = isotopeAfter;
                                  atom.ExplicitC = explicitCAfter;
                                  atom.ExplicitH = explictHAfter;
                                  atom.ExplicitHPlacement = hydrogenPlacementAfter;
                                  atom.ExplicitFunctionalGroupPlacement = explicitFGPlacementAfter;
                                  atom.Parent.UpdateVisual();
                                  //freshen any selection adorner
                                  if (SelectedItems.Contains(atom))
                                  {
                                      RemoveFromSelection(atom);
                                      AddToSelection(atom);
                                  }
                              };

                Action undo = () =>
                              {
                                  atom.Element = elementBaseBefore;
                                  atom.FormalCharge = chargeBefore;
                                  atom.IsotopeNumber = isotopeBefore;
                                  atom.ExplicitC = explicitCBefore;
                                  atom.ExplicitH = explicitHBefore;
                                  atom.ExplicitHPlacement = hydrogenPlacementBefore;
                                  atom.ExplicitFunctionalGroupPlacement = explicitFGPlacementBefore;
                                  atom.Parent.UpdateVisual();
                                  //freshen any selection adorner
                                  if (SelectedItems.Contains(atom))
                                  {
                                      RemoveFromSelection(atom);
                                      AddToSelection(atom);
                                  }
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

        #endregion Methods

        #region Events

        /// <summary>
        /// End of drag event for an atom adorner.
        /// Handled in the controller as the adorner
        /// needs to be removed from the adorner layer
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnDragCompleted_AtomAdorner(object sender, DragCompletedEventArgs e)
        {
            //we've completed the drag operation
            //remove the existing molecule adorner
            SingleObjectSelectionAdorner moleculeSelectionAdorner = ((SingleObjectSelectionAdorner)sender);
            foreach (Molecule mol in moleculeSelectionAdorner.AdornedMolecules)
            {
                RemoveFromSelection(mol);
                //and add in a new one
                AddToSelection(mol);
            }
        }

        #endregion Events
    }
}
