using System;
using System.Collections.Generic;
using System.Diagnostics;
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
		private class SessionExpiredException : UnauthorizedAccessException
		{
			public SessionExpiredException(string message) : base(message) { }
		}

		private readonly IAppConfigService _configService;
		private WebView2? _webView;
		private uint _browserProcessId;
		private bool _isSessionReady;
		private DateTime _sessionEstablishedAt = DateTime.MinValue;
		private readonly SemaphoreSlim _sessionLock = new(1, 1);

		public SeniorService() : this(new AppConfigService())
		{
		}

		public SeniorService(IAppConfigService configService)
		{
			_configService = configService;
		}

		public async Task InicializarSessaoAsync(bool forcarRecarregar = false)
		{
			if (_isSessionReady && !forcarRecarregar) return;

			await _sessionLock.WaitAsync();
			try
			{
				if (_isSessionReady && !forcarRecarregar) return;

				if (Application.Current?.Dispatcher != null && !Application.Current.Dispatcher.CheckAccess())
				{
					await Application.Current.Dispatcher.InvokeAsync(async () =>
					{
						if (forcarRecarregar && _webView?.CoreWebView2 != null)
						{
							await RecarregarWebView2InternoAsync();
						}
						else
						{
							await IniciarWebView2InternoAsync();
						}
					}).Task.Unwrap();
				}
				else
				{
					if (forcarRecarregar && _webView?.CoreWebView2 != null)
					{
						await RecarregarWebView2InternoAsync();
					}
					else
					{
						await IniciarWebView2InternoAsync();
					}
				}
			}
			finally
			{
				_sessionLock.Release();
			}
		}

		public async Task RecarregarSessaoAsync()
		{
			await InicializarSessaoAsync(forcarRecarregar: true);
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
						if (!panel.Children.Contains(_webView))
						{
							panel.Children.Add(_webView);
						}
					}
				}
			}

			if (_webView.CoreWebView2 == null)
			{
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
				_browserProcessId = _webView.CoreWebView2?.BrowserProcessId ?? 0;
			}

			string baseUrl = _configService.WbsSettings.BaseUrl.TrimEnd('/');
			string ssoId = _configService.WbsSettings.SsoId;
			string ssoUrl = $"{baseUrl}/login_with_sso.do?glide_sso_id={ssoId}";

			if (_webView?.CoreWebView2 == null)
			{
				throw new InvalidOperationException("CoreWebView2 não inicializado.");
			}

			_webView.CoreWebView2.Navigate(ssoUrl);

			await AguardarAutenticacaoAsync();
		}

		private async Task RecarregarWebView2InternoAsync()
		{
			if (_webView == null || _webView.CoreWebView2 == null)
			{
				await IniciarWebView2InternoAsync();
				return;
			}

			// Invalida tokens e sinaliza recarga pendente para que o loop não leia dados da sessão anterior
			try
			{
				await _webView.ExecuteScriptAsync(
					"window.__omnidesk_auth_pending = true; " +
					"window.g_ck = ''; " +
					"if (window.NOW) { window.NOW.g_ck = ''; window.NOW.user_token = ''; window.NOW.user_name = ''; }"
				);
			}
			catch { }

			string baseUrl = _configService.WbsSettings.BaseUrl.TrimEnd('/');
			string ssoId = _configService.WbsSettings.SsoId;
			string ssoUrl = $"{baseUrl}/login_with_sso.do?glide_sso_id={ssoId}";

			try
			{
				_webView.CoreWebView2.Navigate(ssoUrl);
			}
			catch
			{
				// Caso o controle esteja em estado inválido/desconectado, reinicializa do zero
				await IniciarWebView2InternoAsync();
				return;
			}

			await AguardarAutenticacaoAsync();
		}

		private async Task AguardarAutenticacaoAsync()
		{
			if (_webView == null)
			{
				throw new InvalidOperationException("WebView2 não inicializado.");
			}

			string gCk = string.Empty;
			DateTime limite = DateTime.UtcNow.AddSeconds(30);

			while (DateTime.UtcNow < limite)
			{
				await Task.Delay(1000);

				try
				{
					string jsCheck = @"(() => {
                        if (window.__omnidesk_auth_pending) {
                            return JSON.stringify({
                                'token': '',
                                'uName': '',
                                'url': window.location.href,
                                'pending': true
                            });
                        }

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
                            'url': currentUrl,
                            'pending': false
                        });
                    })()";

					string resultRaw = await _webView.ExecuteScriptAsync(jsCheck);
					if (!string.IsNullOrEmpty(resultRaw) && resultRaw != "null")
					{
						string innerJson = JsonSerializer.Deserialize<string>(resultRaw) ?? "{}";
						using var doc = JsonDocument.Parse(innerJson);
						var root = doc.RootElement;

						if (root.TryGetProperty("pending", out var pendingProp) && pendingProp.GetBoolean())
						{
							continue;
						}

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
			_sessionEstablishedAt = DateTime.UtcNow;
		}

		public async Task<List<Grupos>> ObterGruposDoUsuarioAsync(string login)
		{
			login = login?.Trim() ?? string.Empty;
			if (string.IsNullOrWhiteSpace(login))
			{
				throw new ArgumentException("Login de usuário não pode ser vazio.", nameof(login));
			}

			// Proativo: se a sessão ainda não foi iniciada ou se passaram mais de 20 minutos de inatividade,
			// renova via SSO antes da consulta para evitar requisições com tokens expirados.
			bool sessaoExpiradaPorTempo = !_isSessionReady || (DateTime.UtcNow - _sessionEstablishedAt > TimeSpan.FromMinutes(20));
			if (sessaoExpiradaPorTempo)
			{
				await RecarregarSessaoAsync();
			}
			else
			{
				await InicializarSessaoAsync();
			}

			try
			{
				return await ExecutarConsultaComDispatcherAsync(login);
			}
			catch (SessionExpiredException)
			{
				// Sessão expirou durante a execução: recarrega a página/tokens e tenta novamente
				await RecarregarSessaoAsync();
				return await ExecutarConsultaComDispatcherAsync(login);
			}
			catch (KeyNotFoundException)
			{
				// Prevenção de falso negativo: se acusou "não encontrado", mas a sessão foi estabelecida
				// há mais de 1 minuto, pode ser que a sessão expirou silenciosamente no backend do ServiceNow.
				// Recarrega uma vez para garantir que não foi expiração de sessão.
				if (DateTime.UtcNow - _sessionEstablishedAt > TimeSpan.FromMinutes(1))
				{
					await RecarregarSessaoAsync();
					return await ExecutarConsultaComDispatcherAsync(login);
				}

				throw;
			}
			catch (Exception ex) when (ex is not ArgumentException)
			{
				// Falhas transitórias (timeout de script, erro de conexão, etc): tenta recarregar e retentar uma vez
				await RecarregarSessaoAsync();
				return await ExecutarConsultaComDispatcherAsync(login);
			}
		}

		private async Task<List<Grupos>> ExecutarConsultaComDispatcherAsync(string login)
		{
			if (Application.Current?.Dispatcher != null && !Application.Current.Dispatcher.CheckAccess())
			{
				return await Application.Current.Dispatcher.InvokeAsync(async () => await ExecutarConsultaNaSessaoAsync(login)).Task.Unwrap();
			}
			else
			{
				return await ExecutarConsultaNaSessaoAsync(login);
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

                    try {
                        let currentUrl = window.location.href || '';
                        let uName = (window.NOW && window.NOW.user_name) || (window.NOW && window.NOW.user_display_name) || '';

                        // Se a página estiver redirecionada para login/SSO ou o usuário for guest, a sessão expirou
                        let isAuthPage = currentUrl.includes('login') || 
                                         currentUrl.includes('auth_redirect') || 
                                         currentUrl.includes('microsoftonline') || 
                                         currentUrl.includes('saml') || 
                                         currentUrl.includes('logout');

                        if (isAuthPage || uName === 'guest') {
                            sendFinish({
                                success: false,
                                sessionExpired: true,
                                error: 'Sessão do ServiceNow redirecionada ou expirada (usuário: ' + (uName || 'não autenticado') + ').'
                            });
                            return;
                        }

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

                        if (!token) {
                            sendFinish({
                                success: false,
                                sessionExpired: true,
                                error: 'Token g_ck do ServiceNow não disponível no contexto da página.'
                            });
                            return;
                        }

                        // PASSO 1: Buscar sys_id via xmlhttp.do
                        const params = new URLSearchParams();
                        params.append('sysparm_processor', 'WEGTableCRUD');
                        params.append('sysparm_name', 'getSingleObject');
                        params.append('sysparm_table', 'sys_user');
                        params.append('sysparm_query', 'user_name=' + login + '^ORu_username=' + login);
                        params.append('sysparm_data', JSON.stringify(['sys_id', 'name', 'user_name']));
                        params.append('sysparm_ck', token);

                        const headers = { 
                            'Content-Type': 'application/x-www-form-urlencoded',
                            'x-usertoken': token
                        };

                        const r1 = await fetch(baseUrl + '/xmlhttp.do', {
                            method: 'POST',
                            headers: headers,
                            body: params.toString()
                        });

                        if (r1.status === 401 || r1.status === 403 || r1.redirected || (r1.url && (r1.url.includes('login') || r1.url.includes('auth') || r1.url.includes('saml')))) {
                            sendFinish({
                                success: false,
                                sessionExpired: true,
                                error: 'Sessão expirada no ServiceNow durante busca do usuário (HTTP ' + r1.status + ').'
                            });
                            return;
                        }

                        if (!r1.ok) {
                            sendFinish({
                                success: false,
                                sessionExpired: true,
                                error: 'Falha HTTP ao consultar ServiceNow: ' + r1.status
                            });
                            return;
                        }

                        const xmlText = await r1.text();
                        let trimmedXml = (xmlText || '').trim();

                        // Verificar se o servidor retornou HTML de login em vez do XML esperado
                        if (trimmedXml.toLowerCase().startsWith('<!doctype') || 
                            trimmedXml.toLowerCase().startsWith('<html') || 
                            trimmedXml.includes('login_with_sso') || 
                            trimmedXml.includes('glide_sso_id') ||
                            trimmedXml.includes('auth_redirect')) {
                            sendFinish({
                                success: false,
                                sessionExpired: true,
                                error: 'Sessão expirada: ServiceNow retornou página de login em vez de XML.'
                            });
                            return;
                        }

                        if (!trimmedXml.includes('<xml')) {
                            sendFinish({
                                success: false,
                                sessionExpired: true,
                                error: 'Resposta inválida do ServiceNow: formato XML não reconhecido.'
                            });
                            return;
                        }

                        let parser = new DOMParser();
                        let xmlDoc = parser.parseFromString(xmlText, 'text/xml');
                        let parserErrors = xmlDoc.getElementsByTagName('parsererror');
                        if (parserErrors && parserErrors.length > 0) {
                            sendFinish({
                                success: false,
                                sessionExpired: true,
                                error: 'Erro ao interpretar XML: provável expiração de sessão.'
                            });
                            return;
                        }

                        let errorAttr = xmlDoc.documentElement.getAttribute('error') || 
                                        xmlDoc.documentElement.getAttribute('sysparm_error') || '';
                        if (errorAttr) {
                            sendFinish({
                                success: false,
                                sessionExpired: true,
                                error: 'ServiceNow retornou erro: ' + errorAttr
                            });
                            return;
                        }

                        let answerAttr = xmlDoc.documentElement.getAttribute('answer');
                        if (answerAttr === null || answerAttr === undefined) {
                            sendFinish({
                                success: false,
                                sessionExpired: true,
                                error: 'Atributo answer ausente na resposta do ServiceNow.'
                            });
                            return;
                        }

                        let sysId = '';
                        try {
                            let parsedData = JSON.parse(answerAttr || '{}');
                            if (parsedData && parsedData.data && parsedData.data.sys_id) {
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
                            'Accept': 'application/json',
                            'x-usertoken': token
                        };

                        const widgetUrl = baseUrl + '/api/now/sp/widget/' + widgetSysId + '?id=sc_cat_item&sys_id=' + catalogSysId;
                        const r2 = await fetch(widgetUrl, {
                            method: 'POST',
                            headers: widgetHeaders,
                            body: JSON.stringify(widgetPayload)
                        });

                        if (r2.status === 401 || r2.status === 403 || r2.redirected) {
                            sendFinish({
                                success: false,
                                sessionExpired: true,
                                error: 'Sessão expirada durante consulta dos grupos do Senior (HTTP ' + r2.status + ').'
                            });
                            return;
                        }

                        let widgetJson;
                        try {
                            widgetJson = await r2.json();
                        } catch(e) {
                            sendFinish({
                                success: false,
                                sessionExpired: true,
                                error: 'Resposta inválida do widget: provável expiração de sessão.'
                            });
                            return;
                        }

                        if (widgetJson && widgetJson.error) {
                            let errDetail = (widgetJson.error.message || '') + ' ' + (widgetJson.error.detail || '');
                            sendFinish({
                                success: false,
                                sessionExpired: true,
                                error: 'Erro retornado pelo widget do ServiceNow: ' + errDetail
                            });
                            return;
                        }

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
                    } catch(err) {
                        sendFinish({
                            success: false,
                            sessionExpired: false,
                            error: 'Exceção não tratada na execução do script: ' + (err && err.message ? err.message : err)
                        });
                    }
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
					if (root.TryGetProperty("sessionExpired", out var seProp) && seProp.GetBoolean())
					{
						string errorMsg = root.TryGetProperty("error", out var eProp) ? eProp.GetString() ?? "" : "Sessão expirada no ServiceNow.";
						throw new SessionExpiredException(errorMsg);
					}

					if (root.TryGetProperty("notFound", out var nfProp) && nfProp.GetBoolean())
					{
						throw new KeyNotFoundException($"O usuário '{login}' não foi encontrado na base corporativa.");
					}

					string errorMsgDefault = root.TryGetProperty("error", out var dProp) ? dProp.GetString() ?? "" : "Falha na consulta.";
					throw new Exception(errorMsgDefault);
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

				_sessionEstablishedAt = DateTime.UtcNow;
				return grupos;
			}
			finally
			{
				_webView.WebMessageReceived -= Handler;
			}
		}

		public void Dispose()
		{
			try
			{
				uint pid = _browserProcessId;
				if (pid == 0 && _webView?.CoreWebView2 != null)
				{
					try
					{
						pid = _webView.CoreWebView2.BrowserProcessId;
					}
					catch { }
				}

				if (_webView != null)
				{
					if (Application.Current?.Dispatcher != null && !Application.Current.Dispatcher.CheckAccess())
					{
						Application.Current.Dispatcher.Invoke(() =>
						{
							_webView?.Dispose();
							_webView = null;
						});
					}
					else
					{
						_webView.Dispose();
						_webView = null;
					}
				}

				if (pid > 0)
				{
					try
					{
						using var proc = Process.GetProcessById((int)pid);
						if (!proc.HasExited)
						{
							if (!proc.WaitForExit(1000))
							{
								proc.Kill(entireProcessTree: true);
							}
						}
					}
					catch (ArgumentException)
					{
						// Processo já finalizou
					}
					catch { }
				}
			}
			catch { }
		}
	}
}
