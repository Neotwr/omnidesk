using System.Windows;
using System.Windows.Controls;
using RemoteAccessUtil.Models;
using RemoteAccessUtil.Services.Abstractions;
using RemoteAccessUtil.Services.Implementations;

namespace RemoteAccessUtil
{
    /// <summary>
    /// Lógica de interação para MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly IActiveDirectoryService _adService;
        private readonly IRemoteAccessService _remoteAccessService;
        private readonly IGrupoComparerService _grupoComparerService;

        public MainWindow()
            : this(new ActiveDirectoryService(), new RemoteAccessService(), new GrupoComparerService())
        {
        }

        public MainWindow(
            IActiveDirectoryService adService,
            IRemoteAccessService remoteAccessService,
            IGrupoComparerService grupoComparerService)
        {
            InitializeComponent();
            _adService = adService;
            _remoteAccessService = remoteAccessService;
            _grupoComparerService = grupoComparerService;
        }

        private void BtnConnect_Click(object sender, RoutedEventArgs e)
        {
            ExecutarAcessoRemoto();
        }

        private void BxtConnect_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter && !e.IsRepeat)
            {
                e.Handled = true;
                ExecutarAcessoRemoto();
            }
        }

        private void ExecutarAcessoRemoto()
        {
            try
            {
                _remoteAccessService.IniciarAssistencialRemota(txtConnect.Text);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Erro no Acesso Remoto", MessageBoxButton.OK, MessageBoxImage.Error);
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
                Task<List<GrupoAD>> tarefaBuscaA = _adService.ObterGruposDoUsuarioAsync(userA);
                Task<List<GrupoAD>> tarefaBuscaRef = _adService.ObterGruposDoUsuarioAsync(userRef);

                await Task.WhenAll(tarefaBuscaA, tarefaBuscaRef);

                List<GrupoAD> gruposUser = tarefaBuscaA.Result;
                List<GrupoAD> gruposRef = tarefaBuscaRef.Result;

                ComparacaoGruposResultado resultado = _grupoComparerService.CompararGrupos(userA, gruposUser, userRef, gruposRef);

                GruposWindow novaJanela = new(resultado.GruposFaltantes, resultado.TituloJanela, resultado.TextoExplicativo, _grupoComparerService);
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
                List<GrupoAD> grupos = await _adService.ObterGruposDoUsuarioAsync(nomeUsuario);

                string desc = $"Grupos de: {nomeUsuario}";
                GruposWindow novaJanela = new(grupos, desc, null, _grupoComparerService);
                novaJanela.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Erro na Busca", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                botaoChamador.IsEnabled = true;
            }
        }
    }
}