using Character;
using Core;
using Farming;
using UnityEngine;
using Farming.Actors;
using Farming.Crops;
using Farming.SceneState;
using Environment.Tiles;
using Farming.Tiles;

/*
* The Farmer class represents the player-controlled character in the farming game. It manages the farmer's stats, resources, 
* and interactions with the farm environment.
*/
public partial class Farmer : MonoBehaviour, IFarmActor
{
    // Shared Actor Stats is used to store stats that are common across all farm actors, such as energy and water. 
    // This allows us to easily create new farm actors with different stats by simply creating new definitions.
    [Header("Shared Actor Stats")]
    [SerializeField] private FarmActorStatsDefinition statsDefinition;

    // Tool Visuals is used to store references to the tool game objects and related settings.
    [Header("Tool Visuals")]
    [SerializeField] private FarmerToolVisuals toolVisuals = new FarmerToolVisuals();
    [SerializeField, HideInInspector] private GameObject wateringCan;
    [SerializeField, HideInInspector] private GameObject gardenHoe;
    [SerializeField, HideInInspector] private bool legacyToolVisualsMigrated;

    // Resource State is used to store references to the energy and water bars, as well as the current values and costs 
    // of using energy and water for actions.
    [Header("Resource State")]
    [SerializeField] private string energyBarObjectName = "EnergyBar";
    [SerializeField] private string waterBarObjectName = "WaterBar";

    // The following fields are for energy and water stats, costs, and regeneration. They are used to manage the farmer's 
    // resources and determine if actions can be performed.
    [Header("Energy")]
    [SerializeField] private float maxEnergy = 100f;
    [SerializeField] private float startingEnergy = 100f;
    [SerializeField] private float energyRegenPerSecond = 8f;
    [SerializeField] private float tillEnergyCost = 15f;
    [SerializeField] private float jumpEnergyCost = 12f;
    [SerializeField] private float sprintEnergyDrainPerSecond = 15f;

    // The water stats and costs are used to manage the farmer's water resource, which is consumed when watering plants.
    [Header("Water")]
    [SerializeField] private float maxWater = 100f;
    [SerializeField] private float startingWater = 100f;
    [SerializeField] private float waterPerUse = 10f;
    [SerializeField] private bool migrateLegacyWaterValues = true;

    // Seed stats are used to manage the farmer's seed inventory, which is consumed when planting new crops.
    [Header("Seed Stats")]
    [SerializeField] private int maxSeeds = 10;
    [SerializeField] private int startingSeeds = 10;
    [SerializeField] private int seedsPerPlant = 1;

    // Action Feedback settings are used to configure the display and behavior of feedback messages.
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
