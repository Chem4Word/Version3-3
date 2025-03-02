﻿// ---------------------------------------------------------------------------
//  Copyright (c) 2025, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.Renderer.OoXmlV4.Enums;
using Chem4Word.Renderer.OoXmlV4.OOXML;
using System.Windows;

namespace Chem4Word.Renderer.OoXmlV4.Entities.Diagnostic
{
    public class DiagnosticLine
    {
        public Point Start { get; }

        public Point End { get; }

        public BondLineStyle Style { get; }

        public string Colour { get; }

        public DiagnosticLine(Point startPoint, Point endPoint, BondLineStyle style, string colour = OoXmlHelper.Black)
        {
            Start = startPoint;
            End = endPoint;
            Style = style;
            Colour = colour;
        }
    }
}