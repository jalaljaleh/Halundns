using Halun.HalunDns.Windows;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Halun.HalunDns.Windows
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {

        private readonly string PathDnsServers = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "serverlist.json");

        private Uri _pathIcon = new Uri("pack://application:,,,/dns.ico", UriKind.RelativeOrAbsolute);
        public Uri PathIcon
        {
            get => _pathIcon;
            set { _pathIcon = value; OnPropertyChanged(nameof(PathIcon)); }
        }

        public ObservableCollection<DnsServer> DnsServers { get; set; } = new ObservableCollection<DnsServer>();

        private bool _isInternalChange;
        private DnsServer? _currentServer;
        private CancellationTokenSource? _bypassCts;
        private CancellationTokenSource? _pingCts;

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;

            Loaded += async (_, _) =>
            {
                await LoadServersAsync();
                CheckCurrentDnsStatus();
            };

            // Event handlers
            BtnDnsPrimary.Click += CopyDns_Click;
            BtnDnsSecondary.Click += CopyDns_Click;
            DataGridServers.SelectionChanged += DataGridServers_SelectionChanged;
            ToggleBtnConnect.Checked += ToggleBtnConnect_Checked;
            ToggleBtnConnect.Unchecked += ToggleBtnConnect_Unchecked;
            BtnUrlBypass.Click += BtnUrlBypass_Click;

            // Assuming BtnDnsPing is the single ping button
            BtnDnsPing.Click += MenuBtnPing_Click;
            MenuBtnPing.Click += MenuBtnPing_Click;
            MenuBtnPingAll.Click += MenuBtnPingAll_Click;

            btnClose.Click += (_, _) => this.Close();

            // External Links
            BtnCopyright.Click += (_, _) => OpenUrl("https://jalaljaleh.github.io/");

            BtnMinimize.Click += (_, _) => WindowState = WindowState.Minimized;
            TitleBorder.MouseDown += TitleBorder_MouseDown;

            // Exit & Tray
            MenuBtnExit.Click += (s, e) => ShutdownApp();
            TrayMenuBtnExit.Click += (s, e) => ShutdownApp();
            TrayMenuBtnRestoreWindow.Click += (s, e) => RestoreWindow();
            NotifyIcon.TrayLeftMouseUp += NotifyIcon_TrayLeftMouseUp;

            // Data Management
            MenuBtnImport.Click += MenuBtnImport_Click;
            MenuBtnExport.Click += MenuBtnExport_Click;
            MenuBtnAdd.Click += MenuBtnAdd_Click;
            MenuBtnEdit.Click += MenuBtnEdit_Click;
            MenuBtnRemove.Click += MenuBtnRemove_Click;
            MenuBtnRemoveAll.Click += MenuBtnRemoveAll_Click;
            this.BtnExpand.Click += BtnExpand_Click;
        }
        private void OpenLick_Click(object s, RoutedEventArgs e) => OpenUrl((s as MenuItem).Tag.ToString());

        private void MenuBtnEdit_Click(object sender, RoutedEventArgs e)
        {
            if (DataGridServers.SelectedItem is DnsServer selected)
            {
                var editWin = new DnsServerWindow(selected);
                if (editWin.ShowDialog() == true)
                {
                    SaveDnsServers();
                    //DataGridServers.Items.Refresh();
                }
            }
        }

        private void MenuBtnAdd_Click(object sender, RoutedEventArgs e)
        {
            var addWin = new DnsServerWindow();
            if (addWin.ShowDialog() == true)
            {
                DnsServers.Add(addWin.ServerResult);
                SaveDnsServers();
            }
        }

        private void OpenUrl(string url)
        {
            try
            {
                Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
            }
            catch (Exception ex)
            {
                LogAction($"Could not open link: {ex.Message}");
            }
        }

        private void ShutdownApp()
        {
            _bypassCts?.Cancel();
            _pingCts?.Cancel();
            Environment.Exit(0);
        }

        private void RestoreWindow()
        {
            this.Show();
            this.WindowState = WindowState.Normal;
            this.Activate();
        }

        private void BtnExpand_Click(object sender, RoutedEventArgs e)
        {
            if (this.BtnExpand.Tag as string != "expanded")
            {
                this.Width = 900;
                this.BtnExpand.Tag = "expanded";
                return;
            }
            this.Width = 380;
            this.BtnExpand.Tag = "0";
        }

        private void MenuBtnRemoveAll_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Are you sure you want to remove all servers?", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
                return;

            UnsetUiState();
            DnsServers.Clear();
            LogAction("All servers removed!");
            SaveDnsServers();
        }

        private void MenuBtnRemove_Click(object sender, RoutedEventArgs e)
        {
            var selected = DataGridServers.SelectedItem as DnsServer;
            if (selected == null)
            {
                LogAction("No server selected to remove");
                return;
            }

            // SAFETY CHECK: Don't allow removing the active server without disconnecting first
            if (_currentServer != null && (selected == _currentServer || selected.DnsAddress == _currentServer.DnsAddress))
            {
                MessageBox.Show("Cannot remove the currently connected server. Please disconnect first.", "Operation Blocked", MessageBoxButton.OK, MessageBoxImage.Stop);
                return;
            }

            DnsServers.Remove(selected);
            LogAction($"{selected.Name} removed from the list!");
            SaveDnsServers();
        }

        void SaveDnsServers()
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                string jsonData = JsonSerializer.Serialize(DnsServers, options);
                File.WriteAllText(PathDnsServers, jsonData);
                LogAction("Server changes saved!");
            }
            catch (Exception ex)
            {
                LogAction($"Error saving servers: {ex.Message}");
            }
        }

        private void MenuBtnExport_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = "JSON Files (*.json)|*.json|All files (*.*)|*.*",
                FileName = "DnsServers_Export.json",
                Title = "Export DNS Servers List"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    var options = new JsonSerializerOptions { WriteIndented = true };
                    string jsonData = JsonSerializer.Serialize(DnsServers, options);
                    File.WriteAllText(saveFileDialog.FileName, jsonData);

                    string filePath = saveFileDialog.FileName;
                    if (File.Exists(filePath))
                    {
                        Process.Start("explorer.exe", $"/select,\"{filePath}\"");
                    }
                    LogAction("Export completed successfully!");
                }
                catch (Exception ex)
                {
                    LogAction($"Error during export: {ex.Message}");
                }
            }
        }

        private void MenuBtnImport_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "JSON Files (*.json)|*.json|All files (*.*)|*.*",
                Title = "Select DNS Servers Export File"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    string jsonData = File.ReadAllText(openFileDialog.FileName);
                    var importedData = JsonSerializer.Deserialize<List<DnsServer>>(jsonData);

                    if (importedData != null)
                    {
                        foreach (var server in importedData)
                        {
                            // Avoid duplicates based on address
                            if (!DnsServers.Any(x => x.DnsAddress == server.DnsAddress))
                            {
                                DnsServers.Add(server);
                            }
                        }
                        LogAction($"{importedData.Count} servers processed successfully!");
                        SaveDnsServers();
                    }
                }
                catch (Exception ex)
                {
                    LogAction($"Error importing file: {ex.Message}\nMake sure the file format is correct.");
                }
            }
        }

        void SetIcon(bool isConnected)
        {
            PathIcon = isConnected
                ? new Uri("pack://application:,,,/dns-connected.ico", UriKind.RelativeOrAbsolute)
                : new Uri("pack://application:,,,/dns.ico", UriKind.RelativeOrAbsolute);
        }

        private void NotifyIcon_TrayLeftMouseUp(object sender, RoutedEventArgs e)
        {
            if (this.IsVisible)
            {
                this.Hide();
            }
            else
            {
                RestoreWindow();
                // Hack to bring to front
                this.Topmost = true;
                this.Topmost = false;
            }
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            e.Cancel = true;
            this.Hide();
        }

        private void TitleBorder_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private void LogAction(string message)
        {
            // Dispatch to UI thread if necessary, though simple property assignment is usually safe 
            // but ScrollToBottom needs UI thread.
            Dispatcher.Invoke(() =>
            {
                LabelLogger.Text = $"{DateTime.Now:HH:mm:ss} /> {message}";
                LabelLoggerMain.Text += $"\n{DateTime.Now:HH:mm:ss} /> {message}";
                ScrollerLogger.ScrollToBottom();
            });
            Debug.WriteLine(message);
        }

        // -------------------- BYPASS TEST --------------------
        private async void BtnUrlBypass_Click(object sender, RoutedEventArgs e)
        {
            var servers = DataGridServers.Items.OfType<DnsServer>().ToList();

            if ((string)BtnUrlBypass.Content == "Cancel")
            {
                _bypassCts?.Cancel();
                foreach (var s in servers) s.Bypass = string.Empty;
                BtnUrlBypass.Content = "Test";
                LogAction("Bypass checks canceled.");
                return;
            }

            string targetUrl = InputUrlBypass.Text.Replace("https://","").Replace("http://", "").Replace("www.", "").Trim();
            if (string.IsNullOrWhiteSpace(targetUrl))
            {
                LogAction("No URL provided for bypass test.");
                return;
            }

            BtnUrlBypass.Content = "Cancel";
            _bypassCts?.Cancel();
            _bypassCts = new CancellationTokenSource();
            var token = _bypassCts.Token;

            LogAction($"Starting PARALLEL bypass test (Max 5 at once) for URL: {targetUrl}");

            foreach (var s in servers) s.Bypass = "Wait...";

            // Use Semaphore to limit concurrency (don't flood the network)
            using (var semaphore = new SemaphoreSlim(5))
            {
                try
                {
                    var tasks = servers.Select(async server =>
                    {
                        await semaphore.WaitAsync(token); // Wait for slot
                        try
                        {
                            token.ThrowIfCancellationRequested();
                            server.Bypass = "Testing...";

                            var taskSanction = DnsPingHelper.CanBypassSanctionAsync(server.DnsAddress, targetUrl);
                            var taskFiltering = DnsPingHelper.CanBypassFilteringAsync(server.DnsAddress, targetUrl);

                            await Task.WhenAll(taskSanction, taskFiltering);

                            bool bypassSanction = await taskSanction;
                            bool bypassFiltering = await taskFiltering;

                            string sanctionResult = bypassSanction ? "✅ Sanction" : "❌ Sanction";
                            string filteringResult = bypassFiltering ? "✅ Filtering" : "❌ Filtering";

                            server.Bypass = $"{sanctionResult} | {filteringResult}";
                        }
                        catch (OperationCanceledException)
                        {
                            server.Bypass = "Canceled";
                            // Don't rethrow here to allow other tasks to finish gracefully if needed
                        }
                        catch (Exception ex)
                        {
                            server.Bypass = "Error";
                            LogAction($"Error testing {server.Name}: {ex.Message}");
                        }
                        finally
                        {
                            semaphore.Release();
                        }
                    });

                    await Task.WhenAll(tasks);
                    LogAction("All bypass tests completed.");
                }
                catch (OperationCanceledException)
                {
                    LogAction("Bypass test process canceled.");
                }
                catch (Exception ex)
                {
                    LogAction($"Critical error during batch test: {ex.Message}");
                }
                finally
                {
                    BtnUrlBypass.Content = "Test";
                    _bypassCts = null;
                }
            }
        }

        // -------------------- PING SINGLE --------------------
        private async void MenuBtnPing_Click(object sender, RoutedEventArgs e)
        {
            var selected = DataGridServers.SelectedItem as DnsServer;
            if (selected == null)
            {
                MessageBox.Show("Please select a DNS server from the list.");
                return;
            }

            // Don't cancel global Pings if just pinging one, but create a fresh token for this op
            _pingCts?.Cancel();
            _pingCts = new CancellationTokenSource();

            await PingServerAsync(selected, _pingCts.Token);
        }

        private async Task PingServerAsync(DnsServer server, CancellationToken token)
        {
            if (server == null || token.IsCancellationRequested) return;

            // Ensure we are on UI thread for property updates or data binding takes care of it
            server.Ping = "⚡ Preparing...";

            // Small delay to allow UI to render "Preparing"
            try
            {
                await Task.Delay(100, token);
            }
            catch (TaskCanceledException) { return; }

            server.Ping = "🔍 Pinging...";

            try
            {
                var result = await DnsPingHelper.PingAddressAsync(server.DnsAddress);

                if (token.IsCancellationRequested) return;

                if (result.IsSuccess)
                {
                    server.Ping = $"{result.RoundtripTime} ms";
                    LogAction($"Ping {server.Name}: {result.RoundtripTime} ms");
                }
                else
                {
                    server.Ping = "❌ Timed Out";
                    LogAction($"Ping timed out for {server.Name}");
                }
            }
            catch (Exception)
            {
                server.Ping = "❌ Error";
            }
        }

        // -------------------- PING ALL --------------------
        private async void MenuBtnPingAll_Click(object sender, RoutedEventArgs e)
        {
            _pingCts?.Cancel();
            _pingCts = new CancellationTokenSource();
            var token = _pingCts.Token;
            var servers = DataGridServers.Items.OfType<DnsServer>().ToList();

            LogAction("Starting parallel ping for all servers.");

            foreach (var s in servers) s.Ping = "Wait...";

            // FIX: Use Parallelism (Semaphore) instead of sequential foreach loop for vastly improved speed
            using (var semaphore = new SemaphoreSlim(10)) // Ping 10 at a time
            {
                try
                {
                    var tasks = servers.Select(async server =>
                    {
                        await semaphore.WaitAsync(token);
                        try
                        {
                            token.ThrowIfCancellationRequested();
                            await PingServerAsync(server, token);
                        }
                        catch (OperationCanceledException)
                        {
                            server.Ping = "Canceled";
                        }
                        finally
                        {
                            semaphore.Release();
                        }
                    });

                    await Task.WhenAll(tasks);
                    LogAction("Ping all completed.");
                }
                catch (OperationCanceledException)
                {
                    LogAction("Ping all canceled.");
                }
                finally
                {
                    _pingCts = null;
                }
            }
        }

        // -------------------- COPY DNS --------------------
        private async void CopyDns_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn) return;

            string text = btn.Name == "BtnDnsPrimary" ? BtnDnsPrimaryLabel.Text : BtnDnsSecondaryLabel.Text;
            if (string.IsNullOrWhiteSpace(text) || text == "-") return;

            try
            {
                Clipboard.SetText(text);
                LogAction($"Copied DNS: {text}");

                var originalContent = btn.Content;
                btn.Content = "Copied!";
                btn.IsEnabled = false;

                await Task.Delay(1500);
                btn.Content = originalContent;
                btn.IsEnabled = true;
            }
            catch (Exception ex)
            {
                LogAction($"Clipboard error: {ex.Message}");
            }
        }

        // -------------------- LOAD SERVERS --------------------
        private async Task LoadServersAsync()
        {
            LogAction("Loading DNS servers...");

            try
            {
                if (!File.Exists(PathDnsServers))
                {
                    await File.WriteAllTextAsync(PathDnsServers, "[]");
                    LogAction("Created empty serverlist.json.");
                }

                var json = await File.ReadAllTextAsync(PathDnsServers);
                var servers = JsonSerializer.Deserialize<List<DnsServer>>(json) ?? new List<DnsServer>();

                // Sort: First Bypass by Priority, then others by Priority
                var bypassServers = servers.Where(s => s.Type == "Bypass").OrderBy(s => s.Priority).ToList();
                var otherServers = servers.Where(s => s.Type != "Bypass").OrderBy(s => s.Priority).ToList();

                DnsServers.Clear();
                foreach (var s in bypassServers.Concat(otherServers))
                {
                    DnsServers.Add(s);
                }

                LogAction($"Loaded {DnsServers.Count} DNS servers.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Load error: {ex.Message}");
                LogAction($"Error loading servers: {ex.Message}");
            }
        }

        // -------------------- STATUS CHECK --------------------
        private void CheckCurrentDnsStatus()
        {
            LogAction("Checking current DNS status.");

            var currentDns = DnsWindowsHelper.GetCurrentDns();
            // Basic check for router defaults or empty
            if (currentDns == null || currentDns.Count == 0
                || currentDns.First() == "192.168.1.1" || currentDns.First() == "192.168.0.1")
            {
                UnsetUiState();
                LogAction("No custom DNS detected.");
                return;
            }

            var match = DnsServers.FirstOrDefault(s => currentDns.Contains(s.DnsAddress) || (s.DnsAddressAlt != null && currentDns.Contains(s.DnsAddressAlt)));

            if (match != null)
            {
                SetUiState(match);
                LogAction($"Matched current DNS to {match.Name}.");
            }
            else
            {
                var unknown = new DnsServer
                {
                    Name = "Unknown/Custom",
                    DnsAddress = currentDns.First(),
                    DnsAddressAlt = currentDns.Count > 1 ? currentDns.Last() : string.Empty
                };
                SetUiState(unknown);
                LogAction($"Current DNS is {unknown.DnsAddress}.");
            }
        }

        // -------------------- CONNECT --------------------
        private void ToggleBtnConnect_Checked(object sender, RoutedEventArgs e)
        {
            if (_isInternalChange) return;

            var selected = DataGridServers.SelectedItem as DnsServer;
            if (selected == null)
            {
                // Fallback to first in list or default Google
                selected = this.DnsServers.FirstOrDefault();
                if (selected == null)
                {
                    selected = new DnsServer()
                    {
                        Name = "Google",
                        DnsAddress = "8.8.8.8",
                        DnsAddressAlt = "8.8.4.4",
                    };
                }
                LogAction($"No server selected. Auto-selecting: {selected.Name}");
            }

            // Execute the connection
            try
            {
                LogAction($"Applying DNS: {selected.Name}...");
                DnsWindowsHelper.SetDns(selected.DnsAddress, selected.DnsAddressAlt);

                _currentServer = selected;
                SetUiState(selected);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to set DNS. Try running as Administrator.\nError: {ex.Message}");
                // Revert toggle
                _isInternalChange = true;
                ToggleBtnConnect.IsChecked = false;
                _isInternalChange = false;
            }
        }

        // -------------------- DISCONNECT --------------------
        private void ToggleBtnConnect_Unchecked(object sender, RoutedEventArgs e)
        {
            if (_isInternalChange) return;

            LogAction("Restoring default DNS...");
            try
            {
                DnsWindowsHelper.UnsetDns();
                _currentServer = null;
                UnsetUiState();
                CheckSelectedItem();
            }
            catch (Exception ex)
            {
                LogAction($"Error unsetting DNS: {ex.Message}");
            }
        }

        // -------------------- UI STATE --------------------
        private async void SetUiState(DnsServer server)
        {
            _isInternalChange = true;
            SetIcon(true);
            LabelStatus.Content = $"Connected to {server.Name}";
            NotifyIcon.ToolTipText = $"Connected to {server.Name}";
            TrayMenuBtnDisconnect.Visibility = Visibility.Visible;

            this.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF83FF96"));

            BtnDnsPrimaryLabel.Text = server.DnsAddress;
            BtnDnsSecondaryLabel.Text = server.DnsAddressAlt ?? "-";

            DataGridServers.SelectedItem = server;
            DataGridServers.ScrollIntoView(server);

            ToggleBtnConnect.Content = "Disconnect";
            ToggleBtnConnect.IsChecked = true;

            DataGridServers.IsEnabled = false;

            // Cancel previous ping loop if any
            _pingCts?.Cancel();
            _pingCts = new CancellationTokenSource();

            // Fire a ping to confirm connectivity
            await PingServerAsync(server, _pingCts.Token);

            _isInternalChange = false;
        }

        private void UnsetUiState()
        {
            _isInternalChange = true;

            LabelStatus.Content = "Default DNS";
            NotifyIcon.ToolTipText = $"Disconnected";
            TrayMenuBtnDisconnect.Visibility = Visibility.Collapsed;
            SetIcon(false);
            this.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF9883FF"));

            BtnDnsPrimaryLabel.Text = "-";
            BtnDnsSecondaryLabel.Text = "-";

            DataGridServers.SelectedItem = null;
            DataGridServers.IsEnabled = true;

            ToggleBtnConnect.Content = "Connect";
            ToggleBtnConnect.IsChecked = false;

            _isInternalChange = false;
        }

        // -------------------- SELECTION --------------------
        private void DataGridServers_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isInternalChange) return;
            CheckSelectedItem();
        }

        private void CheckSelectedItem()
        {
            var selected = DataGridServers.SelectedItem as DnsServer;
            PanelRight.IsEnabled = selected != null;

            if (selected != null)
            {
                // Optional: Log selection
                // LogAction($"Selected: {selected.Name}");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}