using System;
using NUnit.Framework;

namespace Game.Puzzle.Tests
{
    /// <summary>
    /// EditMode test untuk aturan Sliding Puzzle, dengan fokus pada syarat utama brief:
    /// papan yang diacak harus SELALU bisa diselesaikan.
    /// </summary>
    public sealed class SlidingPuzzleModelTests
    {
        [Test]
        public void Constructor_StartsFromASolvedBoard()
        {
            SlidingPuzzleModel model = new SlidingPuzzleModel(3, new Random(11));

            Assert.IsTrue(model.IsSolved);
            Assert.AreEqual(model.SlotCount - 1, model.EmptyIndex);
        }

        [Test]
        public void Restart_ProducesASolvableBoardThatIsNotAlreadySolved()
        {
            for (int seed = 1; seed <= 200; seed++)
            {
                SlidingPuzzleModel model = new SlidingPuzzleModel(3, new Random(seed));
                model.Restart(200);

                Assert.IsFalse(model.IsSolved, $"seed {seed}: a scramble must actually scramble");
                Assert.IsTrue(IsSolvable(model.CreateSnapshot(), model.Size), $"seed {seed}: board must be solvable");
                Assert.AreEqual(0, model.Moves, "player move counter must start at zero");
            }
        }

        [Test]
        public void Restart_KeepsEveryTileExactlyOnce()
        {
            SlidingPuzzleModel model = new SlidingPuzzleModel(3, new Random(12));
            model.Restart(80);

            int[] board = model.CreateSnapshot();
            bool[] seen = new bool[board.Length];

            foreach (int value in board)
            {
                Assert.GreaterOrEqual(value, 0);
                Assert.Less(value, board.Length);
                Assert.IsFalse(seen[value], $"value {value} appears twice");
                seen[value] = true;
            }
        }

        [Test]
        public void TryMove_RejectsTilesThatAreNotAdjacentToTheHole()
        {
            SlidingPuzzleModel model = new SlidingPuzzleModel(3, new Random(13));

            // Pada papan 3x3 yang selesai lubang ada di pojok kanan-bawah, jadi slot 0 tidak bisa digeser.
            Assert.IsFalse(model.CanMove(0));
            Assert.IsFalse(model.TryMove(0, out int target));
            Assert.AreEqual(-1, target);
            Assert.AreEqual(0, model.Moves);
        }

        [Test]
        public void TryMove_SlidesAnAdjacentTileAndCountsTheMove()
        {
            SlidingPuzzleModel model = new SlidingPuzzleModel(3, new Random(14));

            int hole = model.EmptyIndex;
            int neighbour = hole - 1;

            Assert.IsTrue(model.CanMove(neighbour));

            Assert.IsTrue(model.TryMove(neighbour, out int target));
            Assert.AreEqual(hole, target, "the tile moves into the previous hole");
            Assert.AreEqual(neighbour, model.EmptyIndex, "the hole follows the tile");
            Assert.AreEqual(1, model.Moves);
        }

        [Test]
        public void UndoingAMove_RestoresTheSolvedBoard()
        {
            SlidingPuzzleModel model = new SlidingPuzzleModel(3, new Random(15));

            int hole = model.EmptyIndex;
            int neighbour = hole - 1;
            model.TryMove(neighbour, out _);

            // Geser tile yang sama langsung kembali.
            model.TryMove(hole, out _);

            Assert.IsTrue(model.IsSolved);
        }

        [Test]
        public void ScrambledBoard_IsSolvableAccordingToAnIndependentSearch()
        {
            for (int seed = 1; seed <= 3; seed++)
            {
                SlidingPuzzleModel model = new SlidingPuzzleModel(3, new Random(seed));
                model.Restart(200);

                int[] board = model.CreateSnapshot();

                bool paritySaysSolvable = IsSolvable(board, model.Size);
                bool searchSaysSolvable = CanSolveWithinBudget(board, model.Size, 300000);

                Assert.IsTrue(paritySaysSolvable, $"seed {seed}: inversion parity says solvable");
                Assert.AreEqual(paritySaysSolvable, searchSaysSolvable,
                    $"seed {seed}: parity heuristic and breadth-first search must agree");
            }
        }

        /// <summary>
        /// Uji paritas sliding puzzle standar: untuk ukuran ganjil jumlah inversi harus genap;
        /// untuk ukuran genap jumlah inversi ditambah baris slot kosong (dihitung dari bawah)
        /// harus ganjil.
        /// </summary>
        private static bool IsSolvable(int[] board, int size)
        {
            int inversions = 0;
            int blankRowFromBottom = 0;

            for (int i = 0; i < board.Length; i++)
            {
                if (board[i] == 0)
                {
                    blankRowFromBottom = size - i / size;
                    continue;
                }

                for (int j = i + 1; j < board.Length; j++)
                {
                    if (board[j] == 0) continue;
                    if (board[i] > board[j]) inversions++;
                }
            }

            if (size % 2 != 0) return inversions % 2 == 0;

            return (inversions + blankRowFromBottom) % 2 == 1;
        }

        /// <summary>
        /// Pencarian breadth-first atas state papan, dipakai sebagai oracle independen untuk
        /// heuristik paritas di atas (hanya untuk papan kecil).
        /// </summary>
        private static bool CanSolveWithinBudget(int[] board, int size, int budget)
        {
            int[] goal = new int[board.Length];

            for (int i = 0; i < goal.Length - 1; i++) goal[i] = i + 1;

            string start = string.Join(",", board);
            string target = string.Join(",", goal);

            if (start == target) return true;

            System.Collections.Generic.Queue<int[]> queue = new System.Collections.Generic.Queue<int[]>();
            System.Collections.Generic.HashSet<string> visited = new System.Collections.Generic.HashSet<string>();

            queue.Enqueue(board);
            visited.Add(start);

            int expanded = 0;

            while (queue.Count > 0 && expanded++ < budget)
            {
                int[] current = queue.Dequeue();
                int hole = Array.IndexOf(current, 0);

                int column = hole % size;
                int row = hole / size;

                for (int direction = 0; direction < 4; direction++)
                {
                    int nextColumn = column + (direction == 0 ? -1 : direction == 1 ? 1 : 0);
                    int nextRow = row + (direction == 2 ? -1 : direction == 3 ? 1 : 0);

                    if (nextColumn < 0 || nextColumn >= size || nextRow < 0 || nextRow >= size) continue;

                    int swap = nextRow * size + nextColumn;

                    int[] next = (int[])current.Clone();
                    next[hole] = next[swap];
                    next[swap] = 0;

                    string key = string.Join(",", next);
                    if (key == target) return true;
                    if (!visited.Add(key)) continue;

                    queue.Enqueue(next);
                }
            }

            return false;
        }
    }
}
