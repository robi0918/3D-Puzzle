using UnityEngine;

namespace Game.Puzzle
{
    /// <summary>
    /// Konfigurasi Sliding Puzzle yang diatur melalui Inspector.
    /// </summary>
    /// <remarks>
    /// Grid default 3x3. Panjang pengacakan dinyatakan sebagai faktor dari ukuran papan
    /// agar papan yang lebih besar mendapat pengacakan yang proporsional.
    /// </remarks>
    [CreateAssetMenu(fileName = "SlidingPuzzleConfig", menuName = "3D Puzzle Explorer/Sliding Puzzle Config", order = 2)]
    public sealed class SlidingPuzzleConfig : ScriptableObject
    {
        [Header("Papan")]
        [SerializeField, Range(2, 6)] private int _size = 3;

        [Header("Tile (opsional — nilai N memakai sprite N-1)")]
        [SerializeField] private Sprite[] _tileSprites;

        [Header("Pengacakan")]
        [Tooltip("Langkah acak valid = faktor x (size x size). Selalu menghasilkan papan yang bisa diselesaikan.")]
        [SerializeField, Range(4, 200)] private int _shuffleStepsFactor = 30;

        [Header("Tempo (detik)")]
        [SerializeField, Range(0f, 2f)] private float _solvedPause = 0.4f;

        /// <summary>Ukuran grid, dibatasi ke rentang wajar.</summary>
        public int Size => Mathf.Clamp(_size, 2, 6);

        /// <summary>Sprite untuk tile (nilai 1 memakai indeks 0). Boleh kosong.</summary>
        public Sprite[] TileSprites => _tileSprites;

        /// <summary>Jumlah langkah legal acak untuk mengacak papan.</summary>
        public int ShuffleSteps => Mathf.Max(20, _shuffleStepsFactor * Size * Size);

        /// <summary>Jeda setelah papan selesai agar pemain sempat melihat hasilnya.</summary>
        public float SolvedPause => _solvedPause;
    }
}
