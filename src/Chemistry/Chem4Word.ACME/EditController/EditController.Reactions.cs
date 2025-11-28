// ---------------------------------------------------------------------------
//  Copyright (c) 2026, The .NET Foundation.
//  This software is released under the Apache Licence, Version 2.0.
//  The licence and further copyright text can be found in the file LICENCE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.ACME.Commands.Reactions;
using Chem4Word.ACME.Controls;
using Chem4Word.ACME.Drawing.Visuals;
using Chem4Word.ACME.Enums;
using Chem4Word.Model2;
using Chem4Word.Model2.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;

namespace Chem4Word.ACME
{
    public partial class EditController
    {
        #region Fields

        private bool _selectionIsSubscript;
        private bool _selectionIsSuperscript;

        private AnnotationEditor _annotationEditor;
        private bool _isBlockEditing;

        #endregion Fields

        #region Properties

        /// <summary>
        /// Tests for subscript in reactants/conditions block editor
        /// </summary>
        public bool SelectionIsSubscript
        {
            get
            {
                return _selectionIsSubscript;
            }
            set
            {
                _selectionIsSubscript = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Tests for superscript in reactants/conditions block editor
        /// </summary>
        public bool SelectionIsSuperscript
        {
            get
            {
                return _selectionIsSuperscript;
            }
            set
            {
                _selectionIsSuperscript = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Puts the reagents field into edit mode
        /// </summary>
        public EditReagentsCommand EditReagentsCommand
        {
            get
            {
                return _editReagentsCommand;
            }
            set
            {
                _editReagentsCommand = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Puts the conditions field into edit mode
        /// </summary>
        public EditConditionsCommand EditConditionsCommand
        {
            get
            {
                return _editConditionsCommand;
            }
            set
            {
                _editConditionsCommand = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Assigns roles to molecules involved in a reaction.
        /// Ensure that all molecules involved in the reaction are selected
        /// along with the reaction arrow.
        /// </summary>
        public AssignReactionRolesCommand AssignReactionRolesCommand
        {
            get
            {
                return _assignReactionRolesCommand;
            }
            set
            {
                _assignReactionRolesCommand = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Clears any assigned roles from molecules involved in a reaction.
        /// </summary>
        public ClearReactionRolesCommand ClearReactionRolesCommand
        {
            get
            {
                return _clearReactionRolesCommand;
            }
            set
            {
                _clearReactionRolesCommand = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Only molecules sitting in the zone behind the tail of the arrow
        /// are considered reactants
        /// </summary>
        public List<Molecule> ReactantsInSelection
        {
            get
            {
                List<Molecule> reactants = new List<Molecule>();
                List<Molecule> molsSelected = SelectedItems.OfType<Molecule>().ToList();
                Reaction reactionSelected = SelectedItems.OfType<Reaction>().ToList()[0];

                (Point start, Point end) = GetReactionStartAndEnd(reactionSelected);

                foreach (Molecule mol in molsSelected)
                {
                    //the angle test ensures that only molecules 'in front of' the start point are considered reactants
                    if ((mol.Centroid - start).Length < (mol.Centroid - end).Length
                        && Math.Abs(Vector.AngleBetween(mol.Centroid - start, end - start)) > 90)
                    {
                        reactants.Add(mol);
                    }
                }

                return reactants;
            }
        }

        /// <summary>
        /// Helper function to determine start and end points of reaction based on type
        /// Retrosynthetic functions are 'backwards' hence the swap
        /// </summary>
        /// <param name="reactionSelected"></param>
        /// <returns></returns>
        private static (Point start, Point end) GetReactionStartAndEnd(Reaction reactionSelected)
        {
            Point start;
            Point end;
            if (reactionSelected.ReactionType != ReactionType.Retrosynthetic)
            {
                start = reactionSelected.TailPoint;
                end = reactionSelected.HeadPoint;
            }
            else
            {
                end = reactionSelected.TailPoint;
                start = reactionSelected.HeadPoint;
            }

            return (start, end);
        }

        /// <summary>
        /// Only molecules sitting in the zone beyond the head of the arrow
        /// are considered products
        /// </summary>
        public List<Molecule> ProductsInSelection
        {
            get
            {
                List<Molecule> products = new List<Molecule>();
                List<Molecule> molsSelected = SelectedItems.OfType<Molecule>().ToList();
                Reaction reactionSelected = SelectedItems.OfType<Reaction>().ToList()[0];

                (Point start, Point end) = GetReactionStartAndEnd(reactionSelected);

                foreach (Molecule mol in molsSelected)
                {
                    //the angle test ensures that only molecules 'behind' the end point are considered products
                    if ((mol.Centroid - end).Length < (mol.Centroid - start).Length
                        && Math.Abs(Vector.AngleBetween(mol.Centroid - end, start - end)) > 90d)
                    {
                        products.Add(mol);
                    }
                }

                return products;
            }
        }

        /// <summary>
        /// Grabs the active block editor
        /// </summary>
        public AnnotationEditor ActiveBlockEditor
        {
            get
            {
                return _annotationEditor;
            }
            set
            {
                _annotationEditor = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Are we currently editing a block (reagents/conditions)
        /// </summary>
        public bool IsBlockEditing
        {
            get
            {
                return _isBlockEditing;
            }
            set
            {
                _isBlockEditing = value;
                OnPropertyChanged();
            }
        }

        #endregion Properties

        #region Methods

        /// <summary>
        /// Moves a reaction from the start to the end point. Undoable.
        /// </summary>
        /// <param name="reaction"></param>
        /// <param name="startPoint"></param>
        /// <param name="endPoint"></param>
        public void MoveReaction(Reaction reaction, Point startPoint, Point endPoint)
        {
            Point oldStart = reaction.TailPoint;
            Point oldEnd = reaction.HeadPoint;

            Action redo = () =>
            {
                reaction.TailPoint = startPoint;
                reaction.HeadPoint = endPoint;
                RemoveFromSelection(reaction);
                AddToSelection(reaction);
            };
            Action undo = () =>
            {
                reaction.TailPoint = oldStart;
                reaction.HeadPoint = oldEnd;
                RemoveFromSelection(reaction);
                AddToSelection(reaction);
            };

            UndoManager.BeginUndoBlock();
            UndoManager.RecordAction(undo, redo);
            UndoManager.EndUndoBlock();
            redo();
        }

        /// <summary>
        /// Returns a combined flag value representing the reaction types
        /// </summary>
        public ReactionType? SelectedReactionType
        {
            get
            {
                List<ReactionType> selectedReactionTypes = (from ro in SelectedReactions()
                                                            select ro.ReactionType).Distinct().ToList();
                switch (selectedReactionTypes.Count)
                {
                    case 0:
                        return _selectedReactionType;

                    case 1:
                        return selectedReactionTypes[0];

                    default:
                        return null;
                }
            }
            set
            {
                _selectedReactionType = value;

                if (value != null)
                {
                    SetReactionType(value.Value);
                }
            }
        }

        /// <summary>
        /// sets all selected reactions to the currently applied type
        /// Called from the toolbar dropdown
        /// </summary>
        /// <param name="value"></param>
        /// <param name="parentReaction"></param>
        public void SetReactionType(ReactionType value, Reaction parentReaction = null)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                List<Reaction> reactions;
                if (parentReaction is null)
                {
                    reactions = SelectedReactions().ToList();
                }
                else
                {
                    reactions = new List<Reaction> { parentReaction };
                }

                int affectedReactions = reactions.Count;
                WriteTelemetry(module, "Debug",
                               $"Type: {SelectedReactionType}; Affected Reactions {affectedReactions}");

                if (reactions.Any())
                {
                    UndoManager.BeginUndoBlock();

                    foreach (Reaction reaction in reactions)
                    {
                        Action redo = () =>
                        {
                            reaction.ReactionType = value;
                            if (SelectedReactions().Contains(reaction))
                            {
                                RemoveFromSelection(reaction);
                                AddToSelection(reaction);
                            }
                        };
                        ReactionType reactionType = reaction.ReactionType;
                        Action undo = () =>
                        {
                            reaction.ReactionType = reactionType;
                            if (SelectedReactions().Contains(reaction))
                            {
                                RemoveFromSelection(reaction);
                                AddToSelection(reaction);
                            }
                        };
                        UndoManager.RecordAction(undo, redo);
                        redo();
                    }

                    UndoManager.EndUndoBlock();
                }
            }
            catch (Exception exception)
            {
                WriteTelemetryException(module, exception);
            }
        }

        /// <summary>
        /// Returns a list of selected reactions
        /// </summary>
        /// <returns></returns>
        private List<Reaction> SelectedReactions()
        {
            IEnumerable<Reaction> selectedReactions = SelectedItems.OfType<Reaction>();
            return selectedReactions.ToList();
        }

        /// <summary>
        /// Adds a reaction to the model's default reaction scheme. Undoable.
        /// </summary>
        /// <param name="reaction"></param>
        public void AddReaction(Reaction reaction)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                WriteTelemetry(module, "Debug", "Adding a reaction");

                Action redo = () =>
                              {
                                  //check to see if we have a scheme
                                  ReactionScheme scheme = Model.DefaultReactionScheme;
                                  scheme.AddReaction(reaction);
                                  reaction.Parent = scheme;
                              };
                Action undo = () =>
                              {
                                  ReactionScheme scheme = Model.DefaultReactionScheme;
                                  ClearSelection();
                                  scheme.RemoveReaction(reaction);
                                  if (!scheme.Reactions.Any())
                                  {
                                      Model.RemoveReactionScheme(scheme);
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
        }

        /// <summary>
        /// Visually refreshes reactions
        /// </summary>
        /// <param name="reactions"></param>
        private void RefreshReactions(List<Reaction> reactions)
        {
            foreach (Reaction reaction in reactions)
            {
                reaction.UpdateVisual();
            }
        }

        /// <summary>
        /// Deletes reactions from the model. Undoable.
        /// </summary>
        /// <param name="reactions"></param>
        internal void DeleteReactions(IEnumerable<Reaction> reactions)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                WriteTelemetry(module, "Debug", "Called");

                UndoManager.BeginUndoBlock();
                foreach (Reaction r in reactions)
                {
                    Action redo = () =>
                                  {
                                      ClearSelection();
                                      Model.DefaultReactionScheme.RemoveReaction(r);
                                      r.Parent = null;
                                  };

                    Action undo = () =>
                                  {
                                      Model.DefaultReactionScheme.AddReaction(r);
                                      r.Parent = Model.DefaultReactionScheme;
                                      AddToSelection(r);
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

        //annotation editing
        private void CreateBlockEditor(Reaction reaction, bool editingReagents)
        {
            string blocktext;
            Rect block;
            _selReactionVisual = EditingCanvas.ChemicalVisuals[reaction] as ReactionVisual; //should NOT be null!

            //remove the reaction from the selection, otherwise the adorner gets in the way

            RemoveFromSelection(reaction);

            //decide whether we're doing reagents or conditions
            if (editingReagents)
            {
                block = _selReactionVisual.ReagentsBlockRect;
                blocktext = reaction.ReagentText;
            }
            else
            {
                block = _selReactionVisual.ConditionsBlockRect;
                blocktext = reaction.ConditionsText;
            }

            //make the block a bit bigger
            block.Inflate(AcmeConstants.BlockTextPadding, AcmeConstants.BlockTextPadding);

            //locate the editor properly
            BlockEditor.Controller = this;
            BlockEditor.MinWidth = block.Width;
            BlockEditor.MinHeight = block.Height;
            BlockEditor.Visibility = Visibility.Visible;
            BlockEditor.EditingReagents = editingReagents;
            Canvas.SetLeft(BlockEditor, block.Left);
            Canvas.SetTop(BlockEditor, block.Top);

            if (!string.IsNullOrEmpty(blocktext))
            {
                BlockEditor.LoadDocument(blocktext);
            }

            ActiveBlockEditor = BlockEditor;
            BlockEditor.Completed += OnEditorClosed_BlockEditor;
            BlockEditor.SelectionChanged += OnSelectionChanged_BlockEditor;
            IsBlockEditing = true;
            SendStatus((EditingTextStatus, TotUpMolFormulae(), TotUpSelectedMwt()));
            BlockEditor.Focus();
        }

        /// <summary>
        /// updates the text block in the reaction after editing, depending on the editingReagents parameter
        /// </summary>
        /// <param name="selReactionVisual"></param>
        /// <param name="editor"></param>
        /// <param name="editingReagents"></param>
        private void UpdateTextBlock(ReactionVisual selReactionVisual, AnnotationEditor editor, bool editingReagents)
        {
            Reaction reaction = selReactionVisual.ParentReaction;
            string oldText;
            if (editingReagents)
            {
                oldText = reaction.ReagentText?.Trim() ?? "";
            }
            else
            {
                oldText = reaction.ConditionsText?.Trim() ?? "";
            }

            //only commit the text if it's been changed.
            string result = editor.GetDocument();
            if (oldText != result)
            {
                Action redo = () =>
                              {
                                  if (editingReagents)
                                  {
                                      reaction.ReagentText = result;
                                  }
                                  else
                                  {
                                      reaction.ConditionsText = result;
                                  }
                              };
                Action undo = () =>
                              {
                                  if (editingReagents)
                                  {
                                      reaction.ReagentText = oldText;
                                  }
                                  else
                                  {
                                      reaction.ConditionsText = oldText;
                                  }
                              };
                UndoManager.BeginUndoBlock();
                UndoManager.RecordAction(undo, redo, $"Update {(editingReagents ? "Reagents" : "Conditions")}");
                UndoManager.EndUndoBlock();
                redo();
            }
        }

        /// <summary>
        /// Edits the current reagents block
        /// </summary>
        public void EditReagents()
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                CreateBlockEditor(SelectedItems[0] as Reaction, editingReagents: true);
            }
            catch (Exception exception)
            {
                WriteTelemetryException(module, exception);
            }
        }

        /// <summary>
        /// Edits the current conditions block
        /// </summary>
        public void EditConditions()
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                CreateBlockEditor(SelectedItems[0] as Reaction, editingReagents: false);
            }
            catch (Exception exception)
            {
                WriteTelemetryException(module, exception);
            }
        }

        public bool FullReactionSelected()
        {
            if ((SelectionType & SelectionTypeCode.Molecule) == SelectionTypeCode.Molecule
                && (SelectionType & SelectionTypeCode.Reaction) == SelectionTypeCode.Reaction)
            {
                return SelectedItems.OfType<Reaction>().ToList().Count == 1 &&
                       (ReactantsInSelection.Any() && ProductsInSelection.Any());
            }

            return false;
        }

        /// <summary>
        /// Assigns roles to molecules involved in a reaction
        /// </summary>
        ///
        public void AssignReactionRoles()
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                WriteTelemetry(module, "Debug", "Assign reaction roles");

                Reaction selectedReaction = SelectedItems.OfType<Reaction>().ToList()[0];

                Molecule[] currentReactants = ReactantsInSelection.ToArray();
                Molecule[] currentProducts = ProductsInSelection.ToArray();

                Action redo = () =>
                {
                    selectedReaction.ClearReactants();
                    selectedReaction.ClearProducts();
                    foreach (Molecule reactant in currentReactants)
                    {
                        selectedReaction.AddReactant(reactant);
                    }

                    foreach (Molecule product in currentProducts)
                    {
                        selectedReaction.AddProduct(product);
                    }

                    ClearSelection();
                    AddToSelection(selectedReaction);
                };

                Action undo = () =>
                {
                    selectedReaction.ClearReactants();
                    selectedReaction.ClearProducts();
                    foreach (Molecule reactant in currentReactants)
                    {
                        selectedReaction.RemoveReactant(reactant);
                    }

                    foreach (Molecule product in currentProducts)
                    {
                        selectedReaction.RemoveProduct(product);
                    }

                    ClearSelection();
                    AddToSelection(selectedReaction);
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

        /// <summary>
        /// Have we selected one reaction only?
        /// </summary>
        /// <returns></returns>
        public bool SingleReactionSelected()
        {
            return SelectedReactions().Count == 1 &&
                   (SelectedReactions()[0].Reactants.Any() || SelectedReactions()[0].Products.Any());
        }

        /// <summary>
        /// Removes all assigned roles from molecules involved in a reaction
        /// </summary>
        public void ClearReactionRoles()
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                WriteTelemetry(module, "Debug", "Clearing reaction roles");

                Reaction selectedReaction = SelectedItems.OfType<Reaction>().ToList()[0];

                Molecule[] currentReactants = selectedReaction.Reactants.Values.ToArray();
                Molecule[] currentProducts = selectedReaction.Products.Values.ToArray();

                Action redo = () =>
                {
                    selectedReaction.ClearReactants();
                    selectedReaction.ClearProducts();

                    ClearSelection();
                    AddToSelection(selectedReaction);
                };

                Action undo = () =>
                {
                    foreach (Molecule reactant in currentReactants)
                    {
                        selectedReaction.AddReactant(reactant);
                    }

                    foreach (Molecule product in currentProducts)
                    {
                        selectedReaction.AddProduct(product);
                    }

                    ClearSelection();
                    AddToSelection(selectedReaction);
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

        #region Commands

        private EditReagentsCommand _editReagentsCommand;
        private EditConditionsCommand _editConditionsCommand;

        private AssignReactionRolesCommand _assignReactionRolesCommand;
        private ClearReactionRolesCommand _clearReactionRolesCommand;

        #endregion Commands

        #region Event Handlers

        /// <summary>
        /// Fired when we change selection in the block editor.
        /// Updates the sub/superscript properties and corresponding UI
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnSelectionChanged_BlockEditor(object sender, RoutedEventArgs e)
        {
            SelectionIsSubscript = BlockEditor.SelectionIsSubscript;
            SelectionIsSuperscript = BlockEditor.SelectionIsSuperscript;
        }

        /// <summary>
        /// On close of the editor, update the corresponding text block
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnEditorClosed_BlockEditor(object sender, AnnotationEditorEventArgs e)
        {
            AnnotationEditor activeEditor = (AnnotationEditor)sender;
            if (e.Reason != AnnotationEditorExitArgsType.Aborted)
            {
                UpdateTextBlock(_selReactionVisual, activeEditor, activeEditor.EditingReagents);
            }

            activeEditor.Visibility = Visibility.Collapsed;
            activeEditor.Clear();
            activeEditor.Completed -= OnEditorClosed_BlockEditor;
            activeEditor.SelectionChanged -= OnSelectionChanged_BlockEditor;
            activeEditor.Controller = null;
            IsBlockEditing = false;
            ActiveBlockEditor = null;
        }

        #endregion Event Handlers
    }
}
