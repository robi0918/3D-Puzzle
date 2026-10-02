using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Game.Puzzle
{
    /// <summary>
    /// Kontrak UI Sliding Puzzle (implementasi MonoBehaviour di <c>SlidingPuzzleView</c>).
    /// </summary>
    public interface ISlidingPuzzleView
    {
        /// <summary>Mengaktifkan konten puzzle.</summary>
        UniTask ShowAsync(CancellationToken cancellationToken);

        /// <summary>Menonaktifkan konten puzzle.</summary>
        UniTask HideAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Membangun (ulang) papan. Tile di-pool dan dipakai ulang antar sesi, dan posisinya
        /// dihitung sekali per build (tanpa layout group per frame).
        /// </summary>
        /// <param name="size">Ukuran grid (3 = 3x3).</param>
        /// <param name="board">State papan: nilai per slot, <c>0</c> menandai slot kosong.</param>
        /// <param name="spritesByValue">Sprite opsional untuk setiap nilai tile.</param>
        void Build(int size, int[] board, Sprite[] spritesByValue);

        /// <summary>Memperbarui label penghitung langkah (no-op bila nilainya tidak berubah).</summary>
        void SetMoveCount(int moves);

        /// <summary>Selesai saat pemain mengklik tile. Mengabaikan klik saat tidak ada yang menunggu.</summary>
        UniTask<int> WaitForTileClickAsync(CancellationToken cancellationToken);

        /// <summary>Menggeser tile di <paramref name="fromSlot"/> ke <paramref name="toSlot"/>.</summary>
        UniTask AnimateMoveAsync(int fromSlot, int toSlot, CancellationToken cancellationToken);
    }
}
