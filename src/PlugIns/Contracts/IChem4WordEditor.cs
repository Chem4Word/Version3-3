﻿// ---------------------------------------------------------------------------
//  Copyright (c) 2025, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System.Collections.Generic;
using System.Windows.Forms;

namespace IChem4Word.Contracts
{
    public interface IChem4WordEditor : IChem4WordCommon
    {
        DialogResult Edit();

        bool CanEditNestedMolecules { get; }
        bool CanEditFunctionalGroups { get; }
        bool CanEditReactions { get; }
        bool RequiresSeedAtom { get; }

        string DefaultRenderingOptions { get; set; }

        List<string> Used1DProperties { get; set; }
    }
}