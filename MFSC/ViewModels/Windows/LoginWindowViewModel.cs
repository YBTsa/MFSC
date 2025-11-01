using System;
using System.Security.Cryptography;
using System.Text;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MFSC.ViewModels.Windows
{
    public partial class LoginWindowViewModel : ObservableObject
    {
        // 登录成功事件（解耦View与ViewModel）
        public event Action? LoginSuccess;
        private const string ValidPasswordHash = "E58F9BF9D58E638A6131A951C91151CE245ACA3A09AF0E3683C9FCC32D048E62";

        // 密码验证核心方法
        internal void ValidatePassword(string password)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(password))
                {
                    return; // 空密码由View提前校验
                }

                // 计算输入密码的哈希值
                string inputHash = ComputeSha256Hash(password.Trim());

                // 哈希比对（避免明文存储）
                if (inputHash.Equals(ValidPasswordHash, StringComparison.OrdinalIgnoreCase))
                {
                    LoginSuccess?.Invoke(); // 触发登录成功事件
                    return;
                }

                // 密码错误通知（通过View层显示）
                LoginFailed?.Invoke("密码错误，请重新输入");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"登录异常: {ex.Message}\n堆栈: {ex.StackTrace}");
                LoginFailed?.Invoke("登录过程中发生错误，请稍后重试");
            }
        }

        // 登录失败事件（传递错误信息）
        public event Action<string>? LoginFailed;

        // SHA256哈希计算（核心安全方法）
        internal static string ComputeSha256Hash(string rawData)
        {
            byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawData));
            StringBuilder builder = new();
            foreach (byte b in bytes)
            {
                builder.Append(b.ToString("X2")); 
            }
            return builder.ToString();
        }
    }
}