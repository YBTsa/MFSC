using MFSC.ViewModels.Pages;
using Wpf.Ui.Abstractions.Controls;

namespace MFSC.Views.Pages
{
    /// <summary>
    /// PasswordManager.xaml 的交互逻辑
    /// </summary>
    public partial class PasswordManagerPage : INavigableView<PasswordManagerViewModel>
    {
        public PasswordManagerPage(PasswordManagerViewModel viewModel)
        {
            InitializeComponent();
            ViewModel = viewModel;
            DataContext = ViewModel;
        }

        public PasswordManagerViewModel ViewModel { get; }
    }
}
