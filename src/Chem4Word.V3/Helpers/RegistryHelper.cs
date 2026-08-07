// ---------------------------------------------------------------------------
//  Copyright (c) 2026, The .NET Foundation.
//  This software is released under the Apache Licence, Version 2.0.
//  The licence and further copyright text can be found in the file LICENCE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.Core;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace Chem4Word.Helpers
{
    public static class RegistryHelper
    {
        private static readonly string _product = Assembly.GetExecutingAssembly().FullName.Split(',')[0];
        private static readonly string _class = MethodBase.GetCurrentMethod()?.DeclaringType?.Name;

        private static int _counter = 1;

        public static void StoreMessage(string module, string message)
        {
            RegistryKey key = Registry.CurrentUser.CreateSubKey(CoreConstants.Chem4WordMessagesRegistryKey);
            if (key != null)
            {
                int procId = 0;
                try
                {
                    procId = Process.GetCurrentProcess().Id;
                }
                catch
                {
                    //
                }

                string timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture);
                key.SetValue($"{timestamp} [{procId}.{_counter++:000}]", $"[{procId}] {module} {message}");
                key.Dispose();
            }
        }

        public static void StoreException(string module, Exception exception)
        {
            RegistryKey key = Registry.CurrentUser.CreateSubKey(CoreConstants.Chem4WordExceptionsRegistryKey);
            if (key != null)
            {
                int procId = 0;
                try
                {
                    procId = Process.GetCurrentProcess().Id;
                }
                catch
                {
                    //
                }

                string timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture);
                key.SetValue($"{timestamp} [{procId}.{_counter++:000}]", $"[{procId}] {module} {exception}");
                key.Dispose();
            }
        }

        public static void SendSetupActions()
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod()?.Name}()";

            RegistryKey registryKey = Registry.CurrentUser.OpenSubKey(CoreConstants.Chem4WordSetupRegistryKey, true);
            if (registryKey != null)
            {
                SendValues(module, "Setup", registryKey);
            }
        }

        public static void SendUpdateActions()
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod()?.Name}()";

            RegistryKey registryKey = Registry.CurrentUser.OpenSubKey(CoreConstants.Chem4WordUpdateRegistryKey, true);
            if (registryKey != null)
            {
                SendValues(module, "Update", registryKey);
            }
        }

        public static void SendMsiActions()
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod()?.Name}()";

            RegistryKey registryKey = Registry.CurrentUser.OpenSubKey(CoreConstants.Chem4WordMsiActionsRegistryKey, true);
            if (registryKey != null)
            {
                SendValues(module, "MsiActions", registryKey);
            }
        }

        public static void SendMessages()
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod()?.Name}()";

            RegistryKey registryKey = Registry.CurrentUser.OpenSubKey(CoreConstants.Chem4WordMessagesRegistryKey, true);
            if (registryKey != null)
            {
                SendValues(module, "Information", registryKey);
            }
        }

        public static void SendExceptions()
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod()?.Name}()";

            RegistryKey registryKey = Registry.CurrentUser.OpenSubKey(CoreConstants.Chem4WordExceptionsRegistryKey, true);
            if (registryKey != null)
            {
                SendValues(module, "Exception", registryKey);
            }
        }

        private static void SendValues(string module, string level, RegistryKey registryKey)
        {
            string[] keys = registryKey.GetValueNames();
            List<RegistryMessage> values = new List<RegistryMessage>();

            foreach (string key in keys)
            {
                string value = registryKey.GetValue(key).ToString();

                string timestamp = key;
                int bracket = timestamp.IndexOf("[", StringComparison.InvariantCulture);
                if (bracket > 0)
                {
                    timestamp = timestamp.Substring(0, bracket).Trim();
                }

                RegistryMessage temp = new RegistryMessage
                                       {
                                           Date = timestamp
                                       };

                bracket = value.IndexOf("]", StringComparison.InvariantCulture);
                if (bracket > 0)
                {
                    temp.ProcessId = value.Substring(1, bracket - 1).Trim();
                    temp.Message = value.Substring(bracket + 1).Trim();
                }
                else
                {
                    temp.Message = value;
                }

                values.Add(temp);
                registryKey.DeleteValue(key);
            }

            // Group the messages by day
            List<IGrouping<string, RegistryMessage>> days = values
                                                            .GroupBy(g => g.Date.Substring(0, 10))
                                                            .ToList();

            int depth = 0;
            foreach (IGrouping<string, RegistryMessage> day in days)
            {
                SendData(module, level, day.ToList(), ref depth);
            }
        }

        private static void SendData(string module, string level, List<RegistryMessage> messages, ref int depth)
        {
            if (messages.Any())
            {
                depth++;

                // Group messages by process id
                List<IGrouping<string, RegistryMessage>> processes = messages
                                                                     .GroupBy(g => g.ProcessId)
                                                                     .ToList();

                foreach (IGrouping<string, RegistryMessage> process in processes)
                {
                    string message = string.Join(Environment.NewLine, process.ToList());
                    // Maximum length of a message in Azure Service bus is 32K, use 125 for testing
                    if (message.Length < 32_000)
                    {
                        Globals.Chem4WordV3.Telemetry.Write(module, level, message);
                    }
                    else
                    {
                        if (depth < 4)
                        {
                            int extra = depth * 3;

                            // Re-Group by Hour, Minute, Second based on depth
                            List<IGrouping<string, RegistryMessage>> groups = messages
                                                                              .GroupBy(g => g.Date.Substring(0, 10 + extra))
                                                                              .ToList();
                            foreach (IGrouping<string, RegistryMessage> group in groups)
                            {
                                SendData(module, level, group.ToList(), ref depth);
                            }
                        }
                    }
                }

                depth--;
            }
        }
    }
}
