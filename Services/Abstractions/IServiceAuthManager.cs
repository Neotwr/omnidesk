using OmniDesk.Models;

namespace OmniDesk.Services.Abstractions
{
    public interface IServiceAuthManager
    {
        bool PossuiCredenciaisSalvas { get; }
        ServiceAccountCredentials? ObterOuSolicitarCredenciais(bool forcarDialogo = false);
        Task<ServiceAccountCredentials?> ObterOuSolicitarCredenciaisAsync(bool forcarDialogo = false);
        void SalvarCredenciais(ServiceAccountCredentials credenciais);
        void InvalidarCredenciais();
    }
}
