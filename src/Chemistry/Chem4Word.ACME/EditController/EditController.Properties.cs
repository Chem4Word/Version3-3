// ---------------------------------------------------------------------------
//  Copyright (c) 2026, The .NET Foundation.
//  This software is released under the Apache Licence, Version 2.0.
//  The licence and further copyright text can be found in the file LICENCE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.ACME.Commands;
using Chem4Word.ACME.Commands.PropertyEdit;
using Chem4Word.ACME.Utils;
using Chem4Word.Core.Helpers;
using Chem4Word.Model2;
using Chem4Word.Model2.Enums;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace Chem4Word.ACME
{
    /// <summary>
    /// Functionality associated with editing properties of objects
    /// </summary>
    public partial class EditController
    {
        #region Fields

        //Various property-related commands invoked by user actions
        private EditSelectionPropertiesCommand _editSelectionPropertiesCommand;

        private EditActiveAtomPropertiesCommand _editActiveAtomPropertiesCommandCommand;
        private EditActiveBondPropertiesCommand _editActiveBondPropertiesCommandCommand;
        private EditSelectionPropertiesCommand _propertiesCommand;

        //the command tha allows settings to be specified
        private SettingsCommand _settingsCommand;

        #endregion Fields

        #region Properties

        //mostly properties associated with the above command fields

        /// <summary>
        /// Edits the current selection (atom/bond/molecule) properties. Invoked from the toolbar button
        /// </summary>
        public EditSelectionPropertiesCommand EditSelectionPropertiesCommand
        {
            get
            {
                return _editSelectionPropertiesCommand;
            }
            set
            {
                _editSelectionPropertiesCommand = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Edits the active atom properties. Invoked from the context menu
        /// </summary>
        public EditActiveAtomPropertiesCommand EditActiveAtomPropertiesCommand
        {
            get
            {
                return _editActiveAtomPropertiesCommandCommand;
            }
            set
            {
                _editActiveAtomPropertiesCommandCommand = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Edits the active bond properties. Invoked from the context menu
        /// </summary>
        public EditActiveBondPropertiesCommand EditActiveBondPropertiesCommand
        {
            get
            {
                return _editActiveBondPropertiesCommandCommand;
            }
            set
            {
                _editActiveBondPropertiesCommandCommand = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Edits the current selection (atom/bond/molecule) properties. Invoked from the context menu
        /// </summary>
        public EditSelectionPropertiesCommand PropertiesCommand
        {
            get
            {
                return _propertiesCommand;
            }
            set
            {
                _propertiesCommand = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Edits the settings for ACME in general. Invoked from the toolbar button
        /// </summary>
        public SettingsCommand SettingsCommand
        {
            get
            {
                return _settingsCommand;
            }
            set
            {
                _settingsCommand = value;
                OnPropertyChanged();
            }
        }

        #endregion Properties

        #region Methods

        /// <summary>
        /// Splits out a Transform into its components for logging purposes
        /// </summary>
        private List<string> DecodeTransform(Transform transform)
        {
            List<string> result = new List<string>();

            try
            {
                string typeName = transform.GetType().Name.Replace("Transform", "");

                switch (transform)
                {
                    case TransformGroup group:
                        result.Add($"{typeName}");
                        foreach (Transform child in group.Children)
                        {
                            result.AddRange(DecodeTransform(child));
                        }

                        break;

                    case TranslateTransform translate:
                        result.Add($"{typeName} by {SafeDouble.AsString(translate.X)},{SafeDouble.AsString(translate.Y)}");
                        break;

                    case RotateTransform rotate:
                        result.Add($"{typeName} by {SafeDouble.AsString(rotate.Angle)} degrees about {SafeDouble.AsString(rotate.CenterX)},{SafeDouble.AsString(rotate.CenterY)}");
                        break;

                    case ScaleTransform scale:
                        result.Add($"{typeName} by {SafeDouble.AsString(scale.ScaleX)},{SafeDouble.AsString(scale.ScaleY)} about {SafeDouble.AsString(scale.CenterX)},{SafeDouble.AsString(scale.CenterY)}");
                        break;

                    default:
                        result.Add($"{typeName} ???");
                        break;
                }
            }
            catch
            {
                // Do Nothing
            }

            return result;
        }

        private List<string> DecodeTransform(GeneralTransform generalTransform)
        {
            List<string> result = new List<string>();

            try
            {
                string typeName = generalTransform.GetType().Name.Replace("GeneralTransform", "");

                switch (generalTransform)
                {
                    case TransformGroup group:
                        result.Add($"{typeName}");
                        foreach (Transform child in group.Children)
                        {
                            result.AddRange(DecodeTransform(child));
                        }

                        break;

                    case TranslateTransform translate:
                        result.Add($"{typeName} by {SafeDouble.AsString(translate.X)},{SafeDouble.AsString(translate.Y)}");
                        break;

                    case RotateTransform rotate:
                        result.Add($"{typeName} by {SafeDouble.AsString(rotate.Angle)} degrees about {SafeDouble.AsString(rotate.CenterX)},{SafeDouble.AsString(rotate.CenterY)}");
                        break;

                    case ScaleTransform scale:
                        result.Add($"{typeName} by {SafeDouble.AsString(scale.ScaleX)},{SafeDouble.AsString(scale.ScaleY)} about {SafeDouble.AsString(scale.CenterX)},{SafeDouble.AsString(scale.CenterY)}");
                        break;

                    case MatrixTransform transform:
                        string customString = string.Format(CultureInfo.InvariantCulture,
                                                            "M11={0}, M12={1}, M21={2}, M22={3}, OffsetX={4}, OffsetY={5}",
                                                            transform.Matrix.M11, transform.Matrix.M12, transform.Matrix.M21, transform.Matrix.M22,
                                                            transform.Matrix.OffsetX, transform.Matrix.OffsetY);
                        result.Add($"{typeName} {customString}");
                        break;

                    default:
                        result.Add($"{typeName} ???");
                        break;
                }
            }
            catch
            {
                // Do Nothing
            }

            return result;
        }

        /// <summary>
        /// Applies a transform to a group of atoms
        /// e.g. from a rotate or translate operation
        /// Generally used in conjunction with a selection
        /// </summary>
        /// <param name="operation">Transform object</param>
        /// <param name="atoms">List of atoms to be transformed</param>
        public void TransformAtoms(Transform operation, List<Atom> atoms)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                string countString = atoms == null ? "{null}" : $"{atoms.Count}";
                string transform = string.Join(";", DecodeTransform(operation));
                WriteTelemetry(module, "Debug", $"Atoms: {countString} Transform: {transform}");

                if (atoms.Any())
                {
                    Transform inverse = (Transform)operation.Inverse;
                    if (inverse != null)
                    {
                        List<Atom> atomsToTransform = atoms.ToList();
                        //need a reference to the mol later
                        Molecule parent = atoms[0].Parent;

                        Action undo = () =>
                        {
                            HashSet<Bond> affectedBonds = new HashSet<Bond>();
                            ClearSelection();
                            foreach (Atom atom in atomsToTransform)
                            {
                                atom.Position = inverse.Transform(atom.Position);
                                atom.UpdateVisual();

                                affectedBonds.UnionWith(atom.Bonds);
                            }
                            TransformRelatedPushers(affectedBonds.ToList(), inverse);
                            parent.RootMolecule.UpdateVisual();
                            foreach (Atom o in atomsToTransform)
                            {
                                AddToSelection(o);
                            }
                        };

                        Action redo = () =>
                        {
                            HashSet<Bond> affectedBonds = new HashSet<Bond>();
                            ClearSelection();
                            foreach (Atom atom in atomsToTransform)
                            {
                                atom.Position = operation.Transform(atom.Position);
                                atom.UpdateVisual();

                                affectedBonds.UnionWith(atom.Bonds);
                            }
                            TransformRelatedPushers(affectedBonds.ToList(), operation);
                            parent.RootMolecule.UpdateVisual();
                            foreach (Atom o in atomsToTransform)
                            {
                                AddToSelection(o);
                            }
                        };

                        UndoManager.BeginUndoBlock();
                        UndoManager.RecordAction(undo, redo);
                        UndoManager.EndUndoBlock();
                        redo();
                    }
                }
            }
            catch (Exception exception)
            {
                WriteTelemetryException(module, exception);
            }

            CheckModelIntegrity(module);
        }

        /// <summary>
        /// Transforms a collection of objects (molecules, reactions, annotations)
        /// within a selection
        /// </summary>
        /// <param name="operation">Transform to apply to the selection</param>
        /// <param name="objectsToTransform">List of StructuralObject(s) to transform</param>
        public void TransformObjects(Transform operation, List<StructuralObject> objectsToTransform)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            List<Molecule> molecules = objectsToTransform.OfType<Molecule>().ToList();
            List<Reaction> reactions = objectsToTransform.OfType<Reaction>().ToList();
            List<Annotation> annotations = objectsToTransform.OfType<Annotation>().ToList();

            (List<ElectronPusher> firstControlPoints, List<ElectronPusher> secondControlPoints, List<ElectronPusher> pushers) = AffectedPushers(molecules);

            try
            {
                string countMolString = molecules == null ? "{null}" : $"{molecules.Count}";
                string countReactString = reactions == null ? "{null}" : $"{reactions.Count}";
                string countAnnotationString = annotations == null ? "{null}" : $"{annotations.Count}";
                string countPushersString = pushers == null ? "{null}" : $"{pushers.Count}";

                IEnumerable<Molecule> rootMolecules = from m in molecules
                                                      where m.RootMolecule == m
                                                      select m;
                Molecule[] moleculesToTransform = rootMolecules.ToArray();
                string transformAsString = string.Join(";", DecodeTransform(operation));
                GeneralTransform inverse = operation.Inverse;
                string inverseTransformAsString = string.Join(";", DecodeTransform(inverse));

                Action undo = () =>
                {
                    SuppressEditorRedraw(true);
                    ClearSelection();

                    WriteTelemetry(module, "Debug", $"Transform: {inverseTransformAsString}");

                    WriteTelemetry(module, "Debug", $"Molecules: {countMolString}");
                    WriteTelemetry(module, "Debug", $"Reactions: {countReactString}");
                    WriteTelemetry(module, "Debug", $"Annotations: {countAnnotationString}");
                    WriteTelemetry(module, "Debug", $"Pushers: {countPushersString}");

                    foreach (Molecule molecule in moleculesToTransform)
                    {
                        Transform(molecule, (Transform)inverse);
                    }

                    foreach (Reaction reaction in reactions)
                    {
                        reaction.TailPoint = inverse.Transform(reaction.TailPoint);
                        reaction.HeadPoint = inverse.Transform(reaction.HeadPoint);
                    }

                    foreach (Annotation ann in annotations)
                    {
                        ann.Position = inverse.Transform(ann.Position);
                    }

                    SuppressEditorRedraw(false);

                    foreach (Molecule mol in moleculesToTransform)
                    {
                        mol.UpdateVisual();
                    }

                    foreach (Reaction react in reactions)
                    {
                        react.UpdateVisual();
                    }

                    foreach (Annotation ann in annotations)
                    {
                        ann.UpdateVisual();
                    }

                    foreach (ElectronPusher pusher in firstControlPoints)
                    {
                        pusher.FirstControlPoint = inverse.Transform(pusher.FirstControlPoint);
                    }
                    foreach (ElectronPusher pusher in secondControlPoints)
                    {
                        pusher.SecondControlPoint = inverse.Transform(pusher.SecondControlPoint);
                    }
                    foreach (ElectronPusher pusher in pushers)
                    {
                        pusher.UpdateVisual();
                    }

                    AddObjectListToSelection(molecules.Cast<StructuralObject>().ToList());
                    AddObjectListToSelection(reactions.Cast<StructuralObject>().ToList());
                    AddObjectListToSelection(annotations.Cast<StructuralObject>().ToList());
                };

                Action redo = () =>
                {
                    SuppressEditorRedraw(true);
                    ClearSelection();

                    WriteTelemetry(module, "Debug", $"Transform: {transformAsString}");

                    WriteTelemetry(module, "Debug", $"Molecules: {countMolString}");
                    WriteTelemetry(module, "Debug", $"Reactions: {countReactString}");
                    WriteTelemetry(module, "Debug", $"Annotations: {countAnnotationString}");
                    WriteTelemetry(module, "Debug", $"Pushers: {countPushersString}");

                    foreach (Molecule molecule in moleculesToTransform)
                    {
                        Transform(molecule, operation);
                    }

                    foreach (Reaction reaction in reactions)
                    {
                        reaction.TailPoint = operation.Transform(reaction.TailPoint);
                        reaction.HeadPoint = operation.Transform(reaction.HeadPoint);
                    }

                    foreach (Annotation ann in annotations)
                    {
                        ann.Position = operation.Transform(ann.Position);
                    }

                    SuppressEditorRedraw(false);

                    foreach (Molecule mol in moleculesToTransform)
                    {
                        mol.UpdateVisual();
                    }

                    foreach (Reaction react in reactions)
                    {
                        react.UpdateVisual();
                    }

                    foreach (Annotation ann in annotations)
                    {
                        ann.UpdateVisual();
                    }

                    foreach (ElectronPusher pusher in firstControlPoints)
                    {
                        pusher.FirstControlPoint = operation.Transform(pusher.FirstControlPoint);
                    }
                    foreach (ElectronPusher pusher in secondControlPoints)
                    {
                        pusher.SecondControlPoint = operation.Transform(pusher.SecondControlPoint);
                    }
                    foreach (ElectronPusher pusher in pushers)
                    {
                        pusher.UpdateVisual();
                    }

                    AddObjectListToSelection(molecules.Cast<StructuralObject>().ToList());
                    AddObjectListToSelection(reactions.Cast<StructuralObject>().ToList());
                    AddObjectListToSelection(annotations.Cast<StructuralObject>().ToList());
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

        private (List<ElectronPusher> First, List<ElectronPusher> Second, List<ElectronPusher> Affected) AffectedPushers(List<Molecule> molecules)
        {
            List<Atom> atoms = new List<Atom>();
            List<Bond> bonds = new List<Bond>();
            List<Electron> electrons = new List<Electron>();

            Dictionary<string, ElectronPusher> affectedElectronPushers = new Dictionary<string, ElectronPusher>();
            Dictionary<string, ElectronPusher> affectedFirstControlPoint = new Dictionary<string, ElectronPusher>();
            Dictionary<string, ElectronPusher> affectedSecondControlPoint = new Dictionary<string, ElectronPusher>();

            foreach (Molecule molecule in molecules)
            {
                atoms.AddRange(molecule.Atoms.Values);
                bonds.AddRange(molecule.Bonds);
            }

            foreach (Atom atom in atoms)
            {
                electrons.AddRange(atom.AllElectrons());
            }

            foreach (ElectronPusher pusher in Model.ElectronPushers.Values)
            {
                // Handle StartChemistry
                if (atoms.Contains(pusher.StartChemistry))
                {
                    affectedFirstControlPoint[pusher.Path] = pusher;
                }
                if (bonds.Contains(pusher.StartChemistry))
                {
                    affectedFirstControlPoint[pusher.Path] = pusher;
                }
                if (electrons.Contains(pusher.StartChemistry))
                {
                    affectedFirstControlPoint[pusher.Path] = pusher;
                }

                // Handle EndChemistries[]
                foreach (StructuralObject endChemistry in pusher.EndChemistries)
                {
                    if (atoms.Contains(endChemistry))
                    {
                        affectedSecondControlPoint[pusher.Path] = pusher;
                    }

                    if (electrons.Contains(endChemistry))
                    {
                        affectedSecondControlPoint[pusher.Path] = pusher;
                    }

                    if (bonds.Contains(endChemistry))
                    {
                        affectedSecondControlPoint[pusher.Path] = pusher;
                    }
                }
            }

            foreach (KeyValuePair<string, ElectronPusher> pair in affectedFirstControlPoint)
            {
                affectedElectronPushers[pair.Key] = pair.Value;
            }
            foreach (KeyValuePair<string, ElectronPusher> pair in affectedSecondControlPoint)
            {
                affectedElectronPushers[pair.Key] = pair.Value;
            }

            return (affectedFirstControlPoint.Values.ToList(), affectedSecondControlPoint.Values.ToList(), affectedElectronPushers.Values.ToList());
        }

        /// <summary>
        /// Applies a transform to a single molecule.
        /// Generally used on a selection
        /// </summary>
        /// <param name="molecule">Molecule object to transform</param>
        /// <param name="operation">Transform to apply (such as reflect, rotate etc)</param>
        private void Transform(Molecule molecule, Transform operation)
        {
            if (!molecule.IsGrouped)
            {
                foreach (Atom atom in molecule.Atoms.Values)
                {
                    atom.Position = operation.Transform(atom.Position);
                    atom.UpdateVisual();
                }
            }
            else
            {
                foreach (Molecule mol in molecule.Molecules.Values)
                {
                    Transform(mol, operation);
                    mol.UpdateVisual();
                }
            }

            //TransformAllElectronPushers(molecule, operation);
        }

        /// <summary>
        /// Applies a sequence of transforms to a group of molecules
        /// </summary>
        /// <param name="operations">List of Transform objects to sequentially apply</param>
        /// <param name="molecules">List of molecules to transform</param>
        public void MultiTransformMolecules(List<Transform> operations, List<Molecule> molecules)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                string countString = molecules == null ? "{null}" : $"{molecules.Count}";

                IEnumerable<Molecule> rootMolecules = from m in molecules
                                                      where m.RootMolecule == m
                                                      select m;
                Molecule[] moleculesToTransform = rootMolecules.ToArray();

                Action undo = () =>
                {
                    SuppressEditorRedraw(true);

                    for (int i = 0; i < moleculesToTransform.Length; i++)
                    {
                        GeneralTransform inverse = operations[i].Inverse;
                        string transform = string.Join(";", DecodeTransform(inverse));
                        WriteTelemetry(module, "Debug",
                                       $"Molecules: {countString} Transform: {transform}");
                        Transform(moleculesToTransform[i], (Transform)inverse);
                    }

                    SuppressEditorRedraw(false);

                    foreach (Molecule mol in moleculesToTransform)
                    {
                        mol.UpdateVisual();
                    }

                    AddObjectListToSelection(moleculesToTransform.Cast<StructuralObject>().ToList());
                };

                Action redo = () =>
                {
                    SuppressEditorRedraw(true);

                    for (int i = 0; i < moleculesToTransform.Length; i++)
                    {
                        string transform = string.Join(";", DecodeTransform(operations[i]));
                        WriteTelemetry(module, "Debug",
                                       $"Molecules: {countString} Transform: {transform}");
                        Transform(moleculesToTransform[i], operations[i]);
                    }

                    SuppressEditorRedraw(false);

                    foreach (Molecule mol in moleculesToTransform)
                    {
                        mol.UpdateVisual();
                    }

                    AddObjectListToSelection(moleculesToTransform.Cast<StructuralObject>().ToList());
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
        /// Applies a single transform to a group of molecules
        /// </summary>
        /// <param name="operation">Operation to apply</param>
        /// <param name="molecules">List of molecules to transform</param>
        public void TransformMoleculeList(Transform operation, List<Molecule> molecules)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                string countString = molecules == null ? "{null}" : $"{molecules.Count}";
                string transform = string.Join(";", DecodeTransform(operation));
                WriteTelemetry(module, "Debug", $"Molecules: {countString} Transform: {transform}");

                GeneralTransform inverse = operation.Inverse;
                if (inverse != null)
                {
                    IEnumerable<Molecule> rootMolecules = from m in molecules
                                                          where m.RootMolecule == m
                                                          select m;
                    Molecule[] moleculesToTransform = rootMolecules.ToArray();

                    Action undo = () =>
                    {
                        SuppressEditorRedraw(true);
                        ClearSelection();
                        foreach (Molecule mol in moleculesToTransform)
                        {
                            Transform(mol, (Transform)inverse);
                        }

                        SuppressEditorRedraw(false);

                        foreach (Molecule mol in moleculesToTransform)
                        {
                            mol.UpdateVisual();
                        }

                        AddObjectListToSelection(molecules.Cast<StructuralObject>().ToList());
                    };

                    Action redo = () =>
                    {
                        SuppressEditorRedraw(true);
                        ClearSelection();
                        foreach (Molecule mol in moleculesToTransform)
                        {
                            Transform(mol, operation);
                        }

                        SuppressEditorRedraw(false);

                        foreach (Molecule mol in moleculesToTransform)
                        {
                            mol.UpdateVisual();
                        }
                    };

                    UndoManager.BeginUndoBlock();
                    UndoManager.RecordAction(undo, redo);
                    UndoManager.EndUndoBlock();
                    redo();
                }
            }
            catch (Exception exception)
            {
                WriteTelemetryException(module, exception);
            }

            CheckModelIntegrity(module);
        }

        /// <summary>
        /// Inverts the 'sense' of a wedge or hatch bond
        /// </summary>
        /// <param name="bond">Bond object to invert</param>
        public void InvertStereobond(Bond bond)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                WriteTelemetry(module, "Debug", "Called");

                Atom originalStartAtom = bond.StartAtom;
                Atom originalEndAtom = bond.EndAtom;

                Action undo = () =>
                {
                    (bond.StartAtomInternalId, bond.EndAtomInternalId) = (originalStartAtom.InternalId, originalEndAtom.InternalId);
                    bond.UpdateVisual();
                };

                Action redo = () =>
                {
                    (bond.EndAtomInternalId, bond.StartAtomInternalId) = (originalStartAtom.InternalId, originalEndAtom.InternalId);
                    bond.UpdateVisual();
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
        /// One-stop method for setting both bond order and stereochemistry
        /// </summary>
        /// <param name="bond">Bond object to invert</param>
        /// <param name="newOrder">New bond order to apply</param>
        /// <param name="newStereo">New BondStereo value to apply</param>
        public void SetBondAttributes(Bond bond, string newOrder = null, BondStereo? newStereo = null)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                string orderParameter = newOrder ?? CurrentBondOrder;
                BondStereo stereoParameter = newStereo ?? CurrentStereo;
                WriteTelemetry(module, "Debug", $"Order: {orderParameter}; Stereo: {stereoParameter}");

                string order = bond.Order;
                BondStereo stereo = bond.Stereo;

                Action undo = () =>
                {
                    bond.Order = order;
                    bond.Stereo = stereo;
                    bond.StartAtom.UpdateVisual();
                    bond.EndAtom.UpdateVisual();
                    RefreshConnectingWedges(bond);
                };

                Action redo = () =>
                {
                    bond.Order = newOrder ?? CurrentBondOrder;
                    bond.Stereo = newStereo ?? CurrentStereo;
                    bond.StartAtom.UpdateVisual();
                    bond.EndAtom.UpdateVisual();
                    RefreshConnectingWedges(bond);
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

            //local function
            // we need to refresh any wedges/hatches on bonds connected to the atoms at either end of this bond
            void RefreshConnectingWedges(Bond b)
            {
                foreach (Atom a in b.StartAtom.NeighboursExcept(b.EndAtom))
                {
                    Bond otherBond = b.StartAtom.BondBetween(a);

                    otherBond.UpdateVisual();
                }

                foreach (Atom a in b.EndAtom.NeighboursExcept(b.StartAtom))
                {
                    Bond otherBond = b.EndAtom.BondBetween(a);

                    otherBond.UpdateVisual();
                }
            }
        }

        /// <summary>
        /// Single point of entry for editing the properties of the current selection.
        /// Invoked from the main toolbar only.
        /// </summary>
        public void EditSelectionProperties()
        {
            if (SingleMolSelected)
            {
                EditMoleculeProperties();
            }
            else if (SingleAtomSelected)
            {
                EditAtomProperties(SelectedItems[0] as Atom);
            }
            else if (SingleBondSelected)
            {
                EditBondProperties(SelectedItems[0] as Bond);
            }
        }

        /// <summary>
        /// Edits the properties of a single bond
        /// Either select it or right-click on it and choose Properties
        /// </summary>
        /// <param name="selectedBond">Bond whose properties you want to edit</param>
        public void EditBondProperties(Bond selectedBond)
        {
            if (selectedBond != null)
            {
                Point screenPosition = EditingCanvas.PointToScreen(Mouse.GetPosition(EditingCanvas));
                UIUtils.EditBondProperties(selectedBond, this, screenPosition, EditingCanvas);
            }
        }

        /// <summary>
        /// Edits the properties of a single atom
        /// Either select it or right-click on it and choose Properties
        /// </summary>
        /// <param name="selectedAtom">Atom she properties you want to edit</param>
        public void EditAtomProperties(Atom selectedAtom)
        {
            if (selectedAtom != null)
            {
                Point screenPosition = EditingCanvas.PointToScreen(Mouse.GetPosition(EditingCanvas));
                UIUtils.EditAtomProperties(selectedAtom, this, screenPosition, EditingCanvas);
            }
        }

        /// <summary>
        /// Edits the properties of a single molecule.
        /// Select it, click the toolbar button, or right-click on it and choose Properties
        /// </summary>
        private void EditMoleculeProperties()
        {
            if (SelectedItems[0] is Molecule selectedMol)
            {
                Point screenPosition = EditingCanvas.PointToScreen(Mouse.GetPosition(EditingCanvas));
                UIUtils.EditMoleculeProperties(selectedMol, this, screenPosition, EditingCanvas);
            }
        }

        /// <summary>
        /// Sets the formal charge on the selected molecule
        /// </summary>
        /// <param name="charge">Int containing the value to set</param>
        public void SetSelectedMoleculeCharge(int charge)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                Molecule selectedMol = SelectedItems[0] as Molecule;
                int lastCharge = selectedMol.FormalCharge ?? 0;
                if (lastCharge != charge)
                {
                    UndoManager.BeginUndoBlock();

                    Action redo = () =>
                    {
                        selectedMol.FormalCharge = charge;
                        selectedMol.UpdateVisual();
                    };
                    Action undo = () =>
                    {
                        selectedMol.FormalCharge = lastCharge;
                        selectedMol.UpdateVisual();
                    };
                    UndoManager.RecordAction(undo, redo, $"Set Charge to {charge}");
                    redo();
                    UndoManager.EndUndoBlock();
                }
            }
            catch (Exception exception)
            {
                WriteTelemetryException(module, exception);
            }

            CheckModelIntegrity(module);
        }

        /// <summary>
        /// Sets the formal charge on a single atom
        /// </summary>
        /// <param name="atom">Atom to set charge on</param>
        /// <param name="charge">Int contains the value to set</param>
        public void SetCharge(Atom atom, int charge)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                WriteTelemetry(module, "Debug",
                               $"Atom: {atom.Path}; Charge: {charge}");
                int? lastCharge = atom.FormalCharge;
                if (charge != lastCharge)
                {
                    UndoManager.BeginUndoBlock();

                    Action redo = () =>
                    {
                        atom.FormalCharge = charge;
                        atom.UpdateVisual();

                        foreach (Bond bond in atom.Bonds)
                        {
                            bond.UpdateVisual();
                        }
                    };

                    Action undo = () =>
                    {
                        atom.FormalCharge = lastCharge;
                        atom.UpdateVisual();

                        foreach (Bond bond in atom.Bonds)
                        {
                            bond.UpdateVisual();
                        }
                    };

                    UndoManager.RecordAction(undo, redo, $"Set Charge to {charge}");
                    redo();

                    ClearSelection();
                }

                UndoManager.EndUndoBlock();
            }
            catch (Exception exception)
            {
                WriteTelemetryException(module, exception);
            }

            CheckModelIntegrity(module);
        }

        /// <summary>
        /// Sets the radical state on the selected molecule
        /// </summary>
        /// <param name="spin">Nullable int to set the value. Null clears the radical state</param>
        public void SetSelectedMoleculeRadical(int? spin)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            try
            {
                Molecule selectedMol = SelectedItems[0] as Molecule;
                int? lastSpin = selectedMol.SpinMultiplicity;
                if (lastSpin != spin)
                {
                    UndoManager.BeginUndoBlock();

                    Action redo = () =>
                    {
                        selectedMol.SpinMultiplicity = spin;
                        selectedMol.UpdateVisual();
                    };
                    Action undo = () =>
                    {
                        selectedMol.SpinMultiplicity = lastSpin;
                        selectedMol.UpdateVisual();
                    };
                    UndoManager.RecordAction(undo, redo, $"Set Spin Multiplicity to {spin}");
                    redo();
                    UndoManager.EndUndoBlock();
                }
            }
            catch (Exception exception)
            {
                WriteTelemetryException(module, exception);
            }

            CheckModelIntegrity(module);
        }

        /// <summary>
        /// Creates a parent molecule and makes all the selected molecules children
        /// </summary>
        /// <param name="children">List of child molecules</param>
        private void Group(List<Molecule> children)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                WriteTelemetry(module, "Debug", "Called");

                Molecule parent = new Molecule();
                Action redo = () =>
                {
                    ClearSelection();
                    parent.Parent = Model;
                    Model.AddMolecule(parent);
                    Molecule[] kids = children.ToArray();
                    foreach (Molecule molecule in kids)
                    {
                        if (Model.Molecules.Values.Contains(molecule))
                        {
                            Model.RemoveMolecule(molecule);
                            molecule.Parent = parent;
                            parent.AddMolecule(molecule);
                        }
                    }

                    parent.UpdateVisual();
                    AddToSelection(parent);
                    SendStatus((DefaultStatusMessage, TotUpMolFormulae(), TotUpSelectedMwt()));
                };

                Action undo = () =>
                {
                    ClearSelection();

                    Model.RemoveMolecule(parent);
                    parent.Parent = null;
                    Molecule[] kids = parent.Molecules.Values.ToArray();
                    foreach (Molecule child in kids)
                    {
                        if (parent.Molecules.Values.Contains(child))
                        {
                            parent.RemoveMolecule(child);

                            child.Parent = Model;
                            Model.AddMolecule(child);
                            child.UpdateVisual();
                            AddToSelection(child);
                        }
                    }

                    SendStatus((DefaultStatusMessage, TotUpMolFormulae(), TotUpSelectedMwt()));
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
        /// Splits selected grouped molecule into children.
        /// Grandchildren and descendants remain grouped
        /// </summary>
        /// <param name="selection">Active selection within the editor</param>
        public void UnGroup(IEnumerable<object> selection)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                WriteTelemetry(module, "Debug", "Called");

                List<Molecule> selGroups;
                //grab just the grouped molecules first
                selGroups = (from Molecule mol in selection.OfType<Molecule>()
                             where mol.IsGrouped
                             select mol).ToList();
                UnGroup(selGroups);
            }
            catch (Exception exception)
            {
                WriteTelemetryException(module, exception);
            }
        }

        /// <summary>
        /// Splits selected grouped molecules into children.
        /// Grandchildren and descendants remain grouped
        /// </summary>
        /// <param name="selGroups">Active selection of groups within the editor</param>
        private void UnGroup(List<Molecule> selGroups)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                //keep track of parent child relationships for later
                Dictionary<Molecule, List<Molecule>> parentsAndChildren = new Dictionary<Molecule, List<Molecule>>();

                foreach (Molecule selGroup in selGroups)
                {
                    parentsAndChildren[selGroup] = new List<Molecule>();
                    foreach (Molecule child in selGroup.Molecules.Values)
                    {
                        parentsAndChildren[selGroup].Add(child);
                    }
                }

                Action redo = () =>
                {
                    //selected groups are always top level objects
                    foreach (Molecule parent in parentsAndChildren.Keys)
                    {
                        RemoveFromSelection(parent);
                        Model.RemoveMolecule(parent);
                        foreach (Molecule child in parentsAndChildren[parent])
                        {
                            child.Parent = Model;
                            Model.AddMolecule(child);
                            child.UpdateVisual();
                        }
                    }

                    foreach (List<Molecule> molecules in parentsAndChildren.Values)
                    {
                        foreach (Molecule child in molecules)
                        {
                            AddToSelection(child);
                        }
                    }

                    SendStatus((DefaultStatusMessage, TotUpMolFormulae(), TotUpSelectedMwt()));
                };

                Action undo = () =>
                {
                    foreach (KeyValuePair<Molecule, List<Molecule>> oldParent in parentsAndChildren)
                    {
                        Model.AddMolecule(oldParent.Key);
                        foreach (Molecule child in oldParent.Value)
                        {
                            RemoveFromSelection(child);
                            Model.RemoveMolecule(child);
                            child.Parent = oldParent.Key;
                            oldParent.Key.AddMolecule(child);
                            child.UpdateVisual();
                        }

                        oldParent.Key.UpdateVisual();
                    }

                    foreach (Molecule parent in parentsAndChildren.Keys)
                    {
                        AddToSelection(parent);
                    }

                    SendStatus((DefaultStatusMessage, TotUpMolFormulae(), TotUpSelectedMwt()));
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
        ///  Creates a parent molecule and makes all the selected molecules children
        /// </summary>
        /// <param name="selection">Observable collection of ChemistryBase objects</param>
        public void Group(IEnumerable<object> selection)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                //grab just the molecules (to be grouped)
                List<Molecule> children = (from Molecule mol in selection.OfType<Molecule>()
                                           select mol).ToList();
                Group(children);
            }
            catch (Exception exception)
            {
                WriteTelemetryException(module, exception);
            }
        }

        #endregion Methods
    }
}
