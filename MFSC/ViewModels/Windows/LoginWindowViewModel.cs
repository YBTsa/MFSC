using MFSC.Views.Windows;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using Wpf.Ui;
using Wpf.Ui.Controls;
using Wpf.Ui.Extensions;

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
