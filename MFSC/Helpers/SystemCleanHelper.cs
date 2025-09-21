using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace MFSC.Helpers
{
    public partial class SystemCleanHelper
    {
        // 预编译路径分隔符
        private static readonly char _dirSeparator = Path.DirectorySeparatorChar;
        // 目标路径前缀（静态构造函数中初始化）
        private static readonly string _basePathPrefix;

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

        /// <summary>
        /// 获取EasiNote5中目录名符合11位数字格式的临时目录
        /// </summary>
        /// <returns>匹配的目录路径列表，若发生错误返回空列表</returns>
        public static List<string> GetEasiNote5Temps()
        {
            string targetPath = _basePathPrefix;

            // 基础路径无效直接返回
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

                var result = new List<string>();
                foreach (var dir in Directory.EnumerateDirectories(targetPath))
                {
                    if (IsLastDirectory11Digits(dir))
                    {
                        result.Add(dir);
                    }
                }

                Log.Info($"成功获取{result.Count}个EasiNote5临时目录");
                return result;
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

        /// <summary>
        /// 检查路径中最后一级目录名是否为11位数字
        /// </summary>
        private static bool IsLastDirectory11Digits(string fullPath)
        {
            int lastSepIndex = fullPath.LastIndexOf(_dirSeparator);
            int startIndex = lastSepIndex + 1;

            if (startIndex >= fullPath.Length)
                return false;

            int length = fullPath.Length - startIndex;
            if (length != 11)
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
            /// 清理目录（激进模式：并行处理+异步IO）
            /// </summary>
            public static async Task CleanDirectoryAsync(string path, IProgress<int> progress, CancellationToken cancellationToken = default)
            {
                if (!Directory.Exists(path))
                {
                    progress.Report(100);
                    return;
                }

                // 获取所有文件和目录（流式枚举提升性能）
                var files = Directory.EnumerateFiles(path, "*.*", System.IO.SearchOption.AllDirectories).ToList();
                var dirs = Directory.EnumerateDirectories(path, "*", System.IO.SearchOption.AllDirectories).Reverse().ToList();
                var totalItems = files.Count + dirs.Count;
                var processed = 0;

                // 并行删除文件（限制并发数避免系统IO瓶颈）
                if (files.Count > 0)
                {
                    await Parallel.ForEachAsync(files, new ParallelOptions
                    {
                        MaxDegreeOfParallelism = Environment.ProcessorCount * 2,
                        CancellationToken = cancellationToken
                    }, async (file, token) =>
                    {
                        try
                        {
                            // 异步删除文件
                            File.SetAttributes(file, FileAttributes.Normal); // 移除只读等属性
                            await Task.Run(() => { try { 
                                    File.Delete(file);
                                    }
                                catch (Exception ex) when (ex is not OperationCanceledException)
                                {
                                    Log.Warn($"无法删除文件 {file}: {ex.Message}");
                                }
                            }, token);
                        }
                        catch (Exception ex) when (ex is not OperationCanceledException)
                        {
                            Log.Warn($"无法删除文件 {file}: {ex.Message}");
                        }
                        finally
                        {
                            Interlocked.Increment(ref processed);
                            progress.Report((int)((double)processed / totalItems * 100));
                        }
                    });
                }

                // 删除空目录
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
                        processed++;
                        progress.Report((int)((double)processed / totalItems * 100));
                    }
                }
            }

            /// <summary>
            /// 批量清理路径列表
            /// </summary>
            public static async Task CleanPathsAsync(List<string> paths, IProgress<int> progress)
            {
                if (paths.Count == 0)
                {
                    progress.Report(100);
                    return;
                }

                var total = paths.Count;
                for (var i = 0; i < total; i++)
                {
                    await CleanDirectoryAsync(paths[i], new Progress<int>(p =>
                        progress.Report((i * 100 + p) / total)));
                }
            }

            /// <summary>
            /// 使用DISM清理Windows更新缓存（异步）
            /// </summary>
            public static async Task CleanWindowsUpdateCacheAsync()
            {
                using var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "dism.exe",
                        Arguments = "/online /cleanup-image /startcomponentcleanup",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    }
                };

                process.Start();

                // 异步读取输出（避免阻塞）
                var outputTask = process.StandardOutput.ReadToEndAsync();
                var errorTask = process.StandardError.ReadToEndAsync();

                // 等待进程完成
                await process.WaitForExitAsync();

                var output = await outputTask;
                var error = await errorTask;

                if (!string.IsNullOrEmpty(error))
                    throw new InvalidOperationException($"DISM操作失败: {error}");

                Log.Info($"DISM清理完成: {output}");
            }
        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]//声明API函数
#pragma warning disable IDE0079
#pragma warning disable SYSLIB1054
        private static extern int SHEmptyRecycleBin(IntPtr handle, string root, int falgs);
#pragma warning restore SYSLIB1054
#pragma warning restore IDE0079
        const int SHERB_NOCONFIRMATION = 0x000001;  // 整型常量在API中表地删除时没有确认对话框
        const int SHERB_NOPROGRESSUI = 0x000002;    // 在API中表示不显示删除进度条
        const int SHERB_NOSOUND = 0x000004;
        /// <summary>
        /// 清空回收站
        /// </summary>
        public static Task EmptyRecycleBinAsync()
            {
                return Task.Run(() =>
                {
                    try
                    {
                        _ = SHEmptyRecycleBin(IntPtr.Zero, null, SHERB_NOCONFIRMATION | SHERB_NOPROGRESSUI | SHERB_NOSOUND);
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"清空回收站失败: {ex.Message}");
                        throw;
                    }
                });
            }
        }
    }
