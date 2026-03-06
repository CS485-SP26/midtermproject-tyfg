using Character;
using Core;
using Farming;
using UnityEngine;

public partial class Farmer : MonoBehaviour, IFarmActor
{
    [Header("Shared Actor Stats")]
    [SerializeField] private FarmActorStatsDefinition statsDefinition;

    [Header("Tool Visuals")]
    [SerializeField] private FarmerToolVisuals toolVisuals = new FarmerToolVisuals();
    [SerializeField, HideInInspector] private GameObject wateringCan;
    [SerializeField, HideInInspector] private GameObject gardenHoe;
    [SerializeField, HideInInspector] private bool legacyToolVisualsMigrated;

    [Header("Resource State")]
    [SerializeField] private string energyBarObjectName = "EnergyBar";
    [SerializeField] private string waterBarObjectName = "WaterBar";

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
    [SerializeField] private FarmerActionFeedbackSettings actionFeedbackSettings = new FarmerActionFeedbackSettings();
    [SerializeField, HideInInspector] private Canvas notificationCanvas;
    [SerializeField, HideInInspector] private Vector2 feedbackAnchor = new Vector2(0.5f, 0.28f);
    [SerializeField, HideInInspector] private Vector2 feedbackSize = new Vector2(420f, 48f);
    [SerializeField, HideInInspector] private int feedbackFontSize = 20;
    [SerializeField, HideInInspector] private float feedbackDurationSeconds = 0.8f;
    [SerializeField, HideInInspector] private float feedbackRisePixels = 28f;
    [SerializeField, HideInInspector] private float feedbackCooldownSeconds = 0.6f;
    [SerializeField, HideInInspector] private bool legacyFeedbackMigrated;
    [SerializeField] private string lowEnergyMessage = "Not enough energy.";
    [SerializeField] private string lowWaterMessage = "Out of water. Refill at the shed.";
    [SerializeField] private string lowSeedsMessage = "Out of seeds. Buy more seeds.";
    [SerializeField] private int seedsPerPlant = 1;

    private AnimatedController animatedController;
    private MovementController movementController;
    private FarmerResourceState resourceState;
    private EnergyState energyState;
    private readonly FarmerRuntimeResources runtimeResources = new FarmerRuntimeResources();
    private bool sprintInputHeld;
    private FarmerActionFeedback actionFeedback;
    private IEconomyService economyService;
    private readonly FarmerTileInteractionService tileInteractionService = new FarmerTileInteractionService();
}
