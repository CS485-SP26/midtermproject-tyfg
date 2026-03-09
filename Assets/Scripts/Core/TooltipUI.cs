using TMPro;
using UnityEngine;
using UnityEngine.UI;
public class TooltipUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private Image iconImage;
    [SerializeField] private Image rarityBorder;

    public void Populate(TooltipData data)
    {
        titleText.text = data.title;
        descriptionText.text = data.description;

        if (data.icon != null)
        {
            iconImage.sprite = data.icon;
            iconImage.gameObject.SetActive(true);
        }
        else
        {
            iconImage.gameObject.SetActive(false);
        }

        rarityBorder.color = data.rarityColor;
    }

    public void Show() => gameObject.SetActive(true);
    public void Hide() => gameObject.SetActive(false);
}