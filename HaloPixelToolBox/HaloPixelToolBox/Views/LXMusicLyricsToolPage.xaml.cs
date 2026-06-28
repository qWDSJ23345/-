using Microsoft.UI.Xaml.Controls;
using HaloPixelToolBox.ViewModels;

namespace HaloPixelToolBox.Views;

/// <summary>
/// 落雪音乐歌词工具页面
/// </summary>
public sealed partial class LXMusicLyricsToolPage : Page
{
    public LXMusicLyricsToolPageViewModel? ViewModel { get; set; }

    public LXMusicLyricsToolPage()
    {
        this.InitializeComponent();
        
        // 初始化 ViewModel
        ViewModel = new LXMusicLyricsToolPageViewModel();
        this.DataContext = ViewModel;
    }

    /// <summary>
    /// 页面卸载时清理资源
    /// </summary>
    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        base.OnNavigatedFrom(e);
        
        // 停止和清理 ViewModel
        ViewModel?.Stop();
    }
}
