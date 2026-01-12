using System;
using Microsoft.Extensions.Caching.Memory;

namespace Jellyfin.Plugin.BookMetadata.Utils;

/// <summary>
/// Utility class for caching metadata API responses.
/// </summary>
public class MetadataCache : IDisposable
{
    private readonly MemoryCache _cache;
    private readonly TimeSpan _defaultExpiration;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="MetadataCache"/> class.
    /// </summary>
    /// <param name="expirationHours">The cache expiration in hours.</param>
    public MetadataCache(int expirationHours = 24)
    {
        _cache = new MemoryCache(new MemoryCacheOptions
        {
            SizeLimit = 1000 // Limit to 1000 entries
        });
        _defaultExpiration = TimeSpan.FromHours(expirationHours);
    }

    /// <summary>
    /// Gets a value from the cache.
    /// </summary>
    /// <typeparam name="T">The type of the cached value.</typeparam>
    /// <param name="key">The cache key.</param>
    /// <returns>The cached value or default if not found.</returns>
    public T? Get<T>(string key)
    {
        return _cache.TryGetValue(key, out T? value) ? value : default;
    }

    /// <summary>
    /// Sets a value in the cache.
    /// </summary>
    /// <typeparam name="T">The type of the value to cache.</typeparam>
    /// <param name="key">The cache key.</param>
    /// <param name="value">The value to cache.</param>
    /// <param name="expiration">Optional expiration time.</param>
    public void Set<T>(string key, T value, TimeSpan? expiration = null)
    {
        var cacheEntryOptions = new MemoryCacheEntryOptions()
            .SetSize(1)
            .SetAbsoluteExpiration(expiration ?? _defaultExpiration);

        _cache.Set(key, value, cacheEntryOptions);
    }

    /// <summary>
    /// Generates a cache key for a provider and identifier.
    /// </summary>
    /// <param name="provider">The provider name.</param>
    /// <param name="identifier">The identifier.</param>
    /// <returns>The cache key.</returns>
    public static string GenerateKey(string provider, string identifier)
    {
        return $"{provider}:{identifier}";
    }

    /// <summary>
    /// Removes a value from the cache.
    /// </summary>
    /// <param name="key">The cache key.</param>
    public void Remove(string key)
    {
        _cache.Remove(key);
    }

    /// <summary>
    /// Clears all cached values.
    /// </summary>
    public void Clear()
    {
        _cache.Dispose();
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Disposes the cache.
    /// </summary>
    /// <param name="disposing">Whether to dispose managed resources.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            _cache.Dispose();
        }

        _disposed = true;
    }
}
