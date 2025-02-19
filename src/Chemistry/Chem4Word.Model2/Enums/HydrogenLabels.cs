﻿// ---------------------------------------------------------------------------
//  Copyright (c) 2025, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System.ComponentModel;

namespace Chem4Word.Model2.Enums
{
    public enum HydrogenLabels
    {
        /// <summary>
        /// Don't show hydrogen labels
        /// </summary>
        [Description("None")]
        None,

        /// <summary>
        /// Show hydrogen labels for both Hetero and Terminal atoms
        /// </summary>
        [Description("Hetero and Terminal")]
        HeteroAndTerminal,

        /// <summary>
        /// Show hydrogen labels for only Hetero atoms
        /// </summary>
        [Description("Hetero")]
        Hetero,

        /// <summary>
        /// Show hydrogen labels for all atoms
        /// </summary>
        [Description("All")]
        All
    }
}