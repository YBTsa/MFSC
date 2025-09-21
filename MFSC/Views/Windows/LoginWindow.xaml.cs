using MFSC.ViewModels.Windows;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Wpf.Ui;
using Wpf.Ui.Abstractions.Controls;
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
