using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Diagnostics;

namespace MFSC.Helpers
{
    public partial class SystemCleanHelper
    {
        /// <summary>
        /// 获取EasiNote5中目录名符合11位数字格式的临时目录
        /// </summary>
        /// <returns>匹配的目录路径列表，若发生错误返回空列表</returns>
        public static List<string> GetEasiNote5Temps()
        {
            try
            {
                // 1. 构建目标路径
                string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string targetPath = Path.Combine(appDataPath, @"Seewo\EasiNote5\Data");

                // 验证路径有效性（为空或空白）
                ArgumentNullException.ThrowIfNullOrWhiteSpace(targetPath);
                Debug.WriteLine($"EasiNote5 temp path: {targetPath}");

                // 2. 检查目录是否存在（提前规避DirectoryNotFoundException）
                if (!Directory.Exists(targetPath))
                {
                    Debug.WriteLine($"EasiNote5 temp directory not found: {targetPath}");
                    return [];
                }

                // 3. 枚举目录并筛选（使用EnumerateDirectories延迟加载提升性能）
                return [.. Directory.EnumerateDirectories(targetPath).Where(dir => IsLastDirectoryMatch11Digits(dir))];
            }
            catch (Exception ex)
            {
                // 记录异常信息便于调试，避免静默失败
                Debug.WriteLine($"Failed to get EasiNote5 temps: {ex.Message}");
                // 可根据业务需求选择抛出或返回空列表（此处返回空列表更友好）
                return [];
            }
        }

        /// <summary>
        /// 检查路径中最后一级目录名是否为11位数字
        /// </summary>
        /// <param name="fullPath">完整目录路径</param>
        /// <returns>如果最后一级目录是11位数字则返回true，否则返回false</returns>
        private static bool IsLastDirectoryMatch11Digits(string fullPath)
        {
            // 获取最后一个反斜杠的位置（处理路径分隔符）
            int lastBackslashIndex = fullPath.LastIndexOf(Path.DirectorySeparatorChar);

            // 特殊情况处理：路径中没有目录分隔符（理论上在本场景不会出现）
            if (lastBackslashIndex == -1)
            {
                return PhoneNumberRegex().IsMatch(fullPath);
            }

            // 计算最后一级目录名的起始索引和长度（修复原代码的Span长度错误）
            int startIndex = lastBackslashIndex + 1;
            int length = fullPath.Length - startIndex;

            // 防止索引越界（如路径以目录分隔符结尾的情况）
            if (startIndex >= fullPath.Length || length <= 0)
            {
                return false;
            }

            // 使用Span提取最后一级目录名并匹配正则
            return PhoneNumberRegex().IsMatch(fullPath.AsSpan(startIndex, length));
        }

        /// <summary>
        /// 生成匹配11位数字的正则表达式（仅匹配纯数字且长度为11）
        /// </summary>
        [GeneratedRegex(@"^\d{11}$")]
        private static partial Regex PhoneNumberRegex();
    }
}
