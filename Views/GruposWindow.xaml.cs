using System.Windows;
using RemoteAccessUtil.Models;
using RemoteAccessUtil.ViewModels;

namespace RemoteAccessUtil.Views
{
    /// <summary>
    /// Lógica interna para GruposWindow.xaml
    /// </summary>
    public partial class GruposWindow : Window
    {
        public GruposWindow(
            IEnumerable<Grupos> grupos,
            string tituloJanela,
            string? descricao = null)
        {
            InitializeComponent();
            DataContext = new GruposViewModel(grupos, tituloJanela, descricao);
        }
    }
}
