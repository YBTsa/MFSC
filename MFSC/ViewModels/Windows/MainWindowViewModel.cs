using System.Collections.ObjectModel;
using Wpf.Ui.Controls;

namespace MFSC.ViewModels.Windows
{
    public partial class MainWindowViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _applicationTitle = "班级PC管理器";

        [ObservableProperty]
        private ObservableCollection<object> _menuItems =
        [
            new NavigationViewItem()
            {
                Content = "主页",
                Icon = new SymbolIcon { Symbol = SymbolRegular.Home24 },
                TargetPageType = typeof(Views.Pages.DashboardPage)
            },
            new NavigationViewItem()
            {
                Content = "清理",
                Icon = new SymbolIcon { Symbol = SymbolRegular.TopSpeed24},
                TargetPageType = typeof(Views.Pages.CleanerPage)
            },
            new NavigationViewItem()
            {
                Content = "网站屏蔽器",
                Icon = new SymbolIcon { Symbol = SymbolRegular.WebAsset24 },
                TargetPageType = typeof(Views.Pages.WebsiteBlockerPage)
            }
        ];

        [ObservableProperty]
        private ObservableCollection<object> _footerMenuItems =
        [
            new NavigationViewItem()
            {
                Content = "设置",
                Icon = new SymbolIcon { Symbol = SymbolRegular.Settings24 },
                TargetPageType = typeof(Views.Pages.SettingsPage)
            }
        ];

        [ObservableProperty]
        private ObservableCollection<MenuItem> _trayMenuItems =
        [
            new MenuItem { Header = "主页", Tag = "tray_home" }
        ];
    }
}
