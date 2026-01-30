using System.Collections.Generic;

namespace WifiAutoLogin.Models
{
    public class NetworkConfig
    {
        public string Ssid { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string EncryptedPassword { get; set; } = string.Empty;
        public string LoginUrl { get; set; } = string.Empty;
        public string UsernameSelector { get; set; } = string.Empty;
        public string PasswordSelector { get; set; } = string.Empty;
    }

    public class AppConfig
    {
        public bool AutoStart { get; set; } = true;
        public bool EnableLogging { get; set; } = false;
        public int HeartbeatInterval { get; set; } = 60;
        public List<NetworkConfig> Networks { get; set; } = new List<NetworkConfig>();
    }
}
