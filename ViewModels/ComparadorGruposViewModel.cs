using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OmniDesk.Models;
using OmniDesk.Services.Abstractions;

namespace OmniDesk.ViewModels
{
    public partial class ComparadorGruposViewModel : ObservableObject
    {
        private readonly IActiveDirectoryService _adService;
        private readonly ISapService _sapService;
        private readonly ISapAuthManager _sapAuthManager;
        private readonly ISeniorService _seniorService;
        private readonly IServiceAuthManager _serviceAuthManager;
        private readonly IGrupoComparerService _grupoComparerService;
        private readonly IDialogService _dialogService;

        [ObservableProperty]
        private string _usuarioAlvo = string.Empty;

        [ObservableProperty]
        private string _usuarioReferencia = string.Empty;

        [ObservableProperty]
        private bool _isAdChecked = true;

        [ObservableProperty]
        private bool _isSapChecked;

        [ObservableProperty]
        private bool _isSeniorChecked;

        [ObservableProperty]
        private bool _isBusy;

        public ComparadorGruposViewModel(
            IActiveDirectoryService adService,
            ISapService sapService,
            ISapAuthManager sapAuthManager,
            ISeniorService seniorService,
            IServiceAuthManager serviceAuthManager,
            IGrupoComparerService grupoComparerService,
            IDialogService dialogService)
        {
            _adService = adService;
            _sapService = sapService;
            _sapAuthManager = sapAuthManager;
            _seniorService = seniorService;
            _serviceAuthManager = serviceAuthManager;
            _grupoComparerService = grupoComparerService;
            _dialogService = dialogService;
        }

        [RelayCommand]
        public async Task CompararAsync()
        {
            string userA = UsuarioAlvo?.Trim() ?? string.Empty;
            string userRef = UsuarioReferencia?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(userA) || string.IsNullOrWhiteSpace(userRef))
            {
                _dialogService.ShowWarning("Por favor, preencha o Usuário e o Usuário de Referência para comparar.", "Aviso");
                return;
            }

            IsBusy = true;
            string? ambienteAtualSap = null;

            try
            {
                List<Grupos> gruposUser;
                List<Grupos> gruposRef;

                if (IsAdChecked)
                {
                    var creds = await _serviceAuthManager.ObterOuSolicitarCredenciaisAsync();
                    if (creds == null) return;

                    gruposUser = await _adService.ObterGruposDoUsuarioAsync(userA, creds);
                    gruposRef = await _adService.ObterGruposDoUsuarioAsync(userRef, creds);
                }
                else if (IsSapChecked)
                {
                    ambienteAtualSap = _sapAuthManager.UltimoAmbienteSelecionado?.Trim();
                    if (string.IsNullOrWhiteSpace(ambienteAtualSap) || ambienteAtualSap.StartsWith("--"))
                    {
                        _dialogService.ShowWarning("Por favor, selecione um ambiente SAP na guia 'Acessos' antes de comparar.", "Aviso");
                        return;
                    }

                    SapUserSession? sessao = await _sapAuthManager.ObterOuSolicitarSessaoAsync(ambienteAtualSap, forcarDialogo: false);
                    if (sessao == null) return;

                    gruposUser = await _sapService.ObterPerfisDoUsuarioAsync(userA, sessao);
                    gruposRef = await _sapService.ObterPerfisDoUsuarioAsync(userRef, sessao);
                }
                else if (IsSeniorChecked)
                {
                    gruposUser = await _seniorService.ObterGruposDoUsuarioAsync(userA);
                    gruposRef = await _seniorService.ObterGruposDoUsuarioAsync(userRef);
                }
                else
                {
                    _dialogService.ShowWarning("Por favor, selecione um tipo de acesso para comparar.", "Aviso");
                    return;
                }

                ComparacaoGruposResultado resultado = _grupoComparerService.CompararGrupos(userA, gruposUser, userRef, gruposRef);

                _dialogService.ShowGruposWindow(resultado.GruposFaltantes, resultado.TituloJanela, resultado.TextoExplicativo);
            }
            catch (UnauthorizedAccessException ex)
            {
                if (IsSapChecked && !string.IsNullOrWhiteSpace(ambienteAtualSap))
                {
                    _sapAuthManager.InvalidarSessao(ambienteAtualSap);
                }
                else if (IsAdChecked)
                {
                    _serviceAuthManager.InvalidarCredenciais();
                }

                _dialogService.ShowWarning(ex.Message, "Falha de Autenticação");
            }
            catch (KeyNotFoundException ex)
            {
                _dialogService.ShowInfo(ex.Message, "Usuário Não Encontrado");
            }
            catch (Exception ex)
            {
                _dialogService.ShowError(ex.Message, "Erro na Comparação");
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
