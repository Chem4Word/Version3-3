// ---------------------------------------------------------------------------
//  Copyright (c) 2023, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;

namespace Chem4Word.Helpers
{
    public class ApiHelper
    {
        private readonly string _url;

        public ApiHelper(string url)
        {
            _url = url;
        }

        public ApiResult GetCatalogue(Dictionary<string, string> formData, int timeout)
        {
            var result = new ApiResult
            {
                Catalogue = new List<CatalogueEntry>()
            };

            var securityProtocol = ServicePointManager.SecurityProtocol;

            try
            {
                ServicePointManager.Expect100Continue = true;

                ServicePointManager.SecurityProtocol = securityProtocol | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;

                var content = new FormUrlEncodedContent(formData);
                using (var httpClient = new HttpClient())
                {
                    httpClient.DefaultRequestHeaders.Add("user-agent", "Chem4Word");
                    httpClient.Timeout = TimeSpan.FromSeconds(timeout);

                    var response = httpClient.PostAsync($"{_url}/Catalogue", content).Result;
                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        if (response.Content != null)
                        {
                            var responseContent = response.Content;
                            var jsonContent = responseContent.ReadAsStringAsync().Result;

                            result.Catalogue = JsonConvert.DeserializeObject<List<CatalogueEntry>>(jsonContent);
                            result.Success = true;
                        }
                        else
                        {
                            result.HttpStatusCode = 204;
                            result.Message = "Content is missing";
                        }
                    }
                    else
                    {
                        var responseBody = string.Empty;
                        if (response.Content != null)
                        {
                            var responseContent = response.Content;
                            responseBody = responseContent.ReadAsStringAsync().Result;
                        }
                        result.HttpStatusCode = (int)response.StatusCode;
                        result.Message = (response.ReasonPhrase + " " + responseBody).Trim();
                    }
                }
            }
            catch (Exception exception)
            {
                result.Message = NestedExceptionMessages(exception);
                result.HasException = true;
                Debug.WriteLine(exception.Message);
                Debug.WriteLine(exception.StackTrace);
            }
            finally
            {
                ServicePointManager.SecurityProtocol = securityProtocol;
            }

            return result;
        }

        public ApiResult GetPaidFor(Dictionary<string, string> formData, int timeout)
        {
            var result = new ApiResult
                         {
                             Catalogue = new List<CatalogueEntry>(),
                         };

            var securityProtocol = ServicePointManager.SecurityProtocol;

            try
            {
                ServicePointManager.Expect100Continue = true;

                ServicePointManager.SecurityProtocol = securityProtocol | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;

                var content = new FormUrlEncodedContent(formData);
                using (var httpClient = new HttpClient())
                {
                    httpClient.DefaultRequestHeaders.Add("user-agent", "Chem4Word");
                    httpClient.Timeout = TimeSpan.FromSeconds(timeout);

                    var response = httpClient.PostAsync($"{_url}/PaidFor", content).Result;
                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        if (response.Content != null)
                        {
                            var responseContent = response.Content;
                            var jsonContent = responseContent.ReadAsStringAsync().Result;

                            result.Catalogue = JsonConvert.DeserializeObject<List<CatalogueEntry>>(jsonContent);
                            result.Success = true;
                        }
                        else
                        {
                            result.HttpStatusCode = 204;
                            result.Message = "Content is missing";
                        }
                    }
                    else
                    {
                        var responseBody = string.Empty;
                        if (response.Content != null)
                        {
                            var responseContent = response.Content;
                            responseBody = responseContent.ReadAsStringAsync().Result;
                        }
                        result.HttpStatusCode = (int)response.StatusCode;
                        result.Message = (response.ReasonPhrase + " " + responseBody).Trim();
                    }
                }
            }
            catch (Exception exception)
            {
                result.Message = NestedExceptionMessages(exception);
                result.HasException = true;
                Debug.WriteLine(exception.Message);
                Debug.WriteLine(exception.StackTrace);
            }
            finally
            {
                ServicePointManager.SecurityProtocol = securityProtocol;
            }

            return result;
        }

        public ApiResult RequestLibraryDetails(Dictionary<string, string> formData, int timeout)
        {
            var result = new ApiResult
            {
                Details = new LibraryDetails()
            };

            var securityProtocol = ServicePointManager.SecurityProtocol;

            try
            {
                ServicePointManager.Expect100Continue = true;

                ServicePointManager.SecurityProtocol = securityProtocol | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;

                var content = new FormUrlEncodedContent(formData);
                using (var httpClient = new HttpClient())
                {
                    httpClient.DefaultRequestHeaders.Add("user-agent", "Chem4Word");
                    httpClient.Timeout = TimeSpan.FromSeconds(timeout);

                    var response = httpClient.PostAsync($"{_url}/Request", content).Result;
                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        if (response.Content != null)
                        {
                            var responseContent = response.Content;
                            var jsonContent = responseContent.ReadAsStringAsync().Result;

                            result.Details = JsonConvert.DeserializeObject<LibraryDetails>(jsonContent);
                            result.Success = true;
                        }
                        else
                        {
                            result.HttpStatusCode = 204;
                            result.Message = "Content is missing";
                        }
                    }
                    else
                    {
                        var responseBody = string.Empty;
                        if (response.Content != null)
                        {
                            var responseContent = response.Content;
                            responseBody = responseContent.ReadAsStringAsync().Result;
                        }
                        result.HttpStatusCode = (int)response.StatusCode;
                        result.Message = (response.ReasonPhrase + " " + responseBody).Trim();
                    }
                }
            }
            catch (Exception exception)
            {
                result.Message = NestedExceptionMessages(exception);
                result.HasException = true;
                Debug.WriteLine(exception.Message);
                Debug.WriteLine(exception.StackTrace);
                Debugger.Break();
            }
            finally
            {
                ServicePointManager.SecurityProtocol = securityProtocol;
            }

            return result;
        }

        public ApiResult DownloadLibrary(Dictionary<string, string> formData, string path, int timeout)
        {
            var result = new ApiResult();

            var stopwatch = new Stopwatch();
            stopwatch.Start();

            var securityProtocol = ServicePointManager.SecurityProtocol;

            try
            {
                ServicePointManager.Expect100Continue = true;

                ServicePointManager.SecurityProtocol = securityProtocol | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;

                var content = new FormUrlEncodedContent(formData);
                using (var httpClient = new HttpClient())
                {
                    httpClient.DefaultRequestHeaders.Add("user-agent", "Chem4Word");
                    httpClient.Timeout = TimeSpan.FromSeconds(timeout);

                    var response = httpClient.PostAsync($"{_url}/Library", content).Result;
                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        if (response.Content != null)
                        {
                            var bytes = response.Content.ReadAsByteArrayAsync().Result;

                            using (var writer = new BinaryWriter(File.OpenWrite(Path.Combine(path, $"{formData["library"]}.zip"))))
                            {
                                writer.Write(bytes);
                                result.Success = true;
                            }
                        }
                        else
                        {
                            result.HttpStatusCode = 204;
                            result.Message = "Content is missing";
                        }
                    }
                    else
                    {
                        var responseBody = string.Empty;
                        if (response.Content != null)
                        {
                            var responseContent = response.Content;
                            responseBody = responseContent.ReadAsStringAsync().Result;
                        }
                        result.HttpStatusCode = (int)response.StatusCode;
                        result.Message = (response.ReasonPhrase + " " + responseBody).Trim();
                    }
                }
            }
            catch (Exception exception)
            {
                result.Message = NestedExceptionMessages(exception);
                result.HasException = true;
                Debug.WriteLine(exception.Message);
                Debug.WriteLine(exception.StackTrace);
                Debugger.Break();
            }
            finally
            {
                ServicePointManager.SecurityProtocol = securityProtocol;
            }

            return result;
        }

        public ApiResult DownloadDriver(Dictionary<string, string> formData, string path, int timeout)
        {
            var result = new ApiResult();

            var securityProtocol = ServicePointManager.SecurityProtocol;

            try
            {
                ServicePointManager.Expect100Continue = true;

                ServicePointManager.SecurityProtocol = securityProtocol | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;

                var content = new FormUrlEncodedContent(formData);
                using (var httpClient = new HttpClient())
                {
                    httpClient.DefaultRequestHeaders.Add("user-agent", "Chem4Word");
                    httpClient.Timeout = TimeSpan.FromSeconds(timeout);

                    var response = httpClient.PostAsync($"{_url}/Driver", content).Result;
                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        if (response.Content != null)
                        {
                            var bytes = response.Content.ReadAsByteArrayAsync().Result;

                            using (var writer = new BinaryWriter(File.OpenWrite(Path.Combine(path, $"{formData["driver"]}.zip"))))
                            {
                                writer.Write(bytes);
                                result.Success = true;
                            }
                        }
                        else
                        {
                            result.HttpStatusCode = 204;
                            result.Message = "Content is missing";
                        }
                    }
                    else
                    {
                        var responseBody = string.Empty;
                        if (response.Content != null)
                        {
                            var responseContent = response.Content;
                            responseBody = responseContent.ReadAsStringAsync().Result;
                        }
                        result.HttpStatusCode = (int)response.StatusCode;
                        result.Message = (response.ReasonPhrase + " " + responseBody).Trim();
                    }
                }
            }
            catch (Exception exception)
            {
                result.Message = NestedExceptionMessages(exception);
                result.HasException = true;
                Debug.WriteLine(exception.Message);
                Debug.WriteLine(exception.StackTrace);
                Debugger.Break();
            }
            finally
            {
                ServicePointManager.SecurityProtocol = securityProtocol;
            }

            return result;
        }

        private string NestedExceptionMessages(Exception exception)
        {
            if (exception.InnerException == null)
            {
                return exception.Message + " [" + exception.GetType() + "]";
            }

            return exception.Message + " [" + exception.GetType() + "]"
                   + Environment.NewLine
                   + NestedExceptionMessages(exception.InnerException);
        }
    }
}