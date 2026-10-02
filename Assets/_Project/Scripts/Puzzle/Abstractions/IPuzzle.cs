using System.Threading;
using Cysharp.Threading.Tasks;

namespace Game.Puzzle
{
    public enum PuzzleResult
    {
        /// <summary>Puzzle berhasil diselesaikan.</summary>
        Completed = 0,

        /// <summary>Pemain membatalkan, menekan ESC, atau pemanggil menghentikan.</summary>
        Cancelled = 1,
    }

    /// <summary>
    /// Contract yang diimplementasikan setiap jenis puzzle.
    /// </summary>
    /// <remarks>
    /// UI berada di balik interface view. <see cref="PuzzleService"/> memperlakukan semua implementasi sama
    /// </remarks>
    public interface IPuzzle
    {
        /// <summary>Nama yang ditampilkan di header jendela puzzle.</summary>
        string DisplayName { get; }

        /// <summary>
        /// Menjalankan puzzle sampai selesai atau dibatalkan.
        /// </summary>
        /// <param name="cancellationToken">Aktif saat pemain membatalkan (ESC / Batal) atau pemanggil menghentikan.</param>
        /// <returns><see cref="PuzzleResult.Completed"/> atau <see cref="PuzzleResult.Cancelled"/>.</returns>
        UniTask<PuzzleResult> RunAsync(CancellationToken cancellationToken);
    }

    /// <summary>
    /// Menjalankan puzzle dan mengelola lifecycle sekitarnya (freeze input, jendela, pemulihan state).
    /// </summary>
    public interface IPuzzleService
    {
        /// <summary>Benar selama puzzle sedang dimainkan.</summary>
        bool IsRunning { get; }

        /// <summary>
        /// Membuka jendela puzzle, membekukan pemain, dan menunggu hasil puzzle. State game selalu
        /// dipulihkan setelahnya, bahkan bila puzzle dibatalkan atau melempar exception.
        /// </summary>
        UniTask<PuzzleResult> RunAsync(IPuzzle puzzle, CancellationToken cancellationToken);

        /// <summary>Meminta pembatalan puzzle yang berjalan (idempoten).</summary>
        void Abort();
    }
}
