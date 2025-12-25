using Microsoft.Maui.Networking;
using System.Diagnostics;

namespace WorkoutCoachV3.Maui.Services;

public sealed class ConnectivitySyncService : IDisposable
{
    private readonly ISyncService _sync;
    private readonly ITokenStore _tokenStore;

    private readonly SemaphoreSlim _syncGate = new(1, 1);

    private CancellationTokenSource? _debounceCts;
    private bool _started;

    public ConnectivitySyncService(ISyncService sync, ITokenStore tokenStore)
    {
        _sync = sync;
        _tokenStore = tokenStore;
    }

    public void Start()
    {
        if (_started) return;
        _started = true;

        Connectivity.Current.ConnectivityChanged += OnConnectivityChanged;

        _ = TriggerSyncDebouncedAsync();
    }

    public void Stop()
    {
        if (!_started) return;
        _started = false;

        Connectivity.Current.ConnectivityChanged -= OnConnectivityChanged;

        try { _debounceCts?.Cancel(); } catch { }
        _debounceCts = null;
    }

    private void OnConnectivityChanged(object? sender, ConnectivityChangedEventArgs e)
    {
        if (e.NetworkAccess == NetworkAccess.Internet)
        {
            _ = TriggerSyncDebouncedAsync();
        }
    }

    private async Task TriggerSyncDebouncedAsync()
    {
        try
        {
            _debounceCts?.Cancel();
            var cts = new CancellationTokenSource();
            _debounceCts = cts;

            await Task.Delay(750, cts.Token);

            await TriggerSyncAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            // ok
        }
        catch (Exception ex)
        {
            Debug.WriteLine("[ConnectivitySync][DEBOUNCE] " + ex);
        }
    }

    public async Task TriggerSyncAsync(CancellationToken ct = default)
    {
        if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
            return;

        if (!await _tokenStore.HasValidTokenAsync())
            return;

        if (!await _syncGate.WaitAsync(0, ct))
            return;

        try
        {
            Debug.WriteLine("[ConnectivitySync] Internet available -> SyncAllAsync()");
            await _sync.SyncAllAsync(ct);
        }
        catch (Exception ex)
        {
            Debug.WriteLine("[ConnectivitySync][SYNC] " + ex);
        }
        finally
        {
            _syncGate.Release();
        }
    }

    public void Dispose() => Stop();
}
