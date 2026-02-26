using System;

/*
* An interface for an economy service that manages resources in the game.
* It defines methods for getting resource amounts, adding resources, and spending resources.
* It also includes an event that is triggered when a resource amount changes, allowing other parts of the game to react to changes in the economy.
* Exposes:
*   - GetResourceAmount(EconomyResource resource): Returns the current amount of the specified resource.
*   - AddResource(EconomyResource resource, int amount): Adds the specified amount to the specified resource.
*   - TrySpendResource(EconomyResource resource, int amount): Attempts to spend the specified amount of the specified resource, returning true if successful and false if there are insufficient resources.
*   - ResourceChanged: An event that is triggered when a resource amount changes, providing the resource type and the new amount.
*/

namespace Core
{
    // Minimal economy API used by gameplay and UI systems.
    public interface IEconomyService
    {
        // Fired after any resource value changes.
        event Action<EconomyResource, int> ResourceChanged;

        // Reads current amount for a resource.
        int GetResourceAmount(EconomyResource resource);
        // Adds a positive amount to a resource.
        void AddResource(EconomyResource resource, int amount);
        // Attempts to spend an amount; returns false when insufficient.
        bool TrySpendResource(EconomyResource resource, int amount);
    }
}
