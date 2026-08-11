using RemoteAccessUtil.Models;
using RemoteAccessUtil.Services.Abstractions;

namespace RemoteAccessUtil.Services.Implementations
{
    public class GrupoComparerService : IGrupoComparerService
    {
        public ComparacaoGruposResultado CompararGrupos(
            string usuarioAlvo,
            List<GrupoAD> gruposAlvo,
            string usuarioReferencia,
            List<GrupoAD> gruposReferencia)
        {
            var diferencaRefParaAlvo = gruposReferencia
                .Where(grupoRef => !gruposAlvo.Any(grupoAlvo => grupoAlvo.Nome.Equals(grupoRef.Nome, StringComparison.OrdinalIgnoreCase)))
                .OrderBy(grupo => grupo.Nome)
                .ToList();

            return new ComparacaoGruposResultado
            {
                UsuarioAlvo = usuarioAlvo,
                UsuarioReferencia = usuarioReferencia,
                GruposFaltantes = diferencaRefParaAlvo
            };
        }

        public bool FiltrarGrupo(GrupoAD grupo, string filtro)
        {
            if (grupo == null) return false;
            if (string.IsNullOrWhiteSpace(filtro)) return true;

            string termo = filtro.Trim().ToLower();

            bool nomeBate = !string.IsNullOrEmpty(grupo.Nome) && grupo.Nome.ToLower().Contains(termo);
            bool descBate = !string.IsNullOrEmpty(grupo.Descricao) && grupo.Descricao.ToLower().Contains(termo);

            return nomeBate || descBate;
        }
    }
}
