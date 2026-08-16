using RemoteAccessUtil.Models;

namespace RemoteAccessUtil.Services.Abstractions
{
    public interface ISapService
    {
        IEnumerable<string> ObterAmbientesDisponiveis();
        Task<List<string>> ObterAmbientesDisponiveisAsync();
        Task<List<Grupos>> ObterPerfisDoUsuarioAsync(string nomeUsuario, SapUserSession sessao);
    }
}