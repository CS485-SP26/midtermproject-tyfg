using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ProgressBar : MonoBehaviour
{
    [SerializeField] private Image fillImage;
    [SerializeField] private TextMeshProUGUI fillText;

    /// <summary>
    /// Sets the fill amount of the progress bar (0–1).
    /// Automatically clamps and hides fill if near zero.
    /// </summary>
    public float Fill
    {
        set
        {
            float clamped = Mathf.Clamp01(value);
            fillImage.fillAmount = clamped;

            // Optional: hide fill when nearly empty
            fillImage.enabled = (clamped > 0.01f);
        }
    }

    /// <summary>
    /// Sets the label text on the progress bar.
    /// </summary>
    public void SetText(string text)
    {
        if (fillText != null)
            fillText.text = text;
    }
}