﻿// ---------------------------------------------------------------------------
//  Copyright (c) 2025, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System.Collections.Generic;

namespace Chem4Word.Renderer.OoXmlV4.TTF
{
    public class TtfContour
    {
        public List<TtfPoint> Points { get; set; }

        public TtfContour()
        {
            Points = new List<TtfPoint>();
        }
    }
}