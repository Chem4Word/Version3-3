// ---------------------------------------------------------------------------
//  Copyright (c) 2026, The .NET Foundation.
//  This software is released under the Apache Licence, Version 2.0.
//  The licence and further copyright text can be found in the file LICENCE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.Core.Helpers;
using Chem4Word.Renderer.OoXmlV4.TTF;
using System.Windows;

namespace Chem4Word.Renderer.OoXmlV4.Entities
{
    public class AtomLabelCharacter
    {
        public string MoleculePath { get; set; }
        public string AtomPath { get; set; }
        public Point Position { get; set; }
        public TtfCharacter Character { get; set; }
        public string Colour { get; set; }
        public bool IsSmaller { get; set; }
        public bool IsSubScript { get; set; }
        public bool IsSuperScript { get; set; }

        public AtomLabelCharacter(Point position, TtfCharacter character, string colour, string atomPath, string moleculePath)
        {
            Position = position;
            Character = character;
            Colour = colour;
            AtomPath = atomPath;
            MoleculePath = moleculePath;
        }

        public override string ToString()
        {
            return $"{Character.Character} of {AtomPath} @ {PointHelper.AsString(Position)}";
        }
    }
}
