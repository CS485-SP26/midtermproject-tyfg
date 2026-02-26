using UnityEngine;

/*
* This class manages the purchase of seeds when the player enters a trigger zone. It checks for the presence of a Farmer component in the 
*   colliding object and attempts to process a seed purchase if the player is eligible and the cooldown has passed. 
*   It relies on the base class SeedPurchaseControllerBase for the actual purchase logic and notification handling.
* Exposes:
*   - None (the purchase is triggered by collisions, so there are no public methods to call directly)
* Requires:
*   - A reference to the economy service (inherited from SeedPurchaseControllerBase) to handle the purchase transaction.
*   - The colliding object must have a Farmer component in its parent hierarchy for the purchase to be attempted.
*   - The trigger collider must be properly set up on the GameObject for collision detection to work.
*/

namespace Farming
{
    // Collider-driven purchase mode (fake store zone). Player can buy by entering/staying in trigger.
    public class SeedPurchaseTrigger : SeedPurchaseControllerBase
    {
        [Header("Trigger Purchase Timing")]
        // Time delay between automatic purchases from trigger/collision contact.
        [SerializeField] private float purchaseCooldownSeconds = 1.2f;
        // If true, purchases can repeat while player remains inside.
        [SerializeField] private bool repeatWhileInside = true;

        private float nextPurchaseTime = 0f;

        // Clamps purchase cooldown to a sensible minimum.
        protected override void OnValidate()
        {
            base.OnValidate();

            if (purchaseCooldownSeconds < 0.05f)
                purchaseCooldownSeconds = 0.05f;
        }

        // Attempts one purchase when a collider enters trigger.
        private void OnTriggerEnter(Collider other)
        {
            TryPurchaseFromCollider(other);
        }

        // Optionally attempts repeated purchases while inside trigger.
        private void OnTriggerStay(Collider other)
        {
            if (repeatWhileInside)
                TryPurchaseFromCollider(other);
        }

        // Attempts one purchase when a collider collision begins.
        private void OnCollisionEnter(Collision collision)
        {
            TryPurchaseFromCollider(collision.collider);
        }

        // Optionally attempts repeated purchases while collision persists.
        private void OnCollisionStay(Collision collision)
        {
            if (repeatWhileInside)
                TryPurchaseFromCollider(collision.collider);
        }

        // Resolves farmer from collider hierarchy and performs purchase if eligible.
        private void TryPurchaseFromCollider(Collider other)
        {
            if (other == null || Time.time < nextPurchaseTime)
                return;

            Farmer farmer = other.GetComponentInParent<Farmer>();
            if (farmer == null)
                return;

            nextPurchaseTime = Time.time + purchaseCooldownSeconds;
            TryPurchaseAndNotify();
        }
    }
}
