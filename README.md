<br />
<div align="center">
  <a href="https://github.com/jalaljaleh/HalunDns">
    <img src="/Halun.HalunDns.Windows/dns.ico" alt="Logo" width="120" height="120">
  </a>

  <h3 align="center">Halun DNS</h3>

  <p align="center">
    A high-performance, parallel-testing DNS management tool for Windows.
    <br />
    <br />
    <a href="https://jalaljaleh.github.io/">Official Website</a>
    ·
    <a href="https://github.com/jalaljaleh/HalunDns/issues">Report Bug</a>
    ·
    <a href="https://github.com/jalaljaleh/HalunDns/issues">Request Feature</a>
  </p>
</div>

<p align="center">
  <img src="https://img.shields.io/badge/.NET-10.0--windows-blueviolet?style=flat-square" alt=".NET 10">
  <img src="https://img.shields.io/badge/Platform-Windows-blue?style=flat-square" alt="Platform Windows">
  <img src="https://img.shields.io/badge/License-MIT-green?style=flat-square" alt="License MIT">
  <a href="https://github.com/jalaljaleh/HalunDns"><img src="https://komarev.com/ghpvc/?username=jalaljaleh&label=PROJECT%20VIEWS&style=flat-square" alt="Views"></a>
</p>

# Halun DNS

**Halun DNS** is a modern Windows utility designed for users who need fast, reliable, and secure DNS management. Built on **.NET 10**, it features a sophisticated parallel ping engine and advanced bypass testing for sanctions and filtering.



## Key Features

- **⚡ Parallel Ping Engine:** Test latencies across your entire server list simultaneously using asynchronous semaphores.
- **🌍 Sanction & Filtering Bypass:** Real-time testing of URLs against specific DNS nodes to check for accessibility.
- **🎨 Modern WPF UI:** Clean, dark-themed interface with DPI awareness for 4K monitors.
- **📥 Import/Export:** Manage your server lists easily via standard JSON formats.
- **🔔 System Tray Integration:** Stay connected in the background with a native Windows notify icon.
- **🛡 Admin-Safe:** Built-in manifest handling for required network stack permissions.

## Getting Started

### Prerequisites
- Windows 10 or 11
- [.NET 10 Runtime](https://dotnet.microsoft.com/download)

### Installation
1. Download the latest release from the [Releases](https://github.com/jalaljaleh/HalunDns/releases) page.
2. Extract the ZIP file.
3. Run `HalunDns.exe` as **Administrator**.

## Project Structure

* **MainWindow:** The central hub for connecting/disconnecting and monitoring status.
* **DnsServerWindow:** A dedicated dialog for adding and editing custom DNS nodes.
* **DnsWindowsHelper:** Core logic for interacting with the WMI and Windows Registry for DNS changes.
* **DnsPingHelper:** The engine behind the parallel latency and bypass testing.

## Usage Example (serverlist.json)

```json
[
  {
    "Name": "Cloudflare",
    "DnsAddress": "1.1.1.1",
    "DnsAddressAlt": "1.0.0.1",
    "Priority": 1,
    "Type": "Global"
  },
  {
    "Name": "Shecan",
    "DnsAddress": "178.22.122.100",
    "DnsAddressAlt": "185.51.200.2",
    "Priority": 2,
    "Type": "Bypass"
  }
]
```

## Contributing
Contributions are welcome!
Fork the Project
Create your Feature Branch (git checkout -b feature/AmazingFeature)
Commit your Changes (git commit -m 'Add some AmazingFeature')
Push to the Branch (git push origin feature/AmazingFeature)
Open a Pull Request

## Contact
Jalal Jaleh - @jalaljaleh Project Link: https://github.com/jalaljaleh/HalunDns

Developed with ❤️ for a freer internet.
