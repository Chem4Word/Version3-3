// ---------------------------------------------------------------------------
//  Copyright (c) 2025, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.Model2.Enums;
using Chem4Word.Model2.Helpers;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace Chem4Word.ACME.Controls
{
    public static class TextBlockHelper
    {
        /// <summary>
        /// Create a TextBox with the chemical formula
        /// </summary>
        /// <param name="formula"></param>
        /// <param name="prefix"></param>
        /// <returns></returns>
        public static TextBlock FromFormula(string formula, string prefix = null) => FromFormula(null, formula, prefix);

        /// <summary>
        /// This interface may look weird parsing the TextBlock in AND returning it,
        ///  but this is to enable the common code here to be used
        ///   in the FormulaBlock callback as well as when a TextBlock with a formula is required
        /// </summary>
        /// <param name="textBlock"></param>
        /// <param name="formula"></param>
        /// <param name="prefix"></param>
        /// <returns></returns>
        public static TextBlock FromFormula(TextBlock textBlock, string formula, string prefix = null)
        {
            if (textBlock == null)
            {
                // We create the TextBlock in normal TextBlock mode
                textBlock = new TextBlock();
            }
            else
            {
                // We execute this code in FormulaBlock callback mode to clear the previous value
                textBlock.Text = string.Empty;
                textBlock.Inlines.Clear();
            }

            // Add in the new prefix if required
            if (!string.IsNullOrEmpty(prefix))
            {
                var run = new Run($"{prefix} ");
                textBlock.Inlines.Add(run);
            }

            var parts = FormulaHelper.ParseFormulaIntoParts(formula);
            foreach (var formulaPart in parts)
            {
                // Add in the formula parts
                switch (formulaPart.PartType)
                {
                    case FormulaPartType.Multiplier:
                    case FormulaPartType.Separator:
                        var run1 = new Run(formulaPart.Text);
                        textBlock.Inlines.Add(run1);
                        break;

                    case FormulaPartType.Element:
                        var run2 = new Run(formulaPart.Text);
                        textBlock.Inlines.Add(run2);
                        if (formulaPart.Count > 1)
                        {
                            var subscript = new Run($"{formulaPart.Count}")
                            {
                                BaselineAlignment = BaselineAlignment.Subscript
                            };
                            subscript.FontSize -= 2;
                            textBlock.Inlines.Add(subscript);
                        }
                        break;

                    case FormulaPartType.Charge:
                        var absCharge = Math.Abs(formulaPart.Count);
                        if (absCharge > 1)
                        {
                            var superscript1 = new Run($"{absCharge}{formulaPart.Text}")
                            {
                                BaselineAlignment = BaselineAlignment.Top
                            };
                            superscript1.FontSize -= 3;
                            textBlock.Inlines.Add(superscript1);
                        }
                        else
                        {
                            var superscript2 = new Run($"{formulaPart.Text}")
                            {
                                BaselineAlignment = BaselineAlignment.Top
                            };
                            superscript2.FontSize -= 3;
                            textBlock.Inlines.Add(superscript2);
                        }
                        break;
                }
            }

            return textBlock;
        }
    }
}