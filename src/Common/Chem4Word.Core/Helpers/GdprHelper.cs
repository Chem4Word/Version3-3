// ---------------------------------------------------------------------------
//  Copyright (c) 2026, The .NET Foundation.
//  This software is released under the Apache Licence, Version 2.0.
//  The licence and further copyright text can be found in the file LICENCE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System;

namespace Chem4Word.Core.Helpers
{
    public static class GdprHelper
    {
        public static string ReplaceUserName(string value)
        {
            return value.Replace(Environment.UserName, "%UserName%");
        }

        public static string ReplaceDl3Path(string location)
        {
            string[] parts = location.Split('\\');

            int start = 0;

            for (int i = 0; i < parts.Length; i++)
            {
                if (parts[i] == "dl3")
                {
                    start = i;
                    break;
                }
            }

            if (start > 0)
            {
                for (int i = start + 1; i < parts.Length - 1; i++)
                {
                    parts[i] = "*";
                }
            }

            return string.Join("\\", parts);
        }
    }
}
