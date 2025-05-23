﻿// ---------------------------------------------------------------------------
//  Copyright (c) 2025, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.Core.Enums;
using Chem4Word.Core.Helpers;
using Chem4Word.Model2.Annotations;
using Chem4Word.Model2.Enums;
using Chem4Word.Model2.Helpers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;

namespace Chem4Word.Model2
{
    public class Atom : BaseObject, INotifyPropertyChanged, IEquatable<Atom>
    {
        #region Fields

        public List<string> Messages = new List<string>();

        #endregion Fields

        #region Properties

        private bool _isAllenic;
        private bool _doubletRadical;
        private Point _position;

        private CompassPoints? _explicitHPlacement;
        private CompassPoints? _explicitFGPlacement;

        /// <summary>
        /// use this property to SET H placement
        /// and to persist to XML
        /// </summary>
        public CompassPoints? ExplicitHPlacement
        {
            get
            {
                return _explicitHPlacement;
            }
            set
            {
                _explicitHPlacement = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ImplicitHPlacement));
            }
        }

        /// <summary>
        /// Use this property to DRAW H Placement
        /// exclusively
        /// </summary>
        public CompassPoints ImplicitHPlacement
        {
            get
            {
                if (_explicitHPlacement is null)
                {
                    return GetDefaultHOrientation();
                }

                return _explicitHPlacement.Value;
            }
        }

        public bool ShowImplicitHydrogenCharacters =>
            (IsHetero && (InheritedHydrogenLabels == HydrogenLabels.HeteroAndTerminal || InheritedHydrogenLabels == HydrogenLabels.Hetero))
            || IsTerminal && InheritedHydrogenLabels == HydrogenLabels.HeteroAndTerminal
            || InheritedHydrogenLabels == HydrogenLabels.All;

        public string AtomSymbol => IsSingleton || InheritedC || InheritedHydrogenLabels == HydrogenLabels.All
            ? Element.Symbol
            : SymbolText;

        public bool CarbonIsShowing => IsCarbon && (InheritedC || IsSingleton)
                                       || InheritedHydrogenLabels == HydrogenLabels.All
                                       || InheritedHydrogenLabels == HydrogenLabels.HeteroAndTerminal && IsTerminal;

        /// <summary>
        /// Use this property to SET FG placement
        /// and to persist to XML
        /// </summary>
        public CompassPoints? ExplicitFunctionalGroupPlacement
        {
            get
            {
                return _explicitFGPlacement;
            }
            set
            {
                _explicitFGPlacement = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(FunctionalGroupPlacement));
            }
        }

        /// <summary>
        /// Use this property to DRAW FG placement
        /// exclusively
        /// </summary>
        public CompassPoints? FunctionalGroupPlacement
        {
            get
            {
                if (_explicitFGPlacement is null)
                {
                    return GetDefaultFGPlacement();
                }

                return _explicitFGPlacement;
            }
        }

        private CompassPoints GetDefaultFGPlacement()
        {
            if (_element is FunctionalGroup)
            {
                if (Bonds.Count() == 1)
                {
                    var centroid = Parent.Centroid;
                    var vector = Position - centroid;
                    var angle = Vector.AngleBetween(GeometryTool.ScreenNorth, vector);
                    return angle < 0 ? CompassPoints.West : CompassPoints.East;
                }

                if (Bonds.Count() > 1)
                {
                    int leftBondCount = 0, rightBondCount = 0;
                    foreach (Atom neighbour in Neighbours)
                    {
                        Vector tempBondVector = neighbour.Position - Position;
                        double angle = Vector.AngleBetween(GeometryTool.ScreenNorth, tempBondVector);
                        if (angle >= 5.0 && angle <= 175.0)
                        {
                            rightBondCount++;
                        }
                        else
                        {
                            leftBondCount++;
                        }
                    }

                    return rightBondCount > leftBondCount ? CompassPoints.West : CompassPoints.East;
                }
            }

            return CompassPoints.East;
        }

        public bool? ExplicitC { get; set; }

        public bool InheritedC
        {
            get
            {
                switch (_element)
                {
                    case Element _:
                        return ExplicitC ?? Parent.InheritedC;

                    default:
                        return false;
                }
            }
        }

        public HydrogenLabels? ExplicitH { get; set; }

        public HydrogenLabels InheritedHydrogenLabels
        {
            get
            {
                switch (_element)
                {
                    case Element _:
                        return ExplicitH ?? Parent.InheritedHydrogenLabels;

                    default:
                        return HydrogenLabels.HeteroAndTerminal;
                }
            }
        }

        public bool IsTerminal => Bonds.Count() <= 1;

        public bool IsSingleton => !Bonds.Any();

        public bool IsCarbon
        {
            get
            {
                switch (_element)
                {
                    case Element element:
                        return element == Globals.PeriodicTable.C;

                    default:
                        return false;
                }
            }
        }

        public bool IsHetero
        {
            get
            {
                switch (_element)
                {
                    case Element _:
                        return Globals.PeriodicTable.HeteroAtomList.Contains($"|{Element.Symbol}|");

                    default:
                        return false;
                }
            }
        }

        private ElementBase _element;

        public ElementBase Element
        {
            get => _element;
            set
            {
                _element = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(SymbolText));
                OnPropertyChanged(nameof(ImplicitHydrogenCount));
                UpdateVisual();
                if (Bonds.Any())
                {
                    foreach (Bond bond in Bonds)
                    {
                        bond.UpdateVisual();
                    }
                }
            }
        }

        public IEnumerable<Atom> Neighbours => Parent.GetAtomNeighbours(this);

        public HashSet<Atom> NeighbourSet => new HashSet<Atom>(Neighbours);

        /// <summary>
        /// Count of rings that this atom is a member of
        /// </summary>
        public int RingCount
        {
            get
            {
                int result = 0;

                var allRings = Parent.Rings;
                foreach (Ring ring in allRings)
                {
                    if (ring.Atoms.Contains(this))
                    {
                        result++;
                    }
                }

                return result;
            }
        }

        /// <summary>
        /// List of rings that this atom belongs to
        /// </summary>
        public IEnumerable<Ring> Rings
        {
            get
            {
                var result = new List<Ring>();

                var allRings = Parent.Rings;
                foreach (Ring ring in allRings)
                {
                    if (ring.Atoms.Contains(this))
                    {
                        result.Add(ring);
                    }
                }

                return result;
            }
        }

        /// <summary>
        /// Detect if this atom is a member of any rings
        /// </summary>
        public bool IsInRing
        {
            get
            {
                var result = false;

                var allRings = Parent.Rings;
                foreach (Ring ring in allRings)
                {
                    if (ring.Atoms.Contains(this))
                    {
                        result = true;
                        break;
                    }
                }

                return result;
            }
        }

        public Molecule Parent { get; set; }

        public IEnumerable<Bond> Bonds
        {
            get
            {
                IEnumerable<Bond> bonds = new List<Bond>();

                if (Parent != null)
                {
                    bonds = Parent.GetBonds(InternalId);
                }

                return bonds;
            }
        }

        public int Degree => Bonds.Count();

        private string _id;

        public string Id
        {
            get => _id;
            set
            {
                if (_id != value)
                {
                    _id = value;
                }
            }
        }

        public override string Path
        {
            get
            {
                if (Parent == null)
                {
                    return Id;
                }
                else
                {
                    return Parent.Path + "/" + Id;
                }
            }
        }

        public Point Position
        {
            get => _position;
            set
            {
                _position = value;
                OnPropertyChanged();
                if (Bonds.Any())
                {
                    foreach (Bond bond in Bonds)
                    {
                        bond.UpdateVisual();
                    }
                }
            }
        }

        public bool ShowSymbol
        {
            get
            {
                bool result = true;
                _isAllenic = false;

                if (Element == null)
                {
                    result = false;
                }
                else
                {
                    if (Element is FunctionalGroup)
                    {
                        // Use initialised value of true
                    }
                    else
                    {
                        if (IsotopeNumber != null || (FormalCharge ?? 0) != 0)
                        {
                            // Use initialised value of true
                        }
                        else if (IsCarbon)
                        {
                            result = InheritedC;

                            if (Degree == 2)
                            {
                                var bonds = Bonds.ToArray();
                                // This code is triggered when adding the first Atom to a bond
                                //  at this point one of the atoms is undefined
                                Atom a1 = bonds[0].OtherAtom(this);
                                Atom a2 = bonds[1].OtherAtom(this);
                                if (a1 != null && a2 != null)
                                {
                                    double angle1 =
                                        Vector.AngleBetween(-(Position - a1.Position),
                                                            Position - a2.Position);
                                    if (Math.Abs(angle1) < 8)
                                    {
                                        if (bonds[0].OrderValue == 2
                                            && bonds[1].OrderValue == 2)
                                        {
                                            _isAllenic = true;
                                        }
                                        else
                                        {
                                            if (ExplicitC.HasValue)
                                            {
                                                result = ExplicitC.Value;
                                            }
                                        }
                                    }
                                }
                            }

                            if (IsTerminal && InheritedHydrogenLabels == HydrogenLabels.HeteroAndTerminal)
                            {
                                result = true;
                            }
                        }
                    }
                }

                return result;
            }
        }

        //tries to get an estimated bounding box for each atom symbol
        public Rect BoundingBox(double fontSize)
        {
            double halfBoxWidth = fontSize * 0.5;
            Point position = Position;
            Rect baseAtomBox = new Rect(
                new Point(position.X - halfBoxWidth, position.Y - halfBoxWidth),
                new Point(position.X + halfBoxWidth, position.Y + halfBoxWidth));
            if (SymbolText != "")
            {
                double symbolWidth = SymbolText.Length * fontSize;
                Rect mainElementBox = new Rect(
                    new Point(position.X - symbolWidth / 2, position.Y - halfBoxWidth),
                    new Size(symbolWidth, fontSize));

                if (ImplicitHydrogenCount > 0)
                {
                    Vector shift = new Vector();
                    Rect hydrogenBox = baseAtomBox;
                    switch (ImplicitHPlacement)
                    {
                        case CompassPoints.East:
                            shift = GeometryTool.ScreenEast * fontSize;
                            break;

                        case CompassPoints.North:
                            shift = GeometryTool.ScreenNorth * fontSize;
                            break;

                        case CompassPoints.South:
                            shift = GeometryTool.ScreenSouth * fontSize;
                            break;

                        case CompassPoints.West:
                            shift = GeometryTool.ScreenWest * fontSize;
                            break;
                    }

                    hydrogenBox.Offset(shift);
                    mainElementBox.Union(hydrogenBox);
                }

                return mainElementBox;
            }
            else
            {
                return baseAtomBox;
            }
        }

        public string SymbolText
        {
            get
            {
                string result = string.Empty;

                if (Element != null)
                {
                    result = Element.Symbol;

                    if (!ShowSymbol)
                    {
                        result = string.Empty;
                    }

                    if (_isAllenic)
                    {
                        result = Globals.AllenicCarbonSymbol;
                    }
                }

                return result;
            }
        }

        public object Tag { get; set; }

        private int? _isotopeNumber;

        public int? IsotopeNumber
        {
            get { return _isotopeNumber; }
            set
            {
                _isotopeNumber = value;
                OnPropertyChanged();
            }
        }

        private int? _spinMultiplicity;

        public int? SpinMultiplicity
        {
            get { return _spinMultiplicity; }
            set
            {
                _spinMultiplicity = value;
                OnPropertyChanged();
            }
        }

        private int? _formalCharge;

        public int? FormalCharge
        {
            get { return _formalCharge; }
            set
            {
                _formalCharge = value;

                OnPropertyChanged();
            }
        }

        public int ImplicitHydrogenCount
        {
            get
            {
                // Return -1 if we don't need to do anything
                int iHydrogenCount = -1;

                if (Element is FunctionalGroup)
                {
                    return iHydrogenCount;
                }

                if (Element != null)
                {
                    if (Globals.PeriodicTable.ImplicitHydrogenTargets.Contains($"|{Element.Symbol}|"))
                    {
                        int bondCount = (int)Math.Truncate(BondOrders);
                        int charge = FormalCharge ?? 0;
                        int availableElectrons = Globals.PeriodicTable.SpareValencies(Element as Element, bondCount, charge);
                        iHydrogenCount = availableElectrons <= 0 ? 0 : availableElectrons;
                    }
                }

                return iHydrogenCount;
            }
        }

        public double BondOrders
        {
            get
            {
                double order = 0d;
                if (Parent != null)
                {
                    foreach (Bond bond in Bonds)
                    {
                        order += bond.OrderValue ?? 0d;
                    }
                }

                return order;
            }
        }

        public bool DoubletRadical
        {
            get { return _doubletRadical; }
            set
            {
                _doubletRadical = value;
                //Attributed call knows who we are, no need to pass "DoubletRadical" as an argument
                OnPropertyChanged();
            }
        }

        public bool IsUnsaturated => Bonds.Any(b => b.OrderValue >= 2);

        //drawing related properties
        public Vector BalancingVector()
        {
            // This code was adapted from an answer given by ChatGPT

            var result = GeometryTool.ScreenNorth;

            if (Bonds.Any())
            {
                var angles = new List<double>();
                foreach (var bond in Bonds)
                {
                    var otherAtom = bond.OtherAtom(this);
                    var angle = Vector.AngleBetween(GeometryTool.ScreenNorth, otherAtom.Position - Position);
                    angles.Add(angle);
                }

                if (angles.Count == 1)
                {
                    var otherAtom = Bonds.First().OtherAtom(this);
                    result = Position - otherAtom.Position;
                }
                else
                {
                    var leastCrowdedAngle = FindLeastCrowdedAngle(angles);
                    result = VectorFromNorth(leastCrowdedAngle);
                }
            }

            result.Normalize();
            return result;

            // Local Functions

            double FindLeastCrowdedAngle(List<double> existingAngles)
            {
                existingAngles.Sort();

                double leastCrowdedAngle = 0;
                double maxGap = 0;

                // Iterate through pairs of adjacent existing angles to find the largest gap
                for (var i = 0; i < existingAngles.Count - 1; i++)
                {
                    var gap = existingAngles[i + 1] - existingAngles[i];
                    if (gap > maxGap)
                    {
                        maxGap = gap;
                        leastCrowdedAngle = (existingAngles[i] + existingAngles[i + 1]) / 2;
                    }
                }

                // Check for the gap between the first and last existing angles
                var firstLastGap = 360 - (existingAngles[existingAngles.Count - 1] - existingAngles[0]);
                if (firstLastGap > maxGap)
                {
                    leastCrowdedAngle = (existingAngles[existingAngles.Count - 1] + existingAngles[0] + 360) / 2;
                }

                return leastCrowdedAngle;
            }

            Vector VectorFromNorth(double angleDegrees)
            {
                var angleRadians = angleDegrees * (Math.PI / 180);

                var cosAngle = Math.Cos(angleRadians);
                var sinAngle = Math.Sin(angleRadians);
                var newX = GeometryTool.ScreenNorth.X * cosAngle - GeometryTool.ScreenNorth.Y * sinAngle;
                var newY = GeometryTool.ScreenNorth.X * sinAngle + GeometryTool.ScreenNorth.Y * cosAngle;

                return new Vector(newX, newY);
            }
        }

        private List<Atom> UnprocessedNeighbours(Predicate<Atom> unprocessedTest)
        {
            return Neighbours.Where(a => unprocessedTest(a)).ToList();
        }

        /// <summary>
        /// How many atoms we haven't 'done' yet when we're traversing the graph
        /// </summary>
        public int UnprocessedDegree(Predicate<Atom> unprocessedTest) => UnprocessedNeighbours(unprocessedTest).Count;

        public int UnprocessedDegree(Predicate<Atom> unprocessedTest, HashSet<Bond> excludeBonds)
        {
            var unproc = from a in UnprocessedNeighbours(unprocessedTest)
                         where !excludeBonds.Contains(this.BondBetween(a)) && unprocessedTest(a)
                         select a;
            return unproc.Count();
        }

        #endregion Properties

        #region Constructors

        public Atom()
        {
            InternalId = Guid.NewGuid();
            Id = InternalId.ToString("D");
        }

        /// <summary>
        /// The internal ID ties atoms and bonds together
        /// </summary>
        public Guid InternalId { get; internal set; }

        public bool Singleton => Parent?.Atoms.Count == 1 && Parent?.Atoms.Values.First() == this;

        #endregion Constructors

        #region Methods

        public List<Atom> NeighboursExcept(Atom toIgnore)
        {
            return Neighbours.Where(a => a != toIgnore).ToList();
        }

        public List<Atom> NeighboursExcept(params Atom[] toIgnore)
        {
            return Neighbours.Where(a => !toIgnore.Contains(a)).ToList();
        }

        public Bond BondBetween(Atom atom)
        {
            foreach (var parentBond in Parent._bonds)
            {
                if (parentBond.StartAtomInternalId.Equals(InternalId) && parentBond.EndAtomInternalId.Equals(atom.InternalId))
                {
                    return parentBond;
                }
                if (parentBond.EndAtomInternalId.Equals(InternalId) && parentBond.StartAtomInternalId.Equals(atom.InternalId))
                {
                    return parentBond;
                }
            }
            return null;
        }

        private CompassPoints GetDefaultHOrientation()
        {
            var orientation = CompassPoints.East;

            if (ImplicitHydrogenCount >= 1 && Bonds.Any())
            {
                orientation = GetEmptySpaceForHs();
            }

            return orientation;
        }

        public CompassPoints GetEmptySpaceForHs()
        {
            CompassPoints orientation;
            double angleFromNorth = Vector.AngleBetween(GeometryTool.ScreenNorth, BalancingVector());
            orientation = Bonds.Count() == 1
                ? GeometryTool.SnapTo2EW(angleFromNorth)
                : GeometryTool.SnapTo4NESW(angleFromNorth);
            return orientation;
        }

        //notification methods
        public void UpdateVisual()
        {
            OnPropertyChanged(nameof(SymbolText));
        }

        #endregion Methods

        #region Overrides

        public override string ToString()
        {
            var symbol = Element != null ? Element.Symbol : "???";
            return $"Atom {Id} - {Path}: {symbol} @ {PointHelper.AsString(Position)}";
        }

        public override int GetHashCode()
        {
            return InternalId.GetHashCode();
        }

        #endregion Overrides

        #region Events

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion INotifyPropertyChanged

        #endregion Events

        public void SendDummyNotify()
        {
            OnPropertyChanged(nameof(SymbolText));
        }

        public bool Equals(Atom other)
        {
            if (other is null)
            {
                return false;
            }
            return other.InternalId == this.InternalId;
        }

        /// <summary>
        /// indicates whether an atom has exceeded its maximum valence count
        /// </summary>
        public bool Overbonded
        {
            get
            {
                int bondCount = (int)Math.Truncate(BondOrders);
                int charge = FormalCharge ?? 0;
                int availableElectrons = Globals.PeriodicTable.SpareValencies(Element as Element, bondCount, charge);
                bool result = availableElectrons < 0;
                return result;
            }
        }
    }
}