# Upgrade System – Designer Guide

This folder contains the **upgrade database** system. Designers can create and balance upgrades without touching code.

## Overview

- **Rarities**: Define Common, Rare, Legendary, etc. Each has a **probability weight**, a **value multiplier** (applied to the curve result), and a **Display Color** (used to visualise rarity in the upgrade panel).
- **Ability upgrades**: Target a specific ability slot (A/B/X/Y), an **Ability Stat Id** (ScriptableObject), and an **animation curve** (X = level, Y = base value). The value is always evaluated from this curve × rarity multiplier; abilities do not evaluate curves themselves.
- **Stat upgrades**: Same idea for global stats: **Stat Upgrade Id** (ScriptableObject) + curve.
- **Upgrade database**: One asset that holds all rarities, all ability upgrades, and all stat upgrades. The game draws random offers from it when the upgrade panel opens.

## How to Use

### 1. Create rarities

1. In the Project window: **Right‑click → Create → GGJ-2026 → Upgrade Rarity**.
2. Name it (e.g. `Rarity_Common`, `Rarity_Rare`, `Rarity_Legendary`).
3. For each rarity set:
   - **Display Name**: Shown on the card (e.g. "Common", "Rare").
   - **Probability Weight**: Relative chance when drawing. Example: Common 60, Rare 30, Legendary 10. Weights don’t need to sum to 1.
   - **Value Multiplier**: Multiplier applied to the curve value. Example: Common 1, Rare 1.5, Legendary 2.
   - **Display Color**: Used to colour the rarity text and optional card border/background in the upgrade panel.

### 2. Create Stat Ids (ScriptableObjects)

- **Ability Stat Id**: Create via **Create → GGJ-2026 → Ability Stat Id**. Use the **same asset** in ability upgrade definitions and on the ability (e.g. Dash Ability has a “Dash Distance” stat id; the “Dash Distance” ability upgrade definition references the same asset). Matching is by reference.
- **Stat Upgrade Id**: Create via **Create → GGJ-2026 → Stat Upgrade Id**. Use the same asset in stat upgrade definitions and in your stat applier.

### 3. Create ability upgrades

1. **Right‑click → Create → GGJ-2026 → Ability Upgrade Definition**.
2. Configure:
   - **Display Name** / **Description**: Shown on the card.
   - **Ability Slot**: A, B, X, or Y (which button/slot).
   - **Stat Id**: Assign the **Ability Stat Id** ScriptableObject. Use the same asset as on the ability (e.g. Dash Ability’s “Dash Distance” stat id).
   - **Curve**: Animation curve: **X = level**, **Y = base value**. At runtime the ability’s current level is used; the result is multiplied by the rarity’s Value Multiplier. **This is the only source of the value**—abilities do not evaluate their own curves for upgrades.

Example: “Dash Distance” upgrade, slot A, stat id = same Ability Stat Id as on Dash Ability, curve from (1, 0.5) to (10, 3) → at level 1 the base value is 0.5, at level 10 it’s 3; with Rare (1.5x) at level 5 you get curve(5) × 1.5.

### 4. Create stat upgrades

1. **Right‑click → Create → GGJ-2026 → Stat Upgrade Definition**.
2. Set **Display Name**, **Description**, **Stat Id** (assign the **Stat Upgrade Id** ScriptableObject), and **Curve** (X = level/stacks, Y = value).
3. Stat application is implemented in code: override `PlayerUpgradeApplier.ApplyStatUpgrade(UpgradeOffer)` and use `offer.StatUpgradeIdRef` and `offer.EvaluateValue(level)`.

### 5. Create the upgrade database

1. **Right‑click → Create → GGJ-2026 → Upgrade Database**.
2. Name it (e.g. `UpgradeDatabase`).
3. In the inspector:
   - **Rarities**: Add all your rarity assets.
   - **Ability Upgrades**: Add all ability upgrade definitions.
   - **Stat Upgrades**: Add all stat upgrade definitions.

### 6. Hook up the panel

1. Select the **Upgrade Panel** (or the GameObject that has `UpgradePanel`).
2. Assign the **Upgrade Database** asset to the **Upgrade Database** field.
3. Set **Card Count** (e.g. 3) and **Ability Ratio** (0–1: 0 = all stat, 1 = all ability, 0.5 = half and half).

When the panel opens, it calls `UpgradeDatabase.GetRandomUpgrades(cardCount, abilityRatio)`: for each card it rolls a rarity (by weight), picks a random ability or stat definition, and shows an **UpgradeOffer** (definition + rarity) on the card. On choose, the applier uses the offer’s curve and rarity multiplier to compute the value and applies it (ability or stat).

## Flow Summary

1. **Designer** creates: Rarities (name, probability weight, value multiplier), Ability Upgrade Definitions (name, slot, stat id, curve), Stat Upgrade Definitions (name, stat id, curve), and one Upgrade Database that references them all.
2. **Runtime**: Panel opens → database returns N random **UpgradeOffer**s (each = one definition + one rarity) → cards show name, description, rarity.
3. **On choose**: `PlayerUpgradeApplier` gets the offer. If ability: get ability in slot, call `ability.ApplyUpgradeValue(offer.AbilityStatIdRef, offer.EvaluateValue(ability.level))`. The value is **definition curve at ability level × rarity multiplier**; the ability just applies the number. If stat: `ApplyStatUpgrade(offer)` (you implement this).
4. **Abilities**: Override `ApplyUpgradeValue(AbilityStatId statId, float value)` and, when `statId` matches the ability’s stat id asset (by reference), apply `value` (e.g. add to `dashDistance`).

## Rarity visualisation in the upgrade panel

- On each **Upgrade Rarity** asset, set **Display Color** (e.g. grey for Common, blue for Rare, gold for Legendary).
- On the **Card** prefab, assign a **Rarity Text** (TMP_Text): it will show the rarity name and use the rarity’s Display Color. Optionally assign **Rarity Border Image** (Image): it will be tinted with the rarity’s Display Color (e.g. card border or background).

## Adding new stats to an ability

1. Create an **Ability Stat Id** asset (e.g. `StatId_DashDistance`).
2. In the ability script, add a `[SerializeField] AbilityStatId yourStatId` and override `ApplyUpgradeValue(AbilityStatId statId, float value)`.
3. If `statId == yourStatId`, apply `value` (e.g. add to a field).
4. Create an **Ability Upgrade Definition** that references the **same** Ability Stat Id asset and set the curve. Add it to the database.
