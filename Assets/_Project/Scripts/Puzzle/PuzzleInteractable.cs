using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Interaction;
using UnityEngine;

namespace Game.Puzzle
{
    /// <summary>
    /// Objek 3D di dunia yang membuka puzzle 2D saat diinteraksi.
    /// </summary>
    /// <remarks>
    /// Puzzle disuplai melalui factory delegate agar menambah puzzle baru atau objek baru tidak
    /// memerlukan perubahan pada <see cref="Interactor"/> atau <see cref="PuzzleService"/> (Open/Closed).
    /// Selama puzzle berjalan objek menolak interaksi (aman dari spam-klik); setelah selesai objek tetap
    /// bisa diinteraksi lagi, sehingga puzzle bisa dimainkan berulang kali.
    /// </remarks>
    public sealed class PuzzleInteractable : InteractableBase
    {
        private IPuzzleService _puzzleService;
        private Func<IPuzzle> _puzzleFactory;
        private bool _isRunning;

        /// <summary>Disuntikkan oleh Composition Root.</summary>
        public void Construct(IPuzzleService puzzleService, Func<IPuzzle> puzzleFactory)
        {
            _puzzleService = puzzleService ?? throw new ArgumentNullException(nameof(puzzleService));
            _puzzleFactory = puzzleFactory ?? throw new ArgumentNullException(nameof(puzzleFactory));
        }

        /// <inheritdoc />
        public override bool CanInteract() =>
            base.CanInteract() && !_isRunning && _puzzleService != null && _puzzleFactory != null;

        /// <inheritdoc />
        public override void Interact()
        {
            if (!CanInteract()) return;

            RunAsync(destroyCancellationToken).Forget();
        }

        private async UniTaskVoid RunAsync(CancellationToken cancellationToken)
        {
            _isRunning = true;

            try
            {
                // Hasil sengaja diabaikan: Completed maupun Cancelled membiarkan objek siap untuk sesi berikutnya.
                await _puzzleService.RunAsync(_puzzleFactory(), cancellationToken);
            }
            finally
            {
                _isRunning = false;
            }
        }
    }
}
