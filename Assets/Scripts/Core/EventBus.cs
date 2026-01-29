using System;

/// <summary>
/// Typed event bus. Subscribe in OnEnable, unsubscribe in OnDisable.
/// </summary>
public static class EventBus
{
    public static event Action PlayerDied;

    public static void RaisePlayerDied() => PlayerDied?.Invoke();
}
