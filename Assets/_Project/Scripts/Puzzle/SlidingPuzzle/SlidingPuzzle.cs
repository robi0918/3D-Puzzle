using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Game.Puzzle
{
    /// <summary>
    /// Sliding Puzzle: mengendalikan <see cref="SlidingPuzzleModel"/> dan <see cref="ISlidingPuzzleView"/>.
    /// </summary>
    /// <remarks>
    /// Bentuk alurnya sama dengan Memory Match: tunggu klik, validasi ke model, animasikan, ulangi.
    /// Klik ilegal (tile tidak bersebelahan dengan lubang) diabaikan, dan view hanya menerima klik
    /// saat metode ini menunggu, sehingga input tertahan selama tile bergeser.
    /// </remarks>
    public sealed class SlidingPuzzle : IPuzzle
    {
        private readonly SlidingPuzzleConfig _config;
        private readonly ISlidingPuzzleView _view;
        private readonly System.Random _random;

        /// <param name="config">Pengaturan papan (ukuran grid, sprite, panjang pengacakan).</param>
        /// <param name="view">Implementasi UI.</param>
        /// <param name="random">RNG yang disuntikkan untuk test deterministik.</param>
        public SlidingPuzzle(SlidingPuzzleConfig config, ISlidingPuzzleView view, System.Random random = null)
        {
            _config = config != null ? config : throw new ArgumentNullException(nameof(config));
            _view = view != null ? view : throw new ArgumentNullException(nameof(view));
            _random = random ?? new System.Random();
        }

        public string DisplayName => "Sliding Puzzle";

        public async UniTask<PuzzleResult> RunAsync(CancellationToken cancellationToken)
        {
            SlidingPuzzleModel model = new SlidingPuzzleModel(_config.Size, _random);
            model.Restart(_config.ShuffleSteps);

            try
            {
                await _view.ShowAsync(cancellationToken);

                _view.Build(model.Size, model.CreateSnapshot(), _config.TileSprites);
                _view.SetMoveCount(model.Moves);

                while (!model.IsSolved)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    int clickedSlot = await _view.WaitForTileClickAsync(cancellationToken);

                    if (!model.CanMove(clickedSlot)) continue;

                    int fromSlot = clickedSlot;
                    int toSlot = model.EmptyIndex;

                    model.TryMove(fromSlot, out _);

                    _view.SetMoveCount(model.Moves);

                    await _view.AnimateMoveAsync(fromSlot, toSlot, cancellationToken);
                }

                if (_config.SolvedPause > 0f)
                {
                    await UniTask.Delay(TimeSpan.FromSeconds(_config.SolvedPause), cancellationToken: cancellationToken);
                }

                return PuzzleResult.Completed;
            }
            catch (OperationCanceledException)
            {
                return PuzzleResult.Cancelled;
            }
            finally
            {
                // Papan harus disembunyikan meski puzzle dibatalkan.
                await _view.HideAsync(CancellationToken.None);
            }
        }
    }
}
