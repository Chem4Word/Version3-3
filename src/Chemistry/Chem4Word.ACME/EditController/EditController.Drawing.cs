// ---------------------------------------------------------------------------
//  Copyright (c) 2025, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.ACME.Commands.Layout.Alignment;
using Chem4Word.ACME.Commands.Sketching;
using Chem4Word.ACME.Models;
using Chem4Word.Core.Enums;
using Chem4Word.Core.Helpers;
using Chem4Word.Model2;
using Chem4Word.Model2.Converters.CML;
using Chem4Word.Model2.Enums;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Xml.Linq;
using System.Xml.XPath;

namespace Chem4Word.ACME
{
    public partial class EditController
    {
        #region properties

        public AddHydrogensCommand AddHydrogensCommand
        {
            get
            {
                return _addHydrogensCommand;
            }
            set
            {
                _addHydrogensCommand = value;
                OnPropertyChanged();
            }
        }

        public RemoveHydrogensCommand RemoveHydrogensCommand
        {
            get
            {
                return _removeHydrogensCommand;
            }
            set
            {
                _removeHydrogensCommand = value;
                OnPropertyChanged();
            }
        }

        public AlignBottomsCommand AlignBottomsCommand
        {
            get
            {
                return _alignBottomsCommand;
            }
            set
            {
                _alignBottomsCommand = value;
                OnPropertyChanged();
            }
        }

        public AlignMiddlesCommand AlignMiddlesCommand
        {
            get
            {
                return _alignMiddlesCommand;
            }
            set
            {
                _alignMiddlesCommand = value;
                OnPropertyChanged();
            }
        }

        public AlignTopsCommand AlignTopsCommand
        {
            get
            {
                return _alignTopsCommand;
            }
            set
            {
                _alignTopsCommand = value;
                OnPropertyChanged();
            }
        }

        public AlignLeftsCommand AlignLeftsCommand
        {
            get
            {
                return _alignLeftsCommand;
            }
            set
            {
                _alignLeftsCommand = value;
                OnPropertyChanged();
            }
        }

        public AlignCentresCommand AlignCentresCommand
        {
            get
            {
                return _alignCentresCommand;
            }
            set
            {
                _alignCentresCommand = value;
                OnPropertyChanged();
            }
        }

        public AlignRightsCommand AlignRightsCommand
        {
            get
            {
                return _alignRightsCommand;
            }
            set
            {
                _alignRightsCommand = value;
                OnPropertyChanged();
            }
        }

        #endregion properties

        #region Fields

        private AddHydrogensCommand _addHydrogensCommand;
        private RemoveHydrogensCommand _removeHydrogensCommand;
        private AlignBottomsCommand _alignBottomsCommand;
        private AlignMiddlesCommand _alignMiddlesCommand;
        private AlignTopsCommand _alignTopsCommand;
        private AlignLeftsCommand _alignLeftsCommand;
        private AlignCentresCommand _alignCentresCommand;
        private AlignRightsCommand _alignRightsCommand;

        #endregion Fields

        #region Commands

        public AddAtomCommand AddAtomCommand
        {
            get
            {
                return _addAtomCommand;
            }
            set
            {
                _addAtomCommand = value;
                OnPropertyChanged();
            }
        }

        #endregion Commands

        #region Methods

        public void AddNewBond(Atom a, Atom b, Molecule mol, string order = null, BondStereo? stereo = null)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod()?.Name}()";
            try
            {
                string startAtomInfo = "{null}";
                if (a != null)
                {
                    if (mol.Atoms.ContainsKey(a.InternalId))
                    {
                        if (a.Element == null)
                        {
                            startAtomInfo = $"Null @ {PointHelper.AsString(a.Position)}";
                        }
                        else
                        {
                            startAtomInfo = $"{a.Element.Symbol} @ {PointHelper.AsString(a.Position)}";
                        }
                    }
                    else
                    {
                        startAtomInfo = $"{a.InternalId} not found";
                    }
                }

                string endAtomInfo = "{null}";
                if (b != null)
                {
                    if (mol.Atoms.ContainsKey(b.InternalId))
                    {
                        if (b.Element == null)
                        {
                            endAtomInfo = $"Null @ {PointHelper.AsString(b.Position)}";
                        }
                        else
                        {
                            endAtomInfo = $"{b.Element.Symbol} @ {PointHelper.AsString(b.Position)}";
                        }
                    }
                    else
                    {
                        endAtomInfo = $"{b.InternalId} not found";
                    }
                }

                string orderInfo = order ?? CurrentBondOrder;
                WriteTelemetry(module, "Debug",
                               $"StartAtom: {startAtomInfo}; EndAtom: {endAtomInfo}; BondOrder; {orderInfo}");

                // Bond can be created if both atoms are children of the molecule passed in
                bool canAdd = a.Parent == mol && b.Parent == mol;
                if (!canAdd)
                {
                    WriteTelemetry(module, "Warning",
                                   $"Not adding bond to molecule because atoms don't have same parent molecule{Environment.NewLine}StartAtom: {a.Path} EndAtom: {b.Path}");
                    WriteTelemetry(module, "StackTrace", Environment.StackTrace);
                    Debugger.Break();
                }

                // Bond can be created if it's end atoms are different
                canAdd = a.InternalId != b.InternalId;
                if (!canAdd)
                {
                    WriteTelemetry(module, "Warning",
                                   $"Not adding bond to molecule because StartAtom == EndAtom{Environment.NewLine}StartAtom: {a.Path} EndAtom: {b.Path}");
                    WriteTelemetry(module, "StackTrace", Environment.StackTrace);
                    Debugger.Break();
                }

                // If we get here and canAdd is true we can add this bond
                if (canAdd)
                {
                    //keep a handle on some current properties
                    int theoreticalRings = mol.TheoreticalRings;
                    if (stereo == null)
                    {
                        stereo = CurrentStereo;
                    }

                    if (order == null)
                    {
                        order = CurrentBondOrder;
                    }

                    //stash the current molecule properties
                    MoleculePropertyBag mpb = new MoleculePropertyBag();
                    mpb.Store(mol);

                    Bond newbond = new Bond
                    {
                        Stereo = stereo.Value,
                        Order = order,
                        Parent = mol
                    };

                    Action undo = () =>
                    {
                        Atom startAtom = newbond.StartAtom;
                        Atom endAtom = newbond.EndAtom;
                        mol.RemoveBond(newbond);
                        newbond.Parent = null;
                        if (theoreticalRings != mol.TheoreticalRings)
                        {
                            mol.RebuildRings();
                            theoreticalRings = mol.TheoreticalRings;
                        }

                        RefreshAtoms(startAtom, endAtom);

                        mpb.Restore(mol);
                    };

                    Action redo = () =>
                    {
                        newbond.StartAtomInternalId = a.InternalId;
                        newbond.EndAtomInternalId = b.InternalId;
                        newbond.Parent = mol;
                        mol.AddBond(newbond);
                        if (theoreticalRings != mol.TheoreticalRings)
                        {
                            mol.RebuildRings();
                            theoreticalRings = mol.TheoreticalRings;
                        }

                        RefreshAtoms(newbond.StartAtom, newbond.EndAtom);
                        newbond.UpdateVisual();
                        mol.ClearProperties();
                    };

                    UndoManager.BeginUndoBlock();
                    UndoManager.RecordAction(undo, redo);
                    UndoManager.EndUndoBlock();
                    redo();

                    // local function
                    void RefreshAtoms(Atom startAtom, Atom endAtom)
                    {
                        startAtom.UpdateVisual();
                        endAtom.UpdateVisual();
                        foreach (Bond bond in startAtom.Bonds)
                        {
                            bond.UpdateVisual();
                        }

                        foreach (Bond bond in endAtom.Bonds)
                        {
                            bond.UpdateVisual();
                        }
                    }
                }
                else
                {
                    WriteTelemetry(module, "Warning",
                                   $"Molecule: {mol.Path}{Environment.NewLine}StartAtom: {a.Path} EndAtom: {b.Path}");
                    WriteTelemetry(module, "StackTrace", Environment.StackTrace);
                    Debugger.Break();
                }
            }
            catch (Exception exception)
            {
                WriteTelemetryException(module, exception);
            }

            CheckModelIntegrity(module);
        }

        /// <summary>
        /// Adds a new atom to an existing molecule separated by one bond
        /// </summary>
        /// <param name="lastAtom">previous atom to which the new one is bonded.  can be null</param>
        /// <param name="newAtomPos">Position of new atom</param>
        /// <param name="dir">ClockDirection in which to add the atom</param>
        /// <param name="elem">Element of atom (can be a FunctionalGroup).  defaults to current selection</param>
        /// <param name="bondOrder"></param>
        /// <param name="stereo"></param>
        /// <returns></returns>
        public Atom AddAtomChain(Atom lastAtom, Point newAtomPos, ClockDirections dir, ElementBase elem = null,
                                 string bondOrder = null, BondStereo? stereo = null)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                string atomInfo = lastAtom == null
                    ? "{null}"
                    : $"{lastAtom.Element.Symbol} @ {PointHelper.AsString(lastAtom.Position)}";
                string eleInfo = elem == null ? $"{_selectedElement.Symbol}" : $"{elem.Symbol}";
                WriteTelemetry(module, "Debug",
                               $"LastAtom: {atomInfo}; NewAtom: {eleInfo} @ {PointHelper.AsString(newAtomPos)}");

                //create the new atom based on the current selection
                Atom newAtom = new Atom { Element = elem ?? _selectedElement, Position = newAtomPos };

                //the tag stores sprout directions chosen for the chain
                object tag = null;

                if (dir != ClockDirections.Nothing)
                {
                    tag = dir;
                }

                //stash the last sprout direction
                object oldDir = lastAtom?.Tag;

                //are we drawing an isolated atom?
                if (lastAtom == null) //then it's isolated
                {
                    Molecule newMolecule = new Molecule();

                    Action undoAddNewMolecule = () =>
                    {
                        Model.RemoveMolecule(newMolecule);
                        newMolecule.Parent = null;
                    };
                    Action redoAddNewMolecule = () =>
                    {
                        newMolecule.Parent = Model;
                        Model.AddMolecule(newMolecule);
                    };

                    Action undoAddIsolatedAtom = () =>
                    {
                        newAtom.Tag = null;
                        newMolecule.RemoveAtom(newAtom);
                        newAtom.Parent = null;
                    };
                    Action redoAddIsolatedAtom = () =>
                    {
                        newAtom.Parent = newMolecule;
                        newMolecule.AddAtom(newAtom);
                        newAtom.Tag = tag;
                    };

                    UndoManager.BeginUndoBlock();
                    UndoManager.RecordAction(undoAddNewMolecule, redoAddNewMolecule,
                                             $"{nameof(AddAtomChain)}[AddMolecule]");
                    UndoManager.RecordAction(undoAddIsolatedAtom, redoAddIsolatedAtom,
                                             $"{nameof(AddAtomChain)}[AddAtom]");
                    UndoManager.EndUndoBlock();

                    redoAddNewMolecule();
                    redoAddIsolatedAtom();
                }
                else
                {
                    Molecule existingMolecule = lastAtom.Parent;
                    if (existingMolecule != null)
                    {
                        Action undoAddEndAtom = () =>
                        {
                            ClearSelection();
                            lastAtom.Tag = oldDir;
                            existingMolecule.RemoveAtom(newAtom);
                            newAtom.Parent = null;
                            lastAtom.UpdateVisual();
                        };
                        Action redoAddEndAtom = () =>
                        {
                            ClearSelection();
                            lastAtom.Tag =
                                tag; //save the last sprouted direction in the tag object
                            newAtom.Parent = existingMolecule;
                            existingMolecule.AddAtom(newAtom);
                            lastAtom.UpdateVisual();
                            newAtom.UpdateVisual();
                        };

                        UndoManager.BeginUndoBlock();
                        UndoManager.RecordAction(undoAddEndAtom, redoAddEndAtom, $"{nameof(AddAtomChain)}[AddEndAtom]");

                        // Can't put these after of the UndoManager.EndUndoBlock as they are part of the same atomic action
                        redoAddEndAtom();
                        AddNewBond(lastAtom, newAtom, existingMolecule, bondOrder, stereo);

                        UndoManager.EndUndoBlock();

                        lastAtom.UpdateVisual();
                        newAtom.UpdateVisual();
                        foreach (Bond lastAtomBond in lastAtom.Bonds)
                        {
                            lastAtomBond.UpdateVisual();
                        }
                    }
                }

                return newAtom;
            }
            catch (Exception exception)
            {
                WriteTelemetryException(module, exception);
            }

            // This is an error!
            return null;
        }

        private string ListPlacements(List<NewAtomPlacement> newAtomPlacements)
        {
            List<string> lines = new List<string>();

            int count = 0;

            foreach (NewAtomPlacement placement in newAtomPlacements)
            {
                StringBuilder line = new StringBuilder();
                line.Append($"{count++} - ");
                line.Append($"{PointHelper.AsString(placement.Position)}");

                if (placement.ExistingAtom != null)
                {
                    Atom atom = placement.ExistingAtom;
                    line.Append($" {atom.Element.Symbol} {atom.Path}");
                    if (atom.Position != placement.Position)
                    {
                        line.Append($" @ {PointHelper.AsString(atom.Position)}");
                    }
                }

                lines.Add(line.ToString());
            }

            return string.Join(Environment.NewLine, lines);
        }

        /// <summary>
        /// Draws a ring as specified by the new atom placements
        /// </summary>
        /// <param name="newAtomPlacements"></param>
        /// <param name="unsaturated"></param>
        /// <param name="startAt"></param>
        public void DrawRing(List<NewAtomPlacement> newAtomPlacements, bool unsaturated, int startAt = 0)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                string count = newAtomPlacements == null ? "{null}" : $"{newAtomPlacements.Count}";
                WriteTelemetry(module, "Debug", $"Atoms: {count}; Unsaturated: {unsaturated}; StartAt: {startAt}");
                WriteTelemetry(module, "Debug", ListPlacements(newAtomPlacements));

                UndoManager.BeginUndoBlock();

                //work around the ring adding atoms
                for (int i = 1; i <= newAtomPlacements.Count; i++)
                {
                    int currIndex = i % newAtomPlacements.Count;
                    NewAtomPlacement currentPlacement = newAtomPlacements[currIndex];
                    NewAtomPlacement previousPlacement = newAtomPlacements[i - 1];

                    Atom previousAtom = previousPlacement.ExistingAtom;
                    Atom currentAtom = currentPlacement.ExistingAtom;

                    if (currentAtom == null)
                    {
                        Atom insertedAtom = AddAtomChain(previousAtom, currentPlacement.Position,
                                                         ClockDirections.Nothing, ModelGlobals.PeriodicTable.C,
                                                         ModelConstants.OrderSingle, BondStereo.None);
                        if (insertedAtom == null)
                        {
                            WriteTelemetry(module, "Warning", "Inserted Atom is null");
                            WriteTelemetry(module, "StackTrace", Environment.StackTrace);
                            Debugger.Break();
                        }

                        currentPlacement.ExistingAtom = insertedAtom;
                    }
                    else if (previousAtom != null && previousAtom.BondBetween(currentAtom) == null)
                    {
                        AddNewBond(previousAtom, currentAtom, previousAtom.Parent, ModelConstants.OrderSingle,
                                   BondStereo.None);
                    }
                }

                //join up the ring if there is no last bond
                Atom firstAtom = newAtomPlacements[0].ExistingAtom;
                Atom nextAtom = newAtomPlacements[1].ExistingAtom;
                if (firstAtom.BondBetween(nextAtom) == null)
                {
                    AddNewBond(firstAtom, nextAtom, firstAtom.Parent, ModelConstants.OrderSingle, BondStereo.None);
                }

                //set the alternating single and double bonds if unsaturated
                if (unsaturated)
                {
                    MakeRingUnsaturated(newAtomPlacements);
                }

                firstAtom.Parent.RebuildRings();
                Action undo = () =>
                {
                    firstAtom.Parent.Refresh();
                    firstAtom.Parent.UpdateVisual();
                    ClearSelection();
                };
                Action redo = () =>
                {
                    firstAtom.Parent.Refresh();
                    firstAtom.Parent.UpdateVisual();
                };

                UndoManager.RecordAction(undo, redo);
                UndoManager.EndUndoBlock();

                //just refresh the atoms to be on the safe side
                foreach (NewAtomPlacement atomPlacement in newAtomPlacements)
                {
                    atomPlacement.ExistingAtom.UpdateVisual();
                }

                //local function
                void MakeRingUnsaturated(List<NewAtomPlacement> list)
                {
                    for (int i = startAt; i < list.Count + startAt; i++)
                    {
                        int firstIndex = i % list.Count;
                        int secondIndex = (i + 1) % list.Count;

                        Atom thisAtom = list[firstIndex].ExistingAtom;
                        Atom otherAtom = list[secondIndex].ExistingAtom;

                        if (!thisAtom.IsUnsaturated
                            && thisAtom.ImplicitHydrogenCount > 0
                            && !otherAtom.IsUnsaturated
                            && otherAtom.ImplicitHydrogenCount > 0)
                        {
                            Bond bondBetween = thisAtom.BondBetween(otherAtom);
                            if (bondBetween != null)
                            {
                                // Only do this if a bond was created / exists
                                SetBondAttributes(bondBetween, ModelConstants.OrderDouble, BondStereo.None);
                                bondBetween.ExplicitPlacement = null;
                                bondBetween.UpdateVisual();
                            }

                            thisAtom.UpdateVisual();
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                WriteTelemetryException(module, exception);
            }

            CheckModelIntegrity(module);
        }

        private string ListPoints(List<Point> placements)
        {
            List<string> lines = new List<string>();

            int count = 0;

            foreach (Point placement in placements)
            {
                StringBuilder line = new StringBuilder();
                line.Append($"{count++} - ");
                line.Append($"{PointHelper.AsString(placement)}");

                lines.Add(line.ToString());
            }

            return string.Join(Environment.NewLine, lines);
        }

        public void DrawChain(List<Point> placements, Atom startAtom = null)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                string atomInfo = startAtom == null
                    ? "{null}"
                    : $"{startAtom.SymbolText} @ {PointHelper.AsString(startAtom.Position)}";
                string count = placements == null ? "{null}" : $"{placements.Count}";
                WriteTelemetry(module, "Debug", $"Atoms: {count}; StartAtom: {atomInfo}");
                WriteTelemetry(module, "Debug", ListPoints(placements));

                UndoManager.BeginUndoBlock();
                Atom lastAtom = startAtom;
                if (startAtom == null) //we're drawing an isolated chain
                {
                    lastAtom = AddAtomChain(null, placements[0], ClockDirections.Nothing,
                                            bondOrder: ModelConstants.OrderSingle,
                                            stereo: BondStereo.None);
                    if (lastAtom == null)
                    {
                        WriteTelemetry(module, "Warning", "lastAtom is null");
                        WriteTelemetry(module, "StackTrace", Environment.StackTrace);
                        Debugger.Break();
                    }
                }

                foreach (Point placement in placements.Skip(1))
                {
                    lastAtom = AddAtomChain(lastAtom, placement, ClockDirections.Nothing,
                                            bondOrder: ModelConstants.OrderSingle,
                                            stereo: BondStereo.None);
                    if (lastAtom == null)
                    {
                        WriteTelemetry(module, "Warning", "lastAtom is null");
                        WriteTelemetry(module, "StackTrace", Environment.StackTrace);
                        Debugger.Break();
                    }

                    if (placement != placements.Last())
                    {
                        lastAtom.ExplicitC = null;
                    }
                }

                if (startAtom != null)
                {
                    startAtom.UpdateVisual();
                    foreach (Bond bond in startAtom.Bonds)
                    {
                        bond.UpdateVisual();
                    }
                }

                UndoManager.EndUndoBlock();
            }
            catch (Exception exception)
            {
                WriteTelemetryException(module, exception);
            }

            CheckModelIntegrity(module);
        }

        public void RotateHydrogen(Atom parentAtom)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                CompassPoints oldPlacement = parentAtom.ImplicitHPlacement;

                switch (oldPlacement)
                {
                    case CompassPoints.North:
                        SetExplicitHPlacement(parentAtom, CompassPoints.East);
                        break;

                    case CompassPoints.East:
                        SetExplicitHPlacement(parentAtom, CompassPoints.South);
                        break;

                    case CompassPoints.South:
                        SetExplicitHPlacement(parentAtom, CompassPoints.West);
                        break;

                    case CompassPoints.West:
                        SetExplicitHPlacement(parentAtom, CompassPoints.North);
                        break;
                }
            }
            catch (Exception exception)
            {
                WriteTelemetryException(module, exception);
            }
        }

        private void RefreshAtomVisuals(HashSet<Atom> updateAtoms)
        {
            foreach (Atom updateAtom in updateAtoms)
            {
                updateAtom.UpdateVisual();
                foreach (Bond updateAtomBond in updateAtom.Bonds)
                {
                    updateAtomBond.UpdateVisual();
                }
            }
        }

        /// <summary>
        /// Adds a floating symbol (typically a + sign)
        /// </summary>
        /// <param name="pos">Top-left corner of symbol</param>
        /// <param name="symbolText">Text to display</param>
        public void AddFloatingSymbol(Point pos, string symbolText)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                AddAnnotation(pos, symbolText, false);
            }
            catch (Exception exception)
            {
                WriteTelemetryException(module, exception);
            }
        }

        private void AddAnnotation(Point pos, string text, bool isEditable = true)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            try
            {
                WriteTelemetry(module, "Debug", $"Adding Annotation '{text}'");

                Annotation newSymbol = new Annotation { Position = pos, IsEditable = isEditable, SymbolSize = BlockTextSize };
                XElement docElement = new XElement(CMLNamespaces.xaml + "FlowDocument",
                                                   new XElement(CMLNamespaces.xaml + "Paragraph",
                                                                new XElement(CMLNamespaces.xaml + "Run",
                                                                             text)));
                newSymbol.Xaml = docElement.CreateNavigator().OuterXml;

                Action redo = () =>
                              {
                                  Model.AddAnnotation(newSymbol);
                              };
                Action undo = () =>
                              {
                                  Model.RemoveAnnotation(newSymbol);
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
        }

        #endregion Methods
    }
}
