using RemoteAccessUtil.Models;

namespace RemoteAccessUtil.Services.Abstractions
{
    public interface IGrupoComparerService
    {
        ComparacaoGruposResultado CompararGrupos(string usuarioAlvo, List<GrupoAD> gruposAlvo, string usuarioReferencia, List<GrupoAD> gruposReferencia);
        bool FiltrarGrupo(GrupoAD grupo, string filtro);
    }
}
