// ---------------------------------------------------------------------------
//  Copyright (c) 2026, The .NET Foundation.
//  This software is released under the Apache Licence, Version 2.0.
//  The licence and further copyright text can be found in the file LICENCE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System;
using System.Linq;
using System.Reflection;

namespace Chem4Word.Core.Helpers
{
    public static class UnitTestHelper
    {
        public static bool InUnitTest
        {
            get
            {
                Assembly[] list = AppDomain.CurrentDomain.GetAssemblies();
                return list.Any(a => a.FullName.ToLowerInvariant().StartsWith("xunit"));
            }
        }
    }
}
