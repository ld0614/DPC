using DPCLibrary.Models;
using DPCLibrary.Singletons;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using IPAddress = System.Net.IPAddress;

namespace DPCLibrary.Utils
{
    public static class HttpService
    {
        private static bool breakNetwork = false; //Overridden in Tests - not a nice solution but simple without rewriting the entire network stack and removes the need for Microsoft Fakes (Enterprise Only)

        public static Office365Exclusion[] GetOffice365EndPoints(Guid clientId)
        {
            if (breakNetwork)
            {
                throw new Exception("No Connection!");
            }
            HttpClient httpClient = HttpClientService.GetClient();

            string url = "https://endpoints.office.com/endpoints/WorldWide?Format=Json&ClientRequestId=" + clientId.ToString();

            string response = httpClient.GetStringAsync(new Uri(url)).Result;
            return JsonConvert.DeserializeObject<Office365Exclusion[]>(response);
        }

        public static IList<string> GetIPfromDNS(string url)
        {
            if (breakNetwork)
            {
                throw new Exception("No Connection!");
            }

            IPHostEntry dnsResult = Dns.GetHostEntry(url);
            IList<string> returnlist = new List<string>();
            foreach(IPAddress address in dnsResult.AddressList)
            {
                returnlist.Add(address.ToString());
            }
            return returnlist;
        }
    }
}