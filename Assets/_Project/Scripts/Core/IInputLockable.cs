namespace Game.Core
{
    /// <summary>
    /// Kontrak sistem yang harus mengabaikan input pemain saat puzzle terbuka.
    /// Composition root dapat mengunci gerak, kamera, dan interaksi tanpa membuat sistem saling mengenal.
    /// </summary>
    public interface IInputLockable
    {
        /// <summary>
        /// Kunci atau buka input. Implementasi harus melepas input yang sedang ditahan saat penguncian
        /// agar tidak ada tombol yang dianggap terus aktif setelah dibuka kembali.
        /// </summary>
        void SetInputLocked(bool locked);
    }
}
