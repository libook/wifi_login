using System.Windows;
using System.Windows.Controls;
using WifiAutoLogin.Models;
using WifiAutoLogin.Services;

namespace WifiAutoLogin
{
    public partial class MainWindow : Window
    {
        private readonly ConfigService _configService;
        private NetworkConfig? _selectedConfig;

        public MainWindow()
        {
            InitializeComponent();
            _configService = new ConfigService();
            LoadData();
        }

        private void LoadData()
        {
            NetworksList.ItemsSource = null;
            NetworksList.ItemsSource = _configService.CurrentConfig.Networks;
            NetworksList.ItemsSource = _configService.CurrentConfig.Networks;
            ChkAutoStart.IsChecked = _configService.CurrentConfig.AutoStart;
            ChkEnableLogging.IsChecked = _configService.CurrentConfig.EnableLogging;
            ChkShowBrowser.IsChecked = _configService.CurrentConfig.ShowBrowser;
        }

        private void NetworksList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (NetworksList.SelectedItem is NetworkConfig config)
            {
                _selectedConfig = config;
                TxtSsid.Text = config.Ssid;
                TxtUsername.Text = config.Username;
                TxtLoginUrl.Text = config.LoginUrl;
                TxtUsernameSelector.Text = config.UsernameSelector;
                TxtPasswordSelector.Text = config.PasswordSelector;
                TxtLoginButtonSelector.Text = config.LoginButtonSelector;
                TxtPassword.Password = _configService.DecryptPassword(config.EncryptedPassword);
                TxtSsid.IsReadOnly = true; 
            }
            else
            {
                _selectedConfig = null;
                ClearForm();
            }
        }

        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            _selectedConfig = null;
            NetworksList.SelectedItem = null;
            ClearForm();
            TxtSsid.Focus();
        }

        private void ClearForm()
        {
            TxtSsid.Text = "";
            TxtUsername.Text = "";
            TxtPassword.Password = "";
            TxtLoginUrl.Text = "";
            TxtUsernameSelector.Text = "";
            TxtPasswordSelector.Text = "";
            TxtLoginButtonSelector.Text = "";
            TxtSsid.IsReadOnly = false;
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            string ssid = TxtSsid.Text.Trim();
            if (string.IsNullOrEmpty(ssid))
            {
                System.Windows.MessageBox.Show("SSID is required.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (_selectedConfig == null)
            {
                // New Config
                var newConfig = new NetworkConfig
                {
                    Ssid = ssid,
                    Username = TxtUsername.Text.Trim(),
                    EncryptedPassword = _configService.EncryptPassword(TxtPassword.Password),
                    LoginUrl = TxtLoginUrl.Text.Trim(),
                    UsernameSelector = TxtUsernameSelector.Text.Trim(),
                    PasswordSelector = TxtPasswordSelector.Text.Trim(),
                    LoginButtonSelector = TxtLoginButtonSelector.Text.Trim()
                };
                _configService.CurrentConfig.Networks.Add(newConfig);
                _selectedConfig = newConfig; // Select it
            }
            else
            {
                // Update Existing
                _selectedConfig.Ssid = ssid;
                _selectedConfig.Username = TxtUsername.Text.Trim();
                _selectedConfig.EncryptedPassword = _configService.EncryptPassword(TxtPassword.Password);
                _selectedConfig.LoginUrl = TxtLoginUrl.Text.Trim();
                _selectedConfig.UsernameSelector = TxtUsernameSelector.Text.Trim();
                _selectedConfig.PasswordSelector = TxtPasswordSelector.Text.Trim();
                _selectedConfig.LoginButtonSelector = TxtLoginButtonSelector.Text.Trim();
            }

            _configService.SaveConfig();
            LoadData();
            // Reselect
            NetworksList.SelectedItem = _selectedConfig;
            System.Windows.MessageBox.Show("Configuration Saved!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private async void BtnTest_Click(object sender, RoutedEventArgs e)
        {
            var config = new NetworkConfig
            {
                Ssid = TxtSsid.Text.Trim(),
                Username = TxtUsername.Text.Trim(),
                EncryptedPassword = _configService.EncryptPassword(TxtPassword.Password),
                LoginUrl = TxtLoginUrl.Text.Trim(),
                UsernameSelector = TxtUsernameSelector.Text.Trim(),
                PasswordSelector = TxtPasswordSelector.Text.Trim(),
                LoginButtonSelector = TxtLoginButtonSelector.Text.Trim()
            };

            // Auto-detect test page if URL is empty or "test"
            if (string.IsNullOrEmpty(config.LoginUrl) || config.LoginUrl.ToLower() == "test")
            {
                string testPageDir = @"c:\Users\liboo\Documents\Lab\wifi_login\test-page";
                if (System.IO.Directory.Exists(testPageDir))
                {
                    var files = System.IO.Directory.GetFiles(testPageDir, "*.*")
                        .Where(s => s.EndsWith(".htm", StringComparison.OrdinalIgnoreCase) || s.EndsWith(".html", StringComparison.OrdinalIgnoreCase))
                        .ToArray();

                    if (files.Length > 0)
                    {
                        config.LoginUrl = files[0];
                    }
                    else
                    {
                        System.Windows.MessageBox.Show($"No .htm or .html files found in: {testPageDir}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                }
                else
                {
                    System.Windows.MessageBox.Show($"Test page directory not found at: {testPageDir}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }

            BtnTest.IsEnabled = false;
            try
            {
                var loginService = new LoginService();
                bool success = await loginService.PerformLoginAsync(config, config.LoginUrl);

                if (success)
                {
                    System.Windows.MessageBox.Show("Login Test Successful!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    System.Windows.MessageBox.Show("Login Test Failed.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            finally
            {
                BtnTest.IsEnabled = true;
            }
        }


        private void BtnRemove_Click(object sender, RoutedEventArgs e)
        {
            if (NetworksList.SelectedItem is NetworkConfig config)
            {
                if (System.Windows.MessageBox.Show($"Remove {config.Ssid}?", "Confirm", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    _configService.CurrentConfig.Networks.Remove(config);
                    _configService.SaveConfig();
                    LoadData();
                    ClearForm();
                }
            }
        }

        private void ChkAutoStart_Changed(object sender, RoutedEventArgs e)
        {
            if (_configService == null) return;
            _configService.CurrentConfig.AutoStart = ChkAutoStart.IsChecked ?? false;
            _configService.SaveConfig();
            // TODO: Implement registry key toggle for actual auto-start
        }

        private void ChkEnableLogging_Changed(object sender, RoutedEventArgs e)
        {
            if (_configService == null) return;
            bool isEnabled = ChkEnableLogging.IsChecked ?? false;
            _configService.CurrentConfig.EnableLogging = isEnabled;
            _configService.SaveConfig();
            LoggerService.Initialize(isEnabled);
        }

        private void ChkShowBrowser_Changed(object sender, RoutedEventArgs e)
        {
            if (_configService == null) return;
            _configService.CurrentConfig.ShowBrowser = ChkShowBrowser.IsChecked ?? false;
            _configService.SaveConfig();
        }
    }
}