using System;
using System.Collections.Generic;
using Game.Puzzle;

namespace Game.Puzzle.ModelTests
{
    /// <summary>
    /// Executes the puzzle rules outside Unity.
    /// </summary>
    /// <remarks>
    /// Both puzzle models are plain C# with no engine dependency by design, so they can be run
    /// directly on the Mono runtime bundled with the Unity Editor. That makes the promises in
    /// README.md ("a shuffled sliding board is always solvable", "at most two cards are ever face
    /// up", "a perfect game needs one move per pair") verifiable in a second, without opening the
    /// Editor. Run with <c>bash Tools/run-model-tests.sh</c>.<br/>
    /// The same rules are covered as EditMode tests in <c>Assets/_Project/Tests/EditMode</c>.
    /// </remarks>
    public static class ModelTests
    {
        private const int SeedSweep = 500;

        private static int _failures;

        public static int Main()
        {
            Console.WriteLine("=== Memory Match model ===");
            MemoryMatchRules();

            Console.WriteLine();
            Console.WriteLine("=== Sliding Puzzle model ===");
            SlidingPuzzleRules();

            Console.WriteLine();
            Console.WriteLine(_failures == 0 ? "ALL CHECKS PASSED" : $"{_failures} CHECK(S) FAILED");

            return _failures == 0 ? 0 : 1;
        }

        private static void MemoryMatchRules()
        {
            bool everyIconTwice = true;

            for (int seed = 1; seed <= 200; seed++)
            {
                MemoryMatchModel model = new MemoryMatchModel(4, 4, new Random(seed));
                Dictionary<int, int> occurrences = new Dictionary<int, int>();

                for (int card = 0; card < model.CardCount; card++)
                {
                    occurrences.TryGetValue(model.IconIdOf(card), out int count);
                    occurrences[model.IconIdOf(card)] = count + 1;
                }

                if (occurrences.Count != model.PairCount) everyIconTwice = false;

                foreach (KeyValuePair<int, int> entry in occurrences)
                {
                    if (entry.Value != 2) everyIconTwice = false;
                }
            }

            Check(everyIconTwice, "200 shuffles: every icon appears exactly twice (16 cards / 8 pairs)");

            MemoryMatchModel oddBoard = new MemoryMatchModel(3, 3, new Random(7));
            Check(oddBoard.CardCount % 2 == 0,
                $"odd grid request 3x3 -> board forced even ({oddBoard.CardCount} cards)");

            MemoryMatchModel single = new MemoryMatchModel(2, 2, new Random(1));
            bool firstFlip = single.TryFlip(0) == FlipOutcome.FirstCard;
            bool sameCardRejected = single.TryFlip(0) == FlipOutcome.Rejected;
            Check(firstFlip && sameCardRejected, "same card cannot be picked twice");

            single.TryFlip(1);
            Check(single.TryFlip(2) == FlipOutcome.Rejected,
                "a third card is rejected while a pair is pending (max two cards face-up)");

            MemoryMatchModel mismatch = new MemoryMatchModel(2, 2, new Random(4));
            int different = FindCardWithDifferentIcon(mismatch, 0);
            mismatch.TryFlip(0);
            mismatch.TryFlip(different);
            bool matched = mismatch.ResolvePendingPair();

            Check(!matched && !mismatch.IsFaceUp(0) && !mismatch.IsFaceUp(different)
                  && mismatch.Moves == 1 && mismatch.MatchedPairs == 0,
                "mismatch: both cards turn back down, one move counted, no pair matched");

            bool alwaysWinnable = true;
            bool matchedCardsLocked = true;

            for (int seed = 1; seed <= 200; seed++)
            {
                MemoryMatchModel model = new MemoryMatchModel(4, 4, new Random(seed));

                for (int iconId = 0; iconId < model.PairCount; iconId++)
                {
                    int cardA = FindUnmatchedCard(model, iconId, -1);
                    int cardB = FindUnmatchedCard(model, iconId, cardA);

                    model.TryFlip(cardA);
                    model.TryFlip(cardB);

                    if (!model.ResolvePendingPair()) alwaysWinnable = false;
                    if (model.TryFlip(cardA) != FlipOutcome.Rejected) matchedCardsLocked = false;
                    if (model.TryFlip(cardB) != FlipOutcome.Rejected) matchedCardsLocked = false;
                }

                if (!model.IsSolved || model.MatchedPairs != model.PairCount || model.Moves != model.PairCount)
                {
                    alwaysWinnable = false;
                }
            }

            Check(alwaysWinnable, "200 seeds: perfect play always solves the board in exactly 8 moves");
            Check(matchedCardsLocked, "matched cards can never be flipped again");

            MemoryMatchModel restarted = new MemoryMatchModel(2, 2, new Random(5));
            restarted.TryFlip(0);
            restarted.TryFlip(1);
            restarted.ResolvePendingPair();
            restarted.Restart();

            Check(restarted.Moves == 0 && restarted.MatchedPairs == 0
                  && restarted.PendingFirstCard == -1 && !restarted.IsSolved,
                "Restart() resets the counters and clears the board");
        }

        private static void SlidingPuzzleRules()
        {
            SlidingPuzzleModel fresh = new SlidingPuzzleModel(3, new Random(11));
            Check(fresh.IsSolved && fresh.EmptyIndex == fresh.SlotCount - 1,
                "a fresh model starts solved (hole in the bottom-right slot)");

            bool alwaysSolvable = true;
            bool neverAlreadySolved = true;
            bool tilesIntact = true;
            bool movesStartAtZero = true;

            for (int seed = 1; seed <= SeedSweep; seed++)
            {
                SlidingPuzzleModel model = new SlidingPuzzleModel(3, new Random(seed));
                model.Restart(270);

                int[] board = model.CreateSnapshot();

                if (model.IsSolved) neverAlreadySolved = false;
                if (!IsSolvableByParity(board, 3)) alwaysSolvable = false;
                if (model.Moves != 0) movesStartAtZero = false;

                bool[] seen = new bool[board.Length];

                foreach (int value in board)
                {
                    if (value < 0 || value >= board.Length || seen[value])
                    {
                        tilesIntact = false;
                        break;
                    }

                    seen[value] = true;
                }
            }

            Check(alwaysSolvable, $"{SeedSweep} seeds: scrambled board is always solvable (inversion parity)");
            Check(neverAlreadySolved, $"{SeedSweep} seeds: the scramble never hands back a solved board");
            Check(tilesIntact, $"{SeedSweep} seeds: every tile value appears exactly once");
            Check(movesStartAtZero, "the player move counter starts at 0 after Restart");

            SlidingPuzzleModel model2 = new SlidingPuzzleModel(3, new Random(13));
            bool rejected = !model2.CanMove(0) && !model2.TryMove(0, out int rejectedTarget)
                            && rejectedTarget == -1 && model2.Moves == 0;
            Check(rejected, "a tile that is not adjacent to the hole is rejected without counting a move");

            int hole = model2.EmptyIndex;
            int neighbour = hole - 1;
            bool moved = model2.TryMove(neighbour, out int target);

            Check(moved && target == hole && model2.EmptyIndex == neighbour && model2.Moves == 1,
                "a legal move slides the tile into the hole and counts one move");

            model2.TryMove(hole, out _);
            Check(model2.IsSolved, "sliding the same tile back restores the solved board");

            int agreements = 0;
            const int boards = 5;

            for (int seed = 1; seed <= boards; seed++)
            {
                SlidingPuzzleModel model = new SlidingPuzzleModel(3, new Random(seed));
                model.Restart(270);

                int[] board = model.CreateSnapshot();

                if (IsSolvableByParity(board, 3) == IsSolvableBySearch(board, 3, 400000)) agreements++;
            }

            Check(agreements == boards,
                $"parity heuristic and breadth-first search agree ({agreements}/{boards} boards)");
        }

        private static int FindUnmatchedCard(MemoryMatchModel model, int iconId, int skip)
        {
            for (int card = 0; card < model.CardCount; card++)
            {
                if (card == skip || model.IsMatched(card)) continue;
                if (model.IconIdOf(card) == iconId) return card;
            }

            return -1;
        }

        private static int FindCardWithDifferentIcon(MemoryMatchModel model, int card)
        {
            for (int other = 0; other < model.CardCount; other++)
            {
                if (model.IconIdOf(other) != model.IconIdOf(card)) return other;
            }

            return -1;
        }

        /// <summary>
        /// Standard sliding-puzzle parity rule: for odd sizes the inversion count must be even; for
        /// even sizes the inversion count plus the blank's row (counted from the bottom) must be odd.
        /// </summary>
        private static bool IsSolvableByParity(int[] board, int size)
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

            return size % 2 != 0 ? inversions % 2 == 0 : (inversions + blankRowFromBottom) % 2 == 1;
        }

        /// <summary>
        /// Independent oracle: breadth-first search from the scrambled board to the solved board.
        /// Slow but assumption-free, used to validate the parity rule above.
        /// </summary>
        private static bool IsSolvableBySearch(int[] board, int size, int budget)
        {
            int[] goal = new int[board.Length];

            for (int i = 0; i < goal.Length - 1; i++) goal[i] = i + 1;

            string start = string.Join(",", board);
            string target = string.Join(",", goal);

            if (start == target) return true;

            Queue<int[]> queue = new Queue<int[]>();
            HashSet<string> visited = new HashSet<string> { start };

            queue.Enqueue(board);

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
                    if (visited.Add(key)) queue.Enqueue(next);
                }
            }

            return false;
        }

        private static void Check(bool passed, string label)
        {
            if (!passed) _failures++;

            Console.WriteLine("  " + (passed ? "PASS" : "FAIL") + "  " + label);
        }
    }
}
