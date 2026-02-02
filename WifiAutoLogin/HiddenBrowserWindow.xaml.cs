using System;
using System.Threading.Tasks;
using System.Windows;

namespace WifiAutoLogin
{
    public partial class HiddenBrowserWindow : Window
    {
        public Microsoft.Web.WebView2.Wpf.WebView2 WebView => webView;

        public HiddenBrowserWindow()
        {
            InitializeComponent();
            _ = InitializeWebViewAsync();
        }

        public void SetDebugMode(bool debug)
        {
            if (debug)
            {
                this.Opacity = 1.0;
                this.WindowStyle = WindowStyle.SingleBorderWindow;
                this.ShowInTaskbar = true;
                this.Left = (SystemParameters.PrimaryScreenWidth - this.Width) / 2;
                this.Top = (SystemParameters.PrimaryScreenHeight - this.Height) / 2;
                this.ResizeMode = ResizeMode.CanResize;
                this.AllowsTransparency = false;
                this.Background = System.Windows.SystemColors.WindowBrush;
                this.Title = "Wi-Fi Login Debug View";
            }
        }

        private async Task InitializeWebViewAsync()
        {
            try
            {
                await webView.EnsureCoreWebView2Async();
                
                // Clear cookies to ensure fresh login if needed
                webView.CoreWebView2.CookieManager.DeleteAllCookies();
            }
            catch (Exception ex)
            {
                // Handle initialization failure
                Console.WriteLine($"WebView2 Init Failed: {ex.Message}");
            }
        }
    }
}
