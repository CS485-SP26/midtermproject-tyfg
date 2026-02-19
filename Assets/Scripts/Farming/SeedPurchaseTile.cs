using UnityEngine;

namespace Farming
{
    // Tile-select purchase mode. Requires selecting this tile and pressing the normal interact key (F).
    [RequireComponent(typeof(FarmTile))]
    public class SeedPurchaseTile : SeedPurchaseControllerBase
    {
        [SerializeField] private float interactionCooldownSeconds = 0.2f;
        private float nextInteractionTime = 0f;

        protected override void OnValidate()
        {
            base.OnValidate();

            if (interactionCooldownSeconds < 0.05f)
                interactionCooldownSeconds = 0.05f;
        }

        public bool TryPurchaseFromFarmer(Farmer farmer)
        {
            if (farmer == null || Time.time < nextInteractionTime)
                return false;

            nextInteractionTime = Time.time + interactionCooldownSeconds;
            return TryPurchaseAndNotify();
        }
    }
}
