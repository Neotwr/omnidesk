using System.Windows;
using OmniDesk.Models;

namespace OmniDesk.Views
{
    public partial class ServiceLoginDialog : Window
    {
        public ServiceAccountCredentials? CredenciaisCriadas { get; private set; }

        public ServiceLoginDialog(Window? owner = null, ServiceAccountCredentials? credenciaisSugeridas = null)
        {
            InitializeComponent();
            if (owner != null) Owner = owner;

            if (credenciaisSugeridas != null)
            {
                txtServiceUser.Text = credenciaisSugeridas.Usuario;
                txtServicePassword.Password = credenciaisSugeridas.Senha;
                chkSalvarSenha.IsChecked = credenciaisSugeridas.LembrarSenha;
            }

            Loaded += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(txtServiceUser.Text))
                {
                    txtServiceUser.Focus();
                }
                else
                {
                    txtServicePassword.Focus();
                }
            };
        }

        private void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            string usuario = txtServiceUser.Text.Trim();
            string senha = txtServicePassword.Password;

            if (string.IsNullOrWhiteSpace(usuario))
            {
                MessageBox.Show(this, "Por favor, informe o usuário de serviço.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtServiceUser.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(senha))
            {
                MessageBox.Show(this, "Por favor, informe a senha de serviço.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtServicePassword.Focus();
                return;
            }

            CredenciaisCriadas = new ServiceAccountCredentials
            {
                Usuario = usuario,
                Senha = senha,
                LembrarSenha = chkSalvarSenha.IsChecked == true
            };

            DialogResult = true;
            Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
