using System;

namespace Game.Core
{
    /// <summary>
    /// State machine game state sederhana.
    /// <para>
    /// Composition root membuat satu instance dan menyuntikkannya ke semua subscriber.
    /// </para>
    /// </summary>
    public sealed class GameStateService : IGameStateProvider
    {
        public GameState CurrentState { get; private set; } = GameState.Exploration;

        public event Action<GameState> OnStateChanged;

        public void EnterPuzzle() => SetState(GameState.Puzzle);

        public void EnterExploration() => SetState(GameState.Exploration);

        private void SetState(GameState state)
        {
            if (CurrentState == state) return;

            CurrentState = state;
            OnStateChanged?.Invoke(state);
        }
    }
}
