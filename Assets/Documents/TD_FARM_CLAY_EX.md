# Creating a New Soil Type: TD_Farm_Clay

This guide explains how to create a new soil type using the **definition-driven tile system**. Clay soil is used as the example, but the same steps apply to any soil type such as **Sand, Fertile, Volcanic**, or others.

No new scripts are required.

---

## What TD_Farm_Clay Is

`TD_Farm_Clay` is a **ScriptableObject asset** created from `FarmTileDefinition`. It defines how a **Clay Soil Farm Tile** behaves.

The `FarmTile` script reads this definition **at runtime** and adjusts its behavior accordingly.

### Clay Soil Characteristics

- Holds water longer than normal soil
- Slows plant growth slightly
- Supports tilling, watering, and planting
- Still counts toward reward logic based on moisture level

All of this behavior is controlled by **data, not code**.

---

## Step 1 — Create the ScriptableObject

In **Unity**:

Right-click in the **Project window**

```
Create → Farming → Tiles → Farm Tile Definition
```

Name the new asset:

```
TD_Farm_Clay
```

This asset now represents the **Clay Soil tile type**.

---

## Step 2 — Configure Clay Soil Properties

Open **TD_Farm_Clay** in the **Inspector**.

### Soil Profile

```
Soil Profile = Custom
```

### Recommended Clay Soil Values

- **Growth Multiplier:** `0.85`  
  Clay is dense and slightly slows root expansion.

- **Water Decay Multiplier:** `0.55`  
  Clay retains moisture longer than normal soil.

- **Supports Tilling:** enabled  
- **Supports Watering:** enabled  
- **Supports Planting:** enabled  

These values can be adjusted later **without touching code**.

---

## Step 3 — Assign TD_Farm_Clay to a Tile

Select any tile in the scene that has a **FarmTile** component.

In the **Inspector**:

```
FarmTile
 └ Tile Definition → TD_Farm_Clay
```

That tile is now a **Clay Soil Farm Tile**.

### Behavior Changes

The tile will now:

- Dry out slower
- Grow plants slightly slower
- Count toward water-reward logic
- Behave like a normal farm tile, but with **Clay soil rules**

No scripts or prefabs need to change.

---

## How FarmTile Uses TD_Farm_Clay

At runtime:

- `FarmTile` reads the **Clay definition**
- Plant growth uses the **Clay growth multiplier**
- Water decay uses the **Clay decay multiplier**
- Reward logic uses `CountsForWaterReward` and `IsWateredForReward`
- Farmer actions respect Clay’s **capabilities**

The **ScriptableObject is the data**, and `FarmTile` is the **behavior that follows it**.

---

## Summary

Clay soil is created entirely through **data configuration**:

- One new **ScriptableObject asset**
- No new scripts
- No code duplication
- No special tile prefabs

This demonstrates how the **definition-driven tile system** supports **unlimited soil types with zero additional code**.

Additional soil types such as:

- `TD_Farm_Soft`
- `TD_Farm_Rough`
- `TD_Farm_Fertile`

can be created using the **same process**, simply by adjusting the definition values.