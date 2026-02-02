# 产品需求设计文档 (PRD)

**项目名称：** Windows Wi-Fi 自动认证助手 (Wi-Fi Auto-Login Assistant)
**运行环境：** Windows 11 (Desktop)
**架构模式：** 托盘应用程序 (Tray Application)

## 1. 项目概述

本项目旨在开发一款运行在 Windows 11 系统托盘的轻量级工具。该工具能自动监测系统的 Wi-Fi 连接状态，当系统连接到用户配置列表中的特定 SSID 时，自动识别是否需要 Web 认证，并后台模拟操作完成登录，实现“无感联网”。

## 2. 功能角色

* **用户**：配置 SSID 和对应的账号密码，查看运行日志。
* **系统**：后台自动运行，执行网络检测和登录逻辑。

## 3. 详细功能需求

### 3.1 配置管理 (Configuration)

用户界面应提供一个管理面板，允许用户对网络配置进行增删改查。

* **多环境支持**：支持配置多条记录（例如：学校图书馆、公司内网、公寓网络）。
* **字段定义**：
* `SSID` (Wi-Fi名称，必填，唯一键)
* `Username` (登录用户名，必填)
    * `Password` (登录密码，必填，需本地加密存储)
    * `Login URL` (选填，若不填则通过自动探测获取)
    * `Username Selector` (选填，用户名输入框的 CSS 选择器)
    * `Password Selector` (选填，密码输入框的 CSS 选择器)
    * `Login Button Selector` (选填，登录按钮的 CSS 选择器)
    * `Show Login Window` (全局设置，布尔值，用于调试。开启后将显示登录过程的 Webview 界面)


* **开机自启**：软件需默认设置为随 Windows 用户登录启动，并最小化至托盘。

### 3.2 网络状态监听 (Passive Monitoring)

* **事件驱动**：软件不主动发起 Wi-Fi 扫描或连接。
* **状态变更监听**：使用 Windows Native API (`NetworkChange.NetworkAddressChanged` 等) 监听网络连接变化。
* **逻辑判定**：
1. 当网络状态变为“已连接”时，获取当前连接的 SSID。
2. 检查该 SSID 是否存在于用户的“配置列表”中。
3. 若**存在**，进入“连通性检测”流程；若**不存在**，则忽略，不执行任何操作。



### 3.3 连通性检测与心跳 (Heartbeat)

为了满足**低功耗**需求，检测机制分为两个阶段：

* **阶段一：轻量级探测 (Low Power)**
* **原理**：不启动浏览器内核，仅通过 Socket 发送 ICMP (Ping) 或 HTTP Head 请求。
* **目标**：公共稳定地址（如 `8.8.8.8` 或 `www.baidu.com`）。
* **频率**：
* 刚连接 Wifi 时：立即检测。
* 已登录状态：每隔 60 秒（可配置）检测一次。


* **判定**：如果请求超时或返回非 200 状态码（如 302 跳转），则判定为“未联网/认证失效”，触发登录流程。


* **阶段二：Portal 探测 (如果阶段一失败)**
* **原理**：尝试访问 `http://msftconnecttest.com/connecttest.txt` 或类似地址，捕获 HTTP 302 重定向的目标 URL（即登录页面地址）。



### 3.4 自动化登录执行 (Login Execution)

当判定需要登录且当前 SSID 在白名单中时，执行以下流程：

* **技术选型**：使用 **Edge WebView2 (Headless模式)** 或 **Puppeteer Core**。
* *理由：虽然纯 HTTP 请求最快，但现在的 Portal 页面常包含 CSRF Token 或动态 JS 加密，使用无头浏览器兼容性最好，且不需要人工抓包分析。*


* **执行步骤**：
1. 在内存中创建一个隐藏的浏览器实例（用户不可见）。
2. 导航至探测到的登录页面 URL。
3. 等待页面 DOM 加载完成（`DomContentLoaded`）。
4. **注入脚本**：根据配置好的 CSS 选择器查找输入框；若未配置，则尝试根据 HTML 元素的 `name` 或 `id` 属性（如 `user`, `username`, `pwd`, `password` 等常见字段）自动查找。
5. 填充配置中的用户名和密码。
6. **执行点击**：根据配置好的 `Login Button Selector` 查找并点击登录按钮；若未配置，则通过匹配按钮文字（如“登录/连接/Login”）自动查找。
7. **结果校验**：等待页面跳转或检测特定元素（如“注销”按钮出现），或再次执行“轻量级探测”确认外网是否连通。
8. **调试模式**：若用户开启了 `Show Login Window` 设置，浏览器窗口将以可见模式弹出（而非隐藏），并自动居中显示。在调试模式下，登录后的保持时间将自动延长（如从 5s 延至 15s），以便用户观察执行结果。
9. 销毁浏览器实例（释放内存）。



### 3.5 托盘交互与通知

* **托盘图标**：显示当前状态（灰色：闲置/未配置；黄色：检测中/登录中；绿色：在线）。
* **系统通知**：
* “自动登录成功：SSID [名称]”
* “登录失败：请检查账号密码或网络状态”



## 4. 数据结构设计 (Local Storage)

建议使用 JSON 或 SQLite 存储配置，文件路径建议在 `%APPDATA%\WifiAutoLogin\`。

**Config.json 示例：**

```json
{
  "autoStart": true,
  "heartbeatInterval": 60,
  "networks": [
    {
      "ssid": "Campus-Net",
      "username": "student_01",
      "encryptedPassword": "BASE64_ENCRYPTED_STRING...",
      "usernameSelector": "#username",
      "passwordSelector": "#password",
      "loginButtonSelector": ".login-btn",
      "strategy": "default" 
    },
    {
      "ssid": "Office-Guest",
      "username": "staff_99",
      "encryptedPassword": "...",
      "strategy": "default"
    }
  ],
  "showBrowser": false
}

```

## 5. 流程图逻辑 (伪代码)

```text
LOOP (Main Service Loop):
    Wait_For_Network_Change_Event()
    
    Current_SSID = Get_Connected_SSID()
    Target_Config = Find_Config_By_SSID(Current_SSID)
    
    IF Target_Config IS NULL:
        Log("当前网络无需托管")
        Continue
        
    Log("检测到托管网络: " + Current_SSID)
    
    // 初始检查
    Is_Online = Lightweight_Ping_Check()
    
    IF Is_Online == FALSE:
        Perform_Login(Target_Config)
        Start_Heartbeat_Timer()
    ELSE:
        Log("网络已通，无需登录")
        Start_Heartbeat_Timer()

FUNCTION Perform_Login(config):
    Browser = Create_Hidden_WebView()
    Login_Url = Detect_Captive_Portal_Url() 
    Browser.Navigate(Login_Url)
    Browser.Fill(config.user, config.pass)
    Browser.Submit()
    
    Wait(3 seconds)
    
    IF Lightweight_Ping_Check() == TRUE:
        Notify("登录成功")
    ELSE:
        Notify("登录失败，将重试")

FUNCTION Heartbeat_Timer_Tick():
    IF Lightweight_Ping_Check() == FALSE:
        Stop_Timer()
        Perform_Login(Target_Config) // 掉线重连

```

## 6. 开发技术栈推荐

鉴于需要在 Windows 11 完美运行且需要操作底层网络和 UI：

* **编程语言**：C# (.NET 6 或 .NET 8)
* **GUI 框架**：WPF 或 Windows Forms (构建配置界面和托盘)
* **浏览器内核**：Microsoft Edge WebView2 (系统自带，无需打包大体积浏览器)
* **网络库**：`System.Net.NetworkInformation` (状态监听), `System.Net.Http` (心跳检测)
