using System;
using UnityEngine;

namespace Game.Player
{
    /// <summary>
    /// Kontrak input pemain yang memisahkan logika gameplay dari perangkat input.
    /// </summary>
    public interface IPlayerInput
    {
        /// <summary>
        /// Saat <c>false</c> semua axis bernilai nol dan permintaan one-shot dibuang.
        /// Digunakan untuk membekukan pemain saat UI puzzle 2D terbuka.
        /// </summary>
        bool IsEnabled { get; set; }

        /// <summary>Axis gerak mentah, sudah dinormalkan oleh layer device.</summary>
        Vector2 GetMove();

        /// <summary>Delta lihat/putar. Per-frame delta, BUKAN per-detik — jangan dikalikan deltaTime.</summary>
        Vector2 GetLook();

        /// <summary>Apakah tombol lompat sedang ditahan.</summary>
        bool GetJump();

        /// <summary>Apakah tombol sprint sedang ditahan.</summary>
        bool GetSprint();

        /// <summary>Mengembalikan <c>true</c> sekali per penekanan toggle view (edge triggered, self-consuming).</summary>
        bool ConsumeSwitchViewRequest();

        /// <summary>Dipicu sekali per penekanan interaksi.</summary>
        event Action OnInteractRequested;

        /// <summary>Meng-nol-kan semua axis dan menghapus permintaan one-shot yang tertunda.</summary>
        void ResetState();
    }
}
