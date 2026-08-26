using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OmniDesk.Services.Abstractions;

namespace OmniDesk.ViewModels
{
	public partial class UtilViewModel : ObservableObject
	{
		private readonly IProgramLauncherService _programLauncher;
		private readonly IDialogService _dialogService;

		public UtilViewModel(IProgramLauncherService programLauncher, IDialogService dialogService)
		{
			_programLauncher = programLauncher;
			_dialogService = dialogService;
		}

		[RelayCommand]
		private void AbrirPrograma(string? programa)
		{
			if (string.IsNullOrWhiteSpace(programa)) return;

			try
			{
				_programLauncher.ExecutarPrograma(programa);
			}
			catch (Exception ex)
			{
				_dialogService.ShowError(ex.Message, $"Erro ao Iniciar {programa}");
			}
		}
	}
}