using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using WifiAutoLogin.Models;

namespace WifiAutoLogin.Services
{
    public class LoginService
    {
        public async Task<bool> PerformLoginAsync(NetworkConfig config, string initialUrl)
        {
            // Must run on UI thread
            var resultTask = await System.Windows.Application.Current.Dispatcher.InvokeAsync(async () =>
            {
                HiddenBrowserWindow? window = null;
                try
                {
                    var configService = new ConfigService();
                    window = new HiddenBrowserWindow();
                    window.SetDebugMode(configService.CurrentConfig.ShowBrowser);
                    window.Show(); // Need to show for WebView to work
                    
                    var webView = window.WebView;
                    await webView.EnsureCoreWebView2Async();
                    
                    // Navigate
                    var targetUrl = string.IsNullOrEmpty(config.LoginUrl) ? initialUrl : config.LoginUrl;
                    if (string.IsNullOrEmpty(targetUrl)) return false;

                    var completionSource = new TaskCompletionSource<bool>();
                    
                    // Simple timeout
                    var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                    timeoutCts.Token.Register(() => completionSource.TrySetResult(false));

                    // Navigation Completed Handler
                    webView.NavigationCompleted += async (s, e) =>
                    {
                        if (e.IsSuccess)
                        {
                            try
                            {
                                await Task.Delay(3000); // Wait for DOM

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
                                                // Fallback - prioritize 'username', 'user', 'account' for ID/Name
                                                var allInputs = document.querySelectorAll('input[type=text], input[type=email], input[name*=user], input[id*=user], input[id*=account]');
                                                // Convert to array to filter/sort if needed, but for now take all
                                                userInputs = Array.from(allInputs);
                                            }}

                                            if (passSelector && document.querySelector(passSelector)) {{
                                                passInputs = [document.querySelector(passSelector)];
                                            }} else {{
                                                // Fallback
                                                passInputs = document.querySelectorAll('input[type=password], input[name*=pass], input[id*=pass], input[name*=pwd]');
                                            }}

                                            // FILTERING: Try to find the *best* visible inputs, or handle the specific hidden password case
                                            
                                            // 1. Target the specific structure of the test page (hidden pwd input + text placeholder)
                                            if (passInputs.length > 0) {{
                                                var pwd = passInputs[0];
                                                if (window.getComputedStyle(pwd).display === 'none') {{
                                                    // Look for sibling placeholder
                                                    var sibling = pwd.previousElementSibling || pwd.nextElementSibling;
                                                    if (sibling && sibling.tagName === 'INPUT') {{
                                                        console.log('WifiAutoLogin: Found hidden password pattern. Swapping.');
                                                        sibling.style.display = 'none';
                                                        pwd.style.display = 'inline-block';
                                                        // Ensure the password field is visible before interacting
                                                        pwd.type = 'password'; 
                                                    }}
                                                }}
                                            }}

                                            if (userInputs.length > 0) {{
                                                var u = userInputs[0];
                                                u.style.border = '2px solid red'; // Visual Feedback
                                                u.focus();
                                                setNativeValue(u, '{config.Username}');
                                                u.blur();
                                            }}

                                            if (passInputs.length > 0) {{
                                                var p = passInputs[0];
                                                p.style.border = '2px solid red'; // Visual Feedback
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
                                                // Try to find a submit button
                                                for(var btn of buttons) {{
                                                    var txt = btn.innerText || btn.value || '';
                                                    txt = txt.toLowerCase();
                                                    if(txt.includes('login') || txt.includes('connect') || txt.includes('登录') || txt.includes('log in')) {{
                                                        btn.style.border = '2px solid green'; // Visual Feedback
                                                        btn.click();
                                                        return;
                                                    }}
                                                }}
                                                // Fallback: click the first button found in form if any
                                                if(buttons.length > 0) {{
                                                    buttons[0].style.border = '2px solid green';
                                                    buttons[0].click();
                                                }}
                                            }}, 1000);
                                        }} catch (e) {{
                                            console.error('WifiAutoLogin: Script Error', e);
                                            alert('AutoLogin Script Error: ' + e.message);
                                        }}
                                    }})();
                                ";

                                await webView.ExecuteScriptAsync(script);
                                
                                // Wait a bit for post-login
                                await Task.Delay(configService.CurrentConfig.ShowBrowser ? 15000 : 5000);
                                completionSource.TrySetResult(true);
                            }
                            catch
                            {
                                completionSource.TrySetResult(false);
                            }
                        }
                    };

                    webView.CoreWebView2.Navigate(targetUrl);
                    
                    return await completionSource.Task;
                }
                catch (Exception)
                {
                    return false;
                }
                finally
                {
                    window?.Close();
                }
            });
            
            return await resultTask;
        }
    }
}
