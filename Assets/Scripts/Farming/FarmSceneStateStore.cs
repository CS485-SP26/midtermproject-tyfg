using System.Collections.Generic;
using UnityEngine;

namespace Farming
{
    // Runtime-only cross-scene cache for farm tile state snapshots.
    public static class FarmSceneStateStore
    {
        public struct FarmTileSnapshot
        {
            public FarmTile.Condition TileCondition;
            public float WaterAmount;
            public int DaysSinceLastInteraction;
            public bool HasPlant;
            public PlantState PlantState;
            public float PlantGrowTimer;
            public float SavedAtRealtimeSeconds;
        }

        private static readonly Dictionary<string, FarmTileSnapshot> tileSnapshots = new Dictionary<string, FarmTileSnapshot>();

        // Clears cached snapshots when play mode/runtime subsystem resets.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            tileSnapshots.Clear();
        }

        // Stores (or overwrites) a snapshot for a tile key.
        public static void SaveTileState(string tileKey, FarmTileSnapshot snapshot)
        {
            if (string.IsNullOrWhiteSpace(tileKey))
                return;

            tileSnapshots[tileKey] = snapshot;
        }

        // Reads a previously stored snapshot for a tile key.
        public static bool TryGetTileState(string tileKey, out FarmTileSnapshot snapshot)
        {
            if (string.IsNullOrWhiteSpace(tileKey))
            {
                snapshot = default;
                return false;
            }

            return tileSnapshots.TryGetValue(tileKey, out snapshot);
        }

        // Clears all cached farm tile snapshots.
        public static void ClearAll()
        {
            tileSnapshots.Clear();
        }
    }
}
