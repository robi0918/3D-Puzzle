namespace Game.Player
{
    /// <summary>
    /// Rig kamera yang bisa dipilih pemain.
    /// </summary>
    /// <remarks>
    /// Nilai dibuat eksplisit: enum diserialisasi di dalam <see cref="PlayerController.CameraConfig"/>
    /// dan sudah tersimpan di prefab Player (CurrentView = 0), jadi angkanya tidak boleh berubah.
    /// </remarks>
    public enum CameraView
    {
        /// <summary>Third-person: kamera mengorbit pemain, pemain berputar mengikuti input.</summary>
        TPS = 0,

        /// <summary>Top-down: tampilan ortografis, pemain tetap berputar mengikuti input.</summary>
        TopDown = 1,
    }
}
