using System.Configuration;
using System.Data;
using System.Windows;
using System;
using System.Threading;

namespace Halun.HalunDns.Windows
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private const string EventName = "Global\\HalunDns_ShowWindowEvent";
        private const string MutexName = "Global\\HalunDns_Singleton_Mutex";
        private Mutex _mutex;
        private EventWaitHandle _signalEvent;

        protected override void OnStartup(StartupEventArgs e)
        {
            bool createdNew;
            _mutex = new Mutex(true, MutexName, out createdNew);

            if (!createdNew)
            {
                try
                {
                    // Signal existing instance to show its window (if it's in tray)
                    using (var existing = EventWaitHandle.OpenExisting(EventName))
                    {
                        existing.Set();
                    }
                }
                catch (WaitHandleCannotBeOpenedException)
                {
                    // Existing instance may not have created the event yet; ignore.
                }

                // Shutdown this second instance
                Shutdown();
                return;
            }

            // Create an event that other instances can open to request showing the window
            _signalEvent = new EventWaitHandle(false, EventResetMode.AutoReset, EventName);

            // Register a wait so when a second instance signals, we show/activate the main window
            ThreadPool.RegisterWaitForSingleObject(_signalEvent, (state, timedOut) =>
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    var main = Current.MainWindow as MainWindow;
                    if (main == null)
                    {
                        main = new MainWindow();
                        Current.MainWindow = main;
                    }

                    if (!main.IsVisible) main.Show();
                    if (main.WindowState == WindowState.Minimized) main.WindowState = WindowState.Normal;
                    main.Activate();
                }));
            }, null, -1, false);

            // If StartupUri created a window already, keep it; otherwise create and show MainWindow
            if (Current.MainWindow == null)
            {
                var win = new MainWindow();
                Current.MainWindow = win;
                win.Show();
            }

            base.OnStartup(e);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            try
            {
                _signalEvent?.Close();
                if (_mutex != null)
                {
                    _mutex.ReleaseMutex();
                    _mutex.Dispose();
                }
            }
            catch
            {
                // ignore
            }

            base.OnExit(e);
        }
    }

}
