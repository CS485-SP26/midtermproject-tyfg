using UnityEngine;

namespace Farming
{
    // Tile-select purchase mode. Requires selecting this tile and pressing the normal interact key (F).
    [RequireComponent(typeof(FarmTile))]
    public class SeedPurchaseTile : SeedPurchaseControllerBase
    {
        // Minimum time between interaction-triggered purchases.
        [SerializeField] private float interactionCooldownSeconds = 0.2f;
        private float nextInteractionTime = 0f;

        // Clamps cooldown to a sensible minimum.
        protected override void OnValidate()
        {
            base.OnValidate();

            if (interactionCooldownSeconds < 0.05f)
                interactionCooldownSeconds = 0.05f;
        }

        // Attempts purchase from a farmer interaction while respecting cooldown.
        public bool TryPurchaseFromFarmer(Farmer farmer)
        {
            if (farmer == null || Time.time < nextInteractionTime)
                return false;

            nextInteractionTime = Time.time + interactionCooldownSeconds;
            return TryPurchaseAndNotify();
        }
    }
}
