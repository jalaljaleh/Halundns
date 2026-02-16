// Halun DNS - DnsPingHelper
// Purpose: Implements pinging and bypass tests used by the UI to
// evaluate DNS server latency and reachability for specific URLs.
// Author: Jalal Jaleh
// License: MIT

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Threading.Tasks;

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
        public static async Task<bool> CanBypassSanctionAsync(string dnsServerIp, string sanctionedSite = "gemini.google.com", int timeout = 7)
        {
            try
            {
                // Resolve host using system DNS resolver. Note: querying a specific DNS server (dnsServerIp)
                // requires a DNS client library (e.g. DnsClient). For compatibility with .NET Framework
                // we fall back to the system resolver here.
                IPAddress[] ips = await Dns.GetHostAddressesAsync(sanctionedSite);
                var targetIp = ips.FirstOrDefault();
                if (targetIp == null) return false;

                // Use a simple HttpClient request. SocketsHttpHandler is not available on .NET Framework 4.8,
                // so use the standard HttpClientHandler.
                using (var handler = new HttpClientHandler())
                using (var httpClient = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(timeout) })
                {
                    try
                    {
                        var response = await httpClient.GetAsync("https://" + sanctionedSite);
                        return response.IsSuccessStatusCode || response.StatusCode != HttpStatusCode.Forbidden;
                    }
                    catch
                    {
                        return false;
                    }
                }
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
                // Use system DNS resolver as fallback. For querying specific DNS servers use a DNS client library.
                IPAddress[] ips = await Dns.GetHostAddressesAsync(blockedSite);
                if (ips == null || ips.Length == 0) return false;

                foreach (var addr in ips)
                {
                    var ip = addr.ToString();
                    if (ip.StartsWith("10.10.") || ip == "127.0.0.1" || ip == "0.0.0.0") return false;
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
