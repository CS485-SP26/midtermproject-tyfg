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
    // Continuous water loss over time.
    [SerializeField] private float waterDecayPerSecond = 0.2f;
    // Duration in Growing state before becoming Mature.
    [SerializeField] private float growTime = 10f;

    [Header("Visuals")]
    // Per-state models toggled by UpdateVisuals().
    [SerializeField] private GameObject plantedModel;
    [SerializeField] private GameObject growingModel;
    [SerializeField] private GameObject matureModel;
    [SerializeField] private GameObject witheredModel;

    // Current lifecycle state.
    public PlantState CurrentState { get; private set; }

    // Runtime water and growth timers.
    private float currentWater;
    private float growTimer;

    // Initializes plant in newly planted state.
    private void Start()
    {
        SetState(PlantState.Planted);
    }

    // Handles water decay, withering, and growth progression.
    private void Update()
    {
        if (CurrentState == PlantState.Withered)
            return;

        currentWater -= waterDecayPerSecond * Time.deltaTime;

        if (currentWater <= 0f)
        {
            SetState(PlantState.Withered);
            return;
        }

        if (CurrentState == PlantState.Growing)
        {
            growTimer += Time.deltaTime;

            if (growTimer >= growTime)
                SetState(PlantState.Mature);
        }
    }

    // Adds water and advances into Growing once threshold is reached.
    public void AddWater(float amount)
    {
        if (CurrentState == PlantState.Withered || CurrentState == PlantState.Mature)
            return;

        currentWater += amount;

        if (CurrentState == PlantState.Planted && currentWater >= waterNeededToGrow)
        {
            SetState(PlantState.Growing);
        }
    }

    // Destroys the plant object (used when tile resets).
    public void ResetToDirt()
    {
        Destroy(gameObject);
    }

    // Sets current state and refreshes active visual model.
    private void SetState(PlantState newState)
    {
        CurrentState = newState;
        UpdateVisuals();
    }

    // Enables only the model matching current state.
    private void UpdateVisuals()
    {
        plantedModel.SetActive(CurrentState == PlantState.Planted);
        growingModel.SetActive(CurrentState == PlantState.Growing);
        matureModel.SetActive(CurrentState == PlantState.Mature);
        witheredModel.SetActive(CurrentState == PlantState.Withered);
    }
}
