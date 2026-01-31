using System;

/// <summary>
/// Typed event bus. Subscribe in OnEnable, unsubscribe in OnDisable.
/// </summary>
public static class EventBus
{
    public static event Action PlayerDied;

    public static void RaisePlayerDied() => PlayerDied?.Invoke();

    /// <summary>Raised when the player's health changes (current, max). Subscribe in OnEnable, unsubscribe in OnDisable.</summary>
    public static event Action<float, float> PlayerHealthChanged;

    public static void RaisePlayerHealthChanged(float current, float max) => PlayerHealthChanged?.Invoke(current, max);

    /// <summary>Optional: set by the player's Health when enabled so UI can get initial value without a direct reference.</summary>
    public static Func<(float current, float max)> GetPlayerHealth;

    public static void SetPlayerHealthProvider(Func<(float current, float max)> provider) => GetPlayerHealth = provider;

    public static void ClearPlayerHealthProvider() => GetPlayerHealth = null;

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
    /// Raised when the player starts a dash. Use for dodge cancel (e.g. LightAttackAbility cancels attack and hitboxes).
    /// </summary>
    public static event Action<object> PlayerDashStarted;

    public static void RaisePlayerDashStarted(object source) => PlayerDashStarted?.Invoke(source);

    /// <summary>
    /// Raised when the player has chosen an upgrade (e.g. from UpgradePanel). Subscribers apply the upgrade (e.g. ability/stat from UpgradeOffer).
    /// </summary>
    public static event Action<UpgradeOffer> UpgradeChosen;

    public static void RaiseUpgradeChosen(UpgradeOffer offer) => UpgradeChosen?.Invoke(offer);

    /// <summary>Raised when a non-player Health dies (e.g. encounter enemy). For audio, VFX, UI.</summary>
    public static event Action EnemyDied;

    public static void RaiseEnemyDied() => EnemyDied?.Invoke();

    /// <summary>Raised when the current encounter/level is complete (budget depleted and all enemies defeated). For UI, progression, audio.</summary>
    public static event Action LevelComplete;

    public static void RaiseLevelComplete() => LevelComplete?.Invoke();
}
