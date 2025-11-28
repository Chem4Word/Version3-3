// ---------------------------------------------------------------------------
//  Copyright (c) 2026, The .NET Foundation.
//  This software is released under the Apache Licence, Version 2.0.
//  The licence and further copyright text can be found in the file LICENCE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Newtonsoft.Json;
using System.Collections.Generic;

namespace Chem4Word.Searcher.ChEBIPlugin.Models
{
    public class SearchResult
    {
        [JsonProperty("results")]
        public List<Result> Results { get; set; }

        [JsonProperty("total")]
        public int Total { get; set; }

        [JsonProperty("number_pages")]
        public int NumberPages { get; set; }
    }
}
