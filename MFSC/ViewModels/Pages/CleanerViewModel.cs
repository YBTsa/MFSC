using MFSC.Helpers;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;

namespace MFSC.ViewModels.Pages
{
    public class CleanProjectInfo : INotifyPropertyChanged
    {
        private string _name;
        private int _id;
        private string _description;
        private string _targetPath;
        private bool _isChoosed;

        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(); }
        }

        public int Id
        {
            get => _id;
            set { _id = value; OnPropertyChanged(); }
        }

        public string Description
        {
            get => _description;
            set { _description = value; OnPropertyChanged(); }
        }

        public bool IsChoosed
        {
            get => _isChoosed;
            set { _isChoosed = value; OnPropertyChanged(); }
        }

        public string TargetPath
        {
            get => _targetPath;
            set { _targetPath = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public partial class CleanerViewModel : ObservableObject
    {
        [ObservableProperty]
        private int _progressValue;
        [ObservableProperty]
        private bool _isIndeterminate;
        [ObservableProperty]
        private bool _isCleaning;
        [ObservableProperty]
        private string _statusMessage = "准备就绪";
        [ObservableProperty]
        private string _errorMessage = string.Empty;

        public IAsyncRelayCommand CleanCommand { get; }

        private readonly ObservableCollection<CleanProjectInfo> _cleanProjects = [
            new CleanProjectInfo {
                Id = 1,
                Name = "系统临时文件",
                Description = "Windows系统临时文件目录，存放各种程序运行时产生的临时数据",
                TargetPath = Path.GetTempPath()
            },
            new CleanProjectInfo {
                Id = 2,
                Name = "Windows更新缓存",
                Description = "Windows更新下载的安装文件，更新完成后可清理",
                TargetPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "SoftwareDistribution", "Download")
            },
            new CleanProjectInfo {
                Id = 3,
                Name = "回收站",
                Description = "已删除文件的临时存放位置"
            },
            new CleanProjectInfo {
                Id = 4,
                Name = "系统日志缓存",
                Description = "系统事件和服务产生的日志文件",
                TargetPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Logs")
            },
            new CleanProjectInfo {
                Id = 5,
                Name = "应用程序缓存",
                Description = "应用程序和浏览器的网络缓存文件",
                TargetPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Microsoft", "Windows", "INetCache")
            },
            new CleanProjectInfo {
                Id = 6,
                Name = "下载文件夹",
                Description = "用户下载的文件，通常可清理",
                TargetPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + @"\Downloads"
            },
            new CleanProjectInfo {
                Id = 7,  // 修正ID匹配问题
                Name = "希沃白板临时文件",
                Description = "希沃白板下载的文件，通常可清理"
            }
        ];

        public ObservableCollection<CleanProjectInfo> CleanProjects => _cleanProjects;

        public CleanerViewModel()
        {
            CleanCommand = new AsyncRelayCommand(ExecuteCleanAsync, CanExecuteClean);
            foreach (var project in CleanProjects)
            {
                project.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(CleanProjectInfo.IsChoosed))
                        CleanCommand.NotifyCanExecuteChanged();
                };
            }
        }

        private bool CanExecuteClean()
        {
            return !IsCleaning && CleanProjects.Any(p => p.IsChoosed);
        }

        private async Task ExecuteCleanAsync()
        {
            IsCleaning = true;
            ErrorMessage = string.Empty;
            ProgressValue = 0;
            var selectedProjects = CleanProjects.Where(p => p.IsChoosed).ToList();
            var totalItems = selectedProjects.Count;
            int completedItems = 0;

            try
            {
                // 内存优化：使用数组替代IEnumerable重复枚举
                var projectsArray = selectedProjects.ToArray();
                foreach (var project in projectsArray)
                {
                    StatusMessage = $"正在清理: {project.Name}";
                    // 根据项目类型调度资源
                    switch (project.Id)
                    {
                        case 2: // Windows更新缓存（高优先级）
                            IsIndeterminate = true;
                            await Task.Run(async () => await SystemCleanHelper.CleanWindowsUpdateCacheAsync(),
                                CancellationToken.None);
                            break;
                        case 3: // 回收站
                            await SystemCleanHelper.EmptyRecycleBinAsync();
                            break;
                        case 7: // 希沃白板（修正ID匹配）
                            var easiNotePaths = SystemCleanHelper.GetEasiNote5Temps();
                            await SystemCleanHelper.CleanPathsAsync(easiNotePaths, new Progress<int>(p =>
                                ProgressValue = (completedItems * 100 + p) / totalItems));
                            break;
                        default: // 普通清理（按优先级分配资源）
                            if (!string.IsNullOrEmpty(project.TargetPath) && Directory.Exists(project.TargetPath))
                            {
                                await SystemCleanHelper.CleanDirectoryAsync(project.TargetPath,
                                    new Progress<int>(p => ProgressValue = (completedItems * 100 + p) / totalItems));
                            }
                            break;
                    }

                    completedItems++;
                    IsIndeterminate = false;
                    ProgressValue = (completedItems * 100) / totalItems;
                }

                StatusMessage = "清理完成！";
            }
            catch (Exception ex)
            {
                ErrorMessage = $"清理失败: {ex.Message}";
                Log.Error($"清理过程出错: {ex}");
            }
            finally
            {
                IsCleaning = false;
                IsIndeterminate = false;
            }
        }
    }
}