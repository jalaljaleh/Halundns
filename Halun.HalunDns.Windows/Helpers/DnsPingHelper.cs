using DnsClient;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;

namespace Halun.HalunDns.Windows
{
    public static class DnsPingHelper
    {

        /// <summary>
        /// Banned Websites like gemini for Iran
        /// </summary>
        /// <param name="resolvedIp"></param>
        /// <param name="hostName"></param>
        /// <returns></returns>
        public static async Task<bool> CanBypassSanctionAsync(string dnsServerIp, string sanctionedSite = "gemini.google.com",int timeout = 7)
        {
            try
            {
         
                var endpoint = new IPEndPoint(IPAddress.Parse(dnsServerIp), 53);
                var client = new LookupClient(endpoint) {  };
                var dnsResult = await client.QueryAsync(sanctionedSite, QueryType.A);
                var targetIp = dnsResult.Answers.ARecords().FirstOrDefault()?.Address;

                if (targetIp == null) return false;

               
                var handler = new SocketsHttpHandler
                {
                    ConnectCallback = async (context, token) =>
                    {
                        var socket = new Socket(SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
                        await socket.ConnectAsync(new IPEndPoint(targetIp, 443), token);
                        return new NetworkStream(socket, ownsSocket: true);
                    }
                };

                using var httpClient = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(timeout) };
         
                var response = await httpClient.GetAsync($"https://{sanctionedSite}");

                return response.IsSuccessStatusCode || response.StatusCode != HttpStatusCode.Forbidden;
            }
            catch { return false; }
        }
        /// <summary>
        /// Filtering Websites In Iran
        /// </summary>
        /// <param name="dnsServerIp"></param>
        /// <param name="websiteUrl"></param>
        /// <returns></returns>
        public static async Task<bool> CanBypassFilteringAsync(string dnsServerIp, string blockedSite = "youtube.com")
        {
            try
            {
                var endpoint = new IPEndPoint(IPAddress.Parse(dnsServerIp), 53);
                var client = new LookupClient(endpoint) { };

                var result = await client.QueryAsync(blockedSite, QueryType.A);

                if (result.HasError || result.Answers.Count == 0) return false;

                foreach (var record in result.Answers.ARecords())
                {
                    string ip = record.Address.ToString();
            
                    if (ip.StartsWith("10.10.") || ip == "127.0.0.1" || ip == "0.0.0.0")
                    {
                        return false; 
                    }
                }
                return true; 
            }
            catch { return false; }
        }

        /// <summary>
        /// Pings a single IP address asynchronously.
        /// </summary>
        public static async Task<PingResult> PingAddressAsync(string ipAddress, int timeout = 2000)
        {
            try
            {
                using (Ping pingSender = new Ping())
                {
                    // Use Task.Run to ensure the ping operation doesn't block the UI thread
                    PingReply reply = await pingSender.SendPingAsync(ipAddress, timeout);

                    return new PingResult
                    {
                        IPAddress = ipAddress,
                        IsSuccess = reply.Status == IPStatus.Success,
                        RoundtripTime = reply.Status == IPStatus.Success ? reply.RoundtripTime : -1,
                        Status = reply.Status.ToString()
                    };
                }
            }
            catch (Exception ex)
            {
                return new PingResult
                {
                    IPAddress = ipAddress,
                    IsSuccess = false,
                    Status = $"Error: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Pings multiple addresses simultaneously (In Parallel).
        /// </summary>
        public static async Task<List<PingResult>> PingMultipleAddressesAsync(IEnumerable<string> ipAddresses)
        {
            // Start all ping tasks at the same time
            var tasks = ipAddresses.Select(ip => PingAddressAsync(ip));

            // Wait for all of them to complete
            var results = await Task.WhenAll(tasks);

            return results.ToList();
        }
    }
}
