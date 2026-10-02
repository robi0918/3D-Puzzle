namespace Game.Interaction
{
    /// <summary>
    /// Kontrak objek yang dapat diinteraksi oleh pemain.
    /// Deteksi, prompt, dan lifecycle puzzle ditangani terpisah oleh <see cref="Interactor"/> dan puzzle service.
    /// Menambah interactable baru tidak perlu mengubah sistem lain.
    /// </summary>
    public interface IInteractable
    {
        /// <summary>Teks prompt interaksi (misal: "Tekan E untuk membuka").</summary>
        string InteractionPrompt { get; }

        /// <summary>
        /// Apakah objek bisa diinteraksi sekarang (puzzle selesai, dikunci, sedang animasi, ...).
        /// Detektor harus melewati objek yang mengembalikan <c>false</c>.
        /// </summary>
        bool CanInteract();

        /// <summary>Eksekusi interaksi.</summary>
        void Interact();

        /// <summary>Dipanggil saat objek menjadi kandidat aktif detektor (highlight on).</summary>
        void OnFocusEnter();

        /// <summary>Dipanggil saat objek bukan lagi kandidat aktif detektor (highlight off).</summary>
        void OnFocusExit();
    }
}
