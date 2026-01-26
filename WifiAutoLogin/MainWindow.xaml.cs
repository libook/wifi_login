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
            ChkAutoStart.IsChecked = _configService.CurrentConfig.AutoStart;
        }

        private void NetworksList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (NetworksList.SelectedItem is NetworkConfig config)
            {
                _selectedConfig = config;
                TxtSsid.Text = config.Ssid;
                TxtUsername.Text = config.Username;
                TxtLoginUrl.Text = config.LoginUrl;
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
                    LoginUrl = TxtLoginUrl.Text.Trim()
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
            }

            _configService.SaveConfig();
            LoadData();
            // Reselect
            NetworksList.SelectedItem = _selectedConfig;
            System.Windows.MessageBox.Show("Configuration Saved!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
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
    }
}