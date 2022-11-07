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
    public class ListOfLibraries
    {
        [JsonProperty]
        public string SelectedLibrary { get; set; }

        [JsonProperty]
        public string DefaultLocation { get; set; }

        [JsonProperty]
        public List<DatabaseDetails> AvailableDatabases { get; set; } = new List<DatabaseDetails>();

    }
}