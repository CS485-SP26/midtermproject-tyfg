using UnityEngine;

namespace Farming
{
    // Collider-driven purchase mode (fake store zone). Player can buy by entering/staying in trigger.
    public class SeedPurchaseTrigger : SeedPurchaseControllerBase
    {
        [Header("Trigger Purchase Timing")]
        [SerializeField] private float purchaseCooldownSeconds = 1.2f;
        [SerializeField] private bool repeatWhileInside = true;

        private float nextPurchaseTime = 0f;

        protected override void OnValidate()
        {
            base.OnValidate();

            if (purchaseCooldownSeconds < 0.05f)
                purchaseCooldownSeconds = 0.05f;
        }

        private void OnTriggerEnter(Collider other)
        {
            TryPurchaseFromCollider(other);
        }

        private void OnTriggerStay(Collider other)
        {
            if (repeatWhileInside)
                TryPurchaseFromCollider(other);
        }

        private void OnCollisionEnter(Collision collision)
        {
            TryPurchaseFromCollider(collision.collider);
        }

        private void OnCollisionStay(Collision collision)
        {
            if (repeatWhileInside)
                TryPurchaseFromCollider(collision.collider);
        }

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
