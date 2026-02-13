using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Windows.Networking.Connectivity;

namespace WifiAutoLogin.Services
{
    public class ConnectivityChecker
    {
        public async Task<bool> IsConnectedToInternetAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Trusted System Check
                var connectivityLevel = IsSystemPossiblyUnderCaptivePortal();
                LoggerService.Log($"System connectivity level check: {connectivityLevel}");
                
                if (connectivityLevel == NetworkConnectivityLevel.ConstrainedInternetAccess) 
                {
                    LoggerService.Log("System reported ConstrainedInternetAccess (Captive Portal detected). Proceeding to verify.");
                }
                
                if (connectivityLevel == NetworkConnectivityLevel.InternetAccess)
                {
                    LoggerService.Log("System reported InternetAccess. Proceeding with verification.");
                }

                cancellationToken.ThrowIfCancellationRequested();

                LoggerService.Log("Attempting connectivity check via http://connectivitycheck.platform.hicloud.com/generate_204...");
                
                // Use a handler that DOES NOT automatically follow redirects to detect 3xx status
                using var handler = new HttpClientHandler { AllowAutoRedirect = false };
                using var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(5) };

                try 
                {
                    var response = await client.GetAsync("http://connectivitycheck.platform.hicloud.com/generate_204", cancellationToken);
                    LoggerService.Log($"Connectivity check response: {response.StatusCode}");

                    if (response.StatusCode == System.Net.HttpStatusCode.NoContent) // 204
                    {
                        LoggerService.Log("Received 204 NoContent. Internet is connected.");
                        return true;
                    }
                    else if ((int)response.StatusCode >= 300 && (int)response.StatusCode < 400)
                    {
                        LoggerService.Log($"Received redirect status ({response.StatusCode}). Likely captive portal.");
                        return false;
                    }
                    else
                    {
                        LoggerService.Log($"Received unexpected status ({response.StatusCode}). Assuming no internet or captive portal.");
                        return false;
                    }
                }
                catch (HttpRequestException ex)
                {
                     LoggerService.Log($"HTTP request failed: {ex.Message}. Assuming no connectivity.");
                     return false;
                }
            }
            catch (OperationCanceledException)
            {
                throw;
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

        public async Task<string> DetectPortalUrlAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                // We use a handler that DOES NOT automatically follow redirects to capture the 302 location
                using var handler = new HttpClientHandler { AllowAutoRedirect = false };
                using var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(5) };
                
                // Microsoft's connect test URL is standard for this
                LoggerService.Log("Attempting to detect portal URL via msftconnecttest.com...");
                var response = await client.GetAsync("http://www.msftconnecttest.com/connecttest.txt", cancellationToken);
                LoggerService.Log($"Portal detection response: {response.StatusCode}");
                
                if (response.StatusCode == System.Net.HttpStatusCode.Redirect || 
                    response.StatusCode == System.Net.HttpStatusCode.Moved ||
                    response.StatusCode == System.Net.HttpStatusCode.Found)
                {
                    var location = response.Headers.Location?.ToString() ?? string.Empty;
                    LoggerService.Log($"Detected redirect to location: {location}");
                    return location;
                }
                
                LoggerService.Log("No redirect detected during portal URL check.");
                return string.Empty; // No redirect, might be online or blocked differently
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}
