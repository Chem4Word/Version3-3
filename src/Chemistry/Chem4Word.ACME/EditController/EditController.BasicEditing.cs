// ---------------------------------------------------------------------------
//  Copyright (c) 2025, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.ACME.Drawing.Visuals;
using Chem4Word.Model2;
using Chem4Word.Model2.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows.Input;

namespace Chem4Word.ACME
{
    /// <summary>
    /// Key editing operations packaged in this file
    /// Much of the heavy lifting is taking part
    /// in the associated BaseEditBehaviors
    /// </summary>
    public partial class EditController
    {
        #region Methods

        /// <summary>
        /// Turns off (or on) redraw of the editor canvas
        /// </summary>
        /// <param name="state"></param>
        private void SuppressEditorRedraw(bool state)
        {
            if (EditingCanvas != null)
            {
                EditingCanvas.SuppressRedraw = state;
            }
        }

        /// <summary>
        /// Visually refreshes a list of annotations
        /// </summary>
        /// <param name="annotations"></param>
        private void RefreshAnnotations(List<Annotation> annotations)
        {
            foreach (Annotation annotation in annotations)
            {
                annotation.UpdateVisual();
            }
        }

        /// <summary>
        /// Deletes all selected items
        /// </summary>
        public void DeleteSelection()
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                WriteTelemetry(module, "Debug", "Called");

                List<Atom> atoms = SelectedItems.OfType<Atom>().ToList();
                List<Bond> bonds = SelectedItems.OfType<Bond>().ToList();
                List<Molecule> molecules = SelectedItems.OfType<Molecule>().ToList();
                List<Reaction> reactions = SelectedItems.OfType<Reaction>().ToList();
                List<Annotation> annotations = SelectedItems.OfType<Annotation>().ToList();
                UndoManager.BeginUndoBlock();

                if (molecules.Any())
                {
                    DeleteMolecules(molecules);
                }

                if (atoms.Any() || bonds.Any())
                {
                    DeleteAtomsAndBonds(atoms, bonds);
                }

                if (reactions.Any())
                {
                    DeleteReactions(reactions);
                }

                if (annotations.Any())
                {
                    DeleteAnnotations(annotations);
                }

                ClearSelection();
                UndoManager.EndUndoBlock();
            }
            catch (Exception exception)
            {
                WriteTelemetryException(module, exception);
            }

            CheckModelIntegrity(module);
            SendStatus(("Selection deleted", TotUpMolFormulae(), TotUpSelectedMwt()));
        }

        /// <summary>
        /// Deletes a list of annotations
        /// </summary>
        /// <param name="annotations"></param>
        public void DeleteAnnotations(IEnumerable<Annotation> annotations)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                WriteTelemetry(module, "Debug", "Called");

                UndoManager.BeginUndoBlock();
                foreach (Annotation a in annotations)
                {
                    IChemistryContainer parent = a.Parent;
                    Action redo = () =>
                    {
                        ClearSelection();
                        Model.RemoveAnnotation(a);
                        a.Parent = null;
                    };

                    Action undo = () =>
                    {
                        Model.AddAnnotation(a);
                        a.Parent = parent;
                        AddToSelection(a);
                    };
                    redo();
                    UndoManager.RecordAction(undo, redo);
                }

                UndoManager.EndUndoBlock();
            }
            catch (Exception exception)
            {
                WriteTelemetryException(module, exception);
            }
        }

        #endregion Methods

        #region Events

        /// <summary>
        /// End of drag event for an adorner.
        /// Handled in the controller as the adorner
        /// needs to be removed from the adorner layer
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnMouseLeftButtonDown_SelAdorner(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2) //double-click
            {
                ClearSelection();
                Molecule mol = null;
                ChemicalVisual visual = EditingCanvas.GetTargetedVisual(e.GetPosition(EditingCanvas));
                if (visual is AtomVisual av)
                {
                    mol = av.ParentAtom.Parent;
                }
                else if (visual is BondVisual bv)
                {
                    mol = bv.ParentBond.Parent;
                }

                RemoveAtomBondAdorners(mol);
                if (mol != null)
                {
                    AddToSelection(mol);
                }

                SendStatus(("Set atoms/bonds using selectors; drag to reposition; [Delete] to remove.",
                            TotUpMolFormulae(), TotUpSelectedMwt()));
            }
        }

        #endregion Events
    }
}
