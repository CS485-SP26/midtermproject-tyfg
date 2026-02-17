using UnityEngine;
using Farming;

namespace Character
{
    public class PointSelector : TileSelector
    {
        private void OnTriggerEnter(Collider other)
        {
            // TryGetComponent is faster than GetComponent if the component is uncertain
            if(other.TryGetComponent<FarmTile>(out FarmTile tile))
            {
                SetActiveTile(tile);
            }
        }
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