// ---------------------------------------------------------------------------
//  Copyright (c) 2026, The .NET Foundation.
//  This software is released under the Apache Licence, Version 2.0.
//  The licence and further copyright text can be found in the file LICENCE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

namespace Chem4Word.Model2.Converters.Smiles
{
    internal sealed class SmilesBond
    {
        public int From;
        public int To;
        public int Order = 1;
        public bool Aromatic;

        // +1 for '/', -1 for '\', as written from From to To.
        public int Direction;
    }
}
