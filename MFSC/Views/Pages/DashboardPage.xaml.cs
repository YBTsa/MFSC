using MFSC.ViewModels.Pages;
using Wpf.Ui.Abstractions.Controls;

namespace MFSC.Views.Pages
{
    public partial class DashboardPage : INavigableView<DashboardViewModel>
    {
        public DashboardViewModel ViewModel { get; }

        public DashboardPage(DashboardViewModel viewModel)
        {
            ViewModel = viewModel;
            DataContext = ViewModel;
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            // 页面加载后启动监控
            ViewModel.StartMonitoring();
        }
    }
}