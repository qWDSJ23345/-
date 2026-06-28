using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace HaloPixelToolBox.Core.Utilities;

/// <summary>
/// 落雪音乐歌词数据模型
/// </summary>
public class LXMusicLyricsData
{
    public string Title { get; set; } = string.Empty;
    public string Artist { get; set; } = string.Empty;
    public string Album { get; set; } = string.Empty;
    public string CurrentLyric { get; set; } = string.Empty;
    public int CurrentTime { get; set; }
    public int Duration { get; set; }
    public bool IsPlaying { get; set; }
}

/// <summary>
/// 落雪音乐歌词读取器 - 基于官方 OpenAPI
/// </summary>
public class LXMusicLyricsReader
{
    private readonly HttpClient _httpClient;
    
    /// <summary>
    /// API 服务器地址
    /// </summary>
    public string ApiHost { get; set; } = "localhost";
    
    /// <summary>
    /// API 服务器端口
    /// </summary>
    public int ApiPort { get; set; } = 23333;
    
    /// <summary>
    /// 连接超时时间（秒）
    /// </summary>
    public int ConnectionTimeout { get; set; } = 2;

    public LXMusicLyricsReader()
    {
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(ConnectionTimeout)
        };
    }

    /// <summary>
    /// 获取完整 API URL
    /// </summary>
    private string GetApiUrl(string endpoint)
    {
        return $"http://{ApiHost}:{ApiPort}{endpoint}";
    }

    /// <summary>
    /// 测试与落雪音乐 API 的连接
    /// </summary>
    public async Task<bool> TestConnectionAsync()
    {
        try
        {
            using var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(ConnectionTimeout));
            var response = await _httpClient.GetAsync(GetApiUrl("/music/playing"), cts.Token);
            bool isConnected = response.IsSuccessStatusCode;
            
            if (isConnected)
            {
                Console.WriteLine($"[INFO]已连接到落雪音乐 API ({ApiHost}:{ApiPort})");
            }
            else
            {
                Console.WriteLine($"[WARN]落雪音乐 API 返回状态码：{response.StatusCode}");
            }
            
            return isConnected;
        }
        catch (System.Threading.Tasks.TaskCanceledException)
        {
            Console.WriteLine($"[WARN]连接超时，请检查 API 地址和端口 ({ApiHost}:{ApiPort})");
            return false;
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"[WARN]无法连接到落雪音乐 API：{ex.Message}");
            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR]测试连接异常：{ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 获取当前播放的音乐信息
    /// </summary>
    public async Task<LXMusicLyricsData?> GetCurrentPlayingAsync()
    {
        try
        {
            using var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(ConnectionTimeout));
            var response = await _httpClient.GetAsync(GetApiUrl("/music/playing"), cts.Token);
            
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"[DEBUG]获取播放信息失败，状态码：{response.StatusCode}");
                return null;
            }

            var jsonContent = await response.Content.ReadAsStringAsync();
            var doc = JsonDocument.Parse(jsonContent);
            var root = doc.RootElement;

            var data = new LXMusicLyricsData
            {
                Title = GetJsonPropertyString(root, "title"),
                Artist = GetJsonPropertyString(root, "artist"),
                Album = GetJsonPropertyString(root, "album"),
                CurrentTime = GetJsonPropertyInt(root, "currentTime"),
                Duration = GetJsonPropertyInt(root, "duration"),
                IsPlaying = GetJsonPropertyBool(root, "playing")
            };

            return data;
        }
        catch (System.Threading.Tasks.TaskCanceledException)
        {
            Console.WriteLine("[WARN]获取播放信息超时");
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR]获取播放信息异常：{ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// 获取当前歌词
    /// </summary>
    public async Task<string> GetCurrentLyricsAsync()
    {
        try
        {
            // 先获取播放信息以获取当前时间
            var playingData = await GetCurrentPlayingAsync();
            if (playingData == null)
                return string.Empty;

            using var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(ConnectionTimeout));
            var response = await _httpClient.GetAsync(GetApiUrl("/music/lyric"), cts.Token);
            
            if (!response.IsSuccessStatusCode)
                return string.Empty;

            var jsonContent = await response.Content.ReadAsStringAsync();
            return await ExtractCurrentLyricAsync(jsonContent, playingData.CurrentTime);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR]获取歌词异常：{ex.Message}");
            return string.Empty;
        }
    }

    /// <summary>
    /// 获取完整的音乐和歌词数据
    /// </summary>
    public async Task<LXMusicLyricsData?> GetFullDataAsync()
    {
        try
        {
            var playingData = await GetCurrentPlayingAsync();
            if (playingData == null)
                return null;

            // 获取歌词
            var lyrics = await GetCurrentLyricsAsync();
            playingData.CurrentLyric = lyrics;

            return playingData;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR]获取完整数据异常：{ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// 从歌词数据中提取当前时间对应的歌词行
    /// </summary>
    private async Task<string> ExtractCurrentLyricAsync(string lyricJson, int currentTimeMs)
    {
        return await Task.Run(() =>
        {
            try
            {
                var doc = JsonDocument.Parse(lyricJson);
                var root = doc.RootElement;

                // 处理不同的响应格式
                string currentLyric = string.Empty;
                
                // 格式1: {"lyric": [[时间, 歌词], ...]}
                if (root.TryGetProperty("lyric", out var lyricArray) && 
                    lyricArray.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in lyricArray.EnumerateArray())
                    {
                        if (item.ValueKind == JsonValueKind.Array && item.GetArrayLength() >= 2)
                        {
                            try
                            {
                                int time = item[0].GetInt32();
                                string lyric = item[1].GetString() ?? string.Empty;
                                
                                if (time <= currentTimeMs)
                                {
                                    currentLyric = lyric;
                                }
                                else
                                {
                                    break;
                                }
                            }
                            catch
                            {
                                // 格式异常，跳过此项
                                continue;
                            }
                        }
                    }
                }
                // 格式2: 直接是数组 [[时间, 歌词], ...]
                else if (root.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in root.EnumerateArray())
                    {
                        if (item.ValueKind == JsonValueKind.Array && item.GetArrayLength() >= 2)
                        {
                            try
                            {
                                int time = item[0].GetInt32();
                                string lyric = item[1].GetString() ?? string.Empty;
                                
                                if (time <= currentTimeMs)
                                {
                                    currentLyric = lyric;
                                }
                                else
                                {
                                    break;
                                }
                            }
                            catch
                            {
                                continue;
                            }
                        }
                    }
                }

                return currentLyric;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR]解析歌词数据异常：{ex.Message}");
                return string.Empty;
            }
        });
    }

    /// <summary>
    /// 安全获取 JSON 属性值（字符串）
    /// </summary>
    private static string GetJsonPropertyString(JsonElement element, string propertyName)
    {
        try
        {
            if (element.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.String)
            {
                return property.GetString() ?? string.Empty;
            }
        }
        catch { }
        
        return string.Empty;
    }

    /// <summary>
    /// 安全获取 JSON 属性值（整数）
    /// </summary>
    private static int GetJsonPropertyInt(JsonElement element, string propertyName)
    {
        try
        {
            if (element.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.Number)
            {
                return property.GetInt32();
            }
        }
        catch { }
        
        return 0;
    }

    /// <summary>
    /// 安全获取 JSON 属性值（布尔值）
    /// </summary>
    private static bool GetJsonPropertyBool(JsonElement element, string propertyName)
    {
        try
        {
            if (element.TryGetProperty(propertyName, out var property))
            {
                if (property.ValueKind == JsonValueKind.True)
                    return true;
                if (property.ValueKind == JsonValueKind.False)
                    return false;
            }
        }
        catch { }
        
        return false;
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}
