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

        
        // Concurrency control
        private readonly SemaphoreSlim _connectionLock = new SemaphoreSlim(1, 1);
        private CancellationTokenSource? _loginCts;
        private Task? _currentLoginTask;

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



            // Initial Check (if already connected)
            Task.Run(() => 
            {
                var ssid = _networkMonitor.GetConnectedSsid();
                if (!string.IsNullOrEmpty(ssid)) OnWifiConnected(ssid);
            });
        }

        private async void OnWifiConnected(string ssid)
        {
            try
            {
                LoggerService.Log($"OnWifiConnected triggered for SSID: {ssid}. Waiting for connection lock...");
                await _connectionLock.WaitAsync();
                LoggerService.Log("Connection lock acquired.");
                
                // 1. Stop current process
                if (_loginCts != null)
                {
                    LoggerService.Log("Cancelling existing login process...");
                    _loginCts.Cancel();
                }

                if (_currentLoginTask != null && !_currentLoginTask.IsCompleted)
                {
                    try
                    {
                        LoggerService.Log("Waiting for previous login task to finish...");
                        await _currentLoginTask;
                    }
                    catch (Exception) 
                    {
                        // Ignore cancellation/errors from previous task
                    }
                }
                
                // 2. Cleanup resources
                _loginCts?.Dispose();
                _loginCts = new CancellationTokenSource();
                var token = _loginCts.Token;

                // 3. Start new process
                _currentLoginTask = RunLoginFlowAsync(ssid, token);
            }
            catch (Exception ex)
            {
                LoggerService.LogError("Error in OnWifiConnected orchestration", ex);
            }
            finally
            {
                _connectionLock.Release();
            }
        }

        private async Task RunLoginFlowAsync(string ssid, CancellationToken token)
        {
            try
            {
                // UI updates via Dispatcher if needed, but we are on UI thread context usually 
                // (except logic in RunLoginFlowAsync continues on thread pool potentially)
                // However, TrayIconService methods handle UI marshalling or are thread-safe?
                // Looking at TrayIconService (not shown), usually NotifyIcon is thread-specific.
                // We should ensure UI updates happen on UI thread if specific elements are touched.
                // But generally safe if TrayIconService handles it.
                // Assuming TrayIconService uses SynchronizationContext or Invoke.
                
                // We need to be careful: async void OnWifiConnected -> WaitAsync -> RunLoginFlowAsync.
                // The _connectionLock protects the START of the flow.
                
                // Basic UI Update
                await Dispatcher.InvokeAsync(() => 
                    _trayIcon?.SetStatus(true, $"Connected to {ssid}"));

                var config = _configService.CurrentConfig.Networks.FirstOrDefault(n => n.Ssid == ssid);
                if (config == null) return; // Not a managed network

                await Dispatcher.InvokeAsync(() => 
                    _trayIcon?.ShowMessage("Network Detected", $"Checking connection for {ssid}...", System.Windows.Forms.ToolTipIcon.Info));

                bool isOnline = await _connectivityChecker.IsConnectedToInternetAsync(token);
                var connectivityLevel = _connectivityChecker.IsSystemPossiblyUnderCaptivePortal();
                LoggerService.Log($"Network detected: {ssid}. Online Status: {isOnline}, System Status: {connectivityLevel}");

                if (token.IsCancellationRequested) return;

                if (isOnline)
                {
                    if (connectivityLevel == global::Windows.Networking.Connectivity.NetworkConnectivityLevel.ConstrainedInternetAccess)
                    {
                        LoggerService.Log("Network appears Constrained by OS, but active check confirmed Online. Trusting active check.");
                    }

                    await Dispatcher.InvokeAsync(() => 
                        _trayIcon?.ShowMessage("Online", $"{ssid} is already online.", System.Windows.Forms.ToolTipIcon.Info));

                    return;
                }

                // Not online or Captive Portal detected, try login
                await Dispatcher.InvokeAsync(() => 
                    _trayIcon?.ShowMessage("Auto Login", $"Attempting to login to {ssid}...", System.Windows.Forms.ToolTipIcon.Info));
                LoggerService.Log($"Attempting login for {ssid}...");

                // Detect URL if needed
                string targetUrl = config.LoginUrl;
                if (string.IsNullOrEmpty(targetUrl))
                {
                    LoggerService.Log("Configured LoginUrl is empty. Attempting to detect portal URL...");
                    targetUrl = await _connectivityChecker.DetectPortalUrlAsync(token);
                }
                else
                {
                    LoggerService.Log($"Using configured LoginUrl: {targetUrl}");
                }

                if (string.IsNullOrEmpty(targetUrl))
                {
                    if (!token.IsCancellationRequested)
                    {
                        LoggerService.Log("Failed to detect portal URL, and no URL provided in config.");
                        await Dispatcher.InvokeAsync(() => 
                            _trayIcon?.ShowMessage("Error", "Could not detect login URL.", System.Windows.Forms.ToolTipIcon.Error));
                    }
                    return;
                }

                bool result = await _loginService.PerformLoginAsync(config, targetUrl, token);
                LoggerService.Log($"Login attempt finished. Result: {result}");

                if (token.IsCancellationRequested) return;

                if (result)
                {
                    LoggerService.Log("Login service reported success. Verifying internet connection...");
                    // Double check
                    if (await _connectivityChecker.IsConnectedToInternetAsync(token))
                    {
                        LoggerService.Log("Internet connection verified. Login successful.");
                        await Dispatcher.InvokeAsync(() => 
                            _trayIcon?.ShowMessage("Success", $"Successfully logged in to {ssid}!", System.Windows.Forms.ToolTipIcon.Info));

                    }
                    else
                    {
                        LoggerService.Log("Internet connection check failed after login.");
                        await Dispatcher.InvokeAsync(() => 
                            _trayIcon?.ShowMessage("Failed", "Login appeared successful but still offline.", System.Windows.Forms.ToolTipIcon.Warning));
                    }
                }
                else
                {
                    if (!token.IsCancellationRequested)
                    {
                        LoggerService.Log("Login service reported failure.");
                        await Dispatcher.InvokeAsync(() => 
                             _trayIcon?.ShowMessage("Failed", "Auto login failed.", System.Windows.Forms.ToolTipIcon.Error));
                    }
                }
            }
            catch (OperationCanceledException)
            {
                LoggerService.Log($"Login flow for {ssid} was cancelled.");
            }
            catch (Exception ex)
            {
                LoggerService.LogError($"Error in login flow for {ssid}", ex);
            }
        }

        private async void OnDisconnected()
        {
            try
            {
                await _connectionLock.WaitAsync();

                _loginCts?.Cancel();
                if (_currentLoginTask != null && !_currentLoginTask.IsCompleted)
                {
                    try { await _currentLoginTask; } catch {}
                }

                await Dispatcher.InvokeAsync(() => 
                    _trayIcon?.SetStatus(false, "Disconnected"));
                LoggerService.Log("Network Disconnected");

            }
            catch (Exception ex)
            {
                LoggerService.LogError("Error in OnDisconnected", ex);
            }
            finally
            {
                _connectionLock.Release();
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
            _loginCts?.Cancel();
            _trayIcon?.Dispose();
            base.OnExit(e);
        }
    }
}
