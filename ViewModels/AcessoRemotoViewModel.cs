using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OmniDesk.Services.Abstractions;

namespace OmniDesk.ViewModels
{
    public partial class AcessoRemotoViewModel : ObservableObject
    {
        private readonly IRemoteAccessService _remoteAccessService;
        private readonly IServiceAuthManager _serviceAuthManager;
        private readonly IDialogService _dialogService;

        [ObservableProperty]
        private string _destino = string.Empty;

        [ObservableProperty]
        private bool _isMsra = true;

        [ObservableProperty]
        private bool _isRdp;

        public AcessoRemotoViewModel(
            IRemoteAccessService remoteAccessService,
            IServiceAuthManager serviceAuthManager,
            IDialogService dialogService)
        {
            _remoteAccessService = remoteAccessService;
            _serviceAuthManager = serviceAuthManager;
            _dialogService = dialogService;
        }

        [RelayCommand]
        public void TrocarLoginServico()
        {
            _serviceAuthManager.ObterOuSolicitarCredenciais(forcarDialogo: true);
        }

        [RelayCommand]
        public void Conectar()
        {
            if (string.IsNullOrWhiteSpace(Destino))
            {
                _dialogService.ShowWarning("Por favor, informe o patrimônio ou endereço IP.", "Aviso");
                return;
            }

            try
            {
                var creds = _serviceAuthManager.ObterOuSolicitarCredenciais();
                if (creds == null) return;

                _remoteAccessService.IniciarAssistencialRemota(Destino.Trim(), IsMsra, creds);
            }
            catch (Exception ex)
            {
                _dialogService.ShowError(ex.Message, "Erro no Acesso Remoto");
            }
        }
    }
}
