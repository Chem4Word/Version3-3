// ---------------------------------------------------------------------------
//  Copyright (c) 2025, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;

namespace Chem4Word.Core
{
    [JsonObject(MemberSerialization.OptIn)]
    public class AzureSettings
    {
        /// <summary>
        /// Used in ChemicalServices.cs; ServicePointManager.FindServicePoint(...)
        /// </summary>
        [JsonProperty]
        public string ChemicalServicesUri { get; private set; }

        /// <summary>
        /// Used in AzureServiceBusWriter.cs to construct ServiceBusClient
        /// </summary>
        [JsonProperty]
        public string ServiceBusEndPoint { get; private set; }

        /// <summary>
        /// Used in AzureServiceBusWriter.cs to construct ServiceBusClient
        /// </summary>
        [JsonProperty]
        public string ServiceBusToken { get; private set; }

        /// <summary>
        /// Used in AzureServiceBusWriter.cs; CreateSender
        /// </summary>
        [JsonProperty]
        public string ServiceBusQueue { get; private set; }

        /// <summary>
        /// Where the Libraries API is deployed to
        /// </summary>
        [JsonProperty]
        public string LibrariesUri { get; private set; }

        /// <summary>
        /// Stores when last checked
        /// </summary>
        public string LastChecked { get; private set; }

        private bool _dirty;

        public AzureSettings()
        {
            // Must have empty constructor to allow this class to deserialize itself
        }

        public AzureSettings(bool load)
        {
            if (load)
            {
                Load();
            }
        }

        private void Load()
        {
            var today = DateTime.Today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

            // 1. Attempt to get from registry
            var refreshFromWebRequired = GetFromRegistry();

            // 2. If missing / not found / too old; attempt to get from Chem4Word web site(s)
            if (refreshFromWebRequired || string.IsNullOrEmpty(LastChecked) || !LastChecked.Equals(today))
            {
                GetFromWebsite(today);
            }

            // 2.1 Save to registry if found
            if (_dirty)
            {
                SaveToRegistry(today);
            }
        }

        private bool GetFromRegistry()
        {
            var refreshRequired = false;

            try
            {
                var key = Registry.CurrentUser.OpenSubKey(CoreConstants.Chem4WordAzureSettingsRegistryKey, true);
                if (key != null)
                {
                    var names = key.GetValueNames();

                    if (names.Contains(nameof(ChemicalServicesUri)))
                    {
                        var chemicalServicesUri = key.GetValue(nameof(ChemicalServicesUri)).ToString();
                        ChemicalServicesUri = chemicalServicesUri;
                    }
                    else
                    {
                        refreshRequired = true;
                    }

                    if (names.Contains(nameof(ServiceBusEndPoint)))
                    {
                        var serviceBusEndPoint = key.GetValue(nameof(ServiceBusEndPoint)).ToString();
                        ServiceBusEndPoint = serviceBusEndPoint;
                    }
                    else
                    {
                        refreshRequired = true;
                    }

                    if (names.Contains(nameof(ServiceBusToken)))
                    {
                        var serviceBusToken = key.GetValue(nameof(ServiceBusToken)).ToString();
                        ServiceBusToken = serviceBusToken;
                    }
                    else
                    {
                        refreshRequired = true;
                    }

                    if (names.Contains(nameof(ServiceBusQueue)))
                    {
                        var serviceBusQueue = key.GetValue(nameof(ServiceBusQueue)).ToString();
                        ServiceBusQueue = serviceBusQueue;
                    }
                    else
                    {
                        refreshRequired = true;
                    }

                    if (names.Contains(nameof(LibrariesUri)))
                    {
                        var librariesUri = key.GetValue(nameof(LibrariesUri)).ToString();
                        LibrariesUri = librariesUri;
                    }
                    else
                    {
                        refreshRequired = true;
                    }

                    if (names.Contains(nameof(LastChecked)))
                    {
                        var lastChecked = key.GetValue(nameof(LastChecked)).ToString();
                        LastChecked = lastChecked;
                    }
                }
                else
                {
                    refreshRequired = true;
                }
            }
            catch
            {
                refreshRequired = true;
            }

            return refreshRequired;
        }

        private void GetFromWebsite(string today)
        {
            var securityProtocol = ServicePointManager.SecurityProtocol;
            try
            {
                var file = $"{CoreConstants.Chem4WordVersionFiles}/AzureSettings.json";

                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                var found = false;
                var temp = string.Empty;

                foreach (var domain in CoreConstants.OurDomains)
                {
                    using (var client = new HttpClient())
                    {
                        try
                        {
                            client.DefaultRequestHeaders.Add("user-agent", "Chem4Word GetAzureSettings");
                            client.BaseAddress = new Uri(domain);
                            var response = client.GetAsync(file).Result;
                            response.EnsureSuccessStatusCode();

                            var result = response.Content.ReadAsStringAsync().Result;
                            if (result.Contains("Chem4WordAzureSettings"))
                            {
                                found = true;
                                temp = result;
                            }
                        }
                        catch
                        {
                            //Debugger.Break()
                        }
                    }

                    if (found)
                    {
                        break;
                    }
                }

                if (!string.IsNullOrEmpty(temp))
                {
                    var settings = JsonConvert.DeserializeObject<AzureSettings>(temp);

                    _dirty = LastChecked != today;

                    ChemicalServicesUri = settings.ChemicalServicesUri;
                    ServiceBusEndPoint = settings.ServiceBusEndPoint;
                    ServiceBusToken = settings.ServiceBusToken;
                    ServiceBusQueue = settings.ServiceBusQueue;
                    LibrariesUri = settings.LibrariesUri;
                    LastChecked = today;
                }
            }
            catch
            {
                // Do Nothing
            }
            finally
            {
                ServicePointManager.SecurityProtocol = securityProtocol;
            }
        }

        private void SaveToRegistry(string today)
        {
            try
            {
                var key = Registry.CurrentUser.CreateSubKey(CoreConstants.Chem4WordAzureSettingsRegistryKey);
                if (key != null)
                {
                    key.SetValue(nameof(ChemicalServicesUri), ChemicalServicesUri);
                    key.SetValue(nameof(ServiceBusEndPoint), ServiceBusEndPoint);
                    key.SetValue(nameof(ServiceBusToken), ServiceBusToken);
                    key.SetValue(nameof(ServiceBusQueue), ServiceBusQueue);
                    key.SetValue(nameof(LibrariesUri), LibrariesUri);
                    key.SetValue(nameof(LastChecked), today);
                }
            }
            catch
            {
                // Do Nothing
            }
        }
    }
}