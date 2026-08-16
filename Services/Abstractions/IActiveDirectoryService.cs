using RemoteAccessUtil.Models;

namespace RemoteAccessUtil.Services.Abstractions
{
    public interface IActiveDirectoryService
    {
        Task<List<Grupos>> ObterGruposDoUsuarioAsync(string nomeUsuario);
    }
}
