using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Game.Puzzle
{
    /// <summary>
    /// Memory Match mengendalikan <see cref="MemoryMatchModel"/> dan <see cref="IMemoryMatchView"/>,
    /// lalu mengembalikan hasil Completed atau Cancelled.
    /// </summary>
    /// <remarks>
    /// Alur berurutan: menunggu klik, menerapkannya ke model, lalu menganimasikan hasil. View hanya
    /// menerima klik saat metode ini menunggu, sehingga input tertahan selama animasi dan jeda pasangan.
    /// </remarks>
    public sealed class MemoryMatchPuzzle : IPuzzle
    {
        private readonly MemoryMatchConfig _config;
        private readonly IMemoryMatchView _view;
        private readonly System.Random _random;

        /// <param name="config">Pengaturan papan (grid dan ikon).</param>
        /// <param name="view">Implementasi UI.</param>
        /// <param name="random">RNG yang disuntikkan untuk test deterministik.</param>
        public MemoryMatchPuzzle(MemoryMatchConfig config, IMemoryMatchView view, System.Random random = null)
        {
            _config = config != null ? config : throw new ArgumentNullException(nameof(config));
            _view = view != null ? view : throw new ArgumentNullException(nameof(view));
            _random = random ?? new System.Random();
        }

        public string DisplayName => "Memory Match";

        public async UniTask<PuzzleResult> RunAsync(CancellationToken cancellationToken)
        {
            MemoryMatchModel model = new (_config.Columns, _config.Rows, _random);
            Sprite[] iconsByIconId = BuildIconTable(model.PairCount);

            try
            {
                await _view.ShowAsync(cancellationToken);

                _view.Build(model.CardCount, model.Columns, model.CreateIconIdSnapshot(), iconsByIconId);
                _view.SetMoveCount(model.Moves);

                while (!model.IsSolved)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    int cardIndex = await _view.WaitForCardClickAsync(cancellationToken);

                    FlipOutcome outcome = model.TryFlip(cardIndex);
                    if (outcome == FlipOutcome.Rejected) continue;

                    await _view.FlipAsync(cardIndex, true, cancellationToken);

                    if (outcome != FlipOutcome.PairReady) continue;

                    int first = model.PendingFirstCard;
                    int second = model.PendingSecondCard;
                    bool matched = model.ResolvePendingPair();

                    _view.SetMoveCount(model.Moves);

                    if (matched)
                    {
                        await _view.CelebrateMatchAsync(first, second, cancellationToken);
                        continue;
                    }

                    if (_config.MismatchDelay > 0f)
                    {
                        await UniTask.Delay(TimeSpan.FromSeconds(_config.MismatchDelay), cancellationToken: cancellationToken);
                    }

                    await _view.FlipAsync(first, false, cancellationToken);
                    await _view.FlipAsync(second, false, cancellationToken);
                }

                return PuzzleResult.Completed;
            }
            catch (OperationCanceledException)
            {
                return PuzzleResult.Cancelled;
            }
            finally
            {
                // Kartu harus disembunyikan meski puzzle dibatalkan.
                await _view.HideAsync(CancellationToken.None);
            }
        }

        /// <summary>
        /// Membangun tabel ikon. Daftar sprite diulang bila jumlahnya kurang dari pasangan,
        /// sehingga papan tetap bisa dimainkan dan menampilkan nomor kartu bila daftar kosong.
        /// </summary>
        private Sprite[] BuildIconTable(int pairCount)
        {
            Sprite[] source = _config.Icons;
            Sprite[] table = new Sprite[pairCount];

            for (int i = 0; i < pairCount; i++)
            {
                table[i] = source != null && source.Length > 0 ? source[i % source.Length] : null;
            }

            return table;
        }
    }
}
