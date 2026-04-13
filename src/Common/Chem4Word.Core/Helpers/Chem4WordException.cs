// ---------------------------------------------------------------------------
//  Copyright (c) 2026, The .NET Foundation.
//  This software is released under the Apache Licence, Version 2.0.
//  The licence and further copyright text can be found in the file LICENCE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System;
using System.Runtime.Serialization;

namespace Chem4Word.Core.Helpers
{
    [Serializable]
    public class Chem4WordException : Exception
    {
        public Chem4WordException()
        {
            // No code required
        }

        public Chem4WordException(string message)
        : base(message)
        {
            // No code required
        }

        public Chem4WordException(string message, Exception innerException)
            : base(message, innerException)
        {
            // No code required
        }

        // Ensure Exception is Serializable
        protected Chem4WordException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            // No code required
        }
    }
}
