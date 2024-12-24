// ---------------------------------------------------------------------------
//  Copyright (c) 2025, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.ACME.Controls;
using Chem4Word.Model2;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Chem4Word.ACME
{
    public class AtomOption : ComboBoxItem
    {
        public static readonly DependencyProperty ElementProperty =
            DependencyProperty.Register("Element", typeof(ElementBase), typeof(AtomOption),
                                        new PropertyMetadata(default(ElementBase)));

        public AtomOption()
        {
            Width = double.NaN;
            FontSize = 18;
            FontWeight = FontWeights.DemiBold;
            FontFamily = new FontFamily("Arial");
        }

        public AtomOption(FunctionalGroup fg) : this()
        {
            Element = fg;
            // Foreground is now set within FunctionalGroupBlock.cs code
            Content = new FunctionalGroupBlock { ParentGroup = fg };
        }

        public AtomOption(Element elem) : this()
        {
            Element = elem;
            Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(elem.Colour));
            Content = elem.Symbol;
        }

        public ElementBase Element
        {
            get => (ElementBase)GetValue(ElementProperty);
            set => SetValue(ElementProperty, value);
        }
    }
}