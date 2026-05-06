using System.Collections.Generic;

namespace CrosswordGo
{
    public abstract class BotStrategy
    {
        // Returns a list of (cellIndex, letter) pairs to place this turn.
        // All returned letters must come from the provided hand.
        // Implementations must not modify the hand list.
        public abstract List<(int cellIndex, char letter)> ChoosePlacements(
            LevelData level, BoardState board, List<char> hand);

        // Maps every unfilled answer cell index to its correct letter.
        protected static Dictionary<int, char> BuildCorrectMap(LevelData level, BoardState board)
        {
            var map = new Dictionary<int, char>();
            var seen = new HashSet<int>();
            foreach (var slot in level.wordSlots)
            {
                for (int i = 0; i < slot.length; i++)
                {
                    int r = slot.direction == Direction.Across ? slot.startRow : slot.startRow + i;
                    int c = slot.direction == Direction.Across ? slot.startCol + i : slot.startCol;
                    int idx = r * level.gridWidth + c;
                    if (seen.Add(idx) && !board.lockedCells[idx])
                        map[idx] = char.ToUpper(slot.answer[i]);
                }
            }
            return map;
        }

        // Returns every (cellIndex, letter) pair where the hand contains the correct letter.
        // Consumes each hand letter at most once.
        protected static List<(int cellIndex, char letter)> FindMatchable(
            Dictionary<int, char> correctMap, List<char> hand)
        {
            var result = new List<(int, char)>();
            var remaining = new List<char>(hand);
            foreach (var (idx, correct) in correctMap)
            {
                int hi = remaining.IndexOf(correct);
                if (hi >= 0)
                {
                    result.Add((idx, correct));
                    remaining.RemoveAt(hi);
                }
            }
            return result;
        }

        protected static void Shuffle<T>(List<T> list, System.Random rng)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}
