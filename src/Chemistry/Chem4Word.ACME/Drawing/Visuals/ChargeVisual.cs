// ---------------------------------------------------------------------------
//  Copyright (c) 2026, The .NET Foundation.
//  This software is released under the Apache Licence, Version 2.0.
//  The licence and further copyright text can be found in the file LICENCE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.ACME.Drawing.Text;
using Chem4Word.ACME.Utils;
using Chem4Word.Core.Enums;
using Chem4Word.Model2;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

namespace Chem4Word.ACME.Drawing.Visuals
{
    public class ChargeVisual : ChildTextVisual
    {
        private Point _chargeCenter;

        public ChargeVisual(AtomVisual parentVisual,
                            DrawingContext drawingContext, AtomTextMetrics mainAtomMetrics, AtomTextMetrics hMetrics)
        {
            DrawingContext = drawingContext;
            ParentVisual = parentVisual;
            ParentMetrics = mainAtomMetrics;
            HydrogenMetrics = hMetrics;
        }

        private DrawingContext DrawingContext { get; }

        public override void Render()
        {
            var chargeString = TextUtils.GetChargeString(ParentVisual.Charge);
            var chargeText = DrawChargeOrRadical(DrawingContext,
                                                 chargeString,
                                                 ParentVisual.Fill);
            chargeText.TextMetrics.FlattenedPath = chargeText.TextRun.GetOutline();
            Metrics = chargeText.TextMetrics;
        }

        /// <summary>
        /// Draws a charge or radical label at the given point
        /// </summary>
        /// <returns></returns>
        private ChargeLabelText DrawChargeOrRadical(DrawingContext drawingContext, string chargeString, Brush fill)
        {
            ChargeLabelText chargeText = new ChargeLabelText(chargeString, PixelsPerDip(), ParentVisual.SuperscriptSize);
            CoreHull = new List<Point>();

            //center the charge text on the atom to start with
            Point chargeCenter = ParentVisual.Position;
            Rect parentBoundingBox = ParentMetrics.TotalBoundingBox;

            _chargeCenter = chargeCenter;
            chargeText.MeasureAtCenter(_chargeCenter);
            Rect chargeBoundingBox = chargeText.TextMetrics.TotalBoundingBox;

            SetMultipleCharges();

            chargeText.MeasureAtCenter(_chargeCenter);

            chargeText.Fill = fill;
            chargeText.DrawAtBottomLeft(chargeText.TextMetrics.BoundingBox.BottomLeft, drawingContext);

            if (chargeText.FlattenedPath != null)
            {
                CoreHull.AddRange(chargeText.FlattenedPath);
            }

            return chargeText;
            //local function
            //places multiple charges on an atom
            void SetMultipleCharges()
            {
                if (!(HydrogenMetrics is null))
                {
                    var hbb = HydrogenMetrics.TotalBoundingBox;
                    switch (ParentVisual.HydrogenOrientation)
                    {
                        //need to take into account width of subscripted hydrogen when placing charge
                        case CompassPoints.North:
                            _chargeCenter.Y -= parentBoundingBox.Height / 2;
                            _chargeCenter.X += chargeBoundingBox.Width + Math.Max(parentBoundingBox.Width, hbb.Width) / 2;
                            break;

                        case CompassPoints.South:
                        case CompassPoints.West:
                            _chargeCenter.Y -= parentBoundingBox.Height / 2;
                            _chargeCenter.X += (chargeBoundingBox.Width + parentBoundingBox.Width) / 2;
                            break;

                        //hydrogen is out of the way
                        default:
                            {
                                if (chargeString == ModelConstants.EnDashSymbol)
                                {
                                    _chargeCenter.Y -= (parentBoundingBox.Height + chargeBoundingBox.Width * 1.1) / 2;
                                }
                                else
                                {
                                    _chargeCenter.Y -= (parentBoundingBox.Height + chargeBoundingBox.Height * 1.1) / 2;
                                }

                                _chargeCenter.X += parentBoundingBox.Width / 2;
                                break;
                            }
                    }
                }
                else //no hydrogens!
                {
                    _chargeCenter.Y -= parentBoundingBox.Height / 2;
                    _chargeCenter.X = ParentVisual.Position.X + (chargeBoundingBox.Width + parentBoundingBox.Width) / 2;
                }
            }
        }

        public Point Centroid
        {
            get
            {
                return _chargeCenter;
            }
        }
    }
}
