// ---------------------------------------------------------------------------
//  Copyright (c) 2025, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.Core;
using Chem4Word.Core.Helpers;
using Chem4Word.Telemetry;
using IChem4Word.Contracts;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Chem4Word.WebServices
{
    public class ChemicalServices
    {
        private static string _product = Assembly.GetExecutingAssembly().FullName.Split(',')[0];
        private static string _class = MethodBase.GetCurrentMethod().DeclaringType?.Name;

        private IChem4WordTelemetry Telemetry { get; }
        private readonly string _version;
        private AzureSettings _settings = new AzureSettings(true);

        public ChemicalServices(IChem4WordTelemetry telemetry, string version)
        {
            Telemetry = telemetry;
            _version = version;
        }

        public ChemicalServicesResult GetChemicalServicesResult(string molfile)
        {
            var module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            ChemicalServicesResult data = null;

            var formData = new Dictionary<string, string>
                           {
                               { "mol", molfile },
                               { "machine", SystemHelper.GetMachineId() },
                               { "version", _version }
                           };

#if DEBUG
            formData.Add("debug", "true");
#endif

            var apiResult = AzureRestApi.GetResultAsJson($"{_settings.ChemicalServicesUri}Resolve", formData, 15);
            if (apiResult.Success)
            {
                data = JsonConvert.DeserializeObject<ChemicalServicesResult>(apiResult.Json);
                if (data != null)
                {
                    if (data.Messages.Any())
                    {
                        Telemetry.Write(module, "Timing", string.Join(Environment.NewLine, data.Messages));
                    }

                    if (data.Errors.Any())
                    {
                        Telemetry.Write(module, "Exception(Data)", string.Join(Environment.NewLine, data.Errors));
                    }
                }
            }
            else
            {
                Telemetry.Write(module, "Exception", apiResult.Message);
            }
            Telemetry.Write(module, "Timing", $"Calling API took {SafeDouble.AsString0(apiResult.Duration.TotalMilliseconds)}ms");

            return data;
        }
    }
}