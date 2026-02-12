using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using WifiAutoLogin.Models;
using WifiAutoLogin.Services;

namespace WifiAutoLogin
{
    public partial class App : System.Windows.Application
    {
        private TrayIconService? _trayIcon;
        private MainWindow? _mainWindow;
        private NetworkMonitor? _networkMonitor;
        private readonly ConfigService _configService = new ConfigService();
        private readonly ConnectivityChecker _connectivityChecker = new ConnectivityChecker();
        private readonly LoginService _loginService = new LoginService();
        private DispatcherTimer? _heartbeatTimer;
        private readonly SemaphoreSlim _networkLock = new SemaphoreSlim(1, 1);

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Initialize Logger
            LoggerService.Initialize(_configService.CurrentConfig.EnableLogging);
            LoggerService.Log("Application Started");

            // Initialize Tray Icon
            _trayIcon = new TrayIconService(OpenSettings, ExitApplication);
            
            // Start Network Monitoring
            _networkMonitor = new NetworkMonitor();
            _networkMonitor.OnWifiConnected += OnWifiConnected;
            _networkMonitor.OnDisconnected += OnDisconnected;

            // Heartbeat Timer
            _heartbeatTimer = new DispatcherTimer();
            _heartbeatTimer.Interval = TimeSpan.FromSeconds(_configService.CurrentConfig.HeartbeatInterval);
            _heartbeatTimer.Tick += HeartbeatTimer_Tick;

            // Initial Check (if already connected)
            Task.Run(() => 
            {
                var ssid = _networkMonitor.GetConnectedSsid();
                if (!string.IsNullOrEmpty(ssid)) OnWifiConnected(ssid);
            });

            // If auto-start enabled and we are here, we are good. 
            // If user manually started, maybe show settings? 
            // For now, let's keep it quiet unless configured otherwise or manually opened.
        }

        private async void OnWifiConnected(string ssid)
        {
            await _networkLock.WaitAsync();
            try
            {
                _trayIcon?.SetStatus(true, $"Connected to {ssid}");

                var config = _configService.CurrentConfig.Networks.FirstOrDefault(n => n.Ssid == ssid);
                if (config == null) return; // Not a managed network

                _trayIcon?.ShowMessage("Network Detected", $"Checking connection for {ssid}...", System.Windows.Forms.ToolTipIcon.Info);

                bool isOnline = await _connectivityChecker.IsConnectedToInternetAsync();
                var connectivityLevel = _connectivityChecker.IsSystemPossiblyUnderCaptivePortal();
                LoggerService.Log($"Network detected: {ssid}. Online Status: {isOnline}, System Status: {connectivityLevel}");

                if (isOnline && connectivityLevel != global::Windows.Networking.Connectivity.NetworkConnectivityLevel.ConstrainedInternetAccess)
                {
                    _trayIcon?.ShowMessage("Online", $"{ssid} is already online.", System.Windows.Forms.ToolTipIcon.Info);
                    LoggerService.Log($"{ssid} is already online. Starting Heartbeat.");
                    StartHeartbeat();
                    return;
                }

                // Not online or Captive Portal detected, try login
                _trayIcon?.ShowMessage("Auto Login", $"Attempting to login to {ssid}...", System.Windows.Forms.ToolTipIcon.Info);
                LoggerService.Log($"Attempting login for {ssid}...");

                // Detect URL if needed
                string targetUrl = config.LoginUrl;
                if (string.IsNullOrEmpty(targetUrl))
                {
                    targetUrl = await _connectivityChecker.DetectPortalUrlAsync();
                }

                if (string.IsNullOrEmpty(targetUrl))
                {
                    _trayIcon?.ShowMessage("Error", "Could not detect login URL.", System.Windows.Forms.ToolTipIcon.Error);
                    return;
                }

                bool result = await _loginService.PerformLoginAsync(config, targetUrl);
                LoggerService.Log($"Login attempt finished. Result: {result}");

                if (result)
                {
                    // Double check
                    if (await _connectivityChecker.IsConnectedToInternetAsync())
                    {
                        _trayIcon?.ShowMessage("Success", $"Successfully logged in to {ssid}!", System.Windows.Forms.ToolTipIcon.Info);
                        StartHeartbeat();
                    }
                    else
                    {
                        _trayIcon?.ShowMessage("Failed", "Login appeared successful but still offline.", System.Windows.Forms.ToolTipIcon.Warning);
                    }
                }
                else
                {
                    _trayIcon?.ShowMessage("Failed", "Auto login failed.", System.Windows.Forms.ToolTipIcon.Error);
                    LoggerService.Log("Auto login failed.");
                }
            }
            finally
            {
                _networkLock.Release();
            }
        }

        private void OnDisconnected()
        {
            _trayIcon?.SetStatus(false, "Disconnected");
            LoggerService.Log("Network Disconnected");
            StopHeartbeat();
        }

        private void StartHeartbeat()
        {
            if (!_heartbeatTimer!.IsEnabled)
                _heartbeatTimer.Start();
        }

        private void StopHeartbeat()
        {
            _heartbeatTimer?.Stop();
        }

        private async void HeartbeatTimer_Tick(object? sender, EventArgs e)
        {
             bool isOnline = await _connectivityChecker.IsConnectedToInternetAsync();
             if (!isOnline)
             {
                 // Lost connection, retry login?
                 var ssid = _networkMonitor?.GetConnectedSsid();
                 if (!string.IsNullOrEmpty(ssid))
                 {
                     OnWifiConnected(ssid); // Re-trigger logic
                 }
             }
        }

        private void OpenSettings()
        {
            if (_mainWindow == null || !_mainWindow.IsLoaded)
            {
                _mainWindow = new MainWindow();
                _mainWindow.Closed += (s, e) => _mainWindow = null;
                _mainWindow.Show();
            }
            else
            {
                _mainWindow.Activate();
                if (_mainWindow.WindowState == WindowState.Minimized)
                    _mainWindow.WindowState = WindowState.Normal;
            }
        }

        private void ExitApplication()
        {
            _trayIcon?.Dispose();
            Shutdown();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _trayIcon?.Dispose();
            base.OnExit(e);
        }
    }
}
