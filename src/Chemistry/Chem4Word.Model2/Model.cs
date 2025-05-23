﻿// ---------------------------------------------------------------------------
//  Copyright (c) 2025, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.Core.Helpers;
using Chem4Word.Model2.Converters.CML;
using Chem4Word.Model2.Enums;
using Chem4Word.Model2.Helpers;
using Chem4Word.Model2.Interfaces;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;

namespace Chem4Word.Model2
{
    public class Model : IChemistryContainer, INotifyPropertyChanged
    {
        #region Events

        public event NotifyCollectionChangedEventHandler AtomsChanged;

        public event NotifyCollectionChangedEventHandler BondsChanged;

        public event NotifyCollectionChangedEventHandler MoleculesChanged;

        public event NotifyCollectionChangedEventHandler ReactionSchemesChanged;

        public event NotifyCollectionChangedEventHandler ReactionsChanged;

        public event NotifyCollectionChangedEventHandler AnnotationsChanged;

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion Events

        #region Event handlers

        private void UpdateMoleculeEventHandlers(NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
            {
                foreach (var oldItem in e.OldItems)
                {
                    var mol = ((Molecule)oldItem);
                    mol.AtomsChanged -= OnCollectionChanged_Atoms;
                    mol.BondsChanged -= OnCollectionChanged_Bonds;
                    mol.PropertyChanged -= OnPropertyChanged_ChemObject;
                }
            }

            if (e.NewItems != null)
            {
                foreach (var newItem in e.NewItems)
                {
                    var mol = ((Molecule)newItem);
                    mol.AtomsChanged += OnCollectionChanged_Atoms;
                    mol.BondsChanged += OnCollectionChanged_Bonds;
                    mol.PropertyChanged += OnPropertyChanged_ChemObject;
                }
            }
        }

        private void OnMoleculesChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (!InhibitEvents)
            {
                var temp = MoleculesChanged;
                if (temp != null)
                {
                    temp.Invoke(sender, e);
                }
            }
        }

        //responds to a property being changed on an object
        private void OnPropertyChanged_ChemObject(object sender, PropertyChangedEventArgs e)
        {
            OnPropertyChanged(sender, e);
        }

        //transmits the property being changed on an object
        private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (!InhibitEvents)
            {
                var temp = PropertyChanged;
                if (temp != null)
                {
                    temp.Invoke(sender, e);
                }
            }
        }

        public Annotation AddAnnotation(Annotation newAnnotation)
        {
            _annotations[newAnnotation.InternalId] = newAnnotation;
            NotifyCollectionChangedEventArgs e =
                new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, new List<Annotation> { newAnnotation });
            UpdateAnnotationsEventHandlers(e);
            OnAnnotationsChanged(this, e);
            return newAnnotation;
        }

        private void OnCollectionChanged_Annotations(object sender, NotifyCollectionChangedEventArgs e)
        {
            OnAnnotationsChanged(sender, e);
        }

        private void OnAnnotationsChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (!InhibitEvents)
            {
                var temp = AnnotationsChanged;
                if (temp != null)
                {
                    temp.Invoke(sender, e);
                }
            }
        }

        public void RemoveAnnotation(Annotation annotation)
        {
            _annotations.Remove(annotation.InternalId);
            NotifyCollectionChangedEventArgs e =
                new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove,
                    new List<Annotation> { annotation });
            UpdateAnnotationsEventHandlers(e);
            OnAnnotationsChanged(this, e);
        }

        //responds to bonds being added or removed
        private void OnCollectionChanged_Bonds(object sender, NotifyCollectionChangedEventArgs e)
        {
            OnBondsChanged(sender, e);
        }

        //transmits bonds being added or removed
        private void OnBondsChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (!InhibitEvents)
            {
                var temp = BondsChanged;
                if (temp != null)
                {
                    temp.Invoke(sender, e);
                }
            }
        }

        //responds to atoms being added or removed
        private void OnCollectionChanged_Atoms(object sender, NotifyCollectionChangedEventArgs e)
        {
            OnAtomsChanged(sender, e);
        }

        //transmits atoms being added or removed
        private void OnAtomsChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (!InhibitEvents)
            {
                var temp = AtomsChanged;
                if (temp != null)
                {
                    temp.Invoke(sender, e);
                }
            }
        }

        #endregion Event handlers

        #region Properties

        private readonly Dictionary<Guid, Molecule> _molecules;
        public ReadOnlyDictionary<Guid, Molecule> Molecules;

        private readonly Dictionary<Guid, ReactionScheme> _reactionSchemes;
        public ReadOnlyDictionary<Guid, ReactionScheme> ReactionSchemes;

        private readonly Dictionary<Guid, Annotation> _annotations;
        public ReadOnlyDictionary<Guid, Annotation> Annotations;

        public string CustomXmlPartGuid { get; set; }

        public bool ExplicitC { get; set; } = false;
        public HydrogenLabels ExplicitH { get; set; } = HydrogenLabels.HeteroAndTerminal;
        public bool ShowMoleculeGrouping { get; set; } = true;
        public bool ShowColouredAtoms { get; set; } = true;

        public bool InhibitEvents { get; set; }

        internal List<string> GeneralErrors { get; }
        internal List<string> GeneralWarnings { get; }

        public Dictionary<string, CrossedBonds> CrossedBonds { get; set; } = new Dictionary<string, CrossedBonds>();

        public string Path => "/";
        public IChemistryContainer Root => null;

        public bool ScaledForXaml { get; set; }

        private Rect _boundingBox = Rect.Empty;

        public string CreatorGuid { get; set; }

        private Dictionary<string, int> _calculatedFormulas;

        /// <summary>
        /// Bond length used in Xaml
        /// </summary>
        public double XamlBondLength { get; internal set; }

        #endregion Properties

        #region Derived Properties

        /// <summary>
        /// True if this model has any reactions
        /// </summary>
        public bool HasReactions
        {
            get
            {
                var count = 0;

                if (ReactionSchemes.Count > 0)
                {
                    foreach (var scheme in ReactionSchemes.Values)
                    {
                        count += scheme.Reactions.Count;
                    }
                }

                return count > 0;
            }
        }

        /// <summary>
        /// True if this model has any annotations
        /// </summary>
        public bool HasAnnotations => Annotations.Values.Count > 0;

        /// <summary>
        /// Count of functional groups in this model
        /// </summary>
        public int FunctionalGroupsCount
        {
            get
            {
                var result = 0;

                var allAtoms = GetAllAtoms();

                foreach (var atom in allAtoms)
                {
                    if (atom.Element is FunctionalGroup)
                    {
                        result++;
                    }
                }

                return result;
            }
        }

        /// <summary>
        /// True if this model has functional groups
        /// </summary>
        public bool HasFunctionalGroups
        {
            get
            {
                bool result = false;

                var allAtoms = GetAllAtoms();

                foreach (var atom in allAtoms)
                {
                    if (atom.Element is FunctionalGroup)
                    {
                        result = true;
                        break;
                    }
                }

                return result;
            }
        }

        /// <summary>
        /// True if this model has nested molecules
        /// </summary>
        public bool HasNestedMolecules
        {
            get
            {
                bool result = false;

                foreach (var child in Molecules.Values)
                {
                    if (child.Molecules.Count > 0)
                    {
                        result = true;
                        break;
                    }
                }

                return result;
            }
        }

        public List<TextualProperty> AllTextualProperties
        {
            get
            {
                var list = new List<TextualProperty>();

                // Add 2D if relevant
                if (TotalAtomsCount > 0)
                {
                    list.Add(new TextualProperty
                    {
                        Id = "2D",
                        TypeCode = "2D",
                        FullType = "2D",
                        Value = "2D"
                    });
                    list.Add(new TextualProperty
                    {
                        Id = "c0",
                        TypeCode = "F",
                        FullType = "ConciseFormula",
                        Value = ConciseFormula
                    });
                    list.Add(new TextualProperty
                    {
                        Id = "S",
                        TypeCode = "S",
                        FullType = "Separator",
                        Value = "S"
                    });
                }

                foreach (var child in Molecules.Values)
                {
                    list.AddRange(child.AllTextualProperties);
                }

                if (list.Count > 0)
                {
                    list = list.Take(list.Count - 1).ToList();
                }

                return list;
            }
        }

        /// <summary>
        /// Count of atoms in all molecules
        /// </summary>
        public int TotalAtomsCount
        {
            get
            {
                int count = 0;

                foreach (var molecule in Molecules.Values)
                {
                    count += molecule.AtomCount;
                }

                return count;
            }
        }

        public int TotalMoleculesCount
        {
            get
            {
                return GetAllMolecules().Count;
            }
        }

        /// <summary>
        /// Count of bonds in all molecules
        /// </summary>
        public int TotalBondsCount
        {
            get
            {
                int count = 0;

                foreach (var molecule in Molecules.Values)
                {
                    count += molecule.BondCount;
                }

                return count;
            }
        }

        public BondLengthStatistics GetBondLengthStatistics(bool includeHBonds = true)
        {
            List<Bond> bonds;

            if (includeHBonds)
            {
                bonds = GetAllBonds();
            }
            else
            {
                bonds = GetAllBonds().Where(b => !b.IsBondToH()).ToList();
            }

            var lengths = new List<double>(bonds.Count);
            foreach (var bond in bonds)
            {
                lengths.Add(bond.BondLength);
            }

            return new BondLengthStatistics(lengths);
        }

        /// <summary>
        /// Average bond length of all molecules
        /// </summary>
        public double MeanBondLength
        {
            get
            {
                double result = 0.0;
                var bonds = GetAllBonds();
                var lengths = new List<double>(bonds.Count);

                foreach (var bond in bonds)
                {
                    lengths.Add(bond.BondLength);
                }

                if (lengths.Any())
                {
                    result = lengths.Average();
                }
                else
                {
                    if (ScaledForXaml)
                    {
                        result = XamlBondLength;
                    }
                }

                return result;
            }
        }

        public double MolecularWeight
        {
            get
            {
                double weight = 0;

                foreach (var atom in GetAllAtoms())
                {
                    weight += atom.Element.AtomicWeight;
                }

                return weight;
            }
        }

        public string QuickName
        {
            get
            {
                var result = ConciseFormula;

                // Get Captions and Names
                var properties = AllTextualProperties.Where(p => p.TypeCode.Equals("L")).ToList();
                properties.AddRange(AllTextualProperties.Where(p => p.TypeCode.Equals("N")));

                // Sorting by Full type should put captions first then our names
                properties = properties.OrderBy(p => p.FullType).ToList();

                foreach (var property in properties)
                {
                    // Is long enough, not inchi related, not a special string, not a number
                    if (property.Value.Length > 3
                        && !property.FullType.ToLower().Contains("inchi")
                        && !property.Value.ToLower().Equals("unable to calculate")
                        && !property.Value.ToLower().Equals("not found")
                        && !property.Value.ToLower().Equals("not requested")
                        && !decimal.TryParse(property.Value, out _))
                    {
                        result = property.Value;
                        break;
                    }
                }

                return result;
            }
        }

        /// <summary>
        /// Overall Cml bounding box for all atoms
        /// </summary>
        public Rect BoundingBoxOfCmlPoints
        {
            get
            {
                Rect boundingBox = Rect.Empty;

                foreach (var mol in Molecules.Values)
                {
                    boundingBox.Union(mol.BoundingBox);
                }

                foreach (ReactionScheme scheme in ReactionSchemes.Values)
                {
                    foreach (Reaction reaction in scheme.Reactions.Values)
                    {
                        Rect reactionRect = new Rect(reaction.TailPoint, reaction.HeadPoint);
                        boundingBox.Union(reactionRect);
                    }
                }

                foreach (var ann in Annotations.Values)
                {
                    // Use a very small rectangle here
                    Rect rectangle = new Rect(ann.Position, new Size(0.1, 0.1));
                    boundingBox.Union(rectangle);
                }

                return boundingBox;
            }
        }

        /// <summary>
        /// Overall bounding box for all objects allowing for Font Size
        /// </summary>
        public Rect BoundingBoxWithFontSize
        {
            get
            {
                if (_boundingBox == Rect.Empty)
                {
                    var allAtoms = GetAllAtoms();

                    Rect boundingBox = Rect.Empty;

                    if (allAtoms.Count > 0)
                    {
                        boundingBox = allAtoms[0].BoundingBox(FontSize);
                        for (int i = 1; i < allAtoms.Count; i++)
                        {
                            var atom = allAtoms[i];
                            boundingBox.Union(atom.BoundingBox(FontSize));
                        }
                    }

                    foreach (ReactionScheme scheme in ReactionSchemes.Values)
                    {
                        foreach (Reaction reaction in scheme.Reactions.Values)
                        {
                            Rect reactionRect = new Rect(reaction.TailPoint, reaction.HeadPoint);
                            boundingBox.Union(reactionRect);
                        }
                    }

                    foreach (Annotation ann in Annotations.Values)
                    {
                        boundingBox.Union(ann.BoundingBox(FontSize));
                    }

                    _boundingBox = boundingBox;
                }

                return _boundingBox;
            }
        }

        public double MinX => BoundingBoxWithFontSize.Left;
        public double MaxX => BoundingBoxWithFontSize.Right;
        public double MinY => BoundingBoxWithFontSize.Top;
        public double MaxY => BoundingBoxWithFontSize.Bottom;

        /// <summary>
        /// List of all warnings encountered during the import from external file format
        /// </summary>
        public List<string> AllWarnings
        {
            get
            {
                var list = new List<string>();
                list.AddRange(GeneralWarnings);

                foreach (var molecule in Molecules.Values)
                {
                    list.AddRange(molecule.Warnings);
                }

                return list;
            }
        }

        /// <summary>
        /// List of all errors encountered during the import from external file format
        /// </summary>
        public List<string> AllErrors
        {
            get
            {
                var list = new List<string>();
                list.AddRange(GeneralErrors);

                foreach (var molecule in Molecules.Values)
                {
                    list.AddRange(molecule.Errors);
                }

                return list;
            }
        }

        /// <summary>
        /// Font size used for Xaml
        /// </summary>
        public double FontSize
        {
            get
            {
                var allBonds = GetAllBonds();
                double fontSize = Globals.DefaultFontSize * Globals.ScaleFactorForXaml;

                if (allBonds.Any())
                {
                    fontSize = XamlBondLength * Globals.FontSizePercentageBond;
                }

                return fontSize;
            }
        }

        /// <summary>
        /// Concise formula for the model
        /// </summary>
        public string ConciseFormula
        {
            get
            {
                if (_calculatedFormulas == null)
                {
                    _calculatedFormulas = new Dictionary<string, int>();
                    GatherFormulas(Molecules.Values.ToList());
                }

                return CalculatedFormulaAsString();
            }
        }

        private void GatherFormulas(List<Molecule> molecules)
        {
            foreach (var molecule in molecules)
            {
                if (molecule.Atoms.Count > 0)
                {
                    // Add into running totals
                    if (_calculatedFormulas.ContainsKey(molecule.ConciseFormula))
                    {
                        _calculatedFormulas[molecule.ConciseFormula]++;
                    }
                    else
                    {
                        _calculatedFormulas.Add(molecule.ConciseFormula, 1);
                    }
                }
                else
                {
                    // Gather the formulas of the children
                    var children = new List<string>();
                    foreach (var childMolecule in molecule.Molecules.Values.ToList())
                    {
                        children.Add(childMolecule.ConciseFormula);
                    }

                    // Add Brackets and join using Bullet character <Alt>0183
                    var combined = "[" + string.Join(" · ", children) + "]";

                    // Add charge here
                    if (molecule.FormalCharge != null)
                    {
                        var charge = molecule.FormalCharge.Value;
                        var absCharge = Math.Abs(charge);

                        if (charge > 0)
                        {
                            combined += $" + {absCharge}";
                        }
                        if (charge < 0)
                        {
                            combined += $" - {absCharge}";
                        }
                    }

                    // Add combined value into running totals
                    if (_calculatedFormulas.ContainsKey(combined))
                    {
                        _calculatedFormulas[combined]++;
                    }
                    else
                    {
                        _calculatedFormulas.Add(combined, 1);
                    }
                }
            }
        }

        private string CalculatedFormulaAsString()
        {
            var strings = new List<string>();
            foreach (var calculatedFormula in _calculatedFormulas)
            {
                if (calculatedFormula.Value > 1)
                {
                    strings.Add($"{calculatedFormula.Value} {calculatedFormula.Key}");
                }
                else
                {
                    strings.Add(calculatedFormula.Key);
                }
            }

            // Join using Bullet character <Alt>0183
            return string.Join(" · ", strings);
        }

        public ReactionScheme DefaultReactionScheme
        {
            get
            {
                if (!ReactionSchemes.Any())
                {
                    var rs = new ReactionScheme();
                    AddReactionScheme(rs);
                }
                return ReactionSchemes.Values.First();
            }
        }

        #endregion Derived Properties

        #region Constructor(s)

        public Model()
        {
            _molecules = new Dictionary<Guid, Molecule>();
            Molecules = new ReadOnlyDictionary<Guid, Molecule>(_molecules);

            _reactionSchemes = new Dictionary<Guid, ReactionScheme>();
            ReactionSchemes = new ReadOnlyDictionary<Guid, ReactionScheme>(_reactionSchemes);

            _annotations = new Dictionary<Guid, Annotation>();
            Annotations = new ReadOnlyDictionary<Guid, Annotation>(_annotations);

            GeneralErrors = new List<string>();
            GeneralWarnings = new List<string>();
        }

        #endregion Constructor(s)

        #region Methods

        public void SetXamlBondLength(int bondLength)
        {
            XamlBondLength = bondLength;
        }

        /// <summary>
        /// Drags all Atoms back to the origin by the specified offset
        /// </summary>
        /// <param name="x">X offset</param>
        /// <param name="y">Y offset</param>
        public void RepositionAll(double x, double y)
        {
            foreach (Molecule molecule in Molecules.Values)
            {
                molecule.RepositionAll(x, y);
            }
            foreach (ReactionScheme rs in ReactionSchemes.Values)
            {
                rs.RepositionAll(x, y);
            }
            foreach (Annotation annotation in Annotations.Values)
            {
                annotation.RepositionAll(x, y);
            }
            _boundingBox = Rect.Empty;
        }

        public void CenterOn(Point point)
        {
            Rect boundingBox = BoundingBoxWithFontSize;
            Point midPoint = new Point(boundingBox.Left + boundingBox.Width / 2, boundingBox.Top + boundingBox.Height / 2);
            Vector displacement = midPoint - point;
            RepositionAll(displacement.X, displacement.Y);
        }

        public bool RemoveMolecule(Molecule mol)
        {
            var res = _molecules.Remove(mol.InternalId);
            if (res)
            {
                NotifyCollectionChangedEventArgs e =
                    new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove,
                        new List<Molecule> { mol });
                OnMoleculesChanged(this, e);
                UpdateMoleculeEventHandlers(e);
            }

            return res;
        }

        public Molecule AddMolecule(Molecule newMol)
        {
            _molecules[newMol.InternalId] = newMol;
            NotifyCollectionChangedEventArgs e =
                new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add,
                    new List<Molecule> { newMol });
            UpdateMoleculeEventHandlers(e);
            OnMoleculesChanged(this, e);
            return newMol;
        }

        public void SetProtectedLabels(List<string> protectedLabels)
        {
            foreach (Molecule m in Molecules.Values)
            {
                m.SetProtectedLabels(protectedLabels);
            }
        }

        public void ReLabelGuids()
        {
            int bondCount = 0;
            int atomCount = 0;
            int molCount = 0;
            int reactionSchemeCount = 0;
            int reactionCount = 0;
            int annotationCount = 0;

            foreach (var molecule in GetAllMolecules())
            {
                var number = molecule.Id.Substring(1);
                int n;
                if (int.TryParse(number, out n))
                {
                    molCount = Math.Max(molCount, n);
                }
            }

            foreach (var atom in GetAllAtoms())
            {
                var number = atom.Id.Substring(1);
                int n;
                if (int.TryParse(number, out n))
                {
                    atomCount = Math.Max(atomCount, n);
                }
            }

            foreach (var bond in GetAllBonds())
            {
                var number = bond.Id.Substring(1);
                int n;
                if (int.TryParse(number, out n))
                {
                    bondCount = Math.Max(bondCount, n);
                }
            }

            foreach (ReactionScheme scheme in GetAllReactionSchemes())
            {
                var number = scheme.Id.Substring(1);
                int n;
                if (int.TryParse(number, out n))
                {
                    reactionSchemeCount = Math.Max(reactionSchemeCount, n);
                }
            }

            foreach (Reaction reaction in GetAllReactions())
            {
                var number = reaction.Id.Substring(1);
                int n;
                if (int.TryParse(number, out n))
                {
                    reactionCount = Math.Max(reactionCount, n);
                }
            }

            foreach (Molecule m in Molecules.Values)
            {
                m.ReLabelGuids(ref molCount, ref atomCount, ref bondCount);
            }

            foreach (ReactionScheme rs in ReactionSchemes.Values)
            {
                rs.ReLabelGuids(ref reactionSchemeCount, ref reactionCount);
            }

            foreach (Annotation an in Annotations.Values)
            {
                an.ReLabelGuids(ref annotationCount);
            }
        }

        private IEnumerable<Reaction> GetAllReactions()
        {
            var reactions = from rs in GetAllReactionSchemes()
                            from r in rs.Reactions.Values
                            select r;
            return reactions;
        }

        private IEnumerable<ReactionScheme> GetAllReactionSchemes()
        {
            var schemes = from rs in ReactionSchemes.Values
                          select rs;
            return schemes;
        }

        public void Relabel(bool includeNames)
        {
            int bondCount = 0;
            int atomCount = 0;
            int molCount = 0;
            int reactionSchemeCount = 0;
            int reactionCount = 0;
            int annotationCount = 0;

            if (Molecules.Count > 0)
            {
                foreach (Molecule m in Molecules.Values)
                {
                    m.ReLabel(ref molCount, ref atomCount, ref bondCount, includeNames);
                }
            }

            if (ReactionSchemes.Count > 0)
            {
                foreach (var scheme in ReactionSchemes.Values)
                {
                    scheme.ReLabel(ref reactionSchemeCount, ref reactionCount);
                }
            }

            if (Annotations.Count > 0)
            {
                foreach (var annotation in Annotations.Values)
                {
                    annotation.ReLabel(ref annotationCount);
                }
            }
        }

        public void Refresh()
        {
            foreach (var molecule in Molecules.Values)
            {
                molecule.Refresh();
            }
        }

        public Model Copy()
        {
            var modelCopy = new Model();

            if (Molecules.Count > 0)
            {
                foreach (var child in Molecules.Values)
                {
                    var molCopy = child.Copy();
                    modelCopy.AddMolecule(molCopy);
                    molCopy.Parent = modelCopy;
                }
            }

            if (ReactionSchemes.Count > 0)
            {
                foreach (var rs in ReactionSchemes.Values)
                {
                    var schemeCopy = rs.Copy(modelCopy);
                    modelCopy.AddReactionScheme(schemeCopy);
                    schemeCopy.Parent = modelCopy;
                }
            }

            if (Annotations.Count > 0)
            {
                foreach (var ann in Annotations.Values)
                {
                    var annCopy = ann.Copy();
                    modelCopy.AddAnnotation(annCopy);
                    annCopy.Parent = modelCopy;
                }
            }

            modelCopy.ScaledForXaml = ScaledForXaml;
            modelCopy.CustomXmlPartGuid = CustomXmlPartGuid;

            modelCopy.ExplicitC = ExplicitC;
            modelCopy.ExplicitH = ExplicitH;
            modelCopy.ShowColouredAtoms = ShowColouredAtoms;
            modelCopy.ShowMoleculeGrouping = ShowMoleculeGrouping;

            return modelCopy;
        }

        public void SetUserOptions(RenderingOptions options)
        {
            ExplicitC = options.ExplicitC;
            ExplicitH = options.ExplicitH;
            ShowColouredAtoms = options.ShowColouredAtoms;
            ShowMoleculeGrouping = options.ShowMoleculeGrouping;
        }

        public RenderingOptions GetCurrentOptions()
        {
            return new RenderingOptions(this);
        }

        private void ClearMolecules()
        {
            _molecules.Clear();
        }

        public void RemoveExplicitHydrogens()
        {
            var targets = GetHydrogenTargets();

            if (targets.Atoms.Any())
            {
                foreach (var bond in targets.Bonds)
                {
                    bond.Parent.RemoveBond(bond);
                }
                foreach (var atom in targets.Atoms)
                {
                    atom.Parent.RemoveAtom(atom);
                }
            }
        }

        public HydrogenTargets GetHydrogenTargets(List<Molecule> molecules = null)
        {
            var targets = new HydrogenTargets();

            if (molecules == null)
            {
                var allHydrogens = GetAllAtoms().Where(a => a.Element.Symbol.Equals("H")).ToList();
                ProcessHydrogens(allHydrogens);
            }
            else
            {
                foreach (var mol in molecules)
                {
                    var allHydrogens = mol.Atoms.Values.Where(a => a.Element.Symbol.Equals("H")).ToList();
                    ProcessHydrogens(allHydrogens);
                }
            }

            return targets;

            // Local function
            void ProcessHydrogens(List<Atom> hydrogens)
            {
                if (hydrogens.Any())
                {
                    foreach (var hydrogen in hydrogens)
                    {
                        // Terminal Atom?
                        if (hydrogen.Degree == 1)
                        {
                            // Not Stereo
                            if (hydrogen.Bonds.First().Stereo == BondStereo.None)
                            {
                                if (!targets.Molecules.ContainsKey(hydrogen.InternalId))
                                {
                                    targets.Molecules.Add(hydrogen.InternalId, hydrogen.Parent);
                                }
                                targets.Atoms.Add(hydrogen);
                                if (!targets.Molecules.ContainsKey(hydrogen.Bonds.First().InternalId))
                                {
                                    targets.Molecules.Add(hydrogen.Bonds.First().InternalId, hydrogen.Parent);
                                }
                                targets.Bonds.Add(hydrogen.Bonds.First());
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        ///     Ensure that bond length is between 5 and 95, and force to default if required
        /// </summary>
        /// <param name="target">Target bond length (if force true)</param>
        /// <param name="force">true to force setting of bond length to target bond length</param>
        public string EnsureBondLength(double target, bool force)
        {
            string result = string.Empty;

            if (TotalBondsCount > 0 && MeanBondLength > 0)
            {
                if (Math.Abs(MeanBondLength - target) < 0.1)
                {
                    result = string.Empty;
                }
                else
                {
                    if (force)
                    {
                        result = $"Forced BondLength from {SafeDouble.AsString(MeanBondLength)} to {SafeDouble.AsString(target)}";
                        ScaleToAverageBondLength(target);
                    }
                    else
                    {
                        if (MeanBondLength < Constants.MinimumBondLength - Constants.BondLengthTolerance
                            || MeanBondLength > Constants.MaximumBondLength + Constants.BondLengthTolerance)
                        {
                            result = $"Adjusted BondLength from {SafeDouble.AsString(MeanBondLength)} to {SafeDouble.AsString(target)}";
                            ScaleToAverageBondLength(target);
                        }
                        else
                        {
                            result = $"BondLength of {SafeDouble.AsString(MeanBondLength)} is within tolerance";
                        }
                    }
                }
            }

            return result;
        }

        public void SetAnnotationSize(double newSize)
        {
            foreach (var annotation in Annotations)
            {
                annotation.Value.SymbolSize = newSize;
            }
        }

        public void ScaleToAverageBondLength(double newLength, Point centre)
        {
            if ((TotalBondsCount + DefaultReactionScheme.Reactions.Count) > 0 && MeanBondLength > 0)
            {
                ScaleToAverageBondLength(newLength);

                var bb = BoundingBoxWithFontSize;
                var c = new Point(bb.Left + bb.Width / 2, bb.Top + bb.Height / 2);
                RepositionAll(c.X - centre.X, c.Y - centre.Y);
                _boundingBox = Rect.Empty;
            }

            if (ScaledForXaml)
            {
                XamlBondLength = newLength;
            }
        }

        public void ScaleToAverageBondLength(double newLength)
        {
            if (TotalBondsCount > 0 && MeanBondLength > 0)
            {
                double scale = newLength / MeanBondLength;
                var allAtoms = GetAllAtoms();

                foreach (var atom in allAtoms)
                {
                    atom.Position = new Point(atom.Position.X * scale, atom.Position.Y * scale);
                }

                foreach (var scheme in ReactionSchemes.Values)
                {
                    foreach (var reaction in scheme.Reactions.Values)
                    {
                        reaction.TailPoint = new Point(reaction.TailPoint.X * scale, reaction.TailPoint.Y * scale);
                        reaction.HeadPoint = new Point(reaction.HeadPoint.X * scale, reaction.HeadPoint.Y * scale);
                    }
                }

                foreach (Annotation annotation in Annotations.Values)
                {
                    annotation.Position = new Point(annotation.Position.X * scale, annotation.Position.Y * scale);
                }

                _boundingBox = Rect.Empty;
            }
        }

        public List<TextualProperty> GetUniqueNames()
        {
            var result = new Dictionary<string, TextualProperty>();

            foreach (var mol in GetAllMolecules())
            {
                foreach (var property in mol.AllTextualProperties.Where(x => x.TypeCode.Equals("N")))
                {
                    if (!result.ContainsKey(property.Id))
                    {
                        result.Add(property.Id, property);
                    }
                }
            }

            return result.Values.ToList();
        }

        public List<TextualProperty> GetUniqueFormulae()
        {
            var result = new Dictionary<string, TextualProperty>();

            foreach (var mol in GetAllMolecules())
            {
                foreach (var property in mol.AllTextualProperties.Where(x => x.TypeCode.Equals("F")))
                {
                    if (!result.ContainsKey(property.Id))
                    {
                        result.Add(property.Id, property);
                    }
                }
            }

            return result.Values.ToList();
        }

        public List<TextualProperty> GetUniqueCaptions()
        {
            var result = new Dictionary<string, TextualProperty>();

            foreach (var mol in GetAllMolecules())
            {
                foreach (var property in mol.Captions)
                {
                    if (!result.ContainsKey(property.Id))
                    {
                        result.Add(property.Id, property);
                    }
                }
            }

            return result.Values.ToList();
        }

        public List<Atom> GetAllAtoms()
        {
            List<Atom> allAtoms = new List<Atom>();
            foreach (Molecule mol in Molecules.Values)
            {
                mol.BuildAtomList(allAtoms);
            }

            return allAtoms;
        }

        public List<Bond> GetAllBonds()
        {
            List<Bond> allBonds = new List<Bond>();
            foreach (Molecule mol in Molecules.Values)
            {
                mol.BuildBondList(allBonds);
            }

            return allBonds;
        }

        public void SetAnyMissingNameIds()
        {
            foreach (Molecule m in Molecules.Values)
            {
                m.SetAnyMissingNameIds();
            }
        }

        public TextualProperty GetTextPropertyById(string id)
        {
            TextualProperty tp = null;

            foreach (var molecule in GetAllMolecules())
            {
                if (id.StartsWith(molecule.Id))
                {
                    if (id.EndsWith("f0"))
                    {
                        tp = new TextualProperty
                        {
                            Id = $"{molecule.Id}.f0",
                            Value = molecule.ConciseFormula,
                            FullType = "ConciseFormula"
                        };
                        break;
                    }

                    tp = molecule.Formulas.SingleOrDefault(f => f.Id.Equals(id));
                    if (tp != null)
                    {
                        break;
                    }

                    tp = molecule.Names.SingleOrDefault(n => n.Id.Equals(id));
                    if (tp != null)
                    {
                        break;
                    }

                    tp = molecule.Captions.SingleOrDefault(l => l.Id.Equals(id));
                    if (tp != null)
                    {
                        break;
                    }
                }
            }

            return tp;
        }

        public List<Molecule> GetAllMolecules()
        {
            List<Molecule> allMolecules = new List<Molecule>();
            foreach (Molecule mol in Molecules.Values)
            {
                mol.BuildMolList(allMolecules);
            }

            return allMolecules;
        }

        public void RescaleForCml()
        {
            if (ScaledForXaml)
            {
                double newLength = Constants.StandardBondLength / Globals.ScaleFactorForXaml;

                if (TotalBondsCount > 0 && MeanBondLength > 0)
                {
                    newLength = MeanBondLength / Globals.ScaleFactorForXaml;
                }

                ScaleToAverageBondLength(newLength);

                ScaledForXaml = false;
            }
        }

        public void RescaleForXaml(bool forDisplay, double preferredBondLength)
        {
            if (!ScaledForXaml)
            {
                double newLength;

                if (TotalBondsCount > 0 && MeanBondLength > 0)
                {
                    newLength = MeanBondLength * Globals.ScaleFactorForXaml;
                }
                else
                {
                    newLength = preferredBondLength * Globals.ScaleFactorForXaml;
                }

                ScaleToAverageBondLength(newLength);
                XamlBondLength = newLength;
                ScaledForXaml = true;

                var middle = BoundingBoxOfCmlPoints;

                if (forDisplay)
                {
                    // Move to (0,0)
                    RepositionAll(middle.Left, middle.Top);
                }

                OnPropertyChanged(this, new PropertyChangedEventArgs(nameof(BoundingBoxWithFontSize)));
                OnPropertyChanged(this, new PropertyChangedEventArgs(nameof(XamlBondLength)));
            }
        }

        /// <summary>
        /// Checks to make sure the internals of the molecule haven't become busted up.
        /// </summary>
        public List<string> CheckIntegrity()
        {
            var mols = GetAllMolecules();
            var result = new List<string>();

            foreach (Molecule mol in mols)
            {
                result.AddRange(mol.CheckIntegrity());
            }

            var atoms = GetAllAtoms().ToList();
            foreach (var atom in atoms)
            {
                var matches = atoms.Where(a => a.Id != atom.Id && SamePoint(atom.Position, a.Position, MeanBondLength * Globals.BondOffsetPercentage)).ToList();
                if (matches.Any())
                {
                    var plural = matches.Count > 1 ? "s" : "";
                    var clashes = matches.Select(a => a.Id);
                    result.Add($"Atom {atom.Id} - {atom.Element.Symbol} @ {PointHelper.AsString(atom.Position)} clashes with atom{plural} {string.Join(",", clashes)}");
                }
            }

            // Local Function
            bool SamePoint(Point a, Point b, double tolerance)
            {
                bool samePoint;

                if (a.Equals(b))
                {
                    samePoint = true;
                }
                else
                {
                    Vector v = a - b;
                    samePoint = v.Length <= tolerance;
                }

                return samePoint;
            }

            return result;
        }

        public void CentreInCanvas(Size size)
        {
            // Re-Centre scaled drawing on Canvas, does not need to be undone
            double desiredLeft = (size.Width - BoundingBoxWithFontSize.Width) / 2.0;
            double desiredTop = (size.Height - BoundingBoxWithFontSize.Height) / 2.0;
            double offsetLeft = BoundingBoxWithFontSize.Left - desiredLeft;
            double offsetTop = BoundingBoxWithFontSize.Top - desiredTop;

            RepositionAll(offsetLeft, offsetTop);
        }

        public ReactionScheme AddReactionScheme(ReactionScheme newScheme)
        {
            _reactionSchemes[newScheme.InternalId] = newScheme;
            NotifyCollectionChangedEventArgs e =
                new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add,
                    new List<ReactionScheme> { newScheme });
            UpdateReactionSchemeEventHandlers(e);
            OnReactionSchemesChanged(this, e);
            return newScheme;
        }

        private void OnReactionSchemesChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (!InhibitEvents)
            {
                var temp = ReactionSchemesChanged;
                if (temp != null)
                {
                    temp.Invoke(sender, e);
                }
            }
        }

        private void UpdateReactionSchemeEventHandlers(NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
            {
                foreach (var oldItem in e.OldItems)
                {
                    var rs = ((ReactionScheme)oldItem);
                    rs.ReactionsChanged -= OnCollectionChanged_Reactions;
                    rs.PropertyChanged -= OnPropertyChanged_ChemObject;
                }
            }

            if (e.NewItems != null)
            {
                foreach (var newItem in e.NewItems)
                {
                    var rs = ((ReactionScheme)newItem);
                    rs.ReactionsChanged += OnCollectionChanged_Reactions;
                    rs.PropertyChanged += OnPropertyChanged_ChemObject;
                }
            }
        }

        private void UpdateAnnotationsEventHandlers(NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
            {
                foreach (var oldItem in e.OldItems)
                {
                    var ann = ((Annotation)oldItem);

                    ann.PropertyChanged -= OnPropertyChanged_ChemObject;
                }
            }

            if (e.NewItems != null)
            {
                foreach (var newItem in e.NewItems)
                {
                    var ann = ((Annotation)newItem);
                    ann.PropertyChanged += OnPropertyChanged_ChemObject;
                }
            }
        }

        private void OnCollectionChanged_Reactions(object sender, NotifyCollectionChangedEventArgs e)
        {
            OnReactionsChanged(sender, e);
        }

        private void OnReactionsChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (!InhibitEvents)
            {
                var temp = ReactionsChanged;
                if (temp != null)
                {
                    temp.Invoke(sender, e);
                }
            }
        }

        public void RemoveReactionScheme(ReactionScheme scheme)
        {
            _reactionSchemes.Remove(scheme.InternalId);
            NotifyCollectionChangedEventArgs e =
                new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove,
                    new List<ReactionScheme> { scheme });
            UpdateReactionSchemeEventHandlers(e);
            OnReactionSchemesChanged(this, e);
        }

        /// <summary>
        /// Detects which bond lines are crossing.
        /// Uses a variation of Bentley–Ottmann algorithm
        /// </summary>
        public void DetectCrossingLines()
        {
            CrossedBonds = new Dictionary<string, CrossedBonds>();

            foreach (var molecule in Molecules.Values)
            {
                DetectCrossingLines(molecule);
            }
        }

        private void DetectCrossingLines(Molecule molecule)
        {
            var clippingTargets = new List<ClippingTarget>();

            // Step 1 - Fill list with simple facilitating class
            foreach (var bond in molecule.Bonds)
            {
                clippingTargets.Add(new ClippingTarget(bond));
            }

            // Step 2 - Sort the list of bonds by smallest X co-ordinate value
            clippingTargets.Sort();

            // Step 3 - Do the sweep
            foreach (var clippingTarget in clippingTargets)
            {
                // Determine if the bounding box of this line intersects with the next one
                var targets = clippingTargets
                              .Where(a => a.BoundingBox.IntersectsWith(clippingTarget.BoundingBox))
                              .ToList();
                targets.Remove(clippingTarget);

                if (targets.Count > 0)
                {
                    // If any targets found
                    foreach (var target in targets)
                    {
                        var intersection = GeometryTool.GetIntersection(clippingTarget.Start, clippingTarget.End,
                                                                              target.Start, target.End);
                        if (intersection != null)
                        {
                            if (!PointIsAtEndOfALine(intersection.Value, clippingTarget, target))
                            {
                                // Construct key
                                var names = new List<string>
                                            {
                                                target.Name,
                                                clippingTarget.Name
                                            };
                                names.Sort(); // Alphabetically
                                var key = string.Join("|", names);

                                if (!CrossedBonds.ContainsKey(key))
                                {
                                    // Only add it if it's not been seen before
                                    CrossedBonds.Add(key, new CrossedBonds(target.Bond, clippingTarget.Bond, intersection.Value));
                                }
                            }
                            else
                            {
                                // Ignore any false positive where the intersection is a line ending
                            }
                        }
                    }
                }
            }

            // Finally recurse into any child molecules
            foreach (var child in molecule.Molecules.Values)
            {
                DetectCrossingLines(child);
            }
        }

        // Detect if a point is at any end of two lines
        private bool PointIsAtEndOfALine(Point point, ClippingTarget line1, ClippingTarget line2)
        {
            return point.Equals(line1.Start) || point.Equals(line1.End) || point.Equals(line2.Start) || point.Equals(line2.End);
        }

        private int ExpandFunctionalGroup(Atom atom, string expansion)
        {
            var result = 0;

            // Expansion must be defined
            if (!string.IsNullOrEmpty(expansion))
            {
                var converter = new CMLConverter();
                var donorModel = converter.Import(expansion);

                // Expansion must be valid CML
                if (donorModel != null)
                {
                    var stars = donorModel.GetAllAtoms().Where(a => a.Element.Name.Equals("*")).ToList();
                    // Expansion must have only one star atom
                    if (stars.Count == 1)
                    {
                        Debug.WriteLine($"Expanding {atom.Element.Name} in molecule {atom.Parent.Id}");

                        var recipientMolecule = atom.Parent;

                        // Match FG Expansion scale to that of Model
                        donorModel.ScaleToAverageBondLength(MeanBondLength);

                        var donorMolecule = donorModel.Molecules.First().Value;
                        donorMolecule.Id = "donor";

                        // ReLabel to ensure there are no clashes when joining the two molecules
                        var atomCount = recipientMolecule.MaxAtomId() + 100;
                        var bondCount = recipientMolecule.MaxBondId() + 100;
                        var molCount = 100;
                        donorMolecule.ReLabel(ref molCount, ref atomCount, ref bondCount, true);

                        Debug.WriteLine($"  Adding donor molecule {donorMolecule.Id}");
                        recipientMolecule.AddMolecule(donorMolecule);
                        donorMolecule.Parent = recipientMolecule.Parent;

                        var donorAtoms = donorMolecule.Atoms.Values.ToList();
                        // star atom may be anywhere in the model
                        var starAtom = donorAtoms.First(a => a.Element.Name.Equals("*"));

                        var recipientBond = atom.Bonds.First();
                        var donorBond = starAtom.Bonds.First();

                        // Move the expansion into place
                        var recipientPosition = atom.Bonds.First().OtherAtom(atom).Position;
                        var donorAttachmentPoint = starAtom.Position;

                        var offsetX = recipientPosition.X - donorAttachmentPoint.X;
                        var offsetY = recipientPosition.Y - donorAttachmentPoint.Y;
                        donorMolecule.RepositionAll(-offsetX, -offsetY);

                        // Rotate the expansion
                        var recipientBondAngle = recipientBond.AngleStartingAt(atom);
                        var donorBondAngle = donorBond.AngleStartingAt(donorBond.OtherAtom(starAtom));
                        donorMolecule.RotateAbout(starAtom.Position, recipientBondAngle - donorBondAngle);

                        // Get the atoms which determine the fusing points
                        var donorAtom = donorBond.GetAtoms().First(a => !a.Element.Name.Equals("*"));
                        var recipientAtom = recipientBond.OtherAtom(atom);

                        // -----------------------------------------------------------------
                        // Order of removing molecules should preserve ordering of molecules
                        // -----------------------------------------------------------------

                        // Remove FG Atom
                        Debug.WriteLine($"  Removing recipient bond {recipientBond.Id}");
                        recipientMolecule.RemoveBond(recipientBond);
                        Debug.WriteLine($"  Removing recipient atom {atom.Id}");
                        recipientMolecule.RemoveAtom(atom);

                        // Remove star Atom in FG expansion
                        Debug.WriteLine($"  Removing star bond {donorBond.Id}");
                        donorMolecule.RemoveBond(donorBond);
                        Debug.WriteLine($"  Removing star atom {starAtom.Id}");
                        donorMolecule.RemoveAtom(starAtom);

                        // Join the two molecules via a new bond
                        var joiningBond = new Bond(recipientAtom, donorAtom)
                        {
                            Order = recipientBond.Order,
                            Stereo = recipientBond.Stereo,
                            Id = $"b{++bondCount}"
                        };

                        var parentOfFunctionalGroupMolecule = recipientMolecule.Parent;
                        var parentId = string.Empty;
                        if (parentOfFunctionalGroupMolecule is Model)
                        {
                            parentId = "model";
                        }
                        if (parentOfFunctionalGroupMolecule is Molecule molecule)
                        {
                            parentId = $"molecule {molecule.Id}";
                        }

                        // Create new combined molecule
                        Debug.WriteLine($"  Joining molecules {recipientMolecule.Id} and {donorMolecule.Id} by new bond {joiningBond.Id}");
                        var newMolecule = Molecule.Join(recipientMolecule, donorMolecule, joiningBond);
                        Debug.WriteLine($"  New molecule is {newMolecule.Id}");

                        Debug.WriteLine($"  Removing recipient molecule {recipientMolecule.Id}");
                        parentOfFunctionalGroupMolecule.RemoveMolecule(recipientMolecule);
                        recipientMolecule.Parent = null;

                        Debug.WriteLine($"  Removing donor molecule {donorMolecule.Id}");
                        parentOfFunctionalGroupMolecule.RemoveMolecule(donorMolecule);
                        donorMolecule.Parent = null;

                        Debug.WriteLine($"  Adding molecule {newMolecule.Id} to {parentId}");
                        parentOfFunctionalGroupMolecule.AddMolecule(newMolecule);
                        newMolecule.Parent = parentOfFunctionalGroupMolecule;

                        Debug.WriteLine($"  Re-labelling molecule {newMolecule.Id}");
                        atomCount = 0;
                        bondCount = 0;
                        molCount = 100;
                        newMolecule.ReLabel(ref molCount, ref atomCount, ref bondCount, true);

                        // Restore previous Id
                        Debug.WriteLine($"  Renaming molecule {newMolecule.Id} to {recipientMolecule.Id}");
                        newMolecule.Id = recipientMolecule.Id;

                        result++;
                    }
                }
            }

            return result;
        }

        public int ExpandAllFunctionalGroups()
        {
            var result = 0;

            var allMolecules = GetAllMolecules();
            foreach (var molecule in allMolecules)
            {
                var allAtoms = molecule.Atoms.Values.ToList();

                foreach (var atom in allAtoms)
                {
                    if (atom.Bonds.Count() == 1
                        && atom.Element is FunctionalGroup fg)
                    {
                        result += ExpandFunctionalGroup(atom, fg.Expansion);
                    }
                }
            }

            // Do not be tempted to re-label here as this will corrupt molecule id's
            // These are required to be preserved to allow matching of molecules after silent expansion (for fetching properties from property calculator)

            return result;
        }

        #endregion Methods
    }
}