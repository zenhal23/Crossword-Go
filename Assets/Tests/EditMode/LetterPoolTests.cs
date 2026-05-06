using NUnit.Framework;
using UnityEngine;
using CrosswordGo;
using System.Collections.Generic;

namespace CrosswordGo.Tests
{
    public class LetterPoolTests
    {
        // Build a minimal 5x5 level with one Across slot: CAT at row 2, cols 1-3
        private LevelData BuildCatLevel()
        {
            var level = ScriptableObject.CreateInstance<LevelData>();
            level.gridWidth = 5;
            level.gridHeight = 5;
            level.cells = new CellData[25];
            for (int i = 0; i < 25; i++)
                level.cells[i] = new CellData { cellType = CellType.Black, acrossSlotId = -1, downSlotId = -1 };

            // Clue cell at (2,0), answer cells at (2,1), (2,2), (2,3)
            level.cells[2 * 5 + 0] = new CellData { cellType = CellType.Clue, acrossSlotId = 0, downSlotId = -1 };
            level.cells[2 * 5 + 1] = new CellData { cellType = CellType.Answer, acrossSlotId = -1, downSlotId = -1 };
            level.cells[2 * 5 + 2] = new CellData { cellType = CellType.Answer, acrossSlotId = -1, downSlotId = -1 };
            level.cells[2 * 5 + 3] = new CellData { cellType = CellType.Answer, acrossSlotId = -1, downSlotId = -1 };

            level.wordSlots = new WordSlotData[]
            {
                new WordSlotData
                {
                    id = 0,
                    direction = Direction.Across,
                    clueRow = 2, clueCol = 0,
                    startRow = 2, startCol = 1,
                    length = 3,
                    answer = "CAT",
                    clue = "Feline pet"
                }
            };
            return level;
        }

        // A level with two slots sharing an intersection: CAT (Across) and ACE (Down) sharing 'A'
        // CAT: row 2, cols 1-3  →  C(2,1), A(2,2), T(2,3)
        // ACE: rows 0-2, col 2  →  A(0,2), C(1,2), E(2,2)  — 'A'@(2,2) shared
        // Wait: CAT has A at col 2 row 2; ACE has E at row 2 col 2 → conflict.
        // Use: CAT Across (2,1): C,A,T and ARC Down (0,2): A,R,C → C@(2,2) shared ✓
        private LevelData BuildIntersectionLevel()
        {
            var level = ScriptableObject.CreateInstance<LevelData>();
            level.gridWidth = 5;
            level.gridHeight = 5;
            level.cells = new CellData[25];
            for (int i = 0; i < 25; i++)
                level.cells[i] = new CellData { cellType = CellType.Black, acrossSlotId = -1, downSlotId = -1 };

            // CAT Across: clue@(2,0), answers (2,1),(2,2),(2,3)
            level.cells[2 * 5 + 0] = new CellData { cellType = CellType.Clue, acrossSlotId = 0, downSlotId = -1 };
            level.cells[2 * 5 + 1] = new CellData { cellType = CellType.Answer, acrossSlotId = -1, downSlotId = -1 };
            level.cells[2 * 5 + 2] = new CellData { cellType = CellType.Answer, acrossSlotId = -1, downSlotId = -1 };
            level.cells[2 * 5 + 3] = new CellData { cellType = CellType.Answer, acrossSlotId = -1, downSlotId = -1 };

            // ARC Down: clue@(row -1 would be out of bounds) — place clue at col header
            // ARC Down: clue@(0,2) is a clue cell, answers (1,2),(2,2),(3,2) — but (2,2) is shared 'C'
            // ARC answer = A,R,C  → A@(1,2), R... wait let me recalculate
            // ARC Down startRow=0, startCol=2, length=3: cells (0,2),(1,2),(2,2) = A,R,C
            // (2,2) in CAT is 'C', (2,2) in ARC is 'C' ✓
            level.cells[0 * 5 + 2] = new CellData { cellType = CellType.Answer, acrossSlotId = -1, downSlotId = -1 };
            level.cells[1 * 5 + 2] = new CellData { cellType = CellType.Answer, acrossSlotId = -1, downSlotId = -1 };
            // (2,2) already Answer

            level.wordSlots = new WordSlotData[]
            {
                new WordSlotData
                {
                    id = 0, direction = Direction.Across,
                    clueRow = 2, clueCol = 0,
                    startRow = 2, startCol = 1,
                    length = 3, answer = "CAT", clue = "Feline"
                },
                new WordSlotData
                {
                    id = 1, direction = Direction.Down,
                    clueRow = -1, clueCol = 2,   // no visible clue cell for test purposes
                    startRow = 0, startCol = 2,
                    length = 3, answer = "ARC", clue = "Curved line"
                }
            };
            return level;
        }

        [Test]
        public void Initialize_fills_pool_with_one_letter_per_answer_cell()
        {
            var level = BuildCatLevel();
            var board = new BoardState(25);
            var pool = new LetterPool();

            pool.Initialize(level, board);

            Assert.AreEqual(3, pool.Remaining);
        }

        [Test]
        public void Initialize_deduplicates_intersection_cells()
        {
            // CAT (3 cells) + ARC (3 cells) share 1 cell → 5 unique answer cells
            var level = BuildIntersectionLevel();
            var board = new BoardState(25);
            var pool = new LetterPool();

            pool.Initialize(level, board);

            Assert.AreEqual(5, pool.Remaining);
        }

        [Test]
        public void Initialize_skips_already_locked_cells()
        {
            var level = BuildCatLevel();
            var board = new BoardState(25);
            board.LockCell(2 * 5 + 1, 1); // pre-lock the 'C' cell
            var pool = new LetterPool();

            pool.Initialize(level, board);

            Assert.AreEqual(2, pool.Remaining);
        }

        [Test]
        public void Deal_returns_requested_count()
        {
            var level = BuildCatLevel();
            var board = new BoardState(25);
            var pool = new LetterPool();
            pool.Initialize(level, board);

            var hand = pool.Deal(2);

            Assert.AreEqual(2, hand.Count);
        }

        [Test]
        public void Deal_reduces_remaining_count()
        {
            var level = BuildCatLevel();
            var board = new BoardState(25);
            var pool = new LetterPool();
            pool.Initialize(level, board);

            pool.Deal(2);

            Assert.AreEqual(1, pool.Remaining);
        }

        [Test]
        public void Deal_only_returns_letters_present_in_answers()
        {
            var level = BuildCatLevel();
            var board = new BoardState(25);
            var pool = new LetterPool();
            pool.Initialize(level, board);

            var hand = pool.Deal(3);

            var expected = new HashSet<char> { 'C', 'A', 'T' };
            foreach (var letter in hand)
                Assert.IsTrue(expected.Contains(letter), $"Unexpected letter: {letter}");
        }

        [Test]
        public void Deal_caps_at_remaining_when_requesting_more_than_available()
        {
            var level = BuildCatLevel();
            var board = new BoardState(25);
            var pool = new LetterPool();
            pool.Initialize(level, board);

            var hand = pool.Deal(99);

            Assert.AreEqual(3, hand.Count);
        }

        [Test]
        public void OnPoolEmpty_fires_when_pool_is_exhausted()
        {
            var level = BuildCatLevel();
            var board = new BoardState(25);
            var pool = new LetterPool();
            pool.Initialize(level, board);

            bool fired = false;
            pool.OnPoolEmpty += () => fired = true;
            pool.Deal(3);

            Assert.IsTrue(fired);
        }

        [Test]
        public void OnPoolEmpty_does_not_fire_when_letters_remain()
        {
            var level = BuildCatLevel();
            var board = new BoardState(25);
            var pool = new LetterPool();
            pool.Initialize(level, board);

            bool fired = false;
            pool.OnPoolEmpty += () => fired = true;
            pool.Deal(2);

            Assert.IsFalse(fired);
        }
    }
}
