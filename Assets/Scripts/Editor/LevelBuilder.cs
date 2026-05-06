using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace CrosswordGo
{
    public static class LevelBuilder
    {
        public const int GridWidth = 8;
        public const int GridHeight = 10;

        // IDs: Across slots sorted by (clueRow, clueCol) → 0..n-1,
        // then Down slots sorted by (clueRow, clueCol) → n..n+m-1.
        public static CellData[] BuildCells(List<SlotDraft> slots, int gridWidth, int gridHeight)
        {
            var ids = AssignIds(slots);

            var cells = new CellData[gridHeight * gridWidth];
            for (int i = 0; i < cells.Length; i++)
                cells[i] = new CellData { cellType = CellType.Answer, acrossSlotId = -1, downSlotId = -1 };

            foreach (var (slot, id) in ids)
            {
                int idx = slot.clueRow * gridWidth + slot.clueCol;
                var c = cells[idx];
                c.cellType = CellType.Clue;
                if (slot.direction == Direction.Across)
                    c.acrossSlotId = id;
                else
                    c.downSlotId = id;
                cells[idx] = c;
            }

            return cells;
        }

        public static WordSlotData[] BuildWordSlots(List<SlotDraft> slots)
        {
            return AssignIds(slots).Select(x =>
            {
                var (s, id) = x;
                return new WordSlotData
                {
                    id = id,
                    direction = s.direction,
                    clueRow = s.clueRow,
                    clueCol = s.clueCol,
                    startRow = s.direction == Direction.Across ? s.clueRow : s.clueRow + 1,
                    startCol = s.direction == Direction.Across ? s.clueCol + 1 : s.clueCol,
                    length = s.answer.Length,
                    answer = s.answer.ToUpperInvariant(),
                    clue = s.clue,
                    clueType = s.clueType,
                    clueSprite = s.clueImage
                };
            }).ToArray();
        }

        public static List<string> GetAnswerErrors(List<SlotDraft> slots, int gridWidth, int gridHeight)
        {
            var errors = new List<string>();
            foreach (var s in slots)
            {
                if (string.IsNullOrEmpty(s.answer))
                {
                    errors.Add($"{s.direction} at ({s.clueRow},{s.clueCol}): answer is empty");
                    continue;
                }
                int maxLen = s.direction == Direction.Across
                    ? gridWidth - 1 - s.clueCol
                    : gridHeight - 1 - s.clueRow;
                if (s.answer.Length < 1 || s.answer.Length > maxLen)
                    errors.Add($"{s.direction} at ({s.clueRow},{s.clueCol}): '{s.answer}' — length must be 1–{maxLen}");

            }
            return errors;
        }

        public static List<string> GetIntersectionErrors(List<SlotDraft> slots)
        {
            var errors = new List<string>();
            var acrossSlots = slots.Where(s => s.direction == Direction.Across && !string.IsNullOrEmpty(s.answer)).ToList();
            var downSlots = slots.Where(s => s.direction == Direction.Down && !string.IsNullOrEmpty(s.answer)).ToList();

            foreach (var across in acrossSlots)
            {
                foreach (var down in downSlots)
                {
                    int r = across.clueRow;
                    int c = down.clueCol;

                    int acrossPos = c - across.clueCol - 1; // 0-indexed position in Across answer
                    int downPos = r - down.clueRow - 1;     // 0-indexed position in Down answer

                    if (acrossPos < 0 || acrossPos >= across.answer.Length) continue;
                    if (downPos < 0 || downPos >= down.answer.Length) continue;

                    char al = char.ToUpperInvariant(across.answer[acrossPos]);
                    char dl = char.ToUpperInvariant(down.answer[downPos]);
                    if (al != dl)
                        errors.Add($"Intersection at (row={r}, col={c}): Across '{al}' vs Down '{dl}'");
                }
            }

            return errors;
        }

        // Validates that each cell hint matches the answer letter covering that cell.
        public static List<string> GetCellHintErrors(List<SlotDraft> slots, Dictionary<int, string> cellHints, int gridWidth)
        {
            var errors = new List<string>();
            if (cellHints == null || cellHints.Count == 0) return errors;

            // Build cellIndex → (expected letter, slot description) from every slot
            var expected = new Dictionary<int, (char letter, string slotDesc)>();
            foreach (var s in slots)
            {
                if (string.IsNullOrEmpty(s.answer)) continue;
                string upper = s.answer.ToUpperInvariant();
                string slotDesc = $"{s.direction} '{upper}'";
                int startRow = s.direction == Direction.Across ? s.clueRow      : s.clueRow + 1;
                int startCol = s.direction == Direction.Across ? s.clueCol + 1  : s.clueCol;
                for (int i = 0; i < upper.Length; i++)
                {
                    int r = s.direction == Direction.Across ? startRow       : startRow + i;
                    int c = s.direction == Direction.Across ? startCol + i   : startCol;
                    expected[r * gridWidth + c] = (upper[i], slotDesc);
                }
            }

            foreach (var kvp in cellHints)
            {
                if (string.IsNullOrEmpty(kvp.Value)) continue;
                char hint = char.ToUpperInvariant(kvp.Value[0]);
                int row = kvp.Key / gridWidth;
                int col = kvp.Key % gridWidth;

                if (!expected.TryGetValue(kvp.Key, out var exp))
                    errors.Add($"Cell ({row},{col}): hint '{hint}' is not covered by any slot");
                else if (hint != exp.letter)
                    errors.Add($"Cell ({row},{col}): hint '{hint}' does not match '{exp.letter}' in {exp.slotDesc}");
            }
            return errors;
        }

        // Creates a LevelData ScriptableObject in memory (not saved to disk).
        public static LevelData Build(string title, List<SlotDraft> slots, Difficulty difficulty,
                                      int gridWidth, int gridHeight,
                                      Dictionary<int, string> cellHints = null)
        {
            var level = ScriptableObject.CreateInstance<LevelData>();
            level.title = title;
            level.gridWidth = gridWidth;
            level.gridHeight = gridHeight;
            level.cells = BuildCells(slots, gridWidth, gridHeight);
            level.wordSlots = BuildWordSlots(slots);
            level.difficulty = difficulty;

            level.cellHints = new string[gridWidth * gridHeight];
            if (cellHints != null)
                foreach (var kvp in cellHints)
                    if (kvp.Key >= 0 && kvp.Key < level.cellHints.Length && !string.IsNullOrEmpty(kvp.Value))
                        level.cellHints[kvp.Key] = kvp.Value[0].ToString().ToUpperInvariant();

            return level;
        }

        // Reconstructs a cell hints dictionary from an existing LevelData asset.
        public static Dictionary<int, string> LoadCellHints(LevelData level)
        {
            var hints = new Dictionary<int, string>();
            if (level?.cellHints == null) return hints;
            for (int i = 0; i < level.cellHints.Length; i++)
                if (!string.IsNullOrEmpty(level.cellHints[i]))
                    hints[i] = level.cellHints[i];
            return hints;
        }

        // Saves a LevelData asset to the given path (must start with "Assets/").
        public static void SaveAsset(LevelData level, string assetPath)
        {
            AssetDatabase.CreateAsset(level, assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        // Reconstructs a list of SlotDrafts from an existing LevelData asset.
        public static List<SlotDraft> LoadFromLevel(LevelData level)
        {
            var slots = new List<SlotDraft>();
            if (level?.wordSlots == null) return slots;
            foreach (var ws in level.wordSlots)
            {
                slots.Add(new SlotDraft
                {
                    direction      = ws.direction,
                    clueRow        = ws.clueRow,
                    clueCol        = ws.clueCol,
                    answer         = ws.answer ?? "",
                    clue           = ws.clue ?? "",
                    clueType       = ws.clueType,
                    clueImage      = ws.clueSprite
                });
            }
            return slots;
        }

        private static List<(SlotDraft slot, int id)> AssignIds(List<SlotDraft> slots)
        {
            var result = new List<(SlotDraft, int)>();
            int id = 0;
            foreach (var s in slots.Where(s => s.direction == Direction.Across)
                                   .OrderBy(s => s.clueRow).ThenBy(s => s.clueCol))
                result.Add((s, id++));
            foreach (var s in slots.Where(s => s.direction == Direction.Down)
                                   .OrderBy(s => s.clueRow).ThenBy(s => s.clueCol))
                result.Add((s, id++));
            return result;
        }
    }
}
