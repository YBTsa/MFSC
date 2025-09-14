using MFSC.ViewModels.Pages;
using System.Diagnostics;
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
            Debug.WriteLine("Loading");
            _ = UpdateUsage();
        }
        private async Task UpdateUsage()
        {
            await ViewModel.UpdateUsage();
        }
    }
}
