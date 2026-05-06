using System.Collections.Generic;
using System.Linq;

namespace CrosswordGo
{
    // Places all matchable correct letters per turn, targeting word completions first.
    public class HardBotStrategy : BotStrategy
    {
        public override List<(int cellIndex, char letter)> ChoosePlacements(
            LevelData level, BoardState board, List<char> hand)
        {
            var matchable = FindMatchable(BuildCorrectMap(level, board), hand);
            // Sort: cells in slots nearest to completion come first
            matchable.Sort((a, b) =>
                RemainingInSlot(level, board, b.cellIndex)
                    .CompareTo(RemainingInSlot(level, board, a.cellIndex)));
            return matchable;
        }

        // Returns the fewest remaining-unlocked cells across all slots containing this cell.
        private static int RemainingInSlot(LevelData level, BoardState board, int cellIndex)
        {
            int row = cellIndex / level.gridWidth;
            int col = cellIndex % level.gridWidth;
            int best = int.MaxValue;
            foreach (var slot in level.wordSlots)
            {
                bool contains = false;
                for (int i = 0; i < slot.length; i++)
                {
                    int r = slot.direction == Direction.Across ? slot.startRow : slot.startRow + i;
                    int c = slot.direction == Direction.Across ? slot.startCol + i : slot.startCol;
                    if (r == row && c == col) { contains = true; break; }
                }
                if (!contains) continue;

                int remaining = 0;
                for (int i = 0; i < slot.length; i++)
                {
                    int r = slot.direction == Direction.Across ? slot.startRow : slot.startRow + i;
                    int c = slot.direction == Direction.Across ? slot.startCol + i : slot.startCol;
                    if (!board.lockedCells[r * level.gridWidth + c]) remaining++;
                }
                if (remaining < best) best = remaining;
            }
            return best == int.MaxValue ? 0 : best;
        }
    }
}
