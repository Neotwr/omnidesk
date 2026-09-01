using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OmniDesk.Services.Abstractions;
using OmniDesk.Services.Implementations;

namespace OmniDesk.ViewModels
{
	public partial class MainViewModel : ObservableObject, IDisposable
	{
		public AcessoRemotoViewModel AcessoRemoto { get; }
		public ComparadorGruposViewModel ComparadorGrupos { get; }
		public ConsultaAcessosViewModel ConsultaAcessos { get; }
		public UtilViewModel Utils { get; }
		private readonly ISeniorService _seniorService;
		private readonly IServiceAuthManager _serviceAuthManager;

		public MainViewModel()
			: this(
				  new ActiveDirectoryService(),
				  new RemoteAccessService(),
				  new GrupoComparerService(),
				  new SapService(),
				  new SeniorService(),
				  null,
				  null,
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
			IServiceAuthManager? serviceAuthManager,
			IProgramLauncherService? programLauncherService,
			IDialogService dialogService)
		{
			var authManager = sapAuthManager ?? new SapAuthManager(sapService, dialogService);
			var svcAuthManager = serviceAuthManager ?? new ServiceAuthManager(dialogService);
			var programLauncher = programLauncherService ?? new ProgramLauncherService(svcAuthManager);

			_seniorService = seniorService;
			_serviceAuthManager = svcAuthManager;

			AcessoRemoto = new AcessoRemotoViewModel(remoteAccessService, svcAuthManager, dialogService);
			ComparadorGrupos = new ComparadorGruposViewModel(adService, sapService, authManager, seniorService, svcAuthManager, grupoComparerService, dialogService);
			ConsultaAcessos = new ConsultaAcessosViewModel(adService, sapService, authManager, seniorService, svcAuthManager, dialogService);
			Utils = new UtilViewModel(programLauncher, dialogService);
		}

		[RelayCommand]
		public async Task TrocarLoginServicoAsync()
		{
			await _serviceAuthManager.ObterOuSolicitarCredenciaisAsync(forcarDialogo: true);
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

		public void Dispose()
		{
			_seniorService.Dispose();
		}
	}
}
