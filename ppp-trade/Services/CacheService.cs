using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;

namespace ppp_trade.Services;

public class CacheService(IMemoryCache cache)
{
    private CancellationTokenSource _resetCacheToken = new();

    public void Set<T>(string key, T value, TimeSpan? duration = null)
    {
        if (value == null) return;

        var options = new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(duration ?? TimeSpan.FromMinutes(30))
            .AddExpirationToken(new CancellationChangeToken(_resetCacheToken.Token));

        cache.Set(key, value, options);
    }

    public T? Get<T>(string key)
    {
        return cache.TryGetValue(key, out T? value) ? value : default;
    }

    public bool TryGet<T>(string key, out T? value)
    {
        return cache.TryGetValue(key, out value);
    }

    public void Remove(string key)
    {
        cache.Remove(key);
    }

    public void Clear()
    {
        _resetCacheToken.Cancel();
        _resetCacheToken.Dispose();
        _resetCacheToken = new CancellationTokenSource();
    }
}