using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks.Dataflow;

namespace MFSC.Helpers
{
    public partial class SystemCleanHelper
    {
        private static readonly char _dirSeparator = Path.DirectorySeparatorChar;
        private static readonly string _basePathPrefix;
        private const int MAX_PARALLELISM = 32;

        static SystemCleanHelper()
        {
            try
            {
                string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                _basePathPrefix = Path.Combine(appData, "Seewo", "EasiNote5", "Data");
                Log.Info($"EasiNote5基础路径初始化: {_basePathPrefix}");
            }
            catch (Exception ex)
            {
                Log.Error($"初始化EasiNote5基础路径失败: {ex.Message}");
                _basePathPrefix = string.Empty;
            }
        }

        public static List<string> GetEasiNote5Temps()
        {
            string targetPath = _basePathPrefix;
            if (string.IsNullOrEmpty(targetPath))
            {
                Log.Warn("EasiNote5基础路径无效，无法获取临时目录");
                return [];
            }

            try
            {
                if (!Directory.Exists(targetPath))
                {
                    Log.Info($"EasiNote5目录不存在: {targetPath}");
                    return [];
                }

                // 流式筛选，避免ToList()内存占用
                return [.. Directory.EnumerateDirectories(targetPath).Where(IsLastDirectory11Digits)];
            }
            catch (IOException ex)
            {
                Log.Error($"IO错误: 获取EasiNote5临时目录失败 - {ex.Message}");
                return [];
            }
            catch (UnauthorizedAccessException ex)
            {
                Log.Error($"权限错误: 无法访问EasiNote5目录 - {ex.Message}");
                return [];
            }
        }

        private static bool IsLastDirectory11Digits(string fullPath)
        {
            int lastSepIndex = fullPath.LastIndexOf(_dirSeparator);
            int startIndex = lastSepIndex + 1;

            if (startIndex >= fullPath.Length || fullPath.Length - startIndex != 11)
                return false;

            for (int i = 0; i < 11; i++)
            {
                char c = fullPath[startIndex + i];
                if (c < '0' || c > '9')
                    return false;
            }
            return true;
        }

        /// <summary>
        /// 激进模式清理：32线程并行+IO优先级提升+内存优化
        /// </summary>
        public static async Task CleanDirectoryAsync(string path, IProgress<int> progress, CancellationToken cancellationToken = default)
        {
            if (!Directory.Exists(path))
            {
                progress.Report(100);
                return;
            }

            // 流式枚举（无ToList()减少内存占用）
            var files = Directory.EnumerateFiles(path, "*.*", SearchOption.AllDirectories);
            var dirs = Directory.EnumerateDirectories(path, "*", SearchOption.AllDirectories).Reverse().ToList();

            // 预计算总数（避免重复枚举）
            long totalFiles = files.LongCount();
            long totalItems = totalFiles + dirs.Count;
            long processed = 0;

            if (totalFiles > 0)
            {
                // 使用TPL Dataflow控制并行度，比Parallel更高效
                var fileBlock = new ActionBlock<string>(
                    async file =>
                    {
                        try
                        {
                            // 批量移除属性（仅当需要时）
                            var attr = File.GetAttributes(file);
                            if ((attr & (FileAttributes.ReadOnly | FileAttributes.System)) != 0)
                            {
                                File.SetAttributes(file, attr & ~(FileAttributes.ReadOnly | FileAttributes.System));
                            }
                            File.Delete(file);
                        }
                        catch (Exception ex) when (ex is not OperationCanceledException)
                        {
                            Log.Warn($"无法删除文件 {file}: {ex.Message}");
                        }
                        finally
                        {
                            // 原子操作更新进度（减少CPU开销）
                            long current = Interlocked.Increment(ref processed);
                            progress.Report((int)(current * 100 / totalItems));
                        }
                    },
                    new ExecutionDataflowBlockOptions
                    {
                        MaxDegreeOfParallelism = MAX_PARALLELISM,
                        CancellationToken = cancellationToken,
                        // 提升IO线程优先级
                        TaskScheduler = TaskScheduler.Default
                    }
                );

                // 流式加载文件到处理块（内存友好）
                foreach (var file in files)
                {
                    if (cancellationToken.IsCancellationRequested) break;
                    await fileBlock.SendAsync(file, cancellationToken);
                }
                fileBlock.Complete();
                await fileBlock.Completion;
            }

            // 目录删除（保持顺序，避免父目录先删）
            foreach (var dir in dirs)
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    Directory.Delete(dir, false);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    Log.Warn($"无法删除目录 {dir}: {ex.Message}");
                }
                finally
                {
                    long current = Interlocked.Increment(ref processed);
                    progress.Report((int)(current * 100 / totalItems));
                }
            }
        }

        public static async Task CleanPathsAsync(List<string> paths, IProgress<int> progress, CancellationToken cancellationToken = default)
        {
            if (paths.Count == 0)
            {
                progress.Report(100);
                return;
            }

            var total = paths.Count;
            // 为每个路径绑定索引（避免并行时IndexOf的问题）
            var pathsWithIndex = paths.Select((path, index) => new { Path = path, Index = index }).ToList();

            // 使用分区器并行处理，无需显式指定类型参数（编译器可正确推断）
            await Parallel.ForEachAsync(
                source: pathsWithIndex,
                parallelOptions: new ParallelOptions
                {
                    MaxDegreeOfParallelism = Environment.ProcessorCount, // 控制并行度
                    CancellationToken = cancellationToken // 支持外部取消
                },
                body: async (item, token) =>
                {
                    // 使用预绑定的索引，确保准确
                    await CleanDirectoryAsync(
                        item.Path,
                        new Progress<int>(p =>
                        {
                            // 计算整体进度（当前路径的进度占比 + 已完成路径的占比）
                            var overallProgress = (item.Index * 100 + p) / total;
                            progress.Report(overallProgress);
                        }),
                        token
                    );
                }
            );
        }

        public static async Task CleanWindowsUpdateCacheAsync()
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "dism.exe",
                    Arguments = "/online /cleanup-image /startcomponentcleanup /resetbase", // 激进清理参数
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };

            process.Start();
            var outputTask = process.StandardOutput.ReadToEndAsync();
            var errorTask = process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            var output = await outputTask;
            var error = await errorTask;

            if (!string.IsNullOrEmpty(error))
                throw new InvalidOperationException($"DISM操作失败: {error}");

            Log.Info($"DISM清理完成: {output}");
        }

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "SYSLIB1054:使用 “LibraryImportAttribute” 而不是 “DllImportAttribute” 在编译时生成 P/Invoke 封送代码", Justification = "<挂起>")]
        private static extern int SHEmptyRecycleBin(IntPtr handle, string root, int flags);

        const int SHERB_NOCONFIRMATION = 0x000001;
        const int SHERB_NOPROGRESSUI = 0x000002;
        const int SHERB_NOSOUND = 0x000004;

        public static Task EmptyRecycleBinAsync()
        {
            return Task.Run(() =>
            {
                try
                {
                    // 提升线程优先级
                    var currentThread = System.Threading.Thread.CurrentThread;
                    var oldPriority = currentThread.Priority;
                    currentThread.Priority = System.Threading.ThreadPriority.AboveNormal;
                    _ = SHEmptyRecycleBin(IntPtr.Zero, null, SHERB_NOCONFIRMATION | SHERB_NOPROGRESSUI | SHERB_NOSOUND);

                    currentThread.Priority = oldPriority; // 恢复优先级
                }
                catch (Exception ex)
                {
                    Log.Error($"清空回收站失败: {ex.Message}");
                    throw;
                }
            }, CancellationToken.None);
        }
    }
}