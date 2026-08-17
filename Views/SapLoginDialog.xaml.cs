using System.Windows;
using OmniDesk.Models;
using OmniDesk.ViewModels;

namespace OmniDesk.Views
{
    /// <summary>
    /// Lógica interna para SapLoginDialog.xaml
    /// </summary>
    public partial class SapLoginDialog : Window
    {
        public SapLoginViewModel ViewModel { get; }

        public SapUserSession? SessaoCriada => ViewModel.SessaoCriada;

        public SapLoginDialog() : this(Enumerable.Empty<string>())
        {
        }

        public SapLoginDialog(
            IEnumerable<string> ambientes,
            Window? owner = null,
            SapUserSession? sessaoAnterior = null,
            string? ambienteInicial = null)
        {
            InitializeComponent();

            if (owner != null)
            {
                Owner = owner;
            }

            ViewModel = new SapLoginViewModel(ambientes, sessaoAnterior, ambienteInicial);
            DataContext = ViewModel;

            if (sessaoAnterior != null && !string.IsNullOrEmpty(sessaoAnterior.Password))
            {
                txtSapPassword.Password = sessaoAnterior.Password;
            }

            ViewModel.RequestClose += (resultado) =>
            {
                DialogResult = resultado;
                Close();
            };
        }

        private void TxtSapPassword_PasswordChanged(object sender, RoutedEventArgs e)
        {
            ViewModel.Senha = txtSapPassword.Password;
        }
    }
}
