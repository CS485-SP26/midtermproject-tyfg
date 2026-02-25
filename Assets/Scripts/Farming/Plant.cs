using UnityEngine;
public enum PlantState
{
    Planted,
    Growing,
    Mature,
    Withered
}
public class Plant : MonoBehaviour
{
    [Header("Growth Settings")]
    [SerializeField] private float waterNeededToGrow = 5f;
    [SerializeField] private float waterDecayPerSecond = 0.2f;
    [SerializeField] private float growTime = 10f;

    [Header("Visuals")]
    [SerializeField] private GameObject plantedModel;
    [SerializeField] private GameObject growingModel;
    [SerializeField] private GameObject matureModel;
    [SerializeField] private GameObject witheredModel;

    public PlantState CurrentState { get; private set; }

    private float currentWater;
    private float growTimer;

    private void Start()
    {
        SetState(PlantState.Planted);
    }

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

    public void ResetToDirt()
    {
        Destroy(gameObject);
    }

    private void SetState(PlantState newState)
    {
        CurrentState = newState;
        UpdateVisuals();
    }

    private void UpdateVisuals()
    {
        plantedModel.SetActive(CurrentState == PlantState.Planted);
        growingModel.SetActive(CurrentState == PlantState.Growing);
        matureModel.SetActive(CurrentState == PlantState.Mature);
        witheredModel.SetActive(CurrentState == PlantState.Withered);
    }
}