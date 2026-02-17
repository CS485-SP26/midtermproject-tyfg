//using System.Threading.Tasks.Dataflow;
using UnityEngine;
using Farming;
using Character;

public class RaySelector : TileSelector
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
   [SerializeField] private float rayDistance = 5f;
    // Update is called once per frame
    void Update()
    {
        Ray ray = new Ray(transform.position, transform.forward);
        if(Physics.Raycast(ray, out RaycastHit hitInfo, rayDistance))
        {
            if(hitInfo.collider.TryGetComponent<FarmTile>(out FarmTile tile))
            {
                SetActiveTile(tile);
            }
        }
        else//didnt hit anything
        {
            SetActiveTile(null);
        }
    }
}
