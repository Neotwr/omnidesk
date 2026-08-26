using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using OmniDesk.Helpers;
using OmniDesk.Models;
using OmniDesk.Services.Abstractions;

namespace OmniDesk.Services.Implementations
{
	public class RemoteAccessService : IRemoteAccessService
	{
		public void IniciarAssistencialRemota(string destino, bool type, ServiceAccountCredentials? credenciais = null)
		{
			if (string.IsNullOrWhiteSpace(destino))
			{
				throw new ArgumentException("Informe o patrimônio ou endereço IP.", nameof(destino));
			}

			string target = destino.Trim();

			if (type)
			{
				// === MSRA (Assistência Remota - DCOM/RPC) ===
				string exePath = Path.Combine(Environment.SystemDirectory, "msra.exe");
				string arguments = $"/offerra {target}";

				if (credenciais != null && !string.IsNullOrWhiteSpace(credenciais.Usuario) && !string.IsNullOrWhiteSpace(credenciais.Senha))
				{
					try
					{
						ProcessLogonHelper.IniciarComCredenciaisDeRede(exePath, arguments, credenciais.Usuario, credenciais.Senha);
						return;
					}
					catch (Exception ex)
					{
						throw new InvalidOperationException($"Erro ao iniciar msra.exe com conta de serviço: {ex.Message}", ex);
					}
				}

				ProcessLogonHelper.IniciarDireto(exePath, arguments);
			}
			else
			{
				// === RDP (Área de Trabalho Remota - Terminal Services NLA) ===
				bool credencialRegistrada = false;

				if (credenciais != null && !string.IsNullOrWhiteSpace(credenciais.Usuario) && !string.IsNullOrWhiteSpace(credenciais.Senha))
				{
					RegistrarCredencialRdpNoWindows(target, credenciais.Usuario, credenciais.Senha);
					credencialRegistrada = true;
				}

				string exePath = Path.Combine(Environment.SystemDirectory, "mstsc.exe");
				string arguments = $"/v:{target} /admin";

				try
				{
					ProcessLogonHelper.IniciarDireto(exePath, arguments);
				}
				finally
				{
					if (credencialRegistrada)
					{
						// Limpa automaticamente a credencial temporária do Windows após a conexão iniciar
						_ = Task.Run(async () =>
						{
							try
							{
								await Task.Delay(6000);
								RemoverCredencialRdpDoWindows(target);
							}
							catch { }
						});
					}
				}
			}
		}

		private static void RegistrarCredencialRdpNoWindows(string destino, string usuario, string senha)
		{
			try
			{
				ExecutarCmdkey($"/generic:TERMSRV/{destino} /user:\"{usuario}\" /pass:\"{senha}\"");
			}
			catch { }
		}

		private static void RemoverCredencialRdpDoWindows(string destino)
		{
			try
			{
				ExecutarCmdkey($"/delete:TERMSRV/{destino}");
			}
			catch { }
		}

		private static void ExecutarCmdkey(string args)
		{
			var psi = new ProcessStartInfo
			{
				FileName = "cmdkey.exe",
				Arguments = args,
				CreateNoWindow = true,
				UseShellExecute = false,
				WindowStyle = ProcessWindowStyle.Hidden
			};

			using var proc = Process.Start(psi);
			proc?.WaitForExit(3000);
		}
	}
}
