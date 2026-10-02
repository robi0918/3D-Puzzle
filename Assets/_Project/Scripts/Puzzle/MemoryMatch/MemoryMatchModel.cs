using System;

namespace Game.Puzzle
{
    /// <summary>Hasil satu flip kartu.</summary>
    public enum FlipOutcome
    {
        /// <summary>Kartu sudah cocok, sudah terbuka, atau di luar jangkauan.</summary>
        Rejected = 0,

        /// <summary>Kartu pertama dari pasangan baru sudah terbuka.</summary>
        FirstCard = 1,

        /// <summary>Kartu kedua terbuka: panggil <see cref="MemoryMatchModel.ResolvePendingPair"/>.</summary>
        PairReady = 2,
    }

    /// <summary>
    /// Rule Memory Match
    /// </summary>
    /// <remarks>
    /// Memaksa setiap aturan gameplay dibangun dari konstruktor: kartu tidak bisa dipilih dua kali,
    /// tidak lebih dari dua kartu yang terbuka, dan setiap kartu milik tepat satu pasangan (papan
    /// dibangun sebagai pasangan lalu diacak dengan Fisher-Yates).
    /// </remarks>
    public sealed class MemoryMatchModel
    {
        private readonly int[] _iconIdByCard;
        private readonly bool[] _faceUp;
        private readonly bool[] _matched;
        private readonly Random _random;

        /// <param name="columns">Kolom grid (>= 2).</param>
        /// <param name="rows">Baris grid (>= 2).</param>
        /// <param name="random">RNG yang disuntikkan agar test bisa menentukan pengacakan.</param>
        public MemoryMatchModel(int columns, int rows, Random random = null)
        {
            Columns = Math.Max(2, columns);
            Rows = Math.Max(2, rows);

            // Papan selalu genap: setiap ikon butuh tepat satu pasangan.
            if (Columns * Rows % 2 != 0) Columns++;

            PairCount = Columns * Rows / 2;
            _iconIdByCard = new int[Columns * Rows];
            _faceUp = new bool[Columns * Rows];
            _matched = new bool[Columns * Rows];
            _random = random ?? new Random();

            Restart();
        }

        /// <summary>Kolom grid yang dipakai.</summary>
        public int Columns { get; }

        /// <summary>Baris grid yang dipakai.</summary>
        public int Rows { get; }

        /// <summary>Total kartu.</summary>
        public int CardCount => _iconIdByCard.Length;

        /// <summary>Jumlah pasangan di papan.</summary>
        public int PairCount { get; }

        /// <summary>Jumlah pasangan attempt (bertambah saat pasangan diselesaikan).</summary>
        public int Moves { get; private set; }

        /// <summary>Pasangan yang sudah ditemukan.</summary>
        public int MatchedPairs { get; private set; }

        /// <summary>Benar ketika semua pasangan ditemukan.</summary>
        public bool IsSolved => MatchedPairs == PairCount;

        /// <summary>Indeks kartu pertama yang terbuka, menunggu resolusi, atau -1.</summary>
        public int PendingFirstCard { get; private set; } = -1;

        /// <summary>Indeks kartu kedua yang terbuka, menunggu resolusi, atau -1.</summary>
        public int PendingSecondCard { get; private set; } = -1;

        /// <summary>Icon id yang ditampilkan sebuah kartu (kedua kartu pasangan berbagi id).</summary>
        public int IconIdOf(int cardIndex) => _iconIdByCard[cardIndex];

        /// <summary>Apakah kartu sedang terbuka.</summary>
        public bool IsFaceUp(int cardIndex) => _faceUp[cardIndex];

        /// <summary>Apakah kartu sudah cocok.</summary>
        public bool IsMatched(int cardIndex) => _matched[cardIndex];

        /// <summary>Snapshot tata letak teracak, untuk view menggambar.</summary>
        public int[] CreateIconIdSnapshot() => (int[])_iconIdByCard.Clone();

        /// <summary>Mereset hitungan dan mengacak ulang papan (dipanggil tiap kali puzzle dibuka).</summary>
        public void Restart()
        {
            Moves = 0;
            MatchedPairs = 0;
            PendingFirstCard = -1;
            PendingSecondCard = -1;

            for (int i = 0; i < _iconIdByCard.Length; i++)
            {
                _iconIdByCard[i] = i % PairCount;
                _faceUp[i] = false;
                _matched[i] = false;
            }

            Shuffle();
        }

        /// <summary>
        /// Membalik kartu. Menolak kartu yang sudah cocok, sudah terbuka, atau di luar jangkauan,
        /// dan tidak pernah mengizinkan kartu ketiga terbuka saat pasangan sedang menunggu.
        /// </summary>
        public FlipOutcome TryFlip(int cardIndex)
        {
            if (cardIndex < 0 || cardIndex >= _iconIdByCard.Length) return FlipOutcome.Rejected;
            if (_matched[cardIndex] || _faceUp[cardIndex]) return FlipOutcome.Rejected;

            // Pasangan sudah menunggu: jangan izinkan kartu ketiga terbuka.
            if (PendingSecondCard >= 0) return FlipOutcome.Rejected;

            if (PendingFirstCard < 0)
            {
                PendingFirstCard = cardIndex;
                _faceUp[cardIndex] = true;
                return FlipOutcome.FirstCard;
            }

            PendingSecondCard = cardIndex;
            _faceUp[cardIndex] = true;
            return FlipOutcome.PairReady;
        }

        /// <summary>
        /// Menyelesaikan pasangan yang menunggu: menambah langkah, menandai cocok atau menutup kembali kedua kartu.
        /// </summary>
        /// <returns><c>true</c> bila kedua kartu cocok.</returns>
        public bool ResolvePendingPair()
        {
            if (PendingFirstCard < 0 || PendingSecondCard < 0) return false;

            int first = PendingFirstCard;
            int second = PendingSecondCard;

            Moves++;
            bool matched = _iconIdByCard[first] == _iconIdByCard[second];

            if (matched)
            {
                _matched[first] = true;
                _matched[second] = true;
                MatchedPairs++;
            }
            else
            {
                _faceUp[first] = false;
                _faceUp[second] = false;
            }

            PendingFirstCard = -1;
            PendingSecondCard = -1;

            return matched;
        }

        /// <summary>Pengacakan Fisher-Yates: setiap permutasi sama mungkinnya.</summary>
        private void Shuffle()
        {
            for (int i = _iconIdByCard.Length - 1; i > 0; i--)
            {
                int j = _random.Next(i + 1);

                int swap = _iconIdByCard[i];
                _iconIdByCard[i] = _iconIdByCard[j];
                _iconIdByCard[j] = swap;
            }
        }
    }
}
