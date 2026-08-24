using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using OmniDesk.Models;
using OmniDesk.Services.Abstractions;

namespace OmniDesk.Services.Implementations
{
    public class ServiceAuthManager : IServiceAuthManager
    {
        private readonly IDialogService _dialogService;
        private ServiceAccountCredentials? _credenciais;
        private readonly string _storagePath;
        private readonly object _lock = new();

        public bool PossuiCredenciaisSalvas
        {
            get
            {
                lock (_lock)
                {
                    return _credenciais != null;
                }
            }
        }

        public ServiceAuthManager(IDialogService? dialogService = null)
        {
            _dialogService = dialogService ?? new DialogService();
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string appFolder = Path.Combine(appData, "OmniDesk");
            Directory.CreateDirectory(appFolder);
            _storagePath = Path.Combine(appFolder, "service_auth.dat");

            CarregarCredenciaisDeDisco();
        }

        public ServiceAccountCredentials? ObterOuSolicitarCredenciais(bool forcarDialogo = false)
        {
            if (!forcarDialogo)
            {
                lock (_lock)
                {
                    if (_credenciais != null)
                    {
                        return _credenciais;
                    }
                }
            }

            var credenciaisSugeridas = _credenciais;
            var novasCredenciais = _dialogService.ShowServiceLoginDialog(credenciaisSugeridas);

            if (novasCredenciais != null)
            {
                SalvarCredenciais(novasCredenciais);
                return novasCredenciais;
            }

            return null;
        }

        public Task<ServiceAccountCredentials?> ObterOuSolicitarCredenciaisAsync(bool forcarDialogo = false)
        {
            return Task.FromResult(ObterOuSolicitarCredenciais(forcarDialogo));
        }

        public void SalvarCredenciais(ServiceAccountCredentials credenciais)
        {
            lock (_lock)
            {
                _credenciais = credenciais;

                if (credenciais.LembrarSenha)
                {
                    SalvarCredenciaisEmDisco(credenciais);
                }
                else
                {
                    RemoverCredenciaisDeDisco();
                }
            }
        }

        public void InvalidarCredenciais()
        {
            lock (_lock)
            {
                _credenciais = null;
                RemoverCredenciaisDeDisco();
            }
        }

        private void SalvarCredenciaisEmDisco(ServiceAccountCredentials credenciais)
        {
            try
            {
                string json = JsonSerializer.Serialize(credenciais);
                byte[] plainBytes = Encoding.UTF8.GetBytes(json);
                byte[] encryptedBytes = ProtectedData.Protect(plainBytes, null, DataProtectionScope.CurrentUser);

                File.WriteAllBytes(_storagePath, encryptedBytes);
            }
            catch
            {
                // Falha silenciosa de persistência
            }
        }

        private void RemoverCredenciaisDeDisco()
        {
            try
            {
                if (File.Exists(_storagePath))
                {
                    File.Delete(_storagePath);
                }
            }
            catch
            {
                // Falha silenciosa
            }
        }

        private void CarregarCredenciaisDeDisco()
        {
            try
            {
                if (!File.Exists(_storagePath)) return;

                byte[] encryptedBytes = File.ReadAllBytes(_storagePath);
                byte[] plainBytes = ProtectedData.Unprotect(encryptedBytes, null, DataProtectionScope.CurrentUser);
                string json = Encoding.UTF8.GetString(plainBytes);

                var credenciais = JsonSerializer.Deserialize<ServiceAccountCredentials>(json);
                if (credenciais != null && !string.IsNullOrWhiteSpace(credenciais.Usuario))
                {
                    _credenciais = credenciais;
                }
            }
            catch
            {
                try { File.Delete(_storagePath); } catch { }
            }
        }
    }
}
