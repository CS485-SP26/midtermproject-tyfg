using UnityEngine;

public class ShedWaterRefill : MonoBehaviour
{
    [SerializeField] private bool logRefill = false;

    private void OnTriggerEnter(Collider other)
    {
        TryRefill(other);
    }

    private void OnCollisionEnter(Collision collision)
    {
        TryRefill(collision.collider);
    }

    private void TryRefill(Collider other)
    {
        if (other == null)
            return;

        Farmer farmer = other.GetComponentInParent<Farmer>();
        if (farmer == null)
            return;

        farmer.RefillWaterToFull();

        if (logRefill)
            Debug.Log("Water refilled at shed.");
    }
}
