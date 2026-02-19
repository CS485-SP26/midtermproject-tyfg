using Core;
using TMPro;
using UnityEngine;

namespace Farming
{
    public abstract class RewardControllerBase : MonoBehaviour
    {
        [Header("Reward Notification")]
        [SerializeField] private TMP_Text rewardText;
        [SerializeField] private string rewardTextObjectName = "RewardNotification";
        [SerializeField] private bool autoCreateRewardText = true;
        [SerializeField] private float notificationDurationSeconds = 2f;
        [SerializeField] private int notificationFontSize = 24;
        [SerializeField] private Color defaultTextColor = Color.white;

        [Header("Reward Notification Layout")]
        [SerializeField] private Vector2 anchor = new Vector2(0.5f, 0.5f);
        [SerializeField] private Vector2 size = new Vector2(700f, 60f);

        private float hideRewardAtTime = -1f;

        protected virtual void OnValidate()
        {
            if (notificationDurationSeconds < 0.1f)
                notificationDurationSeconds = 0.1f;

            if (notificationFontSize < 10)
                notificationFontSize = 10;
        }

        protected virtual void Start()
        {
            EnsureRewardText();
            SetRewardVisible(false);
        }

        protected virtual void Update()
        {
            if (rewardText != null && rewardText.gameObject.activeSelf && Time.time >= hideRewardAtTime)
                SetRewardVisible(false);
        }

        protected void AwardFundsAndNotify(int amount, string message, Color? color = null, bool richText = false)
        {
            if (amount <= 0)
                return;

            GameManager.Instance.AddFunds(amount);
            ShowNotification(message, color ?? defaultTextColor, richText);
        }

        protected void ShowNotification(string message, Color? color = null, bool richText = false)
        {
            if (string.IsNullOrWhiteSpace(message))
                return;

            if (rewardText == null)
                EnsureRewardText();

            if (rewardText == null)
                return;

            rewardText.richText = richText;
            rewardText.color = color ?? defaultTextColor;
            rewardText.text = message;
            SetRewardVisible(true);
            hideRewardAtTime = Time.time + notificationDurationSeconds;
        }

        private void SetRewardVisible(bool visible)
        {
            if (rewardText != null)
                rewardText.gameObject.SetActive(visible);
        }

        private void EnsureRewardText()
        {
            if (rewardText != null)
                return;

            rewardText = FindTextByName(rewardTextObjectName);
            if (rewardText != null || !autoCreateRewardText)
                return;

            Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            if (canvases == null || canvases.Length == 0)
                return;

            GameObject go = new GameObject(rewardTextObjectName, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(canvases[0].transform, false);

            TextMeshProUGUI label = go.GetComponent<TextMeshProUGUI>();
            label.alignment = TextAlignmentOptions.Center;
            label.fontSize = notificationFontSize;
            label.color = defaultTextColor;

            RectTransform rt = label.rectTransform;
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = size;

            rewardText = label;
        }

        private static TMP_Text FindTextByName(string targetName)
        {
            if (string.IsNullOrWhiteSpace(targetName))
                return null;

            TMP_Text[] allTexts = FindObjectsByType<TMP_Text>(FindObjectsSortMode.None);
            foreach (TMP_Text text in allTexts)
            {
                if (text != null && text.name == targetName)
                    return text;
            }

            return null;
        }
    }
}
