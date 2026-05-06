using System;
using System.Collections.Generic;

namespace CrosswordGo
{
    // Places 0–4 correct letters per turn chosen at random.
    // Simulates a weak opponent that misses many opportunities.
    public class EasyBotStrategy : BotStrategy
    {
        private readonly Random _rng;

        public EasyBotStrategy(int seed = -1)
        {
            _rng = seed < 0 ? new Random() : new Random(seed);
        }

        public override List<(int cellIndex, char letter)> ChoosePlacements(
            LevelData level, BoardState board, List<char> hand)
        {
            var matchable = FindMatchable(BuildCorrectMap(level, board), hand);
            Shuffle(matchable, _rng);
            int maxPlace = Math.Min(4, matchable.Count);
            int count = _rng.Next(0, maxPlace + 1);
            return matchable.GetRange(0, count);
        }
    }
}
