using OmniDesk.Models;

namespace OmniDesk.Services.Abstractions
{
	public interface IProgramLauncherService
	{
		void ExecutarPrograma(string caminhoExe, string? arguments = null, bool usarContaServico = true);
	}

}