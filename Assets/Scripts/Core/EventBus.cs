using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Typed event bus. Subscribe in OnEnable, unsubscribe in OnDisable.
/// </summary>
public static class EventBus
{
    /// <summary>Payload for damage number UI: world position, damage value, and whether the hit was a crit.</summary>
    public readonly struct DamageNumberHit
    {
        public Vector3 WorldPosition { get; }
        public float Damage { get; }
        public bool IsCrit { get; }

        public DamageNumberHit(Vector3 worldPosition, float damage, bool isCrit)
        {
            WorldPosition = worldPosition;
            Damage = damage;
            IsCrit = isCrit;
        }
    }

    /// <summary>Raised when enemies are hit so damage number UI can spawn. Pass all hits (position, damage, isCrit). Subscribe in OnEnable, unsubscribe in OnDisable.</summary>
    public static event Action<IReadOnlyList<DamageNumberHit>> DamageNumbersRequested;

    public static void RaiseDamageNumbersRequested(IReadOnlyList<DamageNumberHit> hits) => DamageNumbersRequested?.Invoke(hits);

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
    /// Raised when something (e.g. light attack during swing) wants to block only movement, not ability input. Allows dash cancel mid-attack.
    /// </summary>
    public static event Action<object> PlayerMovementBlockRequested;

    /// <summary>
    /// Raised when a source that previously blocked movement no longer does.
    /// </summary>
    public static event Action<object> PlayerMovementUnblockRequested;

    public static void RaisePlayerMovementBlockRequested(object source) => PlayerMovementBlockRequested?.Invoke(source);

    public static void RaisePlayerMovementUnblockRequested(object source) => PlayerMovementUnblockRequested?.Invoke(source);

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

    /// <summary>Raised when a menu is shown (pause, death screen, upgrade panel). Subscribers (e.g. Health FMOD) can pause gameplay audio. Use ref count: Paused increments, Resumed decrements.</summary>
    public static event Action GameplayPaused;

    /// <summary>Raised when a menu is hidden. Pair with GameplayPaused for ref counting.</summary>
    public static event Action GameplayResumed;

    public static void RaiseGameplayPaused() => GameplayPaused?.Invoke();

    public static void RaiseGameplayResumed() => GameplayResumed?.Invoke();

    /// <summary>Raised when a non-player Health dies (e.g. encounter enemy). For audio, VFX, UI.</summary>
    public static event Action EnemyDied;

    public static void RaiseEnemyDied() => EnemyDied?.Invoke();

    /// <summary>Raised when the current encounter/level is complete (budget depleted and all enemies defeated). For UI, progression, audio.</summary>
    public static event Action LevelComplete;

    public static void RaiseLevelComplete() => LevelComplete?.Invoke();

    /// <summary>Raised when the player uses the ultimate ability (blast wave). For VFX, audio, etc.</summary>
    public static event Action UltimateUsed;

    public static void RaiseUltimateUsed() => UltimateUsed?.Invoke();

    /// <summary>Optional: set by UltimateAbility so UI/glow can read charge without a direct reference. Returns (current drops, required drops).</summary>
    public static Func<(int current, int required)> GetUltimateCharge;

    public static void SetUltimateChargeProvider(Func<(int current, int required)> provider) => GetUltimateCharge = provider;

    public static void ClearUltimateChargeProvider() => GetUltimateCharge = null;
}
