using OmniDesk.Models;

namespace OmniDesk.Services.Abstractions
{
    public interface IActiveDirectoryService
    {
        Task<List<Grupos>> ObterGruposDoUsuarioAsync(string nomeUsuario, ServiceAccountCredentials? credenciais = null);
    }
}
