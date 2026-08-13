using System.ComponentModel;
using System.Diagnostics;
using RemoteAccessUtil.Services.Abstractions;

namespace RemoteAccessUtil.Services.Implementations
{
    public class RemoteAccessService : IRemoteAccessService
    {
        public void IniciarAssistencialRemota(string destino, bool type)
        {
            if (string.IsNullOrWhiteSpace(destino))
            {
                throw new ArgumentException("Informe o patrimônio ou endereço IP.", nameof(destino));
            }

            string fileName = type
                    ? "msra.exe"
                    : "mstsc.exe";

            string arguments = type
                ? $"/offerra {destino.Trim()}"
                : $"/v:{destino.Trim()} /admin";

            var psi = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                UseShellExecute = true
            };

            try
            {
                Process.Start(psi);
            }
            catch (Win32Exception ex)
            {
                throw new InvalidOperationException($"Erro ao iniciar {fileName}: {ex.Message}", ex);
            }
        }
    }
}
