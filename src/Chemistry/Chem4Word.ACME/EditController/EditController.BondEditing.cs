// ---------------------------------------------------------------------------
//  Copyright (c) 2026, The .NET Foundation.
//  This software is released under the Apache Licence, Version 2.0.
//  The licence and further copyright text can be found in the file LICENCE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.ACME.Commands.PropertyEdit;
using Chem4Word.ACME.Enums;
using Chem4Word.ACME.Models;
using Chem4Word.Model2;
using Chem4Word.Model2.Enums;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Media;

namespace Chem4Word.ACME
{
    public partial class EditController
    {
        #region Fields

        private FlipBondStereoCommand _flipBondStereoCommand;

        #endregion Fields

        #region Properties

        /// <summary>
        /// Flips the stereo of the selected bond(s)
        /// </summary>
        public FlipBondStereoCommand FlipBondStereoCommand
        {
            get
            {
                return _flipBondStereoCommand;
            }
            set
            {
                _flipBondStereoCommand = value;
                OnPropertyChanged();
            }
        }

        #endregion Properties

        #region Methods

        /// <summary>
        /// Sets the bond option (order and stereo) for the selected bonds
        /// </summary>
        /// <param name="bondOptionId"></param>
        /// <param name="selectedBonds"></param>
        public void SetBondOption(int? bondOptionId, Bond[] selectedBonds)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            if (bondOptionId is null)
            {
                bondOptionId = _selectedBondOptionId;
            }

            try
            {
                if (bondOptionId != null)
                {
                    BondOption bondOption = _bondOptions[bondOptionId.Value];
                    string stereoValue = bondOption.Stereo.HasValue ? bondOption.Stereo.Value.ToString() : "{null}";
                    int affectedBonds = selectedBonds.Count();
                    WriteTelemetry(module, "Debug",
                                   $"Order: {bondOption.Order}; Stereo: {stereoValue}; Affected Bonds: {affectedBonds}");

                    if (selectedBonds.Any())
                    {
                        UndoManager.BeginUndoBlock();
                        foreach (Bond bond in selectedBonds)
                        {
                            Action redo = () =>
                                          {
                                              bond.Stereo = bondOption.Stereo.Value;
                                              bond.Order = bondOption.Order;
                                              bond.UpdateVisual();
                                              bond.StartAtom.UpdateVisual();
                                              bond.EndAtom.UpdateVisual();
                                          };

                            BondStereo bondStereo = bond.Stereo;
                            string bondOrder = bond.Order;

                            Action undo = () =>
                                          {
                                              bond.Stereo = bondStereo;
                                              bond.Order = bondOrder;
                                              bond.UpdateVisual();
                                              bond.StartAtom.UpdateVisual();
                                              bond.EndAtom.UpdateVisual();
                                          };

                            UndoManager.RecordAction(undo, redo);
                            bond.Order = bondOption.Order;
                            bond.Stereo = bondOption.Stereo.Value;
                            bond.UpdateVisual();
                            bond.StartAtom.UpdateVisual();
                            bond.EndAtom.UpdateVisual();
                        }

                        ClearSelection();
                        UndoManager.EndUndoBlock();
                    }
                }
                else
                {
                    WriteTelemetry(module, "Warning", "_selectedBondOptionId is {null}");
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
        /// Ups the bond order of the specified bond.
        /// Generally used in 'wiping over' an existing bond with the bond tool
        /// </summary>
        /// <param name="existingBond"></param>
        public void IncreaseBondOrder(Bond existingBond)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                WriteTelemetry(module, "Debug", "Called");

                BondStereo stereo = existingBond.Stereo;
                string order = existingBond.Order;

                Action redo = () =>
                              {
                                  existingBond.Stereo = BondStereo.None;
                                  switch (existingBond.Order)
                                  {
                                      case ModelConstants.OrderZero:
                                          existingBond.Order = ModelConstants.OrderSingle;
                                          break;

                                      case ModelConstants.OrderSingle:
                                          existingBond.Order = ModelConstants.OrderDouble;
                                          break;

                                      case ModelConstants.OrderDouble:
                                          existingBond.Order = ModelConstants.OrderTriple;
                                          break;

                                      case ModelConstants.OrderTriple:
                                          existingBond.Order = ModelConstants.OrderSingle;
                                          break;
                                  }

                                  existingBond.StartAtom.UpdateVisual();
                                  existingBond.EndAtom.UpdateVisual();
                                  existingBond.UpdateVisual();
                              };
                Action undo = () =>
                              {
                                  existingBond.Stereo = stereo;
                                  existingBond.Order = order;

                                  existingBond.StartAtom.UpdateVisual();
                                  existingBond.EndAtom.UpdateVisual();
                                  existingBond.UpdateVisual();
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
        /// Sets average bond length for the model
        /// </summary>
        /// <param name="newLength"></param>
        public void SetAverageBondLength(double newLength)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                WriteTelemetry(module, "Debug", $"Length {newLength / ModelConstants.ScaleFactorForXaml}");

                double currentLength = Model.MeanBondLength;
                double currentSelection = Math.Round(currentLength / ModelConstants.ScaleFactorForXaml / 5.0) * 5 *
                                          ModelConstants.ScaleFactorForXaml;

                Point centre = new Point(Model.BoundingBoxWithFontSize.Left + Model.BoundingBoxWithFontSize.Width / 2,
                                         Model.BoundingBoxWithFontSize.Top + Model.BoundingBoxWithFontSize.Height / 2);

                Action redo = () =>
                              {
                                  Model.ScaleToAverageBondLength(newLength, centre);
                                  SetTextParams(newLength);
                                  Model.SetAnnotationSize(newLength / ModelConstants.ScaleFactorForXaml);
                                  RefreshMolecules(Model.Molecules.Values.ToList());
                                  RefreshReactions(Model.DefaultReactionScheme.Reactions.Values.ToList());
                                  RefreshAnnotations(Model.Annotations.Values.ToList());
                                  Loading = true;
                                  CurrentBondLength = newLength / ModelConstants.ScaleFactorForXaml;
                                  Loading = false;
                              };
                Action undo = () =>
                              {
                                  Model.ScaleToAverageBondLength(currentLength, centre);
                                  SetTextParams(currentSelection);
                                  Model.SetAnnotationSize(currentSelection / ModelConstants.ScaleFactorForXaml);
                                  RefreshMolecules(Model.Molecules.Values.ToList());
                                  RefreshReactions(Model.DefaultReactionScheme.Reactions.Values.ToList());
                                  RefreshAnnotations(Model.Annotations.Values.ToList());
                                  Loading = true;
                                  CurrentBondLength = currentSelection / ModelConstants.ScaleFactorForXaml;
                                  Loading = false;
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
        /// Deletes a list of bonds
        /// </summary>
        /// <param name="bonds"></param>
        public void DeleteBonds(IEnumerable<Bond> bonds)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                string countString = bonds == null ? "{null}" : $"{bonds.Count()}";
                WriteTelemetry(module, "Debug", $"Bonds {countString}");

                DeleteAtomsAndBonds(bondList: bonds);
            }
            catch (Exception exception)
            {
                WriteTelemetryException(module, exception);
            }
            finally
            {
                CheckModelIntegrity(module);
            }
        }

        #endregion Methods

        #region Methods

        public void UpdateBond(Bond bond, BondPropertiesModel model)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                WriteTelemetry(module, "Debug", "Called");

                double bondOrderBefore = bond.OrderValue.Value;
                BondStereo stereoBefore = bond.Stereo;
                BondDirection? directionBefore = bond.ExplicitPlacement;

                double bondOrderAfter = model.BondOrderValue;
                BondStereo stereoAfter = BondStereo.None;
                BondDirection? directionAfter = null;

                Atom startAtom = bond.StartAtom;
                Atom endAtom = bond.EndAtom;

                bool swapAtoms = false;

                if (model.IsSingle)
                {
                    switch (model.SingleBondChoice)
                    {
                        case SingleBondType.None:
                            stereoAfter = BondStereo.None;
                            break;

                        case SingleBondType.Wedge:
                            stereoAfter = BondStereo.Wedge;
                            break;

                        case SingleBondType.BackWedge:
                            stereoAfter = BondStereo.Wedge;
                            swapAtoms = true;
                            break;

                        case SingleBondType.Hatch:
                            stereoAfter = BondStereo.Hatch;
                            break;

                        case SingleBondType.BackHatch:
                            stereoAfter = BondStereo.Hatch;
                            swapAtoms = true;
                            break;

                        case SingleBondType.Indeterminate:
                            stereoAfter = BondStereo.Indeterminate;
                            break;

                        case SingleBondType.Thick:
                            stereoAfter = BondStereo.Thick;
                            break;

                        default:
                            stereoAfter = BondStereo.None;
                            break;
                    }
                }

                if (model.Is1Point5 || model.Is2Point5 || model.IsDouble)
                {
                    if (model.DoubleBondChoice == DoubleBondType.Indeterminate)
                    {
                        stereoAfter = BondStereo.Indeterminate;
                    }
                    else
                    {
                        stereoAfter = BondStereo.None;
                        if (model.DoubleBondChoice != DoubleBondType.Auto)
                        {
                            directionAfter = (BondDirection)model.DoubleBondChoice;
                        }
                    }
                }

                Molecule mol = bond.Parent;
                RotateTransform transform = null;
                GeneralTransform inverse = null;
                bool singleBondTransform = false;
                Atom rotatedAtom = null;

                if (double.TryParse(model.BondAngle, out double angle))
                {
                    if (angle >= -180 && angle <= 180)
                    {
                        double rotateBy = angle - bond.Angle;

                        if (Math.Abs(rotateBy) >= 0.005)
                        {
                            int startAtomBondCount = startAtom.Bonds.Count();
                            int endAtomBondCount = endAtom.Bonds.Count();

                            if (startAtomBondCount == 1 || endAtomBondCount == 1)
                            {
                                singleBondTransform = true;
                                if (startAtomBondCount == 1)
                                {
                                    transform = new RotateTransform(rotateBy, endAtom.Position.X, endAtom.Position.Y);
                                    rotatedAtom = startAtom;
                                    inverse = transform.Inverse;
                                }

                                if (endAtomBondCount == 1)
                                {
                                    transform = new RotateTransform(rotateBy, startAtom.Position.X,
                                                                    startAtom.Position.Y);
                                    rotatedAtom = endAtom;
                                    inverse = transform.Inverse;
                                }
                            }
                            else
                            {
                                Point centroid = mol.Centroid;
                                transform = new RotateTransform(rotateBy, centroid.X, centroid.Y);
                                inverse = transform.Inverse;
                            }
                        }
                    }
                }

                Action redo = () =>
                              {
                                  bond.Order = Bond.OrderValueToOrder(bondOrderAfter);
                                  bond.Stereo = stereoAfter;
                                  bond.ExplicitPlacement = directionAfter;
                                  bond.Parent.UpdateVisual();
                                  if (swapAtoms)
                                  {
                                      bond.EndAtomInternalId = startAtom.InternalId;
                                      bond.StartAtomInternalId = endAtom.InternalId;
                                  }

                                  bond.UpdateVisual();

                                  if (transform != null)
                                  {
                                      if (singleBondTransform && rotatedAtom != null)
                                      {
                                          rotatedAtom.Position = transform.Transform(rotatedAtom.Position);
                                          rotatedAtom.UpdateVisual();
                                      }
                                      else
                                      {
                                          Transform(mol, transform);
                                          mol.UpdateVisual();
                                      }

                                      ClearSelection();
                                  }
                              };

                Action undo = () =>
                              {
                                  bond.Order = Bond.OrderValueToOrder(bondOrderBefore);
                                  bond.Stereo = stereoBefore;
                                  bond.ExplicitPlacement = directionBefore;
                                  bond.Parent.UpdateVisual();
                                  if (swapAtoms)
                                  {
                                      bond.StartAtomInternalId = startAtom.InternalId;
                                      bond.EndAtomInternalId = endAtom.InternalId;
                                  }

                                  bond.UpdateVisual();

                                  if (inverse != null)
                                  {
                                      if (singleBondTransform && rotatedAtom != null)
                                      {
                                          rotatedAtom.Position = inverse.Transform(rotatedAtom.Position);
                                          rotatedAtom.UpdateVisual();
                                      }
                                      else
                                      {
                                          Transform(mol, (Transform)inverse);
                                          mol.UpdateVisual();
                                      }

                                      ClearSelection();
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
    }
}
