using UnityEngine;
using Farming;

/*
* A TileSelector that detects when the player is within a trigger collider of a FarmTile and sets it as the active tile.
* Uses OnTriggerEnter and OnTriggerExit to detect when the player enters or exits the tile's trigger collider.
* Exposes:
*   - activeTile (inherited from TileSelector)
* Requires:
*   - The FarmTile must have a trigger collider for detection.
*/

namespace Character
{
    public class PointSelector : TileSelector
    {
        // Selects a farm tile when the selector trigger enters that tile's collider.
        private void OnTriggerEnter(Collider other)
        {
            // TryGetComponent is faster than GetComponent if the component is uncertain.
            if (other.TryGetComponent<FarmTile>(out FarmTile tile))
            {
                SetActiveTile(tile);
            }
        }

        // Clears selection when exiting the currently selected tile collider.
        private void OnTriggerExit(Collider other)
        {
            other.TryGetComponent<FarmTile>(out var tile);
            if (activeTile == tile)
            {
                SetActiveTile(null);
            }
        }
    }
}
