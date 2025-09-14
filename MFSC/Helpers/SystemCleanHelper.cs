using System;
using System.Collections.Generic;
using System.IO;

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
    }
}
