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

        private async void NetworkChange_NetworkAddressChanged(object? sender, EventArgs e)
        {
            // Give it a moment to stabilize
            await Task.Delay(2000);

            var ssid = GetConnectedSsid();
            if (!string.IsNullOrEmpty(ssid))
            {
                OnWifiConnected?.Invoke(ssid);
            }
            else
            {
                OnDisconnected?.Invoke();
            }
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
