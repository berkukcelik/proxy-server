using System.Collections.Concurrent;
using System.Text.Json;

namespace CachingProxy;

public class CacheManager
{
    private readonly ConcurrentDictionary<string, CachedResponse> _cache;
    private readonly string _cacheFilePath;

    public CacheManager()
    {
        _cache = new ConcurrentDictionary<string, CachedResponse>();
        _cacheFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".caching-proxy-cache.json");
        LoadCacheFromDisk();
    }

    public CachedResponse? GetCachedResponse(string key)
    {
        _cache.TryGetValue(key, out var response);
        return response;
    }

    public void CacheResponse(string key, CachedResponse response)
    {
        _cache[key] = response;
        SaveCacheToDisk();
    }

    public void ClearCache()
    {
        _cache.Clear();
        if (File.Exists(_cacheFilePath))
        {
            File.Delete(_cacheFilePath);
        }
    }

    private void LoadCacheFromDisk()
    {
        try
        {
            if (File.Exists(_cacheFilePath))
            {
                var json = File.ReadAllText(_cacheFilePath);
                var cacheData = JsonSerializer.Deserialize<Dictionary<string, CachedResponse>>(json);
                
                if (cacheData != null)
                {
                    foreach (var kvp in cacheData)
                    {
                        _cache[kvp.Key] = kvp.Value;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Could not load cache from disk: {ex.Message}");
        }
    }

    private void SaveCacheToDisk()
    {
        try
        {
            var cacheData = _cache.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            var json = JsonSerializer.Serialize(cacheData, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_cacheFilePath, json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Could not save cache to disk: {ex.Message}");
        }
    }
}

public class CachedResponse
{
    public int StatusCode { get; set; }
    public Dictionary<string, string[]> Headers { get; set; } = new();
    public Dictionary<string, string[]> ContentHeaders { get; set; } = new();
    public byte[] Body { get; set; } = Array.Empty<byte>();
}