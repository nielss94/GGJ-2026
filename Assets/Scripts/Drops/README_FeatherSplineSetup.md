# Feather spline attachment – correct setup

Feathers attach along a spline on the player's mask when the **same** drop type Id and a **valid** spline are configured. Use this checklist to verify or fix the setup.

## How to tell attachment is working

- **Working:** Feathers sit in an **arc** on the mask (along the half-circle spline), spaced evenly. Particle effects **stop** as soon as each feather attaches (they are stopped and cleared, then the particle GameObjects are destroyed).
- **Not working:** Feathers **cluster** at one spot (usually where they flew to) and/or **keep emitting particles**. That usually means either the spline setup wasn’t used (type mismatch or missing/empty Spline Container) or particles weren’t stopped on attach (fixed in code: stop + clear, then destroy).

## 1. Prefabs and assets

| What | Where | Check |
|------|--------|------|
| **Player prefab** (has Spline + MaskAttachmentReceiver) | `Assets/Prefabs/Player.prefab` | Use this prefab in scenes, not `CH_Witchman.prefab` alone. |
| **Feather drop definition** | `Assets/Systems/Drops/Feather.asset` | Drop Type = FeatherId. |
| **Feather type Id** | `Assets/Systems/Drops/FeatherId.asset` | Name or Display Name = "Feather" (used for matching). |
| **Drop database** | `Assets/Systems/DropDatabase.asset` | Must list `Feather.asset` so enemies can drop feathers. |
| **Feather prefab** | `Assets/Prefabs/Drops/Feather.prefab` | Referenced by `Feather.asset`; has `DroppableItem`. |

## 2. Player prefab – MaskAttachmentReceiver

On the **Mask** (or object with `MaskAttachmentReceiver`):

- **Spline Setups** – at least one entry:
  - **Drop Type**: assign `FeatherId.asset` (or any DropTypeId whose **Id** is `"Feather"`).
  - **Spline Container**: assign the `SplineContainer` on the **Spline** child (same prefab). Must not be null.
- **Mask Transform** / **Fly To Target**: set as needed so drops fly to the mask.

Matching uses **DropTypeId.Id** (name or display name), not the asset reference. Same Id = same type.

## 3. Player prefab – Spline

A child object **Spline** (same prefab as the Mask) must have:

- **SplineContainer** – with at least one spline (e.g. half‑circle). If empty, feathers will not be placed along the spline.
- **HalfCircleSplineBuilder** (optional) – use **Build Half Circle** in the context menu to fill the spline.

The **MaskAttachmentReceiver** → Spline Setups → **Spline Container** must reference this `SplineContainer`. If the reference is missing or the spline has no knots, feathers will not attach to the spline.

## 4. In scenes

- The **Player** in the scene must be an instance of **Player.prefab** (the variant that includes the Spline and MaskAttachmentReceiver), so that the spline and mask setup exist at runtime.
- Enemies that should drop feathers need an **EnemyDropper** with **Drop Database** set to the asset that includes `Feather.asset`.

## 5. Quick verification

1. Open `Player.prefab`.
2. Find the object with **MaskAttachmentReceiver** (e.g. Mask).
3. In **Spline Setups**, confirm one entry has **Drop Type** = FeatherId and **Spline Container** = the Spline’s `SplineContainer` (non‑null).
4. Select the **Spline** child and confirm **SplineContainer** has at least one spline with knots (e.g. 7 knots for a half‑circle).

If all of the above are correct and feathers still don’t attach, the code now matches by **Id** and only uses a setup when **HasValidSpline** is true; check the console for errors and that the correct Player prefab is used in the scene.
