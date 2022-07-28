using Fluxor;
using FluxorConsoleApp.State;

namespace FluxorConsoleApp;

public class App
{
    private readonly IStore _store;
    private readonly IState<CounterState> _counterState;
    private readonly IDispatcher _dispatcher;

    public App(IStore store, IState<CounterState> counterState, IDispatcher dispatcher)
    {
        _store = store;
        _counterState = counterState;
        _dispatcher = dispatcher;
        counterState.StateChanged += CounterState_StateChanged;
    }

    public async Task RunAsync()
    {
        Console.Clear();
        Console.WriteLine("Initializing store");
        await _store.InitializeAsync();
        do
        {
            Console.WriteLine("1: Increment counter");
            Console.WriteLine("x: Exit");
            Console.Write("> ");
            var input = Console.ReadLine();

            switch (input?.ToLowerInvariant())
            {
                case "1":
                    var action = new CounterActions.IncrementCounterAction();
                    _dispatcher.Dispatch(action);
                    break;

                case "x":
                default:
                    Console.WriteLine("Program terminated");
                    return;
            }

        } while (true);
    }

    private void CounterState_StateChanged(object? sender, EventArgs e)
    {
        Console.WriteLine("");
        Console.WriteLine("==========================> CounterState");
        Console.WriteLine("ClickCount is " + _counterState.Value.Count);
        Console.WriteLine("<========================== CounterState");
        Console.WriteLine("");
    }
}
