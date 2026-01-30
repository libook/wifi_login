using System;
using System.IO;
using System.Text;

namespace WifiAutoLogin.Services
{
    public static class LoggerService
    {
        private static readonly string _logPath;
        private static bool _isEnabled;

        static LoggerService()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string logDir = Path.Combine(appData, "WifiAutoLogin");
            Directory.CreateDirectory(logDir);
            _logPath = Path.Combine(logDir, "logs.txt");
        }

        public static void Initialize(bool isEnabled)
        {
            _isEnabled = isEnabled;
        }

        public static void Log(string message)
        {
            if (!_isEnabled) return;
            WriteToFile($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [INFO] {message}");
        }

        public static void LogError(string message, Exception? ex = null)
        {
            if (!_isEnabled) return;
            var sb = new StringBuilder();
            sb.Append($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [ERROR] {message}");
            if (ex != null)
            {
                sb.Append($"\nException: {ex.Message}\nStackTrace: {ex.StackTrace}");
            }
            WriteToFile(sb.ToString());
        }

        private static void WriteToFile(string line)
        {
            try
            {
                lock (_logPath)
                {
                    File.AppendAllText(_logPath, line + Environment.NewLine);
                }
            }
            catch
            {
                // Ignore logging errors to prevent recursive failures
            }
        }
    }
}
