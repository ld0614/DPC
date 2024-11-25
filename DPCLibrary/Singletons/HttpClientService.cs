using DPCLibrary.Utils;
using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;

namespace DPCLibrary.Singletons
{
    internal static class HttpClientService
    {
        private static HttpClient httpClient = null;

        public static HttpClient GetClient()
        {
            if (httpClient == null)
            {
                //Force the application to use the OS for TLS protocol decisions enabling TLS 1.2 and 1.3 if the OS Supports it
                //https://docs.microsoft.com/en-us/dotnet/framework/configure-apps/file-schema/runtime/appcontextswitchoverrides-element
                AppContext.SetSwitch("Switch.System.Net.DontEnableSystemDefaultTlsVersions", false);

                if (!DeviceInfo.GetOSVersion().IsGreaterThanWin10_1703)
                {
                    //DontEnableSystemDefaultTlsVersions was only introduced in .NET 4.7 which shipped with 1703, anything before hand will require manual forcing to TLS1.2
                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12; //Force the use of TLS 1.2
                }

                httpClient = new HttpClient();

                httpClient.DefaultRequestHeaders.Accept.Clear();

                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json")); //Set body encoding
            }

            return httpClient;
        }
    }
}