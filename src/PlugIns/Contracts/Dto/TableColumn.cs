// ---------------------------------------------------------------------------
//  Copyright (c) 2025, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System.Collections.Generic;

namespace IChem4Word.Contracts.Dto
{
    public class TableColumn
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public string NotNull { get; set; }

        #region Overrides of Object

        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }

            if (obj is TableColumn tc)
            {
                return tc.Name.Equals(Name) && tc.Type.Equals(Type) && tc.NotNull.Equals(NotNull);
            }

            return false;
        }

        public override int GetHashCode()
        {
            int hashCode = 613504982;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Name);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Type);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(NotNull);
            return hashCode;
        }

        #endregion Overrides of Object
    }
}