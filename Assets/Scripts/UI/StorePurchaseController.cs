using Core;
using Farming;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StorePurchaseController : SeedPurchaseControllerBase
{
    [Header("Store UI")]
    [SerializeField] private Button purchaseButton;
    [SerializeField] private TMP_Text purchaseButtonText;

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
        TryPurchaseAndNotify();
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
