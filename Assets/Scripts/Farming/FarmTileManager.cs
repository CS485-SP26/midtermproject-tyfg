using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

/*
* This class manages farm tile grid creation/validation in the scene.
* Exposes:
*   - Editor/runtime tile cache refresh and grid instantiation utilities.
* Requires:
*   - A prefab for the farm tile that can be instantiated to create the grid.
*/

namespace Farming
{
    public class FarmTileManager:MonoBehaviour
    {
        // Prefab used to generate each farm tile instance.
        [SerializeField] private GameObject farmTilePrefab;
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
            RefreshTileCache();
        }

        // Subscribes tile day-advance handler when enabled.
        void OnEnable()
        {
            RefreshTileCache();
        }

        // Rebuilds runtime tile cache from this scene.
        private void RefreshTileCache()
        {
            tiles.Clear();
            FarmTile[] sceneTiles = FindObjectsByType<FarmTile>(FindObjectsSortMode.None);
            foreach (FarmTile tile in sceneTiles)
            {
                if (tile != null && tile.gameObject.scene == gameObject.scene)
                    tiles.Add(tile);
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
