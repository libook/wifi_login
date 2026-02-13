using System;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using ManagedNativeWifi;

namespace WifiAutoLogin.Services
{
    public class NetworkMonitor
    {
        public event Action<string>? OnWifiConnected;
        public event Action? OnDisconnected;

        public NetworkMonitor()
        {
            NetworkChange.NetworkAddressChanged += NetworkChange_NetworkAddressChanged;
        }

        private CancellationTokenSource? _debounceCts;
        private readonly object _lock = new object();

        private void NetworkChange_NetworkAddressChanged(object? sender, EventArgs e)
        {
            lock (_lock)
            {
                _debounceCts?.Cancel();
                _debounceCts = new CancellationTokenSource();
            }

            var token = _debounceCts.Token;

            Task.Run(async () =>
            {
                try
                {
                    LoggerService.Log("Network address change detected. Waiting for stabilization...");
                    // Give it a moment to stabilize
                    await Task.Delay(2000, token);

                    if (token.IsCancellationRequested) return;

                    var ssid = GetConnectedSsid();
                    if (!string.IsNullOrEmpty(ssid))
                    {
                        LoggerService.Log($"Connected to SSID: {ssid}. Triggering OnWifiConnected.");
                        // Invoke on UI thread handling if necessary, but Action is usually fine here
                        OnWifiConnected?.Invoke(ssid);
                    }
                    else
                    {
                        LoggerService.Log("No connected SSID found. Triggering OnDisconnected.");
                        OnDisconnected?.Invoke();
                    }
                }
                catch (TaskCanceledException)
                {
                    // Ignore
                }
                catch (Exception ex)
                {
                    LoggerService.LogError("Error in NetworkMonitor", ex);
                }
            }, token);
        }

        public string GetConnectedSsid()
        {
            try
            {
                var connectedNetwork = NativeWifi.EnumerateConnectedNetworkSsids().FirstOrDefault();
                if (connectedNetwork == null) return string.Empty;
                return connectedNetwork.ToString();
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}
