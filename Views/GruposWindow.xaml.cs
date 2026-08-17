using System.Windows;
using OmniDesk.Models;
using OmniDesk.ViewModels;

namespace OmniDesk.Views
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
