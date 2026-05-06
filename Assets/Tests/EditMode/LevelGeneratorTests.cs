using System.Linq;
using NUnit.Framework;

namespace CrosswordGo
{
    public class LevelGeneratorTests
    {
        private static void AssertValidLevel(System.Collections.Generic.List<SlotDraft> slots, string label)
        {
            Assert.IsNotNull(slots, $"{label}: slots is null");
            Assert.IsNotEmpty(slots, $"{label}: no slots generated");

            var answerErrors = LevelBuilder.GetAnswerErrors(slots, LevelBuilder.GridWidth, LevelBuilder.GridHeight);
            Assert.IsEmpty(answerErrors, $"{label} answer errors: {string.Join(", ", answerErrors)}");

            var intersectionErrors = LevelBuilder.GetIntersectionErrors(slots);
            Assert.IsEmpty(intersectionErrors, $"{label} intersection errors: {string.Join(", ", intersectionErrors)}");
        }

        [Test]
        public void generate_easy_produces_valid_level()
        {
            var slots = LevelGenerator.Generate(Difficulty.Easy, new System.Random(42));
            AssertValidLevel(slots, "Easy");
            // Easy has no Down slots
            Assert.IsTrue(slots.All(s => s.direction == Direction.Across), "Easy should have only Across slots");
            Assert.AreEqual(8, slots.Count);
        }

        [Test]
        public void generate_medium_produces_valid_level()
        {
            var slots = LevelGenerator.Generate(Difficulty.Medium, new System.Random(42));
            AssertValidLevel(slots, "Medium");
            int downCount = slots.Count(s => s.direction == Direction.Down);
            Assert.GreaterOrEqual(downCount, 1, "Medium should have at least 1 Down slot");
        }

        [Test]
        public void generate_hard_produces_valid_level()
        {
            var slots = LevelGenerator.Generate(Difficulty.Hard, new System.Random(42));
            AssertValidLevel(slots, "Hard");
            int downCount = slots.Count(s => s.direction == Direction.Down);
            Assert.GreaterOrEqual(downCount, 3, "Hard should have at least 3 Down slots");
        }

        [Test]
        public void generate_hard_different_seeds_produce_different_levels()
        {
            var a = LevelGenerator.Generate(Difficulty.Hard, new System.Random(1));
            var b = LevelGenerator.Generate(Difficulty.Hard, new System.Random(2));
            // Very unlikely to be identical
            bool identical = a.Count == b.Count &&
                System.Linq.Enumerable.Zip(a, b, (x, y) => x.answer == y.answer).All(eq => eq);
            Assert.IsFalse(identical, "Two different seeds should produce different levels");
        }

        [Test]
        public void generate_hard_never_repeats_across_words()
        {
            var slots = LevelGenerator.Generate(Difficulty.Hard, new System.Random(7));
            var acrossAnswers = slots.Where(s => s.direction == Direction.Across).Select(s => s.answer).ToList();
            var unique = acrossAnswers.Distinct().ToList();
            Assert.AreEqual(acrossAnswers.Count, unique.Count, "Across answers should be unique");
        }
    }
}
