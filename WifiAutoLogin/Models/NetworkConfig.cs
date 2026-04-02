using System.Collections.Generic;

namespace WifiAutoLogin.Models
{
    public enum NotificationLevel
    {
        Default = 0,    // 禁用网络检测、已在线、自动登录的通知
        Maximum = 1,    // 不禁用任何通知
        Silent = 2      // 禁用所有通知
    }

    public class NetworkConfig
    {
        public string Ssid { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string EncryptedPassword { get; set; } = string.Empty;
        public string LoginUrl { get; set; } = string.Empty;
        public string UsernameSelector { get; set; } = string.Empty;
        public string PasswordSelector { get; set; } = string.Empty;
        public string LoginButtonSelector { get; set; } = string.Empty;
    }

    public class AppConfig
    {
        public bool AutoStart { get; set; } = true;
        public bool EnableLogging { get; set; } = false;
        public bool ShowBrowser { get; set; } = false;
        public int HeartbeatInterval { get; set; } = 60;
        public NotificationLevel NotificationLevel { get; set; } = NotificationLevel.Default;
        public List<NetworkConfig> Networks { get; set; } = new List<NetworkConfig>();
    }
}
