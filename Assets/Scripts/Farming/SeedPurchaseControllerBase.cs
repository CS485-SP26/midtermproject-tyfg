using Core;
using TMPro;
using UnityEngine;

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

        protected GameManager gameManager;

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

        protected virtual void Awake()
        {
            gameManager = GameManager.Instance;
        }

        protected bool TryPurchaseAndNotify()
        {
            if (gameManager == null)
                gameManager = GameManager.Instance;

            if (gameManager.TrySpendFunds(seedCost))
            {
                gameManager.AddSeeds(seedsPerPurchase);
                SpawnFloatingNotification(BuildPurchaseSuccessMessage(), true);
                OnPurchaseSucceeded();
                return true;
            }

            SpawnFloatingNotification($"<color=#ff4d4d>{insufficientFundsMessage}</color>", true);
            OnPurchaseFailed();
            return false;
        }

        protected virtual void OnPurchaseSucceeded() { }
        protected virtual void OnPurchaseFailed() { }

        private string BuildPurchaseSuccessMessage()
        {
            string seedWord = seedsPerPurchase == 1 ? "seed" : "seeds";
            return $"<color=#ff4d4d>-{seedCost}$</color>  <color=#4dff88>+{seedsPerPurchase} {seedWord}</color>";
        }

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
