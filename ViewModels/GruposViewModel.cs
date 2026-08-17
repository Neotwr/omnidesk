using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using OmniDesk.Models;

namespace OmniDesk.ViewModels
{
    public partial class GruposViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _tituloJanela = "Grupos do Usuário";

        [ObservableProperty]
        private string? _descricao;

        [ObservableProperty]
        private string _termoBusca = string.Empty;

        public ObservableCollection<Grupos> GruposList { get; }
        public ICollectionView GruposView { get; }

        public GruposViewModel(IEnumerable<Grupos> grupos, string tituloJanela, string? descricao = null)
        {
            var lista = grupos.ToList();
            TituloJanela = $"{tituloJanela} (Total: {lista.Count})";
            Descricao = descricao;

            GruposList = new ObservableCollection<Grupos>(lista);
            GruposView = CollectionViewSource.GetDefaultView(GruposList);
            GruposView.Filter = FiltrarGrupo;
        }

        partial void OnTermoBuscaChanged(string value)
        {
            GruposView.Refresh();
        }

        private bool FiltrarGrupo(object obj)
        {
            if (obj is not Grupos grupo) return false;
            if (string.IsNullOrWhiteSpace(TermoBusca)) return true;

            string termo = TermoBusca.Trim().ToLowerInvariant();

            bool nomeBate = !string.IsNullOrEmpty(grupo.Nome) && grupo.Nome.ToLowerInvariant().Contains(termo);
            bool descBate = !string.IsNullOrEmpty(grupo.Descricao) && grupo.Descricao.ToLowerInvariant().Contains(termo);

            return nomeBate || descBate;
        }
    }
}
