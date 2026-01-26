using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using WifiAutoLogin.Models;

namespace WifiAutoLogin.Services
{
    public class ConfigService
    {
        private readonly string _configPath;
        public AppConfig CurrentConfig { get; private set; }

        public ConfigService()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string configDir = Path.Combine(appData, "WifiAutoLogin");
            Directory.CreateDirectory(configDir);
            _configPath = Path.Combine(configDir, "config.json");
            
            LoadConfig();
        }

        public void LoadConfig()
        {
            if (File.Exists(_configPath))
            {
                try
                {
                    string json = File.ReadAllText(_configPath);
                    CurrentConfig = JsonSerializer.Deserialize<AppConfig>(json) ?? new AppConfig();
                }
                catch
                {
                    CurrentConfig = new AppConfig();
                }
            }
            else
            {
                CurrentConfig = new AppConfig();
                SaveConfig();
            }
        }

        public void SaveConfig()
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            string json = JsonSerializer.Serialize(CurrentConfig, options);
            File.WriteAllText(_configPath, json);
        }

        public string EncryptPassword(string password)
        {
            if (string.IsNullOrEmpty(password)) return string.Empty;
            byte[] data = Encoding.UTF8.GetBytes(password);
            byte[] encrypted = ProtectedData.Protect(data, null, DataProtectionScope.CurrentUser);
            return Convert.ToBase64String(encrypted);
        }

        public string DecryptPassword(string encryptedPassword)
        {
            if (string.IsNullOrEmpty(encryptedPassword)) return string.Empty;
            try
            {
                byte[] data = Convert.FromBase64String(encryptedPassword);
                byte[] decrypted = ProtectedData.Unprotect(data, null, DataProtectionScope.CurrentUser);
                return Encoding.UTF8.GetString(decrypted);
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}
