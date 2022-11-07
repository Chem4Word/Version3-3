// ---------------------------------------------------------------------------
//  Copyright (c) 2022, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System.Collections.Generic;
using Newtonsoft.Json;

namespace IChem4Word.Contracts
{
    [JsonObject(MemberSerialization.OptIn)]
    public class DatabaseDetails
    {
        [JsonProperty]
        public string DisplayName { get; set; }

        [JsonProperty]
        public string Connection { get; set; }

        [JsonProperty]
        public string ShortFileName { get; set; }

        [JsonProperty]
        public string Driver { get; set; }

        public Dictionary<string, string> Properties { get; set; } = new Dictionary<string, string>();
    }
}