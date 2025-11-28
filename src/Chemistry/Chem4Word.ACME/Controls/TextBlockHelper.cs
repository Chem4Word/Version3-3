// ---------------------------------------------------------------------------
//  Copyright (c) 2026, The .NET Foundation.
//  This software is released under the Apache Licence, Version 2.0.
//  The licence and further copyright text can be found in the file LICENCE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System.Windows.Controls;
using System.Windows.Documents;

namespace Chem4Word.ACME.Controls
{
    public static class TextBlockHelper
    {
        /// <summary>
        /// Create a TextBox with the chemical formula
        /// </summary>
        /// <param name="unicode"></param>
        /// <param name="prefix"></param>
        /// <returns></returns>
        public static TextBlock FromUnicode(string unicode, string prefix = null) => FromUnicode(null, unicode, prefix);

        public static TextBlock FromUnicode(TextBlock textBlock, string unicode, string prefix)
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
                Run run = new Run($"{prefix} ");
                textBlock.Inlines.Add(run);
            }

            Run run2 = new Run($"{unicode}");
            textBlock.Inlines.Add(run2);

            return textBlock;
        }
    }
}
