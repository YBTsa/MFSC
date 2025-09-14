using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

namespace MFSC.Helpers
{
    public class SystemInfoHelper
    {
        private static readonly PerformanceCounter cpuCounter;
        private static readonly PerformanceCounter diskCounter;

        static SystemInfoHelper()
        {
            cpuCounter = new PerformanceCounter("Processor Information", "% Processor Time", "_Total");
            diskCounter = new PerformanceCounter("PhysicalDisk", "% Disk Time", "_Total");
            cpuCounter.NextValue();
            diskCounter.NextValue();
        }
        public static int GetCpuUsage()
        {
            return (int)cpuCounter.NextValue();
        }

        public static int GetRamUsage()
        {
            var wql = new ObjectQuery("SELECT FreePhysicalMemory, TotalVisibleMemorySize FROM Win32_OperatingSystem");
            var searcher = new ManagementObjectSearcher(wql);
            foreach (ManagementObject queryObj in searcher.Get().Cast<ManagementObject>())
            {
                var freeMemory = Convert.ToUInt64(queryObj["FreePhysicalMemory"]);
                var totalMemory = Convert.ToUInt64(queryObj["TotalVisibleMemorySize"]);
                var usedMemory = totalMemory - freeMemory;
                return (int)((usedMemory * 100) / totalMemory);
            }
            return 0;
        }

        public static int GetDiskUsage()
        {
            return (int)Math.Min(diskCounter.NextValue(), 100);
        }

        public static async Task<int> GetNetworkDownloadUsage()
        {
            var firstBytes = GetTotalBytesReceived(); // Get total bytes received at a point in time
            await Task.Delay(500); // Non-blocking delay
            var secondBytes = GetTotalBytesReceived(); // Get total bytes received after the 500ms
            return (int)((secondBytes - firstBytes) / 1024); // Convert Bytes to KB
        }

        // Similar to GetNetworkDownloadUsage but for upload
        public static async Task<int> GetNetworkUploadUsage()
        {
            var firstBytes = GetTotalBytesSent();
            await Task.Delay(500); // Non-blocking delay
            var secondBytes = GetTotalBytesSent();
            return (int)((secondBytes - firstBytes) / 1024);
        }

        // Get total bytes received by all network interfaces
        private static long GetTotalBytesReceived()
        {
            return NetworkInterface.GetAllNetworkInterfaces()
                .Where(ni => ni.OperationalStatus == OperationalStatus.Up)
                .Sum(ni => ni.GetIPv4Statistics().BytesReceived);
        }

        // Same as GetTotalBytesReceived but for sent bytes
        private static long GetTotalBytesSent()
        {
            return NetworkInterface.GetAllNetworkInterfaces()
                .Where(ni => ni.OperationalStatus == OperationalStatus.Up)
                .Sum(ni => ni.GetIPv4Statistics().BytesSent);
        }
        public static async Task<int> GetProcessesCount()
        {
            return await Task.Run(() => Process.GetProcesses().Length);
        }

        public static async Task<int> GetGpuUsage()
        {
            try
            {
                var category = new PerformanceCounterCategory("GPU Engine");
                var counterNames = category.GetInstanceNames();
                var gpuCounters = new List<PerformanceCounter>();
                var result = 0f;

                foreach (var counterName in counterNames)
                {
                    if (counterName.EndsWith("engtype_3D"))
                    {
                        foreach (var counter in category.GetCounters(counterName))
                        {
                            if (counter.CounterName == "Utilization Percentage")
                            {
                                gpuCounters.Add(counter);
                            }
                        }
                    }
                }

                gpuCounters.ForEach(x =>
                {
                    _ = x.NextValue();
                });
                await Task.Delay(1000);
                gpuCounters.ForEach(x =>
                {
                    result += x.NextValue();
                });
                return (int)result;
            }
            catch
            {
                return 0;
            }
        }
    }
}
