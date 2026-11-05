// ---------------------------------------------------------------------------
//  Copyright (c) 2026, The .NET Foundation.
//  This software is released under the Apache Licence, Version 2.0.
//  The licence and further copyright text can be found in the file LICENCE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System.Collections.Generic;

namespace Chem4Word.Model2.Converters.Smiles
{
    internal sealed class Record
    {
        public ParsedSmiles Parsed;
        public Molecule Molecule;
        public Atom[] Atoms;
        public Bond[] Bonds;
        public bool[] IsBridge;

        // Explicit H atom standing in for the implicit H of a chiral bracket atom, if one had to be added.
        public readonly Dictionary<int, Atom> HydrogenFor = new Dictionary<int, Atom>();
    }
}
