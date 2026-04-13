// ---------------------------------------------------------------------------
//  Copyright (c) 2026, The .NET Foundation.
//  This software is released under the Apache Licence, Version 2.0.
//  The licence and further copyright text can be found in the file LICENCE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.Core.Helpers;
using Chem4Word.Renderer.OoXmlV4.TTF;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Reflection;

namespace Chem4Word.Renderer.OoXmlV4.Helpers
{
    public static class FontHelper
    {
        public static Dictionary<char, TtfCharacter> LoadFont(string fontName)
        {
            string json = ResourceHelper.GetStringResource(Assembly.GetExecutingAssembly(), "Arial.json");
            return JsonConvert.DeserializeObject<Dictionary<char, TtfCharacter>>(json);
        }
    }
}
