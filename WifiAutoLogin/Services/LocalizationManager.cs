using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Threading;

namespace WifiAutoLogin.Services
{
    public class LocalizationManager : INotifyPropertyChanged
    {
        private static LocalizationManager? _instance;
        public static LocalizationManager Instance => _instance ??= new LocalizationManager();

        private readonly Dictionary<string, Dictionary<string, string>> _translations = new()
        {
            ["en-US"] = new Dictionary<string, string>
            {
                ["WindowTitle"] = "Wi-Fi Auto Login Assistant",
                ["HeaderTitle"] = "Network Configuration",
                ["SsidLabel"] = "SSID:",
                ["UsernameLabel"] = "Username:",
                ["PasswordLabel"] = "Password:",
                ["LoginUrlLabel"] = "Login URL (Optional):",
                ["UsernameSelectorLabel"] = "Username CSS Selector (Optional):",
                ["PasswordSelectorLabel"] = "Password CSS Selector (Optional):",
                ["LoginButtonSelectorLabel"] = "Login Button CSS Selector (Optional):",
                ["TestLoginButton"] = "Test Login",
                ["SaveConfigButton"] = "Save Config",
                ["NotificationLevelLabel"] = "Notification Level:",
                ["ShowBrowserCheckBox"] = "Show Login Window (Debug)",
                ["EnableLoggingCheckBox"] = "Enable Log Recording",
                ["AutoStartCheckBox"] = "Run on Startup",
                ["NotificationLevelDefault"] = "Default",
                ["NotificationLevelMaximum"] = "Maximum",
                ["NotificationLevelSilent"] = "Silent",
                ["SsidRequiredError"] = "SSID is required.",
                ["ErrorTitle"] = "Error",
                ["ConfigSavedSuccess"] = "Configuration Saved!",
                ["SuccessTitle"] = "Success",
                ["LoginTestSuccess"] = "Login Test Successful!",
                ["LoginTestFailed"] = "Login Test Failed.",
                ["RemoveConfirm"] = "Remove {0}?",
                ["ConfirmTitle"] = "Confirm",
                ["LanguageLabel"] = "Language:",
                ["LanguageEnglish"] = "English",
                ["LanguageChinese"] = "中文"
            },
            ["zh-CN"] = new Dictionary<string, string>
            {
                ["WindowTitle"] = "Wi-Fi 自动登录助手",
                ["HeaderTitle"] = "网络配置",
                ["SsidLabel"] = "SSID：",
                ["UsernameLabel"] = "用户名：",
                ["PasswordLabel"] = "密码：",
                ["LoginUrlLabel"] = "登录 URL（可选）：",
                ["UsernameSelectorLabel"] = "用户名 CSS 选择器（可选）：",
                ["PasswordSelectorLabel"] = "密码 CSS 选择器（可选）：",
                ["LoginButtonSelectorLabel"] = "登录按钮 CSS 选择器（可选）：",
                ["TestLoginButton"] = "测试登录",
                ["SaveConfigButton"] = "保存配置",
                ["NotificationLevelLabel"] = "通知级别：",
                ["ShowBrowserCheckBox"] = "显示登录窗口（调试）",
                ["EnableLoggingCheckBox"] = "启用日志记录",
                ["AutoStartCheckBox"] = "开机自启",
                ["NotificationLevelDefault"] = "默认",
                ["NotificationLevelMaximum"] = "最大",
                ["NotificationLevelSilent"] = "静默",
                ["SsidRequiredError"] = "SSID 是必填项。",
                ["ErrorTitle"] = "错误",
                ["ConfigSavedSuccess"] = "配置已保存！",
                ["SuccessTitle"] = "成功",
                ["LoginTestSuccess"] = "登录测试成功！",
                ["LoginTestFailed"] = "登录测试失败。",
                ["RemoveConfirm"] = "确定要删除 {0} 吗？",
                ["ConfirmTitle"] = "确认",
                ["LanguageLabel"] = "语言：",
                ["LanguageEnglish"] = "English",
                ["LanguageChinese"] = "中文"
            }
        };

        public event PropertyChangedEventHandler? PropertyChanged;

        public string this[string key]
        {
            get
            {
                var culture = CultureInfo.CurrentUICulture.Name;
                if (_translations.TryGetValue(culture, out var translations))
                {
                    if (translations.TryGetValue(key, out var value))
                    {
                        return value;
                    }
                }
                
                // Fallback to en-US
                if (_translations.TryGetValue("en-US", out var fallback))
                {
                    if (fallback.TryGetValue(key, out var value))
                    {
                        return value;
                    }
                }
                
                return key;
            }
        }

        public void Refresh()
        {
            OnPropertyChanged("Item[]");
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public static void ChangeLanguage(string cultureName)
        {
            var culture = new CultureInfo(cultureName);
            Thread.CurrentThread.CurrentUICulture = culture;
            Thread.CurrentThread.CurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;

            Instance.Refresh();
        }
    }
}
