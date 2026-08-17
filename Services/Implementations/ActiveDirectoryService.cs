using OmniDesk.Models;
using OmniDesk.Services.Abstractions;
using System.DirectoryServices.AccountManagement;

namespace OmniDesk.Services.Implementations
{
    public class ActiveDirectoryService : IActiveDirectoryService
    {
        public async Task<List<Grupos>> ObterGruposDoUsuarioAsync(string nomeUsuario)
        {
            if (string.IsNullOrWhiteSpace(nomeUsuario)) throw new ArgumentException("O nome do usuário não pode ser vazio.", nameof(nomeUsuario));

            return await Task.Run(() =>
            {
                var listaGrupos = new List<Grupos>();

                try
                {
                    using var context = new PrincipalContext(ContextType.Domain);
                    using var user = UserPrincipal.FindByIdentity(context, IdentityType.SamAccountName, nomeUsuario) ??
                        throw new InvalidOperationException($"Usuário '{nomeUsuario}' não foi encontrado no AD.");
                    var grupos = user.GetGroups();

                    foreach (Principal grupo in grupos)
                    {
                        listaGrupos.Add(new Grupos
                        {
                            Nome = grupo.Name,
                            Descricao = grupo.Description ?? string.Empty
                        });
                    }
                }
                catch (Exception ex) when (ex is not InvalidOperationException && ex is not ArgumentException)
                {
                    throw new InvalidOperationException($"Erro na comunicação com o AD: {ex.Message}", ex);
                }

                return listaGrupos.OrderBy(grupo => grupo.Nome).ToList();
            });
        }
    }
}
