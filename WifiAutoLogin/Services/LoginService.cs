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
                    window = new HiddenBrowserWindow();
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
                                await Task.Delay(2000); // Wait for DOM

                                // Decrypt password
                                var configService = new ConfigService();
                                var password = configService.DecryptPassword(config.EncryptedPassword);
                                
                                // Script to find and fill form
                                string script = $@"
                                    (function() {{
                                        function setNativeValue(element, value) {{
                                            const valueSetter = Object.getOwnPropertyDescriptor(element, 'value').set;
                                            const prototype = Object.getPrototypeOf(element);
                                            const prototypeValueSetter = Object.getOwnPropertyDescriptor(prototype, 'value').set;
                                            
                                            if (valueSetter && valueSetter !== prototypeValueSetter) {{
                                                prototypeValueSetter.call(element, value);
                                            }} else {{
                                                valueSetter.call(element, value);
                                            }}
                                            
                                            element.dispatchEvent(new Event('input', {{ bubbles: true }}));
                                        }}

                                        // Selectors from config
                                        var userSelector = '{config.UsernameSelector}';
                                        var passSelector = '{config.PasswordSelector}';

                                        var userInputs = [];
                                        var passInputs = [];

                                        if (userSelector && document.querySelector(userSelector)) {{
                                            userInputs = [document.querySelector(userSelector)];
                                        }} else {{
                                            // Fallback
                                            userInputs = document.querySelectorAll('input[type=text], input[type=email], input[name*=user], input[id*=user]');
                                        }}

                                        if (passSelector && document.querySelector(passSelector)) {{
                                            passInputs = [document.querySelector(passSelector)];
                                        }} else {{
                                            // Fallback
                                            passInputs = document.querySelectorAll('input[type=password], input[name*=pass], input[id*=pass]');
                                        }}

                                        var buttons = document.querySelectorAll('button, input[type=submit], a[href*=login]');

                                        if (userInputs.length > 0) setNativeValue(userInputs[0], '{config.Username}');
                                        if (passInputs.length > 0) setNativeValue(passInputs[0], '{password}');
                                        
                                        setTimeout(() => {{
                                            // Try to find a submit button
                                            for(var btn of buttons) {{
                                                if(btn.innerText && (btn.innerText.toLowerCase().includes('login') || btn.innerText.toLowerCase().includes('connect') || btn.innerText.toLowerCase().includes('登录'))) {{
                                                    btn.click();
                                                    return;
                                                }}
                                                if(btn.value && (btn.value.toLowerCase().includes('login') || btn.value.toLowerCase().includes('connect'))) {{
                                                    btn.click();
                                                    return;
                                                }}
                                            }}
                                            // Fallback: click the first button found in form
                                            if(buttons.length > 0) buttons[0].click();
                                        }}, 500);
                                    }})();
                                ";

                                await webView.ExecuteScriptAsync(script);
                                
                                // Wait a bit for post-login
                                await Task.Delay(5000);
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
