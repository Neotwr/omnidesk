using CommunityToolkit.Mvvm.ComponentModel;
using OmniDesk.Services.Abstractions;
using OmniDesk.Services.Implementations;

namespace OmniDesk.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        public AcessoRemotoViewModel AcessoRemoto { get; }
        public ComparadorGruposViewModel ComparadorGrupos { get; }
        public ConsultaAcessosViewModel ConsultaAcessos { get; }

        private readonly ISeniorService _seniorService;

        public MainViewModel()
            : this(
                  new ActiveDirectoryService(),
                  new RemoteAccessService(),
                  new GrupoComparerService(),
                  new SapService(),
                  new SeniorService(),
                  null,
                  new DialogService())
        {
        }

        public MainViewModel(
            IActiveDirectoryService adService,
            IRemoteAccessService remoteAccessService,
            IGrupoComparerService grupoComparerService,
            ISapService sapService,
            ISeniorService seniorService,
            ISapAuthManager? sapAuthManager,
            IDialogService dialogService)
        {
            var authManager = sapAuthManager ?? new SapAuthManager(sapService, dialogService);
            _seniorService = seniorService;

            AcessoRemoto = new AcessoRemotoViewModel(remoteAccessService, dialogService);
            ComparadorGrupos = new ComparadorGruposViewModel(adService, grupoComparerService, dialogService);
            ConsultaAcessos = new ConsultaAcessosViewModel(adService, sapService, authManager, seniorService, dialogService);
        }

        public async Task InicializarAsync()
        {
            await ConsultaAcessos.CarregarAmbientesAsync();

            // Aquece a sessão silenciosa do Senior/WBS assincronamente em background sem travar a interface
            _ = Task.Run(async () =>
            {
                try
                {
                    await _seniorService.InicializarSessaoAsync();
                }
                catch
                {
                    // Falhas transitórias na inicialização prévia serão tratadas sob demanda caso o usuário consulte
                }
            });
        }
    }
}
