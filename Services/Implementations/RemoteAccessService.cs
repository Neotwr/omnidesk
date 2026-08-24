using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using OmniDesk.Models;
using OmniDesk.Services.Abstractions;

namespace OmniDesk.Services.Implementations
{
    public class RemoteAccessService : IRemoteAccessService
    {
        private const uint LOGON_NETCREDENTIALS_ONLY = 0x00000002;
        private const uint CREATE_NO_WINDOW = 0x08000000;
        private const int STARTF_USESHOWWINDOW = 0x00000001;
        private const short SW_HIDE = 0;

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct STARTUPINFO
        {
            public int cb;
            public string lpReserved;
            public string lpDesktop;
            public string lpTitle;
            public int dwX;
            public int dwY;
            public int dwXSize;
            public int dwYSize;
            public int dwXCountChars;
            public int dwYCountChars;
            public int dwFillAttribute;
            public int dwFlags;
            public short wShowWindow;
            public short cbReserved2;
            public IntPtr lpReserved2;
            public IntPtr hStdInput;
            public IntPtr hStdOutput;
            public IntPtr hStdError;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct PROCESS_INFORMATION
        {
            public IntPtr hProcess;
            public IntPtr hThread;
            public int dwProcessId;
            public int dwThreadId;
        }

        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern bool CreateProcessWithLogonW(
            string lpUsername,
            string? lpDomain,
            string lpPassword,
            uint dwLogonFlags,
            string? lpApplicationName,
            string lpCommandLine,
            uint dwCreationFlags,
            IntPtr lpEnvironment,
            string? lpCurrentDirectory,
            ref STARTUPINFO lpStartupInfo,
            out PROCESS_INFORMATION lpProcessInformation);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr hObject);

        public void IniciarAssistencialRemota(string destino, bool type, ServiceAccountCredentials? credenciais = null)
        {
            if (string.IsNullOrWhiteSpace(destino))
            {
                throw new ArgumentException("Informe o patrimônio ou endereço IP.", nameof(destino));
            }

            string target = destino.Trim();

            if (type)
            {
                // === MSRA (Assistência Remota - DCOM/RPC) ===
                string exePath = Path.Combine(Environment.SystemDirectory, "msra.exe");
                string arguments = $"/offerra {target}";

                if (credenciais != null && !string.IsNullOrWhiteSpace(credenciais.Usuario) && !string.IsNullOrWhiteSpace(credenciais.Senha))
                {
                    try
                    {
                        IniciarMsraComCredenciaisDeRede(exePath, arguments, credenciais.Usuario, credenciais.Senha);
                        return;
                    }
                    catch (Exception ex)
                    {
                        throw new InvalidOperationException($"Erro ao iniciar msra.exe com conta de serviço: {ex.Message}", ex);
                    }
                }

                ExecutarProcessoDireto(exePath, arguments);
            }
            else
            {
                // === RDP (Área de Trabalho Remota - Terminal Services NLA) ===
                bool credencialRegistrada = false;

                if (credenciais != null && !string.IsNullOrWhiteSpace(credenciais.Usuario) && !string.IsNullOrWhiteSpace(credenciais.Senha))
                {
                    RegistrarCredencialRdpNoWindows(target, credenciais.Usuario, credenciais.Senha);
                    credencialRegistrada = true;
                }

                string exePath = Path.Combine(Environment.SystemDirectory, "mstsc.exe");
                string arguments = $"/v:{target} /admin";

                try
                {
                    ExecutarProcessoDireto(exePath, arguments);
                }
                finally
                {
                    if (credencialRegistrada)
                    {
                        // Limpa automaticamente a credencial temporária do Windows após a conexão iniciar
                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                await Task.Delay(6000);
                                RemoverCredencialRdpDoWindows(target);
                            }
                            catch { }
                        });
                    }
                }
            }
        }

        private static void ExecutarProcessoDireto(string exePath, string arguments)
        {
            var psi = new ProcessStartInfo
            {
                FileName = exePath,
                Arguments = arguments,
                UseShellExecute = true
            };

            try
            {
                Process.Start(psi);
            }
            catch (Win32Exception ex)
            {
                throw new InvalidOperationException($"Erro ao iniciar {Path.GetFileName(exePath)}: {ex.Message}", ex);
            }
        }

        private static void IniciarMsraComCredenciaisDeRede(string exePath, string arguments, string usuarioRaw, string senha)
        {
            var si = new STARTUPINFO();
            si.cb = Marshal.SizeOf(si);
            si.dwFlags = STARTF_USESHOWWINDOW;
            si.wShowWindow = SW_HIDE;

            string cmdExe = Path.Combine(Environment.SystemDirectory, "cmd.exe");
            string commandLine = $"\"{cmdExe}\" /c start \"\" \"{exePath}\" {arguments}";
            string currentDirectory = Environment.SystemDirectory;

            string usuario = usuarioRaw.Trim();
            string? dominio = null;

            if (usuario.Contains('\\'))
            {
                var partes = usuario.Split('\\', 2);
                dominio = partes[0];
                usuario = partes[1];
            }
            else if (usuario.Contains('@'))
            {
                var partes = usuario.Split('@', 2);
                usuario = partes[0];
                dominio = partes[1];
            }
            else
            {
                dominio = Environment.UserDomainName;
            }

            bool success = CreateProcessWithLogonW(
                usuario,
                dominio,
                senha,
                LOGON_NETCREDENTIALS_ONLY,
                null,
                commandLine,
                CREATE_NO_WINDOW,
                IntPtr.Zero,
                currentDirectory,
                ref si,
                out var pi);

            if (!success)
            {
                int error = Marshal.GetLastWin32Error();
                throw new Win32Exception(error, $"Falha ao autenticar na rede (código de erro Win32: {error}).");
            }

            if (pi.hThread != IntPtr.Zero) CloseHandle(pi.hThread);
            if (pi.hProcess != IntPtr.Zero) CloseHandle(pi.hProcess);
        }

        private static void RegistrarCredencialRdpNoWindows(string destino, string usuario, string senha)
        {
            try
            {
                ExecutarCmdkey($"/generic:TERMSRV/{destino} /user:\"{usuario}\" /pass:\"{senha}\"");
            }
            catch { }
        }

        private static void RemoverCredencialRdpDoWindows(string destino)
        {
            try
            {
                ExecutarCmdkey($"/delete:TERMSRV/{destino}");
            }
            catch { }
        }

        private static void ExecutarCmdkey(string args)
        {
            var psi = new ProcessStartInfo
            {
                FileName = "cmdkey.exe",
                Arguments = args,
                CreateNoWindow = true,
                UseShellExecute = false,
                WindowStyle = ProcessWindowStyle.Hidden
            };

            using var proc = Process.Start(psi);
            proc?.WaitForExit(3000);
        }
    }
}
