using NUnit.Framework;
using UnityEngine;
using CrosswordGo;
using System.Collections.Generic;

namespace CrosswordGo.Tests
{
    public class ScoringSystemTests
    {
        // 5x5 board, one Across slot: CAT at row2 cols 1-3
        private (LevelData level, BoardState board) BuildCatBoard()
        {
            var level = ScriptableObject.CreateInstance<LevelData>();
            level.gridWidth = 5;
            level.gridHeight = 5;
            level.cells = new CellData[25];
            for (int i = 0; i < 25; i++)
                level.cells[i] = new CellData { cellType = CellType.Black, acrossSlotId = -1, downSlotId = -1 };
            level.cells[2 * 5 + 0] = new CellData { cellType = CellType.Clue, acrossSlotId = 0, downSlotId = -1 };
            level.cells[2 * 5 + 1] = new CellData { cellType = CellType.Answer, acrossSlotId = -1, downSlotId = -1 };
            level.cells[2 * 5 + 2] = new CellData { cellType = CellType.Answer, acrossSlotId = -1, downSlotId = -1 };
            level.cells[2 * 5 + 3] = new CellData { cellType = CellType.Answer, acrossSlotId = -1, downSlotId = -1 };
            level.wordSlots = new WordSlotData[]
            {
                new WordSlotData
                {
                    id = 0, direction = Direction.Across,
                    clueRow = 2, clueCol = 0,
                    startRow = 2, startCol = 1,
                    length = 3, answer = "CAT", clue = "Feline"
                }
            };
            return (level, new BoardState(25));
        }

        // 5x5 board, two Across slots: CAT at row1 cols 1-3, DOT at row3 cols 1-3
        private (LevelData level, BoardState board) BuildTwoWordBoard()
        {
            var level = ScriptableObject.CreateInstance<LevelData>();
            level.gridWidth = 5;
            level.gridHeight = 5;
            level.cells = new CellData[25];
            for (int i = 0; i < 25; i++)
                level.cells[i] = new CellData { cellType = CellType.Black, acrossSlotId = -1, downSlotId = -1 };
            level.cells[1 * 5 + 0] = new CellData { cellType = CellType.Clue, acrossSlotId = 0, downSlotId = -1 };
            level.cells[1 * 5 + 1] = new CellData { cellType = CellType.Answer, acrossSlotId = -1, downSlotId = -1 };
            level.cells[1 * 5 + 2] = new CellData { cellType = CellType.Answer, acrossSlotId = -1, downSlotId = -1 };
            level.cells[1 * 5 + 3] = new CellData { cellType = CellType.Answer, acrossSlotId = -1, downSlotId = -1 };
            level.cells[3 * 5 + 0] = new CellData { cellType = CellType.Clue, acrossSlotId = 1, downSlotId = -1 };
            level.cells[3 * 5 + 1] = new CellData { cellType = CellType.Answer, acrossSlotId = -1, downSlotId = -1 };
            level.cells[3 * 5 + 2] = new CellData { cellType = CellType.Answer, acrossSlotId = -1, downSlotId = -1 };
            level.cells[3 * 5 + 3] = new CellData { cellType = CellType.Answer, acrossSlotId = -1, downSlotId = -1 };
            level.wordSlots = new WordSlotData[]
            {
                new WordSlotData
                {
                    id = 0, direction = Direction.Across,
                    clueRow = 1, clueCol = 0,
                    startRow = 1, startCol = 1,
                    length = 3, answer = "CAT", clue = "Feline"
                },
                new WordSlotData
                {
                    id = 1, direction = Direction.Across,
                    clueRow = 3, clueCol = 0,
                    startRow = 3, startCol = 1,
                    length = 3, answer = "DOT", clue = "Small spot"
                }
            };
            return (level, new BoardState(25));
        }

        [Test]
        public void Evaluate_correct_placement_locks_cell_and_returns_positive_score()
        {
            var (level, board) = BuildCatBoard();
            var scoring = new ScoringSystem();
            int idx = 2 * 5 + 1; // 'C' cell

            // Player has 5 letters but only places 1 — no efficiency bonus
            var placements = new List<(int, char)> { (idx, 'C') };
            var result = scoring.Evaluate(level, board, placements, playerOwner: 1, handSize: 5);

            Assert.AreEqual(1, result.correctCount);
            Assert.IsTrue(board.lockedCells[idx]);
            Assert.AreEqual(1, result.scoreDelta); // +1 per correct letter
        }

        [Test]
        public void Evaluate_incorrect_placement_returns_minus_one_and_letter_in_wrong_list()
        {
            var (level, board) = BuildCatBoard();
            var scoring = new ScoringSystem();
            int idx = 2 * 5 + 1; // correct is 'C', placing 'Z'

            var placements = new List<(int, char)> { (idx, 'Z') };
            var result = scoring.Evaluate(level, board, placements, playerOwner: 1);

            Assert.AreEqual(1, result.wrongCount);
            Assert.IsFalse(board.lockedCells[idx]);
            Assert.Contains('Z', result.returnedLetters);
            Assert.AreEqual(-1, result.scoreDelta); // -1 per wrong letter
        }

        [Test]
        public void Evaluate_completing_a_word_adds_word_bonus()
        {
            // Two-word board: completing CAT leaves DOT empty, so board is not done
            var (level, board) = BuildTwoWordBoard();
            var scoring = new ScoringSystem();

            // Player has 5 letters, places 3 — not all letters used, no efficiency bonus
            var placements = new List<(int, char)>
            {
                (1 * 5 + 1, 'C'),
                (1 * 5 + 2, 'A'),
                (1 * 5 + 3, 'T')
            };
            var result = scoring.Evaluate(level, board, placements, playerOwner: 1, handSize: 5);

            // +3 letters + +5 word = 8
            Assert.AreEqual(1, result.newlyCompletedSlots);
            Assert.AreEqual(8, result.scoreDelta);
        }

        [Test]
        public void Evaluate_all_letters_used_correctly_adds_efficiency_bonus()
        {
            // Two-word board: completing CAT leaves DOT empty, isolating efficiency bonus
            var (level, board) = BuildTwoWordBoard();
            var scoring = new ScoringSystem();

            // Hand of 3, all 3 placed correctly — efficiency bonus triggers
            var placements = new List<(int, char)>
            {
                (1 * 5 + 1, 'C'),
                (1 * 5 + 2, 'A'),
                (1 * 5 + 3, 'T')
            };
            var result = scoring.Evaluate(level, board, placements, playerOwner: 1, handSize: 3);

            Assert.IsTrue(result.usedAllLetters);
            // +3 letters + +5 word + +10 efficiency = 18
            Assert.AreEqual(18, result.scoreDelta);
        }

        [Test]
        public void Evaluate_efficiency_bonus_not_awarded_if_any_letter_wrong()
        {
            var (level, board) = BuildCatBoard();
            var scoring = new ScoringSystem();

            // Place C correctly, A correctly, but Z instead of T
            var placements = new List<(int, char)>
            {
                (2 * 5 + 1, 'C'),
                (2 * 5 + 2, 'A'),
                (2 * 5 + 3, 'Z')
            };
            var result = scoring.Evaluate(level, board, placements, playerOwner: 1);

            Assert.IsFalse(result.usedAllLetters);
        }

        [Test]
        public void Evaluate_word_not_counted_twice_if_completed_across_two_turns()
        {
            var (level, board) = BuildCatBoard();
            var scoring = new ScoringSystem();

            // Turn 1: place C and A
            var turn1 = new List<(int, char)> { (2 * 5 + 1, 'C'), (2 * 5 + 2, 'A') };
            scoring.Evaluate(level, board, turn1, playerOwner: 1);

            // Turn 2: place T to complete the word
            var turn2 = new List<(int, char)> { (2 * 5 + 3, 'T') };
            var result2 = scoring.Evaluate(level, board, turn2, playerOwner: 1);

            Assert.AreEqual(1, result2.newlyCompletedSlots);

            // Turn 3: no new completions possible
            var turn3 = new List<(int, char)>();
            var result3 = scoring.Evaluate(level, board, turn3, playerOwner: 1);

            Assert.AreEqual(0, result3.newlyCompletedSlots);
        }

        [Test]
        public void Evaluate_board_complete_flag_set_when_all_answer_cells_locked()
        {
            var (level, board) = BuildCatBoard();
            var scoring = new ScoringSystem();

            var placements = new List<(int, char)>
            {
                (2 * 5 + 1, 'C'),
                (2 * 5 + 2, 'A'),
                (2 * 5 + 3, 'T')
            };
            var result = scoring.Evaluate(level, board, placements, playerOwner: 1);

            Assert.IsTrue(result.boardComplete);
        }

        [Test]
        public void Evaluate_board_complete_bonus_included_in_score_delta()
        {
            var (level, board) = BuildCatBoard();
            var scoring = new ScoringSystem();

            // Hand of 3, place all 3 — completes word + board + efficiency bonus
            var placements = new List<(int, char)>
            {
                (2 * 5 + 1, 'C'),
                (2 * 5 + 2, 'A'),
                (2 * 5 + 3, 'T')
            };
            var result = scoring.Evaluate(level, board, placements, playerOwner: 1, handSize: 3);

            // +3 letters + +5 word + +10 efficiency + +50 board = 68
            Assert.AreEqual(68, result.scoreDelta);
        }

        [Test]
        public void Evaluate_locked_cells_cannot_be_overwritten()
        {
            var (level, board) = BuildCatBoard();
            board.LockCell(2 * 5 + 1, 2); // pre-locked by bot
            var scoring = new ScoringSystem();

            var placements = new List<(int, char)> { (2 * 5 + 1, 'C') };
            var result = scoring.Evaluate(level, board, placements, playerOwner: 1);

            Assert.AreEqual(0, result.correctCount);
            Assert.AreEqual(0, result.wrongCount);
            Assert.AreEqual(2, board.cellOwner[2 * 5 + 1]); // still owned by bot
        }
    }
}
