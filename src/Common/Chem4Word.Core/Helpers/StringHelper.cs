// ---------------------------------------------------------------------------
//  Copyright (c) 2026, The .NET Foundation.
//  This software is released under the Apache Licence, Version 2.0.
//  The licence and further copyright text can be found in the file LICENCE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System.Net.Mail;

namespace Chem4Word.Core.Helpers
{
    public static class StringHelper
    {
        public static bool IsValidEmail(string email)
        {
            var trimmedEmail = email.Trim();

            if (trimmedEmail.EndsWith("."))
            {
                return false; // suggested by @TK-421
            }

            if (trimmedEmail.Contains(".."))
            {
                return false;
            }

            try
            {
                var address = new MailAddress(trimmedEmail);
                return address.Address == trimmedEmail;
            }
            catch
            {
                return false;
            }
        }
    }
}