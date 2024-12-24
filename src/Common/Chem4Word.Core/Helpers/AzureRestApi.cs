// ---------------------------------------------------------------------------
//  Copyright (c) 2025, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;

namespace Chem4Word.Core.Helpers
{
    public static class AzureRestApi
    {
        private static string _product = Assembly.GetExecutingAssembly().FullName.Split(',')[0];
        private static string _class = MethodBase.GetCurrentMethod()?.DeclaringType?.Name;

        private static readonly HttpClient _httpClient = new HttpClient();

        public static RestApiResult GetResultAsJson(string uri, Dictionary<string, string> properties, int timeout)
        {
            var module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod()?.Name}()";

            var start = DateTime.UtcNow;

            var result = new RestApiResult();

            var securityProtocol = ServicePointManager.SecurityProtocol;

            try
            {
                SetConnectionLeaseTimeout(uri);
                SetServicePointManagerProperties(securityProtocol);

                SetCommonHeaders();

                var request = new HttpRequestMessage(HttpMethod.Post, uri);
                request.Properties["RequestTimeout"] = TimeSpan.FromSeconds(timeout);

                var content = new FormUrlEncodedContent(properties);
                request.Content = content;

                var response = _httpClient.SendAsync(request).Result;
                result.HttpStatusCode = (int)response.StatusCode;
                result.Success = response.IsSuccessStatusCode;
                if (result.Success)
                {
                    result.Json = response.Content.ReadAsStringAsync().Result;
                }
                else
                {
                    result.Message = $"Error: HttpStatusCode: {result.HttpStatusCode}{Environment.NewLine}{response.Content.ReadAsStringAsync().Result}";
                }
            }
            catch (Exception exception)
            {
                result.Message = NestedExceptionMessages(exception) + Environment.NewLine + exception.StackTrace;
                result.HasException = true;
                Debug.WriteLine(module + Environment.NewLine + result.Message);
            }
            finally
            {
                ServicePointManager.SecurityProtocol = securityProtocol;
            }

            result.Duration = DateTime.UtcNow - start;

            return result;
        }

        public static RestApiResult GetResultAsBytes(string uri, Dictionary<string, string> properties, int timeout)
        {
            var module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod()?.Name}()";

            var start = DateTime.UtcNow;

            var result = new RestApiResult();

            var securityProtocol = ServicePointManager.SecurityProtocol;

            try
            {
                SetConnectionLeaseTimeout(uri);
                SetServicePointManagerProperties(securityProtocol);

                SetCommonHeaders();

                var request = new HttpRequestMessage(HttpMethod.Post, uri);
                request.Properties["RequestTimeout"] = TimeSpan.FromSeconds(timeout);

                var content = new FormUrlEncodedContent(properties);
                request.Content = content;

                var response = _httpClient.SendAsync(request).Result;
                result.HttpStatusCode = (int)response.StatusCode;
                result.Success = response.IsSuccessStatusCode;
                if (result.Success)
                {
                    result.Bytes = response.Content.ReadAsByteArrayAsync().Result;
                }
                else
                {
                    result.Message = $"Error: HttpStatusCode: {result.HttpStatusCode}{Environment.NewLine}{response.Content.ReadAsStringAsync().Result}";
                }
            }
            catch (Exception exception)
            {
                result.Message = NestedExceptionMessages(exception) + Environment.NewLine + exception.StackTrace;
                result.HasException = true;
                Debug.WriteLine(module + Environment.NewLine + result.Message);
            }
            finally
            {
                ServicePointManager.SecurityProtocol = securityProtocol;
            }

            result.Duration = DateTime.UtcNow - start;

            return result;
        }

        private static string NestedExceptionMessages(Exception exception)
        {
            if (exception.InnerException == null)
            {
                return exception.Message + " [" + exception.GetType() + "]";
            }

            return exception.Message + " [" + exception.GetType() + "]"
                   + Environment.NewLine
                   + NestedExceptionMessages(exception.InnerException);
        }

        private static void SetCommonHeaders()
        {
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("user-agent", "Chem4Word");

            // Send dummy credentials to avoid sending Windows credentials
            var credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes("chem4word:chem4word"));
            _httpClient.DefaultRequestHeaders.Add("Authorization", "Basic " + credentials);
        }

        private static void SetServicePointManagerProperties(SecurityProtocolType securityProtocol)
        {
            ServicePointManager.DefaultConnectionLimit = 100;
            ServicePointManager.UseNagleAlgorithm = false;
            ServicePointManager.Expect100Continue = false;

            ServicePointManager.SecurityProtocol = securityProtocol | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
        }

        private static void SetConnectionLeaseTimeout(string uri)
        {
            // http://byterot.blogspot.com/2016/07/singleton-httpclient-dns.html
            var sp = ServicePointManager.FindServicePoint(new Uri(uri));
            sp.ConnectionLeaseTimeout = 60 * 1000; // 1 minute
        }
    }
}