// ---------------------------------------------------------------------------
//  Copyright (c) 2025, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.ACME.Drawing.LayoutSupport;
using Chem4Word.Model2;
using Chem4Word.Model2.Enums;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace Chem4Word.ACME.Drawing.Visuals
{
    public class BondVisual : ChemicalVisual
    {
        #region Properties

        public Bond ParentBond { get; }
        public double BondThickness { get; set; }

        public double Standoff { get; set; }

        #endregion Properties

        #region Fields

        private Pen _mainBondPen;
        private Pen _subsidiaryBondPen;
        private List<Point> _enclosingPoly = new List<Point>();

        #endregion Fields

        private Geometry _hullGeometry;
        public BondLayout BondLayout { get; private set; }

        public Geometry HullGeometry
        {
            get

            {
                if (_hullGeometry == null)
                {
                    if (_enclosingPoly != null && _enclosingPoly.Count > 0) //it's not a single-line bond
                    {
                        var result = Utils.Geometry.BuildPolyPath(_enclosingPoly);

                        _hullGeometry = new CombinedGeometry(result,
                                                             result.GetWidenedPathGeometry(
                                                                 new Pen(Brushes.Black, BondThickness)));
                    }
                }

                return _hullGeometry;
            }
        }

        public BondVisual(Bond bond)
        {
            ParentBond = bond;
        }

        /// <summary>
        /// Returns a BondLayout object describing the physical layout of the visual
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="startAtomVisual"></param>
        /// <param name="endAtomVisual"></param>
        /// <param name="modelXamlBondLength"></param>
        /// <param name="standoff"></param>
        /// <param name="ignoreCentroid"></param>
        /// <returns></returns>
        public static BondLayout GetBondLayout(Bond parent, AtomVisual startAtomVisual, AtomVisual endAtomVisual, double modelXamlBondLength, double standoff, bool ignoreCentroid = false)
        {
            //check to see if it's a wedge or a hatch yet
            var startAtomPosition = parent.StartAtom.Position;
            var endAtomPosition = parent.EndAtom.Position;

            Point? centroid = null;
            Point? secondaryCentroid = null;
            if (!ignoreCentroid)
            {
                if (parent.IsCyclic())
                {
                    centroid = parent.PrimaryRing?.Centroid;
                    secondaryCentroid = parent.SubsidiaryRing?.Centroid;
                }
                else
                {
                    centroid = parent.Centroid;
                }
            }

            //do the straightforward cases first -discriminate by stereo
            var parentStereo = parent.Stereo;
            var parentOrderValue = parent.OrderValue;
            var parentPlacement = parent.Placement;

            return GetBondLayout(startAtomVisual, endAtomVisual, modelXamlBondLength, parentStereo, startAtomPosition, endAtomPosition, parentOrderValue, parentPlacement, centroid, secondaryCentroid, standoff);
        }

        public static BondLayout GetBondLayout(AtomVisual startAtomVisual, AtomVisual endAtomVisual,
                                                        double modelXamlBondLength, BondStereo parentStereo,
                                                        Point startAtomPosition, Point endAtomPosition,
                                                        double? parentOrderValue, BondDirection parentPlacement,
                                                        Point? centroid, Point? secondaryCentroid, double standoff)
        {
            List<Point> startAtomHull = new List<Point>();
            List<Point> endAtomHull = new List<Point>();

            if (startAtomVisual.ParentAtom.SymbolText != "" || startAtomVisual.CarbonIsShowing)
            {
                startAtomHull = startAtomVisual.Hull;
            }
            if (endAtomVisual.ParentAtom.SymbolText != "" || endAtomVisual.CarbonIsShowing)
            {
                endAtomHull = endAtomVisual.Hull;
            }

            //stereobond: thick bond
            if (parentStereo == BondStereo.Thick && parentOrderValue == 1)
            {
                ThickBondLayout tbl = new ThickBondLayout
                {
                    Start = startAtomPosition,
                    End = endAtomPosition,
                    StartAtomHull = startAtomHull,
                    EndAtomHull = endAtomHull
                };
                BondGeometry.GetThickBondGeometry(tbl, modelXamlBondLength, standoff);
                return tbl;
            }

            //stereobonds: wedges and hatches
            if ((parentStereo == BondStereo.Wedge || parentStereo == BondStereo.Hatch)
                && parentOrderValue == 1)
            {
                WedgeBondLayout wedgeBondLayout = new WedgeBondLayout
                {
                    Start = startAtomPosition,
                    End = endAtomPosition,
                    StartAtomHull = startAtomHull,
                    EndAtomHull = endAtomHull
                };

                var endAtom = endAtomVisual.ParentAtom;
                var otherBonds = endAtom.Bonds.Except(new[] { startAtomVisual.ParentAtom.BondBetween(endAtom) })
                                        .ToList();

                Bond bond = null;
                bool oblique = true;
                if (otherBonds.Any())
                {
                    bond = otherBonds.ToArray()[0];

                    foreach (Bond b in otherBonds)
                    {
                        Atom otherAtom = b.OtherAtom(endAtom);
                        Vector v = wedgeBondLayout.End - otherAtom.Position;
                        double angle = System.Math.Abs(Vector.AngleBetween(wedgeBondLayout.PrincipleVector, v));

                        if (angle < 109.5 || angle > 130.5)
                        {
                            oblique = false;
                            break;
                        }
                    }
                }

                wedgeBondLayout.Outlined = true;
                var joiningThickBonds = (from b in otherBonds where b.Stereo == BondStereo.Thick select b).ToList();
                var joiningWedgeBonds = (from b in otherBonds where b.Stereo == BondStereo.Wedge select b).ToList();

                bool joiningBondIsThick = joiningThickBonds.Any();
                bool joiningBondIsWedge = joiningWedgeBonds.Any();

                if (joiningBondIsThick && endAtom.IsCarbon)
                //it's a thick bond it terminates at
                {
                    var otb = joiningThickBonds.First();
                    BondGeometry.GetWedgeBondGeometry(wedgeBondLayout, modelXamlBondLength, standoff);
                    BondGeometry.GetWedgeToThickGeometry(wedgeBondLayout, modelXamlBondLength, otb);
                }
                else if (joiningBondIsWedge && endAtom.IsCarbon && joiningWedgeBonds.First().EndAtom == endAtom)
                {
                    var jwb = joiningWedgeBonds.First();
                    BondGeometry.GetWedgeBondGeometry(wedgeBondLayout, modelXamlBondLength, standoff);
                    BondGeometry.GetWedgeToWedgeGeometry(wedgeBondLayout, modelXamlBondLength, jwb);
                }
                else //don't try to mitre a wedge onto a heteroatom
                {
                    bool chamferBond = otherBonds.Any()
                                       && oblique
                                       && otherBonds.All(b => b.Order == ModelConstants.OrderSingle)
                                       && bond.Order == ModelConstants.OrderSingle
                                       && endAtom.Element as Element == ModelGlobals.PeriodicTable.C
                                       && endAtom.SymbolText == "";

                    if (chamferBond)
                    {
                        var otherAtomPoints = (from b in otherBonds
                                               select b.OtherAtom(endAtom).Position).ToList();

                        if (otherAtomPoints.Any())
                        {
                            BondGeometry.GetChamferedWedgeGeometry(wedgeBondLayout, modelXamlBondLength, otherAtomPoints, standoff);
                        }
                        else
                        {
                            BondGeometry.GetWedgeBondGeometry(wedgeBondLayout, modelXamlBondLength, standoff);
                        }
                    }
                    else
                    {
                        wedgeBondLayout.Outlined = true;
                        BondGeometry.GetWedgeBondGeometry(wedgeBondLayout, modelXamlBondLength, standoff);
                    }
                }

                return wedgeBondLayout;
            }
            //wavy bond
            if (parentStereo == BondStereo.Indeterminate && parentOrderValue == 1.0)
            {
                BondLayout sbd = new BondLayout
                {
                    Start = startAtomPosition,
                    End = endAtomPosition,
                    StartAtomHull = startAtomHull,
                    EndAtomHull = endAtomHull
                };
                BondGeometry.GetWavyBondGeometry(sbd, modelXamlBondLength, standoff);
                return sbd;
            }

            switch (parentOrderValue)
            {
                //indeterminate double
                case 2 when parentStereo == BondStereo.Indeterminate:
                    DoubleBondLayout dbd = new DoubleBondLayout()
                    {
                        StartAtomHull = startAtomHull,
                        EndAtomHull = endAtomHull,
                        Start = startAtomPosition,
                        End = endAtomPosition,
                        StartNeigbourPositions = (from Atom a in startAtomVisual.ParentAtom.NeighboursExcept(endAtomVisual.ParentAtom)
                                                  select a.Position).ToList(),
                        EndNeighbourPositions = (from Atom a in endAtomVisual.ParentAtom.NeighboursExcept(startAtomVisual.ParentAtom)
                                                 select a.Position).ToList()
                    };
                    BondGeometry.GetCrossedDoubleGeometry(dbd, modelXamlBondLength, standoff);
                    return dbd;

                //partial or undefined bonds
                case 0:
                case 0.5:
                case 1.0:
                    BondLayout sbd = new BondLayout
                    {
                        Start = startAtomPosition,
                        End = endAtomPosition,
                        StartAtomHull = startAtomHull,
                        EndAtomHull = endAtomHull
                    };

                    BondGeometry.GetSingleBondGeometry(sbd, standoff);
                    return sbd;

                //double bond & 1.5 bond
                case 1.5:
                case 2:
                    DoubleBondLayout dbd2 = new DoubleBondLayout()
                    {
                        StartAtomHull = startAtomHull,
                        EndAtomHull = endAtomHull,
                        Start = startAtomPosition,
                        End = endAtomPosition,
                        Placement = parentPlacement,
                        PrimaryCentroid = centroid,
                        SecondaryCentroid = secondaryCentroid,
                        StartNeigbourPositions = (from Atom a in startAtomVisual.ParentAtom.NeighboursExcept(endAtomVisual.ParentAtom)
                                                  select a.Position).ToList(),
                        EndNeighbourPositions = (from Atom a in endAtomVisual.ParentAtom.NeighboursExcept(startAtomVisual.ParentAtom)
                                                 select a.Position).ToList()
                    };

                    BondGeometry.GetDoubleBondGeometry(dbd2, modelXamlBondLength, standoff);
                    return dbd2;

                //triple and 2.5 bond
                case 2.5:
                case 3:
                    TripleBondLayout tbd = new TripleBondLayout()
                    {
                        StartAtomHull = startAtomHull,
                        EndAtomHull = endAtomHull,
                        Start = startAtomPosition,
                        End = endAtomPosition,
                        Placement = parentPlacement,
                        PrimaryCentroid = centroid,
                        SecondaryCentroid = secondaryCentroid
                    };
                    BondGeometry.GetTripleBondGeometry(tbd, modelXamlBondLength, standoff);
                    return tbd;

                default:
                    return null;
            }
        }

        public static Brush GetHatchBrush(double angle)
        {
            Brush bondBrush;
            bondBrush = new LinearGradientBrush
            {
                MappingMode = BrushMappingMode.Absolute,
                SpreadMethod = GradientSpreadMethod.Repeat,
                StartPoint = new Point(50, 0),
                EndPoint = new Point(50, 3),
                GradientStops = new GradientStopCollection()
                                            {
                                                new GradientStop {Offset = 0d, Color = Colors.Black},
                                                new GradientStop {Offset = 0.25d, Color = Colors.Black},
                                                new GradientStop {Offset = 0.25d, Color = Colors.Transparent},
                                                new GradientStop {Offset = 0.30, Color = Colors.Transparent}
                                            },

                Transform = new RotateTransform
                {
                    Angle = angle
                }
            };
            return bondBrush;
        }

        /// <summary>
        /// Renders a bond to the display
        /// </summary>
        public override void Render()
        {
            Atom startAtom = ParentBond.StartAtom;
            Atom endAtom = ParentBond.EndAtom;

            //set up the shared variables first
            Point startPoint = startAtom.Position;
            Point endPoint = endAtom.Position;

            double standardBondLength = ParentBond.Model.XamlBondLength;

            // Only continue if bond length is not zero
            if (startPoint != endPoint)
            {
                //now get the geometry of start and end atoms
                AtomVisual startVisual = (AtomVisual)ChemicalVisuals[startAtom];
                AtomVisual endVisual = (AtomVisual)ChemicalVisuals[endAtom];

                //first grab the main descriptor
                BondLayout = GetBondLayout(ParentBond, startVisual, endVisual, standardBondLength, Standoff);

                _enclosingPoly = BondLayout.Boundary;
                //set up the default pens for rendering
                _mainBondPen = new Pen(Brushes.Black, BondThickness)
                {
                    StartLineCap = PenLineCap.Round,
                    EndLineCap = PenLineCap.Round,
                    LineJoin = PenLineJoin.Round
                };

                _subsidiaryBondPen = _mainBondPen.Clone();

                switch (ParentBond.Order)
                {
                    case ModelConstants.OrderZero:
                    case ModelConstants.OrderOther:
                    case "unknown":
                        // Handle Zero Bond
                        _mainBondPen.DashStyle = DashStyles.Dot;

                        using (DrawingContext dc = RenderOpen())
                        {
                            dc.DrawGeometry(Brushes.Black, _mainBondPen, BondLayout.DefiningGeometry);
                            //we need to draw another transparent rectangle to expand the bounding box
                            DrawHitTestOverlay(dc);
                            dc.Close();
                        }

                        DoubleBondLayout dbd = new DoubleBondLayout
                        {
                            Start = startPoint,
                            End = endPoint,
                            Placement = ParentBond.Placement
                        };

                        BondGeometry.GetDoubleBondPoints(dbd, standardBondLength);
                        _enclosingPoly = dbd.Boundary;
                        break;

                    case ModelConstants.OrderPartial01:
                        _mainBondPen.DashStyle = DashStyles.Dash;

                        using (DrawingContext dc = RenderOpen())
                        {
                            dc.DrawGeometry(Brushes.Black, _mainBondPen, BondLayout.DefiningGeometry);
                            //we need to draw another transparent thicker line on top of the existing one
                            DrawHitTestOverlay(dc);
                            dc.Close();
                        }

                        //grab the enclosing polygon as for a double ParentBond - this overcomes a hit testing bug
                        DoubleBondLayout dbd2 = new DoubleBondLayout
                        {
                            Start = startPoint,
                            End = endPoint,
                            Placement = ParentBond.Placement
                        };

                        BondGeometry.GetDoubleBondPoints(dbd2, standardBondLength);
                        _enclosingPoly = dbd2.Boundary;

                        break;

                    case "1":
                    case ModelConstants.OrderSingle:
                        // Handle Single bond
                        switch (ParentBond.Stereo)
                        {
                            case BondStereo.Indeterminate:
                            case BondStereo.None:
                            case BondStereo.Wedge:
                                using (DrawingContext dc = RenderOpen())
                                {
                                    dc.DrawGeometry(Brushes.Black, _mainBondPen, BondLayout.DefiningGeometry);
                                    //we need to draw another transparent rectangle to expand the bounding box
                                    DrawHitTestOverlay(dc);
                                    dc.Close();
                                }

                                break;

                            case BondStereo.Hatch:
                                using (DrawingContext dc = RenderOpen())
                                {
                                    var hatchBrush = GetHatchBrush(ParentBond.Angle);
                                    Pen _hatchBondPen = new Pen(hatchBrush, _mainBondPen.Thickness);

                                    dc.DrawGeometry(hatchBrush, _hatchBondPen,
                                                    BondLayout.DefiningGeometry);
                                    //we need to draw another transparent rectangle to expand the bounding box
                                    DrawHitTestOverlay(dc);
                                    dc.Close();
                                }

                                break;

                            case BondStereo.Thick:
                                using (DrawingContext dc = RenderOpen())
                                {
                                    dc.DrawGeometry(Brushes.Black, _mainBondPen, BondLayout.DefiningGeometry);
                                    DrawHitTestOverlay(dc);
                                    dc.Close();
                                }

                                break;
                        }

                        break;

                    case ModelConstants.OrderPartial12:
                    case ModelConstants.OrderAromatic:
                    case "2":
                    case ModelConstants.OrderDouble:
                        DoubleBondLayout dbd3 = (DoubleBondLayout)BondLayout;
                        Point? centroid = ParentBond.Centroid;
                        dbd3.PrimaryCentroid = centroid;

                        if (ParentBond.Order == ModelConstants.OrderPartial12 || ParentBond.Order == ModelConstants.OrderAromatic
                        ) // Handle 1.5 bond
                        {
                            _subsidiaryBondPen.DashStyle = DashStyles.Dash;
                        }

                        _enclosingPoly = dbd3.Boundary;

                        if (ParentBond.Stereo != BondStereo.Indeterminate)
                        {
                            using (DrawingContext dc = RenderOpen())
                            {
                                dc.DrawLine(_mainBondPen, BondLayout.Start, BondLayout.End);
                                dc.DrawLine(_subsidiaryBondPen,
                                            dbd3.SecondaryStart,
                                            dbd3.SecondaryEnd);
                                dc.Close();
                            }
                        }
                        else
                        {
                            using (DrawingContext dc = RenderOpen())
                            {
                                dc.DrawGeometry(_mainBondPen.Brush, _mainBondPen, BondLayout.DefiningGeometry);

                                dc.Close();
                            }
                        }

                        break;

                    case ModelConstants.OrderPartial23:
                    case "3":
                    case ModelConstants.OrderTriple:
                        if (ParentBond.Order == ModelConstants.OrderPartial23) // Handle 2.5 bond
                        {
                            _subsidiaryBondPen.DashStyle = DashStyles.Dash;
                        }

                        var tbd = BondLayout as TripleBondLayout;
                        using (DrawingContext dc = RenderOpen())
                        {
                            if (ParentBond.Placement == BondDirection.Clockwise)
                            {
                                dc.DrawLine(_mainBondPen, tbd.SecondaryStart, tbd.SecondaryEnd);
                                dc.DrawLine(_mainBondPen, tbd.Start, tbd.End);
                                dc.DrawLine(_subsidiaryBondPen, tbd.TertiaryStart, tbd.TertiaryEnd);
                            }
                            else
                            {
                                dc.DrawLine(_subsidiaryBondPen, tbd.SecondaryStart, tbd.SecondaryEnd);
                                dc.DrawLine(_mainBondPen, tbd.Start, tbd.End);
                                dc.DrawLine(_mainBondPen, tbd.TertiaryStart, tbd.TertiaryEnd);
                            }

                            dc.Close();
                        }

                        break;
                }
            }
            //local function
            void DrawHitTestOverlay(DrawingContext dc)
            {
                SolidColorBrush outliner;
#if SHOWBOUNDS
                outliner = new SolidColorBrush(Colors.LightGreen)
                {
                    Opacity = 0.4d
                };
#else
                outliner = new SolidColorBrush(Colors.Transparent);
                outliner.Opacity = 0d;
#endif

                Pen outlinePen = new Pen(outliner, BondThickness * 5);
                dc.DrawGeometry(outliner, outlinePen, BondLayout.DefiningGeometry);
            }
        }

        protected override HitTestResult HitTestCore(PointHitTestParameters hitTestParameters)
        {
            if (HullGeometry != null) //not single bond
            {
                if (HullGeometry.FillContains(hitTestParameters.HitPoint))
                {
                    return new PointHitTestResult(this, hitTestParameters.HitPoint);
                }
            }
            else
            {
                var widepen = new Pen(Brushes.Black, BondThickness * 10.0);
                if (BondLayout.DefiningGeometry.StrokeContains(widepen, hitTestParameters.HitPoint))
                {
                    return new PointHitTestResult(this, hitTestParameters.HitPoint);
                }
            }

            return null;
        }
    }
}