﻿// ---------------------------------------------------------------------------
//  Copyright (c) 2025, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.Model2.Helpers;
using System;
using System.Globalization;
using System.Windows.Media;
using System.Windows.Media.TextFormatting;

namespace Chem4Word.ACME.Drawing.Text
{
    public class LabelTextRunProperties : TextRunProperties
    {
        private string _colour;

        private double _symbolSize;

        public LabelTextRunProperties(string colour, double symbolSize)
        {
            _colour = colour;
            _symbolSize = symbolSize;
        }

        public override Brush BackgroundBrush
        {
            get { return null; }
        }

        public override CultureInfo CultureInfo
        {
            get { return CultureInfo.CurrentCulture; }
        }

        public override double FontHintingEmSize
        {
            get { return _symbolSize; }
        }

        public override double FontRenderingEmSize
        {
            get { return _symbolSize; }
        }

        public override Brush ForegroundBrush
        {
            get
            {
                var brush = Brushes.Black;
                try
                {
                    if (string.IsNullOrEmpty(_colour))
                    {
                        brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(Globals.PeriodicTable.C.Colour));
                    }
                    else
                    {
                        brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(_colour));
                    }
                }
                catch (Exception)
                {
                    // Do nothing
                }
                return brush;
            }
        }

        public override System.Windows.TextDecorationCollection TextDecorations
        {
            get { return new System.Windows.TextDecorationCollection(); }
        }

        public override TextEffectCollection TextEffects
        {
            get { return new TextEffectCollection(); }
        }

        public override Typeface Typeface
        {
            get { return GlyphUtils.SymbolTypeface; }
        }

        public override TextRunTypographyProperties TypographyProperties
        {
            get
            {
                return new LabelTextRunTypographyProperties();
            }
        }
    }
}