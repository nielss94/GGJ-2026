using System;
using System.Collections.Generic;
using UnityEngine;

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
            PruneDestroyedBlockers();
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
            PruneDestroyedBlockers();
            return MovementBlockers.Count > 0;
        }
    }

    /// <summary>
    /// Removes any sources that are destroyed Unity objects so input doesn't stay blocked after scene unload or disabled UI.
    /// </summary>
    private static void PruneDestroyedBlockers()
    {
        PruneDestroyed(Blockers);
        PruneDestroyed(MovementBlockers);
    }

    private static void PruneDestroyed(HashSet<object> set)
    {
        if (set.Count == 0) return;
        var toRemove = new List<object>();
        foreach (var obj in set)
        {
            if (obj is UnityEngine.Object uo && uo == null)
                toRemove.Add(obj);
        }
        foreach (var obj in toRemove)
            set.Remove(obj);
    }

#if UNITY_EDITOR
    /// <summary>
    /// Call from Inspector or debug menu to log current blocker state. Only compiled in Editor.
    /// </summary>
    public static void DebugLogBlockers()
    {
        PruneDestroyedBlockers();
        UnityEngine.Debug.Log($"[PlayerInputBlocker] IsInputBlocked={Blockers.Count > 0} (count={Blockers.Count}), IsMovementBlocked={MovementBlockers.Count > 0} (count={MovementBlockers.Count}). Input sources: {string.Join(", ", Blockers)}. Movement sources: {string.Join(", ", MovementBlockers)}.");
    }
#endif

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
