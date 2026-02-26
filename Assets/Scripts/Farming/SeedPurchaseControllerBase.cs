using Core;
using TMPro;
using UnityEngine;

/*
* This class checks for the win condition of the farm scene, which is when all farmable tiles are watered. It periodically checks the state
     of all farm tiles and awards the player with funds if a tile is newly watered.
* Exposes:
*   - NotifyTileStatePotentiallyChanged(): A static method that can be called by farm tiles when their state changes to trigger a 
*    re-evaluation of the win condition.
* Requires:
*   - A reference to the GameManager to check and set flags for reward distribution.
*/

namespace Farming
{
    public abstract class SeedPurchaseControllerBase : MonoBehaviour
    {
        [Header("Purchase Settings")]
        [SerializeField] protected int seedCost = 5;
        [SerializeField] protected int seedsPerPurchase = 1;

        [Header("Floating Notification")]
        [SerializeField] private Canvas notificationCanvas;
        [SerializeField] private Vector2 notificationAnchor = new Vector2(0.5f, 0.42f);
        [SerializeField] private Vector2 notificationSize = new Vector2(520f, 50f);
        [SerializeField] private int notificationFontSize = 22;
        [SerializeField] private float notificationRisePixels = 40f;
        [SerializeField] private float notificationDurationSeconds = 0.9f;
        [SerializeField] private string insufficientFundsMessage = "Not enough funds to buy seeds.";

        protected IEconomyService economyService;

        // Validates purchase and notification configuration values.
        protected virtual void OnValidate()
        {
            if (seedCost < 1)
                seedCost = 1;

            if (seedsPerPurchase < 1)
                seedsPerPurchase = 1;

            if (notificationFontSize < 10)
                notificationFontSize = 10;

            if (notificationRisePixels < 0f)
                notificationRisePixels = 0f;

            if (notificationDurationSeconds < 0.1f)
                notificationDurationSeconds = 0.1f;
        }

        // Resolves economy service dependency.
        protected virtual void Awake()
        {
            economyService = GameManager.Instance;
        }

        // Attempts purchase transaction, emits notification, and invokes success/fail hooks.
        protected bool TryPurchaseAndNotify()
        {
            if (economyService == null)
                economyService = GameManager.Instance;

            if (economyService != null && economyService.TrySpendResource(EconomyResource.Funds, seedCost))
            {
                economyService.AddResource(EconomyResource.Seeds, seedsPerPurchase);
                SpawnFloatingNotification(BuildPurchaseSuccessMessage(), true);
                OnPurchaseSucceeded();
                return true;
            }

            SpawnFloatingNotification($"<color=#ff4d4d>{insufficientFundsMessage}</color>", true);
            OnPurchaseFailed();
            return false;
        }

        // Optional subclass hook called after successful purchase.
        protected virtual void OnPurchaseSucceeded() { }
        // Optional subclass hook called after failed purchase attempt.
        protected virtual void OnPurchaseFailed() { }

        // Returns current funds balance from economy service.
        protected int GetFundsBalance()
        {
            if (economyService == null)
                economyService = GameManager.Instance;

            return economyService == null ? 0 : economyService.GetResourceAmount(EconomyResource.Funds);
        }

        // Builds formatted success message for UI notification.
        private string BuildPurchaseSuccessMessage()
        {
            string seedWord = seedsPerPurchase == 1 ? "seed" : "seeds";
            return $"<color=#ff4d4d>-{seedCost}$</color>  <color=#4dff88>+{seedsPerPurchase} {seedWord}</color>";
        }

        // Spawns floating UI text popup to communicate purchase outcome.
        protected void SpawnFloatingNotification(string message, bool richText)
        {
            Canvas canvas = ResolveCanvas();
            if (canvas == null)
                return;

            GameObject go = new GameObject("PurchaseNotification", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(CanvasGroup), typeof(FloatingTextPopup));
            go.transform.SetParent(canvas.transform, false);

            TextMeshProUGUI label = go.GetComponent<TextMeshProUGUI>();
            label.richText = richText;
            label.text = message;
            label.alignment = TextAlignmentOptions.Center;
            label.fontSize = notificationFontSize;
            label.color = Color.white;

            RectTransform rt = label.rectTransform;
            rt.anchorMin = notificationAnchor;
            rt.anchorMax = notificationAnchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = notificationSize;

            FloatingTextPopup popup = go.GetComponent<FloatingTextPopup>();
            popup.Configure(notificationDurationSeconds, notificationRisePixels);
        }

        // Finds an active canvas for notification placement.
        private Canvas ResolveCanvas()
        {
            if (notificationCanvas != null)
                return notificationCanvas;

            Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            foreach (Canvas canvas in canvases)
            {
                if (canvas != null && canvas.isActiveAndEnabled)
                {
                    notificationCanvas = canvas;
                    return notificationCanvas;
                }
            }

            return null;
        }
    }
}
