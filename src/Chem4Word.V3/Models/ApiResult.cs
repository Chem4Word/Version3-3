// ---------------------------------------------------------------------------
//  Copyright (c) 2023, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System.Collections.Generic;

namespace Chem4Word.Models
{
    public class ApiResult
    {
        public bool Success { get; set; }

        public List<CatalogueEntry> Catalogue { get; set; }
        public LibraryDetails Details { get; set; }

        public int HttpStatusCode { get; set; }
        public bool HasException { get; set; }
        public string Message { get; set; }
    }
}