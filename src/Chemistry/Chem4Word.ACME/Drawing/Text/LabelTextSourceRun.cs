﻿// ---------------------------------------------------------------------------
//  Copyright (c) 2025, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

namespace Chem4Word.ACME.Drawing.Text
{
    public class LabelTextSourceRun
    {
        public string Text;
        public bool IsAnchor { get; set; }
        public bool IsSubscript { get; set; }
        public bool IsSuperscript { get; set; }
        public bool IsEndParagraph;

        public int Length
        { get { return IsEndParagraph ? 1 : Text.Length; } }

        public bool IsEndOfLine { get; internal set; }
    }
}