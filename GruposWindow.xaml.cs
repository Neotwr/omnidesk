using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace RemoteAccessUtil
{
    /// <summary>
    /// Lógica interna para GruposWindow.xaml
    /// </summary>
    public partial class GruposWindow : Window
    {
        public GruposWindow(List<GrupoAD> grupos, string tituloJanela, string? descricao = null)
        {
            InitializeComponent();

            dgGrupos.ItemsSource = grupos;
            this.Title = $"{tituloJanela} (Total: {grupos.Count})";
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
                filtro = filtro.ToLower();
                view.Filter = item =>
                {
                    GrupoAD? grupo = item as GrupoAD;

                    if (grupo == null) return false;

                    bool nomeBate = grupo.Nome != null && grupo.Nome.ToLower().Contains(filtro);
                    bool descBate = grupo.Descricao != null && grupo.Descricao.ToLower().Contains(filtro);

                    return nomeBate || descBate;
                };
            }
        }
    }
}
