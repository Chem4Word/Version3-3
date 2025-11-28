// ---------------------------------------------------------------------------
//  Copyright (c) 2026, The .NET Foundation.
//  This software is released under the Apache Licence, Version 2.0.
//  The licence and further copyright text can be found in the file LICENCE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System;

namespace Chem4Word.Model2
{
    public abstract class StructuralObject
    {
        public abstract string Path { get; }

        public abstract StructuralObject GetByPath(string path);
        public virtual string Id { get; set; }

        public virtual Guid InternalId { get; internal set; }
    }
}
