// ---------------------------------------------------------------------------
//  Copyright (c) 2025, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using IChem4Word.Contracts;
using System.Windows.Media;

namespace Chem4Word.ACME
{
    public static class ACMEGlobals
    {
        public static IChem4WordTelemetry Telemetry { get; set; }

        public static Color HoverAdornerColor => (Color)ColorConverter.ConvertFromString(AcmeConstants.HoverAdornerColorDef);
        public static Color Chem4WordColor => (Color)ColorConverter.ConvertFromString(AcmeConstants.Chem4WordColorDef);
        public static Color GroupBracketColor => (Color)ColorConverter.ConvertFromString(AcmeConstants.GroupBracketColorDef);
        public const int MinAtomCharge = -5;
        public const int MaxAtomCharge = 9;
        public const int MinMoleculeCharge = -8;
        public const int MaxMoleculeCharge = 8;
    }
}
