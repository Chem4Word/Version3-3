// ---------------------------------------------------------------------------
//  Copyright (c) 2025, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System.Windows;
using System.Windows.Controls;

namespace Chem4Word.ACME.Controls
{
    public class FormulaBlock : TextBlock
    {
        public string Formula
        {
            get { return (string)GetValue(FormulaProperty); }
            set { SetValue(FormulaProperty, value); }
        }

        public static readonly DependencyProperty FormulaProperty =
            DependencyProperty.Register("Formula", typeof(string), typeof(FormulaBlock),
                                        new FrameworkPropertyMetadata("",
                                                                      FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure,
                                                                      FormulaChangedCallback));

        private static void FormulaChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs args)
        {
            if (d is FormulaBlock formulaBlock)
            {
                var newFormula = args.NewValue.ToString();
                _ = (FormulaBlock)TextBlockHelper.FromFormula((TextBlock)formulaBlock, newFormula);
            }
        }
    }
}