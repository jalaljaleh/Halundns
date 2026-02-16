using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

namespace Halun.HalunDns.Windows
{
    public partial class MainWindow : Window
    {
        private readonly MainWindowViewModel _viewModel;

        public MainWindow()
        {
            InitializeComponent(); //
            _viewModel = new MainWindowViewModel();
            DataContext = _viewModel; //

            // Trigger loading routine via Event or Command. We'll execute the async command on load.
            Loaded += (s, e) =>
            {
                if (_viewModel.LoadDataCommand.CanExecute(null))
                    _viewModel.LoadDataCommand.Execute(null);
            };

            // UI-Only event subscriptions
            _viewModel.RequestScrollToBottom += (s, e) => ScrollerLogger.ScrollToBottom(); //
            _viewModel.RequestWindowStateChange += (s, state) =>
            {
                this.Show();
                this.WindowState = state; //
                if (state == WindowState.Normal) this.Activate(); //
            };
        }
        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            var handle = new WindowInteropHelper(this).Handle;

            // Enable Mica effect (Windows 11)
            int trueValue = 0x01;
            int micaAttribute = 38; // DWMWA_MICA_EFFECT
            DwmSetWindowAttribute(handle, micaAttribute, ref trueValue, Marshal.SizeOf(typeof(int)));

            // Enable dark theme for the window chrome (title bar area)
            int darkThemeAttribute = 20; // DWMWA_USE_IMMERSIVE_DARK_MODE
            DwmSetWindowAttribute(handle, darkThemeAttribute, ref trueValue, Marshal.SizeOf(typeof(int)));
        }

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);
        private void TitleBorder_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove(); //
            }
        }
        public void Close_Click(object sender, RoutedEventArgs e)
        {
            NotifyIcon.ShowBalloonTip("HalunDns", "App is still running in the system tray.", this.NotifyIcon.Icon); //
            this.Close();
        }
        protected override void OnClosing(CancelEventArgs e)
        {
            e.Cancel = true; //
            this.Hide(); // Send to tray
        }

        private void NotifyIcon_TrayLeftMouseUp(object sender, RoutedEventArgs e)
        {
            if (this.IsVisible) this.Hide(); //
            else
            {
                this.Show();
                this.WindowState = WindowState.Normal; //
                this.Activate(); //
            }
        }
    }
}