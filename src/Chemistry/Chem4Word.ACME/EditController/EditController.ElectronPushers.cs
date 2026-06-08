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
        /// <param name="shiftIsDown"></param>
        public void AddElectronPusher(StructuralObject startChemistry, StructuralObject targetChemistry,
                                      ElectronPusherType electronPusherType, Point firstControlPoint,
                                      Point secondControlPoint, bool shiftIsDown)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            ElectronPusher newPusher = new ElectronPusher
            {
                StartChemistry = startChemistry,
                FirstControlPoint = firstControlPoint,
                SecondControlPoint = secondControlPoint,
                PusherType = electronPusherType
            };

            try
            {
                WriteTelemetry(module, "Information", $"Adding Electron Pusher starting at '{startChemistry.Path}'");

                //check to see if we're forming a new bond between atoms
                if (startChemistry is Atom startAtom
                    && targetChemistry is Atom endAtom
                    && startAtom.BondBetween(endAtom) is null)
                {
                    if (shiftIsDown)
                    //we're forming a new bond so the electron pusher ends in empty space
                    //between the two atoms
                    {
                        newPusher.EndChemistries.Add(startAtom);
                    }
                    newPusher.EndChemistries.Add(endAtom);
                }
                else if (startChemistry is Electron electron
                         && targetChemistry is Atom endAtom1
                         && (electron.Parent as Atom).BondBetween(endAtom1) is null)
                {
                    //we're forming a new bond so the electron pusher ends in empty space
                    //between the two atoms
                    if (shiftIsDown)
                    {
                        newPusher.EndChemistries.Add(electron.Parent);
                    }
                    newPusher.EndChemistries.Add(endAtom1);
                }
                else if (startChemistry is Bond startBond
                         && targetChemistry is Atom endAtom2
                         && !startBond.GetAtoms().Contains(endAtom2))
                {
                    newPusher.EndChemistries.Add(endAtom2);
                    //if source and target are in different molecules
                    //then we are forming a nascent bond
                    if (shiftIsDown)
                    {
                        //add the closest of the two atoms to the external atom in the bond
                        if ((startBond.StartAtom.Position - endAtom2.Position).Length
                            < (startBond.EndAtom.Position - endAtom2.Position).Length)
                        {
                            newPusher.EndChemistries.Add(startBond.StartAtom);
                        }
                        else
                        {
                            newPusher.EndChemistries.Add(startBond.EndAtom);
                        }
                    }

                    (newPusher.FirstControlPoint, newPusher.SecondControlPoint) = ElectronPusherDrawAdorner.RecalcControlPoints(newPusher, Model.MeanBondLength);
                }
                else
                {
                    newPusher.EndChemistries.Add(targetChemistry);
                }

                WriteTelemetry(module, "Information",
                               newPusher.EndChemistries.Count == 1
                                   ? $"Electron Pusher EndChemistries[0] is '{newPusher.EndChemistriesAsString()}'"
                                   : $"Electron Pusher EndChemistries are '{newPusher.EndChemistriesAsString()}'");

                Action redo = () =>
                              {
                                  Model.AddElectronPusher(newPusher);
                                  newPusher.Parent = Model;
                              };
                Action undo = () =>
                              {
                                  ClearSelection();
                                  Model.RemoveElectronPusher(newPusher);
                                  newPusher.Parent = null;
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
                WriteTelemetry(module, "Information", $"Deleting Electron Pusher {pusher.Path}");

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
                UndoManager.RecordAction(undo, redo);
                UndoManager.EndUndoBlock();
                redo();
            }
            catch (Exception exception)
            {
                WriteTelemetryException(module, exception);
            }
        }

        private void TransformRelatedPushers(List<Bond> moleculeBonds, Transform transform)
        {
            foreach (ElectronPusher ep in Model.ElectronPushers.Values)
            {
                if (moleculeBonds.Contains(ep.StartChemistry) || ep.EndChemistries.Any(c => moleculeBonds.Contains(c)))
                {
                    ep.FirstControlPoint = transform.Transform(ep.FirstControlPoint);
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
