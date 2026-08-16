using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RemoteAccessUtil.Services.Abstractions;

namespace RemoteAccessUtil.ViewModels
{
    public partial class AcessoRemotoViewModel : ObservableObject
    {
        private readonly IRemoteAccessService _remoteAccessService;
        private readonly IDialogService _dialogService;

        [ObservableProperty]
        private string _destino = string.Empty;

        [ObservableProperty]
        private bool _isMsra = true;

        [ObservableProperty]
        private bool _isRdp;

        public AcessoRemotoViewModel(
            IRemoteAccessService remoteAccessService,
            IDialogService dialogService)
        {
            _remoteAccessService = remoteAccessService;
            _dialogService = dialogService;
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
                _remoteAccessService.IniciarAssistencialRemota(Destino.Trim(), IsMsra);
            }
            catch (Exception ex)
            {
                _dialogService.ShowError(ex.Message, "Erro no Acesso Remoto");
            }
        }
    }
}
