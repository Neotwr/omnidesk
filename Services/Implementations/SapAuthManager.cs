using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using RemoteAccessUtil.Models;
using RemoteAccessUtil.Services.Abstractions;

namespace RemoteAccessUtil.Services.Implementations
{
    public class SapAuthManager : ISapAuthManager
    {
        private readonly ISapService _sapService;
        private readonly IDialogService _dialogService;
        private readonly Dictionary<string, SapUserSession> _sessoesPorAmbiente = new(StringComparer.OrdinalIgnoreCase);
        private readonly string _storagePath;
        private readonly object _lock = new();

        public string? UltimoAmbienteSelecionado { get; set; }

        public SapAuthManager(ISapService sapService, IDialogService? dialogService = null)
        {
            _sapService = sapService;
            _dialogService = dialogService ?? new DialogService();
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string appFolder = Path.Combine(appData, "RemoteAccessUtil");
            Directory.CreateDirectory(appFolder);
            _storagePath = Path.Combine(appFolder, "sap_sessions.dat");

            CarregarSessoesDeDisco();
        }

        public bool PossuiSessaoAtiva(string ambiente)
        {
            if (string.IsNullOrWhiteSpace(ambiente)) return false;
            lock (_lock)
            {
                return _sessoesPorAmbiente.ContainsKey(ambiente);
            }
        }

        public SapUserSession? ObterSessao(string ambiente)
        {
            if (string.IsNullOrWhiteSpace(ambiente)) return null;
            lock (_lock)
            {
                _sessoesPorAmbiente.TryGetValue(ambiente, out var sessao);
                return sessao;
            }
        }

        public SapUserSession? ObterOuSolicitarSessao(string ambiente, bool forcarDialogo = false)
        {
            if (!forcarDialogo && !string.IsNullOrWhiteSpace(ambiente))
            {
                lock (_lock)
                {
                    if (_sessoesPorAmbiente.TryGetValue(ambiente, out var sessaoExistente))
                    {
                        UltimoAmbienteSelecionado = ambiente;
                        return sessaoExistente;
                    }
                }
            }

            var ambientes = _sapService.ObterAmbientesDisponiveis();
            var sessaoSugerida = ObterSessaoSugerida(ambiente);

            var novaSessao = _dialogService.ShowSapLoginDialog(ambientes, sessaoSugerida, ambienteInicial: ambiente);

            if (novaSessao != null)
            {
                lock (_lock)
                {
                    _sessoesPorAmbiente[novaSessao.DestinationName] = novaSessao;
                    UltimoAmbienteSelecionado = novaSessao.DestinationName;

                    if (novaSessao.LembrarSenha)
                    {
                        SalvarSessoesEmDisco();
                    }
                    else
                    {
                        RemoverSessaoDeDisco(novaSessao.DestinationName);
                    }
                }
                return novaSessao;
            }

            return null;
        }

        public async Task<SapUserSession?> ObterOuSolicitarSessaoAsync(string ambiente, bool forcarDialogo = false)
        {
            if (!forcarDialogo && !string.IsNullOrWhiteSpace(ambiente))
            {
                lock (_lock)
                {
                    if (_sessoesPorAmbiente.TryGetValue(ambiente, out var sessaoExistente))
                    {
                        UltimoAmbienteSelecionado = ambiente;
                        return sessaoExistente;
                    }
                }
            }

            var ambientes = await _sapService.ObterAmbientesDisponiveisAsync();
            var sessaoSugerida = ObterSessaoSugerida(ambiente);

            var novaSessao = _dialogService.ShowSapLoginDialog(ambientes, sessaoSugerida, ambienteInicial: ambiente);

            if (novaSessao != null)
            {
                lock (_lock)
                {
                    _sessoesPorAmbiente[novaSessao.DestinationName] = novaSessao;
                    UltimoAmbienteSelecionado = novaSessao.DestinationName;

                    if (novaSessao.LembrarSenha)
                    {
                        SalvarSessoesEmDisco();
                    }
                    else
                    {
                        RemoverSessaoDeDisco(novaSessao.DestinationName);
                    }
                }
                return novaSessao;
            }

            return null;
        }

        public void InvalidarSessao(string ambiente)
        {
            if (string.IsNullOrWhiteSpace(ambiente)) return;
            lock (_lock)
            {
                _sessoesPorAmbiente.Remove(ambiente);
                RemoverSessaoDeDisco(ambiente);
            }
        }

        private SapUserSession? ObterSessaoSugerida(string ambiente)
        {
            lock (_lock)
            {
                if (!string.IsNullOrWhiteSpace(ambiente) && _sessoesPorAmbiente.TryGetValue(ambiente, out var sessao))
                {
                    return sessao;
                }

                var ultimaSessao = _sessoesPorAmbiente.Values.LastOrDefault();
                if (ultimaSessao != null)
                {
                    return new SapUserSession
                    {
                        DestinationName = ambiente,
                        Client = ultimaSessao.Client,
                        User = ultimaSessao.User,
                        Password = ultimaSessao.Password,
                        Language = ultimaSessao.Language,
                        LembrarSenha = ultimaSessao.LembrarSenha
                    };
                }

                return null;
            }
        }

        private void SalvarSessoesEmDisco()
        {
            try
            {
                var sessoesParaSalvar = _sessoesPorAmbiente.Values
                    .Where(s => s.LembrarSenha)
                    .ToList();

                var payload = new SapStoragePayload
                {
                    UltimoAmbiente = UltimoAmbienteSelecionado,
                    Sessoes = sessoesParaSalvar
                };

                string json = JsonSerializer.Serialize(payload);
                byte[] plainBytes = Encoding.UTF8.GetBytes(json);
                byte[] encryptedBytes = ProtectedData.Protect(plainBytes, null, DataProtectionScope.CurrentUser);

                File.WriteAllBytes(_storagePath, encryptedBytes);
            }
            catch
            {
                // Falha silenciosa de persistência
            }
        }

        private void RemoverSessaoDeDisco(string ambiente)
        {
            try
            {
                if (!File.Exists(_storagePath)) return;

                var sessoesParaSalvar = _sessoesPorAmbiente.Values
                    .Where(s => s.LembrarSenha && !s.DestinationName.Equals(ambiente, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                if (sessoesParaSalvar.Count == 0)
                {
                    File.Delete(_storagePath);
                }
                else
                {
                    var payload = new SapStoragePayload
                    {
                        UltimoAmbiente = UltimoAmbienteSelecionado,
                        Sessoes = sessoesParaSalvar
                    };

                    string json = JsonSerializer.Serialize(payload);
                    byte[] plainBytes = Encoding.UTF8.GetBytes(json);
                    byte[] encryptedBytes = ProtectedData.Protect(plainBytes, null, DataProtectionScope.CurrentUser);
                    File.WriteAllBytes(_storagePath, encryptedBytes);
                }
            }
            catch
            {
                // Falha silenciosa
            }
        }

        private void CarregarSessoesDeDisco()
        {
            try
            {
                if (!File.Exists(_storagePath)) return;

                byte[] encryptedBytes = File.ReadAllBytes(_storagePath);
                byte[] plainBytes = ProtectedData.Unprotect(encryptedBytes, null, DataProtectionScope.CurrentUser);
                string json = Encoding.UTF8.GetString(plainBytes);

                var payload = JsonSerializer.Deserialize<SapStoragePayload>(json);
                if (payload != null)
                {
                    UltimoAmbienteSelecionado = payload.UltimoAmbiente;
                    if (payload.Sessoes != null)
                    {
                        foreach (var sessao in payload.Sessoes)
                        {
                            if (!string.IsNullOrWhiteSpace(sessao.DestinationName))
                            {
                                _sessoesPorAmbiente[sessao.DestinationName] = sessao;
                            }
                        }
                    }
                }
            }
            catch
            {
                try { File.Delete(_storagePath); } catch { }
            }
        }

        private class SapStoragePayload
        {
            public string? UltimoAmbiente { get; set; }
            public List<SapUserSession> Sessoes { get; set; } = new();
        }
    }
}
