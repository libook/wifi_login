using System;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using Windows.Networking.Connectivity;

namespace WifiAutoLogin.Services
{
    public class ConnectivityChecker
    {
        private static readonly HttpClient _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };

        public async Task<bool> IsConnectedToInternetAsync()
        {
            try
            {
                // Trusted System Check
                var connectivityLevel = IsSystemPossiblyUnderCaptivePortal();
                // If system explicitly says "Constrained", we are in captive portal -> Not fully connected
                if (connectivityLevel == NetworkConnectivityLevel.ConstrainedInternetAccess) return false;
                
                // If system says "InternetAccess", we trust it generally, but Double Check with Ping
                if (connectivityLevel == NetworkConnectivityLevel.InternetAccess)
                {
                     // Optional: Double check because sometimes OS is slow to update
                }

                // Try Ping First
                using var ping = new Ping();
                var reply = await ping.SendPingAsync("8.8.8.8", 2000);
                if (reply.Status == IPStatus.Success) return true;
                
                // Fallback to HTTP Head
                var response = await _httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Head, "http://www.baidu.com"));
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                LoggerService.LogError("Connectivity Check Failed", ex);
                return false;
            }
        }

        public NetworkConnectivityLevel IsSystemPossiblyUnderCaptivePortal()
        {
            try
            {
                var profile = NetworkInformation.GetInternetConnectionProfile();
                if (profile == null) return NetworkConnectivityLevel.None;
                
                return profile.GetNetworkConnectivityLevel();
            }
            catch (Exception ex)
            {
                LoggerService.LogError("WinRT Connectivity Check Failed", ex);
                return NetworkConnectivityLevel.None;
            }
        }

        public async Task<string> DetectPortalUrlAsync()
        {
            try
            {
                // We use a handler that DOES NOT automatically follow redirects to capture the 302 location
                using var handler = new HttpClientHandler { AllowAutoRedirect = false };
                using var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(5) };
                
                // Microsoft's connect test URL is standard for this
                var response = await client.GetAsync("http://www.msftconnecttest.com/connecttest.txt");
                
                if (response.StatusCode == System.Net.HttpStatusCode.Redirect || 
                    response.StatusCode == System.Net.HttpStatusCode.Moved ||
                    response.StatusCode == System.Net.HttpStatusCode.Found)
                {
                    return response.Headers.Location?.ToString() ?? string.Empty;
                }
                
                return string.Empty; // No redirect, might be online or blocked differently
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}
