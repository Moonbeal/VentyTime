using System.Text.Json;
using Microsoft.JSInterop;
using Blazored.LocalStorage;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace VentyTime.Client.Services;

public class LocalStorageService : ILocalStorageService
{
    private readonly IJSRuntime _jsRuntime;

    public event EventHandler<ChangingEventArgs> Changing = (sender, e) => { };
    public event EventHandler<ChangedEventArgs> Changed = (sender, e) => { };

    public LocalStorageService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public async ValueTask<T> GetItemAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        var json = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", cancellationToken, key);
        if (string.IsNullOrEmpty(json))
            return default!;

        try
        {
            return JsonSerializer.Deserialize<T>(json)!;
        }
        catch
        {
            return default!;
        }
    }

    public async ValueTask<string> GetItemAsStringAsync(string key, CancellationToken cancellationToken = default)
    {
        return await _jsRuntime.InvokeAsync<string>("localStorage.getItem", cancellationToken, key) ?? string.Empty;
    }

    public async ValueTask SetItemAsync<T>(string key, T value, CancellationToken cancellationToken = default)
    {
        var e = new ChangingEventArgs { Key = key, OldValue = await GetItemAsStringAsync(key, cancellationToken) };
        Changing?.Invoke(this, e);

        if (e.Cancel) return;

        var serializedValue = JsonSerializer.Serialize(value);
        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", cancellationToken, key, serializedValue);

        Changed?.Invoke(this, new ChangedEventArgs { Key = key, OldValue = e.OldValue, NewValue = serializedValue });
    }

    public async ValueTask SetItemAsStringAsync(string key, string value, CancellationToken cancellationToken = default)
    {
        var e = new ChangingEventArgs { Key = key, OldValue = await GetItemAsStringAsync(key, cancellationToken) };
        Changing?.Invoke(this, e);

        if (e.Cancel) return;

        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", cancellationToken, key, value);

        Changed?.Invoke(this, new ChangedEventArgs { Key = key, OldValue = e.OldValue, NewValue = value });
    }

    public async ValueTask RemoveItemAsync(string key, CancellationToken cancellationToken = default)
    {
        var e = new ChangingEventArgs { Key = key, OldValue = await GetItemAsStringAsync(key, cancellationToken) };
        Changing?.Invoke(this, e);

        if (e.Cancel) return;

        await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", cancellationToken, key);

        Changed?.Invoke(this, new ChangedEventArgs { Key = key, OldValue = e.OldValue });
    }

    public async ValueTask RemoveItemsAsync(IEnumerable<string> keys, CancellationToken cancellationToken = default)
    {
        foreach (var key in keys)
        {
            await RemoveItemAsync(key, cancellationToken);
        }
    }

    public async ValueTask ClearAsync(CancellationToken cancellationToken = default)
    {
        await _jsRuntime.InvokeVoidAsync("localStorage.clear", cancellationToken);
    }

    public async ValueTask<int> LengthAsync(CancellationToken cancellationToken = default)
    {
        return await _jsRuntime.InvokeAsync<int>("eval", cancellationToken, "localStorage.length");
    }

    public async ValueTask<string> KeyAsync(int index, CancellationToken cancellationToken = default)
    {
        return await _jsRuntime.InvokeAsync<string>("eval", cancellationToken, $"localStorage.key({index})") ?? string.Empty;
    }

    public async ValueTask<bool> ContainKeyAsync(string key, CancellationToken cancellationToken = default)
    {
        var value = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", cancellationToken, key);
        return value != null;
    }

    public async ValueTask<IEnumerable<string>> KeysAsync(CancellationToken cancellationToken = default)
    {
        var length = await LengthAsync(cancellationToken);
        var keys = new List<string>();

        for (var i = 0; i < length; i++)
        {
            var key = await KeyAsync(i, cancellationToken);
            if (!string.IsNullOrEmpty(key))
            {
                keys.Add(key);
            }
        }

        return keys;
    }
}
