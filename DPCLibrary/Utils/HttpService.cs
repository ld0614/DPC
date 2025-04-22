using DPCLibrary.Enums;
using DPCLibrary.Models;
using DPCLibrary.Singletons;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using IPAddress = System.Net.IPAddress;

namespace DPCLibrary.Utils
{
    public static class HttpService
    {
        private static bool breakNetwork = false; //Overridden in Tests - not a nice solution but simple without rewriting the entire network stack and removes the need for Microsoft Fakes (Enterprise Only)

        /// <summary>
        /// This method handles the core logic of preparing to get the latest Office 365 exclusion routes. The Microsoft endpoint requires a unique identifier
        /// so we get it from registry if it already exists, if it doesn't we generate a new one and save it.
        /// After we get the results from the HTTP Service we process the results to only return the results that DPC can handle. This is because the service
        /// will return various types of result including URLs, wildcard URLs, IPv4 and IPv6 routes
        /// </summary>
        public static List<string> GetOffice365ExcludeRoutes(IPAddressFamily addressFamily)
        {
            Guid? nClientId = AccessRegistry.ReadMachineGuid(RegistrySettings.ClientId, RegistrySettings.InternalStateOffset);
            Guid clientId;
            if (nClientId == null)
            {
                clientId = Guid.NewGuid();
                AccessRegistry.SaveMachineData(RegistrySettings.ClientId, clientId.ToString());
            }
            else
            {
                clientId = (Guid)nClientId;
            }

            Office365Exclusion[] Office365Endpoints = HttpService.GetOffice365EndPoints(clientId);
            List<string[]> UsableIPList = Office365Endpoints.Where(e => e.Ips != null && e.Category == Office365EndpointCategory.Optimize).Select(e => e.Ips).ToList();
            List<string> ipList = new List<string>();
            foreach (string[] list in UsableIPList)
            {
                foreach (string item in list)
                {
                    if (ipList.Contains(item)) continue;

                    if (addressFamily == IPAddressFamily.IPv4)
                    {
                        if (Validate.IPv4(item) || Validate.IPv4CIDR(item))
                        {
                            ipList.Add(item);
                        }
                    }
                    else if (addressFamily == IPAddressFamily.IPv6)
                    {
                        if (Validate.IPv6(item) || Validate.IPv6CIDR(item))
                        {
                            ipList.Add(item);
                        }
                    }
                }
            }

            return ipList;
        }

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