# Ultimate ability charging – setup per level

This describes how ultimate charging works and what each level needs so the ultimate ability and its UI work correctly.

## How it works

- **UltimateAbility** (on the player) charges when the player collects drops (via **PlayerDropManager**). When charge reaches the required amount, the ultimate can be used.
- **UltimateChargeDisplay** (on the game HUD) shows current/required charge. It reads from the **designated player's** UltimateAbility, not from a global EventBus provider.
- **LevelProgressionManager** defines the **designated player** (the transform that is moved to each level and used for gameplay). The UI and charge logic both use this same player.

## What the persistent scene (BaseGame) must have

These are required for ultimate charging and UI in **all** levels:

1. **LevelProgressionManager** (e.g. on GameRoot)
   - **Player Transform** must be assigned to the player object that has UltimateAbility and PlayerDropManager.
   - This is the "designated player" used for gameplay and for the ultimate charge display.

2. **Player** (the object assigned as Player Transform above)
   - **UltimateAbility** component (enabled).
   - **PlayerDropManager** (so drops can be collected and counted for charge).
   - Other usual player setup (PlayerAbilityManager, movement, etc.).

3. **Game HUD** (in BaseGame)
   - Contains **UltimateChargeDisplay** with a TMP_Text assigned.
   - **Player Source** on UltimateChargeDisplay can be left empty; it will use `LevelProgressionManager.DesignatedPlayer` automatically.
   - Optional: assign **Player Source** if you want this display to show charge for a specific player (e.g. in a test scene).

No per-level setup is needed in the persistent scene beyond ensuring the above is correct once.

## What each level scene needs

Level scenes do **not** need to add UltimateAbility, PlayerDropManager, or the charge UI. They only need:

1. **PlayerSpawn** (optional but recommended)
   - At least one GameObject in the level with a **PlayerSpawn** component.
   - The designated player is moved to this transform when the level loads.

If a level has no PlayerSpawn, the player keeps its previous position when the level loads.

## Summary

| Where            | What to set up |
|------------------|----------------|
| **Persistent (BaseGame)** | LevelProgressionManager with Player Transform → that player has UltimateAbility + PlayerDropManager. Game HUD has UltimateChargeDisplay (Player Source can be empty). |
| **Level scenes**         | PlayerSpawn (optional) so the player is placed correctly. Nothing else for ultimate or charge UI. |

## Testing a single level in the editor

If you hit Play on a **level scene only** (without loading BaseGame first), there is no LevelProgressionManager and no designated player. The ultimate charge display will show the placeholder (e.g. `-/-`) because there is no designated player. To test ultimate charging in a level, run the game from the main menu (or a scene that loads BaseGame and then the level) so the persistent scene and designated player exist.
