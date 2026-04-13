// ---------------------------------------------------------------------------
//  Copyright (c) 2026, The .NET Foundation.
//  This software is released under the Apache Licence, Version 2.0.
//  The licence and further copyright text can be found in the file LICENCE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Chem4Word.Core.Helpers
{
    public static class StackHelper
    {
        /// <summary>
        /// Dumps stack trace
        /// Example of usage :-
        ///   StackTrace stackTrace = new StackTrace(true);
        ///   string stack = StackHelper.ShowStack(stackTrace);
        ///   Debug.WriteLine($"Electron {Path} - Placement: {type} -> {result}\n{stack}");
        /// </summary>
        /// <param name="stack"></param>
        /// <param name="onlyOurs"></param>
        /// <returns></returns>
        public static string ShowStack(StackTrace stack, bool onlyOurs = true)
        {
            List<string> result = new List<string>();

            string[] lines = stack.ToString().Split(Environment.NewLine.ToCharArray(), StringSplitOptions.RemoveEmptyEntries);

            if (onlyOurs)
            {
                if (lines.Any())
                {
                    foreach (string line in lines)
                    {
                        if (line.Contains(" in "))
                        {
                            result.Add(line);
                        }
                    }
                }
            }
            else
            {
                result = lines.ToList();
            }
            return string.Join(Environment.NewLine, result);
        }
    }
}
