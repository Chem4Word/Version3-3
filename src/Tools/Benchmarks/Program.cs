// ---------------------------------------------------------------------------
//  Copyright (c) 2026, The .NET Foundation.
//  This software is released under the Apache Licence, Version 2.0.
//  The licence and further copyright text can be found in the file LICENCE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using BenchmarkDotNet.Running;
using Performance.Benchmarks;
using Performance.Forms;
using System;
using System.Diagnostics;

namespace Performance
{
    internal static class Program
    {
        private static void Main()
        {
            if (Debugger.IsAttached)
            {
                OoXml b = new OoXml();
                b.Setup();
                b.BoundingBoxesWithMargin();
                b.SimpleHull();
                b.ComplexHull();
                b.CleanUp();

                Visualiser plot = new Visualiser();
                plot.AtomPosition = b.AtomPosition;
                plot.CharacterShapes = b.BaseCharacters;
                plot.Hull1 = b.Hull1;
                plot.Hull2 = b.Hull2;
                plot.Hull3 = b.Hull3;
                plot.ShowDialog();

                Console.WriteLine("+--------------------------------------------------------------+");
                Console.WriteLine("| Benchmarks MUST be compiled and run in release configuration |");
                Console.WriteLine("+--------------------------------------------------------------+");
                Console.ReadLine();
            }
            else
            {
                // Change this to swap in or out different benchmarks
                BenchmarkRunner.Run<OoXml>();

                Console.WriteLine("Press enter to finish");
                Console.ReadLine();
            }
        }
    }
}
