using Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StorePurchaseController : MonoBehaviour
{
    [Header("Purchase Settings")]
    [SerializeField] private int seedCost = 10;
    [SerializeField] private int seedsPerPurchase = 1;

    [Header("Optional UI References")]
    [SerializeField] private Button purchaseButton;
    [SerializeField] private TMP_Text purchaseButtonText;
    [SerializeField] private TMP_Text feedbackText;

    [Header("Messages")]
    [SerializeField] private string insufficientFundsMessage = "Not enough funds.";

    private GameManager gameManager;

    private void OnValidate()
    {
        if (seedCost < 1)
            seedCost = 1;

        if (seedsPerPurchase < 1)
            seedsPerPurchase = 1;
    }

    private void Start()
    {
        gameManager = GameManager.Instance;
        gameManager.FundsChanged += HandleFundsChanged;

        if (purchaseButtonText != null)
            purchaseButtonText.SetText("Purchase Seeds ({0})", seedCost);

        UpdatePurchaseAvailability(gameManager.Funds);
    }

    private void OnDestroy()
    {
        if (gameManager == null)
            return;

        gameManager.FundsChanged -= HandleFundsChanged;
    }

    public void PurchaseSeeds()
    {
        if (gameManager == null)
            gameManager = GameManager.Instance;

        if (gameManager.TrySpendFunds(seedCost))
        {
            gameManager.AddSeeds(seedsPerPurchase);
            if (feedbackText != null)
                feedbackText.SetText("Purchased {0} seed(s).", seedsPerPurchase);
        }
        else
        {
            if (feedbackText != null)
                feedbackText.text = insufficientFundsMessage;
        }

        UpdatePurchaseAvailability(gameManager.Funds);
    }

    private void HandleFundsChanged(int funds)
    {
        UpdatePurchaseAvailability(funds);
    }

    private void UpdatePurchaseAvailability(int funds)
    {
        if (purchaseButton != null)
            purchaseButton.interactable = funds >= seedCost;
    }
}
