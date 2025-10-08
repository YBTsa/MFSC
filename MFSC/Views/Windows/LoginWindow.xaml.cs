using MFSC.ViewModels.Windows;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

namespace MFSC.Views.Windows
{
    /// <summary>
    /// LoginWindow.xaml 的交互逻辑
    /// </summary>
    public partial class LoginWindow : FluentWindow
    {
        internal bool _needToClose = true;
        public LoginWindow()
        {
            InitializeComponent();
            ViewModel = new LoginWindowViewModel(this);
            SystemThemeWatcher.Watch(this);
            DataContext = ViewModel;
            Closed += LoginWindow_Closed;
        }

        private void LoginWindow_Closed(object? sender, EventArgs e)
        {
            if (_needToClose)
                Application.Current.Shutdown();
        }

        public LoginWindowViewModel ViewModel;
    }
}
