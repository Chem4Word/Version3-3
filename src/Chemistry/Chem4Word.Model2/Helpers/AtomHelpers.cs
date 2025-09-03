// ---------------------------------------------------------------------------
//  Copyright (c) 2025, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.Model2.Enums;
using System.Linq;

namespace Chem4Word.Model2.Helpers
{
    public static class AtomHelpers
    {
        public static bool TryParse(string text, bool fixInternalOrLegacy, out ElementBase result)
        {
            bool success = false;
            result = null;

            if (ModelGlobals.PeriodicTable.HasElement(text))
            {
                result = ModelGlobals.PeriodicTable.Elements[text];
                success = true;
            }
            else
            {
                result = ModelGlobals.FunctionalGroupsList.FirstOrDefault(n => n.Name.Equals(text));
                success = result != null;

                if (success && fixInternalOrLegacy)
                {
                    // Force internal or legacy FG to prime Element

                    var fg = (FunctionalGroup)result;

                    if ((fg.GroupType == GroupType.Internal
                         || fg.GroupType == GroupType.Legacy)
                        && ModelGlobals.PeriodicTable.HasElement(fg.Components[0].Component))
                    {
                        result = ModelGlobals.PeriodicTable.Elements[fg.Components[0].Component];
                    }
                }
            }

            return success;
        }
    }
}