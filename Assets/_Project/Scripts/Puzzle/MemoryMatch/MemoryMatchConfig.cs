using UnityEngine;

namespace Game.Puzzle
{
    /// <summary>
    /// Konfigurasi Memory Match yang diatur melalui Inspector.
    /// </summary>
    /// <remarks>
    /// ScriptableObject agar pengaturan papan bisa dipakai ulang oleh beberapa objek dan diedit
    /// tanpa mengubah kode. Jumlah pasangan di Inheritance dari grid, bukan diisi manual.
    /// </remarks>
    [CreateAssetMenu(fileName = "MemoryMatchConfig", menuName = "3D Puzzle Explorer/Memory Match Config", order = 1)]
    public sealed class MemoryMatchConfig : ScriptableObject
    {
        [Header("Grid")]
        [SerializeField, Range(2, 8)] private int _columns = 4;
        [SerializeField, Range(2, 8)] private int _rows = 4;

        [Header("Ikon (satu sprite per pasangan)")]
        [SerializeField] private Sprite[] _icons;

        [Header("Tempo (detik)")]
        [Tooltip("Lama pasangan yang tidak cocok tetap terlihat sebelum tertutup kembali.")]
        [SerializeField, Range(0f, 2f)] private float _mismatchDelay = 0.75f;

        /// <summary>Jumlah kolom grid (minimal 2).</summary>
        public int Columns => Mathf.Max(2, _columns);

        /// <summary>Jumlah baris grid (minimal 2).</summary>
        public int Rows => Mathf.Max(2, _rows);

        /// <summary>Total kartu. Selalu genap agar setiap ikon memiliki pasangan.</summary>
        public int CardCount => Columns * Rows;

        /// <summary>Jumlah pasangan ikon di papan.</summary>
        public int PairCount => CardCount / 2;

        /// <summary>Sprite ikon; puzzle mengulang array bila jumlahnya kurang dari jumlah pasangan.</summary>
        public Sprite[] Icons => _icons;

        /// <summary>Jeda sebelum pasangan yang salah ditutup kembali.</summary>
        public float MismatchDelay => _mismatchDelay;

        private void OnValidate()
        {
            // Jumlah kartu ganjil membuat satu ikon tidak memiliki pasangan; genapkan papan.
            if (CardCount % 2 == 0) return;

            _columns = Mathf.Min(8, Columns + 1);
        }
    }
}
