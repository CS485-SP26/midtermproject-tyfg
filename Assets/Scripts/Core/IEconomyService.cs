using System;

namespace Core
{
    public interface IEconomyService
    {
        event Action<EconomyResource, int> ResourceChanged;

        int GetResourceAmount(EconomyResource resource);
        void AddResource(EconomyResource resource, int amount);
        bool TrySpendResource(EconomyResource resource, int amount);
    }
}
