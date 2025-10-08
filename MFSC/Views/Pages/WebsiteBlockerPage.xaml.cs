using MFSC.ViewModels.Pages;
using Wpf.Ui.Abstractions.Controls;

namespace MFSC.Views.Pages
{
    public partial class WebsiteBlockerPage : INavigableView<WebsiteBlockerViewModel>
    {
        public WebsiteBlockerViewModel ViewModel { get; }

        public WebsiteBlockerPage(WebsiteBlockerViewModel viewModel)
        {
            ViewModel = viewModel;
            DataContext = ViewModel;
            InitializeComponent();
        }
    }
}
