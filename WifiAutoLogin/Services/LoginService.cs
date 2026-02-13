using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using WifiAutoLogin.Models;

namespace WifiAutoLogin.Services
{
    public class LoginService
    {
        public async Task<bool> PerformLoginAsync(NetworkConfig config, string initialUrl, CancellationToken cancellationToken = default)
        {
            try 
            {
                // Must run on UI thread
                LoggerService.Log("Performing login on UI thread...");
                var resultTask = await System.Windows.Application.Current.Dispatcher.InvokeAsync(async () =>
                {
                    HiddenBrowserWindow? window = null;
                    try
                    {
                        if (cancellationToken.IsCancellationRequested) return false;

                        var configService = new ConfigService();
                        window = new HiddenBrowserWindow();
                        window.SetDebugMode(configService.CurrentConfig.ShowBrowser);
                        window.Show(); 
                        LoggerService.Log("HiddenBrowserWindow created and shown.");
                        
                        var webView = window.WebView;
                        LoggerService.Log("Ensuring CoreWebView2...");
                        await webView.EnsureCoreWebView2Async();
                        LoggerService.Log("CoreWebView2 initialized.");
                        
                        // Navigate
                        var targetUrl = string.IsNullOrEmpty(config.LoginUrl) ? initialUrl : config.LoginUrl;
                        if (string.IsNullOrEmpty(targetUrl)) 
                        {
                            LoggerService.Log("No target URL specified for login.");
                            return false;
                        }
                        LoggerService.Log($"Preparing to navigate to: {targetUrl}");

                        var completionSource = new TaskCompletionSource<bool>();
                        bool isSuccessFlowStarted = false;
                        
                        // Link cancellation token to TCS
                        using (cancellationToken.Register(() => completionSource.TrySetCanceled()))
                        {
                            // Simple timeout
                            var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(configService.CurrentConfig.ShowBrowser ? 60 : 30));
                            // Link timeout to TCS as well
                            timeoutCts.Token.Register(() => 
                            {
                                LoggerService.Log("Login flow timed out.");
                                completionSource.TrySetResult(false);
                            });

                            // Navigation Completed Handler
                            webView.NavigationCompleted += async (s, e) =>
                            {
                                if (cancellationToken.IsCancellationRequested || completionSource.Task.IsCompleted || isSuccessFlowStarted) 
                                {
                                    return;
                                }

                                LoggerService.Log($"Navigation completed. Success: {e.IsSuccess}, StatusCode: {e.WebErrorStatus}");

                                if (e.IsSuccess)
                                {
                                    isSuccessFlowStarted = true;
                                    try
                                    {
                                        LoggerService.Log("Waiting 3 seconds for DOM to stabilize...");
                                        await Task.Delay(3000, cancellationToken); // Wait for DOM

                                        // Decrypt password
                                        var password = configService.DecryptPassword(config.EncryptedPassword);
                                        
                                        // Script to find and fill form
                                        string script = $@"
                                            (function() {{
                                                try {{
                                                    console.log('WifiAutoLogin: Starting script execution...');
                                                    
                                                    function setNativeValue(element, value) {{
                                                        var descriptor = Object.getOwnPropertyDescriptor(element, 'value');
                                                        var valueSetter = descriptor ? descriptor.set : undefined;
                                                        
                                                        var prototype = Object.getPrototypeOf(element);
                                                        var protoDescriptor = Object.getOwnPropertyDescriptor(prototype, 'value');
                                                        var prototypeValueSetter = protoDescriptor ? protoDescriptor.set : undefined;
                                                        
                                                        if (valueSetter && valueSetter !== prototypeValueSetter) {{
                                                            prototypeValueSetter.call(element, value);
                                                        }} else if (prototypeValueSetter) {{
                                                            prototypeValueSetter.call(element, value);
                                                        }} else {{
                                                            element.value = value;
                                                        }}
                                                        
                                                        element.dispatchEvent(new Event('input', {{ bubbles: true }}));
                                                        element.dispatchEvent(new Event('change', {{ bubbles: true }}));
                                                        element.dispatchEvent(new Event('blur', {{ bubbles: true }}));
                                                    }}

                                                    // Selectors from config
                                                    var userSelector = '{config.UsernameSelector}';
                                                    var passSelector = '{config.PasswordSelector}';
                                                    var btnSelector = '{config.LoginButtonSelector}';

                                                    var userInputs = [];
                                                    var passInputs = [];

                                                    if (userSelector && document.querySelector(userSelector)) {{
                                                        userInputs = [document.querySelector(userSelector)];
                                                    }} else {{
                                                        // Fallback
                                                        var allInputs = document.querySelectorAll('input[type=text], input[type=email], input[name*=user], input[id*=user], input[id*=account]');
                                                        userInputs = Array.from(allInputs);
                                                    }}

                                                    if (passSelector && document.querySelector(passSelector)) {{
                                                        passInputs = [document.querySelector(passSelector)];
                                                    }} else {{
                                                        // Fallback
                                                        passInputs = document.querySelectorAll('input[type=password], input[name*=pass], input[id*=pass], input[name*=pwd]');
                                                    }}

                                                    // FILTERING: Try to find the *best* visible inputs
                                                    if (passInputs.length > 0) {{
                                                        var pwd = passInputs[0];
                                                        if (window.getComputedStyle(pwd).display === 'none') {{
                                                            var sibling = pwd.previousElementSibling || pwd.nextElementSibling;
                                                            if (sibling && sibling.tagName === 'INPUT') {{
                                                                console.log('WifiAutoLogin: Found hidden password pattern. Swapping.');
                                                                sibling.style.display = 'none';
                                                                pwd.style.display = 'inline-block';
                                                                pwd.type = 'password'; 
                                                            }}
                                                        }}
                                                    }}

                                                    if (userInputs.length > 0) {{
                                                        var u = userInputs[0];
                                                        u.style.border = '2px solid red'; 
                                                        u.focus();
                                                        setNativeValue(u, '{config.Username}');
                                                        u.blur();
                                                    }}

                                                    if (passInputs.length > 0) {{
                                                        var p = passInputs[0];
                                                        p.style.border = '2px solid red'; 
                                                        p.focus();
                                                        setNativeValue(p, '{password}');
                                                        p.blur();
                                                    }}
                                                    
                                                    setTimeout(() => {{
                                                        if (btnSelector && document.querySelector(btnSelector)) {{
                                                            document.querySelector(btnSelector).click();
                                                            return;
                                                        }}

                                                        var buttons = document.querySelectorAll('button, input[type=submit], a[href*=login], .btn');
                                                        for(var btn of buttons) {{
                                                            var txt = btn.innerText || btn.value || '';
                                                            txt = txt.toLowerCase();
                                                            if(txt.includes('login') || txt.includes('connect') || txt.includes('登录') || txt.includes('log in')) {{
                                                                btn.style.border = '2px solid green'; 
                                                                btn.click();
                                                                return;
                                                            }}
                                                        }}
                                                        if(buttons.length > 0) {{
                                                            buttons[0].style.border = '2px solid green';
                                                            buttons[0].click();
                                                        }}
                                                    }}, 1000);
                                                }} catch (e) {{
                                                    console.error('WifiAutoLogin: Script Error', e);
                                                }}
                                            }})();
                                        ";

                                        LoggerService.Log("Executing login script...");
                                        await webView.ExecuteScriptAsync(script);
                                        LoggerService.Log("Login script executed.");
                                        
                                        // Wait a bit for post-login
                                        int waitTime = configService.CurrentConfig.ShowBrowser ? 15000 : 5000;
                                        LoggerService.Log($"Waiting {waitTime}ms for post-login actions...");
                                        await Task.Delay(waitTime, cancellationToken);
                                        LoggerService.Log("Login flow completed in WebView.");
                                        completionSource.TrySetResult(true);
                                    }
                                    catch (OperationCanceledException)
                                    {
                                        completionSource.TrySetCanceled();
                                    }
                                    catch (Exception ex)
                                    {
                                        LoggerService.LogError("Error in navigation handler", ex);
                                        completionSource.TrySetResult(false);
                                    }
                                }
                                else
                                {
                                    // Only fail if WE initiated this navigation attempt and it failed. 
                                    // Retrying or redirects might fix it, so maybe don't fail immediately unless strict?
                                    // If we are here, isSuccessFlowStarted is false.
                                    LoggerService.Log($"Navigation failed (WebErrorStatus: {e.WebErrorStatus}). Waiting for potential redirect or retry...");
                                    // completionSource.TrySetResult(false); // Can cause premature failure on redirects
                                }
                            };

                            LoggerService.Log("Starting navigation...");
                            webView.CoreWebView2.Navigate(targetUrl);
                            
                            return await completionSource.Task;
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        return false; 
                    }
                    catch (Exception ex)
                    {
                        LoggerService.LogError("Error in PerformLoginAsync inner task", ex);
                        return false;
                    }
                    finally
                    {
                        LoggerService.Log("Closing HiddenBrowserWindow...");
                        window?.Close();
                    }
                }, System.Windows.Threading.DispatcherPriority.Normal, cancellationToken);
                
                return await resultTask;
            }
            catch (OperationCanceledException)
            {
                return false;
            }
        }
    }
}
