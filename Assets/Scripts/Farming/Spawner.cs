using UnityEngine;
using System.Collections.Generic;
public class Spawner : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    [SerializeField] int rows = 4;
    [SerializeField] int cols = 4;
    [SerializeField]  GameObject prefab;
    List<GameObject> spawnedObjects = new List<GameObject>();

    void OnValidate()
    {
        BuildGrid();
    }
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
    void Start()
    {
       
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
