using UnityEngine;
using Farming;


/*
* This class doesn't select the tile it manages the tile that is selected 
*/
namespace Character 
{
    public abstract class TileSelector : MonoBehaviour
    {
        [SerializeField] protected FarmTile activeTile; // good for debugging
        public FarmTile GetSelectedTile() { return activeTile; }

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
