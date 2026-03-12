using System;
using Microsoft.Win32;
using WifiAutoLogin.Services;

namespace WifiAutoLogin.Services
{
    public static class StartupService
    {
        private const string AppName = "WifiAutoLogin";
        private const string RunRegistryKey = @"Software\Microsoft\Windows\CurrentVersion\Run";

        public static void SetAutoStart(bool enable)
        {
            try
            {
                using (RegistryKey? key = Registry.CurrentUser.OpenSubKey(RunRegistryKey, true))
                {
                    if (key == null)
                    {
                        LoggerService.LogError("Could not open registry key for Run", new Exception("OpenSubKey returned null"));
                        return;
                    }

                    if (enable)
                    {
                        string? appPath = Environment.ProcessPath;
                        if (string.IsNullOrEmpty(appPath))
                        {
                            LoggerService.LogError("Could not determine application process path", new Exception("ProcessPath is null or empty"));
                            return;
                        }

                        key.SetValue(AppName, $"\"{appPath}\"");
                        LoggerService.Log($"Auto-start enabled. Registry key set for path: {appPath}");
                    }
                    else
                    {
                        key.DeleteValue(AppName, false);
                        LoggerService.Log("Auto-start disabled. Registry key removed.");
                    }
                }
            }
            catch (Exception ex)
            {
                LoggerService.LogError("Failed to update auto-start registry key", ex);
            }
        }
    }
}
