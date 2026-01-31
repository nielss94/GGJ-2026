using System;

/// <summary>
/// Typed event bus. Subscribe in OnEnable, unsubscribe in OnDisable.
/// </summary>
public static class EventBus
{
    public static event Action PlayerDied;

    public static void RaisePlayerDied() => PlayerDied?.Invoke();

    /// <summary>
    /// Raised when something (e.g. UI) wants to block player input. Pass the requesting source (e.g. this).
    /// </summary>
    public static event Action<object> PlayerInputBlockRequested;

    /// <summary>
    /// Raised when a source that previously blocked no longer does. Pass the same source used in Block.
    /// </summary>
    public static event Action<object> PlayerInputUnblockRequested;

    public static void RaisePlayerInputBlockRequested(object source) => PlayerInputBlockRequested?.Invoke(source);

    public static void RaisePlayerInputUnblockRequested(object source) => PlayerInputUnblockRequested?.Invoke(source);

    /// <summary>
    /// Raised when the player has chosen an upgrade (e.g. from UpgradePanel). Subscribers apply the upgrade (e.g. ability/stat from UpgradeOffer).
    /// </summary>
    public static event Action<UpgradeOffer> UpgradeChosen;

    public static void RaiseUpgradeChosen(UpgradeOffer offer) => UpgradeChosen?.Invoke(offer);
}
