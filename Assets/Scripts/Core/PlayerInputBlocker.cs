using System;
using System.Collections.Generic;

/// <summary>
/// Central place for "is player input blocked?" (e.g. by upgrade panel, pause menu).
/// Subscribes to EventBus block/unblock requests and tracks sources so multiple systems can block without conflict.
/// No hard coupling: UI raises events; movement/abilities read IsInputBlocked.
/// </summary>
public static class PlayerInputBlocker
{
    private static readonly HashSet<object> Blockers = new HashSet<object>();
    private static bool subscribed;

    /// <summary>
    /// True when any source has requested input to be blocked.
    /// </summary>
    public static bool IsInputBlocked
    {
        get
        {
            EnsureSubscribed();
            return Blockers.Count > 0;
        }
    }

    private static void EnsureSubscribed()
    {
        if (subscribed) return;
        subscribed = true;
        EventBus.PlayerInputBlockRequested += OnBlockRequested;
        EventBus.PlayerInputUnblockRequested += OnUnblockRequested;
    }

    private static void OnBlockRequested(object source)
    {
        if (source != null)
            Blockers.Add(source);
    }

    private static void OnUnblockRequested(object source)
    {
        if (source != null)
            Blockers.Remove(source);
    }
}
