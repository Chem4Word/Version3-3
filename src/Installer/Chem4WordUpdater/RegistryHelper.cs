﻿// ---------------------------------------------------------------------------
//  Copyright (c) 2025, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.Globalization;

namespace Chem4WordUpdater
{
    public static class RegistryHelper
    {
        private static int _counter = 1;

        public static void WriteAction(string action)
        {
            var key = Registry.CurrentUser.CreateSubKey(Constants.Chem4WordUpdateRegistryKey);
            if (key != null)
            {
                var procId = 0;
                try
                {
                    procId = Process.GetCurrentProcess().Id;
                }
                catch
                {
                    //
                }
                var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture);
                key.SetValue($"{timestamp} [{procId}.{_counter++.ToString("000")}]", $"[{procId}] {action}");
            }
        }
    }
}