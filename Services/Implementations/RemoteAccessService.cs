using System.ComponentModel;
using System.Diagnostics;
using RemoteAccessUtil.Services.Abstractions;

namespace RemoteAccessUtil.Services.Implementations
{
    public class RemoteAccessService : IRemoteAccessService
    {
        public void IniciarAssistencialRemota(string destino)
        {
            if (string.IsNullOrWhiteSpace(destino))
            {
                throw new ArgumentException("Informe o patrimônio ou endereço IP.", nameof(destino));
            }

            var psi = new ProcessStartInfo
            {
                FileName = "msra.exe",
                Arguments = $"/offerra {destino.Trim()}",
                UseShellExecute = true
            };

            try
            {
                Process.Start(psi);
            }
            catch (Win32Exception ex)
            {
                throw new InvalidOperationException($"Erro ao iniciar msra.exe: {ex.Message}", ex);
            }
        }
    }
}
