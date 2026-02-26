//using System.Threading.Tasks.Dataflow;
using UnityEngine;
using Farming;
using Character;

/*
* A TileSelector that uses a raycast to detect which FarmTile the player is looking at and sets it as the active tile.
* Casts a ray forward from the player's position and checks if it hits a FarmTile within a specified distance.
* Exposes:
*   - activeTile (inherited from TileSelector)
* Requires:
*   - The FarmTile must have a collider for detection.
*/

public class RaySelector : TileSelector
{
    // Max distance for forward tile-selection raycast.
   [SerializeField] private float rayDistance = 5f;

    // Casts forward each frame to select/deselect farm tiles.
    void Update()
    {
        Ray ray = new Ray(transform.position, transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hitInfo, rayDistance))
        {
            if (hitInfo.collider.TryGetComponent<FarmTile>(out FarmTile tile))
            {
                SetActiveTile(tile);
            }
        }
        else // Did not hit anything.
        {
            SetActiveTile(null);
        }
    }
}
