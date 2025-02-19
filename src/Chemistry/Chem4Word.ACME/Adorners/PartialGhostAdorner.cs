// ---------------------------------------------------------------------------
//  Copyright (c) 2025, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.ACME.Controls;
using Chem4Word.ACME.Drawing.Visuals;
using Chem4Word.ACME.Utils;
using Chem4Word.Model2;
using Chem4Word.Model2.Annotations;
using Chem4Word.Model2.Enums;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace Chem4Word.ACME.Adorners
{
    public class PartialGhostAdorner : Adorner
    {
        [NotNull]
        private SolidColorBrush _ghostBrush;

        [NotNull]
        private Pen _ghostPen;

        [NotNull]
        private Transform _shear;

        public Transform Shear
        {
            get => _shear;
            set => _shear = value;
        }

        private IEnumerable<Atom> _atomList;

        public IEnumerable<Atom> AtomList
        {
            get { return _atomList; }
            set
            {
                _atomList = value;
                InvalidateVisual();
            }
        }

        public EditorCanvas CurrentEditor { get; }
        public EditController CurrentController { get; }

        public PartialGhostAdorner(EditController controller) : base(
            controller.CurrentEditor)
        {
            var myAdornerLayer = AdornerLayer.GetAdornerLayer(controller.CurrentEditor);
            myAdornerLayer.Add(this);
            PreviewMouseMove += OnPreviewMouseMove_PartialGhostAdorner;
            PreviewMouseUp += OnPreviewMouseUp_PartialGhostAdorner;
            MouseUp += OnMouseUp_PartialGhostAdorner;
            CurrentController = controller;

            CurrentEditor = CurrentController.CurrentEditor;
        }

        private void OnMouseUp_PartialGhostAdorner(object sender, MouseButtonEventArgs e)
        {
            CurrentEditor.RaiseEvent(e);
        }

        private void OnPreviewMouseUp_PartialGhostAdorner(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            CurrentEditor.RaiseEvent(e);
        }

        private void OnPreviewMouseMove_PartialGhostAdorner(object sender, System.Windows.Input.MouseEventArgs e)
        {
            CurrentEditor.RaiseEvent(e);
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            _ghostBrush = (SolidColorBrush)FindResource(Common.GhostBrush);
            _ghostPen = new Pen(_ghostBrush, Common.BondThickness);

            HashSet<Bond> bondSet = new HashSet<Bond>();
            Dictionary<Atom, Point> transformedPositions = new Dictionary<Atom, Point>();

            //compile a set of all the neighbours of the selected atoms

            foreach (Atom atom in AtomList)
            {
                foreach (Atom neighbour in atom.Neighbours)
                {
                    //add in all the existing position for neigbours not in selected atoms
                    if (!AtomList.Contains(neighbour))
                    {
                        //neighbourSet.Add(neighbour); //don't worry about adding them twice
                        transformedPositions[neighbour] = neighbour.Position;
                    }
                }

                //add in the bonds
                foreach (Bond bond in atom.Bonds)
                {
                    bondSet.Add(bond); //don't worry about adding them twice
                }

                transformedPositions[atom] = Shear.Transform(atom.Position);
            }

            var modelXamlBondLength = CurrentController.Model.XamlBondLength;
            double atomRadius = modelXamlBondLength / 7.50;

            foreach (Bond bond in bondSet)
            {
                var startAtomPosition = transformedPositions[bond.StartAtom];
                var endAtomPosition = transformedPositions[bond.EndAtom];
                if (bond.OrderValue != 1.0 ||
                    !(bond.Stereo == BondStereo.Hatch || bond.Stereo == BondStereo.Wedge))
                {
                    var descriptor = BondVisual.GetBondDescriptor(CurrentEditor.GetAtomVisual(bond.StartAtom),
                                                                  CurrentEditor.GetAtomVisual(bond.EndAtom),
                                                                  modelXamlBondLength,
                                                                  bond.Stereo, startAtomPosition, endAtomPosition,
                                                                  bond.OrderValue,
                                                                  bond.Placement, bond.Centroid,
                                                                  bond.SubsidiaryRing?.Centroid,
                                                                  CurrentEditor.Controller.Standoff);
                    descriptor.Start = startAtomPosition;
                    descriptor.End = endAtomPosition;
                    var bondGeometry = descriptor.DefiningGeometry;
                    drawingContext.DrawGeometry(_ghostBrush, _ghostPen, bondGeometry);
                }
                else
                {
                    drawingContext.DrawLine(_ghostPen, startAtomPosition, endAtomPosition);
                }
            }

            foreach (Atom atom in transformedPositions.Keys)
            {
                var newPosition = transformedPositions[atom];

                if (atom.SymbolText != "")
                {
                    drawingContext.DrawEllipse(SystemColors.WindowBrush, _ghostPen, newPosition, atomRadius, atomRadius);
                }
            }
        }
    }
}