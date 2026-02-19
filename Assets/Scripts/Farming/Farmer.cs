using UnityEngine;
using Farming;
using Character;

public class Farmer : MonoBehaviour
{
    [SerializeField] private GameObject wateringCan;
    [SerializeField] private GameObject gardenHoe;
    [SerializeField] private ProgressBar waterLevelUI;

    [SerializeField] private float waterLevel = 1f; // 0-1
    [SerializeField] private float waterPerUse = 0.1f;

    private AnimatedController animatedController;

    void Start()
    {
        animatedController = GetComponent<AnimatedController>();
        Debug.Assert(animatedController, "Farmer requires an AnimatedController");

        if (waterLevelUI != null)
            waterLevelUI.SetText("Water Level");

        SetWaterLevel(waterLevel);
        SetTool("None");
    }

    public void SetTool(string tool)
    {
        Debug.Log("SetTool called with: " + tool);
        wateringCan.SetActive(false);
        gardenHoe.SetActive(false);

        switch (tool)
        {
            case "GardenHoe":
                gardenHoe.SetActive(true);
                break;

            case "WaterCan":
                wateringCan.SetActive(true);
                break;
        }
    }

    public void TryTileInteraction(FarmTile tile)
    {
        if (tile == null)
            return;

        if (tile.TryGetComponent<SeedPurchaseTile>(out SeedPurchaseTile purchaseTile))
        {
            purchaseTile.TryPurchaseFromFarmer(this);
            return;
        }

        switch (tile.GetCondition)
        {
            case FarmTile.Condition.Grass:
                animatedController.SetTrigger("Till");
                tile.Interact(); // tilling always allowed
                break;

            case FarmTile.Condition.Tilled:
                if (waterLevel > 0f)
                {
                    animatedController.SetTrigger("Water");
                    SetWaterLevel(waterLevel - waterPerUse);
                    tile.Interact(); // only water when you have water
                }
                else
                {
                    Debug.Log("No water left!");
                }
                break;

            case FarmTile.Condition.Watered:
                Debug.Log("Tile is ready for planting");
                break;
        }
    }

    public void RefillWaterToFull()
    {
        SetWaterLevel(1f);
    }

    private void SetWaterLevel(float value)
    {
        waterLevel = Mathf.Clamp01(value);
        if (waterLevel < 0.01f)
            waterLevel = 0f;

        if (waterLevelUI != null)
            waterLevelUI.Fill = waterLevel;
    }
}
