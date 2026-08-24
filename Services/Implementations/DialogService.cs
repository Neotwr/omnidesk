using System.Linq;
using System.Windows;
using OmniDesk.Models;
using OmniDesk.Services.Abstractions;
using OmniDesk.Views;

namespace OmniDesk.Services.Implementations
{
    public class DialogService : IDialogService
    {
        public void ShowError(string message, string title = "Erro")
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
            });
        }

        public void ShowWarning(string message, string title = "Aviso")
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Warning);
            });
        }

        public void ShowInfo(string message, string title = "Informação")
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
            });
        }

        public void ShowGruposWindow(IEnumerable<Grupos> grupos, string titulo, string? descricao = null)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                var janela = new GruposWindow(grupos, titulo, descricao);
                janela.Show();
            });
        }

        public SapUserSession? ShowSapLoginDialog(IEnumerable<string> ambientes, SapUserSession? sessaoSugerida = null, string? ambienteInicial = null)
        {
            return Application.Current.Dispatcher.Invoke(() =>
            {
                Window? activeWindow = Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive)
                    ?? Application.Current.MainWindow;

                var dialog = new SapLoginDialog(ambientes, activeWindow, sessaoSugerida, ambienteInicial);
                bool? resultado = dialog.ShowDialog();

                return resultado == true ? dialog.SessaoCriada : null;
            });
        }

        public ServiceAccountCredentials? ShowServiceLoginDialog(ServiceAccountCredentials? credenciaisSugeridas = null)
        {
            return Application.Current.Dispatcher.Invoke(() =>
            {
                Window? activeWindow = Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive)
                    ?? Application.Current.MainWindow;

                var dialog = new ServiceLoginDialog(activeWindow, credenciaisSugeridas);
                bool? resultado = dialog.ShowDialog();

                return resultado == true ? dialog.CredenciaisCriadas : null;
            });
        }
    }
}
