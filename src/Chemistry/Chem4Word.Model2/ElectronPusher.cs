// ---------------------------------------------------------------------------
//  Copyright (c) 2026, The .NET Foundation.
//  This software is released under the Apache Licence, Version 2.0.
//  The licence and further copyright text can be found in the file LICENCE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------
using Chem4Word.Model2.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using static Chem4Word.Model2.ModelConstants;

namespace Chem4Word.Model2
{
    public partial class ElectronPusher : StructuralObject, INotifyPropertyChanged
    {
        private Point _firstControlPoint;
        private IChemistryContainer _parent;
        private ElectronPusherType _pusherType;
        private StructuralObject _startChemistry;
        private readonly List<StructuralObject> _endChemistries;
        private Point _secondControlPoint;


        public ElectronPusher()
        {
            InternalId = Guid.NewGuid();
            Id = InternalId.ToString("D");
            _endChemistries = new List<StructuralObject>();
        }

        public IChemistryContainer Parent
        {
            get
            {
                return _parent;
            }
            set
            {
                _parent = value;
            }
        }

        private Model Model
        {
            get
            {
                return Parent as Model;
            }
        }

        public override string Path
        {
            get
            {
                var path = "";

                if (Parent == null)
                {
                    path = Id;
                }

                if (Parent is Model model)
                {
                    path = model.Path + Id;
                }

                if (Parent is Molecule molecule)
                {
                    path = molecule.Path + MoleculePathSeparator + Id;
                }

                return path;
            }
        }

        public override StructuralObject GetByPath(string path)
        {
            //ElectronPushers do not support child objects, period.
            return null;
        }

        //actual chemical properties

        public ElectronPusherType PusherType
        {
            get
            {
                return _pusherType;
            }
            set
            {
                _pusherType = value;
                OnPropertyChanged();
            }
        }

        public StructuralObject StartChemistry
        {
            get
            {
                return _startChemistry;
            }
            set
            {
                _startChemistry = value;
                OnPropertyChanged();
            }
        }

        public List<StructuralObject> EndChemistries
        {
            get
            {
                return _endChemistries;
            }
        }

        public Point FirstControlPoint
        {
            get
            {
                return _firstControlPoint;
            }
            set
            {
                _firstControlPoint = value;
                OnPropertyChanged();
            }
        }

        public Point SecondControlPoint
        {
            get
            {
                return _secondControlPoint;
            }
            set
            {
                _secondControlPoint = value;
                OnPropertyChanged();
            }
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void ReLabel(ref int electronPusherCount)
        {
            Id = $"ep{++electronPusherCount}";
        }

        public void ReLabelGuids(ref int epCount)
        {
            if (Guid.TryParse(Id, out Guid guid))
            {
                Id = $"ep{++epCount}";
            }
        }

        public void RepositionAll(double x, double y)
        {
            Vector offsetVector = new Vector(-x, -y);
            FirstControlPoint += offsetVector;
            SecondControlPoint += offsetVector;
        }

        public void UpdateVisual()
        {
            OnPropertyChanged(nameof(PusherType));
        }

        public ElectronPusher Copy(Model newCopy)
        {
            var newStartChemistry = newCopy.GetByPath(StartChemistry.Path);
            var copy = new ElectronPusher()
                       {
                           Id = Id,
                           StartChemistry = newStartChemistry,
                           FirstControlPoint = FirstControlPoint,
                           SecondControlPoint = SecondControlPoint,
                           PusherType = PusherType
                       };

            foreach (StructuralObject chemistry in EndChemistries)
            {
                var newEndChemistry = newCopy.GetByPath(chemistry.Path);
                copy.EndChemistries.Add(newEndChemistry);
            }
            return copy;
        }
    }
}
