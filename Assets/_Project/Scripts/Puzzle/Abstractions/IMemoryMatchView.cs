using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Game.Puzzle
{
    /// <summary>
    /// Kontrak UI Memory Match (implementasi MonoBehaviour di <c>MemoryMatchView</c>).
    /// </summary>
    /// <remarks>
    /// View hanya melaporkan *klik* dan memainkan *animasi*; ia tidak pernah memutuskan aturan.
    /// Aturan ada di <see cref="MemoryMatchModel"/>, orkestrasi di <see cref="MemoryMatchPuzzle"/>.
    /// </remarks>
    public interface IMemoryMatchView
    {
        /// <summary>Mengaktifkan konten puzzle.</summary>
        UniTask ShowAsync(CancellationToken cancellationToken);

        /// <summary>Menonaktifkan konten puzzle.</summary>
        UniTask HideAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Membangun (ulang) grid kartu. Kartu di-pool dan dipakai ulang antar sesi.
        /// </summary>
        /// <param name="cardCount">Jumlah kartu yang ditampilkan.</param>
        /// <param name="columns">Jumlah kolom grid.</param>
        /// <param name="iconIdByCard">Icon id untuk setiap indeks kartu, sesuai tata letak model.</param>
        /// <param name="iconsByIconId">Sprite untuk setiap icon id (boleh berisi <c>null</c>).</param>
        void Build(int cardCount, int columns, int[] iconIdByCard, Sprite[] iconsByIconId);

        /// <summary>Memperbarui label penghitung langkah (no-op bila nilainya tidak berubah).</summary>
        void SetMoveCount(int moves);

        /// <summary>Selesai saat pemain mengklik kartu. Mengabaikan klik saat tidak ada yang menunggu.</summary>
        UniTask<int> WaitForCardClickAsync(CancellationToken cancellationToken);

        /// <summary>Menganimasikan satu kartu menjadi menghadap atas atau bawah.</summary>
        UniTask FlipAsync(int cardIndex, bool faceUp, CancellationToken cancellationToken);

        /// <summary>Memainkan umpan balik "cocok" untuk sepasang kartu.</summary>
        UniTask CelebrateMatchAsync(int firstCard, int secondCard, CancellationToken cancellationToken);
    }
}
