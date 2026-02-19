# Part 6 Implementation Notes (My Work)

Last updated: February 19, 2026

## Summary
I implemented the Part 6 reward/purchase loop so it works in the current farm scene even before a full `Scene2-Store` exists. I also hardened the setup so rewards and currency UI work without fragile manual scene wiring.

## Main systems implemented

### 1) Persistent game state and scene loading
File: `Assets/Scripts/Core/GameManager.cs`

I expanded `GameManager` into a persistent singleton with:
- shared state: `Funds`, `Seeds`
- mutation methods: `AddFunds`, `TrySpendFunds`, `AddSeeds`, `TryConsumeSeeds`
- update events: `FundsChanged`, `SeedsChanged`
- progress flags: `SetFlag`, `IsFlagSet`, `ResetSessionData`
- scene loading methods: `LoadScenebyName`, `LoadSceneByName`
- audio listener safety: enforces a single enabled `AudioListener`

Compatibility path:
- `LoadScene : GameManager` kept for old serialized button references.

### 2) Win reward controller with auto-bootstrap
Files:
- `Assets/Scripts/Farming/RewardControllerBase.cs`
- `Assets/Scripts/Farming/FarmWinController.cs`

I created a reusable reward base and a farm win implementation:
- `RewardControllerBase` handles reward notifications (auto-find or auto-create TMP text)
- `FarmWinController` checks if all farmable tiles are watered
- reward is one-time per completed watered state using flag `farm_all_tiles_reward_given`
- controller auto-installs at runtime if farm tiles exist (no manual attachment required)

Important fix:
- win checks ignore `SeedPurchaseTile` objects so fake store tiles do not block the all-watered condition.

### 3) Fallback reward check directly from tile state changes
File: `Assets/Scripts/Farming/FarmTile.cs`

To avoid missing rewards due to scene setup timing:
- tile interaction/day-change now notifies `FarmWinController`
- added a direct fallback all-tiles reward evaluation in `FarmTile`
- fallback uses the same reward flag gate to prevent duplicate payouts

This means the reward still fires even if a controller object was not manually set in scene.

### 4) Currency UI auto-binding and robust updates
File: `Assets/Scripts/UI/CurrencyTextUI.cs`

I implemented auto-wired currency display with:
- runtime singleton bootstrap
- scene-load rebind to current `GameManager`
- object-name binding (`FundAmount` for funds, `SeedAmount` for seeds)
- event-driven updates (`FundsChanged`/`SeedsChanged`)
- polling fallback in `Update()` so UI still refreshes if event subscription is missed

### 5) Seed purchase architecture (shared base + multiple interaction modes)
Files:
- `Assets/Scripts/Farming/SeedPurchaseControllerBase.cs`
- `Assets/Scripts/Farming/SeedPurchaseTrigger.cs`
- `Assets/Scripts/Farming/SeedPurchaseTile.cs`
- `Assets/Scripts/UI/StorePurchaseController.cs`
- `Assets/Scripts/UI/FloatingTextPopup.cs`

I split purchase logic into a reusable base and mode-specific controllers:
- `SeedPurchaseControllerBase` centralizes spend/add-seed logic + notifications
- `SeedPurchaseTrigger` supports collider-based fake store purchases (with cooldown/repeat)
- `SeedPurchaseTile` supports tile-select + interact-key purchasing
- `StorePurchaseController` supports button-based purchasing for future real store scene
- `FloatingTextPopup` makes purchase notifications float up/fade for repeated buys

Notification format used:
- red cost: `-5$`
- green gain: `+1 seed` (or pluralized)

### 6) Scene/input safety bootstrap
File: `Assets/Scripts/UI/SceneUIBootstrap.cs`

I added runtime scene fixes to reduce setup regressions:
- reassign valid UI input actions when needed (`AssignDefaultActions`)
- rebind intro start button to load `Scene1-FarmingSim` if wiring is broken

### 7) Water refill quality-of-life for testing
Files:
- `Assets/Scripts/Farming/ShedWaterRefill.cs`
- `Assets/Scripts/Farming/Farmer.cs`

I added shed contact refill so full-tile watering is practical in test runs:
- `Farmer.RefillWaterToFull()`
- `ShedWaterRefill` trigger/collision call to refill

## What to apply in scene (current test flow)

### Funds UI
- Ensure TMP object named `FundAmount` exists.
- Optional seeds UI object named `SeedAmount`.

### Win reward
- No manual `FarmWinController` placement required (runtime bootstrap handles it).

### Fake store collider (second box collider)
- Add `SeedPurchaseTrigger` to the store zone object.
- Keep a `BoxCollider` on that object (usually `Is Trigger` enabled).
- Ensure player has `Farmer` on self or parent.
- Configure in inspector: `seedCost` (default 5), `seedsPerPurchase` (default 1), `purchaseCooldownSeconds`, `repeatWhileInside`.

### Optional tile-select purchase mode
- Add `SeedPurchaseTile` to a selected tile-like object when using interact-key purchase flow.

## Functional result currently achieved
- Water all farmable tiles -> funds are awarded.
- Reward is gated so it does not spam repeatedly while all tiles remain watered.
- Funds and seeds UI updates correctly.
- Seeds can be purchased using the fake store collider in Scene1.
- Purchase feedback appears as floating colored text.

## Known remaining work (outside current Part 6 code loop)
- Create and wire real `Scene2-Store`.
- Add farm <-> store travel UI flow.
- Final polish, team reflection, demo recording, and submission steps.
