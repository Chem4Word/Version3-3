﻿// ---------------------------------------------------------------------------
//  Copyright (c) 2024, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;

namespace Chem4Word.Helpers
{
    public class ConfigWatcher : IDisposable
    {
        // Files to watch for
        private const string _filter = "*.json";

        // Config settings to watch
        private Config[] _watchedConfigs = {
            new Config { Name = "ShowHydrogens", Type = "bool" },
            new Config { Name = "ShowCarbons", Type = "bool" },
            new Config { Name = "ShowMoleculeGrouping", Type = "bool" },
            new Config { Name = "ColouredAtoms", Type = "bool" },
            new Config { Name = "BondLength", Type = "int" }
        };

        private FileSystemWatcher _watcher;
        private string _watchedPath;
        private bool _handleEvents = true;

        public ConfigWatcher(string watchedPath)
        {
            _watchedPath = watchedPath;

            _watcher = new FileSystemWatcher();

            _watcher.Path = _watchedPath;
            _watcher.Filter = _filter;
            _watcher.NotifyFilter = NotifyFilters.LastWrite;

            _watcher.Changed += OnChanged;
            _watcher.EnableRaisingEvents = true;
        }

        public void Dispose()
        {
            _watcher.EnableRaisingEvents = false;
            _watcher.Changed -= OnChanged;

            _watcher.Dispose();
        }

        private void OnChanged(object source, FileSystemEventArgs e)
        {
            if (_handleEvents)
            {
                try
                {
                    _handleEvents = false;
                    _watcher.EnableRaisingEvents = false;

                    var sourceConfigs = new Dictionary<string, Config>();

                    var thisFile = e.FullPath;
                    Debug.WriteLine($"Trigger file is {thisFile}");
                    Thread.Sleep(250);

                    using (var sr = File.OpenText(e.FullPath))
                    {
                        using (var reader = new JsonTextReader(sr))
                        {
                            var jObject = (JObject)JToken.ReadFrom(reader);
                            foreach (var config in _watchedConfigs)
                            {
                                var t = jObject[config.Name];
                                if (t != null)
                                {
                                    sourceConfigs.Add(config.Name, new Config { Type = config.Type, Value = t.Value<string>() });
                                }
                            }
                        }
                    }

                    if (sourceConfigs.Any())
                    {
                        var files = Directory.GetFiles(_watchedPath, _filter);
                        foreach (var file in files)
                        {
                            if (!file.Equals(thisFile))
                            {
                                JObject jObject = null;

                                var targetTokens = new List<JToken>();
                                using (var sr = File.OpenText(file))
                                {
                                    using (var reader = new JsonTextReader(sr))
                                    {
                                        jObject = (JObject)JToken.ReadFrom(reader);
                                        foreach (var config in _watchedConfigs)
                                        {
                                            var t = jObject[config.Name];
                                            if (t != null)
                                            {
                                                targetTokens.Add(t);
                                            }
                                        }
                                    }
                                }

                                if (targetTokens.Any())
                                {
                                    var write = false;

                                    foreach (var target in targetTokens)
                                    {
                                        foreach (var kvp in sourceConfigs)
                                        {
                                            if (target.Path.Equals(kvp.Key))
                                            {
                                                if (!target.Value<string>().Equals(kvp.Value.Value))
                                                {
                                                    Debug.WriteLine($"Changing setting {kvp.Key} to {kvp.Value.Value}");
                                                    switch (kvp.Value.Type)
                                                    {
                                                        case "bool":
                                                            jObject[kvp.Key] = bool.Parse(kvp.Value.Value);
                                                            write = true;
                                                            break;

                                                        case "int":
                                                            jObject[kvp.Key] = int.Parse(kvp.Value.Value);
                                                            write = true;
                                                            break;
                                                    }
                                                }
                                            }
                                        }
                                    }

                                    if (write)
                                    {
                                        Debug.WriteLine($"Writing file {file}");
                                        Globals.Chem4WordV3.OptionsReloadRequired = true;
                                        var json = JsonConvert.SerializeObject(jObject, Formatting.Indented);
                                        File.WriteAllText(file, json);
                                    }
                                }
                            }
                        }
                    }

                    _watcher.EnableRaisingEvents = true;
                    _handleEvents = true;
                }
                catch
                {
                    // Do Nothing
                }
            }
        }

        private class Config
        {
            public string Type { get; set; }
            public string Name { get; set; }
            public string Value { get; set; }
        }
    }
}