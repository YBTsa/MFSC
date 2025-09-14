using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Threading.Tasks;
using Wpf.Ui.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Windows;

namespace MFSC.ViewModels.Pages
{
    // 网站信息模型，用于统一管理网站数据
    public record class WebsiteInfo
    {
        public string Name { get; set; } // 网站名称
        public string Domain { get; set; } // 要屏蔽的域名
        public bool IsBlocked { get; set; } // 是否屏蔽
    }

    public partial class WebsiteBlockerViewModel : ObservableObject
    {
        // 网站集合（用于绑定UI）
        public ObservableCollection<WebsiteInfo> Websites { get; } = new()
        {
            new WebsiteInfo { Name = "哔哩哔哩", Domain = "www.bilibili.com" },
            new WebsiteInfo { Name = "优酷视频", Domain = "www.youku.com" },
            new WebsiteInfo { Name = "腾讯视频", Domain = "v.qq.com" },
            new WebsiteInfo { Name = "抖音", Domain = "www.douyin.com" },
            new WebsiteInfo { Name = "快手", Domain = "www.kuaishou.com" },
            new WebsiteInfo { Name = "爱奇艺", Domain = "www.iqiyi.com" },
            new WebsiteInfo { Name = "搜狐视频", Domain = "tv.sohu.com" },
            new WebsiteInfo { Name = "芒果视频", Domain = "www.mgtv.com" }
        };

        [ObservableProperty]
        private string _statusMessage = string.Empty; // 状态提示信息

        [ObservableProperty]
        private bool _isProcessing; // 是否正在处理

        private readonly string _hostsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.System),
            @"drivers\etc\hosts"
        );

        public WebsiteBlockerViewModel()
        {
            LoadBlockedStatus();
        }

        // 加载已屏蔽状态
        private void LoadBlockedStatus()
        {
            try
            {
                if (!File.Exists(_hostsPath))
                {
                    StatusMessage = " hosts 文件不存在";
                    return;
                }

                var hostsContent = File.ReadAllText(_hostsPath);

                foreach (var site in Websites)
                {
                    site.IsBlocked = hostsContent.Contains(site.Domain);
                }
            }
            catch (UnauthorizedAccessException)
            {
                StatusMessage = "没有权限访问 hosts 文件，请以管理员身份运行";
            }
            catch (IOException ex)
            {
                StatusMessage = $"读取 hosts 文件失败: {ex.Message}";
            }
            catch (Exception ex)
            {
                StatusMessage = $"加载失败: {ex.Message}";
            }
        }

        [RelayCommand]
        private async Task SaveAsync()
        {
            IsProcessing = true;
            StatusMessage = "正在保存...";

            try
            {
                // 检查管理员权限
                if (!IsRunningAsAdmin())
                {
                    StatusMessage = "需要管理员权限才能修改 hosts 文件";
                    IsProcessing = false;
                    return;
                }

                // 备份 hosts 文件
                BackupHostsFile();

                // 读取现有内容
                var lines = (await File.ReadAllLinesAsync(_hostsPath)).ToList();

                // 处理每个网站的屏蔽状态
                foreach (var site in Websites)
                {
                    var targetLine = $"127.0.0.1 {site.Domain}";
                    var exists = lines.Any(l => l.Trim() == targetLine);

                    // 需要屏蔽且不存在则添加
                    if (site.IsBlocked && !exists)
                    {
                        lines.Add(targetLine);
                    }
                    // 不需要屏蔽但存在则移除
                    else if (!site.IsBlocked && exists)
                    {
                        lines.RemoveAll(l => l.Trim() == targetLine);
                    }
                }

                // 写入修改后的内容
                await File.WriteAllLinesAsync(_hostsPath, lines);

                // 刷新DNS缓存
                await FlushDnsAsync();

                StatusMessage = "保存成功，已生效";
            }
            catch (UnauthorizedAccessException)
            {
                StatusMessage = "权限不足，无法修改 hosts 文件";
            }
            catch (IOException ex)
            {
                StatusMessage = $"文件操作失败: {ex.Message}";
            }
            catch (Exception ex)
            {
                StatusMessage = $"保存失败: {ex.Message}";
            }
            finally
            {
                IsProcessing = false;
            }
        }

        // 备份 hosts 文件
        private void BackupHostsFile()
        {
            var backupPath = $"{_hostsPath}.bak";
            if (!File.Exists(backupPath))
            {
                File.Copy(_hostsPath, backupPath, overwrite: false);
            }
        }

        // 刷新DNS缓存
        private async Task FlushDnsAsync()
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "ipconfig.exe",
                    Arguments = "/flushdns",
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            process.Start();
            await process.WaitForExitAsync();
        }

        // 检查是否以管理员身份运行
        private bool IsRunningAsAdmin()
        {
            var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
            var principal = new System.Security.Principal.WindowsPrincipal(identity);
            return principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
        }
    }
}