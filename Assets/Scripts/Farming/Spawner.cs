using UnityEngine;
using System.Collections.Generic;

public class Spawner : MonoBehaviour
{
    // Grid dimensions for spawned prefab instances.
    [SerializeField] int rows = 4;
    [SerializeField] int cols = 4;
    // Prefab cloned for each grid cell.
    [SerializeField] GameObject prefab;
    // Runtime list of spawned clones for cleanup/rebuild.
    List<GameObject> spawnedObjects = new List<GameObject>();

    // Editor hook for rebuild behavior (currently disabled).
    void OnValidate()
    {
        // BuildGrid();
    }

    // Spawns a simple grid of prefabs under this object.
    void BuildGrid()
    {
        foreach (GameObject obj in spawnedObjects)
        {
            #if UNITY_EDITOR
            DestroyImmediate(obj);
            #else
            Destroy(obj);
            #endif
        }

        Vector3 spawnPos = transform.position;
        int count = 0;
        GameObject clone = null;

        for (int c = 0; c < cols; c++)
        {
            for (int r = 0; r < rows; r++)
            {
                clone = Instantiate(prefab, spawnPos, Quaternion.identity);
                spawnedObjects.Add(clone);
                clone.transform.SetParent(this.transform);
                spawnPos.x += 1.0f;
                count++;
            }

            spawnPos.x = transform.position.x;
            spawnPos.z += 1.0f;
        }
    }

    // Reserved startup hook.
    void Start()
    {
    }

    // Reserved frame update hook.
    void Update()
    {
    }
}
