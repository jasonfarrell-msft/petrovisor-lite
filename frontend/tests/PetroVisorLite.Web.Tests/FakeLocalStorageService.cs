using Blazored.LocalStorage;

namespace PetroVisorLite.Web.Tests;

/// <summary>
/// Minimal in-memory <see cref="ILocalStorageService"/> stand-in for unit tests, so
/// <see cref="PetroVisorLite.Web.Auth.JwtAuthenticationStateProvider"/> can be exercised
/// without a real browser's localStorage or JS interop. Only implements the string-based
/// members the provider actually uses.
/// </summary>
public class FakeLocalStorageService : ILocalStorageService
{
    private readonly Dictionary<string, string> _store = new();

#pragma warning disable CS0067 // events required by interface, unused by tests
    public event EventHandler<ChangingEventArgs>? Changing;
    public event EventHandler<ChangedEventArgs>? Changed;
#pragma warning restore CS0067

    public ValueTask ClearAsync(CancellationToken cancellationToken = default)
    {
        _store.Clear();
        return ValueTask.CompletedTask;
    }

    public ValueTask<string?> GetItemAsStringAsync(string key, CancellationToken cancellationToken = default) =>
        ValueTask.FromResult(_store.TryGetValue(key, out var value) ? value : null);

    public ValueTask SetItemAsStringAsync(string key, string data, CancellationToken cancellationToken = default)
    {
        _store[key] = data;
        return ValueTask.CompletedTask;
    }

    public ValueTask RemoveItemAsync(string key, CancellationToken cancellationToken = default)
    {
        _store.Remove(key);
        return ValueTask.CompletedTask;
    }

    public ValueTask RemoveItemsAsync(IEnumerable<string> keys, CancellationToken cancellationToken = default)
    {
        foreach (var key in keys) _store.Remove(key);
        return ValueTask.CompletedTask;
    }

    public ValueTask<int> LengthAsync(CancellationToken cancellationToken = default) => ValueTask.FromResult(_store.Count);

    public ValueTask<string?> KeyAsync(int index, CancellationToken cancellationToken = default) =>
        ValueTask.FromResult(_store.Keys.ElementAtOrDefault(index));

    public ValueTask<IEnumerable<string>> KeysAsync(CancellationToken cancellationToken = default) =>
        ValueTask.FromResult<IEnumerable<string>>(_store.Keys.ToList());

    public ValueTask<bool> ContainKeyAsync(string key, CancellationToken cancellationToken = default) =>
        ValueTask.FromResult(_store.ContainsKey(key));

    public ValueTask<T?> GetItemAsync<T>(string key, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException("Not used by JwtAuthenticationStateProvider tests.");

    public ValueTask SetItemAsync<T>(string key, T data, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException("Not used by JwtAuthenticationStateProvider tests.");
}
