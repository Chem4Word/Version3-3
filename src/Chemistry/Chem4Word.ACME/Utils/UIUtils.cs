// ---------------------------------------------------------------------------
//  Copyright (c) 2026, The .NET Foundation.
//  This software is released under the Apache Licence, Version 2.0.
//  The licence and further copyright text can be found in the file LICENCE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.ACME.Behaviors;
using Chem4Word.ACME.Controls;
using Chem4Word.ACME.Drawing.Visuals;
using Chem4Word.ACME.Entities;
using Chem4Word.ACME.Enums;
using Chem4Word.ACME.Models;
using Chem4Word.Core.Enums;
using Chem4Word.Model2;
using Chem4Word.Model2.Enums;
using IChem4Word.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Interop;
using System.Windows.Media;
using Application = System.Windows.Application;
using ContextMenu = System.Windows.Controls.ContextMenu;
using MenuItem = System.Windows.Controls.MenuItem;

namespace Chem4Word.ACME.Utils
{
    public static class UIUtils
    {
        public static void ShowDialog(Window dialog, object parent)
        {
            HwndSource source = (HwndSource)HwndSource.FromVisual((Visual)parent);
            if (source != null)
            {
                new WindowInteropHelper(dialog).Owner = source.Handle;
            }

            dialog.ShowDialog();
        }

        public static RenderingOptions ShowAcmeSettings(EditorCanvas currentEditor, RenderingOptions currentOptions, RenderingOptions userDefaultOptions, IChem4WordTelemetry telemetry, Point topLeft)
        {
            ShutdownMode mode = Application.Current.ShutdownMode;
            Application.Current.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            AcmeSettingsHost pe = new AcmeSettingsHost(currentOptions, userDefaultOptions, telemetry, topLeft);
            ShowDialog(pe, currentEditor);
            RenderingOptions result = pe.Result;

            Application.Current.ShutdownMode = mode;

            return result;
        }

        public static Point GetOffScreenPoint()
        {
            int maxX = int.MinValue;
            int maxY = int.MinValue;

            foreach (Screen screen in Screen.AllScreens)
            {
                maxX = Math.Max(maxX, screen.Bounds.Right);
                maxY = Math.Max(maxY, screen.Bounds.Bottom);
            }

            return new Point(maxX + 100, maxY + 100);
        }

        public static Point GetOnScreenCentrePoint(Point target, double width, double height)
        {
            double left = target.X - width / 2;
            double top = target.Y - height / 2;

            foreach (Screen screen in Screen.AllScreens)
            {
                if (screen.Bounds.Contains((int)target.X, (int)target.Y))
                {
                    // Checks are done in this order to ensure the title bar is always accessible

                    // Handle too far right
                    if (left + width > screen.WorkingArea.Right)
                    {
                        left = screen.WorkingArea.Right - width;
                    }

                    // Handle too low
                    if (top + height > screen.WorkingArea.Bottom)
                    {
                        top = screen.WorkingArea.Bottom - height;
                    }

                    // Handle too far left
                    if (left < screen.WorkingArea.Left)
                    {
                        left = screen.WorkingArea.Left;
                    }

                    // Handle too high
                    if (top < screen.WorkingArea.Top)
                    {
                        top = screen.WorkingArea.Top;
                    }
                }
            }

            return new Point(left, top);
        }

        public static void EditBondProperties(Bond bond,
                                              EditController controller,
                                              Point screenPosition,
                                              EditorCanvas currentEditor)
        {
            ShutdownMode mode = Application.Current.ShutdownMode;

            Application.Current.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            BondPropertiesModel model = new BondPropertiesModel
            {
                Centre = screenPosition,
                Path = bond.Path,
                Angle = bond.Angle,
                Length = bond.BondLength / ModelConstants.ScaleFactorForXaml,
                BondOrderValue = bond.OrderValue.Value,
                IsSingle = bond.Order.Equals(ModelConstants.OrderSingle),
                IsDouble = bond.Order.Equals(ModelConstants.OrderDouble),
                Is1Point5 = bond.Order.Equals(ModelConstants.OrderPartial12),
                Is2Point5 = bond.Order.Equals(ModelConstants.OrderPartial23)
            };

            model.BondAngle = model.AngleString;
            model.DoubleBondChoice = DoubleBondType.Auto;

            if (model.IsDouble || model.Is1Point5 || model.Is2Point5)
            {
                if (bond.ExplicitPlacement != null)
                {
                    model.DoubleBondChoice = (DoubleBondType)bond.ExplicitPlacement.Value;
                }
                else
                {
                    if (model.IsDouble && bond.Stereo == BondStereo.Indeterminate)
                    {
                        model.DoubleBondChoice = DoubleBondType.Indeterminate;
                    }
                }
            }

            if (model.IsSingle)
            {
                model.SingleBondChoice = SingleBondType.None;

                switch (bond.Stereo)
                {
                    case BondStereo.Wedge:
                        model.SingleBondChoice = SingleBondType.Wedge;
                        break;

                    case BondStereo.Hatch:
                        model.SingleBondChoice = SingleBondType.Hatch;
                        break;

                    case BondStereo.Indeterminate:
                        model.SingleBondChoice = SingleBondType.Indeterminate;
                        break;

                    case BondStereo.Thick:
                        model.SingleBondChoice = SingleBondType.Thick;
                        break;

                    default:
                        model.SingleBondChoice = SingleBondType.None;
                        break;
                }
            }

            model.ClearFlags();

            BondPropertyEditor pe = new BondPropertyEditor(model);
            ShowDialog(pe, currentEditor);
            Application.Current.ShutdownMode = mode;

            if (model.Save)
            {
                controller.UpdateBond(bond, model);
                controller.ClearSelection();

                bond.Order = Bond.OrderValueToOrder(model.BondOrderValue);
                if (controller.ActiveBehavior is SelectBehaviour)
                {
                    controller.AddToSelection(bond);
                }
            }
        }

        public static void EditAtomProperties(Atom atom,
                                              EditController controller,
                                              Point screenPosition,
                                              EditorCanvas currentEditor)
        {
            ShutdownMode mode = Application.Current.ShutdownMode;

            Application.Current.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            AtomPropertiesModel atomPropertiesModel = new AtomPropertiesModel
            {
                Centre = screenPosition,
                Path = atom.Path,
                Element = atom.Element
            };

            if (atom.Element is Element)
            {
                atomPropertiesModel.IsFunctionalGroup = false;
                atomPropertiesModel.IsElement = true;

                atomPropertiesModel.Charge = atom.FormalCharge ?? 0;
                atomPropertiesModel.Isotope = atom.IsotopeNumber.ToString();
                atomPropertiesModel.ExplicitC = atom.ExplicitC;
                atomPropertiesModel.ExplicitH = atom.ExplicitH;
                atomPropertiesModel.ExplicitHydrogenPlacement = atom.ExplicitHPlacement;

                atomPropertiesModel.ExplicitElectronPlacements = new Dictionary<CompassPoints, ElectronType>();
                atomPropertiesModel.Electrons = new List<Electron>();

                if (atom.Electrons.Count > 0)
                {
                    int manualPlacementsCount = atom.Electrons.Values.Count(e => e.ExplicitPlacement != null);

                    CompassPoints cp = CompassPoints.North;

                    foreach (Electron electron in atom.Electrons.Values)
                    {
                        ElectronType ty = electron.TypeOfElectron;

                        // There are some manual placements
                        if (manualPlacementsCount > 0)
                        {
                            if (electron.ExplicitPlacement.HasValue)
                            {
                                // This is a manual placement
                                cp = electron.ExplicitPlacement.Value;
                            }
                            else
                            {
                                // This is an automatic placement - find the next free compass point
                                while (atomPropertiesModel.ExplicitElectronPlacements.ContainsKey(cp))
                                {
                                    cp = Model2.Helpers.Utils.NextCompassPoint(cp);
                                }
                            }

                            // Add to the explicit placements
                            atomPropertiesModel.ExplicitElectronPlacements.Add(cp, ty);
                        }
                        else
                        {
                            // All are automatic placements
                            atomPropertiesModel.Electrons.Add(electron);
                        }
                    }
                }

                atomPropertiesModel.ShowHydrogenLabels = true;
            }

            if (atom.Element is FunctionalGroup)
            {
                atomPropertiesModel.IsElement = false;
                atomPropertiesModel.IsFunctionalGroup = true;

                atomPropertiesModel.ExplicitFunctionalGroupPlacement = atom.ExplicitFunctionalGroupPlacement;
                atomPropertiesModel.ShowHydrogenLabels = false;
            }

            atomPropertiesModel.IsNotSingleton = !atom.Singleton;

            atomPropertiesModel.MicroModel = new Model();
            atomPropertiesModel.MicroModel.SetUserOptions(currentEditor.Controller.Model.GetCurrentOptions());

            Molecule molecule = new Molecule { Id = "mx" };

            atomPropertiesModel.MicroModel.AddMolecule(molecule);
            molecule.Parent = atomPropertiesModel.MicroModel;

            Atom newAtom = new Atom
            {
                Id = atom.Id,
                Element = atom.Element,
                Position = atom.Position,
                ExplicitC = atom.ExplicitC,
                ExplicitH = atom.ExplicitH,
                FormalCharge = atom.FormalCharge,
                IsotopeNumber = atom.IsotopeNumber,
            };

            foreach (Electron electron in atom.Electrons.Values)
            {
                newAtom.AddElectron(electron.Copy());
            }
            molecule.AddAtom(newAtom);
            newAtom.Parent = molecule;

            int atomId = 0;
            foreach (Bond bond in atom.Bonds)
            {
                Atom ac = new Atom
                {
                    Id = $"aa{atomId++}",
                    Element = ModelGlobals.PeriodicTable.C,
                    ExplicitC = false,
                    ExplicitH = HydrogenLabels.None,
                    Position = bond.OtherAtom(atom).Position
                };
                molecule.AddAtom(ac);
                ac.Parent = molecule;
                Bond b = new Bond(newAtom, ac) { Order = bond.Order };
                if (bond.Stereo != BondStereo.None)
                {
                    b.Stereo = bond.Stereo;
                    if (bond.Stereo == BondStereo.Wedge || bond.Stereo == BondStereo.Hatch)
                    {
                        if (atom.Path.Equals(bond.StartAtom.Path))
                        {
                            b.StartAtomInternalId = newAtom.InternalId;
                            b.EndAtomInternalId = ac.InternalId;
                        }
                        else
                        {
                            b.StartAtomInternalId = ac.InternalId;
                            b.EndAtomInternalId = newAtom.InternalId;
                        }
                    }
                }
                molecule.AddBond(b);
                b.Parent = molecule;
            }
            atomPropertiesModel.MicroModel.ScaleToAverageBondLength(20);

            AtomPropertyEditor atomPropertyEditor = new AtomPropertyEditor(atomPropertiesModel);

            ShowDialog(atomPropertyEditor, currentEditor);
            Application.Current.ShutdownMode = mode;

            if (atomPropertiesModel.Save)
            {
                controller.UpdateAtom(atom, atomPropertiesModel);

                controller.ClearSelection();
                if (controller.ActiveBehavior is SelectBehaviour)
                {
                    controller.AddToSelection(atom);
                }

                if (atomPropertiesModel.AddedElement != null)
                {
                    AddOptionIfNeeded(atomPropertiesModel);
                }
                controller.SelectedElement = atomPropertiesModel.Element;
            }
            atomPropertyEditor.Close();

            void AddOptionIfNeeded(AtomPropertiesModel model)
            {
                if (!controller.AtomOptions.Any(ao => ao.Element.Symbol == model.AddedElement.Symbol))
                {
                    AtomOption newOption = null;
                    switch (model.AddedElement)
                    {
                        case Element elem:
                            newOption = new AtomOption(elem);
                            break;

                        case FunctionalGroup group:
                            newOption = new AtomOption(group);
                            break;
                    }
                    controller.AtomOptions.Add(newOption);
                }
            }
        }

        public static void EditMoleculeProperties(Molecule moleculeBeingEdited,
                                                  EditController controller,
                                                  Point screenPosition,
                                                  EditorCanvas currentEditor)
        {
            ShutdownMode mode = Application.Current.ShutdownMode;
            Application.Current.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            MoleculePropertiesModel moleculePropertiesModel = new MoleculePropertiesModel
            {
                Centre = screenPosition,
                Path = moleculeBeingEdited.Path,
                Used1DProperties = controller.Used1DProperties,
                Data = new Model()
            };

            Model parent = moleculeBeingEdited.Model;
            moleculePropertiesModel.Data.SetUserOptions(parent.GetCurrentOptions());

            Molecule molecule = moleculeBeingEdited.Copy();

            moleculePropertiesModel.Data.AddMolecule(molecule);
            molecule.Parent = moleculePropertiesModel.Data;

            moleculePropertiesModel.Charge = molecule.FormalCharge;
            moleculePropertiesModel.Count = molecule.Count;
            moleculePropertiesModel.SpinMultiplicity = molecule.SpinMultiplicity;
            moleculePropertiesModel.ShowMoleculeBrackets = molecule.ShowMoleculeBrackets;

            moleculePropertiesModel.ExplicitC = molecule.ExplicitC;
            moleculePropertiesModel.ExplicitH = molecule.ExplicitH;

            MoleculePropertyEditor pe = new MoleculePropertyEditor(moleculePropertiesModel);
            ShowDialog(pe, currentEditor);

            if (moleculePropertiesModel.Save)
            {
                Molecule thisMolecule = moleculePropertiesModel.Data.Molecules.First().Value;
                controller.UpdateMolecule(moleculeBeingEdited, thisMolecule);
            }

            Application.Current.ShutdownMode = mode;
        }

        private static void BuildAtomChargeMenu(MenuItem cmi, EditController controller, Atom atom)
        {
            cmi.Items.Clear();
            var atomPropertiesModel = new AtomPropertiesModel();
            foreach (ChargeValue chargeValue in atomPropertiesModel.Charges)
            {
                MenuItem chargeItem = new MenuItem
                {
                    Header = chargeValue.Label,
                    IsEnabled = true
                };
                chargeItem.Click += (s, e) =>
                                    {
                                        controller.SetCharge(atom, chargeValue.Value);
                                    };

                cmi.Items.Add(chargeItem);
                chargeItem.IsChecked = (atom.FormalCharge ?? 0) == chargeValue.Value;
            }
        }

        private static void BuildMoleculeChargeMenu(MenuItem cmi, EditController controller)
        {
            cmi.Items.Clear();
            var moleculePropertiesModel = new MoleculePropertiesModel();

            foreach (ChargeValue charge in moleculePropertiesModel.Charges)
            {
                MenuItem chargeItem = new MenuItem
                {
                    Header = charge.Label,
                    IsEnabled = true
                };
                chargeItem.Click += (s, e) =>
                                    {
                                        controller.SetSelectedMoleculeCharge(charge.Value);
                                    };
                cmi.Tag = controller;
                cmi.Items.Add(chargeItem);
                chargeItem.IsChecked = ((controller.SelectedItems[0] as Molecule)?.FormalCharge ?? 0) == charge.Value;
            }
        }

        public static (string message, string formula, string molecularWeight) ToggleSelect(ChemicalVisual activeVisual, EditController editController)
        {
            switch (activeVisual)
            {
                case GroupVisual gv:
                    Molecule mol = gv.ParentMolecule;
                    if (!editController.SelectedItems.Contains(mol))
                    {
                        editController.AddToSelection(mol);
                    }
                    else
                    {
                        editController.RemoveFromSelection(mol);
                    }

                    return (AcmeConstants.SelStatusMessage, editController.TotUpMolFormulae(), editController.TotUpSelectedMwt());

                case AtomVisual av:
                    {
                        Atom atom = av.ParentAtom;
                        //check just in case the parent atom is null -- can happen occasionally
                        if (atom != null)
                        {
                            Molecule rootMolecule = atom.Parent.RootMolecule;
                            if (rootMolecule.IsGrouped)
                            {
                                editController.AddToSelection(rootMolecule);
                            }
                            else
                            {
                                if (!editController.SelectedItems.Contains(atom))
                                {
                                    editController.AddToSelection(atom);
                                }
                                else
                                {
                                    editController.RemoveFromSelection(atom);
                                }
                            }
                        }

                        return (AcmeConstants.SelStatusMessage, editController.TotUpMolFormulae(), editController.TotUpSelectedMwt());
                    }

                case BondVisual bv:
                    {
                        Bond bond = bv.ParentBond;
                        Molecule rootMolecule = bond.Parent.RootMolecule;
                        if (rootMolecule.IsGrouped)
                        {
                            editController.AddToSelection(rootMolecule);
                        }

                        if (!editController.SelectedItems.Contains(bond))
                        {
                            editController.AddToSelection(bond);
                        }
                        else
                        {
                            editController.RemoveFromSelection(bond);
                        }

                        return (AcmeConstants.SelStatusMessage, editController.TotUpMolFormulae(), editController.TotUpSelectedMwt());
                    }

                case ReactionVisual rv:
                    {
                        Reaction reaction = rv.ParentReaction;
                        if (!editController.SelectedItems.Contains(reaction))
                        {
                            editController.AddToSelection(reaction);
                        }
                        else
                        {
                            editController.RemoveFromSelection(reaction);
                        }

                        return ("", "", "");
                    }

                case AnnotationVisual anv:
                    {
                        Annotation annotation = anv.ParentAnnotation;
                        if (!editController.SelectedItems.Contains(anv))
                        {
                            editController.AddToSelection(annotation);
                        }
                        else
                        {
                            editController.RemoveFromSelection(annotation);
                        }

                        return ("", "", "");
                    }
                default:
                    editController.ClearSelection();
                    return (AcmeConstants.SelectDefaultMessage, editController.TotUpMolFormulae(), editController.TotUpSelectedMwt());
            }
        }

        public static void HandleAtomContextMenuClick(EditorCanvas currentEditor, Atom atom)
        {
            ContextMenu contextMenu = null;
            EditController controller = currentEditor.Controller as EditController;

            BuildAtomContextMenu(ref contextMenu, atom);

            if (contextMenu != null)
            {
                contextMenu.IsOpen = true;
            }
            //local function
            void BuildAtomContextMenu(ref ContextMenu cm, Atom avParentAtom)
            {
                cm = (ContextMenu)currentEditor.FindResource("AtomContextMenu");

                foreach (object contextMenuItem in cm.Items)
                {
                    MenuItem cmi = contextMenuItem as MenuItem;
                    switch (cmi?.Header)
                    {
                        case "Charge":
                            BuildAtomChargeMenu(cmi, controller, avParentAtom);
                            break;

                        case "Radical":
                            break;

                        case "Properties...":
                            cmi.Command = controller.EditActiveAtomPropertiesCommand;
                            cmi.CommandParameter = avParentAtom;
                            break;
                    }
                }
            }
        }

        public static void HandleBondContextMenuClick(EditorCanvas currentEditor, Bond bond)
        {
            ContextMenu contextMenu = null;
            EditController controller = currentEditor.Controller as EditController;

            BuildBondContextMenu(ref contextMenu, bond);

            if (contextMenu != null)
            {
                contextMenu.IsOpen = true;
            }

            void BuildBondContextMenu(ref ContextMenu cm, Bond bvParentBond)
            {
                cm = (ContextMenu)currentEditor.FindResource("BondContextMenu");
                cm.DataContext = currentEditor.Controller;
                cm.Tag = bvParentBond;

                foreach (object contextMenuItem in cm.Items)
                {
                    MenuItem cmi = contextMenuItem as MenuItem;
                    cm.DataContext = currentEditor.Controller;

                    if (cmi?.Header is "Flip Stereo")
                    {
                        cmi.CommandParameter = bvParentBond;
                        cmi.Command = controller.FlipBondStereoCommand;
                        controller.FlipBondStereoCommand.RaiseCanExecChanged();
                    }
                    else if (cmi?.Header is "Properties...")
                    {
                        cmi.CommandParameter = bvParentBond;
                        cmi.Command = controller.EditActiveBondPropertiesCommand;
                    }
                }
            }
        }

        public static void HandleMoleculeContextMenuClick(EditorCanvas currentEditor,
                                                  bool singleAtomSelected)
        {
            ContextMenu contextMenu = null;
            EditController controller = currentEditor.Controller as EditController;

            BuildMoleculeContextMenu(ref contextMenu);

            if (contextMenu != null)
            {
                contextMenu.IsOpen = true;
            }

            return;

            //local functions
            void BuildMoleculeContextMenu(ref ContextMenu cm)
            {
                bool singleton = (currentEditor.Controller as EditController).SingleMolSelected;

                cm = (ContextMenu)currentEditor.FindResource("MoleculeContextMenu");

                foreach (object contextMenuItem in cm.Items)
                {
                    MenuItem cmi = contextMenuItem as MenuItem;

                    if (cmi?.Header is "Copy")
                    {
                        cmi.Command = controller.CopyCommand;
                    }
                    else if (cmi?.Header is "Cut")
                    {
                        cmi.Command = controller.CutCommand;
                    }
                    else if (cmi?.Header is "Paste")
                    {
                        cmi.Command = controller.PasteCommand;
                    }
                    else if (cmi?.Header is "Properties...")
                    {
                        cmi.Command = controller.EditSelectionPropertiesCommand;
                    }
                    else if (cmi?.Header is "Charge")
                    {
                        if (cmi.IsEnabled = singleton)
                        {
                            BuildMoleculeChargeMenu(cmi, controller);
                        }
                    }
                    else if (cmi?.Header is "Radical")
                    {
                        if (cmi.IsEnabled = singleton)
                        {
                            var selectedMol = controller.SelectedItems[0] as Molecule;
                            foreach (MenuItem radItem in cmi.Items)
                            {
                                bool spinNotNull = int.TryParse((string)radItem.Tag, out int result);

                                if (!spinNotNull)
                                {
                                    radItem.IsChecked = selectedMol.SpinMultiplicity == null;
                                }
                                else
                                {
                                    radItem.IsChecked = selectedMol.SpinMultiplicity == result;
                                }
                            }
                        }
                    }
                    else if (cmi?.Header is "Group")
                    {
                        cmi.Command = controller.GroupCommand;
                    }
                    else if (cmi?.Header is "Ungroup")
                    {
                        cmi.Command = controller.UnGroupCommand;
                    }
                    else if (cmi?.Header is "Flip Horizontal")
                    {
                        cmi.Command = controller.FlipHorizontalCommand;
                        cmi.IsEnabled = !singleAtomSelected;
                    }
                    else if (cmi?.Header is "Flip Vertical")
                    {
                        cmi.Command = controller.FlipVerticalCommand;
                        cmi.IsEnabled = !singleAtomSelected;
                    }
                }
            }
        }
    }
}
