using Microsoft.AspNetCore.Http;

namespace Rsp.Portal.UnitTests;

/// <summary>
///     Minimal in-memory ISession implementation for unit tests.
///     Works with SessionExtensions SetString/GetString.
/// </summary>
public sealed class InMemorySession : ISession
{
    private readonly Dictionary<string, byte[]> _store = new();

    public bool IsAvailable => true;
    public string Id { get; } = Guid.NewGuid().ToString();
    public IEnumerable<string> Keys => _store.Keys;

    public void Clear()
    {
        _store.Clear();
    }

    public Task CommitAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task LoadAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public void Remove(string key)
    {
        _store.Remove(key);
    }

    public void Set(string key, byte[] value)
    {
        _store[key] = value;
    }

    public bool TryGetValue(string key, out byte[] value)
    {
        return _store.TryGetValue(key, out value);
    }
}