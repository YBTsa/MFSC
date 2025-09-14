using MFSC.ViewModels.Pages;
using System.Diagnostics;
using Wpf.Ui.Abstractions.Controls;

namespace MFSC.Views.Pages
{
    public partial class CleanerPage : INavigableView<CleanerViewModel>
    {
        public CleanerViewModel ViewModel { get; }

        public CleanerPage(CleanerViewModel viewModel)
        {
            ViewModel = viewModel;
            DataContext = ViewModel;

            InitializeComponent();
        }
    }
}
