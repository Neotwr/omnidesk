using RemoteAccessUtil.Models;

namespace RemoteAccessUtil.Services.Abstractions
{
    public interface IGrupoComparerService
    {
        ComparacaoGruposResultado CompararGrupos(string usuarioAlvo, List<Grupos> gruposAlvo, string usuarioReferencia, List<Grupos> gruposReferencia);
        bool FiltrarGrupo(Grupos grupo, string filtro);
    }
}
