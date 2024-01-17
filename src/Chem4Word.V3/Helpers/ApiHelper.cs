// ---------------------------------------------------------------------------
//  Copyright (c) 2024, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.Core.Helpers;
using Chem4Word.Models;
using IChem4Word.Contracts;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace Chem4Word.Helpers
{
    public class ApiHelper
    {
        private static readonly string _product = Assembly.GetExecutingAssembly().FullName.Split(',')[0];
        private static readonly string _class = MethodBase.GetCurrentMethod()?.DeclaringType?.Name;

        private readonly string _url;
        private readonly IChem4WordTelemetry _telemetry;

        public ApiHelper(string url, IChem4WordTelemetry telemetry)
        {
            _url = url;
            _telemetry = telemetry;
        }

        public List<CatalogueEntry> GetCatalogue(Dictionary<string, string> formData, int timeout)
        {
            var module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod()?.Name}()";

            var result = new List<CatalogueEntry>();

            var apiResult = AzureRestApi.GetResultAsJson($"{_url}/Catalogue", formData, timeout);

            if (apiResult.Success)
            {
                result = JsonConvert.DeserializeObject<List<CatalogueEntry>>(apiResult.Json);
            }
            else
            {
                _telemetry.Write(module, "Exception", apiResult.Message);
            }
            _telemetry.Write(module, "Timing", $"Calling API took {SafeDouble.AsString0(apiResult.Duration.TotalMilliseconds)}ms");

            return result;
        }

        public List<CatalogueEntry> GetPaidFor(Dictionary<string, string> formData, int timeout)
        {
            var module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod()?.Name}()";

            var result = new List<CatalogueEntry>();

            var apiResult = AzureRestApi.GetResultAsJson($"{_url}/PaidFor", formData, timeout);
            if (apiResult.Success)
            {
                result = JsonConvert.DeserializeObject<List<CatalogueEntry>>(apiResult.Json);
            }
            else
            {
                _telemetry.Write(module, "Exception", apiResult.Message);
            }
            _telemetry.Write(module, "Timing", $"Calling API took {SafeDouble.AsString0(apiResult.Duration.TotalMilliseconds)}ms");

            return result;
        }

        public LibraryDetails RequestLibraryDetails(Dictionary<string, string> formData, int timeout)
        {
            var module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod()?.Name}()";

            var result = new LibraryDetails();

            var apiResult = AzureRestApi.GetResultAsJson($"{_url}/Request", formData, timeout);
            if (apiResult.Success)
            {
                result = JsonConvert.DeserializeObject<LibraryDetails>(apiResult.Json);
            }
            else
            {
                _telemetry.Write(module, "Exception", apiResult.Message);
            }
            _telemetry.Write(module, "Timing", $"Calling API took {SafeDouble.AsString0(apiResult.Duration.TotalMilliseconds)}ms");

            return result;
        }

        public void DownloadLibrary(Dictionary<string, string> formData, string path, int timeout)
        {
            var module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod()?.Name}()";

            var apiResult = AzureRestApi.GetResultAsBytes($"{_url}/Library", formData, timeout);
            if (apiResult.Success)
            {
                using (var writer = new BinaryWriter(File.OpenWrite(Path.Combine(path, $"{formData["LibraryName"]}.zip"))))
                {
                    writer.Write(apiResult.Bytes);
                }
            }
            else
            {
                _telemetry.Write(module, "Exception", apiResult.Message);
            }
            _telemetry.Write(module, "Timing", $"Calling API took {SafeDouble.AsString0(apiResult.Duration.TotalMilliseconds)}ms");
        }

        public void DownloadDriver(Dictionary<string, string> formData, string path, int timeout)
        {
            var module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod()?.Name}()";

            var apiResult = AzureRestApi.GetResultAsBytes($"{_url}/Driver", formData, timeout);
            if (apiResult.Success)
            {
                using (var writer = new BinaryWriter(File.OpenWrite(Path.Combine(path, $"{formData["Driver"]}.zip"))))
                {
                    writer.Write(apiResult.Bytes);
                }
            }
            else
            {
                _telemetry.Write(module, "Exception", apiResult.Message);
            }
            _telemetry.Write(module, "Timing", $"Calling API took {SafeDouble.AsString0(apiResult.Duration.TotalMilliseconds)}ms");
        }
    }
}