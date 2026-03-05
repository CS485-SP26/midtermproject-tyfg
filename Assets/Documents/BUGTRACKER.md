# Documentation Plan

## Folder Structure (recommended)

- `Docs/`
- `Docs/BugTracker.md` (live doc)
- `Docs/KnownIssues.md` (live doc)
- `Docs/DevLogs/` (dated progress notes)
- `Docs/Screenshots/` (development screenshots + short captions)
- `Docs/Screenshots/README.md` (index of screenshots and what they show)

## Live Bug Tracker (starting entries)

### Bug: “All tiles watered” reward fails after planting
- **Symptom:** Reward works only if every tile stays in `Watered`; planting one tile prevents reward.
- **Check these functions:**
- `FarmWinController.EvaluateWinCondition`
- `FarmWinController.AreAllTilesWatered`
- `FarmTile.Interact`
- `FarmTile.PlantSeed`
- `FarmTile.EvaluateAllTilesRewardFallback`
- **Relevant files:**
- [FarmWinController.cs](c:/Cs485GameProgramming/NewMidterm/midtermproject-tyfg/Assets/Scripts/Farming/FarmWinController.cs)
- [FarmTile.cs](c:/Cs485GameProgramming/NewMidterm/midtermproject-tyfg/Assets/Scripts/Farming/FarmTile.cs)

### Bug: Store UI does not reflect day/time passing
- **Symptom:** Time appears frozen in store UI, but progresses when returning to farm.
- **Check these functions:**
- `DayController.Update`
- `DayController.UpdateDayLabel`
- `DayController.ResolveDayLabelIfNeeded`
- `DayController.SyncFromRuntimeWithCatchUp`
- `SceneUIBootstrap.EnsurePersistentGameplayHud`
- `SceneUIBootstrap.PromoteHudObjectsFromScene`
- **Relevant files:**
- [DayController.cs](c:/Cs485GameProgramming/NewMidterm/midtermproject-tyfg/Assets/Scripts/Environment/DayController.cs)
- [SceneUIBootstrap.cs](c:/Cs485GameProgramming/NewMidterm/midtermproject-tyfg/Assets/Scripts/UI/SceneUIBootstrap.cs)

### Bug: Energy appears paused in store, then refilled on exit
- **Symptom:** Store bar looks static; on exit, energy is high/full.
- **Check these functions:**
- `Farmer.OnDisable`
- `Farmer.RegenerateEnergyIfIdle`
- `Farmer.SetEnergyLevel`
- `FarmerResourceState.Update`
- `FarmerResourceState.SetFarmerPresent`
- `FarmerResourceState.ApplyValuesToBars`
- `FarmerResourceState.HandleSceneLoaded`
- **Relevant files:**
- [Farmer.cs](c:/Cs485GameProgramming/NewMidterm/midtermproject-tyfg/Assets/Scripts/Farming/Farmer.cs)
- [FarmerResourceState.cs](c:/Cs485GameProgramming/NewMidterm/midtermproject-tyfg/Assets/Scripts/Farming/FarmerResourceState.cs)

### Bug: Plant visuals/state mismatch (`Plant_3`, withered confusion, Turnip defaults)
- **Symptom:** Withered model and inspector values look inconsistent with expected carrot-only setup.
- **Check these functions:**
- `Plant.FixedUpdate`
- `Plant.SetState`
- `Plant.UpdateVisuals`
- `Plant.RestoreFromSnapshot`
- `FarmTile.SimulateElapsedOffSceneTime`
- `FarmTile.ApplySnapshot`
- **Relevant files:**
- [Plant.cs](c:/Cs485GameProgramming/NewMidterm/midtermproject-tyfg/Assets/Scripts/Farming/Plant.cs)
- [FarmTile.cs](c:/Cs485GameProgramming/NewMidterm/midtermproject-tyfg/Assets/Scripts/Farming/FarmTile.cs)
- [Plant.prefab](c:/Cs485GameProgramming/NewMidterm/midtermproject-tyfg/Assets/Prefabs/Plant.prefab)
