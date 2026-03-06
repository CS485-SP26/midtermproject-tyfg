using Farming;
using UnityEngine;

namespace Character
{
    /// <summary>
    /// Central place for deciding whether a player can farm right now.
    /// Defaults keep current behavior unchanged (allowed).
    /// </summary>
    public class FarmAccessGate : MonoBehaviour
    {
        [Header("Gate Defaults")]
        [SerializeField] private bool farmingEnabled = true;
        [SerializeField] private bool requireFarmerComponent = true;
        [SerializeField] private bool requireSelectedTile = true;

        public bool FarmingEnabled => farmingEnabled;

        public void SetFarmingEnabled(bool enabled)
        {
            farmingEnabled = enabled;
        }

        public bool CanFarmNow(Farmer farmer, FarmTile selectedTile)
        {
            if (!farmingEnabled)
                return false;

            if (requireFarmerComponent && farmer == null)
                return false;

            if (requireSelectedTile && selectedTile == null)
                return false;

            return true;
        }
    }
}
