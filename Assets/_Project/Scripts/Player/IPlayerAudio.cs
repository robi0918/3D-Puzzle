using UnityEngine;

namespace Game.Player
{
    /// <summary>
    /// Kontrak layanan audio pemain.
    /// </summary>
    /// <remarks>
    /// Kode gerak hanya melaporkan apa yang terjadi (kecepatan, grounded, mendarat);
    /// keputusan pemutaran seperti kadens, pemilihan clip, variasi pitch, dan volume ada di implementasi.
    /// </remarks>
    public interface IPlayerAudio
    {
        /// <summary>
        /// Menyuntikkan audio source yang dipakai. Argumen boleh <c>null</c>; implementasi
        /// akan jatuh ke source yang ditemukan di GameObject-nya sendiri.
        /// </summary>
        void Configure(AudioSource footsteps, AudioSource landing);

        /// <summary>Dipanggil sekali per frame selama grounded.</summary>
        /// <param name="normalisedSpeed">Kecepatan saat ini dibagi kecepatan sprint (0-1).</param>
        /// <param name="isGrounded">Apakah karakter sedang berpijak.</param>
        void UpdateLocomotion(float normalisedSpeed, bool isGrounded);

        /// <summary>Memutar suara mendarat satu kali.</summary>
        void PlayLanding();

    }
}
