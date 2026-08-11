using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using RemoteAccessUtil.Models;
using RemoteAccessUtil.Services.Abstractions;
using RemoteAccessUtil.Services.Implementations;

namespace RemoteAccessUtil
{
    /// <summary>
    /// Lógica interna para GruposWindow.xaml
    /// </summary>
    public partial class GruposWindow : Window
    {
        private readonly IGrupoComparerService _grupoComparerService;

        public GruposWindow(
            List<GrupoAD> grupos,
            string tituloJanela,
            string? descricao = null,
            IGrupoComparerService? grupoComparerService = null)
        {
            InitializeComponent();
            _grupoComparerService = grupoComparerService ?? new GrupoComparerService();

            dgGrupos.ItemsSource = grupos;
            Title = $"{tituloJanela} (Total: {grupos.Count})";

            if (descricao != null)
            {
                lblDescricao.Content = descricao;
            }
        }

        private void TxtBusca_TextChanged(object sender, TextChangedEventArgs e)
        {
            ICollectionView view = CollectionViewSource.GetDefaultView(dgGrupos.ItemsSource);
            if (view == null) return;

            string filtro = txtBusca.Text;

            if (string.IsNullOrWhiteSpace(filtro))
            {
                view.Filter = null;
            }
            else
            {
                view.Filter = item => item is GrupoAD grupo && _grupoComparerService.FiltrarGrupo(grupo, filtro);
            }
        }
    }
}
