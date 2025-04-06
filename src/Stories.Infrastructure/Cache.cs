using Microsoft.Extensions.Caching.Memory;
using Stories.Application.Services;

namespace Stories.Infrastructure;

public class Cache : ICache
{
    private readonly IMemoryCache _cache;
    private readonly TimeSpan _duration;

    public Cache(IMemoryCache cache, TimeSpan duration)
    {
        _cache = cache;
        _duration = duration;
    }

    public T? Get<T>(int key)
    {
        _cache.TryGetValue(key, out var value);
        return (T?)value;
    }

    public void Put<T>(int key, T value)
    {
        _cache.Set(key, value, _duration);
    }
}