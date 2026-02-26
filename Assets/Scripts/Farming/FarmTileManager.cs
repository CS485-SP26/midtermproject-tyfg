using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Environment;

/*
* This class manages the farm tiles in the game. It is responsible for instantiating the grid of farm tiles based on specified rows and columns, 
    and it listens for day passed events to update the state of all farm tiles accordingly.
* Exposes:
*   - OnDayPassed(): A method that should be called when a day passes in the game, which will update the state of all farm tiles.
* Requires:
*   - A prefab for the farm tile that can be instantiated to create the grid.
*   - A reference to the DayController to subscribe to day passed events.
*/

namespace Farming
{
    public class FarmTileManager:MonoBehaviour
    {
        // Prefab used to generate each farm tile instance.
        [SerializeField] private GameObject farmTilePrefab;
        // Day system event source that advances tile state each day.
        [SerializeField] DayController dayController;
        // Grid dimensions.
        [SerializeField] private int rows = 4;
        [SerializeField] private int cols = 4;
        // Gap between instantiated tiles.
        [SerializeField] private float tileGap = 0.1f;
        // Runtime list of managed tile instances.
        private List<FarmTile> tiles = new List<FarmTile>();
        
        // Validates required references.
        void Start()
        {
            Debug.Assert(farmTilePrefab, "FarmTileManager requires a farmTilePrefab");
            Debug.Assert(dayController, "FarmTileManager requires a dayController");
        }

        // Subscribes tile day-advance handler when enabled.
        void OnEnable()
        {
            dayController.dayPassedEvent.AddListener(this.OnDayPassed);
        }

        // Unsubscribes tile day-advance handler when disabled.
        void OnDisable()
        {
            dayController.dayPassedEvent.RemoveListener(this.OnDayPassed);            
        }

        // UnityEvent callback for one-day progression.
        public void OnDayPassed()
        {
            IncrementDays(1);
        }

        // Advances every tile by the requested number of in-game days.
        public void IncrementDays(int count)
        {
            while (count > 0)
            {
                foreach (FarmTile farmTile in tiles)
                {
                    farmTile.OnDayPassed();
                }
                count--;
            }
        }

        // Instantiates/positions tiles to form a rows x cols grid.
        void InstantiateTiles()
        {
            Vector3 spawnPos = transform.position;
            int count = 0;
            GameObject clone = null; 

            for (int c = 0; c < cols; c++)
            {
                for (int r = 0; r < rows; r++)
                {
                    clone = Instantiate(farmTilePrefab, spawnPos, Quaternion.identity);
                    clone.name = "Farm Tile " + count++.ToString();
                    spawnPos.x += clone.transform.localScale.x + tileGap;
                    clone.transform.parent = transform; // build heirarchy
                    tiles.Add(clone.GetComponent<FarmTile>()); // for resize/delete
                }
                spawnPos.z += clone.transform.localScale.z + tileGap;
                spawnPos.x = transform.position.x;
            }
        }

        // ***************************************************************** //
        // Below this line is code to support the Unity Editor (Advanced)
        // Please feel free to disregard everything below this
        // ***************************************************************** //
        // Defers grid validation to editor delay-call so hierarchy is safe to modify.
        void OnValidate()
        {
            #if UNITY_EDITOR
            EditorApplication.delayCall += () => {
                if (this == null) return; // Guard against the object being deleted
                ValidateGrid();
            };
            #endif
        }

        // Rebuilds grid in editor when dimensions/prefab become out of sync.
        void ValidateGrid() 
        {
            if (!farmTilePrefab) return;
            tiles.Clear();
            foreach (Transform child in transform)
            {
                if (child.gameObject.TryGetComponent<FarmTile>(out var tile))
                {
                    tiles.Add(tile);
                }
            }

            int newCount = rows * cols;

            if (tiles.Count != newCount)
            {
                DestroyTiles();
                InstantiateTiles();
            }
        }

        // Destroys all tracked tile instances and clears local list.
        void DestroyTiles()
        {
            foreach (FarmTile tile in tiles)
            {
                #if UNITY_EDITOR
                DestroyImmediate(tile.gameObject);
                #else
                Destroy(tile.gameObject);
                #endif
            }
            tiles.Clear();
        }
    }
}
