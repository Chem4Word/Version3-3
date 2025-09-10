// ---------------------------------------------------------------------------
//  Copyright (c) 2025, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.Renderer.OoXmlV4.Entities.Diagnostic;
using System.Collections.Generic;
using System.Windows;

namespace Chem4Word.Renderer.OoXmlV4.Entities
{
    public class RendererOutputs
    {
        // Atom / Annotation / Molecular Weight characters
        public List<AtomLabelCharacter> AtomLabelCharacters { get; } = new List<AtomLabelCharacter>();

        public Dictionary<string, List<Point>> ConvexHulls { get; } = new Dictionary<string, List<Point>>();

        public Rect AllCharacterExtents { get; set; } = Rect.Empty;

        // Bond Lines
        public List<BondLine> BondLines { get; } = new List<BondLine>();

        public List<Point> CrossingPoints { get; } = new List<Point>();

        // Rings
        public List<Point> RingCenters { get; } = new List<Point>();

        public List<InnerCircle> InnerCircles { get; } = new List<InnerCircle>();

        // Brackets
        public List<Rect> GroupBrackets { get; } = new List<Rect>();

        public List<Rect> MoleculeBrackets { get; } = new List<Rect>();

        // Drawing Extents
        public List<MoleculeExtents> AllMoleculeExtents { get; } = new List<MoleculeExtents>();

        public Diagnostics Diagnostics { get; } = new Diagnostics();
    }
}