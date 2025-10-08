using MFSC.Helpers;
using System.Diagnostics;
using System.Windows.Input;

namespace MFSC.ViewModels.Pages
{
    public partial class DashboardViewModel : ObservableObject
    {
        private readonly CancellationTokenSource _cts = new();

        // 字段缓存，减少属性更新次数
        [ObservableProperty]
        private int _cpuUsage;
        [ObservableProperty]
        private int _memoryUsage;
        [ObservableProperty]
        private int _diskUsage;
        [ObservableProperty]
        private int _networkUploadUsage;
        [ObservableProperty]
        private int _networkDownloadUsage;
        [ObservableProperty]
        private int _gpuUsage;
        [ObservableProperty]
        private int _processCount;
        public ICommand LinkToGithubCommand { get; }
        public ICommand LinkToWebsiteCommand { get; }

        public DashboardViewModel()
        {
            LinkToGithubCommand = new RelayCommand(LinkToGithub);
            LinkToWebsiteCommand = new RelayCommand(LinkToWebsite);
        }

        private void LinkToGithub() => Process.Start(new ProcessStartInfo("https://github.com/YBTsa/MFSC") { UseShellExecute = true });
        private void LinkToWebsite() => Process.Start(new ProcessStartInfo("https://www.youngbat.qzz.io") { UseShellExecute = true });

        // 启动监控循环（带取消机制）
        public void StartMonitoring()
        {
            _ = UpdateUsageLoop(_cts.Token);
        }

        // 优化的监控循环
        private async Task UpdateUsageLoop(CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    // 并行执行所有数据采集（无冗余Task.Run包装）
                    var cpuTask = Task.FromResult(SystemInfoHelper.GetCpuUsage());
                    var ramTask = Task.FromResult(SystemInfoHelper.GetRamUsage());
                    var diskTask = Task.FromResult(SystemInfoHelper.GetDiskUsage());
                    var gpuTask = SystemInfoHelper.GetGpuUsage();
                    var networkUploadTask = SystemInfoHelper.GetNetworkUploadUsage();
                    var networkDownloadTask = SystemInfoHelper.GetNetworkDownloadUsage();
                    var processCountTask = SystemInfoHelper.GetProcessesCount();

                    // 等待所有任务完成（总耗时取决于最慢的任务）
                    await Task.WhenAll(gpuTask, networkDownloadTask, networkUploadTask, cpuTask, ramTask, diskTask, processCountTask)
                        .ConfigureAwait(false);

                    // 更新属性（UI线程安全）
                    CpuUsage = cpuTask.Result;
                    MemoryUsage = ramTask.Result;
                    DiskUsage = diskTask.Result;
                    GpuUsage = gpuTask.Result;
                    ProcessCount = processCountTask.Result;
                    NetworkDownloadUsage = networkDownloadTask.Result;
                    NetworkUploadUsage = networkUploadTask.Result;

                    // 控制更新频率（总周期约500ms，比原来快一倍）
                    await Task.Delay(500, token).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
                // 正常取消，忽略
            }
        }
    }
}