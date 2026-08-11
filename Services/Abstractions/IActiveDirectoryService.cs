using RemoteAccessUtil.Models;

namespace RemoteAccessUtil.Services.Abstractions
{
    public interface IActiveDirectoryService
    {
        Task<List<GrupoAD>> ObterGruposDoUsuarioAsync(string nomeUsuario);
    }
}
