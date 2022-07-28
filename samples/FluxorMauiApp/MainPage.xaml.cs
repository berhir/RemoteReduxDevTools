using Fluxor;
using FluxorMauiApp.State;

namespace FluxorMauiApp
{
    public partial class MainPage : ContentPage
    {
        private readonly IState<CounterState> _counterState;
        private readonly Fluxor.IDispatcher _dispatcher;

        public MainPage(IStore store, IState<CounterState> counterState, Fluxor.IDispatcher dispatcher)
        {
            InitializeComponent();
            _counterState = counterState;
            _dispatcher = dispatcher;

            _counterState.StateChanged += CounterState_StateChanged;

            Task.Run(() =>
            {
                Dispatcher.Dispatch(async () => await store.InitializeAsync());
            });
        }

        private void CounterState_StateChanged(object sender, EventArgs e)
        {
            var count = _counterState.Value.Count;
            if (count == 1)
                CounterBtn.Text = $"Clicked {count} time";
            else
                CounterBtn.Text = $"Clicked {count} times";

            SemanticScreenReader.Announce(CounterBtn.Text);
        }

        private void OnCounterClicked(object sender, EventArgs e)
        {
            _dispatcher.Dispatch(new CounterActions.IncrementCounterAction());
        }
    }
}