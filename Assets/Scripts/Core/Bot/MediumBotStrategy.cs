using System;
using System.Collections.Generic;
using System.Linq;

namespace CrosswordGo
{
    // Places 4–5 correct letters per turn, prioritising words closest to completion.
    public class MediumBotStrategy : BotStrategy
    {
        private readonly Random _rng;

        public MediumBotStrategy(int seed = -1)
        {
            _rng = seed < 0 ? new Random() : new Random(seed);
        }

        public override List<(int cellIndex, char letter)> ChoosePlacements(
            LevelData level, BoardState board, List<char> hand)
        {
            var matchable = FindMatchable(BuildCorrectMap(level, board), hand);
            // Sort by how many cells in the same slot are already locked (descending)
            matchable.Sort((a, b) =>
                LockedNeighbours(level, board, b.cellIndex)
                    .CompareTo(LockedNeighbours(level, board, a.cellIndex)));

            int maxPlace = Math.Min(5, matchable.Count);
            int count = _rng.Next(Math.Max(0, maxPlace - 1), maxPlace + 1);
            return matchable.GetRange(0, Math.Min(count, matchable.Count));
        }

        private static int LockedNeighbours(LevelData level, BoardState board, int cellIndex)
        {
            int row = cellIndex / level.gridWidth;
            int col = cellIndex % level.gridWidth;
            int count = 0;
            foreach (var slot in level.wordSlots)
            {
                for (int i = 0; i < slot.length; i++)
                {
                    int r = slot.direction == Direction.Across ? slot.startRow : slot.startRow + i;
                    int c = slot.direction == Direction.Across ? slot.startCol + i : slot.startCol;
                    if (r == row && c == col)
                    {
                        // Count locked cells in this slot
                        for (int j = 0; j < slot.length; j++)
                        {
                            int nr = slot.direction == Direction.Across ? slot.startRow : slot.startRow + j;
                            int nc = slot.direction == Direction.Across ? slot.startCol + j : slot.startCol;
                            if (board.lockedCells[nr * level.gridWidth + nc]) count++;
                        }
                    }
                }
            }
            return count;
        }
    }
}
