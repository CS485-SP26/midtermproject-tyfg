using UnityEngine;
using Farming;
using Character;
using TMPro;
using UnityEngine.SceneManagement;

public class Farmer : MonoBehaviour
{
    [Header("Tool Visuals")]
    [SerializeField] private GameObject wateringCan;
    [SerializeField] private GameObject gardenHoe;

    [Header("Resource UI")]
    [SerializeField] private ProgressBar energyLevelUI;
    [SerializeField] private ProgressBar waterLevelUI;
    [SerializeField] private string energyBarObjectName = "EnergyBar";
    [SerializeField] private string waterBarObjectName = "WaterBar";
    [SerializeField] private Color staminaBarColor = new Color(1f, 0.86f, 0.2f, 1f);

    [Header("Energy")]
    [SerializeField] private float maxEnergy = 100f;
    [SerializeField] private float startingEnergy = 100f;
    [SerializeField] private float energyRegenPerSecond = 8f;
    [SerializeField] private float tillEnergyCost = 15f;
    [SerializeField] private float jumpEnergyCost = 12f;
    [SerializeField] private float sprintEnergyDrainPerSecond = 15f;

    [Header("Water")]
    [SerializeField] private float maxWater = 100f;
    [SerializeField] private float startingWater = 100f;
    [SerializeField] private float waterPerUse = 10f;
    [SerializeField] private bool migrateLegacyWaterValues = true;

    [Header("Action Feedback")]
    [SerializeField] private Canvas notificationCanvas;
    [SerializeField] private Vector2 feedbackAnchor = new Vector2(0.5f, 0.28f);
    [SerializeField] private Vector2 feedbackSize = new Vector2(420f, 48f);
    [SerializeField] private int feedbackFontSize = 20;
    [SerializeField] private float feedbackDurationSeconds = 0.8f;
    [SerializeField] private float feedbackRisePixels = 28f;
    [SerializeField] private float feedbackCooldownSeconds = 0.6f;
    [SerializeField] private string lowEnergyMessage = "Not enough energy.";
    [SerializeField] private string lowWaterMessage = "Out of water. Refill at the shed.";

    private AnimatedController animatedController;
    private MovementController movementController;
    private FarmerResourceState resourceState;
    private float currentEnergy;
    private float currentWater;
    private bool sprintInputHeld;
    private float nextFeedbackTime;

    private void OnValidate()
    {
        maxEnergy = Mathf.Max(1f, maxEnergy);
        maxWater = Mathf.Max(1f, maxWater);
        startingEnergy = Mathf.Clamp(startingEnergy, 0f, maxEnergy);
        startingWater = Mathf.Clamp(startingWater, 0f, maxWater);

        energyRegenPerSecond = Mathf.Max(0f, energyRegenPerSecond);
        tillEnergyCost = Mathf.Max(0f, tillEnergyCost);
        jumpEnergyCost = Mathf.Max(0f, jumpEnergyCost);
        sprintEnergyDrainPerSecond = Mathf.Max(0f, sprintEnergyDrainPerSecond);
        waterPerUse = Mathf.Max(0f, waterPerUse);

        feedbackFontSize = Mathf.Max(10, feedbackFontSize);
        feedbackDurationSeconds = Mathf.Max(0.1f, feedbackDurationSeconds);
        feedbackRisePixels = Mathf.Max(0f, feedbackRisePixels);
        feedbackCooldownSeconds = Mathf.Max(0.05f, feedbackCooldownSeconds);
    }

    private void Start()
    {
        animatedController = GetComponent<AnimatedController>();
        movementController = GetComponent<MovementController>();
        Debug.Assert(animatedController, "Farmer requires an AnimatedController");
        Debug.Assert(movementController, "Farmer requires a MovementController");

        ApplyLegacyWaterMigration();
        resourceState = FarmerResourceState.Instance;
        if (resourceState != null)
        {
            resourceState.Configure(maxEnergy, maxWater, energyRegenPerSecond, energyBarObjectName, waterBarObjectName);
            resourceState.InitializeIfNeeded(startingEnergy, startingWater);
            resourceState.SetFarmerPresent(true);
        }

        AutoBindProgressBars();
        EnsureBothProgressBars();

        if (energyLevelUI != null)
        {
            energyLevelUI.SetText("Energy");
            energyLevelUI.SetFillColor(staminaBarColor);
        }
        if (waterLevelUI != null)
            waterLevelUI.SetText("Water Level");

        float initialEnergy = resourceState != null && resourceState.IsInitialized ? resourceState.CurrentEnergy : startingEnergy;
        float initialWater = resourceState != null && resourceState.IsInitialized ? resourceState.CurrentWater : startingWater;
        SetEnergyLevel(initialEnergy);
        SetWaterLevel(initialWater);
        SetTool("None");
    }

    private void OnEnable()
    {
        if (resourceState == null)
            resourceState = FarmerResourceState.Instance;

        if (resourceState != null)
            resourceState.SetFarmerPresent(true);
    }

    private void OnDisable()
    {
        if (resourceState != null)
            resourceState.SetFarmerPresent(false);
    }

    private void OnDestroy()
    {
        if (resourceState != null)
            resourceState.SetFarmerPresent(false);
    }

    private void Update()
    {
        DrainSprintEnergyIfNeeded();
        RegenerateEnergyIfIdle();
    }

    public void SetTool(string tool)
    {
        Debug.Log("SetTool called with: " + tool);

        if (wateringCan != null)
            wateringCan.SetActive(false);

        if (gardenHoe != null)
            gardenHoe.SetActive(false);

        switch (tool)
        {
            case "GardenHoe":
                if (gardenHoe != null)
                    gardenHoe.SetActive(true);
                break;

            case "WaterCan":
                if (wateringCan != null)
                    wateringCan.SetActive(true);
                break;
        }
    }

    public void SetSprintInput(bool sprintPressed)
    {
        sprintInputHeld = sprintPressed;

        if (movementController == null)
            return;

        if (!sprintPressed)
        {
            movementController.SetSprint(false);
            return;
        }

        if (currentEnergy <= 0f)
        {
            sprintInputHeld = false;
            movementController.SetSprint(false);
            ShowActionBlockedFeedback(lowEnergyMessage);
        }
    }

    public bool TryConsumeJumpEnergy()
    {
        if (TryConsumeEnergy(jumpEnergyCost))
            return true;

        ShowActionBlockedFeedback(lowEnergyMessage);
        return false;
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
                if (!TryConsumeEnergy(tillEnergyCost))
                {
                    ShowActionBlockedFeedback(lowEnergyMessage);
                    return;
                }

                animatedController.SetTrigger("Till");
                tile.Interact(); // tilling always allowed
                break;

            case FarmTile.Condition.Tilled:
                if (!TryConsumeWater(waterPerUse))
                {
                    ShowActionBlockedFeedback(lowWaterMessage);
                    return;
                }

                animatedController.SetTrigger("Water");
                tile.Interact();
                break;

            case FarmTile.Condition.Watered:
                Debug.Log("Tile is ready for planting");
                break;
        }
    }

    public void RefillWaterToFull()
    {
        SetWaterLevel(maxWater);
    }

    private void DrainSprintEnergyIfNeeded()
    {
        bool hasMovementInput = movementController != null && movementController.HasMovementInput;
        bool shouldSprint = sprintInputHeld && hasMovementInput;

        if (shouldSprint && !TryConsumeEnergy(sprintEnergyDrainPerSecond * Time.deltaTime))
        {
            shouldSprint = false;
            sprintInputHeld = false;
            ShowActionBlockedFeedback(lowEnergyMessage);
        }

        if (movementController != null)
            movementController.SetSprint(shouldSprint);
    }

    private void RegenerateEnergyIfIdle()
    {
        bool isActivelySprinting = sprintInputHeld && movementController != null && movementController.HasMovementInput;
        if (isActivelySprinting || energyRegenPerSecond <= 0f || currentEnergy >= maxEnergy)
            return;

        SetEnergyLevel(currentEnergy + (energyRegenPerSecond * Time.deltaTime));
    }

    private bool TryConsumeEnergy(float amount)
    {
        if (amount <= 0f)
            return true;

        if (currentEnergy + 0.001f < amount)
            return false;

        SetEnergyLevel(currentEnergy - amount);
        return true;
    }

    private bool TryConsumeWater(float amount)
    {
        if (amount <= 0f)
            return true;

        if (currentWater + 0.001f < amount)
            return false;

        SetWaterLevel(currentWater - amount);
        return true;
    }

    private void SetEnergyLevel(float value)
    {
        currentEnergy = Mathf.Clamp(value, 0f, maxEnergy);
        float normalized = maxEnergy <= 0f ? 0f : currentEnergy / maxEnergy;

        if (energyLevelUI != null)
            energyLevelUI.Fill = normalized;

        if (resourceState != null)
            resourceState.SetEnergy(currentEnergy);
    }

    private void SetWaterLevel(float value)
    {
        currentWater = Mathf.Clamp(value, 0f, maxWater);
        float normalized = maxWater <= 0f ? 0f : currentWater / maxWater;

        if (waterLevelUI != null)
            waterLevelUI.Fill = normalized;

        if (resourceState != null)
            resourceState.SetWater(currentWater);
    }

    private void ShowActionBlockedFeedback(string message)
    {
        if (string.IsNullOrWhiteSpace(message) || Time.time < nextFeedbackTime)
            return;

        nextFeedbackTime = Time.time + feedbackCooldownSeconds;
        Canvas canvas = ResolveCanvas();
        if (canvas == null)
        {
            Debug.Log(message);
            return;
        }

        GameObject go = new GameObject("FarmerFeedback", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(CanvasGroup), typeof(FloatingTextPopup));
        go.transform.SetParent(canvas.transform, false);

        TextMeshProUGUI label = go.GetComponent<TextMeshProUGUI>();
        label.richText = false;
        label.text = message;
        label.alignment = TextAlignmentOptions.Center;
        label.fontSize = feedbackFontSize;
        label.color = Color.white;

        RectTransform rt = label.rectTransform;
        rt.anchorMin = feedbackAnchor;
        rt.anchorMax = feedbackAnchor;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = feedbackSize;

        FloatingTextPopup popup = go.GetComponent<FloatingTextPopup>();
        popup.Configure(feedbackDurationSeconds, feedbackRisePixels);
    }

    private Canvas ResolveCanvas()
    {
        if (notificationCanvas != null && notificationCanvas.isActiveAndEnabled)
            return notificationCanvas;

        Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        foreach (Canvas canvas in canvases)
        {
            if (canvas != null && canvas.isActiveAndEnabled)
            {
                notificationCanvas = canvas;
                return notificationCanvas;
            }
        }

        return null;
    }

    private void AutoBindProgressBars()
    {
        ProgressBar[] bars = FindProgressBarsInCurrentScene();
        if (bars.Length == 0)
            return;

        if (energyLevelUI == null)
            energyLevelUI = FindProgressBarByName(energyBarObjectName, bars);

        if (waterLevelUI == null)
            waterLevelUI = FindProgressBarByName(waterBarObjectName, bars);

        if (waterLevelUI == null)
            waterLevelUI = FindProgressBarByPartialName("water", bars);

        if (energyLevelUI == null)
            energyLevelUI = FindProgressBarByPartialName("energy", bars);

        if (waterLevelUI == null)
            waterLevelUI = bars[0];

        if (energyLevelUI == null && bars.Length > 1)
        {
            foreach (ProgressBar bar in bars)
            {
                if (bar != null && bar != waterLevelUI)
                {
                    energyLevelUI = bar;
                    break;
                }
            }
        }
    }

    private ProgressBar[] FindProgressBarsInCurrentScene()
    {
        Scene scene = gameObject.scene;
        if (!scene.IsValid())
            return FindObjectsByType<ProgressBar>(FindObjectsSortMode.None);

        GameObject[] roots = scene.GetRootGameObjects();
        int count = 0;
        foreach (GameObject root in roots)
        {
            if (root == null)
                continue;

            count += root.GetComponentsInChildren<ProgressBar>(true).Length;
        }

        if (count == 0)
            return FindObjectsByType<ProgressBar>(FindObjectsSortMode.None);

        ProgressBar[] result = new ProgressBar[count];
        int index = 0;
        foreach (GameObject root in roots)
        {
            if (root == null)
                continue;

            ProgressBar[] bars = root.GetComponentsInChildren<ProgressBar>(true);
            foreach (ProgressBar bar in bars)
            {
                result[index++] = bar;
            }
        }

        return result;
    }

    private static ProgressBar FindProgressBarByName(string objectName, ProgressBar[] bars)
    {
        if (string.IsNullOrWhiteSpace(objectName) || bars == null || bars.Length == 0)
            return null;

        foreach (ProgressBar bar in bars)
        {
            if (bar != null && bar.name == objectName)
                return bar;
        }

        return null;
    }

    private static ProgressBar FindProgressBarByPartialName(string token, ProgressBar[] bars)
    {
        if (string.IsNullOrWhiteSpace(token) || bars == null || bars.Length == 0)
            return null;

        string loweredToken = token.ToLowerInvariant();
        foreach (ProgressBar bar in bars)
        {
            if (bar == null || string.IsNullOrWhiteSpace(bar.name))
                continue;

            if (bar.name.ToLowerInvariant().Contains(loweredToken))
                return bar;
        }

        return null;
    }

    private void EnsureBothProgressBars()
    {
        if (energyLevelUI != null && waterLevelUI != null)
            return;

        if (energyLevelUI == null && waterLevelUI != null)
        {
            energyLevelUI = CloneCompanionBar(waterLevelUI, energyBarObjectName, new Vector2(0f, 36f));
            return;
        }

        if (waterLevelUI == null && energyLevelUI != null)
            waterLevelUI = CloneCompanionBar(energyLevelUI, waterBarObjectName, new Vector2(0f, -36f));
    }

    private static ProgressBar CloneCompanionBar(ProgressBar template, string objectName, Vector2 positionOffset)
    {
        if (template == null)
            return null;

        Transform parent = template.transform.parent;
        GameObject clone = UnityEngine.Object.Instantiate(template.gameObject, parent);
        clone.name = string.IsNullOrWhiteSpace(objectName) ? $"{template.name}_Clone" : objectName;

        RectTransform templateRect = template.GetComponent<RectTransform>();
        RectTransform cloneRect = clone.GetComponent<RectTransform>();
        if (templateRect != null && cloneRect != null)
            cloneRect.anchoredPosition = templateRect.anchoredPosition + positionOffset;

        return clone.GetComponent<ProgressBar>();
    }

    private void ApplyLegacyWaterMigration()
    {
        if (!migrateLegacyWaterValues || maxWater <= 1f)
            return;

        // Previous versions serialized water as normalized [0..1].
        if (startingWater > 0f && startingWater <= 1f)
            startingWater *= maxWater;

        if (waterPerUse > 0f && waterPerUse <= 1f)
            waterPerUse *= maxWater;

        startingWater = Mathf.Clamp(startingWater, 0f, maxWater);
    }
}
