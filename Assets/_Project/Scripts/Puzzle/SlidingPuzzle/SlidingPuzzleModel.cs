using System;

namespace Game.Puzzle
{
    /// <summary>
    /// Aturan Sliding Puzzle — C# murni tanpa dependensi Unity, mudah diuji unit.
    /// </summary>
    /// <remarks>
    /// Papan adalah array int datar dengan <c>0</c> sebagai slot kosong; state selesai adalah
    /// <c>[1, 2, ..., n-1, 0]</c> (row-major). Papan diacak dengan gerakan legal acak dari state
    /// selesai, menjamin hasil selalu bisa diselesaikan — permutasi acak murni tidak dipakai.
    /// </remarks>
    public sealed class SlidingPuzzleModel
    {
        private readonly int[] _board;
        private readonly int[] _neighbourBuffer = new int[4];
        private readonly Random _random;

        /// <param name="size">Ukuran grid (3 = 3x3).</param>
        /// <param name="random">RNG yang disuntikkan agar test bisa menentukan pengacakan.</param>
        public SlidingPuzzleModel(int size, Random random = null)
        {
            Size = Math.Max(2, size);
            _board = new int[Size * Size];
            _random = random ?? new Random();

            SetSolvedBoard();
        }

        /// <summary>Ukuran grid.</summary>
        public int Size { get; }

        /// <summary>Jumlah slot, termasuk slot kosong.</summary>
        public int SlotCount => _board.Length;

        /// <summary>Gerakan legal (oleh pemain) sejak <see cref="Restart"/> terakhir.</summary>
        public int Moves { get; private set; }

        /// <summary>Indeks slot kosong.</summary>
        public int EmptyIndex { get; private set; }

        /// <summary>Benar ketika semua tile sudah kembali ke urutan semula.</summary>
        public bool IsSolved
        {
            get
            {
                int last = _board.Length - 1;

                for (int slot = 0; slot < last; slot++)
                {
                    if (_board[slot] != slot + 1) return false;
                }

                return _board[last] == 0;
            }
        }

        /// <summary>Nilai tile di sebuah slot (0 = slot kosong).</summary>
        public int ValueAt(int slot) => _board[slot];

        /// <summary>Snapshot papan, untuk view menggambar.</summary>
        public int[] CreateSnapshot() => (int[])_board.Clone();

        /// <summary>
        /// Mereset hitungan dan mengacak papan dengan <paramref name="shuffleSteps"/> gerakan legal acak.
        /// </summary>
        public void Restart(int shuffleSteps)
        {
            int steps = Math.Max(1, shuffleSteps);

            // Ulangi bila (dengan sangat kecil kemungkinan) pengacakan menghasilkan papan selesai.
            do
            {
                SetSolvedBoard();

                int avoid = -1;

                for (int i = 0; i < steps; i++)
                {
                    int emptyBeforeMove = EmptyIndex;
                    int slot = PickRandomNeighbour(avoid);
                    if (slot < 0) break;

                    SwapIntoEmpty(slot);

                    // Jangan lakukan gerakan balik: itu akan mempertahankan papan selesai.
                    avoid = emptyBeforeMove;
                }
            }
            while (IsSolved);

            Moves = 0;
        }

        /// <summary>Apakah tile di <paramref name="slot"/> bisa bergeser ke slot kosong sekarang.</summary>
        public bool CanMove(int slot)
        {
            if (slot < 0 || slot >= _board.Length) return false;
            if (slot == EmptyIndex) return false;

            int column = slot % Size;
            int row = slot / Size;
            int emptyColumn = EmptyIndex % Size;
            int emptyRow = EmptyIndex / Size;

            return Math.Abs(column - emptyColumn) + Math.Abs(row - emptyRow) == 1;
        }

        /// <summary>
        /// Menggeser tile di <paramref name="slot"/> ke slot kosong.
        /// </summary>
        /// <param name="slot">Slot tile yang diklik pemain.</param>
        /// <param name="targetSlot">Slot yang dituju tile (sebelumnya kosong).</param>
        /// <returns><c>true</c> bila gerakan legal dan diterapkan.</returns>
        public bool TryMove(int slot, out int targetSlot)
        {
            targetSlot = -1;

            if (!CanMove(slot)) return false;

            targetSlot = EmptyIndex;
            SwapIntoEmpty(slot);
            Moves++;

            return true;
        }

        private void SetSolvedBoard()
        {
            for (int slot = 0; slot < _board.Length; slot++)
            {
                _board[slot] = slot + 1;
            }

            // Slot terakhir menyimpan tile kosong; nilainya 1..n-1 ditambah 0.
            _board[_board.Length - 1] = 0;
            EmptyIndex = _board.Length - 1;
        }

        /// <summary>Memindahkan tile di <paramref name="slot"/> ke slot kosong.</summary>
        private void SwapIntoEmpty(int slot)
        {
            _board[EmptyIndex] = _board[slot];
            _board[slot] = 0;
            EmptyIndex = slot;
        }

        /// <summary>Memilih tetangga acak dari slot kosong, tidak pernah memilih <paramref name="avoid"/>.</summary>
        private int PickRandomNeighbour(int avoid)
        {
            int count = 0;
            int empty = EmptyIndex;
            int column = empty % Size;
            int row = empty / Size;

            if (row > 0) AddCandidate(empty - Size, avoid, ref count);
            if (row < Size - 1) AddCandidate(empty + Size, avoid, ref count);
            if (column > 0) AddCandidate(empty - 1, avoid, ref count);
            if (column < Size - 1) AddCandidate(empty + 1, avoid, ref count);

            return count == 0 ? -1 : _neighbourBuffer[_random.Next(count)];
        }

        private void AddCandidate(int slot, int avoid, ref int count)
        {
            if (slot == avoid) return;

            _neighbourBuffer[count] = slot;
            count++;
        }
    }
}
