using MFSC.Views.Windows;
using System.Diagnostics;

namespace MFSC.ViewModels.Windows
{
    public partial class LoginWindowViewModel(LoginWindow window) : ObservableObject
    {
        [ObservableProperty]
        private string _password = string.Empty;
        [RelayCommand]
        private async Task TaskLogin()
        {
            try
            {
                if (Password.Trim() == "scqwz371123456789")
                {
                    window._needToClose = false;
                    window.Close();
                    return;
                }
                var uiMessageBox = new Wpf.Ui.Controls.MessageBox
                {
                    Title = "登录",
                    Content =
                                "密码错误，请重新输入。"
                };

                _ = await uiMessageBox.ShowDialogAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error during encryption: {ex.Message}");
            }
        }
    }
}
