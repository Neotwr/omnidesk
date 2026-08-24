using System;
using System.Collections.Generic;
using System.DirectoryServices.AccountManagement;
using System.Linq;
using System.Threading.Tasks;
using OmniDesk.Models;
using OmniDesk.Services.Abstractions;

namespace OmniDesk.Services.Implementations
{
    public class ActiveDirectoryService : IActiveDirectoryService
    {
        public async Task<List<Grupos>> ObterGruposDoUsuarioAsync(string nomeUsuario, ServiceAccountCredentials? credenciais = null)
        {
            if (string.IsNullOrWhiteSpace(nomeUsuario)) throw new ArgumentException("O nome do usuário não pode ser vazio.", nameof(nomeUsuario));

            return await Task.Run(() =>
            {
                var listaGrupos = new List<Grupos>();

                try
                {
                    using PrincipalContext context = (credenciais != null && !string.IsNullOrWhiteSpace(credenciais.Usuario))
                        ? new PrincipalContext(ContextType.Domain, null, credenciais.Usuario, credenciais.Senha)
                        : new PrincipalContext(ContextType.Domain);

                    using var user = UserPrincipal.FindByIdentity(context, IdentityType.SamAccountName, nomeUsuario) ??
                        throw new KeyNotFoundException($"O usuário '{nomeUsuario}' não foi encontrado no Active Directory.");

                    var grupos = user.GetGroups();

                    foreach (Principal grupo in grupos)
                    {
                        listaGrupos.Add(new Grupos
                        {
                            Nome = grupo.Name,
                            Descricao = grupo.Description ?? string.Empty,
                            Origem = "Active Directory"
                        });
                    }
                }
                catch (KeyNotFoundException)
                {
                    throw;
                }
                catch (PrincipalServerDownException ex)
                {
                    throw new InvalidOperationException($"Não foi possível alcançar o servidor de domínio do AD: {ex.Message}", ex);
                }
                catch (PrincipalOperationException ex)
                {
                    throw new UnauthorizedAccessException($"Falha de autenticação no Active Directory com a conta de serviço: {ex.Message}", ex);
                }
                catch (Exception ex) when (ex is not InvalidOperationException && ex is not ArgumentException && ex is not UnauthorizedAccessException)
                {
                    throw new InvalidOperationException($"Erro na comunicação com o AD: {ex.Message}", ex);
                }

                return listaGrupos.OrderBy(grupo => grupo.Nome).ToList();
            });
        }
    }
}
