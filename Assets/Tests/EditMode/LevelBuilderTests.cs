using System.Collections.Generic;
using NUnit.Framework;

namespace CrosswordGo
{
    public class LevelBuilderTests
    {
        // clueRow/clueCol: position of the clue cell.
        // Across word occupies row=clueRow, cols clueCol+1..clueCol+answer.Length
        // Down word occupies col=clueCol, rows clueRow+1..clueRow+answer.Length

        private static SlotDraft Across(int clueRow, int clueCol, string answer) =>
            new SlotDraft { direction = Direction.Across, clueRow = clueRow, clueCol = clueCol, answer = answer, clue = "test" };

        private static SlotDraft Down(int clueRow, int clueCol, string answer) =>
            new SlotDraft { direction = Direction.Down, clueRow = clueRow, clueCol = clueCol, answer = answer, clue = "test" };

        // ── BuildCells ──────────────────────────────────────────────────────

        [Test]
        public void build_cells_all_answer_when_no_slots()
        {
            var cells = LevelBuilder.BuildCells(new List<SlotDraft>(), LevelBuilder.GridWidth, LevelBuilder.GridHeight);
            foreach (var cell in cells)
            {
                Assert.AreEqual(CellType.Answer, cell.cellType);
                Assert.AreEqual(-1, cell.acrossSlotId);
                Assert.AreEqual(-1, cell.downSlotId);
            }
        }

        [Test]
        public void build_cells_marks_across_clue_cell()
        {
            var slots = new List<SlotDraft> { Across(clueRow: 4, clueCol: 0, "CASTLE") };
            var cells = LevelBuilder.BuildCells(slots, LevelBuilder.GridWidth, LevelBuilder.GridHeight);
            int idx = 4 * LevelBuilder.GridWidth + 0;
            Assert.AreEqual(CellType.Clue, cells[idx].cellType);
            Assert.AreEqual(0, cells[idx].acrossSlotId);
            Assert.AreEqual(-1, cells[idx].downSlotId);
        }

        [Test]
        public void build_cells_marks_down_clue_cell()
        {
            var slots = new List<SlotDraft> { Down(clueRow: 0, clueCol: 3, "CARDINAL") };
            var cells = LevelBuilder.BuildCells(slots, LevelBuilder.GridWidth, LevelBuilder.GridHeight);
            int idx = 0 * LevelBuilder.GridWidth + 3;
            Assert.AreEqual(CellType.Clue, cells[idx].cellType);
            Assert.AreEqual(-1, cells[idx].acrossSlotId);
            Assert.AreEqual(0, cells[idx].downSlotId);
        }

        [Test]
        public void build_cells_dual_clue_cell_has_both_ids()
        {
            var slots = new List<SlotDraft>
            {
                Across(clueRow: 2, clueCol: 1, "CASTLE"),
                Down(clueRow: 2, clueCol: 1, "CARDINAL")
            };
            var cells = LevelBuilder.BuildCells(slots, LevelBuilder.GridWidth, LevelBuilder.GridHeight);
            int idx = 2 * LevelBuilder.GridWidth + 1;
            Assert.AreEqual(CellType.Clue, cells[idx].cellType);
            Assert.AreEqual(0, cells[idx].acrossSlotId); // Across sorted first
            Assert.AreEqual(1, cells[idx].downSlotId);
        }

        [Test]
        public void build_cells_down_ids_offset_by_across_count()
        {
            var slots = new List<SlotDraft>
            {
                Across(1, 0, "CASTLE"),
                Across(2, 0, "BRIGHT"),
                Down(0, 2, "CARDINAL")
            };
            var cells = LevelBuilder.BuildCells(slots, LevelBuilder.GridWidth, LevelBuilder.GridHeight);
            // 2 Across slots → Down gets ID 2
            int idx = 0 * LevelBuilder.GridWidth + 2;
            Assert.AreEqual(2, cells[idx].downSlotId);
        }

        [Test]
        public void build_cells_across_ids_sorted_by_row()
        {
            var slots = new List<SlotDraft>
            {
                Across(5, 0, "FINGER"),
                Across(2, 0, "BRIGHT")
            };
            var cells = LevelBuilder.BuildCells(slots, LevelBuilder.GridWidth, LevelBuilder.GridHeight);
            Assert.AreEqual(0, cells[2 * LevelBuilder.GridWidth + 0].acrossSlotId, "row 2 → ID 0");
            Assert.AreEqual(1, cells[5 * LevelBuilder.GridWidth + 0].acrossSlotId, "row 5 → ID 1");
        }

        [Test]
        public void build_cells_answer_cells_remain_answer()
        {
            var slots = new List<SlotDraft> { Across(1, 0, "CASTLE") };
            var cells = LevelBuilder.BuildCells(slots, LevelBuilder.GridWidth, LevelBuilder.GridHeight);
            // row 1, col 1 (first answer cell of the word) must still be Answer
            Assert.AreEqual(CellType.Answer, cells[1 * LevelBuilder.GridWidth + 1].cellType);
        }

        // ── GetAnswerErrors ─────────────────────────────────────────────────

        [Test]
        public void get_answer_errors_returns_empty_for_valid_slots()
        {
            var slots = new List<SlotDraft>
            {
                Across(2, 0, "CASTLE"),    // 6 letters, max = GridWidth-1-0 = 7 ✓
                Down(0, 2, "CAPTAINS")    // 8 letters, max = GridHeight-1-0 = 9 ✓
            };
            Assert.IsEmpty(LevelBuilder.GetAnswerErrors(slots, LevelBuilder.GridWidth, LevelBuilder.GridHeight));
        }

        [Test]
        public void get_answer_errors_flags_empty_answer()
        {
            var slots = new List<SlotDraft> { Across(1, 0, "") };
            var errors = LevelBuilder.GetAnswerErrors(slots, LevelBuilder.GridWidth, LevelBuilder.GridHeight);
            Assert.IsNotEmpty(errors);
            StringAssert.Contains("empty", errors[0]);
        }

        [Test]
        public void get_answer_errors_flags_answer_too_long()
        {
            // Max Across length from col 2 = GridWidth-1-2 = 5; CASTLE is 6 → too long
            var slots = new List<SlotDraft> { Across(1, 2, "CASTLE") };
            var errors = LevelBuilder.GetAnswerErrors(slots, LevelBuilder.GridWidth, LevelBuilder.GridHeight);
            Assert.IsNotEmpty(errors);
            StringAssert.Contains("1,2", errors[0]);
        }

        [Test]
        public void get_answer_errors_accepts_single_letter_answer()
        {
            var slots = new List<SlotDraft> { Down(0, 1, "A") }; // minimum is 1
            var errors = LevelBuilder.GetAnswerErrors(slots, LevelBuilder.GridWidth, LevelBuilder.GridHeight);
            Assert.IsEmpty(errors);
        }

        // ── GetIntersectionErrors ────────────────────────────────────────────

        [Test]
        public void get_intersection_errors_returns_empty_when_letters_match()
        {
            // Across clue at (2,0), word: row 2 cols 1-6 → CASTLE[c-1]
            // Down clue at (0,2), word: col 2 rows 1-8  → CAPTAINS[r-1]
            // Intersection (r=2, c=2): acrossPos=2-0-1=1 → 'A', downPos=2-0-1=1 → 'A' ✓
            var slots = new List<SlotDraft>
            {
                Across(2, 0, "CASTLE"),
                Down(0, 2, "CAPTAINS")
            };
            Assert.IsEmpty(LevelBuilder.GetIntersectionErrors(slots));
        }

        [Test]
        public void get_intersection_errors_flags_mismatch()
        {
            // Across (2,0) CASTLE: col2 → 'A'
            // Down (0,2) TERRIBLE: row2 → 'E' → mismatch
            var slots = new List<SlotDraft>
            {
                Across(2, 0, "CASTLE"),
                Down(0, 2, "TERRIBLE")
            };
            var errors = LevelBuilder.GetIntersectionErrors(slots);
            Assert.IsNotEmpty(errors);
            StringAssert.Contains("row=2", errors[0]);
            StringAssert.Contains("col=2", errors[0]);
        }

        [Test]
        public void get_intersection_errors_skips_non_overlapping_slots()
        {
            // Across at row 3, cols 1-6; Down at col 5, rows 1-2 (answer "AB")
            // Intersection row=3, col=5: downPos = 3-0-1 = 2, but answer.Length=2 → out of range → skip
            var slots = new List<SlotDraft>
            {
                Across(3, 0, "CASTLE"),
                Down(0, 5, "AB")
            };
            // Down word only covers rows 1-2; Across is at row 3 → no overlap → no error
            Assert.IsEmpty(LevelBuilder.GetIntersectionErrors(slots));
        }
    }
}
