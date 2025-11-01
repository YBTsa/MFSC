using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;

namespace MFSC.Helpers
{
    public partial class SystemInfoHelper
    {
        // 静态计数器（全局复用，减少初始化开销）
        private static readonly PerformanceCounter _cpuCounter;
        private static readonly PerformanceCounter _diskCounter;
        private static readonly List<PerformanceCounter> _gpuCounters;

        // 网络采样缓存（新增）
        private static long _lastBytesReceived;
        private static long _lastBytesSent;
        private static DateTime _lastNetworkCheck = DateTime.MinValue;

        // 网络接口缓存（减少重复枚举开销）
        private static IEnumerable<NetworkInterface> _activeNetInterfaces;
        private static DateTime _lastNetInterfaceUpdate = DateTime.MinValue;
        private const int NetInterfaceCacheMs = 1000; // 接口缓存有效期

        // 静态构造函数一次性初始化所有计数器
        static SystemInfoHelper()
        {
            // CPU计数器初始化+预热
            _cpuCounter = new PerformanceCounter("Processor Information", "% Processor Time", "_Total");
            _cpuCounter.NextValue(); // 首次调用返回0，预热

            // 磁盘计数器初始化+预热
            _diskCounter = new PerformanceCounter("PhysicalDisk", "% Disk Time", "_Total");
            _diskCounter.NextValue(); // 首次调用返回0，预热

            // GPU计数器初始化+缓存（仅一次）
            _gpuCounters = InitializeGpuCounters();
        }

        #region CPU使用率（复用计数器）
        public static int GetCpuUsage()
        {
            return (int)_cpuCounter.NextValue();
        }
        #endregion

        #region 内存使用率（P/Invoke实现）
        [StructLayout(LayoutKind.Sequential)]
        private struct MEMORYSTATUSEX
        {
            public uint dwLength;
            public uint dwMemoryLoad;
            public ulong ullTotalPhys;
            public ulong ullAvailPhys;
            public ulong ullTotalPageFile;
            public ulong ullAvailPageFile;
            public ulong ullTotalVirtual;
            public ulong ullAvailVirtual;
            public ulong ullAvailExtendedVirtual;
        }

        [LibraryImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX lpBuffer);

        public static int GetRamUsage()
        {
            var memStatus = new MEMORYSTATUSEX { dwLength = (uint)Marshal.SizeOf<MEMORYSTATUSEX>() };
            return GlobalMemoryStatusEx(ref memStatus) ? (int)memStatus.dwMemoryLoad : 0;
        }
        #endregion

        #region 磁盘使用率（复用计数器）
        public static int GetDiskUsage()
        {
            return (int)Math.Min(_diskCounter.NextValue(), 100);
        }
        #endregion

        #region 网络使用率（采样-计算分离模式，移除内部延迟）
        // 缓存活跃网络接口（1秒更新一次）
        private static IEnumerable<NetworkInterface> GetActiveNetInterfaces()
        {
            var now = DateTime.UtcNow;
            if (now - _lastNetInterfaceUpdate > TimeSpan.FromMilliseconds(NetInterfaceCacheMs) || _activeNetInterfaces == null)
            {
                _activeNetInterfaces = NetworkInterface.GetAllNetworkInterfaces();
                _lastNetInterfaceUpdate = now;
            }
            foreach (var ni in _activeNetInterfaces)
            {
                if (ni.OperationalStatus == OperationalStatus.Up)
                    yield return ni;
            }
        }

        private static long GetTotalBytesReceived()
        {
            long total = 0;
            foreach (var ni in GetActiveNetInterfaces())
                total += ni.GetIPv4Statistics().BytesReceived;
            return total;
        }

        private static long GetTotalBytesSent()
        {
            long total = 0;
            foreach (var ni in GetActiveNetInterfaces())
                total += ni.GetIPv4Statistics().BytesSent;
            return total;
        }

        // 新增：采样当前网络状态（无延迟）
        public static void SampleNetworkStats()
        {
            _lastBytesReceived = GetTotalBytesReceived();
            _lastBytesSent = GetTotalBytesSent();
            _lastNetworkCheck = DateTime.UtcNow;
        }

        // 新增：基于上次采样计算下载量（无延迟）
        public static int CalculateNetworkDownload()
        {
            var current = GetTotalBytesReceived();
            return (int)((current - _lastBytesReceived) / 1024 * 2);
        }

        // 新增：基于上次采样计算上传量（无延迟）
        public static int CalculateNetworkUpload()
        {
            var current = GetTotalBytesSent();
            return (int)((current - _lastBytesSent) / 1024 * 2);
        }
        #endregion

        #region 进程计数（改为同步方法）
        public static int GetProcessesCount()
        {
            return Process.GetProcesses().Length;
        }
        #endregion

        #region GPU使用率（移除内部延迟，改为同步）
        private static List<PerformanceCounter> InitializeGpuCounters()
        {
            try
            {
                var category = new PerformanceCounterCategory("GPU Engine");
                var counterNames = category.GetInstanceNames();
                var counters = new List<PerformanceCounter>();

                foreach (var name in counterNames)
                {
                    if (name.EndsWith("engtype_3D"))
                    {
                        foreach (var counter in category.GetCounters(name))
                        {
                            if (counter.CounterName == "Utilization Percentage")
                            {
                                counters.Add(counter);
                                counter.NextValue(); // 预热
                            }
                        }
                    }
                }
                return counters;
            }
            catch
            {
                return [];
            }
        }

        public static int GetGpuUsage() // 移除async和延迟
        {
            if (_gpuCounters.Count == 0)
                return 0;

            float total = 0;
            foreach (var counter in _gpuCounters)
                total += counter.NextValue();
            return (int)total;
        }
        #endregion
    }
}