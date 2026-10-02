using System;

namespace Game.Core
{
    /// <summary>
    /// Mode utama permainan. Nilai eksplisit agar konsisten dengan serialisasi di scene/prefab.
    /// </summary>
    public enum GameState
    {
        /// <summary>Eksplorasi bebas: input karakter, kamera, dan interaksi aktif.</summary>
        Exploration = 0,

        /// <summary>Puzzle UI 2D sedang terbuka: input karakter, kamera, dan interaksi dibekukan.</summary>
        Puzzle = 1,
    }

    /// <summary>
    /// Kontrak penyedia state permainan dengan notifikasi perubahan state.
    /// </summary>
    public interface IGameStateProvider
    {
        /// <summary>State permainan saat ini.</summary>
        GameState CurrentState { get; }

        /// <summary>Dipicu hanya ketika state benar-benar berubah (tanpa notifikasi duplikat).</summary>
        event Action<GameState> OnStateChanged;

        /// <summary>Masuk ke mode <see cref="GameState.Puzzle"/>.</summary>
        void EnterPuzzle();

        /// <summary>Kembali ke mode <see cref="GameState.Exploration"/>.</summary>
        void EnterExploration();
    }
}
