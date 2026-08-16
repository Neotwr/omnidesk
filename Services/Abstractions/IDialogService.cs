using RemoteAccessUtil.Models;

namespace RemoteAccessUtil.Services.Abstractions
{
    public interface IDialogService
    {
        void ShowError(string message, string title = "Erro");
        void ShowWarning(string message, string title = "Aviso");
        void ShowInfo(string message, string title = "Informação");
        void ShowGruposWindow(IEnumerable<Grupos> grupos, string titulo, string? descricao = null);
        SapUserSession? ShowSapLoginDialog(IEnumerable<string> ambientes, SapUserSession? sessaoSugerida = null, string? ambienteInicial = null);
    }
}
