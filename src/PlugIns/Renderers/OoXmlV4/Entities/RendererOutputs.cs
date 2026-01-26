// ---------------------------------------------------------------------------
//  Copyright (c) 2026, The .NET Foundation.
//  This software is released under the Apache Licence, Version 2.0.
//  The licence and further copyright text can be found in the file LICENCE.md
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

        // Electrons
        public Dictionary<string, List<OoXmlElectron>> AtomsWithElectrons { get; set; } = new Dictionary<string, List<OoXmlElectron>>();

        // Electrons Pushers
        public List<OoXmlElectronPusher> Pushers { get; } = new List<OoXmlElectronPusher>();

        // Bond Lines
        public List<BondLine> BondLines { get; } = new List<BondLine>();

        // Brackets
        public List<Rect> GroupBrackets { get; } = new List<Rect>();

        public List<Rect> MoleculeBrackets { get; } = new List<Rect>();

        // Rings
        public List<Point> RingCenters { get; } = new List<Point>();

        // Experimental
        public List<Point> CrossingPoints { get; } = new List<Point>();

        public List<InnerCircle> InnerCircles { get; } = new List<InnerCircle>();

        // Drawing Extents
        public Rect AllCharacterExtents { get; set; } = Rect.Empty;

        public List<MoleculeExtents> AllMoleculeExtents { get; } = new List<MoleculeExtents>();

        // Convex Hull of each atom (Label + Hydrogens + Electrons)
        public Dictionary<string, List<Point>> ConvexHulls { get; } = new Dictionary<string, List<Point>>();

        public Diagnostics Diagnostics { get; } = new Diagnostics();
    }
}
