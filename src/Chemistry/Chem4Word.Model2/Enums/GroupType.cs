// ---------------------------------------------------------------------------
//  Copyright (c) 2026, The .NET Foundation.
//  This software is released under the Apache Licence, Version 2.0.
//  The licence and further copyright text can be found in the file LICENCE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

namespace Chem4Word.Model2.Enums
{
    // When the Functional Group Expansion Editor is run the result which is copied to the clipboard is sorted by this enum
    public enum GroupType
    {
        AttachmentPoint = 0,
        Residue,
        Legacy,
        Internal,
        SuperAtom,
        Placeholder,
        Unknown
    }
}