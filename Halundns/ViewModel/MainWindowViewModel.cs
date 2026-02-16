using Halun.HalunDns.Windows;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace Halun.HalunDns
{
    // Simple Base Class for INotifyPropertyChanged
    public class ViewModelBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
            protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
                => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

            protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
            {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }

    public class MainWindowViewModel : ViewModelBase
    {
        // --- Paths & Constants ---
        private readonly string PathDnsServers = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "serverlist.json"); //
        private readonly SolidColorBrush BrushDisconnected = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFF177FF")); //
        private readonly SolidColorBrush BrushConnected = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFC6FF98")); //

        // --- Backing Fields ---
        private Uri _pathIcon = new Uri("pack://application:,,,/dns.ico", UriKind.RelativeOrAbsolute); //
        private DnsServer _selectedServer;
        private DnsServer _currentServer;
        private string _targetUrl = string.Empty;
        private string _bypassButtonText = "Test"; //
        private bool _isConnected;
        private string _statusText = "Local DNS"; //
        private string _primaryDnsText = "-"; //
        private string _secondaryDnsText = "-"; //
        private string _loggerText = "Program Started"; //
        private string _lastLogText = "Program Started"; //
        private double _windowWidth = 900; //
        private Brush _windowBorderBrush;
        private Visibility _disconnectTrayVisibility = Visibility.Collapsed; //

        private CancellationTokenSource _bypassCts;
        private CancellationTokenSource _pingCts;

        // --- Properties ---
        public ObservableCollection<DnsServer> DnsServers { get; set; } = new ObservableCollection<DnsServer>(); //

        public Uri PathIcon { get => _pathIcon; set => SetProperty(ref _pathIcon, value); }
        public DnsServer SelectedServer { get => _selectedServer; set => SetProperty(ref _selectedServer, value); }
        public string TargetUrl { get => _targetUrl; set => SetProperty(ref _targetUrl, value); }
        public string BypassButtonText { get => _bypassButtonText; set => SetProperty(ref _bypassButtonText, value); }
        public string StatusText { get => _statusText; set => SetProperty(ref _statusText, value); }
        public string PrimaryDnsText { get => _primaryDnsText; set => SetProperty(ref _primaryDnsText, value); }
        public string SecondaryDnsText { get => _secondaryDnsText; set => SetProperty(ref _secondaryDnsText, value); }
        public string LoggerText { get => _loggerText; set => SetProperty(ref _loggerText, value); }
        public string LastLogText { get => _lastLogText; set => SetProperty(ref _lastLogText, value); }
        public double WindowWidth { get => _windowWidth; set => SetProperty(ref _windowWidth, value); }
        public Brush WindowBorderBrush { get => _windowBorderBrush; set => SetProperty(ref _windowBorderBrush, value); }
        public Visibility DisconnectTrayVisibility { get => _disconnectTrayVisibility; set => SetProperty(ref _disconnectTrayVisibility, value); }

        public bool IsConnected
        {
            get => _isConnected;
            set
            {
                if (SetProperty(ref _isConnected, value))
                {
                    if (_isConnected) ConnectServer();
                    else DisconnectServer();
                }
            }
        }

        // --- Events ---
        public event EventHandler RequestScrollToBottom; // For UI scrolling
        public event EventHandler<WindowState> RequestWindowStateChange;

        // --- Commands ---
        public ICommand LoadDataCommand { get; }
        public ICommand PingCommand { get; }
        public ICommand PingAllCommand { get; }
        public ICommand BypassTestCommand { get; }
        public ICommand CopyPrimaryCommand { get; }
        public ICommand CopySecondaryCommand { get; }
        public ICommand CopySelectedCommand { get; }
        public ICommand ToggleExpandCommand { get; }
        public ICommand AddServerCommand { get; }
        public ICommand EditServerCommand { get; }
        public ICommand RemoveServerCommand { get; }
        public ICommand RemoveAllCommand { get; }
        public ICommand ImportCommand { get; }
        public ICommand ExportCommand { get; }
        public ICommand OpenUrlCommand { get; }
        public ICommand ShutdownCommand { get; }
        public ICommand RestoreWindowCommand { get; }
        public ICommand MinimizeWindowCommand { get; }

        public MainWindowViewModel()
        {
            _windowBorderBrush = BrushDisconnected;

            // Initialize Commands
            LoadDataCommand = new AsyncRelayCommand(InitializeDataAsync);
            PingCommand = new AsyncRelayCommand(PingSelectedAsync);
            PingAllCommand = new AsyncRelayCommand(PingAllAsync);
            BypassTestCommand = new AsyncRelayCommand(TestBypassAsync);

            CopyPrimaryCommand = new RelayCommand(_ => CopyToClipboard(SelectedServer?.DnsAddress ?? string.Empty));
            CopySecondaryCommand = new RelayCommand(_ => CopyToClipboard(SelectedServer?.DnsAddressAlt ?? string.Empty));
            CopySelectedCommand = new RelayCommand(_ => CopyToClipboard(SelectedServer?.ToString() ?? string.Empty));

            ToggleExpandCommand = new RelayCommand(_ => WindowWidth = WindowWidth == 360 ? 900 : 360); //

            AddServerCommand = new RelayCommand(_ => AddServer());
            EditServerCommand = new RelayCommand(_ => EditServer());
            RemoveServerCommand = new RelayCommand(_ => RemoveServer());
            RemoveAllCommand = new RelayCommand(_ => RemoveAllServers());

            ImportCommand = new RelayCommand(_ => ImportServers());
            ExportCommand = new RelayCommand(_ => ExportServers());

            OpenUrlCommand = new RelayCommand(url => OpenUrl(url?.ToString() ?? string.Empty));
            ShutdownCommand = new RelayCommand(_ => ShutdownApp());
            RestoreWindowCommand = new RelayCommand(_ => RequestWindowStateChange?.Invoke(this, WindowState.Normal)); //
            MinimizeWindowCommand = new RelayCommand(_ => RequestWindowStateChange?.Invoke(this, WindowState.Minimized)); //
        }

        // --- Core Methods ---
        private async Task InitializeDataAsync()
        {
            await LoadServersAsync(); //
            CheckCurrentDnsStatus(); //
        }

        private async Task LoadServersAsync()
        {
            LogAction("Loading DNS servers..."); //
            try
            {
                if (!File.Exists(PathDnsServers))
                {
                     File.WriteAllText(PathDnsServers, "[]"); //
                    LogAction("Created empty serverlist.json."); //
                }

                var json = File.ReadAllText(PathDnsServers); //
                var servers = System.Text.Json.JsonSerializer.Deserialize<List<DnsServer>>(json) ?? new List<DnsServer>(); //

                var bypassServers = servers.Where(s => s.Type == "Bypass").OrderBy(s => s.Priority).ToList(); //
                var otherServers = servers.Where(s => s.Type != "Bypass").OrderBy(s => s.Priority).ToList(); //

                DnsServers.Clear(); //
                foreach (var s in bypassServers.Concat(otherServers))
                {
                    DnsServers.Add(s); //
                }

                LogAction($"Loaded {DnsServers.Count} DNS servers."); //
            }
            catch (Exception ex)
            {
                LogAction($"Error loading servers: {ex.Message}"); //
            }
        }

        private void SaveDnsServers()
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true }; //
                string jsonData = JsonSerializer.Serialize(DnsServers, options); //
                File.WriteAllText(PathDnsServers, jsonData); //
                LogAction("Server changes saved!"); //
            }
            catch (Exception ex)
            {
                LogAction($"Error saving servers: {ex.Message}"); //
            }
        }

        private void CheckCurrentDnsStatus()
        {
            LogAction("Checking current DNS status."); //
            var currentDns = DnsWindowsHelper.GetCurrentDns(); //

            if (currentDns == null || currentDns.Count == 0 || currentDns.First() == "192.168.1.1" || currentDns.First() == "192.168.0.1") //
            {
                UnsetUiState(); //
                LogAction("No custom DNS detected."); //
                return;
            }

            var match = DnsServers.FirstOrDefault(s => currentDns.Contains(s.DnsAddress) || (s.DnsAddressAlt != null && currentDns.Contains(s.DnsAddressAlt))); //
            if (match != null)
            {
                SetUiState(match); //
                LogAction($"Matched current DNS to {match.Name}."); //
            }
            else
            {
                var unknown = new DnsServer
                {
                    Name = "Unknown/Custom", //
                    DnsAddress = currentDns.First(), //
                    DnsAddressAlt = currentDns.Count > 1 ? currentDns.Last() : string.Empty //
                };
                SetUiState(unknown); //
                LogAction($"Current DNS is {unknown.DnsAddress}."); //
            }
        }

        private async void ConnectServer()
        {
            var target = SelectedServer ?? DnsServers.FirstOrDefault() ?? new DnsServer { Name = "Google", DnsAddress = "8.8.8.8", DnsAddressAlt = "8.8.4.4" }; //

            try
            {
                LogAction($"Applying DNS: {target.Name}..."); //
                DnsWindowsHelper.SetDns(target.DnsAddress, target.DnsAddressAlt); //
                _currentServer = target; //
                SetUiState(target); //
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to set DNS. Try running as Administrator.\nError: {ex.Message}"); //
                IsConnected = false; // Revert toggle
            }
        }

        private void DisconnectServer()
        {
            LogAction("Restoring default DNS..."); //
            try
            {
                DnsWindowsHelper.UnsetDns(); //
                _currentServer = null; //
                UnsetUiState(); //
            }
            catch (Exception ex)
            {
                LogAction($"Error unsetting DNS: {ex.Message}"); //
            }
        }

        private async void SetUiState(DnsServer server)
        {
            PathIcon = new Uri("pack://application:,,,/dns-connected.ico", UriKind.RelativeOrAbsolute); //
            StatusText = $"Connected to {server.Name}"; //
            DisconnectTrayVisibility = Visibility.Visible; //
            WindowBorderBrush = BrushConnected; //       
            PrimaryDnsText = server.DnsAddress; //
            SecondaryDnsText = server.DnsAddressAlt ?? "-"; //
            SelectedServer = server; //
            _isConnected = true;
            OnPropertyChanged(nameof(IsConnected)); // Prevent recursive triggers

            _pingCts?.Cancel(); //
            _pingCts = new CancellationTokenSource(); //
            await PingServerAsync(server, _pingCts.Token); //
        }

        private void UnsetUiState()
        {
            PathIcon = new Uri("pack://application:,,,/dns.ico", UriKind.RelativeOrAbsolute); //
            StatusText = "Local DNS (Router)"; //
            DisconnectTrayVisibility = Visibility.Collapsed; //
            WindowBorderBrush = BrushDisconnected; //

            PrimaryDnsText = "-"; //
            SecondaryDnsText = "-"; //
            SelectedServer = null; //
            _isConnected = false;
            OnPropertyChanged(nameof(IsConnected));
        }

        // --- Utility Operations ---
        private async Task TestBypassAsync()
        {
            if (BypassButtonText == "Cancel") //
            {
                _bypassCts?.Cancel(); //
                foreach (var s in DnsServers) s.Bypass = string.Empty; //
                BypassButtonText = "Test"; //
                LogAction("Bypass checks canceled."); //
                return;
            }

            string target = TargetUrl.Replace("https://", "").Replace("http://", "").Replace("www.", "").Trim(); //
            if (string.IsNullOrWhiteSpace(target))
            {
                LogAction("No URL provided for bypass test."); //
                return;
            }

            BypassButtonText = "Cancel"; //
            _bypassCts?.Cancel(); //
            _bypassCts = new CancellationTokenSource(); //
            var token = _bypassCts.Token; //

            LogAction($"Starting PARALLEL bypass test (Max 10 at once) for URL: {target}"); //
            foreach (var s in DnsServers) s.Bypass = "Wait..."; //

            using (var semaphore = new SemaphoreSlim(10)) //
            {
                try
                {
                    var tasks = DnsServers.Select(async server => //
                    {
                        await semaphore.WaitAsync(token); //
                        try
                        {
                            token.ThrowIfCancellationRequested(); //
                            server.Bypass = "Testing..."; //

                            var taskSanction = DnsPingHelper.CanBypassSanctionAsync(server.DnsAddress, target); //
                            var taskFiltering = DnsPingHelper.CanBypassFilteringAsync(server.DnsAddress, target); //

                            await Task.WhenAll(taskSanction, taskFiltering); //

                            string sanctionResult = await taskSanction ? "✅ Sanction" : "❌ Sanction"; //
                            string filteringResult = await taskFiltering ? "✅ Filtering" : "❌ Filtering"; //

                            server.Bypass = $"{sanctionResult} | {filteringResult}"; //
                        }
                        catch (OperationCanceledException) { server.Bypass = "Canceled"; } //
                        catch (Exception ex) { server.Bypass = "Error"; LogAction($"Error testing {server.Name}: {ex.Message}"); } //
                        finally { semaphore.Release(); } //
                    });

                    await Task.WhenAll(tasks); //
                    LogAction("All bypass tests completed."); //
                }
                catch (OperationCanceledException) { LogAction("Bypass test process canceled."); } //
                finally
                {
                    BypassButtonText = "Test"; //
                    _bypassCts = null; //
                }
            }
        }

        private async Task PingSelectedAsync()
        {
            if (SelectedServer == null) return; //
            _pingCts?.Cancel(); //
            _pingCts = new CancellationTokenSource(); //
            await PingServerAsync(SelectedServer, _pingCts.Token); //
        }

        private async Task PingAllAsync()
        {
            _pingCts?.Cancel(); //
            _pingCts = new CancellationTokenSource(); //
            var token = _pingCts.Token; //

            LogAction("Starting parallel ping for all servers."); //
            foreach (var s in DnsServers) s.Ping = "Wait..."; //

            using (var semaphore = new SemaphoreSlim(10)) //
            {
                try
                {
                    var tasks = DnsServers.Select(async server => //
                    {
                        await semaphore.WaitAsync(token); //
                        try { token.ThrowIfCancellationRequested(); await PingServerAsync(server, token); } //
                        catch (OperationCanceledException) { server.Ping = "Canceled"; } //
                        finally { semaphore.Release(); } //
                    });

                    await Task.WhenAll(tasks); //
                    LogAction("Ping all completed."); //
                }
                catch (OperationCanceledException) { LogAction("Ping all canceled."); } //
            }
        }

        private async Task PingServerAsync(DnsServer server, CancellationToken token)
        {
            if (server == null || token.IsCancellationRequested) return; //
            server.Ping = "⚡ Preparing..."; //

            try { await Task.Delay(100, token); } catch (TaskCanceledException) { return; } //

            server.Ping = "🔍 Pinging..."; //
            try
            {
                var result = await DnsPingHelper.PingAddressAsync(server.DnsAddress); //
                if (token.IsCancellationRequested) return; //

                if (result.IsSuccess)
                {
                    server.Ping = $"{result.RoundtripTime} ms"; //
                    LogAction($"Ping {server.Name}: {result.RoundtripTime} ms"); //
                }
                else
                {
                    server.Ping = "❌ Timed Out"; //
                    LogAction($"Ping timed out for {server.Name}"); //
                }
            }
            catch (Exception) { server.Ping = "❌ Error"; } //
        }

        // --- UI Events and Basic Logic ---
        private void CopyToClipboard(string text)
        {
            if (string.IsNullOrWhiteSpace(text) || text == "-") return; //
            try
            {
                Clipboard.SetText(text); //
                LogAction($"Copied to clipboard: {text}"); //
            }
            catch (Exception ex) { LogAction($"Clipboard error: {ex.Message}"); } //
        }

        private void AddServer()
        {
            var addWin = new DnsServerWindow(); //
            if (addWin.ShowDialog() == true) //
            {
                DnsServers.Add(addWin.ServerResult); //
                SaveDnsServers(); //
            }
        }

        private void EditServer()
        {
            if (SelectedServer != null) //
            {
                var editWin = new DnsServerWindow(SelectedServer); //
                if (editWin.ShowDialog() == true) SaveDnsServers(); //
            }
        }

        private void RemoveServer()
        {
            if (SelectedServer == null) return; //
            if (_currentServer != null && (SelectedServer == _currentServer || SelectedServer.DnsAddress == _currentServer.DnsAddress)) //
            {
                MessageBox.Show("Cannot remove the currently connected server. Please disconnect first.", "Operation Blocked", MessageBoxButton.OK, MessageBoxImage.Stop); //
                return;
            }
            LogAction($"{SelectedServer.Name} removed from the list!"); //
            DnsServers.Remove(SelectedServer); //
            SaveDnsServers(); //
        }

        private void RemoveAllServers()
        {
            if (MessageBox.Show("Are you sure you want to remove all servers?", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return; //
            if (IsConnected) DisconnectServer(); // Ensure safe detachment
            DnsServers.Clear(); //
            LogAction("All servers removed!"); //
            SaveDnsServers(); //
        }

        private void ExportServers()
        {
            var saveFileDialog = new SaveFileDialog { Filter = "JSON Files (*.json)|*.json|All files (*.*)|*.*", FileName = "DnsServers_Export.json", Title = "Export DNS Servers List" }; //
            if (saveFileDialog.ShowDialog() == true) //
            {
                try
                {
                    var options = new JsonSerializerOptions { WriteIndented = true }; //
                    string jsonData = JsonSerializer.Serialize(DnsServers, options); //
                    File.WriteAllText(saveFileDialog.FileName, jsonData); //
                    LogAction("Export completed successfully!"); //
                }
                catch (Exception ex) { LogAction($"Error during export: {ex.Message}"); } //
            }
        }

        private void ImportServers()
        {
            var openFileDialog = new OpenFileDialog { Filter = "JSON Files (*.json)|*.json|All files (*.*)|*.*", Title = "Select DNS Servers Export File" }; //
            if (openFileDialog.ShowDialog() == true) //
            {
                try
                {
                    string jsonData = File.ReadAllText(openFileDialog.FileName); //
                    var importedData = JsonSerializer.Deserialize<List<DnsServer>>(jsonData); //
                    if (importedData != null)
                    {
                        foreach (var server in importedData) //
                        {
                            if (!DnsServers.Any(x => x.DnsAddress == server.DnsAddress)) DnsServers.Add(server); //
                        }
                        LogAction($"{importedData.Count} servers processed successfully!"); //
                        SaveDnsServers(); //
                    }
                }
                catch (Exception ex) { LogAction($"Error importing file: {ex.Message}"); } //
            }
        }

        private void OpenUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return;
            try { Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true }); } //
            catch (Exception ex) { LogAction($"Could not open link: {ex.Message}"); } //
        }

        private void ShutdownApp()
        {
            Environment.Exit(0);
        }

        private void LogAction(string message)
        {
            string time = DateTime.Now.ToString("HH:mm:ss"); //
            LastLogText = $"{time}  {message}"; //

            // For multiline logger
            if (LoggerText == "Program Started") LoggerText = LastLogText;
            else LoggerText += $"\n{LastLogText}"; //

            // Tell the View to scroll down safely
            RequestScrollToBottom?.Invoke(this, EventArgs.Empty);
            Debug.WriteLine(message); //
        }
    }
}