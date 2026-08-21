using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using OmniDesk.Models;
using OmniDesk.Services.Abstractions;

namespace OmniDesk.Services.Implementations
{
	public class SeniorService : ISeniorService
	{
		private readonly IAppConfigService _configService;
		private WebView2? _webView;
		private bool _isSessionReady;
		private readonly SemaphoreSlim _sessionLock = new(1, 1);

		public SeniorService() : this(new AppConfigService())
		{
		}

		public SeniorService(IAppConfigService configService)
		{
			_configService = configService;
		}

		public async Task InicializarSessaoAsync()
		{
			if (_isSessionReady) return;

			await _sessionLock.WaitAsync();
			try
			{
				if (_isSessionReady) return;

				if (Application.Current?.Dispatcher != null && !Application.Current.Dispatcher.CheckAccess())
				{
					await Application.Current.Dispatcher.InvokeAsync(async () => await IniciarWebView2InternoAsync()).Task.Unwrap();
				}
				else
				{
					await IniciarWebView2InternoAsync();
				}
			}
			finally
			{
				_sessionLock.Release();
			}
		}

		private async Task IniciarWebView2InternoAsync()
		{
			if (_webView == null)
			{
				_webView = new WebView2
				{
					Width = 0,
					Height = 0,
					Visibility = Visibility.Collapsed
				};

				if (Application.Current?.MainWindow is Window mainWindow)
				{
					if (mainWindow.Content is System.Windows.Controls.Panel panel)
					{
						panel.Children.Add(_webView);
					}
				}
			}

			string userDataFolder = Path.Combine(
				Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
				"OmniDesk"
			);

			var options = new CoreWebView2EnvironmentOptions
			{
				AllowSingleSignOnUsingOSPrimaryAccount = true
			};

			var env = await CoreWebView2Environment.CreateAsync(null, userDataFolder, options);
			await _webView.EnsureCoreWebView2Async(env);

			string baseUrl = _configService.WbsSettings.BaseUrl.TrimEnd('/');
			string ssoId = _configService.WbsSettings.SsoId;
			string ssoUrl = $"{baseUrl}/login_with_sso.do?glide_sso_id={ssoId}";

			_webView.Source = new Uri(ssoUrl);

			string gCk = string.Empty;
			DateTime limite = DateTime.UtcNow.AddSeconds(30);

			while (DateTime.UtcNow < limite)
			{
				await Task.Delay(1000);

				try
				{
					string jsCheck = @"(() => {
                        let token = window.g_ck || (window.NOW && window.NOW.g_ck) || (window.NOW && window.NOW.user_token) || '';
                        let uName = (window.NOW && window.NOW.user_name) || (window.NOW && window.NOW.user_display_name) || '';
                        let currentUrl = window.location.href;

                        if (!token) {
                            let frames = document.querySelectorAll('iframe');
                            for (let f of frames) {
                                try {
                                    let win = f.contentWindow;
                                    if (win && (win.g_ck || (win.NOW && win.NOW.g_ck))) {
                                        token = win.g_ck || win.NOW.g_ck;
                                        if (win.NOW && win.NOW.user_name) uName = win.NOW.user_name;
                                        break;
                                    }
                                } catch(e) {}
                            }
                        }

                        return JSON.stringify({
                            'token': token,
                            'uName': uName,
                            'url': currentUrl
                        });
                    })()";

					string resultRaw = await _webView.ExecuteScriptAsync(jsCheck);
					if (!string.IsNullOrEmpty(resultRaw) && resultRaw != "null")
					{
						string innerJson = JsonSerializer.Deserialize<string>(resultRaw) ?? "{}";
						using var doc = JsonDocument.Parse(innerJson);
						var root = doc.RootElement;

						string tokenFound = root.TryGetProperty("token", out var t) ? t.GetString() ?? "" : "";
						string userFound = root.TryGetProperty("uName", out var u) ? u.GetString() ?? "" : "";
						string urlFound = root.TryGetProperty("url", out var url) ? url.GetString() ?? "" : "";

						bool isRedirecting = urlFound.Contains("auth_redirect") ||
											 urlFound.Contains("login_with_sso") ||
											 urlFound.Contains("microsoftonline") ||
											 urlFound.Contains("saml2");

						bool isUserIdentified = !string.IsNullOrEmpty(userFound) && userFound != "guest";

						// Só conclui quando sair dos redirecionamentos e aterrissar na página final autenticada
						if (!isRedirecting && !string.IsNullOrEmpty(tokenFound) && (isUserIdentified || urlFound.Contains("navpage") || urlFound.Contains("/now/") || urlFound.Contains("/sp")))
						{
							gCk = tokenFound;
							break;
						}
					}
				}
				catch { }
			}

			if (string.IsNullOrEmpty(gCk))
			{
				throw new UnauthorizedAccessException("Não foi possível autenticar a sessão do WBS automaticamente via SSO.");
			}

			_isSessionReady = true;
		}

		public async Task<List<Grupos>> ObterGruposDoUsuarioAsync(string login)
		{
			login = login?.Trim() ?? string.Empty;
			if (string.IsNullOrWhiteSpace(login))
			{
				throw new ArgumentException("Login de usuário não pode ser vazio.", nameof(login));
			}

			try
			{
				await InicializarSessaoAsync();

				if (Application.Current?.Dispatcher != null && !Application.Current.Dispatcher.CheckAccess())
				{
					return await Application.Current.Dispatcher.InvokeAsync(async () => await ExecutarConsultaNaSessaoAsync(login)).Task.Unwrap();
				}
				else
				{
					return await ExecutarConsultaNaSessaoAsync(login);
				}
			}
			catch (Exception ex) when (ex is not KeyNotFoundException && ex is not ArgumentException)
			{
				_isSessionReady = false;
				await InicializarSessaoAsync();

				if (Application.Current?.Dispatcher != null && !Application.Current.Dispatcher.CheckAccess())
				{
					return await Application.Current.Dispatcher.InvokeAsync(async () => await ExecutarConsultaNaSessaoAsync(login)).Task.Unwrap();
				}
				else
				{
					return await ExecutarConsultaNaSessaoAsync(login);
				}
			}
		}

		private async Task<List<Grupos>> ExecutarConsultaNaSessaoAsync(string login)
		{
			if (_webView == null)
			{
				throw new InvalidOperationException("WebView2 não inicializado.");
			}

			var tcs = new TaskCompletionSource<string>();

			void Handler(object? s, CoreWebView2WebMessageReceivedEventArgs e)
			{
				try
				{
					string msg = e.TryGetWebMessageAsString();
					if (string.IsNullOrEmpty(msg)) msg = e.WebMessageAsJson;

					using var doc = JsonDocument.Parse(msg);
					if (doc.RootElement.TryGetProperty("type", out var typeProp))
					{
						string type = typeProp.GetString() ?? "";
						if (type == "finish")
						{
							tcs.TrySetResult(msg);
						}
					}
				}
				catch (Exception ex)
				{
					tcs.TrySetException(ex);
				}
			}

			_webView.WebMessageReceived += Handler;

			try
			{
				string baseUrl = _configService.WbsSettings.BaseUrl.TrimEnd('/');
				string catalogSysId = _configService.WbsSettings.CatalogSysId;
				string widgetSysId = _configService.WbsSettings.WidgetSysId;

				string jsRunner = @"(async (login, baseUrl, catalogSysId, widgetSysId) => {
                    const sendFinish = (data) => {
                        try {
                            window.chrome.webview.postMessage(JSON.stringify({ type: 'finish', data: data }));
                        } catch(e) {}
                    };

                    let token = window.g_ck || (window.NOW && window.NOW.g_ck) || (window.NOW && window.NOW.user_token) || '';
                    if (!token) {
                        let frames = document.querySelectorAll('iframe');
                        for (let f of frames) {
                            try {
                                let win = f.contentWindow;
                                if (win && (win.g_ck || (win.NOW && win.NOW.g_ck))) {
                                    token = win.g_ck || win.NOW.g_ck;
                                    break;
                                }
                            } catch(e) {}
                        }
                    }

                    // PASSO 1: Buscar sys_id via xmlhttp.do
                    const params = new URLSearchParams();
                    params.append('sysparm_processor', 'WEGTableCRUD');
                    params.append('sysparm_name', 'getSingleObject');
                    params.append('sysparm_table', 'sys_user');
                    params.append('sysparm_query', 'user_name=' + login + '^ORu_username=' + login);
                    params.append('sysparm_data', JSON.stringify(['sys_id', 'name', 'user_name']));
                    if (token) params.append('sysparm_ck', token);

                    const headers = { 'Content-Type': 'application/x-www-form-urlencoded' };
                    if (token) headers['x-usertoken'] = token;

                    const r1 = await fetch(baseUrl + '/xmlhttp.do', {
                        method: 'POST',
                        headers: headers,
                        body: params.toString()
                    });

                    const xmlText = await r1.text();
                    let sysId = '';

                    try {
                        let parser = new DOMParser();
                        let xmlDoc = parser.parseFromString(xmlText, 'text/xml');
                        let answer = xmlDoc.documentElement.getAttribute('answer') || '{}';
                        let parsedData = JSON.parse(answer);
                        if (parsedData.data && parsedData.data.sys_id) {
                            sysId = parsedData.data.sys_id;
                        }
                    } catch(e) {}

                    if (!sysId) {
                        sendFinish({
                            success: false,
                            notFound: true,
                            error: 'Usuário não encontrado na base corporativa.'
                        });
                        return;
                    }

                    // PASSO 2: Consultar grupos do Senior no Widget
                    const widgetPayload = {
                        action: 'searchGroup',
                        userName: sysId,
                        type: 'hr',
                        wordFilter: '',
                        sapSystem: 'all'
                    };

                    const widgetHeaders = {
                        'Content-Type': 'application/json',
                        'Accept': 'application/json'
                    };
                    if (token) widgetHeaders['x-usertoken'] = token;

                    const widgetUrl = baseUrl + '/api/now/sp/widget/' + widgetSysId + '?id=sc_cat_item&sys_id=' + catalogSysId;
                    const r2 = await fetch(widgetUrl, {
                        method: 'POST',
                        headers: widgetHeaders,
                        body: JSON.stringify(widgetPayload)
                    });

                    const widgetJson = await r2.json();
                    let gruposList = [];

                    if (widgetJson && widgetJson.result && widgetJson.result.data && widgetJson.result.data.userMr) {
                        for (let item of widgetJson.result.data.userMr) {
                            let gName = item.group_name || item.widget_access || '';
                            let gDesc = item.widget_description || '';
                            if (gName) {
                                gruposList.push({
                                    nome: gName,
                                    descricao: gDesc
                                });
                            }
                        }
                    }

                    sendFinish({
                        success: true,
                        grupos: gruposList
                    });
                })('" + login + "', '" + baseUrl + "', '" + catalogSysId + "', '" + widgetSysId + "');";

				await _webView.ExecuteScriptAsync(jsRunner);

				var completedTask = await Task.WhenAny(tcs.Task, Task.Delay(25000));
				if (completedTask != tcs.Task)
				{
					throw new TimeoutException("Tempo limite esgotado ao consultar os grupos no ServiceNow.");
				}

				string jsonResponse = await tcs.Task;
				using var doc = JsonDocument.Parse(jsonResponse);
				var root = doc.RootElement.GetProperty("data");

				bool success = root.TryGetProperty("success", out var sProp) && sProp.GetBoolean();
				if (!success)
				{
					if (root.TryGetProperty("notFound", out var nfProp) && nfProp.GetBoolean())
					{
						throw new KeyNotFoundException($"O usuário '{login}' não foi encontrado na base corporativa.");
					}

					string errorMsg = root.TryGetProperty("error", out var eProp) ? eProp.GetString() ?? "" : "Falha na consulta.";
					throw new Exception(errorMsg);
				}

				var grupos = new List<Grupos>();
				if (root.TryGetProperty("grupos", out var gruposElem))
				{
					foreach (var item in gruposElem.EnumerateArray())
					{
						string nome = item.TryGetProperty("nome", out var n) ? n.GetString() ?? "" : "";
						string desc = item.TryGetProperty("descricao", out var d) ? d.GetString() ?? "" : "";

						if (!string.IsNullOrEmpty(nome))
						{
							grupos.Add(new Grupos
							{
								Nome = nome,
								Descricao = desc,
								Origem = "Senior"
							});
						}
					}
				}

				return grupos;
			}
			finally
			{
				_webView.WebMessageReceived -= Handler;
			}
		}
	}
}
