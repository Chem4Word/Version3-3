// ---------------------------------------------------------------------------
//  Copyright (c) 2025, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.ACME.Controls;
using Chem4Word.ACME.Drawing.Visuals;
using Chem4Word.ACME.Enums;
using Chem4Word.ACME.Models;
using Chem4Word.Model2;
using Chem4Word.Model2.Enums;
using IChem4Word.Contracts;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using Application = System.Windows.Application;

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
            var mode = Application.Current.ShutdownMode;
            Application.Current.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            var pe = new AcmeSettingsHost(currentOptions, userDefaultOptions, telemetry, topLeft);
            ShowDialog(pe, currentEditor);
            var result = pe.Result;

            Application.Current.ShutdownMode = mode;

            return result;
        }

        public static Point GetOffScreenPoint()
        {
            int maxX = int.MinValue;
            int maxY = int.MinValue;

            foreach (var screen in Screen.AllScreens)
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

            foreach (var screen in Screen.AllScreens)
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

        public static void DoPropertyEdit(MouseButtonEventArgs e, EditorCanvas currentEditor)
        {
            var controller = (EditController)currentEditor.Controller;

            var position = e.GetPosition(currentEditor);
            var screenPosition = currentEditor.PointToScreen(position);

            // Did RightClick occur on a Molecule Selection Adorner?
            var moleculeAdorner = currentEditor.GetMoleculeAdorner(position);
            if (moleculeAdorner != null)
            {
                if (moleculeAdorner.AdornedMolecules.Count == 1)
                {
                    screenPosition = GetDpiAwareScaledPosition(screenPosition, moleculeAdorner);

                    var mode = Application.Current.ShutdownMode;
                    Application.Current.ShutdownMode = ShutdownMode.OnExplicitShutdown;

                    var moleculePropertiesModel = new MoleculePropertiesModel
                    {
                        Centre = screenPosition,
                        Path = moleculeAdorner.AdornedMolecules[0].Path,
                        Used1DProperties = controller.Used1DProperties
                    };

                    moleculePropertiesModel.Data = new Model();
                    var parent = moleculeAdorner.AdornedMolecules[0].Model;
                    moleculePropertiesModel.Data.SetUserOptions(parent.GetCurrentOptions());

                    var molecule = moleculeAdorner.AdornedMolecules[0].Copy();

                    moleculePropertiesModel.Data.AddMolecule(molecule);
                    molecule.Parent = moleculePropertiesModel.Data;

                    moleculePropertiesModel.Charge = molecule.FormalCharge;
                    moleculePropertiesModel.Count = molecule.Count;
                    moleculePropertiesModel.SpinMultiplicity = molecule.SpinMultiplicity;
                    moleculePropertiesModel.ShowMoleculeBrackets = molecule.ShowMoleculeBrackets;

                    moleculePropertiesModel.ExplicitC = molecule.ExplicitC;
                    moleculePropertiesModel.ExplicitH = molecule.ExplicitH;

                    var pe = new MoleculePropertyEditor(moleculePropertiesModel);
                    ShowDialog(pe, currentEditor);

                    if (moleculePropertiesModel.Save)
                    {
                        var thisMolecule = moleculePropertiesModel.Data.Molecules.First().Value;
                        controller.UpdateMolecule(moleculeAdorner.AdornedMolecules[0], thisMolecule);
                    }

                    Application.Current.ShutdownMode = mode;
                }
            }
            else
            {
                // Did RightClick occur on a ChemicalVisual?
                var activeVisual = currentEditor.GetTargetedVisual(position);
                if (activeVisual != null)
                {
                    screenPosition = GetDpiAwareScaledPosition(screenPosition, activeVisual);

                    // Did RightClick occur on an AtomVisual?
                    if (activeVisual is AtomVisual av && !(activeVisual is HydrogenVisual))
                    {
                        var mode = Application.Current.ShutdownMode;

                        Application.Current.ShutdownMode = ShutdownMode.OnExplicitShutdown;

                        var atom = av.ParentAtom;
                        var atomPropertiesModel = new AtomPropertiesModel
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

                        Molecule m = new Molecule();
                        atomPropertiesModel.MicroModel.AddMolecule(m);
                        m.Parent = atomPropertiesModel.MicroModel;

                        Atom a = new Atom();
                        a.Id = atom.Id;
                        a.Element = atom.Element;
                        a.Position = atom.Position;
                        a.ExplicitC = atom.ExplicitC;
                        a.ExplicitH = atom.ExplicitH;
                        a.FormalCharge = atom.FormalCharge;
                        a.IsotopeNumber = atom.IsotopeNumber;
                        m.AddAtom(a);
                        a.Parent = m;

                        int atomId = 0;
                        foreach (var bond in atom.Bonds)
                        {
                            Atom ac = new Atom();
                            ac.Id = $"aa{atomId++}";
                            ac.Element = ModelGlobals.PeriodicTable.C;
                            ac.ExplicitC = false;
                            ac.ExplicitH = HydrogenLabels.None;
                            ac.Position = bond.OtherAtom(atom).Position;
                            m.AddAtom(ac);
                            ac.Parent = m;
                            Bond b = new Bond(a, ac);
                            b.Order = bond.Order;
                            if (bond.Stereo != BondStereo.None)
                            {
                                b.Stereo = bond.Stereo;
                                if (bond.Stereo == BondStereo.Wedge || bond.Stereo == BondStereo.Hatch)
                                {
                                    if (atom.Path.Equals(bond.StartAtom.Path))
                                    {
                                        b.StartAtomInternalId = a.InternalId;
                                        b.EndAtomInternalId = ac.InternalId;
                                    }
                                    else
                                    {
                                        b.StartAtomInternalId = ac.InternalId;
                                        b.EndAtomInternalId = a.InternalId;
                                    }
                                }
                            }
                            m.AddBond(b);
                            b.Parent = m;
                        }
                        atomPropertiesModel.MicroModel.ScaleToAverageBondLength(20);

                        var atomPropertyEditor = new AtomPropertyEditor(atomPropertiesModel);

                        ShowDialog(atomPropertyEditor, currentEditor);
                        Application.Current.ShutdownMode = mode;

                        if (atomPropertiesModel.Save)
                        {
                            controller.UpdateAtom(atom, atomPropertiesModel);

                            controller.ClearSelection();
                            controller.AddToSelection(atom);

                            if (atomPropertiesModel.AddedElement != null)
                            {
                                AddOptionIfNeeded(atomPropertiesModel);
                            }
                            controller.SelectedElement = atomPropertiesModel.Element;
                        }
                        atomPropertyEditor.Close();
                    }

                    // Did RightClick occur on a BondVisual?
                    if (activeVisual is BondVisual bv)
                    {
                        var mode = Application.Current.ShutdownMode;

                        Application.Current.ShutdownMode = ShutdownMode.OnExplicitShutdown;

                        var bond = bv.ParentBond;

                        var model = new BondPropertiesModel
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

                        var pe = new BondPropertyEditor(model);
                        ShowDialog(pe, currentEditor);
                        Application.Current.ShutdownMode = mode;

                        if (model.Save)
                        {
                            controller.UpdateBond(bond, model);
                            controller.ClearSelection();

                            bond.Order = Bond.OrderValueToOrder(model.BondOrderValue);
                            controller.AddToSelection(bond);
                        }
                    }
                }
            }

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

        private static Point GetDpiAwareScaledPosition(Point screenPosition, Visual visual)
        {
            Point pp = screenPosition;

            PresentationSource source = PresentationSource.FromVisual(visual);
            if (source != null && source.CompositionTarget != null)
            {
                double dpiX = 96.0 * source.CompositionTarget.TransformToDevice.M11;
                double dpiY = 96.0 * source.CompositionTarget.TransformToDevice.M22;

                pp = new Point(pp.X * 96.0 / dpiX, pp.Y * 96.0 / dpiY);
            }

            return pp;
        }
    }
}