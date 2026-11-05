// ---------------------------------------------------------------------------
//  Copyright (c) 2026, The .NET Foundation.
//  This software is released under the Apache Licence, Version 2.0.
//  The licence and further copyright text can be found in the file LICENCE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System.Windows;

namespace Chem4Word.Model2.Cleaners
{
    internal sealed class ElectronPusherSnapshot
    {
        public ElectronPusher Pusher;
        public Molecule Molecule;
        public bool AnchoredAtStart;
        public Vector First;
        public Vector Second;
    }
}
