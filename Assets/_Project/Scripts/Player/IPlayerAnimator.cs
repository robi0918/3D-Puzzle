namespace Game.Player
{
    /// <summary>
    /// Kontrak layanan animasi pemain.
    /// </summary>
    /// <remarks>
    /// Membuat kode gerak mendorong state animasi lewat abstraksi, bukan langsung ke
    /// <see cref="UnityEngine.Animator"/>, sehingga <c>PlayerController</c> tetap mudah diuji
    /// dan animator bisa diganti atau dimatikan tanpa menyentuh logika gerak.
    /// </remarks>
    public interface IPlayerAnimator
    {
        /// <summary>Memperbarui flag grounded.</summary>
        void SetGrounded(bool value);

        /// <summary>Memperbarui flag jump.</summary>
        void SetJump(bool value);

        /// <summary>Memperbarui flag free-fall.</summary>
        void SetFreeFall(bool value);

        /// <summary>Memicu trigger interact.</summary>
        void TriggerInteract();

        /// <summary>
        /// Mendorong nilai locomotion blend. Dipanggil sekali per frame oleh <c>PlayerController</c>.
        /// </summary>
        /// <param name="speedMetersPerSecond">Kecepatan horizontal saat ini, dalam m/s (yang diharapkan blend tree).</param>
        void UpdateLocomotion(float speedMetersPerSecond);
    }
}
