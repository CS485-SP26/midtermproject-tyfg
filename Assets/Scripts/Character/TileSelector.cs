using UnityEngine;
using Farming;


/*
* This class does not select tiles by itself; it stores/manages the currently selected tile.
*/
namespace Character 
{
    public abstract class TileSelector : MonoBehaviour
    {
        // Active tile reference (serialized mainly for runtime debugging).
        [SerializeField] protected FarmTile activeTile;

        // Returns the current selected tile.
        public FarmTile GetSelectedTile()
        {
            return activeTile;
        }

        // Updates selected tile and handles highlight transition between old/new tile.
        protected void SetActiveTile(FarmTile tile)
        {
            if (activeTile != tile)
            {
                activeTile?.SetHighlight(false);
                activeTile = tile;
                activeTile?.SetHighlight(true);
            }
        }
    }
}
