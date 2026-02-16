// Halun DNS - DnsWindowsHelper
// Purpose: Contains methods that interact with Windows networking APIs
// to get and set DNS server entries. Requires administrative privileges
// for modifying system network settings.
// Author: Jalal Jaleh
// License: MIT

using System;
using System.Collections.Generic;
using System.Management;
using System.Net.NetworkInformation;

namespace Halun.HalunDns.Windows
{
    public static class DnsWindowsHelper
    {
        /// <summary>
        /// Sets the DNS servers for all active network adapters that have a Default Gateway.
        /// </summary>
        /// <param name="primaryDns">The primary DNS IP (e.g., "178.22.122.100")</param>
        /// <param name="secondaryDns">The secondary DNS IP (e.g., "178.22.122.101")</param>
        public static void SetDns(string primaryDns, string secondaryDns)
        {
            string[] dnsServers = { primaryDns, secondaryDns };

            // We use WMI (Windows Management Instrumentation) to manage network adapters
            using (var networkConfigMng = new ManagementClass("Win32_NetworkAdapterConfiguration"))
            using (var networkConfigs = networkConfigMng.GetInstances())
            {
                foreach (ManagementObject adapter in networkConfigs)
                {
                    // Only modify adapters that are IP enabled
                    if ((bool)adapter["IPEnabled"])
                    {
                        // OPTIONAL: Only modify adapters that have a default gateway (Internet connected)
                        // This prevents messing up virtual adapters (like Docker/VMware)
                        if (adapter["DefaultIPGateway"] != null)
                        {
                            try
                            {
                                var newDns = adapter.GetMethodParameters("SetDNSServerSearchOrder");
                                newDns["DNSServerSearchOrder"] = dnsServers;

                                var result = adapter.InvokeMethod("SetDNSServerSearchOrder", newDns, null);

                                // Result 0 = Successful, 1 = Reboot Needed. anything else is an error.
                                // You can log 'result' here if needed.
                            }
                            catch (Exception ex)
                            {
                                throw new Exception($"Failed to set DNS on adapter {adapter["Description"]}: {ex.Message}");
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Reverts DNS to "Obtain DNS server address automatically" (DHCP) for active adapters.
        /// </summary>
        public static void UnsetDns()
        {
            using (var networkConfigMng = new ManagementClass("Win32_NetworkAdapterConfiguration"))
            using (var networkConfigs = networkConfigMng.GetInstances())
            {
                foreach (ManagementObject adapter in networkConfigs)
                {
                    if ((bool)adapter["IPEnabled"] && adapter["DefaultIPGateway"] != null)
                    {
                        try
                        {
                            // Passing null tells Windows to switch back to automatic (DHCP)
                            var newDns = adapter.GetMethodParameters("SetDNSServerSearchOrder");
                            newDns["DNSServerSearchOrder"] = null;

                            adapter.InvokeMethod("SetDNSServerSearchOrder", newDns, null);
                        }
                        catch (Exception ex)
                        {
                            throw new Exception($"Failed to unset DNS on adapter {adapter["Description"]}: {ex.Message}");
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Returns a formatted string of the current DNS servers on the active internet adapter.
        /// </summary>
        public static List<string> GetCurrentDns()
        {
            var dnsList = new List<string>();

            // Using NetworkInterface is faster/cleaner for READING data than WMI
            NetworkInterface[] adapters = NetworkInterface.GetAllNetworkInterfaces();
            foreach (NetworkInterface n in adapters)
            {
                // Filter for active network connections only
                if (n.NetworkInterfaceType != NetworkInterfaceType.Loopback &&
                    n.OperationalStatus == OperationalStatus.Up)
                {
                    IPInterfaceProperties ipProps = n.GetIPProperties();

                    // Check if it has a gateway (internet access)
                    if (ipProps.GatewayAddresses.Count > 0)
                    {
                        foreach (var dns in ipProps.DnsAddresses)
                        {
                            // Filter out IPv6 if you only care about IPv4
                            if (dns.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                            {
                                dnsList.Add(dns.ToString());
                            }
                        }
                        // Usually we only care about the first active adapter we find
                        // If you have multiple active connections (Wifi + Ethernet), remove this break.
                        if (dnsList.Count > 0) break;
                    }
                }
            }
            return dnsList;
        }
    }
}
