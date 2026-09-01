using System.Windows;
using OmniDesk.ViewModels;

namespace OmniDesk.Views
{
    /// <summary>
    /// Lógica de interação para MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainViewModel ViewModel { get; }

        public MainWindow() : this(new MainViewModel())
        {
        }

        public MainWindow(MainViewModel viewModel)
        {
            InitializeComponent();
            ViewModel = viewModel;
            DataContext = ViewModel;

            Loaded += async (s, e) => await ViewModel.InicializarAsync();
        }

        protected override void OnClosed(EventArgs e)
        {
            try
            {
                ViewModel.Dispose();
            }
            catch { }

            base.OnClosed(e);
            Environment.Exit(0);
        }
    }
}