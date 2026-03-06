using System;

namespace Core
{
    // Minimal runtime resource contract for presenter/view bindings.
    public interface IResourceState
    {
        float Current { get; }
        float Max { get; }
        event Action<float, float> ValueChanged;
    }
}
