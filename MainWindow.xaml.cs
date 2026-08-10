using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace RemoteAccessUtil
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        public ActiveDirectoryHelper adHelper = new();

        private void StartRemoteAccess(string target)
        {
            var psi = new ProcessStartInfo
            {
                FileName = "msra.exe",
                Arguments = $"/offerra {target}",
                UseShellExecute = true
            };

            try
            {
                Process.Start(psi);
            }
            catch (System.ComponentModel.Win32Exception ex)
            {
                MessageBox.Show($"Erro ao iniciar msra.exe: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnConnect_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtConnect.Text)) return;

            StartRemoteAccess(txtConnect.Text);
        }
        private void BxtConnect_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtConnect.Text)) return;

            if (e.Key == System.Windows.Input.Key.Enter && !e.IsRepeat)
            {
                e.Handled = true;
                StartRemoteAccess(txtConnect.Text);
            }
        }

        private async void BtnSearchA_Click(object sender, RoutedEventArgs e)
        {
            await RealizarBuscaEExibirAsync(txtUserA.Text, btnSearchA);    
        }

        private async void BtnSearchB_Click(object sender, RoutedEventArgs e)
        {
            await RealizarBuscaEExibirAsync(txtUserB.Text, btnSearchB);
        }
        private async void BtnComparar_Click(object sender, RoutedEventArgs e)
        {
            string userA = txtUserA.Text;
            string userRef = txtUserB.Text;

            if (string.IsNullOrWhiteSpace(userA) || string.IsNullOrWhiteSpace(userRef))
            {
                MessageBox.Show("Por favor, preencha o Usuário e o Usuário de Referência para comparar.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            btnComparar.IsEnabled = false;

            try
            {
                Task<List<GrupoAD>> tarefaBuscaA = adHelper.ObterGruposDoUsuarioAsync(userA);
                Task<List<GrupoAD>> tarefaBuscaRef = adHelper.ObterGruposDoUsuarioAsync(userRef);

                await Task.WhenAll(tarefaBuscaA, tarefaBuscaRef);

                List<GrupoAD> gruposUser = tarefaBuscaA.Result;
                List<GrupoAD> gruposRef = tarefaBuscaRef.Result;

                List<GrupoAD> diferencaRefparaUser = gruposRef
                    .Where(grupoRef => !gruposUser.Any(grupoUser => grupoUser.Nome.Equals(grupoRef.Nome, StringComparison.OrdinalIgnoreCase)))
                    .ToList();

                string titulo = $"Grupos que *{userRef}* possui e faltam em *{userA}*";
                string textoExplicativo = $"Estes são os grupos que faltam para o user {userA}";

                GruposWindow novaJanela = new(diferencaRefparaUser, titulo, textoExplicativo);
                novaJanela.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Erro na Comparação", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                btnComparar.IsEnabled = true;
            }
        }
        
        // helpers ----------------------
        private async Task RealizarBuscaEExibirAsync(string nomeUsuario, Button botaoChamador)
        {
            if (string.IsNullOrWhiteSpace(nomeUsuario))
            {
                MessageBox.Show("Por favor, digite o nome do usuário.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            botaoChamador.IsEnabled = false;

            try
            {
                List<GrupoAD> grupos = await adHelper.ObterGruposDoUsuarioAsync(nomeUsuario);

                string desc = $"Grupos de: {nomeUsuario}";
                GruposWindow novaJanela = new(grupos, desc);
                novaJanela.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Erro na busca", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                botaoChamador.IsEnabled = true;
            }
        }

    }
}