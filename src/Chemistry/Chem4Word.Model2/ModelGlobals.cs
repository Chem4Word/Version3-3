// ---------------------------------------------------------------------------
//  Copyright (c) 2026, The .NET Foundation.
//  This software is released under the Apache Licence, Version 2.0.
//  The licence and further copyright text can be found in the file LICENCE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System.Collections.Generic;

namespace Chem4Word.Model2
{
    public static class ModelGlobals
    {
        public static PeriodicTable PeriodicTable = new PeriodicTable();
        public static List<FunctionalGroup> FunctionalGroupsList = FunctionalGroups.ShortcutList;
    }
}