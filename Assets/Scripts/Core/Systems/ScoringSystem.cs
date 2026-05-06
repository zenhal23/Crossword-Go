using System.Collections.Generic;

namespace CrosswordGo
{
    public class ScoringSystem
    {
        public const int PointsCorrectLetter = 1;
        public const int PointsWrongLetter = -1;
        public const int PointsWordComplete = 5;
        public const int PointsAllLettersUsed = 10;
        public const int PointsBoardComplete = 50;

        public struct TurnResult
        {
            public int correctCount;
            public int wrongCount;
            public List<int> newlyCompletedSlotIds;
            public bool usedAllLetters;
            public bool boardComplete;
            public List<char> returnedLetters;

            public int newlyCompletedSlots => newlyCompletedSlotIds?.Count ?? 0;

            public int scoreDelta =>
                correctCount * PointsCorrectLetter +
                wrongCount * PointsWrongLetter +
                newlyCompletedSlots * PointsWordComplete +
                (usedAllLetters ? PointsAllLettersUsed : 0) +
                (boardComplete ? PointsBoardComplete : 0);
        }

        public TurnResult Evaluate(
            LevelData level,
            BoardState board,
            List<(int cellIndex, char letter)> placements,
            int playerOwner,
            int handSize = 0)
        {
            var result = new TurnResult
            {
                returnedLetters = new List<char>(),
                newlyCompletedSlotIds = new List<int>()
            };

            foreach (var (idx, letter) in placements)
            {
                if (board.lockedCells[idx]) continue; // already locked, skip silently

                char correct = GetCorrectLetter(level, idx);
                if (correct != '\0' && char.ToUpper(letter) == correct)
                {
                    board.LockCell(idx, playerOwner);
                    result.correctCount++;
                }
                else
                {
                    result.returnedLetters.Add(letter);
                    result.wrongCount++;
                }
            }

            foreach (var slot in level.wordSlots)
            {
                if (board.completedSlotIds.Contains(slot.id)) continue;
                if (IsSlotComplete(level, board, slot))
                {
                    board.completedSlotIds.Add(slot.id);
                    result.newlyCompletedSlotIds.Add(slot.id);
                }
            }

            result.usedAllLetters = handSize > 0 &&
                                    placements.Count == handSize &&
                                    result.correctCount == handSize;
            result.boardComplete = IsBoardComplete(level, board);

            return result;
        }

        public static char GetCorrectLetter(LevelData level, int cellIndex)
        {
            int row = cellIndex / level.gridWidth;
            int col = cellIndex % level.gridWidth;
            foreach (var slot in level.wordSlots)
            {
                for (int i = 0; i < slot.length; i++)
                {
                    int r = slot.direction == Direction.Across ? slot.startRow : slot.startRow + i;
                    int c = slot.direction == Direction.Across ? slot.startCol + i : slot.startCol;
                    if (r == row && c == col)
                        return char.ToUpper(slot.answer[i]);
                }
            }
            return '\0';
        }

        private static bool IsSlotComplete(LevelData level, BoardState board, WordSlotData slot)
        {
            for (int i = 0; i < slot.length; i++)
            {
                int r = slot.direction == Direction.Across ? slot.startRow : slot.startRow + i;
                int c = slot.direction == Direction.Across ? slot.startCol + i : slot.startCol;
                if (!board.lockedCells[r * level.gridWidth + c]) return false;
            }
            return true;
        }

        private static bool IsBoardComplete(LevelData level, BoardState board)
        {
            for (int i = 0; i < level.cells.Length; i++)
                if (level.cells[i].cellType == CellType.Answer && !board.lockedCells[i])
                    return false;
            return true;
        }
    }
}
