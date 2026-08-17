using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OmniDesk.Models;
using OmniDesk.Services.Abstractions;

namespace OmniDesk.ViewModels
{
    public partial class ConsultaAcessosViewModel : ObservableObject
    {
        private readonly IActiveDirectoryService _adService;
        private readonly ISapService _sapService;
        private readonly ISapAuthManager _sapAuthManager;
        private readonly IDialogService _dialogService;

        [ObservableProperty]
        private string _usuario = string.Empty;

        [ObservableProperty]
        private bool _isAdChecked = true;

        [ObservableProperty]
        private bool _isSapChecked;

        [ObservableProperty]
        private ObservableCollection<string> _ambientes = new();

        [ObservableProperty]
        private string? _ambienteSelecionado;

        [ObservableProperty]
        private bool _isBusy;

        public ConsultaAcessosViewModel(
            IActiveDirectoryService adService,
            ISapService sapService,
            ISapAuthManager sapAuthManager,
            IDialogService dialogService)
        {
            _adService = adService;
            _sapService = sapService;
            _sapAuthManager = sapAuthManager;
            _dialogService = dialogService;
        }

        [RelayCommand]
        public async Task CarregarAmbientesAsync()
        {
            try
            {
                var lista = await _sapService.ObterAmbientesDisponiveisAsync();
                Ambientes.Clear();
                foreach (var amb in lista)
                {
                    Ambientes.Add(amb);
                }

                if (Ambientes.Count > 0)
                {
                    if (!string.IsNullOrWhiteSpace(_sapAuthManager.UltimoAmbienteSelecionado) &&
                        Ambientes.Contains(_sapAuthManager.UltimoAmbienteSelecionado))
                    {
                        AmbienteSelecionado = _sapAuthManager.UltimoAmbienteSelecionado;
                    }
                    else
                    {
                        AmbienteSelecionado = Ambientes[0];
                    }
                }
            }
            catch
            {
                // Silencioso na inicialização da UI
            }
        }

        partial void OnAmbienteSelecionadoChanged(string? value)
        {
            if (!string.IsNullOrWhiteSpace(value) && !value.StartsWith("--"))
            {
                _sapAuthManager.UltimoAmbienteSelecionado = value;
            }
        }

        [RelayCommand]
        public async Task TrocarLoginSapAsync()
        {
            string? ambiente = AmbienteSelecionado?.Trim();
            if (string.IsNullOrWhiteSpace(ambiente) || ambiente.StartsWith("--"))
            {
                _dialogService.ShowWarning("Por favor, selecione um ambiente SAP antes de alterar as credenciais.", "Aviso");
                return;
            }

            IsBusy = true;
            try
            {
                await _sapAuthManager.ObterOuSolicitarSessaoAsync(ambiente, forcarDialogo: true);
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        public async Task ConsultarAsync()
        {
            string usuarioAlvo = Usuario?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(usuarioAlvo))
            {
                _dialogService.ShowWarning("Por favor, digite o nome do usuário.", "Aviso");
                return;
            }

            IsBusy = true;
            string? ambienteAtual = null;

            try
            {
                if (IsAdChecked)
                {
                    List<Grupos> grupos = await _adService.ObterGruposDoUsuarioAsync(usuarioAlvo);
                    _dialogService.ShowGruposWindow(grupos, $"Grupos AD de: {usuarioAlvo}", "Origem: Active Directory");
                }
                else if (IsSapChecked)
                {
                    ambienteAtual = AmbienteSelecionado?.Trim();
                    if (string.IsNullOrWhiteSpace(ambienteAtual) || ambienteAtual.StartsWith("--"))
                    {
                        _dialogService.ShowWarning("Por favor, selecione ou informe um ambiente SAP válido para a consulta.", "Aviso");
                        return;
                    }

                    SapUserSession? sessao = await _sapAuthManager.ObterOuSolicitarSessaoAsync(ambienteAtual, forcarDialogo: false);
                    if (sessao == null)
                    {
                        // Cancelado pelo usuário
                        return;
                    }

                    List<Grupos> perfis = await _sapService.ObterPerfisDoUsuarioAsync(usuarioAlvo, sessao);
                    _dialogService.ShowGruposWindow(perfis, $"Perfis SAP de: {usuarioAlvo}", $"Ambiente: {sessao.DestinationName} (Mandante: {sessao.Client})");
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                if (!string.IsNullOrWhiteSpace(ambienteAtual))
                {
                    _sapAuthManager.InvalidarSessao(ambienteAtual);
                }
                _dialogService.ShowWarning(ex.Message, "Falha de Autenticação SAP");
            }
            catch (KeyNotFoundException ex)
            {
                _dialogService.ShowInfo(ex.Message, "Usuário Não Encontrado");
            }
            catch (Exception ex)
            {
                _dialogService.ShowError(ex.Message, "Erro na Consulta");
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
