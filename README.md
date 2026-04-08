# Windows Wi-Fi Auto-Login Assistant

A lightweight Windows 11 tray application designed to automatically identify and complete Wi-Fi Web authentication (Captive Portal), achieving "seamless connectivity".

## Main Features

- **Background Monitoring**: Automatically monitors network connection status changes
- **Configuration Management**: Supports multiple SSID configurations with encrypted storage of account credentials
- **Intelligent Detection**: Automatically identifies whether Web authentication is required and detects login page URLs
- **Seamless Login**: Uses Edge WebView2 kernel to automatically fill and submit login forms in the background
- **Heartbeat Maintenance**: Periodically checks connectivity and automatically reconnects when disconnected
- **Internationalization Support**: Supports switching between Chinese and English interfaces
- **Log Recording**: Optional logging functionality for troubleshooting
- **Auto-start on Boot**: Supports automatic startup on system boot

## Build Guide

### Prerequisites

1. **.NET 8 SDK**: Ensure [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) is installed
2. **WebView2 Runtime**: Usually included with Windows 11. If not installed, download from [Microsoft website](https://developer.microsoft.com/en-us/microsoft-edge/webview2/)

### Build Steps

Run the following commands in the project root directory:

```powershell
# Restore dependencies and build
dotnet build

# Or run directly
dotnet run
```

## Usage Instructions

### 1. Start the Program

After successful build, run the program:
```powershell
dotnet run
```

The program will minimize to the **system tray**, and the tray icon will display the current network connection status.

### 2. Open Settings Interface

- **Method 1**: Double-click the tray icon
- **Method 2**: Right-click the tray icon and select "Settings"

### 3. Configure Networks

In the settings interface:

1. **Add Network Configuration**:
   - Click the **"+"** button in the bottom left corner
   - Fill in the following information:
     - **SSID**: Wi-Fi network name (required)
     - **Username**: Login username
     - **Password**: Login password
     - **Login URL** (optional): Login page address, leave empty for auto-detection
     - **Username CSS Selector** (optional): CSS selector for the username input field
     - **Password CSS Selector** (optional): CSS selector for the password input field
     - **Login Button CSS Selector** (optional): CSS selector for the login button
   - Click **"Save Config"** to save the configuration

2. **Edit Configuration**:
   - Select the network to edit from the left-side network list
   - Modify the information in the right-side form
   - Click **"Save Config"** to save

3. **Delete Configuration**:
   - Select the network to delete from the left-side network list
   - Click the **"-"** button in the bottom left corner
   - Confirm deletion

4. **Test Login**:
   - Select or fill in network configuration information
   - Click **"Test Login"** to test if login is successful

### Test Login Function

The test login function allows you to test configurations without connecting to an actual Wi-Fi network.

#### Usage Method

1. **Prepare Test Page**:
   - Create a `test-page` folder in the project root directory
   - Place HTML files (`.htm` or `.html`) of the login page in this folder
   - If the login page has related resource files (such as images, CSS, JS), place them together

   Directory structure example:
   ```
   wifi_login/
   ├── WifiAutoLogin/
   ├── test-page/
   │   ├── login.html          # Login page
   │   └── login_files/        # Related resource files (optional)
   └── README.md
   ```

2. **Execute Test**:
   - Fill in network configuration information in the settings interface
   - Leave the **Login URL** field empty or enter `test`
   - Click the **"Test Login"** button
   - The program will automatically use the first HTML file in the `test-page` folder for testing

3. **View Results**:
   - Test successful: Shows "Login Test Successful" prompt
   - Test failed: Shows "Login Test Failed" prompt
   - If **"Show Login Window (Debug)"** is enabled, you can see the browser window during the login process

#### Notes

- The test page should be a real login page HTML containing username and password input fields
- If the test fails, you may need to configure the correct CSS selectors
- The form action address in the test page may need to be modified to the actual server address

### 4. Global Settings

At the bottom of the settings interface, you can configure the following global options:

- **Language**: Switch interface language (English / 中文)
- **Notification Level**:
  - **Default**: Only shows important notifications like errors and successes
  - **Maximum**: Shows all notifications (including network detection, online status, etc.)
  - **Silent**: Disables all notifications
- **Show Login Window (Debug)**: Display login window (debug mode)
- **Enable Log Recording**: Enable logging
- **Run on Startup**: Auto-start on system boot

### 5. Auto-Login Process

When the system connects to a configured Wi-Fi network:

1. The program automatically detects network connection status
2. Checks if Web authentication (Captive Portal) is required
3. If authentication is required:
   - Automatically detects or uses the configured login URL
   - Uses WebView2 to load the login page in the background
   - Automatically fills in username and password
   - Automatically clicks the login button
   - Verifies if login is successful
4. Shows login results through system notifications

## Advanced Configuration

### CSS Selector Configuration

If auto-login fails, you may need to manually configure CSS selectors:

1. Open the login page in a browser
2. Use browser developer tools (F12) to inspect page elements
3. Find the username input field, password input field, and login button
4. Right-click the element, select "Copy" → "Copy selector"
5. Paste the selector into the corresponding configuration field

Example:
```
Username CSS Selector: #username
Password CSS Selector: #password
Login Button CSS Selector: button[type="submit"]
```

### Log Files

When logging is enabled, log files are saved in:
```
%APPDATA%\WifiAutoLogin\logs\
```

Log files are named by date in the format: `log-YYYY-MM-DD.txt`

## Notes

### Security

- Passwords are encrypted using **Windows DPAPI (ProtectedData)**
- Can only be decrypted by the currently logged-in user
- Not stored in plain text on disk

### Network Compatibility

- Works with most Wi-Fi networks using Web form authentication
- Supports automatic detection of common Captive Portal pages
- For special authentication pages, manual CSS selector configuration may be required

### Performance Optimization

- Uses background WebView2 processes, doesn't affect foreground applications
- Intelligent detection mechanism avoids unnecessary network requests
- Supports concurrency control to prevent duplicate login attempts

## Technology Stack

- **C# / .NET 8**: Main development language and runtime
- **WPF**: User interface framework
- **WinForms**: System tray icon support
- **WebView2**: Headless browser engine for executing Web logins
- **ManagedNativeWifi**: Windows Wi-Fi API wrapper for network status monitoring
- **DPAPI**: Windows Data Protection API for password encryption

## Troubleshooting

### Common Issues

1. **Program fails to start**
   - Confirm .NET 8 SDK is installed
   - Confirm WebView2 Runtime is installed

2. **Cannot detect login page**
   - Manually configure Login URL
   - Check if network connection is normal

3. **Auto-login fails**
   - Use the Test Login function to test
   - Configure correct CSS selectors
   - Enable logging to view detailed error information

4. **Cannot login after saving password**
   - Check if username and password are correct
   - Some networks may require special character escaping

5. **Test login cannot find test page**
   - Confirm the `test-page` folder is in the project root directory
   - Confirm there are `.htm` or `.html` files in the folder
   - Leave the Login URL field empty or enter `test`

6. **Test login fails but actual network can login**
   - The form action in the test page may point to the wrong address
   - Check if the test page has all resources saved completely
   - Try enabling "Show Login Window (Debug)" to view the detailed process

## License

This project is for personal learning and research use only.
