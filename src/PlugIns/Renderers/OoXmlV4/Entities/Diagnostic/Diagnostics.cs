// ---------------------------------------------------------------------------
//  Copyright (c) 2026, The .NET Foundation.
//  This software is released under the Apache Licence, Version 2.0.
//  The licence and further copyright text can be found in the file LICENCE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System.Collections.Generic;

namespace Chem4Word.Renderer.OoXmlV4.Entities.Diagnostic
{
    public class Diagnostics
    {
        public List<DiagnosticLine> Lines { get; } = new List<DiagnosticLine>();
        public List<DiagnosticPolygon> Polygons { get; } = new List<DiagnosticPolygon>();
        public List<DiagnosticSpot> Points { get; } = new List<DiagnosticSpot>();
        public List<DiagnosticRectangle> Rectangles { get; } = new List<DiagnosticRectangle>();
    }
}