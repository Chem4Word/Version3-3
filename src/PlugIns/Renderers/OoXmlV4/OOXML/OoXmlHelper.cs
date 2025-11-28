// ---------------------------------------------------------------------------
//  Copyright (c) 2026, The .NET Foundation.
//  This software is released under the Apache Licence, Version 2.0.
//  The licence and further copyright text can be found in the file LICENCE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.Model2;
using Chem4Word.Renderer.OoXmlV4.Entities;
using DocumentFormat.OpenXml;
using System.Windows;

namespace Chem4Word.Renderer.OoXmlV4.OoXml
{
    public static class OoXmlHelper
    {
        public static double BracketOffset(double bondLength)
            => bondLength * OoXmlConstants.BracketOffsetPercentage;

        public static Rect GetAllCharacterExtents(Model model, RendererOutputs outputs)
        {
            var characterExtents = model.BoundingBoxOfCmlPoints;

            foreach (var alc in outputs.AtomLabelCharacters)
            {
                if (alc.IsSmaller)
                {
                    var r = new Rect(alc.Position,
                                     new Size(ScaleCsTtfToCml(alc.Character.Width, model.MeanBondLength) * OoXmlConstants.SubscriptScaleFactor,
                                              ScaleCsTtfToCml(alc.Character.Height, model.MeanBondLength) * OoXmlConstants.SubscriptScaleFactor));
                    characterExtents.Union(r);
                }
                else
                {
                    var r = new Rect(alc.Position,
                                     new Size(ScaleCsTtfToCml(alc.Character.Width, model.MeanBondLength),
                                              ScaleCsTtfToCml(alc.Character.Height, model.MeanBondLength)));
                    characterExtents.Union(r);
                }
            }

            foreach (var group in outputs.AllMoleculeExtents)
            {
                characterExtents.Union(group.ExternalCharacterExtents);
            }

            // Bullet proofing - Error seen in telemetry :-
            // System.InvalidOperationException: Cannot call this method on the Empty Rect.
            //   at System.Windows.Rect.Inflate(Double width, Double height)
            if (characterExtents == Rect.Empty)
            {
                characterExtents = new Rect(new Point(0, 0), new Size(OoXmlConstants.DrawingMargin * 10, OoXmlConstants.DrawingMargin * 10));
            }
            else
            {
                characterExtents.Inflate(OoXmlConstants.DrawingMargin, OoXmlConstants.DrawingMargin);
            }

            return characterExtents;
        }

        /// <summary>
        /// Scales a CML X or Y co-ordinate to DrawingML Units (EMU)
        /// </summary>
        /// <param name="XorY"></param>
        /// <returns></returns>
        public static Int64Value ScaleCmlToEmu(double XorY)
        {
            var scaled = XorY * OoXmlConstants.EmusPerCmlPoint;
            return Int64Value.FromInt64((long)scaled);
        }

        #region C# TTF

        // These calculations yield a font which has a point size of 8 at a bond length of 20
        private static double EmusPerCsTtfPoint(double bondLength)
            => bondLength / 2.5;

        /// <summary>
        /// Scales a CS TTF SubScript X or Y co-ordinate to DrawingML Units (EMU)
        /// <param name="XorY"></param>
        /// <param name="bondLength"></param>
        /// </summary>
        public static Int64Value ScaleCsTtfSubScriptToEmu(double XorY, double bondLength)
        {
            if (bondLength > 0.1)
            {
                var scaled = XorY * EmusPerCsTtfPointSubscript(bondLength);
                return Int64Value.FromInt64((long)scaled);
            }
            else
            {
                var scaled = XorY * EmusPerCsTtfPointSubscript(20);
                return Int64Value.FromInt64((long)scaled);
            }
        }

        /// <summary>
        /// Scales a C# TTF X or Y co-ordinate to CML Units
        /// </summary>
        /// <param name="XorY"></param>
        /// <param name="bondLength"></param>
        /// <returns></returns>
        public static double ScaleCsTtfToCml(double XorY, double bondLength)
        {
            if (bondLength > 0.1)
            {
                return XorY / CsTtfToCml(bondLength);
            }
            else
            {
                return XorY / CsTtfToCml(20);
            }
        }

        /// <summary>
        /// Scales a C# TTF X or Y co-ordinate to DrawingML Units (EMU)
        /// </summary>
        /// <param name="XorY"></param>
        /// <param name="bondLength"></param>
        /// <returns></returns>
        public static Int64Value ScaleCsTtfToEmu(double XorY, double bondLength)
        {
            if (bondLength > 0.1)
            {
                var scaled = XorY * EmusPerCsTtfPoint(bondLength);
                return Int64Value.FromInt64((long)scaled);
            }
            else
            {
                var scaled = XorY * EmusPerCsTtfPoint(20);
                return Int64Value.FromInt64((long)scaled);
            }
        }

        private static double CsTtfToCml(double bondLength)
        {
            if (bondLength > 0.1)
            {
                return OoXmlConstants.EmusPerCmlPoint / EmusPerCsTtfPoint(bondLength);
            }
            else
            {
                return OoXmlConstants.EmusPerCmlPoint / EmusPerCsTtfPoint(20);
            }
        }

        private static double EmusPerCsTtfPointSubscript(double bondLength)
        {
            if (bondLength > 0.1)
            {
                return EmusPerCsTtfPoint(bondLength) * OoXmlConstants.SubscriptScaleFactor;
            }
            else
            {
                return EmusPerCsTtfPoint(20) * OoXmlConstants.SubscriptScaleFactor;
            }
        }

        #endregion C# TTF
    }
}