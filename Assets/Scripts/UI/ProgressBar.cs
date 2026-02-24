using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ProgressBar : MonoBehaviour
{
    [Header("Bar Parts")]
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Image fillImage;
    [SerializeField] private TextMeshProUGUI fillText;

    private void Awake()
    {
        EnsureVisualStructure();
        ApplyLockedStyle();
    }

    private void Reset()
    {
        EnsureVisualStructure();
        ApplyLockedStyle();
    }

    private void OnValidate()
    {
        EnsureVisualStructure();
        ApplyLockedStyle();
    }

    public float Fill
    {
        set
        {
            EnsureVisualStructure();
            float clamped = Mathf.Clamp01(value);
            if (fillImage == null)
                return;

            fillImage.fillAmount = clamped;
            fillImage.enabled = clamped > 0.01f;
        }
    }

    public void SetText(string text)
    {
        EnsureVisualStructure();
        if (fillText != null)
            fillText.text = text;
    }

    public void SetFillColor(Color color)
    {
        EnsureVisualStructure();
        if (fillImage != null)
            fillImage.color = color;
    }

    private void EnsureVisualStructure()
    {
        // First pass: bind anything already present in the hierarchy.
        AutoBindExistingChildren();

        // Second pass: synthesize missing companions so all bars render with a boxed frame.
        if (fillImage != null && backgroundImage == null)
            backgroundImage = CreateBackgroundFromFill(fillImage);

        if (backgroundImage != null && fillImage == null)
            fillImage = CreateFillFromBackground(backgroundImage);

        if (fillText == null)
            fillText = FindTextByName("Fill text");

        if (fillText == null)
            fillText = CreateCenteredLabel();

        AutoBindExistingChildren();
    }

    private void AutoBindExistingChildren()
    {
        if (backgroundImage == null)
            backgroundImage = FindImageByName("Max");

        if (fillImage == null)
            fillImage = FindImageByName("Fill");

        if (fillImage == null)
            fillImage = FindFilledImageFallback();

        if (fillText == null)
            fillText = FindTextByName("Fill text");

        if (fillText == null)
            fillText = GetComponentInChildren<TextMeshProUGUI>(true);
    }

    private void ApplyLockedStyle()
    {
        if (backgroundImage != null)
        {
            backgroundImage.enabled = true;
            backgroundImage.color = new Color(1f, 1f, 1f, Mathf.Max(0.7f, backgroundImage.color.a));
        }

        if (fillImage == null)
            return;

        if (backgroundImage != null)
        {
            RectTransform fillRect = fillImage.rectTransform;
            RectTransform backgroundRect = backgroundImage.rectTransform;
            CopyRectTransform(backgroundRect, fillRect);
            backgroundImage.transform.SetAsLastSibling();
        }

        fillImage.type = Image.Type.Filled;
        fillImage.fillMethod = Image.FillMethod.Horizontal;
        fillImage.fillOrigin = 0;
        fillImage.fillClockwise = true;
        fillImage.fillAmount = Mathf.Clamp01(fillImage.fillAmount);
    }

    private Image FindImageByName(string targetName)
    {
        if (string.IsNullOrWhiteSpace(targetName))
            return null;

        Image[] images = GetComponentsInChildren<Image>(true);
        foreach (Image image in images)
        {
            if (image != null && image.name == targetName)
                return image;
        }

        return null;
    }

    private Image FindFilledImageFallback()
    {
        Image[] images = GetComponentsInChildren<Image>(true);
        foreach (Image image in images)
        {
            if (image != null && image.type == Image.Type.Filled)
                return image;
        }

        return null;
    }

    private static void CopyRectTransform(RectTransform source, RectTransform target)
    {
        if (source == null || target == null)
            return;

        target.anchorMin = source.anchorMin;
        target.anchorMax = source.anchorMax;
        target.pivot = source.pivot;
        target.anchoredPosition = source.anchoredPosition;
        target.sizeDelta = source.sizeDelta;
        target.localRotation = source.localRotation;
        target.localScale = source.localScale;
    }

    private Image CreateBackgroundFromFill(Image fill)
    {
        if (fill == null)
            return null;

        GameObject go = new GameObject("Max", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        RectTransform backgroundRect = go.GetComponent<RectTransform>();
        RectTransform fillRect = fill.rectTransform;
        Transform parent = fillRect.parent == null ? transform : fillRect.parent;
        go.transform.SetParent(parent, false);
        CopyRectTransform(fillRect, backgroundRect);

        Image image = go.GetComponent<Image>();
        image.color = Color.white;
        image.raycastTarget = fill.raycastTarget;
        if (fill.sprite != null)
        {
            image.sprite = fill.sprite;
            image.type = Image.Type.Sliced;
        }
        else
        {
            Sprite uiSprite = GetDefaultUISprite();
            if (uiSprite != null)
            {
                image.sprite = uiSprite;
                image.type = Image.Type.Sliced;
            }
        }

        go.transform.SetSiblingIndex(fill.transform.GetSiblingIndex() + 1);
        return image;
    }

    private Image CreateFillFromBackground(Image background)
    {
        if (background == null)
            return null;

        GameObject go = new GameObject("Fill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        RectTransform fillRect = go.GetComponent<RectTransform>();
        RectTransform backgroundRect = background.rectTransform;
        Transform parent = backgroundRect.parent == null ? transform : backgroundRect.parent;
        go.transform.SetParent(parent, false);
        CopyRectTransform(backgroundRect, fillRect);

        Image image = go.GetComponent<Image>();
        image.color = new Color(0.2f, 0.38f, 0.88f, 1f);
        image.sprite = background.sprite != null ? background.sprite : GetDefaultUISprite();
        image.type = Image.Type.Filled;
        image.fillMethod = Image.FillMethod.Horizontal;
        image.fillAmount = 1f;
        image.raycastTarget = background.raycastTarget;
        go.transform.SetSiblingIndex(Mathf.Max(0, background.transform.GetSiblingIndex() - 1));
        return image;
    }

    private static Sprite GetDefaultUISprite()
    {
        Sprite sprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/UISprite.psd");
        if (sprite == null)
            sprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/Background.psd");

        return sprite;
    }

    private TextMeshProUGUI FindTextByName(string targetName)
    {
        if (string.IsNullOrWhiteSpace(targetName))
            return null;

        TextMeshProUGUI[] texts = GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach (TextMeshProUGUI text in texts)
        {
            if (text != null && text.name == targetName)
                return text;
        }

        return null;
    }

    private TextMeshProUGUI CreateCenteredLabel()
    {
        Transform parent = backgroundImage != null ? backgroundImage.transform : transform;
        if (parent == null)
            return null;

        GameObject go = new GameObject("Fill text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        RectTransform rect = go.GetComponent<RectTransform>();
        go.transform.SetParent(parent, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = Vector2.zero;

        TextMeshProUGUI text = go.GetComponent<TextMeshProUGUI>();
        text.alignment = TextAlignmentOptions.Center;
        text.fontSize = 20f;
        text.color = Color.white;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.text = "Bar";
        return text;
    }
}
