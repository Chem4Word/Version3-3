// ---------------------------------------------------------------------------
//  Copyright (c) 2026, The .NET Foundation.
//  This software is released under the Apache Licence, Version 2.0.
//  The licence and further copyright text can be found in the file LICENCE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

namespace Chem4Word.Model2.Formula
{
    public static class FormulaConstants
    {
        public static char[] SubScriptNumbers = {
                                                     '\u2080', '\u2081', '\u2082', '\u2083', '\u2084',
                                                     '\u2085', '\u2086', '\u2087', '\u2088', '\u2089'
                                                 };

        public static char[] SuperScriptNumbers = {
                                                       '\u2070', '\u00B9', '\u00B2', '\u00B3', '\u2074',
                                                       '\u2075', '\u2076', '\u2077', '\u2078', '\u2079'
                                                   };

        public const char SuperScriptPlus = '\u207a';
        public const char SuperScriptMinus = '\u207b';

        //  Bullet character <Alt>0183
        public const char BulletSeparator = '·';

        public const char BracketStart = '[';
        public const char BracketEnd = ']';
    }
}
