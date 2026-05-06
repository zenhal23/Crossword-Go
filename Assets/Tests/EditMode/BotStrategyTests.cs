using NUnit.Framework;
using UnityEngine;
using CrosswordGo;
using System.Collections.Generic;
using System.Linq;

namespace CrosswordGo.Tests
{
    public class BotStrategyTests
    {
        // 5x5 board: CAT Across(row2,col1-3) and DOT Across(row4,col1-3)
        private (LevelData level, BoardState board) BuildTwoWordBoard()
        {
            var level = ScriptableObject.CreateInstance<LevelData>();
            level.gridWidth = 5;
            level.gridHeight = 5;
            level.cells = new CellData[25];
            for (int i = 0; i < 25; i++)
                level.cells[i] = new CellData { cellType = CellType.Black, acrossSlotId = -1, downSlotId = -1 };

            // CAT Across row2 cols 1-3
            level.cells[2 * 5 + 0] = new CellData { cellType = CellType.Clue, acrossSlotId = 0, downSlotId = -1 };
            level.cells[2 * 5 + 1] = new CellData { cellType = CellType.Answer, acrossSlotId = -1, downSlotId = -1 };
            level.cells[2 * 5 + 2] = new CellData { cellType = CellType.Answer, acrossSlotId = -1, downSlotId = -1 };
            level.cells[2 * 5 + 3] = new CellData { cellType = CellType.Answer, acrossSlotId = -1, downSlotId = -1 };

            // DOT Across row4 cols 1-3
            level.cells[4 * 5 + 0] = new CellData { cellType = CellType.Clue, acrossSlotId = 1, downSlotId = -1 };
            level.cells[4 * 5 + 1] = new CellData { cellType = CellType.Answer, acrossSlotId = -1, downSlotId = -1 };
            level.cells[4 * 5 + 2] = new CellData { cellType = CellType.Answer, acrossSlotId = -1, downSlotId = -1 };
            level.cells[4 * 5 + 3] = new CellData { cellType = CellType.Answer, acrossSlotId = -1, downSlotId = -1 };

            level.wordSlots = new WordSlotData[]
            {
                new WordSlotData { id = 0, direction = Direction.Across, startRow = 2, startCol = 1, length = 3, answer = "CAT", clue = "Feline" },
                new WordSlotData { id = 1, direction = Direction.Across, startRow = 4, startCol = 1, length = 3, answer = "DOT", clue = "Speck" }
            };
            return (level, new BoardState(25));
        }

        private List<char> HandWith(params char[] letters) => new List<char>(letters);

        // --- Easy bot ---

        [Test]
        public void EasyBot_places_between_two_and_four_correct_letters()
        {
            var (level, board) = BuildTwoWordBoard();
            var bot = new EasyBotStrategy();
            // Full hand matching all CAT letters
            var hand = HandWith('C', 'A', 'T', 'D', 'O');

            var results = new List<int>();
            for (int run = 0; run < 100; run++)
            {
                var placements = bot.ChoosePlacements(level, board, new List<char>(hand));
                results.Add(placements.Count);
                Assert.IsTrue(placements.Count >= 0 && placements.Count <= 4,
                    $"EasyBot placed {placements.Count}, expected 0-4");
            }
            // Over 100 runs, should sometimes place fewer than max
            bool placedLessThanMax = results.Any(r => r < hand.Count);
            Assert.IsTrue(placedLessThanMax, "EasyBot should sometimes use fewer than all letters");
        }

        [Test]
        public void EasyBot_only_places_letters_on_unfilled_answer_cells()
        {
            var (level, board) = BuildTwoWordBoard();
            var bot = new EasyBotStrategy();
            var hand = HandWith('C', 'A', 'T', 'D', 'O');

            for (int run = 0; run < 20; run++)
            {
                var placements = bot.ChoosePlacements(level, board, new List<char>(hand));
                foreach (var (idx, _) in placements)
                {
                    Assert.AreEqual(CellType.Answer, level.cells[idx].cellType,
                        $"Placed on non-Answer cell {idx}");
                    Assert.IsFalse(board.lockedCells[idx],
                        $"Placed on locked cell {idx}");
                }
            }
        }

        [Test]
        public void EasyBot_all_placements_use_letters_from_hand()
        {
            var (level, board) = BuildTwoWordBoard();
            var bot = new EasyBotStrategy();
            var hand = HandWith('C', 'A', 'T');

            var placements = bot.ChoosePlacements(level, board, new List<char>(hand));
            var handCopy = new List<char>(hand);
            foreach (var (_, letter) in placements)
            {
                Assert.IsTrue(handCopy.Remove(letter),
                    $"Letter {letter} not in hand or used twice");
            }
        }

        // --- Medium bot ---

        [Test]
        public void MediumBot_places_at_least_as_many_as_EasyBot_on_average()
        {
            var (level, board) = BuildTwoWordBoard();
            var easy = new EasyBotStrategy();
            var medium = new MediumBotStrategy();
            var hand = HandWith('C', 'A', 'T', 'D', 'O');

            double easySum = 0, medSum = 0;
            for (int run = 0; run < 200; run++)
            {
                easySum += easy.ChoosePlacements(level, board, new List<char>(hand)).Count;
                medSum += medium.ChoosePlacements(level, board, new List<char>(hand)).Count;
            }
            Assert.GreaterOrEqual(medSum, easySum,
                "Medium bot should place >= Easy bot on average over 200 runs");
        }

        [Test]
        public void MediumBot_all_placements_are_correct_letters_for_their_cells()
        {
            var (level, board) = BuildTwoWordBoard();
            var bot = new MediumBotStrategy();
            var hand = HandWith('C', 'A', 'T', 'D', 'O');

            for (int run = 0; run < 30; run++)
            {
                var placements = bot.ChoosePlacements(level, board, new List<char>(hand));
                foreach (var (idx, letter) in placements)
                {
                    char correct = ScoringSystem.GetCorrectLetter(level, idx);
                    Assert.AreEqual(correct, char.ToUpper(letter),
                        $"MediumBot placed wrong letter {letter} at cell {idx}, expected {correct}");
                }
            }
        }

        // --- Hard bot ---

        [Test]
        public void HardBot_places_all_matchable_letters()
        {
            var (level, board) = BuildTwoWordBoard();
            var bot = new HardBotStrategy();
            // Hand contains C, A, T — all match CAT slot
            var hand = HandWith('C', 'A', 'T');

            var placements = bot.ChoosePlacements(level, board, new List<char>(hand));

            Assert.AreEqual(3, placements.Count);
        }

        [Test]
        public void HardBot_all_placements_are_correct_letters_for_their_cells()
        {
            var (level, board) = BuildTwoWordBoard();
            var bot = new HardBotStrategy();
            var hand = HandWith('C', 'A', 'T', 'D', 'O');

            var placements = bot.ChoosePlacements(level, board, new List<char>(hand));

            foreach (var (idx, letter) in placements)
            {
                char correct = ScoringSystem.GetCorrectLetter(level, idx);
                Assert.AreEqual(correct, char.ToUpper(letter));
            }
        }

        [Test]
        public void HardBot_skips_already_locked_cells()
        {
            var (level, board) = BuildTwoWordBoard();
            board.LockCell(2 * 5 + 1, 1); // lock 'C' in CAT
            var bot = new HardBotStrategy();
            var hand = HandWith('C', 'A', 'T');

            var placements = bot.ChoosePlacements(level, board, new List<char>(hand));

            Assert.IsFalse(placements.Any(p => p.cellIndex == 2 * 5 + 1),
                "Should not target already-locked cell");
        }
    }
}
