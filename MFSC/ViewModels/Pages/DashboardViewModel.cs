using MFSC.Helpers;
using System.Diagnostics;
using System.Threading;
using System.Windows.Input;

namespace MFSC.ViewModels.Pages
{
    public partial class DashboardViewModel : ObservableObject
    {
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
        private void LinkToGithub()
        {
            Process.Start("explorer.exe", "https://github.com/YBTsa");
        }
        private void LinkToWebsite()
        {
            Process.Start("explorer.exe", "https://www.youngbat.qzz.io");
        }
        public async Task UpdateUsage()
        {
            try
            {
                while (true)
                {
                    var cpuTask = Task.Run(() => SystemInfoHelper.GetCpuUsage());
                    var ramTask = Task.Run(() => SystemInfoHelper.GetRamUsage());
                    var diskTask = Task.Run(() => SystemInfoHelper.GetDiskUsage());
                    var gpuTask = Task.Run(() => SystemInfoHelper.GetGpuUsage());
                    var networkUploadTask = Task.Run(() => SystemInfoHelper.GetNetworkUploadUsage());
                    var networkDownloadTask = Task.Run(() => SystemInfoHelper.GetNetworkDownloadUsage());
                    var processCountTask = Task.Run(() => SystemInfoHelper.GetProcessesCount());
                    await Task.WhenAll(cpuTask, ramTask, diskTask, gpuTask, networkUploadTask, networkDownloadTask, processCountTask);
                    CpuUsage = cpuTask.Result;
                    MemoryUsage = ramTask.Result;
                    DiskUsage = diskTask.Result;
                    GpuUsage = gpuTask.Result;
                    NetworkUploadUsage = networkUploadTask.Result;
                    NetworkDownloadUsage = networkDownloadTask.Result;
                    ProcessCount = processCountTask.Result;
                    await Task.Delay(1000).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
                // ignore
            }
        }
    }
}
