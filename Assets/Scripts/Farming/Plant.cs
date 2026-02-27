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
    [Header("Growth Settings")]
    // Water threshold required to transition from Planted -> Growing.
    [SerializeField] private float waterNeededToGrow = 5f;
    // Duration in Growing state before becoming Mature.
    [SerializeField] private float growTime = 1000f;
    // Whether the plant continues to produce fruit after first harvest
    [SerializeField] private bool canRegrowFruit = false;

    [Header("Visuals")]
    // Per-state models toggled by UpdateVisuals().
    [SerializeField] private GameObject plantedModel;
    [SerializeField] private GameObject growingModel;
    [SerializeField] private GameObject matureModel;
    [SerializeField] private GameObject witheredModel;

    [Header("For reference, don't change")]
    [SerializeField] private string plantState;
    [SerializeField] private float CurrentWater;
    [SerializeField] private float GrowTimeLeft;


    // Current lifecycle state.
    public PlantState CurrentState { get; private set; }


    // Runtime growth timer.
     [SerializeField] private float growTimer = 0f;

    // Reference to parent FarmTile
    private FarmTile Tile;

    // Initializes plant in newly planted state.
    private void Start()
    {
        SetState(PlantState.Planted);
        Debug.Log("Plant's parent tile: " + Tile.ToString());
    }

    // Handles water decay, withering, and growth progression.
    private void FixedUpdate()
    {
        // For debugging:
        plantState = CurrentState.ToString();
        if (Tile != null) CurrentWater = Tile.GetWater();
        

        if (Tile == null) return;
        if (CurrentState == PlantState.Withered || CurrentState == PlantState.Mature)
            return;

        if (Tile.GetWater() <= 0.1f)
        {
            SetState(PlantState.Withered);
            Debug.Log("A plant has withered. Water: " + Tile.GetWater());
            return;
        }

        if (CurrentState == PlantState.Planted || CurrentState == PlantState.Growing)
        {
            growTimer += Time.fixedDeltaTime;

            // HW6 Part 11 - Growing Plants
            
            if (growTimer >= growTime)
            {
                SetState(PlantState.Mature);
            }
            else if (growTimer >= growTime / 4) // Plant will "sprout" a quarter of the way through its growth cycle.
                                            // This timing is arbitrary for now.
            {
                SetState(PlantState.Growing);
            }

            GrowTimeLeft = growTimer - growTime;
        }
    }

    // Adds water and advances into Growing once threshold is reached.
    public void AddWater(float amount)
    {
        if (CurrentState == PlantState.Withered || CurrentState == PlantState.Mature)
            return;

        if (CurrentState == PlantState.Planted && Tile.GetWater() >= waterNeededToGrow)
        {
            SetState(PlantState.Growing);
        }

        //if (CurrentState == PlantState.Planted)
    }

    // Destroys the plant object (used when tile resets).
    public void ResetToDirt()
    {
        Destroy(gameObject);
    }

    // Sets current state and refreshes active visual model.
    private void SetState(PlantState newState)
    {
        Debug.Log("Plant changed state: " + newState.ToString());
        CurrentState = newState;
        UpdateVisuals();
    }

    // Enables only the model matching current state.
    private void UpdateVisuals()
    {
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
