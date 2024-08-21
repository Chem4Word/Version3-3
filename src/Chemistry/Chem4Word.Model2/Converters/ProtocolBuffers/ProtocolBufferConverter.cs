// ---------------------------------------------------------------------------
//  Copyright (c) 2024, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.Core.Enums;
using Chem4Word.Model2.Helpers;
using Chem4Word.Model2.Interfaces;
using Google.Protobuf;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Chem4Word.Model2.Converters.ProtocolBuffers
{
    /// <summary>
    /// Creates a snapshot of the model 'as-is'.
    /// performs no ring perception or other niceties, unlike other converters
    /// This class is designed to speed loading of static structures
    /// by cutting down parsing and processing
    /// </summary>
    public class ProtocolBufferConverter
    {
        public string Description => "Protocol Buffers";
        public string[] Extensions => new string[] { "*.pbuff" };

        /// <summary>
        /// Exports a Chem4Word.Model to protocol buffer
        /// </summary>
        /// <param name="model">Model to export</param>
        /// <returns>Protocol Buffer representation of the model</returns>
        public byte[] Export(Model model)
        {
            Dictionary<string, PBMolecule> moleculeLookup = new Dictionary<string, PBMolecule>();
            //create the model first
            PBModel pbModel = new PBModel();

            //add annotations
            foreach (var ann in model.Annotations.Values)
            {
                pbModel.Annotations[ann.Id] = AnnotationToPBuff(ann);
            }

            //recurse through molecule tree
            foreach (var molecule in model.Molecules.Values)
            {
                var moleculeBuffer = MoleculeToPBuff(molecule);
                pbModel.Molecules[molecule.Id] = moleculeBuffer;
                moleculeLookup[molecule.Id] = moleculeBuffer;
            }

            //reaction schemes
            foreach (var scheme in model.ReactionSchemes.Values)
            {
                var schemeBuffer = new PBReactionScheme()
                {
                    Id = scheme.Id
                };

                foreach (var reaction in scheme.Reactions.Values)
                {
                    var reactionBuffer = new PBReaction()
                    {
                        ConditionsText = reaction.ConditionsText,
                        Id = reaction.Id,
                        HeadPoint = new PBPoint
                        {
                            X = reaction.HeadPoint.X,
                            Y = reaction.HeadPoint.Y
                        },
                        ReactionType = (PBReactionType)reaction.ReactionType,
                        ReagentText = reaction.ReagentText,
                        TailPoint = new PBPoint
                        {
                            X = reaction.TailPoint.X,
                            Y = reaction.TailPoint.Y
                        }
                    };

                    foreach (var reactant in reaction.Reactants.Values)
                    {
                        reactionBuffer.Reactants.Add(reactant.Id);
                    }

                    foreach (var product in reaction.Products.Values)
                    {
                        reactionBuffer.Products.Add(product.Id);
                    }

                    schemeBuffer.Reactions[reaction.Id] = reactionBuffer;
                }

                pbModel.ReactionSchemes[schemeBuffer.Id] = schemeBuffer;
            }

            return pbModel.ToByteArray();
        }

        private PBMolecule MoleculeToPBuff(Molecule mol)
        {
            Dictionary<string, PBAtom> atomLookup = new Dictionary<string, PBAtom>();
            PBMolecule result = new PBMolecule
            {
                Count = mol.Count,
                DictRef = mol.DictRef,
                FormalCharge = mol.FormalCharge,
                Id = mol.Id,
                SpinMultiplicity = mol.SpinMultiplicity,
                Title = mol.Title
            };

            foreach (var atom in mol.Atoms.Values)
            {
                var atomToPBuff = AtomToPBuff(atom);
                result.Atoms[atom.Id] = atomToPBuff;
                atomLookup[atom.Id] = atomToPBuff;
            }

            foreach (var molecule in mol.Molecules.Values)
            {
                result.Molecules[molecule.Id] = MoleculeToPBuff(molecule);
            }

            foreach (var bond in mol.Bonds)
            {
                result.Bonds.Add(BondToPBuff(bond));
            }

            foreach (var ring in mol.Rings)
            {
                PBRing newRing = new PBRing();
                foreach (var modelAtom in ring.Atoms)
                {
                    newRing.Atoms.Add(atomLookup[modelAtom.Id]);
                }
                result.Rings.Add(newRing);
            }

            //misc props
            foreach (var formula in mol.Formulas)
            {
                result.Formulas.Add(formula.Id, TPToPBuff(formula));
            }

            foreach (var name in mol.Names)
            {
                result.Names.Add(name.Id, TPToPBuff(name));
            }

            foreach (var caption in mol.Captions)
            {
                result.Captions.Add(caption.Id, TPToPBuff(caption));
            }

            return result;
        }

        private PBTextualProperty TPToPBuff(TextualProperty property)
        {
            PBTextualProperty result = new PBTextualProperty
            {
                Id = property.Id,
                FullType = property.FullType,
                Value = property.Value
            };
            return result;
        }

        private PBAtom AtomToPBuff(Atom atom)
        {
            PBAtom result = new PBAtom
            {
                ExplicitC = atom.ExplicitC,
                Id = atom.Id,
                Position = new PBPoint { X = atom.Position.X, Y = atom.Position.Y },
                SpinMultiplicity = atom.SpinMultiplicity,
                FormalCharge = atom.FormalCharge,
                DoubletRadical = atom.DoubletRadical,
                IsotopeNumber = atom.IsotopeNumber,
            };

            if (atom.Element is FunctionalGroup fg)
            {
                result.FunctionalGroup = new PBFunctionalGroup
                {
                    ShortCode = fg.Name,
                    PlacementFG = (int?)atom.ExplicitFunctionalGroupPlacement
                };
            }
            else
            {
                result.Element = new PBChemicalElement
                {
                    Symbol = atom.Element.Symbol,
                    PlacementH = (int?)atom.ExplicitHPlacement
                };
            }

            return result;
        }

        private PBBond BondToPBuff(Bond bond)
        {
            PBBond result = new PBBond
            {
                Id = bond.Id,
                StartAtomID = bond.StartAtom.Id,
                EndAtomID = bond.EndAtom.Id,
                Order = bond.Order,
                Stereo = (PBBondStereo)bond.Stereo,
                Placement = (int?)bond.ExplicitPlacement
            };
            return result;
        }

        private PBAnnotation AnnotationToPBuff(Annotation annValue) =>
            new PBAnnotation
            {
                Id = annValue.Id,
                IsEditable = annValue.IsEditable,
                Position = new PBPoint { X = annValue.Position.X, Y = annValue.Position.Y },
                Xaml = annValue.Xaml,
                SymbolSize = annValue.SymbolSize
            };

        /// <summary>
        /// Converts a protocol buffer to a Model
        /// </summary>
        /// <param name="protoBuffModel"></param>
        /// <returns></returns>
        public Model Import(byte[] bytes)
        {
            var protoBuffModel = PBModel.Parser.ParseFrom(bytes);

            var moleculeLookup = new Dictionary<string, Molecule>();

            Model result = new Model();
            ImportAnnotations(protoBuffModel, result);

            ImportMolecules(protoBuffModel, result, moleculeLookup);
            //now do the reactants

            ImportReactionSchemes(protoBuffModel, result);

            return result;

            //local function
            void ImportAnnotations(PBModel model, Model result1)
            {
                foreach (var ann in model.Annotations)
                {
                    var annValue = ann.Value;
                    var newAnnotation = new Annotation()
                    {
                        Id = annValue.Id,
                        Xaml = annValue.Xaml,
                        Position = new System.Windows.Point(annValue.Position.X, annValue.Position.Y),
                        IsEditable = annValue.IsEditable,
                        SymbolSize = annValue.SymbolSize,
                        Parent = result1
                    };
                    result1.AddAnnotation(newAnnotation);
                }
            }

            //local function
            void ImportMolecules(PBModel protoBuffModel1, Model model1, Dictionary<string, Molecule> dictionary)
            {
                foreach (var molecule in protoBuffModel1.Molecules)
                {
                    PBuffToMolecule(molecule, model1, dictionary);
                }
            }

            //local function
            Reaction ImportReaction(PBReaction reactionVal, ReactionScheme rs)
            {
                var newReaction = new Reaction()
                {
                    ConditionsText = reactionVal.ConditionsText,
                    HeadPoint = new System.Windows.Point(
                                          reactionVal.HeadPoint.X, reactionVal.HeadPoint.Y),
                    Id = reactionVal.Id,
                    Parent = rs,
                    ReactionType = (ReactionType)reactionVal.ReactionType,
                    ReagentText = reactionVal.ReagentText,
                    TailPoint = new System.Windows.Point(reactionVal.TailPoint.X, reactionVal.TailPoint.Y)
                };

                foreach (string reactantId in reactionVal.Reactants)
                {
                    var reactant = moleculeLookup[reactantId];
                    newReaction.AddReactant(reactant);
                }

                foreach (string productId in reactionVal.Products)
                {
                    var product = moleculeLookup[productId];
                    newReaction.AddProduct(product);
                }

                return newReaction;
            }

            //local function
            void ImportReactionSchemes(PBModel protoBuffModel2, Model result2)
            {
                foreach (var scheme in protoBuffModel2.ReactionSchemes)
                {
                    var schemeval = scheme.Value;
                    ReactionScheme rs = new ReactionScheme()
                    {
                        Id = schemeval.Id,
                        Parent = result2
                    };
                    result2.AddReactionScheme(rs);

                    foreach (var reaction in schemeval.Reactions)
                    {
                        var reactionVal = reaction.Value;
                        var newReaction = ImportReaction(reactionVal, rs);
                        rs.AddReaction(newReaction);
                    }
                }
            }
        }

        private void PBuffToMolecule(KeyValuePair<string, PBMolecule> molecule,
                                     IChemistryContainer parent,
                                     Dictionary<string, Molecule> moleculeLookup)
        {
            var atomLookup = new Dictionary<string, Atom>();
            var molval = molecule.Value;

            Molecule newMol = new Molecule
            {
                Id = molval.Id,
                Count = molval.Count,
                DictRef = molval.DictRef,
                FormalCharge = molval.FormalCharge,
                SpinMultiplicity = molval.SpinMultiplicity,
                Title = molval.Title,
                Parent = parent
            };

            foreach (var atom in molval.Atoms.Values)
            {
                Atom newAtom = new Atom
                {
                    DoubletRadical = atom.DoubletRadical,
                    ExplicitC = atom.ExplicitC,
                    FormalCharge = atom.FormalCharge,
                    Id = atom.Id,
                    IsotopeNumber = atom.IsotopeNumber,
                    Position = new System.Windows.Point(atom.Position.X, atom.Position.Y),
                    SpinMultiplicity = atom.SpinMultiplicity,
                    Parent = newMol
                };
                switch (atom.SymbolCase)
                {
                    case PBAtom.SymbolOneofCase.Element:
                        newAtom.Element = Globals.PeriodicTable.Elements[atom.Element.Symbol];
                        newAtom.ExplicitHPlacement = (CompassPoints?)atom.Element.PlacementH;
                        break;

                    case PBAtom.SymbolOneofCase.FunctionalGroup:
                        newAtom.Element = Globals.FunctionalGroupsList.FirstOrDefault(n => n.Name.Equals(atom.FunctionalGroup.ShortCode));
                        newAtom.ExplicitFunctionalGroupPlacement = (CompassPoints?)atom.FunctionalGroup.PlacementFG;
                        break;
                }
                newMol.AddAtom(newAtom);
                atomLookup[newAtom.Id] = newAtom;
            }

            foreach (var bond in molval.Bonds)
            {
                Bond newBond = new Bond
                {
                    Id = bond.Id,
                    EndAtomInternalId = atomLookup[bond.EndAtomID].InternalId,
                    StartAtomInternalId = atomLookup[bond.StartAtomID].InternalId,
                    Order = bond.Order,
                    Stereo = (Enums.BondStereo)bond.Stereo,
                    Parent = newMol,
                    ExplicitPlacement = (Enums.BondDirection?)bond.Placement
                };

                if (!newBond.StartAtomInternalId.Equals(newBond.EndAtomInternalId))
                {
                    newMol.AddBond(newBond);
                }
                else
                {
                    newMol.Warnings.Add($"Bond line {newBond.Id} - Skipped as StartAtom == EndAtom");
                }
            }

            foreach (var ring in molval.Rings)
            {
                Ring newRing = new Ring();
                foreach (PBAtom ringAtom in ring.Atoms)
                {
                    newRing.Atoms.Add(atomLookup[ringAtom.Id]);
                }
                newMol.AddRing(newRing);
            }

            // Misc stuff
            foreach (var name in molval.Names)
            {
                newMol.Names.Add(MakeTextualProperty(name));
            }

            foreach (var formula in molval.Formulas)
            {
                newMol.Formulas.Add(MakeTextualProperty(formula));
            }

            foreach (var caption in molval.Captions)
            {
                newMol.Captions.Add(MakeTextualProperty(caption));
            }

            parent.AddMolecule(newMol);
            moleculeLookup[molval.Id] = newMol;

            foreach (var child in molval.Molecules)
            {
                PBuffToMolecule(child, newMol, moleculeLookup);
            }
        }

        private static TextualProperty MakeTextualProperty(KeyValuePair<string, PBTextualProperty> property)
        {
            var tp = new TextualProperty
            {
                Id = property.Value.Id,
                FullType = property.Value.FullType,
                Value = property.Value.Value
            };
            return tp;
        }
    }
}