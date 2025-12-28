namespace WorkoutCoachV3.Maui.Data;

// Eenvoudige sync-status voor lokale records t.o.v. de server.
public enum SyncState
{
    // Record is in sync met de server (geen lokale wijzigingen die nog moeten doorgestuurd worden).
    Synced = 0,
    // Record bevat lokale wijzigingen en moet nog gesynct worden.
    Dirty = 1
}
