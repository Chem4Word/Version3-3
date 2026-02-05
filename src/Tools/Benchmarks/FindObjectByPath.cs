// ---------------------------------------------------------------------------
//  Copyright (c) 2025, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using BenchmarkDotNet.Attributes;
using Chem4Word.Model2;
using System;
using System.Collections.Generic;

namespace Benchmarks
{
    [MemoryDiagnoser] // Adds memory usage diagnostics to the benchmark
    public class FindObjectByPath
    {
        private Model _modelC60 = new Model();
        private List<string> _pathsC60 = new List<string>();

        private Model _modelNested = new Model();
        private List<string> _pathsNested = new List<string>();

        private Model _modelMaitotoxin = new Model();
        private List<string> _pathsMaitotoxin = new List<string>();

        [GlobalSetup] // Runs once before all benchmarks
        public void Setup()
        {
            _modelC60 = Common.LoadCml("C60.xml");
            _pathsC60 = Common.GetPaths(_modelC60);
            Console.WriteLine($"C60 model has {_pathsC60.Count:N0} paths");

            _modelNested = Common.LoadCml("Nested.xml");
            _pathsNested = Common.GetPaths(_modelNested);
            Console.WriteLine($"Nested model has {_pathsNested.Count:N0} paths");

            _modelNested = Common.LoadCml("Maitotoxin.xml");
            _pathsNested = Common.GetPaths(_modelNested);
            Console.WriteLine($"Maitotoxin model has {_pathsNested.Count:N0} paths");
        }

        [Benchmark]
        public void GetByPathInC60()
        {
            foreach (string path in _pathsC60)
            {
                _modelC60.GetByPath(path);
            }
        }

        [Benchmark]
        public void GetByPathInNested()
        {
            foreach (string path in _pathsNested)
            {
                _modelNested.GetByPath(path);
            }
        }

        [Benchmark]
        public void GetByPathInMaitotoxin()
        {
            foreach (string path in _pathsC60)
            {
                _modelC60.GetByPath(path);
            }
        }
    }
}
