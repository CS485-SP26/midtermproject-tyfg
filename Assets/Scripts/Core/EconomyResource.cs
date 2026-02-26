/*
* This enum defines the different types of resources in the game's economy system.
* It can be used to identify and manage different resources such as funds, seeds, and skill points.
* Exposes:
*   - Funds: Represents the in-game currency used for purchasing items and upgrades.
*   - Seeds: Represents the resource used for planting crops in the farming system.
*   - SkillPoints: Represents the resource used for unlocking and upgrading skills in the skill tree  // not implemented yet, 
*  // but could be used in the future for skill upgrades.
*/


namespace Core
{
    // Shared economy resource keys used by GameManager and economy consumers.
    public enum EconomyResource
    {
        // Currency used for purchases and rewards.
        Funds = 0,
        // Seed inventory used for planting/purchase flows.
        Seeds = 1,
        // Upgrade currency for skill systems.
        SkillPoints = 2
    }
}
