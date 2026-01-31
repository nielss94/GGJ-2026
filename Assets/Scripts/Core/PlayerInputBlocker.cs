using System;
using System.Collections.Generic;

/// <summary>
/// Central place for "is player input blocked?" and "is movement only blocked?".
/// Input block: upgrade panel, dash — no movement, no abilities. Movement block: e.g. light attack during swing — no movement, abilities (e.g. dash) still work.
/// </summary>
public static class PlayerInputBlocker
{
    private static readonly HashSet<object> Blockers = new HashSet<object>();
    private static readonly HashSet<object> MovementBlockers = new HashSet<object>();
    private static bool subscribed;

    /// <summary>
    /// True when any source has requested input to be blocked (no movement, no ability input).
    /// </summary>
    public static bool IsInputBlocked
    {
        get
        {
            EnsureSubscribed();
            return Blockers.Count > 0;
        }
    }

    /// <summary>
    /// True when any source has requested movement only to be blocked. Abilities (e.g. dash) still receive input.
    /// </summary>
    public static bool IsMovementBlocked
    {
        get
        {
            EnsureSubscribed();
            return MovementBlockers.Count > 0;
        }
    }

    private static void EnsureSubscribed()
    {
        if (subscribed) return;
        subscribed = true;
        EventBus.PlayerInputBlockRequested += OnBlockRequested;
        EventBus.PlayerInputUnblockRequested += OnUnblockRequested;
        EventBus.PlayerMovementBlockRequested += OnMovementBlockRequested;
        EventBus.PlayerMovementUnblockRequested += OnMovementUnblockRequested;
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

    private static void OnMovementBlockRequested(object source)
    {
        if (source != null)
            MovementBlockers.Add(source);
    }

    private static void OnMovementUnblockRequested(object source)
    {
        if (source != null)
            MovementBlockers.Remove(source);
    }
}
