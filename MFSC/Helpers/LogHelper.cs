using System;
using System.IO;
using System.Text;

namespace MFSC.Helpers
{
    // 日志级别枚举
    public enum LogLevel
    {
        Debug,
        Info,
        Warn,
        Error
    }

    // 对外暴露的静态访问入口，直接通过Log调用
    public static class Log
    {
        // 内部持有LogHelper的单例实例
        private static readonly LogHelper _instance = LogHelper.Instance;

        // 不同级别日志的快捷方法
        public static void Debug(string message) => _instance.WriteLog(LogLevel.Debug, message);
        public static void Info(string message) => _instance.WriteLog(LogLevel.Info, message);
        public static void Warn(string message) => _instance.WriteLog(LogLevel.Warn, message);
        public static void Error(string message) => _instance.WriteLog(LogLevel.Error, message);

        // 基础写入方法（支持指定级别）
        public static void WriteLog(LogLevel level, string message) => _instance.WriteLog(level, message);

        // 内部实现类（隐藏具体实现）
        private sealed class LogHelper : IDisposable
        {
            // 单例实例（内部使用）
            internal static LogHelper Instance => _instance.Value;
            private static readonly Lazy<LogHelper> _instance = new(() => new LogHelper());

            // 路径相关字段
            private readonly string _baseLogPath; // 基础日志目录
            private string? _currentDateFolder;    // 当前日期文件夹（yyyy-MM-dd）
            private string? _currentLogPath;       // 完整日志路径（基础目录+日期文件夹）
            private readonly string _baseFileName;

            // 线程安全锁
            private readonly Lock _lock = new();

            // 文件操作对象
            private FileStream? _fileStream;
            private StreamWriter? _streamWriter;
            private long _currentFileSize;

            // 常量定义
            private static readonly int MaxFileSize = 1024 * 1024 * 16; // 16MB
            private static readonly string BaseName = "MFSC";
            private static readonly string Ext = ".log";
            private static readonly Encoding LogEncoding = Encoding.UTF8;

            // 缓存字符串构建器（预分配更大容量减少扩容）
            private readonly StringBuilder _logBuilder = new(512);

            // 私有构造函数，确保只能通过单例访问
            private LogHelper()
            {
                // 基础日志目录（固定不变）
                _baseLogPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "MFSC", "Logs"
                );
                _baseFileName = $"{BaseName}{Ext}";

                // 初始化当前日期文件夹
                UpdateDateFolder();
                // 确保目录存在并初始化文件流
                InitializeFileStream();
            }

            // 更新日期文件夹（每天自动切换）
            private void UpdateDateFolder()
            {
                var currentDate = DateTime.Now.Date;
                _currentDateFolder = currentDate.ToString("yyyy-MM-dd");
                _currentLogPath = Path.Combine(_baseLogPath, _currentDateFolder);
            }

            private void InitializeFileStream()
            {
                Directory.CreateDirectory(_currentLogPath!);
                var fileName = GetNextUsableFileName();
                var fullPath = Path.Combine(_currentLogPath!, fileName);

                // 释放旧资源
                _streamWriter?.Dispose();
                _fileStream?.Dispose();

                // 创建新流（优化缓冲和文件共享设置）
                _fileStream = new FileStream(
                    fullPath,
                    FileMode.Append,
                    FileAccess.Write,
                    FileShare.Read,
                    bufferSize: 4096,
                    useAsync: false
                );

                _streamWriter = new StreamWriter(_fileStream, LogEncoding, 4096, leaveOpen: true);

                if (_fileStream == null || _streamWriter == null)
                    throw new NullReferenceException("File Stream or Stream Writer initialization failed!");

                _currentFileSize = _fileStream.Length;
            }

            private string GetNextUsableFileName()
            {
                int maxSequence = 0;
                string latestFile = _baseFileName;
                var baseNameLength = BaseName.Length;

                // 只扫描当前日期文件夹内的日志文件
                foreach (var filePath in Directory.EnumerateFiles(_currentLogPath!, $"{BaseName}*{Ext}"))
                {
                    var fileName = Path.GetFileNameWithoutExtension(filePath);

                    // 基础文件（无序号）
                    if (fileName.Length == baseNameLength && fileName == BaseName)
                    {
                        latestFile = _baseFileName;
                        continue;
                    }

                    // 带序号的文件（如MFSC_1）
                    if (fileName.Length > baseNameLength + 1 &&
                        fileName[baseNameLength] == '_' &&
                        int.TryParse(fileName.AsSpan(baseNameLength + 1), out int sequence) &&
                        sequence > maxSequence)
                    {
                        maxSequence = sequence;
                        latestFile = $"{BaseName}_{sequence}{Ext}";
                    }
                }

                // 检查最新文件是否需要轮转
                var checkPath = Path.Combine(_currentLogPath!, latestFile);
                if (File.Exists(checkPath))
                {
                    using var tempStream = new FileStream(checkPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    if (tempStream.Length >= MaxFileSize)
                    {
                        return maxSequence == 0 ? $"{BaseName}_1{Ext}" : $"{BaseName}_{maxSequence + 1}{Ext}";
                    }
                }

                return latestFile;
            }

            internal void WriteLog(LogLevel level, string message)
            {
                if (string.IsNullOrEmpty(message)) return;

                lock (_lock)
                {
                    // 跨天检查与处理
                    var currentDate = DateTime.Now.Date;
                    if (!_currentDateFolder!.Equals(currentDate.ToString("yyyy-MM-dd")))
                    {
                        UpdateDateFolder();
                        InitializeFileStream();
                    }

                    // 文件大小检查与轮转
                    if (_currentFileSize >= MaxFileSize)
                    {
                        InitializeFileStream();
                    }

                    // 构建日志内容（包含级别信息）
                    _logBuilder.Clear();
                    _logBuilder.Append('[');
                    _logBuilder.Append(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));
                    _logBuilder.Append("] [");
                    _logBuilder.Append(level.ToString().ToUpper()); // 级别大写显示
                    _logBuilder.Append("] ");
                    _logBuilder.Append(message);

                    var logLine = _logBuilder.ToString();
                    _streamWriter!.WriteLine(logLine);

                    // 更新缓存文件大小（精确计算字节数）
                    _currentFileSize += LogEncoding.GetByteCount(logLine) +
                                       LogEncoding.GetByteCount(Environment.NewLine);

                    // 条件性Flush（平衡性能与可靠性）
                    if (_currentFileSize % 4096 < 1024)
                    {
                        _streamWriter!.Flush();
                    }
                }
            }

            public void Dispose()
            {
                _streamWriter?.Flush();
                _streamWriter?.Dispose();
                _fileStream?.Dispose();
            }
        }
    }
}
