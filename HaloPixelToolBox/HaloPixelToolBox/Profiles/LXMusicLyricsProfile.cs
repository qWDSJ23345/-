namespace HaloPixelToolBox.Profiles.CrossVersionProfiles;

/// <summary>
/// 落雪音乐歌词工具配置文件
/// </summary>
public static class LXMusicLyricsProfile
{
    // ==================== 功能开关 ====================
    
    /// <summary>
    /// 是否启用落雪音乐歌词显示
    /// </summary>
    public static bool EnableLXMusicLyrics { get; set; } = true;

    /// <summary>
    /// 音乐暂停时是否切换回时钟界面
    /// </summary>
    public static bool SwitchBackWhenPause { get; set; } = true;

    // ==================== 超时配置 ====================

    /// <summary>
    /// 切换回时钟界面的超时时间（秒）
    /// </summary>
    public static int SwitchBackTimeout { get; set; } = 10;

    /// <summary>
    /// 更新间隔（毫秒）
    /// </summary>
    public static int UpdateInterval { get; set; } = 100;

    // ==================== API 配置 ====================

    /// <summary>
    /// 落雪音乐 API 服务器地址
    /// 默认为本地机器
    /// </summary>
    public static string ApiHost { get; set; } = "localhost";

    /// <summary>
    /// 落雪音乐 OpenAPI 服务端口
    /// 默认端口：23333
    /// </summary>
    public static int ApiPort { get; set; } = 23333;

    /// <summary>
    /// API 连接超时时间（秒）
    /// </summary>
    public static int ApiConnectionTimeout { get; set; } = 2;

    // ==================== 显示配置 ====================

    /// <summary>
    /// 默认歌词显示方式
    /// </summary>
    public static HaloPixelTextLayout DefaultHaloPixelTextLayout { get; set; } = 
        HaloPixelTextLayout.ScrollRightToLeft;

    /// <summary>
    /// 默认 UI 模式（返回时钟界面）
    /// </summary>
    public static HaloPixelUIModel DefaultHaloPixelUIModel { get; set; } = 
        HaloPixelUIModel.ClockUI;

    /// <summary>
    /// 长文本的滚动触发字符数阈值
    /// </summary>
    public static int ScrollTriggerThreshold { get; set; } = 30;

    // ==================== 调试配置 ====================

    /// <summary>
    /// 是否启用详细调试输出
    /// </summary>
    public static bool EnableDebugLogging { get; set; } = true;

    /// <summary>
    /// 是否在无法获取歌词时显示占位符
    /// </summary>
    public static bool ShowPlaceholderWhenNoLyric { get; set; } = false;

    public static string NoLyricPlaceholder { get; set; } = "等待歌词...";

    // ==================== 配置持久化 ====================

    private static readonly string ConfigFileName = "lxmusic_config.json";
    private static string ConfigFilePath => Path.Combine(AppPath.AppDataPath, ConfigFileName);

    /// <summary>
    /// 加载配置文件
    /// </summary>
    public static void LoadConfig()
    {
        try
        {
            if (!Directory.Exists(Path.GetDirectoryName(ConfigFilePath)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(ConfigFilePath)!);
            }

            if (File.Exists(ConfigFilePath))
            {
                var json = File.ReadAllText(ConfigFilePath);
                Console.WriteLine("[INFO]已加载落雪音乐配置");
                // TODO: 实现 JSON 反序列化逻辑
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR]加载配置文件失败：{ex.Message}");
        }
    }

    /// <summary>
    /// 保存配置文件
    /// </summary>
    public static void SaveConfig()
    {
        try
        {
            var configDir = Path.GetDirectoryName(ConfigFilePath);
            if (!Directory.Exists(configDir))
            {
                Directory.CreateDirectory(configDir!);
            }

            // TODO: 实现 JSON 序列化逻辑
            Console.WriteLine("[INFO]配置已保存");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR]保存配置文件失败：{ex.Message}");
        }
    }

    /// <summary>
    /// 重置为默认配置
    /// </summary>
    public static void ResetToDefault()
    {
        EnableLXMusicLyrics = true;
        SwitchBackWhenPause = true;
        SwitchBackTimeout = 10;
        UpdateInterval = 100;
        ApiHost = "localhost";
        ApiPort = 23333;
        ApiConnectionTimeout = 2;
        DefaultHaloPixelTextLayout = HaloPixelTextLayout.ScrollRightToLeft;
        DefaultHaloPixelUIModel = HaloPixelUIModel.ClockUI;
        ScrollTriggerThreshold = 30;
        EnableDebugLogging = true;
        ShowPlaceholderWhenNoLyric = false;

        Console.WriteLine("[INFO]配置已重置为默认值");
    }
}

/// <summary>
/// RGB 音响文本显示方式
/// </summary>
public enum HaloPixelTextLayout
{
    /// <summary>从右往左滚动</summary>
    ScrollRightToLeft = 0,
    /// <summary>从左往右滚动</summary>
    ScrollLeftToRight = 1,
    /// <summary>居中显示</summary>
    Center = 2,
    /// <summary>居左显示</summary>
    Left = 3,
    /// <summary>居右显示</summary>
    Right = 4
}

/// <summary>
/// RGB 音响 UI 模式
/// </summary>
public enum HaloPixelUIModel
{
    /// <summary>时钟界面</summary>
    ClockUI = 0,
    /// <summary>自定义界面</summary>
    CustomUI = 1,
    /// <summary>音乐频谱</summary>
    MusicSpectrum = 2
}

/// <summary>
/// 应用路径管理
/// </summary>
public static class AppPath
{
    /// <summary>
    /// 应用数据目录
    /// </summary>
    public static string AppDataPath => 
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "HaloPixelToolBox");
}
