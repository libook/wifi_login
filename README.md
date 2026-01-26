# Windows Wi-Fi Auto-Login Assistant

这是一个轻量级的 Windows 11 托盘应用程序，旨在自动识别并完成 Wi-Fi Web 认证（Captive Portal），实现“无感联网”。

## 主要功能

- **后台监控**：自动监听网络连接状态变化。
- **配置管理**：支持多 SSID 配置，加密存储账号密码。
- **智能探测**：自动识别是否需要 Web 认证及登录页面 URL。
- **无感登录**：使用 Edge WebView2 内核后台自动填充并提交登录表单。
- **心跳维护**：定期检测连通性，掉线自动重连。

## 构建指南

### 前置条件

1. **.NET 8 SDK**: 确保已安装 [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)。
2. **WebView2 Runtime**: Windows 11 通常自带。如果未安装，请从 [Microsoft 官网](https://developer.microsoft.com/en-us/microsoft-edge/webview2/) 下载。

### 构建步骤

在项目根目录下运行以下命令：

```powershell
# 进入项目目录
cd WifiAutoLogin

# 还原依赖并构建
dotnet build
```

## 使用说明

1. **运行程序**:
   构建成功后，在 `WifiAutoLogin` 目录下执行：
   ```powershell
   dotnet run
   ```

2. **配置网络**:
   - 程序启动后将最小化至**系统托盘**。
   - 右键点击托盘图标，选择 **Settings**（或双击图标）。
   - 点击 **"+"** 按钮添加新配置。
   - 输入目标 Wi-Fi 的 **SSID**、**Username** 与 **Password**。
   - 点击 **Save Config** 保存。

3. **自动登录**:
   - 当系统连接到已配置的 SSID 时，程序会自动进行连通性检测。
   - 如果检测到存在 Captive Portal，将通过通知提醒用户正在执行自动登录。
   - 登录成功或失败均会有系统弹窗报告。

## 注意事项

- **安全性**: 密码使用 Windows DPAPI (ProtectedData) 进行加密，仅当前登录用户可解密，不会以明文形式存储在磁盘上。
- **自动启动**: 可以在设置界面勾选 "Run on Startup"。

## 技术栈

- **C# / .NET 8**
- **WPF** (界面管理)
- **WinForms** (托盘图标支持)
- **WebView2** (无头浏览器执行)
- **ManagedNativeWifi** (网络状态监控)
