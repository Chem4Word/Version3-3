// ---------------------------------------------------------------------------
//  Copyright (c) 2025, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using BenchmarkDotNet.Running;
using System;

namespace Benchmarks
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            BenchmarkRunner.Run<FindObjectByPath>();

            Console.WriteLine("Press enter to finish");
            Console.ReadLine();
        }
    }
}
