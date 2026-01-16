// ---------------------------------------------------------------------------
//  Copyright (c) 2026, The .NET Foundation.
//  This software is released under the Apache Licence, Version 2.0.
//  The licence and further copyright text can be found in the file LICENCE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Chem4Word.Core.Helpers
{
    public static class StackHelper
    {
        public static string ShowStack(StackTrace stack)
        {
            List<string> result = new List<string>();

            string[] lines = stack.ToString().Split(Environment.NewLine.ToCharArray());

            foreach (string line in lines)
            {
                if (line.Contains(" in "))
                {
                    result.Add(line);
                }
            }

            return string.Join(Environment.NewLine, result);
        }
    }
}
