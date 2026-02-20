using UnityEngine;
using TMPro;
using Core;
public class FundsUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI fundsText;

    private void OnEnable()
    {
        GameManager.Instance.FundsChanged += UpdateFunds;
    }

    private void OnDisable()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.FundsChanged -= UpdateFunds;
    }

    private void Start()
    {
        // Initialize with current value
        UpdateFunds(GameManager.Instance.Funds);
    }

    private void UpdateFunds(int amount)
    {
        fundsText.text = amount.ToString();
    }
}