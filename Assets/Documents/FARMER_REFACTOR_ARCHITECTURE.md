# Farmer Refactor + HUD Ownership Notes

## Scope
This document tracks the recent `Farmer` refactor and the UI resource-bar architecture changes made to stop bar desync/cross-binding issues.

## Core Decisions
1. `FarmerResourceState` is the single driver for resource bar rendering.
2. `Farmer` updates gameplay resource values only, then pushes those values to shared state systems.
3. Direct, multi-writer bar updates were removed from `Farmer`.
4. `ResourceBarPresenter` is no longer auto-created at runtime.

## Why This Was Changed
1. Energy/water bars were being updated by multiple systems at once.
2. Scene transitions (farm/store/shed) caused bar rebinding conflicts.
3. This produced symptoms like wrong bar colors, wrong bar fill source, and bars swapping ownership.

## New Farmer Structure
`Farmer` was split into partials and helper classes to reduce responsibilities.

### Partial files
1. `Assets/Scripts/Farming/Farmer.cs`
- Data shell only: serialized config + dependencies + backing fields.

2. `Assets/Scripts/Farming/Farmer.Lifecycle.cs`
- Validation, startup wiring, migration hooks, actor-present toggles, lifecycle setup.

3. `Assets/Scripts/Farming/Farmer.Resources.cs`
- Runtime consume/regenerate/set logic and push-to-state methods.

4. `Assets/Scripts/Farming/Farmer.Actions.cs`
- Public gameplay actions, tile interaction entry point, blocked-action feedback calls, `IFarmActor` contract methods.

### Extracted helper classes
1. `Assets/Scripts/Farming/FarmerRuntimeResources.cs`
- Owns current energy/water values and mutation rules.

2. `Assets/Scripts/Farming/FarmerToolVisuals.cs`
- Owns tool model visibility switching (`GardenHoe`, `WaterCan`).

3. `Assets/Scripts/Farming/FarmerActionFeedback.cs`
- Owns feedback settings + cooldown-based feedback display trigger.

## HUD/Bar Binding Behavior
Bar binding and style logic is centralized in:
- `Assets/Scripts/Farming/FarmerResourceState.cs`

Key behavior:
1. Finds bars by exact configured names (`EnergyBar`, `WaterBar`).
2. If missing, falls back to a template bar (`ProgressBar`) and clones companions.
3. Applies canonical labels/colors in one place.
4. Re-resolves safely on scene load.

## Legacy Data Migration
To avoid breaking existing inspector assignments, legacy serialized fields are retained as hidden fields in `Farmer` and migrated once into new extracted objects:
1. Tool visuals (`wateringCan`, `gardenHoe`) -> `FarmerToolVisuals`.
2. Feedback fields (`notificationCanvas`, anchor/size/font/cooldown) -> `FarmerActionFeedbackSettings`.

Migration flags:
1. `legacyToolVisualsMigrated`
2. `legacyFeedbackMigrated`

## Compile/Verification
Validation run:
1. `dotnet build Assembly-CSharp.csproj`
2. Result: success (warnings only from unrelated camera/event code).

## Test Checklist
1. Start game and confirm both bars appear with correct labels/colors.
2. Sprint until energy drains, stop sprinting, verify regen behavior.
3. Till/water/plant loop, verify water and energy bars update correctly.
4. Trigger reward while consuming stamina, confirm bars remain stable.
5. Enter and exit store, verify bars continue updating and do not swap.
6. Refill water at shed, verify only water bar changes.
7. Restart play mode once and verify legacy inspector references still work.

## Next Refactor Targets
1. Split economic concerns from `Farmer` (`TryConsumeSeeds`) into a dedicated adapter/service.
2. Move stat/config hydration (`ApplyStatsDefinitionIfAssigned`) into a dedicated loader component.
3. Optionally move sprint gating into a dedicated movement-energy bridge class.
