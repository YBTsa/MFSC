using MFSC.ViewModels.Windows;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

namespace MFSC.Views.Windows
{
    public partial class LoginWindow : FluentWindow
    {
        internal bool _needToClose = true;
        public LoginWindowViewModel ViewModel { get; }

        public LoginWindow()
        {
            InitializeComponent();
            ViewModel = new LoginWindowViewModel();
            // 订阅登录成功事件（解耦核心）
            ViewModel.LoginSuccess += OnLoginSuccess;
            SystemThemeWatcher.Watch(this);
            DataContext = ViewModel;
            Closed += LoginWindow_Closed;
        }

        // 回车键触发登录
        private void PasswordBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                ExecuteLogin();
            }
        }

        // 登录按钮点击事件
        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            ExecuteLogin();
        }

        // 统一登录逻辑（使用SecureString处理密码）
        private void ExecuteLogin()
        {
            var securePassword = PasswordBox.Password;
            if (securePassword.Length == 0)
            {
                ShowMessageBox("提示", "请输入密码");
                return;
            }

            // 安全转换SecureString为字符串（使用后立即清除）
            string password = securePassword.Trim();

            ViewModel.ValidatePassword(password);
            PasswordBox.Clear(); // 清空输入框
        }

        // 登录成功回调
        private void OnLoginSuccess()
        {
            _needToClose = false;
            Close();
        }

        private void LoginWindow_Closed(object? sender, EventArgs e)
        {
            if (_needToClose)
                Application.Current.Shutdown();
        }

        // 封装消息提示
        private void ShowMessageBox(string title, string content)
        {
            var messageBox = new Wpf.Ui.Controls.MessageBox
            {
                Title = title,
                Content = content,
                Owner = this
            };
            _ = messageBox.ShowDialogAsync();
        }
    }
}