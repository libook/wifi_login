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
