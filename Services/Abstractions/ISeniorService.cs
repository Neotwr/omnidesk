using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using OmniDesk.Models;

namespace OmniDesk.Services.Abstractions
{
    public interface ISeniorService : IDisposable
    {
        Task<List<Grupos>> ObterGruposDoUsuarioAsync(string login);
		Task InicializarSessaoAsync(bool forcarRecarregar = false);
		Task RecarregarSessaoAsync();
	}
}
