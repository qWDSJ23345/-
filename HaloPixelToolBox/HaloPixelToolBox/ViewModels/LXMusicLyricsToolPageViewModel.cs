using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using HaloPixelToolBox.Core.Utilities;
using HaloPixelToolBox.Profiles.CrossVersionProfiles;

namespace HaloPixelToolBox.ViewModels;

/// <summary>
/// 落雪音乐歌词工具页面的 ViewModel
/// 负责协调设备、API 和 UI 之间的交互
/// </summary>
public partial class LXMusicLyricsToolPageViewModel : ObservableObject
{
    // ==================== 可观测属性 ====================

    /// <summary>
    /// RGB 音响设备是否已就绪
    /// </summary>
    [ObservableProperty]
    private bool deviceReady;

    /// <summary>
    /// 落雪音乐 API 是否已连接
    /// </summary>
    [ObservableProperty]
    private bool lxmusicReady;

    /// <summary>
    /// 是否启用歌词显示功能
    /// </summary>
    [ObservableProperty]
    private bool enableLXMusicLyrics = LXMusicLyricsProfile.EnableLXMusicLyrics;

    /// <summary>
    /// 音乐暂停时是否切换回时钟界面
    /// </summary>
    [ObservableProperty]
    private bool switchBackWhenPause = LXMusicLyricsProfile.SwitchBackWhenPause;

    /// <summary>
    /// 切换回时钟界面的超时时间（秒）
    /// </summary>
    [ObservableProperty]
    private int switchBackTimeout = LXMusicLyricsProfile.SwitchBackTimeout;

    /// <summary>
    /// 落雪音乐连接状态描述
    /// </summary>
    [ObservableProperty]
    private string lxmusicStatus = "未连接";

    /// <summary>
    /// 当前歌曲信息（格式：歌曲名 - 艺术家）
    /// </summary>
    [ObservableProperty]
    private string currentSongInfo = string.Empty;

    /// <summary>
    /// 当前显示的歌词
    /// </summary>
    [ObservableProperty]
    private string currentLyric = string.Empty;

    /// <summary>
    /// API 服务器地址
    /// </summary>
    [ObservableProperty]
    private string apiHost = LXMusicLyricsProfile.ApiHost;

    /// <summary>
    /// API 服务器端口
    /// </summary>
    [ObservableProperty]
    private int apiPort = LXMusicLyricsProfile.ApiPort;

    /// <summary>
    /// 更新间隔（毫秒）
    /// </summary>
    [ObservableProperty]
    private int updateInterval = LXMusicLyricsProfile.UpdateInterval;

    // ==================== 私有字段 ====================

    private HaloPixelDevice? _device;
    private LXMusicLyricsReader? _reader;
    private bool _isRunning = false;

    // ==================== 属性 ====================

    public HaloPixelDevice Device => _device ??= new HaloPixelDevice();
    public LXMusicLyricsReader Reader => _reader ??= new LXMusicLyricsReader();

    // ==================== 构造函数 ====================

    public LXMusicLyricsToolPageViewModel()
    {
        Console.WriteLine("[INFO]初始化落雪音乐歌词工具");
        
        // 应用配置
        Reader.ApiHost = ApiHost;
        Reader.ApiPort = ApiPort;

        // 启动后台线程
        StartBackgroundTasks();
    }

    // ==================== 属性变更处理 ====================

    partial void OnEnableLXMusicLyricsChanged(bool value)
    {
        LXMusicLyricsProfile.EnableLXMusicLyrics = value;
        Console.WriteLine($"[DEBUG]歌词显示已{(value ? "启用" : "禁用")}");
    }

    partial void OnSwitchBackWhenPauseChanged(bool value)
    {
        LXMusicLyricsProfile.SwitchBackWhenPause = value;
    }

    partial void OnSwitchBackTimeoutChanged(int value)
    {
        LXMusicLyricsProfile.SwitchBackTimeout = value;
    }

    partial void OnApiHostChanged(string value)
    {
        LXMusicLyricsProfile.ApiHost = value;
        if (_reader != null)
        {
            _reader.ApiHost = value;
        }
    }

    partial void OnApiPortChanged(int value)
    {
        LXMusicLyricsProfile.ApiPort = value;
        if (_reader != null)
        {
            _reader.ApiPort = value;
        }
    }

    partial void OnUpdateIntervalChanged(int value)
    {
        LXMusicLyricsProfile.UpdateInterval = value;
    }

    // ==================== 业务逻辑 ====================

    /// <summary>
    /// 启动所有后台任务
    /// </summary>
    private void StartBackgroundTasks()
    {
        _isRunning = true;

        // 任务 1: 初始化并监控设备连接
        _ = Task.Run(InitializeDeviceAsync);

        // 任务 2: 初始化并监控 API 连接
        _ = Task.Run(InitializeAPIAsync);

        // 任务 3: 主循环 - 读取歌词并显示
        _ = Task.Run(MainLyricsLoopAsync);
    }

    /// <summary>
    /// 初始化 RGB 音响设备
    /// </summary>
    private async Task InitializeDeviceAsync()
    {
        try
        {
            Console.WriteLine("[INFO]正在搜索 RGB 音响设备...");
            
            while (_isRunning && !DeviceReady)
            {
                try
                {
                    var ready = Device.Initialize();
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        DeviceReady = ready;
                    });

                    if (ready)
                    {
                        Console.WriteLine("[INFO]RGB 音响设备已连接");
                        break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[WARN]初始化设备异常：{ex.Message}");
                }

                await Task.Delay(500);
            }

            // 设备就绪后显示启动提示
            if (DeviceReady)
            {
                Device.SetTextLayout(HaloPixelTextLayout.Center);
                Device.ShowText("落雪工具已启动~");
                await Task.Delay(3000);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR]设备初始化线程异常：{ex.Message}");
        }
    }

    /// <summary>
    /// 初始化并连接到落雪音乐 API
    /// </summary>
    private async Task InitializeAPIAsync()
    {
        try
        {
            Console.WriteLine("[INFO]正在连接落雪音乐 API...");
            
            while (_isRunning && !LxmusicReady)
            {
                try
                {
                    var ready = await Reader.TestConnectionAsync();
                    
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        LxmusicReady = ready;
                        LxmusicStatus = ready ? "已连接" : "未连接";
                    });

                    if (ready)
                    {
                        Console.WriteLine("[INFO]落雪音乐 API 已连接");
                        break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[WARN]连接 API 异常：{ex.Message}");
                }

                await Task.Delay(1000);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR]API 初始化线程异常：{ex.Message}");
        }
    }

    /// <summary>
    /// 主循环 - 持续读取歌词并更新显示
    /// </summary>
    private async Task MainLyricsLoopAsync()
    {
        try
        {
            Console.WriteLine("[INFO]启动歌词主循环");

            // 等待设备就绪
            while (_isRunning && !DeviceReady)
            {
                await Task.Delay(500);
            }

            string lastLyric = string.Empty;
            bool isScrolling = false;
            int idleTime = 0;

            while (_isRunning)
            {
                try
                {
                    // 检查功能是否启用
                    if (!EnableLXMusicLyrics || !LxmusicReady)
                    {
                        if (isScrolling)
                        {
                            Device.ShowText(string.Empty);
                            isScrolling = false;
                        }
                        await Task.Delay(UpdateInterval);
                        continue;
                    }

                    // 获取当前音乐数据
                    var musicData = await Reader.GetFullDataAsync();

                    if (musicData != null)
                    {
                        // 更新歌曲信息
                        var songInfo = FormatSongInfo(musicData);
                        MainThread.BeginInvokeOnMainThread(() =>
                        {
                            CurrentSongInfo = songInfo;
                        });

                        // 处理歌词显示
                        if (!string.IsNullOrEmpty(musicData.CurrentLyric))
                        {
                            if (lastLyric != musicData.CurrentLyric)
                            {
                                Console.WriteLine($"[DEBUG]歌词：{musicData.CurrentLyric}");
                                lastLyric = musicData.CurrentLyric;
                                idleTime = 0;

                                // 清空之前的滚动显示
                                if (isScrolling)
                                {
                                    Device.ShowText(string.Empty);
                                    await Task.Delay(100);
                                    isScrolling = false;
                                }

                                // 设置文本布局
                                Device.SetTextLayout(LXMusicLyricsProfile.DefaultHaloPixelTextLayout);
                                Device.ShowText(musicData.CurrentLyric);

                                // 检查是否需要滚动
                                if (musicData.CurrentLyric.Length > LXMusicLyricsProfile.ScrollTriggerThreshold)
                                {
                                    isScrolling = true;
                                    await Task.Delay(500);
                                    Device.SetTextLayout(HaloPixelTextLayout.ScrollRightToLeft);
                                }

                                // 更新当前歌词显示
                                MainThread.BeginInvokeOnMainThread(() =>
                                {
                                    CurrentLyric = musicData.CurrentLyric;
                                });
                            }
                        }
                        else if (LXMusicLyricsProfile.ShowPlaceholderWhenNoLyric)
                        {
                            // 显示占位符
                            Device.SetTextLayout(HaloPixelTextLayout.Center);
                            Device.ShowText(LXMusicLyricsProfile.NoLyricPlaceholder);
                        }

                        // 处理暂停状态 - 超时后切换回时钟界面
                        if (!musicData.IsPlaying && SwitchBackWhenPause)
                        {
                            idleTime += UpdateInterval;
                            if (idleTime >= SwitchBackTimeout * 1000)
                            {
                                Device.SetUIModel(LXMusicLyricsProfile.DefaultHaloPixelUIModel);
                                Console.WriteLine("[DEBUG]已切换至时钟界面");
                                lastLyric = string.Empty; // 重置以便恢复播放时能重新显示
                                idleTime = 0;
                            }
                        }
                        else if (musicData.IsPlaying)
                        {
                            idleTime = 0; // 音乐播放时重置计时器
                        }
                    }
                    else
                    {
                        // 无数据时显示占位符
                        if (LXMusicLyricsProfile.ShowPlaceholderWhenNoLyric && 
                            lastLyric != LXMusicLyricsProfile.NoLyricPlaceholder)
                        {
                            Device.SetTextLayout(HaloPixelTextLayout.Center);
                            Device.ShowText(LXMusicLyricsProfile.NoLyricPlaceholder);
                            lastLyric = LXMusicLyricsProfile.NoLyricPlaceholder;
                        }
                    }

                    await Task.Delay(UpdateInterval);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ERROR]主循环异常：{ex.Message}");
                    if (LXMusicLyricsProfile.EnableDebugLogging)
                    {
                        Console.WriteLine($"[TRACE]{ex.StackTrace}");
                    }
                    await Task.Delay(1000);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR]歌词主循环启动异常：{ex.Message}");
        }
    }

    // ==================== 辅助方法 ====================

    /// <summary>
    /// 格式化歌曲信息显示
    /// </summary>
    private static string FormatSongInfo(LXMusicLyricsData data)
    {
        if (string.IsNullOrEmpty(data.Title))
            return "等待歌曲...";

        var parts = new System.Collections.Generic.List<string>();

        if (!string.IsNullOrEmpty(data.Title))
            parts.Add(data.Title);

        if (!string.IsNullOrEmpty(data.Artist))
            parts.Add(data.Artist);

        if (!string.IsNullOrEmpty(data.Album))
            parts.Add($"《{data.Album}》");

        return string.Join(" - ", parts);
    }

    /// <summary>
    /// 停止所有后台任务
    /// </summary>
    public void Stop()
    {
        _isRunning = false;
        Console.WriteLine("[INFO]已停止歌词工具");
    }

    /// <summary>
    /// 清理资源
    /// </summary>
    public void Dispose()
    {
        Stop();
        _reader?.Dispose();
    }
}

/// <summary>
/// 主线程辅助类（跨平台兼容）
/// </summary>
internal static class MainThread
{
    public static void BeginInvokeOnMainThread(Action action)
    {
        try
        {
            // 尝试使用 WinUI 的 Dispatcher
            var dispatcher = DispatcherQueue.GetForCurrentThread() ?? 
                            MainWindow.Current?.DispatcherQueue;
            
            if (dispatcher != null)
            {
                dispatcher.TryEnqueue(() => action?.Invoke());
            }
            else
            {
                action?.Invoke();
            }
        }
        catch
        {
            // 降级方案：直接调用
            action?.Invoke();
        }
    }
}
