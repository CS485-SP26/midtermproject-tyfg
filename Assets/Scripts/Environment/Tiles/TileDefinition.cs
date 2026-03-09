using UnityEngine;

namespace Environement.Tiles
{
    // Base tile definition for identity and shared metadata across any tile domain.
    public abstract class TileDefinition : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string tileTypeId = "tile";
        [SerializeField] private string displayName = "Tile";

        public string TileTypeId => string.IsNullOrWhiteSpace(tileTypeId) ? name : tileTypeId;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    }
}
