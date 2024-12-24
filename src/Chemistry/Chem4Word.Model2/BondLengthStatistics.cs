// ---------------------------------------------------------------------------
//  Copyright (c) 2025, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;

namespace Chem4Word.Model2
{
    /// <summary>
    /// Given a list of doubles this class calculates the Mean, Mode and Median values
    /// </summary>
    public class BondLengthStatistics
    {
        /// <summary>
        /// This is the traditional Average
        /// </summary>
        public double Mean { get; }

        /// <summary>
        /// This is the most frequent value
        /// </summary>
        public double Mode { get; }

        /// <summary>
        /// This is the middle value when all the values are sorted
        /// </summary>
        public double Median { get; }

        public BondLengthStatistics(List<double> lengths)
        {
            if (lengths.Count > 0)
            {
                Mean = lengths.Average();
                Mode = CalculateMode(lengths);
                Median = CalculateMedian(lengths);
            }
        }

        private double CalculateMode(List<double> numbers)
        {
            var grouped = numbers.GroupBy(x => x).ToList();
            var maxCount = grouped.Max(g => g.Count());
            var mode = grouped.Find(g => g.Count() == maxCount)?.Key;
            return mode ?? double.NaN;
        }

        private double CalculateMedian(List<double> numbers)
        {
            var n = numbers.Count;
            if (n % 2 == 0)
            {
                var middle1 = numbers.OrderBy(x => x).ElementAt(n / 2 - 1);
                var middle2 = numbers.OrderBy(x => x).ElementAt(n / 2);
                return (middle1 + middle2) / 2.0;
            }

            return numbers.OrderBy(x => x).ElementAt(n / 2);
        }
    }
}