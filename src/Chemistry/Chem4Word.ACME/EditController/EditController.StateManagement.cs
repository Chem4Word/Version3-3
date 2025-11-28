// ---------------------------------------------------------------------------
//  Copyright (c) 2026, The .NET Foundation.
//  This software is released under the Apache Licence, Version 2.0.
//  The licence and further copyright text can be found in the file LICENCE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.ACME.Commands.Editing;
using Chem4Word.ACME.Commands.Undo;
using Chem4Word.ACME.Utils;
using Chem4Word.Core.Helpers;
using Chem4Word.Model2;
using Chem4Word.Model2.Converters.CML;
using Chem4Word.Model2.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows;

namespace Chem4Word.ACME
{
    /// <summary>
    /// Handles basic state operations such as undo and redo, also clipboard operations
    /// </summary>
    public partial class EditController
    {
        #region Fields

        private UndoCommand _undoCommand;
        private RedoCommand _redoCommand;
        private CopyCommand _copyCommand;
        private CutCommand _cutCommand;
        private PasteCommand _pasteCommand;
        private readonly ClipboardMonitor _clipboardMonitor;

        #endregion Fields

        #region Properties

        public ClipboardMonitor ClipboardMonitor
        {
            get
            {
                return _clipboardMonitor;
            }
        }

        public UndoCommand UndoCommand
        {
            get
            {
                return _undoCommand;
            }
            set
            {
                _undoCommand = value;
                OnPropertyChanged();
            }
        }

        public RedoCommand RedoCommand
        {
            get
            {
                return _redoCommand;
            }
            set
            {
                _redoCommand = value;
                OnPropertyChanged();
            }
        }

        public CopyCommand CopyCommand
        {
            get
            {
                return _copyCommand;
            }
            set
            {
                _copyCommand = value;
                OnPropertyChanged();
            }
        }

        public CutCommand CutCommand
        {
            get
            {
                return _cutCommand;
            }
            set
            {
                _cutCommand = value;
                OnPropertyChanged();
            }
        }

        public PasteCommand PasteCommand
        {
            get
            {
                return _pasteCommand;
            }
            set
            {
                _pasteCommand = value;
                OnPropertyChanged();
            }
        }

        #endregion Properties

        #region Methods

        public void CopySelection()
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod()?.Name}()";
            try
            {
                WriteTelemetry(module, "Debug", "Called");

                CMLConverter converter = new CMLConverter();
                Model tempModel = new Model();
                //if selection isn't null
                if (SelectedItems.Count > 0)
                {
                    HashSet<Atom> copiedAtoms = new HashSet<Atom>();

                    //iterate through the active selection
                    foreach (object selectedItem in SelectedItems)
                    {
                        //if the current selection is a molecule
                        if (selectedItem is Molecule molecule)
                        {
                            tempModel.AddMolecule(molecule);
                        }
                        else if (selectedItem is Reaction reaction)
                        {
                            //TODO: [DCD] Handle multiple reaction schemes in the future. This is a kludge
                            tempModel.DefaultReactionScheme.AddReaction(reaction);
                        }
                        else if (selectedItem is Annotation annotation)
                        {
                            tempModel.AddAnnotation(annotation);
                        }
                        else if (selectedItem is Atom atom)
                        {
                            copiedAtoms.Add(atom);
                        }
                    }

                    //keep track of added atoms
                    Dictionary<string, Atom> aa = new Dictionary<string, Atom>();
                    //while the atom copy list isn't empty
                    while (copiedAtoms.Any())
                    {
                        Atom seedAtom = copiedAtoms.First();
                        //create a new molecule
                        Molecule newMol = new Molecule();
                        Molecule oldParent = seedAtom.Parent;

                        HashSet<Atom> thisAtomGroup = new HashSet<Atom>();

                        //Traverse the molecule, excluding atoms that have been processed and bonds that aren't in the list
                        oldParent.TraverseBFS(seedAtom,
                                              atom =>
                                              {
                                                  copiedAtoms.Remove(atom);

                                                  thisAtomGroup.Add(atom);
                                              },
                                              atom2 =>
                                              {
                                                  return !thisAtomGroup.Contains(atom2) && copiedAtoms.Contains(atom2);
                                              });

                        //add the atoms and bonds to the new molecule

                        foreach (Atom thisAtom in thisAtomGroup)
                        {
                            Atom a = new Atom
                            {
                                Id = thisAtom.Id,
                                Position = thisAtom.Position,
                                Element = thisAtom.Element,
                                FormalCharge = thisAtom.FormalCharge,
                                IsotopeNumber = thisAtom.IsotopeNumber,
                                ExplicitC = thisAtom.ExplicitC,
                                Parent = newMol
                            };

                            newMol.AddAtom(a);
                            aa[a.Id] = a;
                        }

                        Bond thisBond = null;
                        List<Bond> copiedBonds = new List<Bond>();
                        foreach (Atom startAtom in thisAtomGroup)
                        {
                            foreach (Atom otherAtom in thisAtomGroup)
                            {
                                if ((thisBond = startAtom.BondBetween(otherAtom)) != null &&
                                    !copiedBonds.Contains(thisBond))
                                {
                                    copiedBonds.Add(thisBond);
                                    Atom s = aa[thisBond.StartAtom.Id];
                                    Atom e = aa[thisBond.EndAtom.Id];
                                    Bond b = new Bond(s, e)
                                    {
                                        Id = thisBond.Id,
                                        Order = thisBond.Order,
                                        Stereo = thisBond.Stereo,
                                        ExplicitPlacement = thisBond.ExplicitPlacement,
                                        Parent = newMol
                                    };

                                    newMol.AddBond(b);
                                }
                            }
                        }

                        newMol.Parent = tempModel;
                        tempModel.AddMolecule(newMol);
                    }

                    tempModel.RescaleForCml();
                    string export = converter.Export(tempModel);
                    Clipboard.Clear();
                    IDataObject ido = new DataObject();
                    ido.SetData(ModelConstants.FormatCML, export);
                    ido.SetData(DataFormats.Text, XmlHelper.AddHeader(export));
                    Clipboard.SetDataObject(ido, true);
                }
            }
            catch (Exception exception)
            {
                WriteTelemetryException(module, exception);
            }
        }

        public void CutSelection()
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                WriteTelemetry(module, "Debug", "Called");

                UndoManager.BeginUndoBlock();
                CopySelection();
                DeleteSelection();
                UndoManager.EndUndoBlock();
            }
            catch (Exception exception)
            {
                WriteTelemetryException(module, exception);
            }
        }

        public void PasteCML(string pastedCml, Point? pasteAt = null)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                WriteTelemetry(module, "Debug", "Called");

                CMLConverter cc = new CMLConverter();
                Model buffer = cc.Import(pastedCml);
                PasteModel(buffer, true, !buffer.FromChem4Word, pasteAt: pasteAt);
            }
            catch (Exception exception)
            {
                WriteTelemetryException(module, exception);
            }
        }

        public void PasteModel(Model buffer, bool fromCML = false, bool rescale = true, Point? pasteAt = null)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                WriteTelemetry(module, "Debug", "Called");
                ClearSelection();
                // Match to current model's settings
                buffer.Relabel(true);
                // above should be buffer.StripLabels(true)
                if (rescale)
                {
                    buffer.ScaleToAverageBondLength(Model.XamlBondLength);
                }

                if (!fromCML && buffer.Molecules.Count > 1)
                {
                    Packer packer = new Packer { Model = buffer };
                    packer.Pack(Model.XamlBondLength * 2);
                }

                List<Molecule> molList = buffer.Molecules.Values.ToList();
                List<Reaction> reactionList = buffer.DefaultReactionScheme.Reactions.Values.ToList();
                List<Annotation> annotationList = buffer.Annotations.Values.ToList();

                if (pasteAt != null)
                {
                    //offset the pasted content so that its centroid is at the pasteAt location
                    buffer.CenterOn(pasteAt.Value);
                }
                else
                {
                    //grab the metrics of the editor's viewport
                    double editorControlHorizontalOffset = EditorControl.HorizontalOffset;
                    double editorControlViewportWidth = EditorControl.ViewportWidth;
                    double editorControlVerticalOffset = EditorControl.VerticalOffset;
                    double editorControlViewportHeight = EditorControl.ViewportHeight;
                    //to center on the X coordinate, we need to set the left extent of the model to the horizontal offset
                    //plus half the viewport width, minus half the model width
                    //Similar for the height
                    double leftCenter = editorControlHorizontalOffset + editorControlViewportWidth / 2;
                    double topCenter = editorControlVerticalOffset + editorControlViewportHeight / 2;
                    //these two coordinates now give us the point where the new model should be centered
                    buffer.CenterOn(new Point(leftCenter, topCenter));
                }

                Action undo = () =>
                              {
                                  foreach (Molecule mol in molList)
                                  {
                                      RemoveFromSelection(mol);
                                      Model.RemoveMolecule(mol);
                                      mol.Parent = null;
                                  }

                                  foreach (Reaction reaction in reactionList)
                                  {
                                      RemoveFromSelection(reaction);
                                      Model.DefaultReactionScheme.RemoveReaction(reaction);
                                      reaction.Parent = null;
                                  }

                                  foreach (Annotation annotation in annotationList)
                                  {
                                      RemoveFromSelection(annotation);
                                      Model.RemoveAnnotation(annotation);
                                      annotation.Parent = null;
                                  }
                              };
                Action redo = () =>
                              {
                                  ClearSelection();
                                  foreach (Molecule mol in molList)
                                  {
                                      mol.Parent = Model;
                                      Model.AddMolecule(mol);
                                      AddToSelection(mol);
                                  }

                                  foreach (Reaction reaction in reactionList)
                                  {
                                      reaction.Parent = Model.DefaultReactionScheme;
                                      Model.DefaultReactionScheme.AddReaction(reaction);
                                      AddToSelection(reaction);
                                  }

                                  foreach (Annotation annotation in annotationList)
                                  {
                                      annotation.Parent = Model;
                                      Model.AddAnnotation(annotation);
                                      AddToSelection(annotation);
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

        #endregion Methods

        #region Event Handlers

        private void OnClipboardContentChanged_ClipboardMonitor(object sender, EventArgs e)
        {
            PasteCommand.RaiseCanExecChanged();
        }

        #endregion Event Handlers
    }
}
