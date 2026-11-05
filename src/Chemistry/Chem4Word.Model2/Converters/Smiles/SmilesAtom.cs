// ---------------------------------------------------------------------------
//  Copyright (c) 2026, The .NET Foundation.
//  This software is released under the Apache Licence, Version 2.0.
//  The licence and further copyright text can be found in the file LICENCE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System.Collections.Generic;

namespace Chem4Word.Model2.Converters.Smiles
{
    internal sealed class SmilesAtom
    {
        public string Symbol;
        public bool Aromatic;
        public bool Bracket;
        public int? Isotope;
        public int HydrogenCount;
        public int Charge;

        // 0 = none, 1 = @ (anticlockwise), 2 = @@ (clockwise)
        public int Chirality;

        public bool HasPrevious;

        // Neighbours in the order they appear in the string, which is what @ and @@ refer to.
        public readonly List<int> Neighbours = new List<int>();
    }
}
