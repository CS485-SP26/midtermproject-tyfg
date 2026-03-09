using UnityEngine;

public class ShedWaterRefill : MonoBehaviour
{
    // Optional debug log when a refill occurs.
    [SerializeField] private bool logRefill = false;

    // Trigger entry hook for refill zones.
    private void OnTriggerEnter(Collider other)
    {
        TryRefill(other);
    }

    // Collision entry hook for non-trigger refill objects.
    private void OnCollisionEnter(Collision collision)
    {
        TryRefill(collision.collider);
    }

    // Finds farmer on collider hierarchy and refills their water resource.
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
