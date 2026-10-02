using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Core;

namespace Game.Puzzle
{
    /// <summary>
    /// Mengelola siklus hidup puzzle: freeze pemain, membuka window, menunggu, lalu memulihkan state.
    /// </summary>
    /// <remarks>
    /// Class C# murni (tanpa MonoBehaviour) yang di-wire oleh Composition Root. Blok <c>finally</c>
    /// memastikan apa pun hasilnya: selesai, batal, exception — game selalu kembali ke eksplorasi
    /// dan window puzzle tertutup.
    /// </remarks>
    public sealed class PuzzleService : IPuzzleService, IDisposable
    {
        private readonly IGameStateProvider _gameState;
        private readonly IPuzzleUI _ui;

        private CancellationTokenSource _abortSource;

        /// <param name="gameState">State machine untuk freeze/memulihkan pemain.</param>
        /// <param name="ui">Window puzzle opsional; puzzle tetap bisa jalan tanpa UI (misalnya untuk test).</param>
        public PuzzleService(IGameStateProvider gameState, IPuzzleUI ui = null)
        {
            _gameState = gameState ?? throw new ArgumentNullException(nameof(gameState));
            _ui = ui;
        }

        public bool IsRunning { get; private set; }

        public async UniTask<PuzzleResult> RunAsync(IPuzzle puzzle, CancellationToken cancellationToken)
        {
            if (puzzle == null) throw new ArgumentNullException(nameof(puzzle));

            if (IsRunning) return PuzzleResult.Cancelled;
            if (cancellationToken.IsCancellationRequested) return PuzzleResult.Cancelled;

            IsRunning = true;
            _abortSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            CancellationToken token = _abortSource.Token;

            if (_ui != null) _ui.CancelRequested += Abort;

            try
            {
                _gameState.EnterPuzzle();

                if (_ui != null) await _ui.OpenAsync(puzzle.DisplayName, token);

                return await puzzle.RunAsync(token);
            }
            catch (OperationCanceledException)
            {
                return PuzzleResult.Cancelled;
            }
            finally
            {
                if (_ui != null)
                {
                    _ui.CancelRequested -= Abort;

                    // Window harus selalu tertutup meski pemanggil sudah membatalkan token.
                    await _ui.CloseAsync(CancellationToken.None);
                }

                _gameState.EnterExploration();

                _abortSource?.Dispose();
                _abortSource = null;
                IsRunning = false;
            }
        }

        public void Abort()
        {
            CancellationTokenSource source = _abortSource;
            if (source == null || source.IsCancellationRequested) return;

            try
            {
                source.Cancel();
            }
            catch (ObjectDisposedException)
            {
            }
        }

        public void Dispose()
        {
            _abortSource?.Dispose();
            _abortSource = null;
        }
    }
}
