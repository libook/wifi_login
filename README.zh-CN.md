# Windows Wi-Fi Auto-Login Assistant

这是一个轻量级的 Windows 11 托盘应用程序，旨在自动识别并完成 Wi-Fi Web 认证（Captive Portal），实现"无感联网"。

## 主要功能

- **后台监控**：自动监听网络连接状态变化
- **配置管理**：支持多 SSID 配置，加密存储账号密码
- **智能探测**：自动识别是否需要 Web 认证及登录页面 URL
- **无感登录**：使用 Edge WebView2 内核后台自动填充并提交登录表单
- **心跳维护**：定期检测连通性，掉线自动重连
- **国际化支持**：支持中文和英文界面切换
- **日志记录**：可选的日志记录功能，便于问题排查
- **开机自启**：支持开机自动启动

## 构建指南

### 前置条件

1. **.NET 8 SDK**: 确保已安装 [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
2. **WebView2 Runtime**: Windows 11 通常自带。如果未安装，请从 [Microsoft 官网](https://developer.microsoft.com/en-us/microsoft-edge/webview2/) 下载

### 构建步骤

在项目根目录下运行以下命令：

```powershell
# 还原依赖并构建
dotnet build

# 或者直接运行
dotnet run
```

## 使用说明

### 1. 启动程序

构建成功后，运行程序：
```powershell
dotnet run
```

程序启动后将最小化至**系统托盘**，托盘图标会显示当前网络连接状态。

### 2. 打开设置界面

- **方式一**：双击托盘图标
- **方式二**：右键点击托盘图标，选择 "Settings"

### 3. 配置网络

在设置界面中：

1. **添加网络配置**：
   - 点击左下角的 **"+"** 按钮
   - 填写以下信息：
     - **SSID**：Wi-Fi 网络名称（必填）
     - **Username**：登录用户名
     - **Password**：登录密码
     - **Login URL**（可选）：登录页面地址，留空则自动检测
     - **Username CSS Selector**（可选）：用户名输入框的 CSS 选择器
     - **Password CSS Selector**（可选）：密码输入框的 CSS 选择器
     - **Login Button CSS Selector**（可选）：登录按钮的 CSS 选择器
   - 点击 **"Save Config"** 保存配置

2. **编辑配置**：
   - 在左侧网络列表中选择要编辑的网络
   - 修改右侧表单中的信息
   - 点击 **"Save Config"** 保存

3. **删除配置**：
   - 在左侧网络列表中选择要删除的网络
   - 点击左下角的 **"-"** 按钮
   - 确认删除

4. **测试登录**：
   - 选择或填写网络配置信息
   - 点击 **"Test Login"** 测试登录是否成功

### 测试登录功能

测试登录功能允许你在不连接实际 Wi-Fi 的情况下测试配置是否正确。

#### 使用方法

1. **准备测试页面**：
   - 在项目根目录下创建 `test-page` 文件夹
   - 将登录页面的 HTML 文件（`.htm` 或 `.html`）放入该文件夹
   - 如果登录页面有相关资源文件（如图片、CSS、JS），也一并放入

   目录结构示例：
   ```
   wifi_login/
   ├── WifiAutoLogin/
   ├── test-page/
   │   ├── login.html          # 登录页面
   │   └── login_files/        # 相关资源文件（可选）
   └── README.md
   ```

2. **执行测试**：
   - 在设置界面填写网络配置信息
   - **Login URL** 字段留空或输入 `test`
   - 点击 **"Test Login"** 按钮
   - 程序会自动使用 `test-page` 文件夹中的第一个 HTML 文件进行测试

3. **查看结果**：
   - 测试成功：显示"登录测试成功"提示
   - 测试失败：显示"登录测试失败"提示
   - 如果启用了 **"Show Login Window (Debug)"**，可以看到登录过程的浏览器窗口

#### 注意事项

- 测试页面应该是真实的登录页面 HTML，包含用户名和密码输入框
- 如果测试失败，可能需要配置正确的 CSS 选择器
- 测试页面中的表单 action 地址可能需要修改为实际的服务器地址

### 4. 全局设置

在设置界面底部，可以配置以下全局选项：

- **Language**（语言）：切换界面语言（English / 中文）
- **Notification Level**（通知级别）：
  - **Default**（默认）：仅显示错误、成功等重要通知
  - **Maximum**（最大）：显示所有通知（包括网络检测、已在线等）
  - **Silent**（静默）：禁用所有通知
- **Show Login Window (Debug)**：显示登录窗口（调试模式）
- **Enable Log Recording**：启用日志记录
- **Run on Startup**：开机自动启动

### 5. 自动登录流程

当系统连接到已配置的 Wi-Fi 网络时：

1. 程序自动检测网络连接状态
2. 检查是否需要 Web 认证（Captive Portal）
3. 如果需要认证：
   - 自动检测或使用配置的登录 URL
   - 使用 WebView2 后台加载登录页面
   - 自动填充用户名和密码
   - 自动点击登录按钮
   - 验证登录是否成功
4. 通过系统通知提示登录结果

## 高级配置

### CSS 选择器配置

如果自动登录失败，可能需要手动配置 CSS 选择器：

1. 在浏览器中打开登录页面
2. 使用浏览器开发者工具（F12）检查页面元素
3. 找到用户名输入框、密码输入框和登录按钮
4. 右键点击元素，选择"复制" -> "复制选择器"
5. 将选择器粘贴到对应的配置项中

示例：
```
Username CSS Selector: #username
Password CSS Selector: #password
Login Button CSS Selector: button[type="submit"]
```

### 日志文件

启用日志记录后，日志文件保存在：
```
%APPDATA%\WifiAutoLogin\logs\
```

日志文件按日期命名，格式为：`log-YYYY-MM-DD.txt`

## 注意事项

### 安全性

- 密码使用 **Windows DPAPI (ProtectedData)** 进行加密
- 仅当前登录用户可解密
- 不会以明文形式存储在磁盘上

### 网络兼容性

- 适用于大多数使用 Web 表单认证的 Wi-Fi 网络
- 支持自动检测常见的 Captive Portal 页面
- 对于特殊认证页面，可能需要手动配置 CSS 选择器

### 性能优化

- 使用后台 WebView2 进程，不影响前台应用
- 智能检测机制，避免不必要的网络请求
- 支持并发控制，防止重复登录尝试

## 技术栈

- **C# / .NET 8**：主要开发语言和运行时
- **WPF**：用户界面框架
- **WinForms**：系统托盘图标支持
- **WebView2**：无头浏览器引擎，用于执行 Web 登录
- **ManagedNativeWifi**：Windows Wi-Fi API 封装，用于网络状态监控
- **DPAPI**：Windows 数据保护 API，用于密码加密

## 故障排除

### 常见问题

1. **程序无法启动**
   - 确认已安装 .NET 8 SDK
   - 确认已安装 WebView2 Runtime

2. **无法检测到登录页面**
   - 手动配置 Login URL
   - 检查网络是否正常连接

3. **自动登录失败**
   - 使用 Test Login 功能测试
   - 配置正确的 CSS 选择器
   - 启用日志记录查看详细错误信息

4. **密码保存后无法登录**
   - 检查用户名和密码是否正确
   - 某些网络可能需要特殊字符转义

5. **测试登录找不到测试页面**
   - 确认 `test-page` 文件夹位于项目根目录
   - 确认文件夹中有 `.htm` 或 `.html` 文件
   - Login URL 字段留空或输入 `test`

6. **测试登录失败但实际网络可以登录**
   - 测试页面的表单 action 可能指向错误的地址
   - 检查测试页面是否完整保存了所有资源
   - 尝试启用 "Show Login Window (Debug)" 查看详细过程

## 许可证

本项目采用 MIT 许可证 - 详见 [LICENSE](LICENSE) 文件。

MIT 许可证允许您：
- ✅ 将软件用于任何目的
- ✅ 修改软件
- ✅ 分发软件
- ✅ 私人使用
- ✅ 商业使用

唯一的要求是在软件的任何副本中包含原始版权和许可声明。
