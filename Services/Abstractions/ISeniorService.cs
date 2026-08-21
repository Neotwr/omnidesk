using OmniDesk.Models;

namespace OmniDesk.Services.Abstractions
{
    public interface ISeniorService
    {
        Task<List<Grupos>> ObterGruposDoUsuarioAsync(string login);
        Task InicializarSessaoAsync();
    }
}
