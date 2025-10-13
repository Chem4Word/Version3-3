// ---------------------------------------------------------------------------
//  Copyright (c) 2025, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.ACME.Adorners.Sketching;
using Chem4Word.ACME.Controls;
using Chem4Word.ACME.Drawing.Visuals;
using Chem4Word.ACME.Utils;
using Chem4Word.Core.Helpers;
using Chem4Word.Model2;
using Chem4Word.Model2.Enums;
using Chem4Word.Model2.Geometry;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;

namespace Chem4Word.ACME.Behaviors
{
    /// <summary>
    /// Does freehand drawing of atoms and bonds
    /// </summary>
    public class DrawBehavior : BaseEditBehavior
    {
        private AtomVisual _currentAtomVisual;
        private bool IsDrawing { get; set; }

        private Snapper _angleSnapper;

        private DrawBondAdorner _adorner;

        private AtomVisual _lastAtomVisual;
        private Cursor _lastCursor;

        //used in right-click events with menus to grab the last object hit
        private Bond _lastBond;

        private Atom _lastAtom;

        protected override void OnAttached()
        {
            base.OnAttached();

            CurrentEditor = (EditorCanvas)AssociatedObject;
            _lastCursor = CurrentEditor.Cursor;
            CurrentEditor.Cursor = CursorUtils.Pencil;
            EditController.ClearSelection();

            //if you connect a new event here, you must disconnect it in OnDetaching()
            CurrentEditor.MouseLeftButtonDown += OnMouseLeftButtonDown_CurrentEditor;
            CurrentEditor.PreviewMouseLeftButtonUp += OnPreviewMouseLeftButtonUp_CurrentEditor;
            CurrentEditor.PreviewMouseMove += OnPreviewMouseMove_CurrentEditor;
            CurrentEditor.PreviewMouseRightButtonDown += OnPreviewMouseRightButtonDown_CurrentEditor;
            CurrentEditor.PreviewMouseRightButtonUp += OnPreviewMouseRightButtonUp_CurrentEditor;
            CurrentEditor.IsHitTestVisible = true;

            CurrentStatus = (AcmeConstants.DefaultDrawText, EditController.TotUpMolFormulae(), EditController.TotUpSelectedMwt());
        }

        private void OnPreviewMouseRightButtonDown_CurrentEditor(object sender, MouseButtonEventArgs e)
        {
            if (CurrentEditor.ActiveVisual != null)
            {
                if (CurrentEditor.ActiveVisual is BondVisual bv)
                {
                    _lastBond = bv.ParentBond;
                }
                else if (CurrentEditor.ActiveVisual is AtomVisual av)
                {
                    _lastAtom = av.ParentAtom;
                }
                else
                {
                    _lastBond = null;
                    _lastAtom = null;
                }
            }
        }

        private void OnPreviewMouseRightButtonUp_CurrentEditor(object sender, MouseButtonEventArgs e)
        {
            if (!(_lastAtom is null))
            {
                UIUtils.HandleAtomContextMenuClick(CurrentEditor, _lastAtom);
            }
            else if (!(_lastBond is null))
            {
                UIUtils.HandleBondContextMenuClick(CurrentEditor, _lastBond);
            }

            _lastAtom = null;
            _lastBond = null;
        }

        private void OnPreviewMouseMove_CurrentEditor(object sender, MouseEventArgs e)
        {
            Bond existingBond = null;

            if (_adorner != null)
            {
                RemoveAdorner(ref _adorner);
            }

            ChemicalVisual targetedVisual = CurrentEditor.ActiveVisual;
            string bondOrder = EditController.CurrentBondOrder;
            //check to see if we have already got an atom remembered
            if (_currentAtomVisual != null && !(_currentAtomVisual is HydrogenVisual))
            {
                Point? lastPos;

                if (Dragging(e))
                {
                    CurrentStatus = (AcmeConstants.UnlockStandardMessage, EditController.TotUpMolFormulae(), EditController.TotUpSelectedMwt());
                    //are we already on top of an atom?
                    if (targetedVisual is GroupVisual)
                    {
                        CurrentEditor.Cursor = Cursors.No;
                        lastPos = null;
                    }
                    else if (!(targetedVisual is HydrogenVisual) && targetedVisual is AtomVisual atomUnderCursor)
                    {
                        CurrentEditor.Cursor = CursorUtils.Pencil;
                        //if so. snap to the atom's position
                        lastPos = atomUnderCursor.Position;
                        //if we are stroking over an existing bond
                        //then draw a double bond adorner

                        existingBond = _lastAtomVisual.ParentAtom.BondBetween(atomUnderCursor.ParentAtom);
                        if (_lastAtomVisual != null &&
                            existingBond != null)
                        {
                            if (existingBond.Order == ModelConstants.OrderSingle)
                            {
                                bondOrder = ModelConstants.OrderDouble;
                            }
                            else if (existingBond.Order == ModelConstants.OrderDouble)
                            {
                                bondOrder = ModelConstants.OrderTriple;
                            }
                            else if (existingBond.Order == ModelConstants.OrderTriple)
                            {
                                bondOrder = ModelConstants.OrderSingle;
                            }
                        }
                    }
                    else //or dangling over free space?
                    {
                        CurrentEditor.Cursor = CursorUtils.Pencil;
                        lastPos = e.GetPosition(CurrentEditor);

                        var angleBetween =
                            Vector.AngleBetween(_lastAtomVisual?.ParentAtom?.BalancingVector() ?? GeometryTool.ScreenNorth,
                                                GeometryTool.ScreenNorth);
                        //snap a bond into position
                        lastPos = _angleSnapper.SnapBond(lastPos.Value, angleBetween);
                    }

                    if (lastPos != null)
                    {
                        if (_adorner is null)
                        {
                            _adorner = new DrawBondAdorner(CurrentEditor, AcmeConstants.BondThickness, _currentAtomVisual.Position);
                        }

                        _adorner.Stereo = EditController.CurrentStereo;
                        _adorner.BondOrder = bondOrder;
                        _adorner.ExistingBond = existingBond;

                        _adorner.EndPoint = lastPos.Value;
                        _adorner.InvalidateVisual();
                    }
                }
            }
            else
            {
                if (targetedVisual != null)
                {
                    switch (targetedVisual)
                    {
                        case ReactionVisual _:
                            CurrentStatus = (AcmeConstants.ReactionTypeStdMessage, EditController.TotUpMolFormulae(), EditController.TotUpSelectedMwt());
                            CurrentEditor.Cursor = CursorUtils.Pencil;
                            break;

                        case GroupVisual _:
                            CurrentStatus = (AcmeConstants.UngroupWarningMessage, EditController.TotUpMolFormulae(), EditController.TotUpSelectedMwt());
                            CurrentEditor.Cursor = Cursors.No;
                            break;

                        case HydrogenVisual _:
                            CurrentStatus = (AcmeConstants.DefaultRotateHydrogenMessage, "", "");
                            CurrentEditor.Cursor = Cursors.Hand;
                            break;

                        case AtomVisual av:
                            CurrentEditor.Cursor = CursorUtils.Pencil;
                            if (EditController.SelectedElement != av.ParentAtom.Element)
                            {
                                CurrentStatus = (AcmeConstants.DefaultSetElementMessage, EditController.TotUpMolFormulae(), EditController.TotUpSelectedMwt());
                            }
                            else
                            {
                                CurrentStatus = (AcmeConstants.DrawSproutChainMessage, EditController.TotUpMolFormulae(), EditController.TotUpSelectedMwt());
                            }
                            break;

                        case BondVisual _:
                            CurrentEditor.Cursor = CursorUtils.Pencil;
                            CurrentStatus = (AcmeConstants.DrawModifyBondMessage, EditController.TotUpMolFormulae(), EditController.TotUpSelectedMwt());
                            break;
                    }
                }
                else
                {
                    CurrentEditor.Cursor = CursorUtils.Pencil;
                    CurrentStatus = (AcmeConstants.DrawAtomMessage, EditController.TotUpMolFormulae(), EditController.TotUpSelectedMwt());
                }
            }
        }

        private void OnPreviewMouseLeftButtonUp_CurrentEditor(object sender, MouseButtonEventArgs e)
        {
            CurrentEditor.ReleaseMouseCapture();
            CurrentStatus = ("", "", "");
            if (_currentAtomVisual is HydrogenVisual) //just exit
            {
                return;
            }
            if (IsDrawing)
            {
                var newPos = e.GetPosition(CurrentEditor);

                //first get the current active visuals
                ChemicalVisual targetedVisual = CurrentEditor.GetTargetedVisual(newPos);
                var landedGroupVisual = targetedVisual as GroupVisual;
                var landedAtomVisual = targetedVisual as AtomVisual;
                var landedBondVisual = targetedVisual as BondVisual;
                if (landedAtomVisual is HydrogenVisual) //just exit
                {
                    return;
                }

                //check to see whether or not we've clicked and released on the same atom
                bool sameAtom = landedAtomVisual == _currentAtomVisual;

                //check to see whether the target is in the same molecule
                bool sameMolecule = landedAtomVisual?.ParentAtom.Parent == _currentAtomVisual?.ParentAtom.Parent;

                if (landedGroupVisual != null)
                {
                    ClearTemporaries();
                    return;
                }

                //check bonds first - we can't connect to a bond so we need to simply do some stuff with it
                if (landedBondVisual != null)
                {
                    //clicking on a stereo bond should just invert it
                    var parentBond = landedBondVisual.ParentBond;
                    if (parentBond.Stereo == BondStereo.Hatch && EditController.CurrentStereo == BondStereo.Hatch
                        || parentBond.Stereo == BondStereo.Wedge && EditController.CurrentStereo == BondStereo.Wedge)
                    {
                        EditController.SwapBondDirection(parentBond);
                    }
                    else
                    {
                        //modify the bond attribute (order, stereo, whatever's selected really)
                        EditController.SetBondAttributes(parentBond);
                    }
                }
                else //we clicked on empty space or an atom
                {
                    Atom parentAtom = _currentAtomVisual?.ParentAtom;
                    if (landedAtomVisual == null) //no atom hit
                    {
                        if (parentAtom != null)
                        {
                            //so just sprout a chain off it at two-o-clock
                            EditController.AddAtomChain(
                                parentAtom, _angleSnapper.SnapBond(newPos),
                                ClockDirections.II);
                        }
                        else
                        {
                            //otherwise create a singleton
                            EditController.AddAtomChain(null, newPos, ClockDirections.II);
                        }
                    }
                    else //we went mouse-up on an atom
                    {
                        Atom lastAtom = landedAtomVisual.ParentAtom;
                        if (sameAtom) //both are the same atom
                        {
                            if (lastAtom.Element.Symbol != EditController.SelectedElement.Symbol)
                            {
                                EditController.SetElement(EditController.SelectedElement, new List<Atom> { lastAtom });
                            }
                            else
                            {
                                (Point point, ClockDirections sproutDir) = GetNewChainEndPos(landedAtomVisual);
                                EditController.AddAtomChain(lastAtom, point, sproutDir);
                                parentAtom.UpdateVisual();
                            }
                        }
                        else //we must have hit a different atom altogether
                        {
                            if (parentAtom != null)
                            {
                                //already has a bond to the target atom
                                var existingBond = parentAtom.BondBetween(lastAtom);
                                if (existingBond != null) //it must be in the same molecule
                                {
                                    EditController.IncreaseBondOrder(existingBond);
                                }
                                else //doesn't have a bond to the target atom
                                {
                                    if (sameMolecule)
                                    {
                                        EditController.AddNewBond(parentAtom, lastAtom, parentAtom.Parent);
                                    }
                                    else
                                    {
                                        EditController.JoinMolecules(parentAtom, lastAtom,
                                                                    EditController.CurrentBondOrder,
                                                                    EditController.CurrentStereo);
                                    }
                                    parentAtom.UpdateVisual();
                                    lastAtom.UpdateVisual();
                                }
                            }
                        }
                    }
                }
            }

            ClearTemporaries();
        }

        public override void Abort()
        {
            CurrentEditor.ReleaseMouseCapture();
            CurrentStatus = ("", "", "");
            ClearTemporaries();
        }

        private void ClearTemporaries()
        {
            if (_adorner != null)
            {
                RemoveAdorner(ref _adorner);
            }

            _currentAtomVisual = null;
            IsDrawing = false;
            //clear this to prevent a weird bug in drawing
            CurrentEditor.ActiveChemistry = null;
            CurrentEditor.Focus();
        }

        private void RemoveAdorner(ref DrawBondAdorner adorner)
        {
            var layer = AdornerLayer.GetAdornerLayer(CurrentEditor);

            layer.Remove(adorner);
            adorner = null;
        }

        /// <summary>
        /// tells you where to put a new atom
        /// </summary>
        /// <param name="lastAtomVisual"></param>
        /// <returns></returns>
        private (Point NewPos, ClockDirections sproutDir) GetNewChainEndPos(AtomVisual lastAtomVisual)
        {
            var lastAtom = lastAtomVisual.ParentAtom;
            Vector newDirection;

            ClockDirections newTag;
            var newBondLength = EditController.CurrentBondLength * ModelConstants.ScaleFactorForXaml;
            if (lastAtom.Degree == 0) //isolated atom
            {
                newDirection = ClockDirections.II.ToVector() * newBondLength;
                newTag = ClockDirections.II;
            }
            else if (lastAtom.Degree == 1)
            {
                var bondVector = lastAtom.Position - lastAtom.Neighbours.First().Position;

                var hour = SnapToHour(bondVector);

                if (VirginAtom(lastAtom)) //it hasn't yet sprouted
                {
                    //Tag is used to store the direction the atom sprouted from its previous atom
                    newTag = GetNewSproutDirection(hour);
                    newDirection = newTag.ToVector() * newBondLength;
                }
                else //it has sprouted, so where to put the new branch?
                {
                    var vecA = ((ClockDirections)lastAtom.Tag).ToVector();
                    vecA.Normalize();
                    var vecB = -bondVector;
                    vecB.Normalize();

                    var balancingVector = -(vecA + vecB);
                    balancingVector.Normalize();
                    newTag = SnapToHour(balancingVector);
                    newDirection = balancingVector * newBondLength;
                }
            }
            else
            {
                var balancingVector = lastAtom.BalancingVector();
                newDirection = balancingVector * newBondLength;
                newTag = SnapToHour(balancingVector);
            }

            return (newDirection + lastAtom.Position, newTag);

            //local function
            ClockDirections SnapToHour(Vector bondVector)
            {
                var bondAngle = Vector.AngleBetween(GeometryTool.ScreenNorth, bondVector);
                return (ClockDirections)GeometryTool.SnapToClock(bondAngle);
            }
        }

        private bool VirginAtom(Atom lastAtom)
        {
            return lastAtom.Tag == null;
        }

        private static ClockDirections GetNewSproutDirection(ClockDirections hour)
        {
            ClockDirections newTag;
            switch (hour)
            {
                case ClockDirections.I:
                    newTag = ClockDirections.III;
                    break;

                case ClockDirections.II:
                    newTag = ClockDirections.IV;
                    break;

                case ClockDirections.III:
                    newTag = ClockDirections.II;
                    break;

                case ClockDirections.IV:
                    newTag = ClockDirections.II;
                    break;

                case ClockDirections.V:
                    newTag = ClockDirections.III;
                    break;

                case ClockDirections.VI:
                    newTag = ClockDirections.VIII;
                    break;

                case ClockDirections.VII:
                    newTag = ClockDirections.IX;
                    break;

                case ClockDirections.VIII:
                    newTag = ClockDirections.X;
                    break;

                case ClockDirections.IX:
                    newTag = ClockDirections.XI;
                    break;

                case ClockDirections.X:
                    newTag = ClockDirections.VIII;
                    break;

                case ClockDirections.XII:
                    newTag = ClockDirections.I;
                    break;

                default:
                    newTag = ClockDirections.II;
                    break;
            }
            return newTag;
        }

        private void OnMouseLeftButtonDown_CurrentEditor(object sender, MouseButtonEventArgs e)
        {
            var position = e.GetPosition(CurrentEditor);
            ChemicalVisual chemicalVisual = CurrentEditor.GetTargetedVisual(position);
            if (chemicalVisual is ReactionVisual rv) //we hit a reaction
            {
                //only bother modifying if the selected reaction type is different
                if (EditController.SelectedReactionType.Value != rv.ParentReaction.ReactionType)
                {
                    EditController.SetReactionType(EditController.SelectedReactionType.Value, rv.ParentReaction);
                }
            }
            else
            {
                _currentAtomVisual = chemicalVisual as AtomVisual;
                IsDrawing = true;

                if (_currentAtomVisual is null)
                {
                    _angleSnapper = new Snapper(position, EditController);
                }
                else if (!(_currentAtomVisual is HydrogenVisual))
                {
                    _angleSnapper = new Snapper(_currentAtomVisual.ParentAtom.Position, EditController);
                    Mouse.Capture(CurrentEditor);
                    _lastAtomVisual = _currentAtomVisual;
                }
                else //its a hydrogen visual
                {
                    HydrogenVisual hv = (HydrogenVisual)_currentAtomVisual;
                    EditController.RotateHydrogen(hv.ParentVisual.ParentAtom);
                    IsDrawing = false;  //stops drop of an isolated atom
                    e.Handled = true;
                }
            }
        }

        private bool Dragging(MouseEventArgs e)
        {
            return e.LeftButton == MouseButtonState.Pressed & IsDrawing;
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            CurrentEditor.MouseLeftButtonDown -= OnMouseLeftButtonDown_CurrentEditor;
            CurrentEditor.PreviewMouseLeftButtonUp -= OnPreviewMouseLeftButtonUp_CurrentEditor;
            CurrentEditor.PreviewMouseMove -= OnPreviewMouseMove_CurrentEditor;
            CurrentEditor.PreviewMouseRightButtonDown -= OnPreviewMouseRightButtonDown_CurrentEditor;
            CurrentEditor.PreviewMouseRightButtonUp -= OnPreviewMouseRightButtonUp_CurrentEditor;
            CurrentStatus = ("", "", "");
            CurrentEditor.Cursor = _lastCursor;
        }
    }
}
