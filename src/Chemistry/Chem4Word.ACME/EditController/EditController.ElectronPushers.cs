// ---------------------------------------------------------------------------
//  Copyright (c) 2026, The .NET Foundation.
//  This software is released under the Apache Licence, Version 2.0.
//  The licence and further copyright text can be found in the file LICENCE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.ACME.Adorners.Sketching;
using Chem4Word.Model2;
using Chem4Word.Model2.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Media;

namespace Chem4Word.ACME
{
    public partial class EditController
    {
        /// <summary>
        /// Adds an electron pusher to the model
        /// </summary>
        /// <param name="startChemistry">Atom or bond</param>
        /// <param name="targetChemistry">Atom or bond. If not directly connected then special case applies</param>
        /// <param name="electronPusherType"></param>
        /// <param name="firstControlPoint"></param>
        /// <param name="secondControlPoint"></param>
        public void AddElectronPusher(StructuralObject startChemistry, StructuralObject targetChemistry,
                                      ElectronPusherType electronPusherType, Point firstControlPoint,
                                      Point secondControlPoint)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            ElectronPusher ep = new ElectronPusher
            {
                StartChemistry = startChemistry,
                FirstControlPoint = firstControlPoint,
                SecondControlPoint = secondControlPoint,
                PusherType = electronPusherType
            };
            try
            {
                Point startPoint, endPoint;

                WriteTelemetry(module, "Debug", $"Adding Electron Pusher ");

                //check to see if we're forming a new bond between atoms
                if (startChemistry is Atom startAtom && targetChemistry is Atom endAtom &&
                    startAtom.BondBetween(endAtom) is null)
                {
                    //we're forming a new bond so the electron pusher ends in empty space
                    //between the two atoms
                    ep.EndChemistries.Add(startAtom);
                    ep.EndChemistries.Add(endAtom);
                    (startPoint, endPoint, ep.FirstControlPoint, ep.SecondControlPoint) = ElectronPusherDrawAdorner.RecalcControlPoints(ep, Model.MeanBondLength);
                }
                else if (startChemistry is Bond startBond && targetChemistry is Atom endAtom2 &&
                         !startBond.GetAtoms().Contains(endAtom2))
                {
                    ep.EndChemistries.Add(endAtom2);
                    //add the closest of the two atoms to the external atom in the bond
                    if ((startBond.StartAtom.Position - endAtom2.Position).Length <
                        (startBond.EndAtom.Position - endAtom2.Position).Length)
                    {
                        ep.EndChemistries.Add(startBond.StartAtom);
                    }
                    else
                    {
                        ep.EndChemistries.Add(startBond.EndAtom);
                    }

                    (startPoint, endPoint, ep.FirstControlPoint, ep.SecondControlPoint) = ElectronPusherDrawAdorner.RecalcControlPoints(ep, Model.MeanBondLength);
                }
                else
                {
                    ep.EndChemistries.Add(targetChemistry);
                }

                Action redo = () =>
                              {
                                  Model.AddElectronPusher(ep);
                                  ep.Parent = Model;
                              };
                Action undo = () =>
                              {
                                  Model.RemoveElectronPusher(ep);
                                  ep.Parent = null;
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

        public void DeleteElectronPushers(List<ElectronPusher> electronPushers)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                WriteTelemetry(module, "Debug", "Called");

                UndoManager.BeginUndoBlock();

                foreach (ElectronPusher pusher in electronPushers)
                {
                    DeleteElectronPusher(pusher);
                }

                UndoManager.EndUndoBlock();
            }
            catch (Exception exception)
            {
                WriteTelemetryException(module, exception);
            }
        }

        private void DeleteElectronPusher(ElectronPusher pusher)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                WriteTelemetry(module, "Debug", "Called");

                UndoManager.BeginUndoBlock();

                Action redo = () =>
                              {
                                  ClearSelection();
                                  Model.RemoveElectronPusher(pusher);
                                  pusher.Parent = null;
                              };

                Action undo = () =>
                              {
                                  Model.AddElectronPusher(pusher);
                                  pusher.Parent = Model;
                                  AddToSelection(pusher);
                              };
                redo();
                UndoManager.RecordAction(undo, redo);
                UndoManager.EndUndoBlock();
            }
            catch (Exception exception)
            {
                WriteTelemetryException(module, exception);
            }
        }

        private void TransformAllElectronPushers(Molecule molecule, Transform transform)
        {
            TransformAtomRelatedPushers(molecule.Atoms.Values.ToList(), transform);
            TransformBondRelatedPushers(molecule.Bonds.ToList(), transform);
        }

        private void TransformBondRelatedPushers(List<Bond> moleculeBonds, Transform transform)
        {
            foreach (ElectronPusher ep in Model.ElectronPushers.Values)
            {
                if (moleculeBonds.Contains(ep.StartChemistry))
                {
                    ep.FirstControlPoint = transform.Transform(ep.FirstControlPoint);
                    ep.UpdateVisual();
                }
                if (ep.EndChemistries.Any(c => moleculeBonds.Contains(c)))
                {
                    ep.SecondControlPoint = transform.Transform(ep.SecondControlPoint);
                    ep.UpdateVisual();
                }
            }
        }

        private void TransformAtomRelatedPushers(List<Atom> atomsValues, Transform transform)
        {
            foreach (ElectronPusher ep in Model.ElectronPushers.Values)
            {
                if (atomsValues.Contains(ep.StartChemistry))
                {
                    ep.FirstControlPoint = transform.Transform(ep.FirstControlPoint);
                }
                if (ep.EndChemistries.Any(c => atomsValues.Contains(c)))
                {
                    ep.SecondControlPoint = transform.Transform(ep.SecondControlPoint);
                }
                ep.UpdateVisual();
            }
        }

        public void EndElectronPusherResize(ElectronPusher parentPusher, Point firstControlPointTemp, Point secondControlPointTemp)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            Point old1stControlPoint = parentPusher.FirstControlPoint;
            Point old2ndControlPoint = parentPusher.SecondControlPoint;
            try
            {
                WriteTelemetry(module, "Debug", "Called");

                UndoManager.BeginUndoBlock();

                Action redo = () =>
                              {
                                  ClearSelection();
                                  parentPusher.FirstControlPoint = firstControlPointTemp;
                                  parentPusher.SecondControlPoint = secondControlPointTemp;
                                  parentPusher.UpdateVisual();
                                  AddToSelection(parentPusher);
                              };

                Action undo = () =>
                              {
                                  ClearSelection();
                                  parentPusher.FirstControlPoint = old1stControlPoint;
                                  parentPusher.SecondControlPoint = old2ndControlPoint;
                                  parentPusher.UpdateVisual();
                                  AddToSelection(parentPusher);
                              };
                redo();
                UndoManager.RecordAction(undo, redo);
                UndoManager.EndUndoBlock();
            }
            catch (Exception exception)
            {
                WriteTelemetryException(module, exception);
            }
        }
    }
}
