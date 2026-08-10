using System.DirectoryServices.AccountManagement;

namespace RemoteAccessUtil { 
public class ActiveDirectoryHelper
{
        public async Task<List<GrupoAD>> ObterGruposDoUsuarioAsync(string nomeUsuario)
            {
                return await Task.Run(() =>
                {
                    List<GrupoAD> listaGrupos = new List<GrupoAD>();

                    try
                    {
                        using (PrincipalContext context = new PrincipalContext(ContextType.Domain))
                        {
                            using (UserPrincipal user = UserPrincipal.FindByIdentity(context, IdentityType.SamAccountName, nomeUsuario))
                            {
                                if (user != null)
                                {
                                    var grupos = user.GetGroups();

                                    foreach (Principal grupo in grupos)
                                    {
                                        listaGrupos.Add(new GrupoAD
                                        {
                                            Nome = grupo.Name,
                                            Descricao = grupo.Description ?? ""
                                        });
                                    }
                                }
                                else
                                {
                                    throw new Exception("Usuário não encontrado no AD.");
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        throw new Exception($"Erro na comunicação com o AD: {ex.Message}");
                    }

                    return listaGrupos.OrderBy(grupo => grupo.Nome).ToList();
                });
            }
    }
    public class GrupoAD
    {
        public required string Nome { get; set; }
        public string? Descricao { get; set; }
    }


}