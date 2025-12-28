using Microsoft.Maui.Networking;
using System.Diagnostics;

// :contentReference[oaicite:1]{index=1} // Sync service: start sync bij internet connectie (debounced + gated).
namespace WorkoutCoachV3.Maui.Services;

public sealed class ConnectivitySyncService : IDisposable
{
    // Sync engine + token check om enkel te syncen wanneer ingelogd.
    private readonly ISyncService _sync;
    private readonly ITokenStore _tokenStore;

    // Gate om te voorkomen dat meerdere syncs tegelijk lopen.
    private readonly SemaphoreSlim _syncGate = new(1, 1);

    // Debounce token source om snelle connectiviteit-events te bundelen.
    private CancellationTokenSource? _debounceCts;
    private bool _started;

    public ConnectivitySyncService(ISyncService sync, ITokenStore tokenStore)
    {
        _sync = sync;
        _tokenStore = tokenStore;
    }

    // Start: subscribe op ConnectivityChanged + trigger init sync (debounced).
    public void Start()
    {
        if (_started) return;
        _started = true;

        Connectivity.Current.ConnectivityChanged += OnConnectivityChanged;

        _ = TriggerSyncDebouncedAsync();
    }

    // Stop: unsubscribe + cancel debounce zodat er niets meer loopt.
    public void Stop()
    {
        if (!_started) return;
        _started = false;

        Connectivity.Current.ConnectivityChanged -= OnConnectivityChanged;

        try { _debounceCts?.Cancel(); } catch { }
        _debounceCts = null;
    }

    // Callback: alleen syncen wanneer internet effectief beschikbaar is.
    private void OnConnectivityChanged(object? sender, ConnectivityChangedEventArgs e)
    {
        if (e.NetworkAccess == NetworkAccess.Internet)
        {
            _ = TriggerSyncDebouncedAsync();
        }
    }

    // Debounce: wacht even zodat connect/disconnect bursts niet meerdere syncs starten.
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
            // Catch-all logging zodat connectivity events nooit de app breken.
            Debug.WriteLine("[ConnectivitySync][DEBOUNCE] " + ex);
        }
    }

    // Effectieve sync: vereist internet + geldig token + geen lopende sync.
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
            // Sync errors loggen (server/down/validatie) zonder crash.
            Debug.WriteLine("[ConnectivitySync][SYNC] " + ex);
        }
        finally
        {
            _syncGate.Release();
        }
    }

    // Dispose: netjes unsubscribe zodat er geen leaks zijn.
    public void Dispose() => Stop();
}
