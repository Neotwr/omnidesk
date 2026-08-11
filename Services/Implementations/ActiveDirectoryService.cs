using System.DirectoryServices.AccountManagement;
using RemoteAccessUtil.Models;
using RemoteAccessUtil.Services.Abstractions;

namespace RemoteAccessUtil.Services.Implementations
{
    public class ActiveDirectoryService : IActiveDirectoryService
    {
        public async Task<List<GrupoAD>> ObterGruposDoUsuarioAsync(string nomeUsuario)
        {
            if (string.IsNullOrWhiteSpace(nomeUsuario))
            {
                throw new ArgumentException("O nome do usuário não pode ser vazio ou nulo.", nameof(nomeUsuario));
            }

            return await Task.Run(() =>
            {
                var listaGrupos = new List<GrupoAD>();

                try
                {
                    using (var context = new PrincipalContext(ContextType.Domain))
                    {
                        using (var user = UserPrincipal.FindByIdentity(context, IdentityType.SamAccountName, nomeUsuario))
                        {
                            if (user == null)
                            {
                                throw new InvalidOperationException($"Usuário '{nomeUsuario}' não foi encontrado no AD.");
                            }

                            var grupos = user.GetGroups();

                            foreach (Principal grupo in grupos)
                            {
                                listaGrupos.Add(new GrupoAD
                                {
                                    Nome = grupo.Name,
                                    Descricao = grupo.Description ?? string.Empty
                                });
                            }
                        }
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
