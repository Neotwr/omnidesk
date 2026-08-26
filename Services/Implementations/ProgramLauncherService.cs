using System;
using System.IO;
using OmniDesk.Helpers;
using OmniDesk.Services.Abstractions;

namespace OmniDesk.Services.Implementations
{
	public class ProgramLauncherService : IProgramLauncherService
	{
		private readonly IServiceAuthManager _authManager;

		public ProgramLauncherService(IServiceAuthManager authManager)
		{
			_authManager = authManager;
		}

		public void ExecutarPrograma(string caminhoExe, string? arguments = null, bool usarContaServico = true)
		{
			string targetExe = caminhoExe;
			string targetArgs = arguments ?? string.Empty;

			if (caminhoExe.EndsWith(".msc", StringComparison.OrdinalIgnoreCase))
			{
				targetExe = "mmc.exe";
				targetArgs = string.IsNullOrWhiteSpace(arguments)
					? caminhoExe
					: $"{caminhoExe} {arguments}";
			}

			if (usarContaServico)
			{
				var credenciais = _authManager.ObterOuSolicitarCredenciais();
				if (credenciais == null) return;

				ProcessLogonHelper.IniciarComCredenciaisDeRede(targetExe, targetArgs, credenciais.Usuario, credenciais.Senha);
			}
			else
			{
				ProcessLogonHelper.IniciarDireto(targetExe, targetArgs);
			}
		}
	}
}