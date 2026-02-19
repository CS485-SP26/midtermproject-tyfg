using UnityEngine;

public class FloatingTextPopup : MonoBehaviour
{
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private float durationSeconds = 0.8f;
    private float risePixels = 40f;
    private float elapsedSeconds = 0f;
    private Vector2 startAnchoredPosition;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        if (rectTransform != null)
            startAnchoredPosition = rectTransform.anchoredPosition;
    }

    public void Configure(float duration, float rise)
    {
        durationSeconds = Mathf.Max(0.05f, duration);
        risePixels = Mathf.Max(0f, rise);
        elapsedSeconds = 0f;

        if (rectTransform == null)
            rectTransform = GetComponent<RectTransform>();
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        if (rectTransform != null)
            startAnchoredPosition = rectTransform.anchoredPosition;

        if (canvasGroup != null)
            canvasGroup.alpha = 1f;
    }

    private void Update()
    {
        elapsedSeconds += Time.deltaTime;
        float t = Mathf.Clamp01(elapsedSeconds / durationSeconds);

        if (rectTransform != null)
            rectTransform.anchoredPosition = startAnchoredPosition + new Vector2(0f, risePixels * t);

        if (canvasGroup != null)
            canvasGroup.alpha = 1f - t;

        if (t >= 1f)
            Destroy(gameObject);
    }
}
