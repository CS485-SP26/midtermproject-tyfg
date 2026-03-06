# Resource State Architecture (Energy First Migration)

## Goal
Decouple gameplay logic from HUD wiring so gameplay systems update resource state, and UI presenters render state to bars.

## What Changed

### 1) `IResourceState` (core contract)
- File: `Assets/Scripts/Core/IResourceState.cs`
- Defines:
  - `Current`
  - `Max`
  - `ValueChanged` event

This allows UI code to bind to resources without knowing gameplay classes.

### 2) `EnergyState` (runtime model)
- File: `Assets/Scripts/Farming/EnergyState.cs`
- Responsibilities:
  - Holds energy runtime value (`current`, `max`)
  - Applies off-actor regen when actor is not present
  - Emits `ValueChanged` events
  - Persists across scenes (`DontDestroyOnLoad` singleton)

### 3) `ResourceBarPresenter` (view/presenter)
- File: `Assets/Scripts/UI/ResourceBarPresenter.cs`
- Responsibilities:
  - Resolves an `IResourceState` source (currently Energy)
  - Resolves a `ProgressBar` by name/token
  - Applies fill + optional style/label
  - Rebinds on scene load

## Farmer Integration
- File: `Assets/Scripts/Farming/Farmer.cs`
- `Farmer` now uses `EnergyState` for energy source-of-truth updates (`SetEnergyLevel` -> `EnergyState.SetValue`).
- `Farmer` toggles actor presence on `OnEnable/OnDisable/OnDestroy` for off-scene regen behavior.
- Water remains on existing path for compatibility.

## Why This Helps
- Removes direct HUD dependency from main gameplay paths.
- Prevents bar-crossbinding style bugs caused by per-system bar lookup/write logic.
- Creates a reusable pattern for future resources (water, health, stamina variants).

## Current Compatibility Notes
- This is a non-breaking migration step.
- Existing systems still run; energy now has state+presenter path introduced.
- Water is intentionally left on existing implementation until its tool/state migration is completed.

## Next Steps (recommended)
1. Move water to tool-owned runtime state (`WaterCanToolState`) with optional fallback.
2. Add `ResourceBarPresenter` bindings for water (state-driven, no direct farmer bar writes).
3. Remove remaining direct `ProgressBar` writes from gameplay classes.
4. Split `FarmerResourceState` into:
   - actor energy/stamina state
   - tool water state
   - HUD presentation/binding
