// ---------------------------------------------------------------------------
//  Copyright (c) 2026, The .NET Foundation.
//  This software is released under the Apache Licence, Version 2.0.
//  The licence and further copyright text can be found in the file LICENCE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using BenchmarkDotNet.Attributes;
using Chem4Word.Core.Helpers;
using Chem4Word.Renderer.OoXmlV4.Entities;
using Chem4Word.Renderer.OoXmlV4.Helpers;
using Chem4Word.Renderer.OoXmlV4.OoXml;
using Chem4Word.Renderer.OoXmlV4.TTF;
using System;
using System.Collections.Generic;
using System.Windows;

namespace Performance.Benchmarks
{
    [MemoryDiagnoser] // Adds memory usage diagnostics to the benchmark
    public class OoXml
    {
        private Dictionary<char, TtfCharacter> _characterSet;
        private TtfCharacter _hydrogenCharacter;

        private readonly List<AtomLabelCharacter> _atomSymbol = new List<AtomLabelCharacter>();

        private double _bondLength;
        private double _margin;

        public List<Point> Hull1 = new List<Point>();
        public List<Point> Hull2 = new List<Point>();
        public List<Point> Hull3 = new List<Point>();

        public List<List<Point>> BaseCharacters = new List<List<Point>>();

        public Point AtomPosition = new Point(50, 250);

        [GlobalSetup] // Runs once before all benchmarks
        public void Setup()
        {
            // Load font
            _characterSet = FontHelper.LoadFont("Arial.json");
            _hydrogenCharacter = _characterSet['H'];

            // Setup molecule properties
            _bondLength = 400.0;
            double offset = _bondLength * OoXmlConstants.MultipleBondOffsetPercentage;
            _margin = offset / 4.0;

            // Setup List<AtomLabelCharacter>
            string atomSymbol = "Qy3-Hg7*";
            Point cursor = AtomPosition;

            foreach (char c in atomSymbol)
            {
                TtfCharacter ttfCharacter = _characterSet[c];

                AtomLabelCharacter alc = new AtomLabelCharacter(cursor, ttfCharacter, OoXmlColours.Black, "m1/a1", "/m1");
                if (char.IsNumber(c))
                {
                    alc.IsSmaller = true;
                }

                Point position = new Point(cursor.X + OoXmlHelper.ScaleCsTtfToCml(ttfCharacter.OriginX, _bondLength),
                                           cursor.Y + OoXmlHelper.ScaleCsTtfToCml(ttfCharacter.OriginY, _bondLength));

                if (alc.IsSmaller)
                {
                    position.Offset(0, OoXmlHelper.ScaleCsTtfSubScriptToCml(_hydrogenCharacter.Height, _bondLength));
                }

                alc.Position = position;

                // Move to next Character position
                cursor.Offset(alc.IsSmaller
                                  ? OoXmlHelper.ScaleCsTtfSubScriptToCml(ttfCharacter.IncrementX, _bondLength)
                                  : OoXmlHelper.ScaleCsTtfToCml(ttfCharacter.IncrementX, _bondLength), 0);

                _atomSymbol.Add(alc);
            }

            foreach (AtomLabelCharacter character in _atomSymbol)
            {
                List<Point> pp = OoXmlHelper.SimpleHull(character, _bondLength);
                BaseCharacters.Add(pp);
            }
        }

        [Benchmark]
        public void BoundingBoxesWithMargin()
        {
            List<Point> points = new List<Point>();
            foreach (AtomLabelCharacter character in _atomSymbol)
            {
                List<Point> pp = OoXmlHelper.BoundingBox(character, _bondLength, _margin);
                points.AddRange(pp);
            }

            Hull1 = GeometryTool.MakeConvexHull(points);
        }

        [Benchmark]
        public void SimpleHull()
        {
            List<Point> points = OoXmlHelper.SimpleHull(_atomSymbol, _bondLength);
            Hull2 = GeometryTool.InflateConvexHull(points, _margin * 1.5);
        }

        [Benchmark]
        public void ComplexHull()
        {
            List<Point> points = OoXmlHelper.ComplexHull(_atomSymbol, _bondLength);
            Hull3 = GeometryTool.InflateConvexHull(points, _margin * 2);
        }

        [GlobalCleanup]
        public void CleanUp()
        {
            Console.WriteLine($"Hull 1 has {Hull1.Count} points");
            Console.WriteLine($"Hull 2 has {Hull2.Count} points");
            Console.WriteLine($"Hull 3 has {Hull3.Count} points");
        }
    }
}
