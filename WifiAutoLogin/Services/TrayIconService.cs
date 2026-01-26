using System;
using System.Drawing;
using System.Windows.Forms;

namespace WifiAutoLogin.Services
{
    public class TrayIconService : IDisposable
    {
        private readonly NotifyIcon _notifyIcon;
        private readonly Action _onOpenSettings;
        private readonly Action _onExit;

        public TrayIconService(Action onOpenSettings, Action onExit)
        {
            _onOpenSettings = onOpenSettings;
            _onExit = onExit;

            _notifyIcon = new NotifyIcon
            {
                Icon = SystemIcons.Application,
                Visible = true,
                Text = "Wi-Fi Auto Login"
            };

            var contextMenu = new ContextMenuStrip();
            contextMenu.Items.Add("Settings", null, (s, e) => _onOpenSettings());
            contextMenu.Items.Add("Exit", null, (s, e) => _onExit());

            _notifyIcon.ContextMenuStrip = contextMenu;
            _notifyIcon.DoubleClick += (s, e) => _onOpenSettings();
        }

        public void ShowMessage(string title, string message, ToolTipIcon icon = ToolTipIcon.Info)
        {
            _notifyIcon.ShowBalloonTip(3000, title, message, icon);
        }

        public void SetStatus(bool isConnected, string text)
        {
            _notifyIcon.Text = $"Wi-Fi Auto Login: {text}";
            // In a real app, we would switch icons here (Gray/Yellow/Green)
        }

        public void Dispose()
        {
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
        }
    }
}
