using System.Collections.Concurrent;
using Tarot.Core.Interfaces;

namespace Tarot.Infrastructure.Services;

public class InMemoryRedisService : IRedisService
{
    private readonly ConcurrentDictionary<string, string> _storage = new();
    private readonly ConcurrentDictionary<string, DateTimeOffset> _expirations = new();
    private readonly ConcurrentDictionary<string, object> _locks = new();

    public Task<bool> AcquireLockAsync(string key, TimeSpan expiry)
    {
        // Simple in-memory lock simulation
        if (_locks.TryAdd(key, new object()))
        {
            // Auto-release simulation not implemented for brevity, 
            // but in real Redis it expires. 
            // We can simulate expiry by removing it after delay if we wanted.
            return Task.FromResult(true);
        }
        return Task.FromResult(false);
    }

    public Task ReleaseLockAsync(string key)
    {
        _locks.TryRemove(key, out _);
        return Task.CompletedTask;
    }

    public Task SetAsync<T>(string key, T value, TimeSpan? expiry = null)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(value);
        _storage[key] = json;
        if (expiry.HasValue)
        {
            _expirations[key] = DateTimeOffset.UtcNow.Add(expiry.Value);
        }
        else
        {
            _expirations.TryRemove(key, out _);
        }
        return Task.CompletedTask;
    }

    public Task<T?> GetAsync<T>(string key)
    {
        if (_expirations.TryGetValue(key, out var expiry) && DateTimeOffset.UtcNow > expiry)
        {
            _storage.TryRemove(key, out _);
            _expirations.TryRemove(key, out _);
            return Task.FromResult<T?>(default);
        }

        if (_storage.TryGetValue(key, out var json))
        {
            return Task.FromResult(System.Text.Json.JsonSerializer.Deserialize<T>(json));
        }
        return Task.FromResult<T?>(default);
    }

    public Task RemoveAsync(string key)
    {
        _storage.TryRemove(key, out _);
        _expirations.TryRemove(key, out _);
        return Task.CompletedTask;
    }
}
