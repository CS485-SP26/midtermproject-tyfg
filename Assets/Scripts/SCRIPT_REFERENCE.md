# Scripts Reference

This document describes what each script file contributes, what it depends on from other project scripts, and how it works.

## Character

### `Assets/Scripts/Character/AnimatedController.cs`
- **Provides:** Animation parameter/trigger bridge from gameplay state to `Animator`.
- **Asks From:** `MovementController` for movement/grounding values.
- **How:** Reads `GetHorizontalSpeedPercent`, grounded/velocity/crouch/jump flags every frame and writes animator params; exposes trigger helper methods.

### `Assets/Scripts/Character/CameraFollow.cs`
- **Provides:** Simple camera follow behavior.
- **Asks From:** Player transform (`GameObject player`).
- **How:** In `LateUpdate`, sets camera position to `player.position + offset`.

### `Assets/Scripts/Character/GroundProximity.cs`
- **Provides:** `nearGround` boolean for proximity-to-ground checks.
- **Asks From:** Ground colliders by layer/tag.
- **How:** Trigger enter/exit sets `nearGround` when colliders match configured ground rules.

### `Assets/Scripts/Character/MovementController.cs`
- **Provides:** Base movement state and API (`Move`, `Jump`, crouch/walk/sprint flags, grounded/jump tracking).
- **Asks From:** `Rigidbody`, `CapsuleCollider`, optional `GroundProximity`, collision contacts.
- **How:** Stores input and movement state, detects ground collisions, manages jump availability and crouch capsule changes; physics motion is deferred to subclasses.

### `Assets/Scripts/Character/PhysicsController.cs` (`PhysicsMovement`)
- **Provides:** Actual Rigidbody-based movement, rotation, jump application, and velocity clamping.
- **Asks From:** Base `MovementController` fields/state (`moveInput`, sprint/walk/crouch flags, jump queue).
- **How:** In `FixedUpdate`, applies acceleration toward target speed, caps horizontal speed, smooth-turns with `Quaternion.Slerp`, and consumes queued jump.

### `Assets/Scripts/Character/PlayerController.cs`
- **Provides:** Input action handlers for movement, jump, sprint, crouch, interact, and emote trigger.
- **Asks From:** `MovementController`, `AnimatedController`, `Farmer`, `TileSelector`.
- **How:** Forwards input to movement; routes farming interactions to `Farmer`; applies stamina-gated jump/sprint when farmer exists.

### `Assets/Scripts/Character/TileSelector.cs`
- **Provides:** Shared selected-tile state with highlight management.
- **Asks From:** `FarmTile`.
- **How:** `SetActiveTile` unhighlights previous tile, swaps reference, highlights new tile.

### `Assets/Scripts/Character/PointSelector.cs`
- **Provides:** Trigger-volume based tile selection strategy.
- **Asks From:** `FarmTile` colliders.
- **How:** On trigger enter/exit, sets or clears current selected tile via `TileSelector`.

### `Assets/Scripts/Character/RaySelector.cs`
- **Provides:** Raycast-based tile selection strategy.
- **Asks From:** `FarmTile` hit by forward raycast.
- **How:** Each frame raycasts forward and selects hit tile, or clears selection if no hit.

## Core

### `Assets/Scripts/Core/EconomyResource.cs`
- **Provides:** Shared resource enum keys (`Funds`, `Seeds`, `SkillPoints`).
- **Asks From:** Nothing.
- **How:** Enum used by economy interfaces and systems.

### `Assets/Scripts/Core/IEconomyService.cs`
- **Provides:** Economy contract (`GetResourceAmount`, `AddResource`, `TrySpendResource`, `ResourceChanged` event).
- **Asks From:** `EconomyResource`.
- **How:** Interface implemented by `GameManager` and consumed by purchase/reward systems.

### `Assets/Scripts/Core/GameManager.cs` (`GameManager`, `LoadScene`)
- **Provides:** Singleton economy/state manager, resource events, scene loading wrappers, session flags, audio-listener safety.
- **Asks From:** `EconomyResource`, `IEconomyService`, `SceneManager`.
- **How:** Initializes balances from starting data, clamps/updates balances, emits generic + per-resource events, tracks flags, supports resets, and ensures one active audio listener.

### `Assets/Scripts/Core/AutoDestroy.cs`
- **Provides:** Timed self-destruction utility behavior.
- **Asks From:** Nothing.
- **How:** Decrements lifespan by `Time.deltaTime` and destroys object at expiry.

## Environment

### `Assets/Scripts/Environment/DayController.cs`
- **Provides:** Day progression timer, day UI text updates, sun rotation visuals, `dayPassedEvent`.
- **Asks From:** `Light` (sun), optional TMP label, listeners like `FarmTileManager`.
- **How:** Tracks elapsed time; when day length reached, increments day, resets timer, updates label, invokes event, updates sun rotation continuously.

## Farming

### `Assets/Scripts/Farming/Farmer.cs`
- **Provides:** Player farming resource logic (energy/water), sprint drain/regen, tile interactions, tool visuals, feedback popups.
- **Asks From:** `AnimatedController`, `MovementController`, `FarmerResourceState`, `FarmTile`, `SeedPurchaseTile`, `ProgressBar`, `FloatingTextPopup`.
- **How:** Consumes resources for actions, blocks actions when insufficient, syncs bars + persistent resource state, handles tile condition interactions, and emits UI feedback.

### `Assets/Scripts/Farming/FarmerResourceState.cs`
- **Provides:** Cross-scene persistent farmer resource store and bar syncing.
- **Asks From:** `ProgressBar`, scene load callbacks.
- **How:** Singleton persists energy/water between scenes, can regen while farmer absent, rebinding bars each scene and pushing normalized values.

### `Assets/Scripts/Farming/FarmTile.cs`
- **Provides:** Tile state machine (`Grass/Tilled/Watered/Planted`), visuals/highlight, day decay, plant spawning/watering logic.
- **Asks From:** `Plant`, `SeedPurchaseTile`, `FarmWinController`, `GameManager`.
- **How:** `Interact` advances tile state, day ticks decay state/wither plants, updates materials/audio, and triggers win/reward reevaluation hooks.

### `Assets/Scripts/Farming/FarmTileManager.cs`
- **Provides:** Farm grid creation/management and day-based tile advancement.
- **Asks From:** `FarmTile`, `DayController`.
- **How:** Maintains tile list, subscribes to `dayPassedEvent`, calls `OnDayPassed` on each tile; editor helpers rebuild grid on validation.

### `Assets/Scripts/Farming/Plant.cs`
- **Provides:** Crop lifecycle model (`Planted -> Growing -> Mature` or `Withered`) and state visuals.
- **Asks From:** Water input from `FarmTile.Water`.
- **How:** Decays water over time, transitions states on thresholds/timers, toggles correct model per state.

### `Assets/Scripts/Farming/RewardControllerBase.cs`
- **Provides:** Shared reward-notification and funds-award utility for reward controllers.
- **Asks From:** `GameManager`, `TMP_Text`.
- **How:** Finds/creates reward label, shows timed notification text, and centralizes `AwardFundsAndNotify`.

### `Assets/Scripts/Farming/PerTileWaterRewardController.cs`
- **Provides:** Incremental rewards when tiles newly become `Watered`.
- **Asks From:** `FarmTile`, `RewardControllerBase`.
- **How:** Polls tile conditions at intervals, compares against `lastSeenCondition`, pays per newly-watered tile and shows message.

### `Assets/Scripts/Farming/FarmWinController.cs`
- **Provides:** "All tiles watered" win reward flow with scene bootstrap.
- **Asks From:** `FarmTile`, `SeedPurchaseTile` exclusion, `RewardControllerBase`, `GameManager`.
- **How:** Periodically checks all farmable tiles; awards once via session flag and exposes static `NotifyTileStatePotentiallyChanged` for immediate rechecks.

### `Assets/Scripts/Farming/SeedPurchaseControllerBase.cs`
- **Provides:** Shared seed purchase transaction + floating notification logic.
- **Asks From:** `IEconomyService` (`GameManager`), `FloatingTextPopup`, `Canvas`.
- **How:** Tries spending `Funds`, adds `Seeds` on success, triggers success/fail hooks, and spawns styled popup text.

### `Assets/Scripts/Farming/SeedPurchaseTile.cs`
- **Provides:** Tile interaction purchase mode.
- **Asks From:** `Farmer`, `SeedPurchaseControllerBase`.
- **How:** `TryPurchaseFromFarmer` enforces cooldown then calls base purchase flow.

### `Assets/Scripts/Farming/SeedPurchaseTrigger.cs`
- **Provides:** Trigger/collision purchase zone mode.
- **Asks From:** `Farmer`, `SeedPurchaseControllerBase`.
- **How:** On trigger/collision enter/stay (optionally repeating), finds farmer in collider parent and attempts purchase with cooldown.

### `Assets/Scripts/Farming/ShedWaterRefill.cs`
- **Provides:** Water refill interaction zone.
- **Asks From:** `Farmer`.
- **How:** On trigger/collision contact, finds farmer and calls `RefillWaterToFull`.

### `Assets/Scripts/Farming/Spawner.cs`
- **Provides:** Generic prefab grid spawner utility (currently mostly dormant).
- **Asks From:** Prefab reference.
- **How:** `BuildGrid` destroys old spawned objects and instantiates a rows/cols grid under this transform.

## UI

### `Assets/Scripts/UI/ProgressBar.cs`
- **Provides:** Self-healing progress bar UI component with fill/text/color API.
- **Asks From:** `Image`/`TextMeshProUGUI` children.
- **How:** Auto-binds or creates missing background/fill/label elements, applies style constraints, and exposes `Fill`, `SetText`, `SetFillColor`.

### `Assets/Scripts/UI/FloatingTextPopup.cs`
- **Provides:** Reusable floating + fade-out popup behavior.
- **Asks From:** `RectTransform`, `CanvasGroup`.
- **How:** On update, interpolates vertical offset and alpha over duration, then destroys itself.

### `Assets/Scripts/UI/CurrencyTextUI.cs`
- **Provides:** Funds/seeds HUD display, scene bootstrap, auto-binding/fallback label creation, intro-scene hide logic.
- **Asks From:** `GameManager` events, TMP labels, optional `SceneUIBootstrap`-managed HUD objects.
- **How:** Ensures instance exists, binds to `FundsChanged`/`SeedsChanged`, auto-finds or creates text targets, updates labels on events or polling fallback.

### `Assets/Scripts/UI/StorePurchaseController.cs`
- **Provides:** Store-scene purchase button orchestration and seed buying from UI.
- **Asks From:** `SeedPurchaseControllerBase`, `GameManager`, scene buttons/canvas.
- **How:** Auto-installs in store scenes, binds or creates purchase button, styles from reference buttons, handles click purchase flow, cleans generated buttons outside store.

### `Assets/Scripts/UI/ShopEnterZone.cs`
- **Provides:** World trigger that shows "enter store" button and loads store scene.
- **Asks From:** Player tag, assigned UI button.
- **How:** Shows button on player trigger enter, hides on exit, and loads configured store scene from button handler.

### `Assets/Scripts/UI/LeaveStoreButton.cs`
- **Provides:** Simple button handler to return to farming scene.
- **Asks From:** `SceneManager`.
- **How:** `LeaveStore()` loads `Scene1-FarmingSim`.

### `Assets/Scripts/UI/SceneUIBootstrap.cs`
- **Provides:** Runtime scene UI bootstrap/repair pipeline (intro redirect, persistent HUD, intro overlay creation, store controller injection, cleanup rules).
- **Asks From:** `CurrencyTextUI`, `StorePurchaseController`, `ProgressBar`, Unity UI/EventSystem stack.
- **How:** Hooks scene load events, conditionally redirects startup to intro, promotes HUD objects to persistent canvas, creates/fixes missing intro/store UI pieces, and removes scene-inappropriate leftovers.

## Editor

### `Assets/Scripts/Editor/ForceIntroPlayModeStartScene.cs`
- **Provides:** Editor-only play mode start scene enforcement.
- **Asks From:** UnityEditor scene APIs.
- **How:** On editor load, sets `EditorSceneManager.playModeStartScene` to `Scene0-Intro` if asset exists.
