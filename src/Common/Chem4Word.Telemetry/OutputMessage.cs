﻿// ---------------------------------------------------------------------------
//  Copyright (c) 2025, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System;
using System.Threading;

namespace Chem4Word.Telemetry
{
    public class OutputMessage
    {
        private static readonly object MessageLock = Guid.NewGuid();

        private static long _order;

        public OutputMessage(int processId)
        {
            lock (MessageLock)
            {
                // Fixed PartitionKey
                PartitionKey = "Chem4Word";

                // Generate Unique RowKey

                // First part of RowKey is to enable "default" sort of time descending
                // Second part of RowKey is to give a sequence per process
                // Third part of RowKey is to guarantee uniqueness
                var parts = new string[3];

                var messageTicks = DateTime.MaxValue.Ticks - DateTime.UtcNow.Ticks;
                parts[0] = $"{messageTicks:D19}";
                _order++;
                parts[1] = processId + "-" + _order.ToString("000000");
                parts[2] = Guid.NewGuid().ToString("N");

                RowKey = string.Join(".", parts);

                Monitor.PulseAll(MessageLock);
            }
        }

        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public string AssemblyVersionNumber { get; set; }
        public string MachineId { get; set; }
        public string Operation { get; set; }
        public string Level { get; set; }
        public string Message { get; set; }
    }
}