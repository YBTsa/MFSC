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
            // 直接调用已初始化的计数器，减少对象创建
            return (int)_cpuCounter.NextValue();
        }
        #endregion

        #region 内存使用率（替换WMI为P/Invoke，速度提升10倍+）
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
            // 直接调用内核API，替代缓慢的WMI查询
            var memStatus = new MEMORYSTATUSEX { dwLength = (uint)Marshal.SizeOf<MEMORYSTATUSEX>() };
            return GlobalMemoryStatusEx(ref memStatus) ? (int)memStatus.dwMemoryLoad : 0;
        }
        #endregion

        #region 磁盘使用率（复用计数器）
        public static int GetDiskUsage()
        {
            // 限制最大值为100，保持原逻辑
            return (int)Math.Min(_diskCounter.NextValue(), 100);
        }
        #endregion

        #region 网络使用率（缓存活跃接口，减少LINQ开销）
        // 缓存活跃网络接口（1秒更新一次）
        private static IEnumerable<NetworkInterface> GetActiveNetInterfaces()
        {
            var now = DateTime.UtcNow;
            // 超过缓存有效期则重新获取
            if (now - _lastNetInterfaceUpdate > TimeSpan.FromMilliseconds(NetInterfaceCacheMs) || _activeNetInterfaces == null)
            {
                _activeNetInterfaces = NetworkInterface.GetAllNetworkInterfaces();
                _lastNetInterfaceUpdate = now;
            }
            // 循环筛选替代LINQ（减少委托开销）
            foreach (var ni in _activeNetInterfaces)
            {
                if (ni.OperationalStatus == OperationalStatus.Up)
                    yield return ni;
            }
        }

        private static long GetTotalBytesReceived()
        {
            long total = 0;
            // 循环累加替代LINQ Sum（减少迭代器开销）
            foreach (var ni in GetActiveNetInterfaces())
            {
                total += ni.GetIPv4Statistics().BytesReceived;
            }
            return total;
        }

        private static long GetTotalBytesSent()
        {
            long total = 0;
            // 循环累加替代LINQ Sum（减少迭代器开销）
            foreach (var ni in GetActiveNetInterfaces())
            {
                total += ni.GetIPv4Statistics().BytesSent;
            }
            return total;
        }

        public static async Task<int> GetNetworkDownloadUsage()
        {
            var firstBytes = GetTotalBytesReceived();
            await Task.Delay(500).ConfigureAwait(false); // 保持原延迟
            var secondBytes = GetTotalBytesReceived();
            return (int)((secondBytes - firstBytes) / 1024 * 2); // 保持原转换逻辑
        }

        public static async Task<int> GetNetworkUploadUsage()
        {
            var firstBytes = GetTotalBytesSent();
            await Task.Delay(500).ConfigureAwait(false); // 保持原延迟
            var secondBytes = GetTotalBytesSent();
            return (int)((secondBytes - firstBytes) / 1024 * 2); // 保持原转换逻辑
        }
        #endregion

        #region 进程计数

        public static async Task<int> GetProcessesCount()
        {
            return await Task.Run(() =>
            {
                return Process.GetProcesses().Length;
            }).ConfigureAwait(false);
        }
        #endregion

        #region GPU使用率（缓存计数器，减少重复初始化）
        private static List<PerformanceCounter> InitializeGpuCounters()
        {
            try
            {
                var category = new PerformanceCounterCategory("GPU Engine");
                var counterNames = category.GetInstanceNames();
                var counters = new List<PerformanceCounter>();

                // 循环筛选替代LINQ（减少委托和迭代器开销）
                foreach (var name in counterNames)
                {
                    if (name.EndsWith("engtype_3D"))
                    {
                        foreach (var counter in category.GetCounters(name))
                        {
                            if (counter.CounterName == "Utilization Percentage")
                            {
                                counters.Add(counter);
                                counter.NextValue(); // 预热计数器
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

        public static async Task<int> GetGpuUsage()
        {
            if (_gpuCounters.Count == 0)
                return 0;

            await Task.Delay(1000).ConfigureAwait(false); // 保持原延迟

            float total = 0;
            // 循环累加替代ForEach（减少委托开销）
            foreach (var counter in _gpuCounters)
            {
                total += counter.NextValue();
            }
            return (int)total; // 保持原逻辑
        }
        #endregion
    }
}