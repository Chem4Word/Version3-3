// ---------------------------------------------------------------------------
//  Copyright (c) 2026, The .NET Foundation.
//  This software is released under the Apache Licence, Version 2.0.
//  The licence and further copyright text can be found in the file LICENCE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.Core.Enums;
using Chem4Word.Model2.Annotations;
using Chem4Word.Model2.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using static Chem4Word.Model2.ModelConstants;

namespace Chem4Word.Model2
{
    /// <summary>
    /// Represents the electronic configuration of an atom or bond
    /// These may be used to represent lone pairs, radical electrons, or bond order
    /// Each Electron object is associated with a parent StructuralObject (Atom or Bond)
    /// Delocalized bonds (when available) will use this to determine any residual charges
    /// </summary>
    public class Electron : StructuralObject, INotifyPropertyChanged, IEquatable<Electron>
    {
        #region Fields

        private int _count;
        private ElectronType _type;
        private StructuralObject _parent;
        private CompassPoints? _explicitPlacement;

        #endregion Fields

        #region Constructors

        public Electron()
        {
            Errors = new List<string>();
            Warnings = new List<string>();
            InternalId = Guid.NewGuid();
            Id = InternalId.ToString("D");
        }

        #endregion Constructors

        #region Properties

        public List<string> Warnings { get; }

        public List<string> Errors { get; }

        public override string Id { get; set; }

        public override string Path
        {
            get
            {
                return Parent.Path + MoleculePathSeparator + Id;
            }
        }

        /// <summary>
        /// How many physical electrons map to the representation on screen
        /// 1 for a radical, 2 for a lone pair etc
        /// </summary>
        public int Count
        {
            get
            {
                return _count;
            }
            set
            {
                _count = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// See the ElectronType enum for details
        /// </summary>
        public ElectronType Type
        {
            get
            {
                return _type;
            }
            set
            {
                _type = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Used to explicitly set the placement of the electron
        /// Use this when configuring from CML or the editor
        /// But use the Placement property to get the actual placement
        /// Set to null to clear any explicit placement
        /// </summary>
        public CompassPoints? ExplicitPlacement
        {
            get
            {
                return _explicitPlacement;
            }
            set
            {
                _explicitPlacement = value;
                OnPropertyChanged(nameof(Placement));
            }
        }

        /// <summary>
        /// Where in space they are situated relative to the parent StructuralObject.
        /// If null, let the atom work out where to shove the electrons.
        /// Use this when displaying atoms.
        /// </summary>
        public CompassPoints? Placement
        {
            get
            {
                if (ExplicitPlacement is null && Parent is Atom atom)
                {
                    return atom.GetEmptySpaceForElectrons(this);
                }
                return ExplicitPlacement;
            }
        }

        /// <summary>
        /// Atom or Bond that this Electron is associated with
        /// </summary>
        public StructuralObject Parent
        {
            get
            {
                return _parent;
            }
            set
            {
                _parent = value;
                OnPropertyChanged();
            }
        }

        #endregion Properties

        #region Methods

        /// <summary>
        /// Retrieves a <see cref="StructuralObject"/> based on the specified path.
        /// </summary>
        /// <param name="path">The path used to locate the <see cref="StructuralObject"/>. This parameter is currently not utilized.</param>
        /// <returns>Always returns <see langword="null"/> as this implementation does not support retrieving objects by path.</returns>
        public override StructuralObject GetByPath(string path)
        {
            //electrons cannot have children (yet)
            //so return null always
            return null;
        }

        /// <summary>
        /// Used when copying molecules and the suchlike
        /// </summary>
        /// <returns></returns>
        public Electron Copy()
        {
            Electron newElectron = new Electron
            {
                Id = Id,
                Count = Count,
                Type = Type,
                ExplicitPlacement = ExplicitPlacement
            };
            return newElectron;
        }

        #endregion Methods

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        public virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion INotifyPropertyChanged

        #region IEquatable<Electron> Members

        public bool Equals(Electron other)
        {
            if (other is null)
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return Equals(PropertyChanged, other.PropertyChanged) && Path == other.Path && Count == other.Count && Type == other.Type && Placement == other.Placement;
        }

        public override bool Equals(object obj)
        {
            if (obj is null)
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (obj.GetType() != GetType())
            {
                return false;
            }

            return Equals((Electron)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = (PropertyChanged != null ? PropertyChanged.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Path != null ? Path.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ Count;
                hashCode = (hashCode * 397) ^ (int)Type;
                hashCode = (hashCode * 397) ^ (int)Placement;
                return hashCode;
            }
        }

        #endregion IEquatable<Electron> Members
    }
}
