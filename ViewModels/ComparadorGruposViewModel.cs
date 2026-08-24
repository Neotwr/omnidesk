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
        private readonly IGrupoComparerService _grupoComparerService;
        private readonly IServiceAuthManager _serviceAuthManager;
        private readonly IDialogService _dialogService;

        [ObservableProperty]
        private string _usuarioAlvo = string.Empty;

        [ObservableProperty]
        private string _usuarioReferencia = string.Empty;

        [ObservableProperty]
        private bool _isBusy;

        public ComparadorGruposViewModel(
            IActiveDirectoryService adService,
            IGrupoComparerService grupoComparerService,
            IServiceAuthManager serviceAuthManager,
            IDialogService dialogService)
        {
            _adService = adService;
            _grupoComparerService = grupoComparerService;
            _serviceAuthManager = serviceAuthManager;
            _dialogService = dialogService;
        }

        [RelayCommand]
        public async Task BuscarUsuarioAlvoAsync()
        {
            await BuscarUsuarioIndividualAsync(UsuarioAlvo);
        }

        [RelayCommand]
        public async Task BuscarUsuarioReferenciaAsync()
        {
            await BuscarUsuarioIndividualAsync(UsuarioReferencia);
        }

        private async Task BuscarUsuarioIndividualAsync(string nomeUsuario)
        {
            if (string.IsNullOrWhiteSpace(nomeUsuario))
            {
                _dialogService.ShowWarning("Por favor, digite o nome do usuário.", "Aviso");
                return;
            }

            IsBusy = true;
            try
            {
                var creds = await _serviceAuthManager.ObterOuSolicitarCredenciaisAsync();
                if (creds == null) return;

                List<Grupos> grupos = await _adService.ObterGruposDoUsuarioAsync(nomeUsuario.Trim(), creds);
                _dialogService.ShowGruposWindow(grupos, $"Grupos de: {nomeUsuario.Trim()}", $"Origem: Active Directory");
            }
            catch (UnauthorizedAccessException ex)
            {
                _serviceAuthManager.InvalidarCredenciais();
                _dialogService.ShowWarning(ex.Message, "Falha de Autenticação AD");
            }
            catch (Exception ex)
            {
                _dialogService.ShowError(ex.Message, "Erro na Busca");
            }
            finally
            {
                IsBusy = false;
            }
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
            try
            {
                var creds = await _serviceAuthManager.ObterOuSolicitarCredenciaisAsync();
                if (creds == null) return;

                Task<List<Grupos>> tarefaBuscaA = _adService.ObterGruposDoUsuarioAsync(userA, creds);
                Task<List<Grupos>> tarefaBuscaRef = _adService.ObterGruposDoUsuarioAsync(userRef, creds);

                await Task.WhenAll(tarefaBuscaA, tarefaBuscaRef);

                List<Grupos> gruposUser = tarefaBuscaA.Result;
                List<Grupos> gruposRef = tarefaBuscaRef.Result;

                ComparacaoGruposResultado resultado = _grupoComparerService.CompararGrupos(userA, gruposUser, userRef, gruposRef);

                _dialogService.ShowGruposWindow(resultado.GruposFaltantes, resultado.TituloJanela, resultado.TextoExplicativo);
            }
            catch (UnauthorizedAccessException ex)
            {
                _serviceAuthManager.InvalidarCredenciais();
                _dialogService.ShowWarning(ex.Message, "Falha de Autenticação AD");
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
