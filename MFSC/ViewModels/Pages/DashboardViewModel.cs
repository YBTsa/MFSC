using MFSC.Helpers;
using System.Diagnostics;
using System.Windows.Input;
using System.Windows.Threading;

namespace MFSC.ViewModels.Pages
{
    public partial class DashboardViewModel : ObservableObject
    {
        private readonly CancellationTokenSource _cts = new();
        private readonly Dispatcher _dispatcher; // 新增UI调度器

        // 字段缓存，减少属性更新次数
        [ObservableProperty] private int _cpuUsage;
        [ObservableProperty] private int _memoryUsage;
        [ObservableProperty] private int _diskUsage;
        [ObservableProperty] private int _networkUploadUsage;
        [ObservableProperty] private int _networkDownloadUsage;
        [ObservableProperty] private int _gpuUsage;
        [ObservableProperty] private int _processCount;

        public ICommand LinkToGithubCommand { get; }
        public ICommand LinkToWebsiteCommand { get; }

        public DashboardViewModel()
        {
            _dispatcher = Dispatcher.CurrentDispatcher; // 获取UI线程调度器
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

        // 优化的监控循环（统一周期+UI线程更新）
        private async Task UpdateUsageLoop(CancellationToken token)
        {
            try
            {
                // 首次网络采样
                SystemInfoHelper.SampleNetworkStats();
                await Task.Delay(750, token).ConfigureAwait(false);

                while (!token.IsCancellationRequested)
                {
                    // 同步获取所有数据（无内部延迟）
                    var cpu = SystemInfoHelper.GetCpuUsage();
                    var memory = SystemInfoHelper.GetRamUsage();
                    var disk = SystemInfoHelper.GetDiskUsage();
                    var gpu = SystemInfoHelper.GetGpuUsage();
                    var processCount = SystemInfoHelper.GetProcessesCount();

                    // 基于上次采样计算网络流量
                    var networkDownload = SystemInfoHelper.CalculateNetworkDownload();
                    var networkUpload = SystemInfoHelper.CalculateNetworkUpload();

                    // 强制在UI线程更新属性
                    _dispatcher.Invoke(() =>
                    {
                        CpuUsage = cpu;
                        MemoryUsage = memory;
                        DiskUsage = disk;
                        GpuUsage = gpu;
                        ProcessCount = processCount;
                        NetworkDownloadUsage = networkDownload;
                        NetworkUploadUsage = networkUpload;
                    });

                    // 采样下次网络数据，等待固定周期
                    SystemInfoHelper.SampleNetworkStats();
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