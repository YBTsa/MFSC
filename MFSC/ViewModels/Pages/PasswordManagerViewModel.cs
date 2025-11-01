using CommunityToolkit.Mvvm.ComponentModel;
using MFSC.Models;
using Newtonsoft.Json;
using System.Collections.ObjectModel;
using System.IO;


namespace MFSC.ViewModels.Pages
{
    public partial class PasswordManagerViewModel : ObservableObject
    {
        private readonly string _jsonFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "MFSC", "passwords.json");

        IReadOnlyDictionary<string, string> Passwords { get; init; }

        [ObservableProperty]
        private ObservableCollection<PasswordItem> _passwordItems = [];

        [ObservableProperty]
        private string _statusMessage = "就绪";

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private string _errorMessage = string.Empty;

        [ObservableProperty]
        private PasswordItem _newPasswordItem = new();

        public PasswordManagerViewModel()
        {
            _ = LoadPasswordsCommand.ExecuteAsync(null);
        }

        [RelayCommand(IncludeCancelCommand = true)]
        private async Task LoadPasswordsAsync(CancellationToken token)
        {
            IsLoading = true;
            StatusMessage = "正在加载密码...";
            ErrorMessage = string.Empty;

            try
            {
                if (!File.Exists(_jsonFilePath))
                {
                    PasswordItems.Clear();
                    StatusMessage = "无密码数据";
                    return;
                }

                var json = await File.ReadAllTextAsync(_jsonFilePath, token);
                var items = JsonConvert.DeserializeObject<List<PasswordItem>>(json);

                if (items != null)
                {
                    PasswordItems = new ObservableCollection<PasswordItem>(items);
                    StatusMessage = $"已加载 {PasswordItems.Count} 条密码";
                }
            }
            catch (OperationCanceledException)
            {
                StatusMessage = "加载已取消";
            }
            catch (Exception ex)
            {
                ErrorMessage = $"加载失败: {ex.Message}";
                StatusMessage = "加载出错";
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task SavePasswordsAsync(bool isDeling)
        {
            if (PasswordItems.Count == 0 && !isDeling) return;

            IsLoading = true;
            StatusMessage = "正在保存密码...";
            ErrorMessage = string.Empty;

            try
            {
                var dir = Path.GetDirectoryName(_jsonFilePath);
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir!);

                var tempPath = $"{_jsonFilePath}.tmp";

                // 直接写入临时文件（无重试逻辑）
                var json = JsonConvert.SerializeObject(PasswordItems, Formatting.Indented);
                await File.WriteAllTextAsync(tempPath, json);

                // 原子替换操作
                if (File.Exists(_jsonFilePath))
                {
                    File.Replace(tempPath, _jsonFilePath, $"{_jsonFilePath}.bak", true);
                }
                else
                {
                    File.Move(tempPath, _jsonFilePath);
                }

                StatusMessage = $"已保存 {PasswordItems.Count} 条密码";
            }
            catch (Exception ex)
            {
                ErrorMessage = $"保存失败: {ex.Message}";
                StatusMessage = "保存出错";
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private void AddPassword()
        {
            if (string.IsNullOrWhiteSpace(NewPasswordItem.Username) ||
                string.IsNullOrWhiteSpace(NewPasswordItem.Password))
            {
                ErrorMessage = "用户名和密码不能为空";
                return;
            }

            PasswordItems.Add(new PasswordItem
            {
                Id = Guid.NewGuid().ToString(),
                Username = NewPasswordItem.Username,
                Password = NewPasswordItem.Password
            });

            NewPasswordItem = new PasswordItem();
            ErrorMessage = string.Empty;
            _ = SavePasswordsCommand.ExecuteAsync(null);
        }

        [RelayCommand]
        private void DeletePassword(PasswordItem item)
        {
            if (item != null)
            {
                PasswordItems.Remove(item);
                _ = SavePasswordsCommand.ExecuteAsync(true);
            }
        }
    }
}