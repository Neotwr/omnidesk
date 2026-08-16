using CommunityToolkit.Mvvm.ComponentModel;
using RemoteAccessUtil.Services.Abstractions;
using RemoteAccessUtil.Services.Implementations;

namespace RemoteAccessUtil.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        public AcessoRemotoViewModel AcessoRemoto { get; }
        public ComparadorGruposViewModel ComparadorGrupos { get; }
        public ConsultaAcessosViewModel ConsultaAcessos { get; }

        public MainViewModel()
            : this(
                  new ActiveDirectoryService(),
                  new RemoteAccessService(),
                  new GrupoComparerService(),
                  new SapService(),
                  null,
                  new DialogService())
        {
        }

        public MainViewModel(
            IActiveDirectoryService adService,
            IRemoteAccessService remoteAccessService,
            IGrupoComparerService grupoComparerService,
            ISapService sapService,
            ISapAuthManager? sapAuthManager,
            IDialogService dialogService)
        {
            var authManager = sapAuthManager ?? new SapAuthManager(sapService, dialogService);

            AcessoRemoto = new AcessoRemotoViewModel(remoteAccessService, dialogService);
            ComparadorGrupos = new ComparadorGruposViewModel(adService, grupoComparerService, dialogService);
            ConsultaAcessos = new ConsultaAcessosViewModel(adService, sapService, authManager, dialogService);
        }

        public async Task InicializarAsync()
        {
            await ConsultaAcessos.CarregarAmbientesAsync();
        }
    }
}
