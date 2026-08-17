using OmniDesk.Models;

namespace OmniDesk.Services.Abstractions
{
    public interface ISapAuthManager
    {
        string? UltimoAmbienteSelecionado { get; set; }
        bool PossuiSessaoAtiva(string ambiente);
        SapUserSession? ObterSessao(string ambiente);
        SapUserSession? ObterOuSolicitarSessao(string ambiente, bool forcarDialogo = false);
        Task<SapUserSession?> ObterOuSolicitarSessaoAsync(string ambiente, bool forcarDialogo = false);
        void InvalidarSessao(string ambiente);
    }
}
