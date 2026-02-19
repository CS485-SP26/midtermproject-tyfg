# HW5 Team Checklist (Farming 1: Multiple Scenes)

## Project Setup / GitHub
- [x] Classroom repo is in use: `C:\Cs485GameProgramming\NewMidterm\midtermproject-tyfg`
- [x] Commit rename changes: `LoadScene.cs` -> `GameManager.cs`
- [x] Push latest `main` so all teammates can pull cleanly
- [x] Each teammate makes at least one visible commit

## Part 3 - GameManager (Scenes + Persistence)
- [x] `Assets/Scripts/Core/GameManager.cs` exists
- [ ] Remove `using UnityEditor.Build.Content;` from `GameManager.cs`
- [ ] Add persistent data fields (funds, seeds)
- [ ] Add methods for data operations (`AddFunds`, `TrySpendFunds`, `AddSeeds`, etc.)
- [ ] Keep singleton + `DontDestroyOnLoad` working without duplicates
- [ ] Verify Intro button calls `GameManager.LoadScenebyName(...)`
- [ ] Update any stale `LoadScene` references in Scene0 button events

## Part 4 - Build Store Scene
- [ ] Create `Assets/Scenes/Scene2-Store.unity`
- [ ] Add store visuals/layout
- [ ] Add `Exit Store` UI button (loads `Scene1-FarmingSim`)
- [ ] Add funds text in store scene (same value as farm scene)
- [ ] Save and commit store scene changes

## Part 5 - Scene Navigation Loop
- [ ] Add shop location object in `Scene1-FarmingSim`
- [ ] Add player-near-shop detection (trigger, raycast, or equivalent)
- [ ] Show `Enter Store` UI button when player is in range
- [ ] Hook `Enter Store` button to load `Scene2-Store`
- [ ] Verify travel Farm -> Store -> Farm repeatedly without errors

## Part 6 - Purchase Loop
- [x] Add win condition in farm scene: all tiles watered simultaneously
- [ ] Show congrats message when win condition is met
- [x] Award funds once (no infinite repeat)
- [ ] Store scene: show `Purchase Seeds` button with cost
- [ ] Purchase consumes funds and increases seeds
- [ ] Add/update UI text: `Seeds: [count]`
- [ ] Verify full gameplay loop: water -> earn funds -> buy seeds

## Build Settings
- [x] `Scene0-Intro` in build settings
- [x] `Scene1-FarmingSim` in build settings
- [ ] Add `Scene2-Store` to build settings
- [ ] Confirm scene names match exact strings used in button events

## Polish / Optional S-Rank
- [ ] Store visuals upgrade
- [ ] Mini-map camera
- [ ] Gameplay optimization(s) documented
- [ ] Dependency/project-management notes

## Submission / Reflection
- [ ] Reflection Q1 answered (dependency mapping + blockers)
- [ ] Reflection Q2 answered (task split + merge conflicts)
- [ ] Reflection Q3 answered (team overhead vs benefit)
- [ ] Record demo video showing core game loop
- [ ] Create/push `HW5 Complete` checkpoint commit
- [ ] Submit on Canvas before **Fri Feb 20, 2026 11:59 PM**

## Team Tracking (edit as you go)
- [x] Teammate A: _________Anthony Douglas_________
- [ ] Teammate B: ________Tyler Usrey__________
- [ ] Teammate C: ________Julian__________
- [ ] Teammate D: __________________
