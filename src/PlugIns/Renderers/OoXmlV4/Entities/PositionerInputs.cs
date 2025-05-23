﻿// ---------------------------------------------------------------------------
//  Copyright (c) 2025, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.Core.UI.Forms;
using Chem4Word.Model2;
using Chem4Word.Renderer.OoXmlV4.TTF;
using IChem4Word.Contracts;
using System.Collections.Generic;

namespace Chem4Word.Renderer.OoXmlV4.Entities
{
    public class PositionerInputs
    {
        public Model Model { get; set; }
        public OoXmlV4Options Options { get; set; }
        public IChem4WordTelemetry Telemetry { get; set; }
        public Dictionary<char, TtfCharacter> TtfCharacterSet { get; set; }
        public double MeanBondLength { get; set; }
        public Progress Progress { get; set; }
    }
}