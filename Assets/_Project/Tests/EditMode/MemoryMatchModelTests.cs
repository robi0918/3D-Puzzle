using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace Game.Puzzle.Tests
{
    /// <summary>
    /// EditMode test untuk aturan Memory Match. Model berupa C# murni, sehingga tidak perlu scene atau
    /// play mode — cukup seed RNG dan seluruh papan menjadi deterministik.
    /// </summary>
    public sealed class MemoryMatchModelTests
    {
        [Test]
        public void Constructor_BuildsBoardWhereEveryIconAppearsExactlyTwice()
        {
            MemoryMatchModel model = new MemoryMatchModel(4, 4, new Random(1234));

            Dictionary<int, int> occurrences = new Dictionary<int, int>();

            for (int card = 0; card < model.CardCount; card++)
            {
                int icon = model.IconIdOf(card);

                occurrences.TryGetValue(icon, out int count);
                occurrences[icon] = count + 1;
            }

            Assert.AreEqual(model.PairCount, occurrences.Count, "one entry per pair");
            foreach (KeyValuePair<int, int> pair in occurrences)
            {
                Assert.AreEqual(2, pair.Value, $"icon {pair.Key} must appear exactly twice");
            }
        }

        [Test]
        public void Constructor_KeepsBoardEven_WhenGridWouldBeOdd()
        {
            MemoryMatchModel model = new MemoryMatchModel(3, 3, new Random(7));

            Assert.AreEqual(0, model.CardCount % 2, "an odd board would leave an icon without a partner");
        }

        [Test]
        public void TryFlip_RejectsTheSameCardTwice()
        {
            MemoryMatchModel model = new MemoryMatchModel(2, 2, new Random(1));

            Assert.AreEqual(FlipOutcome.FirstCard, model.TryFlip(0));
            Assert.AreEqual(FlipOutcome.Rejected, model.TryFlip(0), "a card cannot be picked twice");
        }

        [Test]
        public void TryFlip_RejectsAThirdCard_WhileAPairIsPending()
        {
            MemoryMatchModel model = new MemoryMatchModel(2, 2, new Random(2));

            model.TryFlip(0);
            model.TryFlip(1);

            Assert.AreEqual(FlipOutcome.Rejected, model.TryFlip(2),
                "at most two cards may be face-up at any time");
        }

        [Test]
        public void TryFlip_RejectsMatchedCards()
        {
            MemoryMatchModel model = new MemoryMatchModel(2, 2, new Random(3));

            int first = FindPair(model, out int second);

            model.TryFlip(first);
            model.TryFlip(second);
            Assert.IsTrue(model.ResolvePendingPair());

            Assert.AreEqual(FlipOutcome.Rejected, model.TryFlip(first));
            Assert.AreEqual(FlipOutcome.Rejected, model.TryFlip(second));
        }

        [Test]
        public void ResolvePendingPair_OnMismatch_TurnsBothCardsBackDown()
        {
            MemoryMatchModel model = new MemoryMatchModel(2, 2, new Random(4));

            int first = 0;
            int second = FindDifferentIcon(model, first);

            model.TryFlip(first);
            model.TryFlip(second);

            Assert.IsFalse(model.ResolvePendingPair(), "different icons must not match");
            Assert.IsFalse(model.IsFaceUp(first));
            Assert.IsFalse(model.IsFaceUp(second));
            Assert.AreEqual(1, model.Moves, "a resolved attempt counts as one move");
            Assert.AreEqual(0, model.MatchedPairs);
        }

        [Test]
        public void PerfectPlay_AlwaysSolvesTheBoard()
        {
            for (int seed = 1; seed <= 100; seed++)
            {
                MemoryMatchModel model = new MemoryMatchModel(4, 4, new Random(seed));

                // Perfect play = bermain dengan tahu lebih dulu: ambil satu pasangan yang diketahui, balik keduanya, selesaikan.
                for (int iconId = 0; iconId < model.PairCount; iconId++)
                {
                    int first = FindUnmatchedCardWithIcon(model, iconId, -1);
                    int second = FindUnmatchedCardWithIcon(model, iconId, first);

                    Assert.GreaterOrEqual(first, 0, $"seed {seed}: pair {iconId} is missing");
                    Assert.GreaterOrEqual(second, 0, $"seed {seed}: pair {iconId} has only one card");

                    Assert.AreEqual(FlipOutcome.FirstCard, model.TryFlip(first));
                    Assert.AreEqual(FlipOutcome.PairReady, model.TryFlip(second));
                    Assert.IsTrue(model.ResolvePendingPair(), $"seed {seed}: pair {iconId} must match");
                }

                Assert.IsTrue(model.IsSolved, $"seed {seed}: the board must always be winnable");
                Assert.AreEqual(model.PairCount, model.Moves, "a perfect game needs exactly one move per pair");
            }
        }

        [Test]
        public void Restart_ResetsCountersAndClearsTheBoard()
        {
            MemoryMatchModel model = new MemoryMatchModel(2, 2, new Random(5));

            model.TryFlip(0);
            model.TryFlip(1);
            model.ResolvePendingPair();

            model.Restart();

            Assert.AreEqual(0, model.Moves);
            Assert.AreEqual(0, model.MatchedPairs);
            Assert.AreEqual(-1, model.PendingFirstCard);
            Assert.IsFalse(model.IsSolved);

            for (int card = 0; card < model.CardCount; card++)
            {
                Assert.IsFalse(model.IsFaceUp(card));
                Assert.IsFalse(model.IsMatched(card));
            }
        }

        private static int FindPair(MemoryMatchModel model, out int partner)
        {
            for (int card = 0; card < model.CardCount; card++)
            {
                for (int other = card + 1; other < model.CardCount; other++)
                {
                    if (model.IconIdOf(card) != model.IconIdOf(other)) continue;

                    partner = other;
                    return card;
                }
            }

            partner = -1;
            Assert.Fail("no pair found on a freshly shuffled board");
            return -1;
        }

        private static int FindDifferentIcon(MemoryMatchModel model, int card)
        {
            for (int other = 0; other < model.CardCount; other++)
            {
                if (model.IconIdOf(other) != model.IconIdOf(card)) return other;
            }

            Assert.Fail("board has only one icon");
            return -1;
        }

        private static int FindUnmatchedCardWithIcon(MemoryMatchModel model, int iconId, int skip)
        {
            for (int card = 0; card < model.CardCount; card++)
            {
                if (card == skip) continue;
                if (model.IsMatched(card)) continue;
                if (model.IconIdOf(card) == iconId) return card;
            }

            return -1;
        }
    }
}
