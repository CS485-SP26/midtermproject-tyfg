# Changes to Tile Handling

## Tile Definition

<div align="center">

### *TileDefinition.cs*

</div>

This class is the **base identity asset** for all tile types in the system. In other words, it is the *root ScriptableObject* that every specific tile definition will inherit from. It does not contain behavior or gameplay rules — only the **universal metadata** that every tile type should share.

### What this class represents

- What kind of tile is this? (`tileTypeId`)
- What should this tile be called in UI or debugging? (`displayName`)

### Why it’s abstract

Marking it `abstract` means you never create a **generic `TileDefinition` asset**. Instead, you create **specific tile definitions**, such as:

- `FarmTileDefinition`
- `RoughSoilFarmTileDefinition`
- `ShopTileDefinition`
- `TeleportTileDefinition`

Each of these inherits the identity fields from **TileDefinition**, ensuring every tile shares a consistent base identity while allowing specialized data to be added in subclasses.

---

## FarmTile Definition

<div align="center">

### *FarmTileDefinition.cs*

</div>

This class extends `TileDefinition` and defines **data specific to farming tiles**.

This is the correct place to store:

- Soil profile (Normal, Soft, Rough, Custom)
- Growth multipliers
- Water-decay multipliers
- Farming-related capabilities

Placing this data in the ScriptableObject makes the farming system **data-driven instead of hardcoded**.

Example soil behaviors:

- **Soft Soil** → faster growth, slower dryout  
- **Rough Soil** → slower growth, faster dryout  

Each soil type is simply represented by **a different ScriptableObject asset** created from `FarmTileDefinition`. This allows designers to tweak farming behavior without modifying code.

---

## FarmTile

<div align="center">

### *FarmTile.cs — Runtime Behavior*

</div>

`FarmTile` is the **runtime component** responsible for executing farming behavior in the game world. Instead of hardcoding rules, it **reads the data from its associated `FarmTileDefinition`** and applies the logic during gameplay.

The following definition-driven methods were added:

- `CountsForWaterReward`
- `IsWateredForReward`
- `GetGrowthMultiplier`
- `SupportsTilling`
- `SupportsWatering`
- `SupportsPlanting`

### System Flow

The system now follows a clear separation of responsibilities:

1. **The ScriptableObject defines the rules**  
   (`TileDefinition` / `FarmTileDefinition`)

2. **FarmTile reads those rules**  
   It accesses the definition data attached to the tile.

3. **FarmTile executes the behavior**  
   Gameplay logic is applied based on the definition values.

This design cleanly separates:

- **Data** → ScriptableObjects (`TileDefinition`, `FarmTileDefinition`)
- **Behavior** → Runtime components (`FarmTile`)

### Benefits of this Approach

- Easier balancing and tweaking without code changes
- Cleaner separation between systems
- Scales well as new tile types are added
- Reduces hardcoded gameplay logic
- Makes tile behavior configurable through assets