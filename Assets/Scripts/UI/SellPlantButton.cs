using Core;
using UnityEngine;

public class SellPlantButton : MonoBehaviour
{
    [SerializeField] private Farmer farmer;
    [SerializeField] private int sellPrice = 10;
    public void SellPlant()
    {
        Debug.Log("SELL BUTTON CLICKED");
        GameManager gm = GameManager.Instance;
        bool sold = gm.TrySpendResource(EconomyResource.Plants,1);
        if (!sold)
        {
            Debug.Log("No plants to sell");
            return;
        }
        gm.AddResource(EconomyResource.Funds, sellPrice);
        Debug.Log("Sold 1 plant for " + sellPrice);
    }
}
