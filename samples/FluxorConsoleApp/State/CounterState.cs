using Fluxor;

namespace FluxorConsoleApp.State;

[FeatureState]
public record CounterState
{
    public int Count { get; init; } = 0;
}
