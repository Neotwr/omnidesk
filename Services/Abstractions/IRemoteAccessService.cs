using OmniDesk.Models;

namespace OmniDesk.Services.Abstractions
{
	public interface IRemoteAccessService
	{
		void IniciarAssistencialRemota(string destino, bool type, ServiceAccountCredentials? credenciais = null);
	}
}
