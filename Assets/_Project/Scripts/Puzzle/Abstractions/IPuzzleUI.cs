using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Game.Puzzle
{
    /// <summary>
    /// Contract shell UI modal yang membungkus tampilan puzzle.
    /// </summary>
    public interface IPuzzleUI
    {
        /// <summary>Dipicu oleh tombol "Batal" atau tombol ESC.</summary>
        event Action CancelRequested;

        /// <summary>Menampilkan jendela dengan judul yang diberikan.</summary>
        UniTask OpenAsync(string title, CancellationToken cancellationToken);

        /// <summary>Menyembunyikan jendela (fade dan nonaktifkan).</summary>
        UniTask CloseAsync(CancellationToken cancellationToken);
    }
}
