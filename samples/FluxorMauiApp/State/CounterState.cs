using Fluxor;

namespace FluxorMauiApp.State;

[FeatureState]
public record CounterState
{
    public int Count { get; init; } = 0;
}
