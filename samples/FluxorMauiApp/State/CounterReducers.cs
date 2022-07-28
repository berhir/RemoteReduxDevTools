using Fluxor;

namespace FluxorMauiApp.State;

public static class CounterReducers
{
    [ReducerMethod(typeof(CounterActions.IncrementCounterAction))]
    public static CounterState ReduceIncrementCounterAction(CounterState state) =>
        state with { Count = state.Count + 1 };
}
