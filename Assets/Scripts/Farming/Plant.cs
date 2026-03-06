using System;
using Farming;
using Unity.VisualScripting;
using UnityEngine;

/*
* This class represents a plant in the farming system. It manages the plant's growth stages, water requirements, and visual representation based on its current state.
* The plant can be watered to grow, and it will wither if it runs out of water. It also provides a method to reset the plant back to dirt, which can be called when the tile is tilled again.
* Exposes:
*   - CurrentState: A property to get the current state of the plant (Planted, Growing, Mature, Withered).
*   - AddWater(float amount): A method to add water to the plant, which can trigger growth if the plant is in the Planted state and 
*       receives enough water.
*   - ResetToDirt(): A method to reset the plant back to dirt, which destroys the plant GameObject. This should be called when the tile is 
*       tilled again.
* Requires:
*   - A set of GameObjects for the different growth stages (planted, growing, mature, withered) that can be enabled or disabled 
*       based on the plant's state.
*/

public enum PlantState
{
    // Just placed on tile and waiting for enough water.
    Planted,
    // Actively growing toward maturity.
    Growing,
    // Fully grown.
    Mature,
    // Dried out/dead due to no water.
    Withered
}

public class Plant : MonoBehaviour
{
    public const float SproutTimerSeconds = 0f;

    [Header("Growth Settings")]
    // Water threshold required to transition from Planted -> Growing.
    [SerializeField] private float waterNeededToGrow = 5f;
    // Duration in Growing state before becoming Mature.
    [SerializeField] private float growTime = 1f;
    // Water level considered "dry" for withering.
    [SerializeField] private float witherWaterThreshold = 0.1f;
    // How long the plant can stay dry before withering.
    [SerializeField] private float dryOutGraceSeconds = 60f;
    // Whether the plant continues to produce fruit after first harvest
    [SerializeField] private bool regrowsFruit = false;
    public float WaterNeededToGrow => waterNeededToGrow;
    public float GrowTimeSeconds => growTime;
    public float WitherWaterThreshold => witherWaterThreshold;
    public float DryOutGraceSeconds => dryOutGraceSeconds;
    public bool RegrowsFruit
    {
        get { return regrowsFruit; }
    }

    [Header("Visuals")]
    // Per-state models toggled by UpdateVisuals().
    [SerializeField] private GameObject plantedModel;
    [SerializeField] private GameObject growingModel;
    [SerializeField] private GameObject matureModel;
    [SerializeField] private GameObject witheredModel;

    [Header("For reference, don't change in inspector")]
    [SerializeField] private string plantState;
    [SerializeField] private float CurrentWater;
    [SerializeField] private float GrowTimeLeft;


    // Current lifecycle state.
    public PlantState CurrentState { get; private set; }


    // Runtime growth timer.
    [SerializeField] private float growTimer = 0f;
    // Accumulated time spent at/under dry threshold.
    [SerializeField] private float dryTimerSeconds = 0f;

    // Reference to parent FarmTile
    private FarmTile Tile;
    // True when state/timer were restored before Start().
    private bool restoredFromSnapshot;
    [SerializeField] private string plantName = "Turnip";
    [SerializeField] private int sellValue = 10;

    public PlantData GetHarvestData()
    {
        return new PlantData(plantName, sellValue, 1);
    }   
    // Initializes plant in newly planted state.
    private void Start()
    {
        SetModelsInactive();
        if (!restoredFromSnapshot)
        {
            SetState(PlantState.Planted);
            growTimer = 0f;
            dryTimerSeconds = 0f;
        }
        else
        {
            UpdateVisuals();
        }

        if (Tile != null)
            Debug.Log("Plant's parent tile: " + Tile.name);
    }

    private void SetModelsInactive()
    {
        plantedModel.SetActive(false);
        growingModel.SetActive(false);
        matureModel.SetActive(false);
        witheredModel.SetActive(false);
    }

    // Handles water decay, withering, and growth progression.
    private void FixedUpdate()
    {
        // For debugging:
        plantState = CurrentState.ToString();
        
        if (Tile == null) return;
        else CurrentWater = Tile.GetWater();

        if (CurrentState == PlantState.Withered || CurrentState == PlantState.Mature)
            return;

        float water = Tile.GetWater();
        if (water <= witherWaterThreshold)
        {
            dryTimerSeconds += Time.fixedDeltaTime;
            if (dryTimerSeconds >= dryOutGraceSeconds)
            {
                SetState(PlantState.Withered);
                Debug.Log("A plant has withered. Water: " + water);
                return;
            }
        }
        else
        {
            dryTimerSeconds = 0f;
        }

        // HW6 Part 11 - Growing Plants:

        // Conditions necessary for plant to sprout.
        if (water >= waterNeededToGrow)
        {
            // Plant will "sprout" when growTimer reaches 5. (arbitrary)
            if (growTimer >= SproutTimerSeconds && CurrentState == PlantState.Planted)
            {
                SetState(PlantState.Growing);
            }

            if (CurrentState == PlantState.Growing && growTimer >= growTime)
            {

                SetState(PlantState.Mature);
                Tile.TileCondition = FarmTile.Condition.Harvestable;
                growTimer = 0f;
                GrowTimeLeft = 0f;
            }

            float growthMultiplier = Tile != null ? Tile.GetGrowthMultiplier() : 1f;
            growTimer += Time.fixedDeltaTime * growthMultiplier;
            GrowTimeLeft = growTime - growTimer;
        }
        
    }

    // Destroys the plant object (used when tile resets).
    public void ResetToDirt()
    {
        Destroy(gameObject);
    }

    // Returns current grow timer for persistence snapshots.
    public float GetGrowTimer()
    {
        return growTimer;
    }

    // Returns accumulated dry-time for persistence snapshots.
    public float GetDryTimer()
    {
        return dryTimerSeconds;
    }

    // Restores state/timer from persistence snapshot before gameplay resumes.
    public void RestoreFromSnapshot(PlantState state, float restoredGrowTimer, float restoredDryTimer = 0f)
    {
        restoredFromSnapshot = true;
        growTimer = Mathf.Max(0f, restoredGrowTimer);
        dryTimerSeconds = Mathf.Max(0f, restoredDryTimer);
        GrowTimeLeft = Mathf.Max(0f, growTime - growTimer);
        SetModelsInactive();
        SetState(state);
    }

    // Sets current state and refreshes active visual model.
    private void SetState(PlantState newState)
    {
        Debug.Log("Plant changed state: " + newState.ToString());
        CurrentState = newState;
        UpdateVisuals();

        Debug.Log($"Plant visibility: {plantedModel.activeSelf}, {growingModel.activeSelf}, {matureModel.activeSelf}, {witheredModel.activeSelf}");
    }

    // Enables only the model matching current state.
    private void UpdateVisuals()
    {
        Debug.Log("CURRENT STATE: " + CurrentState);
        plantedModel.SetActive(CurrentState == PlantState.Planted); // Shows this model when seed is first planted until growTime is reached
        growingModel.SetActive(CurrentState == PlantState.Growing);
        matureModel.SetActive(CurrentState == PlantState.Mature); // Changes model to fully grown plant at end of growTime
        witheredModel.SetActive(CurrentState == PlantState.Withered);
    }

    // Tell the plant what tile it's on
    internal void SetParentTile(FarmTile farmTile)
    {
        Tile = farmTile;
    }
}
