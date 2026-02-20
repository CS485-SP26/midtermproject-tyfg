# SOLID Refactor Recommendations (No Code Changes Yet)

Last updated: February 20, 2026

## Scope
This document lists what to refactor next to better follow SOLID principles, without changing behavior right now.  
Focus scripts reviewed:
- `Assets/Scripts/Core/GameManager.cs`
- `Assets/Scripts/Farming/FarmTile.cs`
- `Assets/Scripts/Farming/FarmWinController.cs`
- `Assets/Scripts/Farming/RewardControllerBase.cs`
- `Assets/Scripts/Farming/SeedPurchaseControllerBase.cs`
- `Assets/Scripts/Farming/SeedPurchaseTrigger.cs`
- `Assets/Scripts/Farming/SeedPurchaseTile.cs`
- `Assets/Scripts/Farming/Farmer.cs`
- `Assets/Scripts/UI/CurrencyTextUI.cs`
- `Assets/Scripts/UI/StorePurchaseController.cs`
- `Assets/Scripts/UI/SceneUIBootstrap.cs`

## High-Impact Refactors (Do First)

### 1) Split `GameManager` by responsibility (SRP, DIP)
File reference: `Assets/Scripts/Core/GameManager.cs:8`

Current `GameManager` handles:
- singleton lifecycle and persistence
- funds/seeds state
- progress flags
- scene loading
- audio listener enforcement

Why this should change:
- Too many reasons to change one class.
- Farming-specific data (`Funds`, `Seeds`, flags) is mixed with engine-level concerns (scene loading, audio listener policy).
- Harder to test because everything is accessed through `GameManager.Instance`.

Suggested split:
- `SessionState` or `EconomyState`: owns `Funds`, `Seeds`, flags, events, reset behavior.
- `SceneNavigator`: owns scene name loading.
- `AudioListenerCoordinator`: owns "only one listener enabled" policy.
- `GameBootstrap`: creates/wires services and keeps `DontDestroyOnLoad`.

Suggested interfaces:
- `IEconomyService` (`AddFunds`, `TrySpendFunds`, `AddSeeds`, `TryConsumeSeeds`, events)
- `IProgressFlagStore` (`SetFlag`, `IsFlagSet`)
- `ISceneLoader` (`LoadSceneByName`)

Reasoning:
- This keeps game-specific systems depending on small contracts, not a global all-in-one manager.

### 2) Remove reward logic from `FarmTile` fallback (SRP, OCP)
File references:
- `Assets/Scripts/Farming/FarmTile.cs:10`
- `Assets/Scripts/Farming/FarmTile.cs:115`

Current issue:
- `FarmTile` now contains fallback reward policy (`FallbackAllTilesRewardFunds`, global scan, flag mutation).
- Tile entity is now responsible for both tile state and game economy outcome.

Why this should change:
- `FarmTile` should model tile behavior only (state transitions, visuals/audio for that tile).
- Reward policy should be centralized in one reward system.
- Duplicated reward logic now exists in both tile and win controller paths.

Suggested extraction:
- `IWinConditionEvaluator` + `FarmAllWateredEvaluator`
- `IRewardGrantService` (awards and gates one-time rewards)
- `FarmTile` only raises a tile-changed event

Reasoning:
- New reward rules can be added without touching tile core behavior.

### 3) Decouple purchase domain logic from UI spawning (SRP, DIP)
File reference: `Assets/Scripts/Farming/SeedPurchaseControllerBase.cs:7`

Current issue:
- Purchase transaction and TMP popup creation are in the same class.

Why this should change:
- Buying seeds and rendering popups are separate concerns.
- Hard to reuse purchase logic for non-UI contexts (tests, dedicated store scene).

Suggested split:
- `SeedPurchaseService`: validates spend, mutates funds/seeds, returns a typed result.
- `IPurchaseFeedbackPresenter`: displays success/failure text (floating popup, HUD text, sound, etc.).
- controllers (`SeedPurchaseTrigger`, `StorePurchaseController`, `SeedPurchaseTile`) only decide when a purchase attempt happens

Reasoning:
- Keeps interaction mode separate from transaction rules and separate from UI implementation.

## Medium-Impact Refactors

### 4) Refactor `CurrencyTextUI` into presenter + view (SRP)
File reference: `Assets/Scripts/UI/CurrencyTextUI.cs:7`

Current `CurrencyTextUI` does:
- singleton lifecycle
- GameManager resolution and subscription
- fallback polling
- UI label search/creation/layout
- label formatting

Suggested split:
- `CurrencyPresenter`: subscribes to economy events and passes values to view.
- `CurrencyView`: only sets text on known references.
- optional `CurrencyViewBootstrap`: handles auto-find/auto-create behavior.

Reasoning:
- clearer ownership and easier replacement if UI design changes.

### 5) Replace static/service-locator usage with dependencies (DIP)
Examples:
- `GameManager.Instance` usage in reward, purchase, and UI classes
- static bootstrap patterns in `SceneUIBootstrap` and `FarmWinController`

Suggested direction:
- inject interface references via serialized fields or a small installer/bootstrap object in scene
- keep one compatibility adapter initially to avoid big bang rewrite

Reasoning:
- lowers hidden coupling and improves testability.

### 6) Separate tile model from tile presentation (SRP)
File reference: `Assets/Scripts/Farming/FarmTile.cs:8`

Current `FarmTile` handles:
- condition state machine
- material changes
- highlight emission
- audio playbacks
- day-aging transitions
- reward side effects

Suggested split:
- `FarmTileState` (pure state transitions)
- `FarmTileView` (materials/highlight/audio)
- `FarmTileAging` (day-based decay handling)

Reasoning:
- visual/audio changes can evolve without risk to gameplay logic.

## Lower-Impact / Cleanup Refactors

### 7) Replace string constants with typed/config assets (OCP)
Examples:
- scene names in `SceneUIBootstrap`
- flag names like `farm_all_tiles_reward_given`
- UI object names (`FundAmount`, `SeedAmount`, `RewardNotification`)

Suggested direction:
- `ScriptableObject` config assets or centralized constants class
- optional enum-backed identifiers for known flags/reward ids

Reasoning:
- reduces typo-driven bugs and improves discoverability.

### 8) Reduce duplicate tile scans (SRP/performance side benefit)
Classes currently scanning global objects:
- `FarmWinController`, `PerTileWaterRewardController`, `FarmTile`, `CurrencyTextUI`, `SceneUIBootstrap`

Suggested direction:
- tile registry (`IFarmTileRegistry`) owned by `FarmTileManager`
- systems subscribe to registry and tile events instead of repeated global searches

Reasoning:
- cleaner architecture and less hidden scene-wide coupling.

### 9) Replace string-based tool selection in `Farmer` (OCP)
File reference: `Assets/Scripts/Farming/Farmer.cs:28`

Current issue:
- tool selection uses raw strings (`"GardenHoe"`, `"WaterCan"`).

Suggested direction:
- `enum ToolType` or dedicated `ITool` behavior classes.

Reasoning:
- compile-time safety and easier extension.

## SOLID Mapping Summary

### Single Responsibility Principle
Top offenders:
- `GameManager`
- `FarmTile`
- `CurrencyTextUI`
- `SeedPurchaseControllerBase`

### Open/Closed Principle
Top issues:
- adding new purchase/reward/UI rules often requires editing core classes instead of adding strategies/services.

### Liskov Substitution Principle
No severe violation found, but inheritance trees (`RewardControllerBase`, `SeedPurchaseControllerBase`) can become fragile if base classes keep accumulating unrelated responsibilities.

### Interface Segregation Principle
Current classes depend on broad `GameManager` API; narrow interfaces would reduce that.

### Dependency Inversion Principle
Most systems depend on concrete singletons/static entry points. Move toward interface-based dependencies and explicit composition.

## Proposed Refactor Order (Low Risk to Higher)

1. Introduce interfaces (`IEconomyService`, `IProgressFlagStore`, `ISceneLoader`) with adapter wrappers around current `GameManager`.
2. Extract purchase transaction logic from `SeedPurchaseControllerBase` into `SeedPurchaseService`.
3. Extract currency UI view/presenter split while preserving current auto-binding behavior.
4. Move reward fallback logic out of `FarmTile` into a dedicated evaluator/reward orchestrator.
5. Split `GameManager` into bootstrap + services and keep a thin compatibility facade temporarily.

## Notes on your `GameManager` question
Your intuition is correct: keeping one generic app-level manager and one farming-specific domain service is a better long-term structure.

Concrete target:
- `GameManager` becomes composition/bootstrap only.
- `FarmingGameState` (or `EconomyState`) becomes the domain-specific state holder.
- other systems depend on domain interfaces rather than directly on a global singleton.
