using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace OmniDesk.Helpers
{
	public static class ProcessLogonHelper
	{
		private const uint LOGON_WITH_PROFILE = 0x00000001;
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

		public static void IniciarComCredenciaisDeRede(string exePath, string? arguments, string usuarioRaw, string senha)
		{
			var si = new STARTUPINFO();
			si.cb = Marshal.SizeOf(si);
			si.dwFlags = STARTF_USESHOWWINDOW;
			si.wShowWindow = SW_HIDE;

			string cmdExe = Path.Combine(Environment.SystemDirectory, "cmd.exe");
			string commandLine = string.IsNullOrWhiteSpace(arguments)
				? $"\"{cmdExe}\" /c start \"\" \"{exePath}\""
				: $"\"{cmdExe}\" /c start \"\" \"{exePath}\" {arguments}";

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

			uint logonFlags = Path.GetFileName(exePath).Equals("msra.exe", StringComparison.OrdinalIgnoreCase)
				? LOGON_NETCREDENTIALS_ONLY
				: LOGON_WITH_PROFILE;

			bool success = CreateProcessWithLogonW(
				usuario,
				dominio,
				senha,
				logonFlags,
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
				throw new Win32Exception(error, $"Falha ao autenticar e iniciar processo (Erro Win32: {error}).");
			}

			if (pi.hThread != IntPtr.Zero) CloseHandle(pi.hThread);
			if (pi.hProcess != IntPtr.Zero) CloseHandle(pi.hProcess);
		}

		/// <summary>
		/// Inicia um processo diretamente no contexto da sessão atual.
		/// </summary>
		public static void IniciarDireto(string exePath, string? arguments = null)
		{
			var psi = new ProcessStartInfo
			{
				FileName = exePath,
				Arguments = arguments ?? string.Empty,
				UseShellExecute = true,
				WorkingDirectory = Environment.SystemDirectory
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
	}
}

