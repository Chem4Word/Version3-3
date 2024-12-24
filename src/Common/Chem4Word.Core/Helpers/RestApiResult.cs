// ---------------------------------------------------------------------------
//  Copyright (c) 2025, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System;

namespace Chem4Word.Core.Helpers
{
    public class RestApiResult
    {
        // The Result
        public string Json { get; set; }

        public byte[] Bytes { get; set; }

        // Information
        public int HttpStatusCode { get; set; }

        public bool Success { get; set; }
        public TimeSpan Duration { get; set; }

        // Exception details
        public bool HasException { get; set; }

        public string Message { get; set; }
    }
}