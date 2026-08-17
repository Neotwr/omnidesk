using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OmniDesk.Models;
using OmniDesk.Services.Abstractions;

namespace OmniDesk.ViewModels
{
    public partial class SapLoginViewModel : ObservableObject
    {
        private readonly IDialogService _dialogService;

        public event Action<bool?>? RequestClose;

        public ObservableCollection<string> Ambientes { get; } = new();

        [ObservableProperty]
        private string? _ambienteSelecionado;

        [ObservableProperty]
        private string _client = "100";

        [ObservableProperty]
        private string _usuario = string.Empty;

        [ObservableProperty]
        private string _senha = string.Empty;

        [ObservableProperty]
        private string _idioma = "PT";

        [ObservableProperty]
        private bool _lembrarSenha;

        public SapUserSession? SessaoCriada { get; private set; }

        public SapLoginViewModel(
            IEnumerable<string> ambientes,
            SapUserSession? sessaoAnterior = null,
            string? ambienteInicial = null,
            IDialogService? dialogService = null)
        {
            _dialogService = dialogService ?? new Services.Implementations.DialogService();

            var lista = ambientes.ToList();
            foreach (var amb in lista)
            {
                Ambientes.Add(amb);
            }

            if (Ambientes.Count > 0)
            {
                if (!string.IsNullOrWhiteSpace(ambienteInicial) && Ambientes.Contains(ambienteInicial))
                {
                    AmbienteSelecionado = ambienteInicial;
                }
                else if (sessaoAnterior != null && Ambientes.Contains(sessaoAnterior.DestinationName))
                {
                    AmbienteSelecionado = sessaoAnterior.DestinationName;
                }
                else
                {
                    AmbienteSelecionado = Ambientes[0];
                }
            }
            else if (!string.IsNullOrWhiteSpace(ambienteInicial))
            {
                AmbienteSelecionado = ambienteInicial;
            }

            if (sessaoAnterior != null)
            {
                Client = string.IsNullOrWhiteSpace(sessaoAnterior.Client) ? "100" : sessaoAnterior.Client;
                Usuario = sessaoAnterior.User;
                Senha = sessaoAnterior.Password;
                Idioma = string.IsNullOrWhiteSpace(sessaoAnterior.Language) ? "PT" : sessaoAnterior.Language;
                LembrarSenha = sessaoAnterior.LembrarSenha;
            }
        }

        [RelayCommand]
        public void Entrar()
        {
            string? ambiente = AmbienteSelecionado?.Trim();
            string cliente = Client?.Trim() ?? string.Empty;
            string usuario = Usuario?.Trim() ?? string.Empty;
            string senha = Senha;
            string idioma = Idioma?.Trim().ToUpperInvariant() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(ambiente) || ambiente.StartsWith("--"))
            {
                _dialogService.ShowWarning("Por favor, selecione ou informe um ambiente SAP válido.", "Aviso");
                return;
            }

            if (string.IsNullOrWhiteSpace(cliente) || cliente.Length != 3)
            {
                _dialogService.ShowWarning("Por favor, informe um número de cliente/mandante válido com 3 dígitos (ex: 100).", "Aviso");
                return;
            }

            if (string.IsNullOrWhiteSpace(usuario))
            {
                _dialogService.ShowWarning("Por favor, informe o seu usuário SAP.", "Aviso");
                return;
            }

            if (string.IsNullOrEmpty(senha))
            {
                _dialogService.ShowWarning("Por favor, digite a sua senha do SAP.", "Aviso");
                return;
            }

            if (string.IsNullOrWhiteSpace(idioma) || idioma.Length != 2)
            {
                _dialogService.ShowWarning("Por favor, informe o idioma com 2 letras (ex: PT, EN).", "Aviso");
                return;
            }

            SessaoCriada = new SapUserSession
            {
                DestinationName = ambiente,
                Client = cliente,
                User = usuario.ToUpperInvariant(),
                Password = senha,
                Language = idioma,
                LembrarSenha = LembrarSenha
            };

            RequestClose?.Invoke(true);
        }

        [RelayCommand]
        public void Cancelar()
        {
            RequestClose?.Invoke(false);
        }
    }
}
