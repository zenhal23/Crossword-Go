using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace CrosswordGo
{
    // Standard 9×7 layout (9 rows, 7 cols — no black cells):
    //
    //      c0    c1    c2    c3    c4    c5    c6
    //  r0 [hdr] [ ↓ ] [ ↓ ] [ ↓ ] [ ↓ ] [ ↓ ] [ ↓ ]  ← Down clue cells (slotId=-1 if no Down word)
    //  r1 [ → ] [ A  ] [ A  ] [ A  ] [ A  ] [ A  ] [ A  ]  ← Across word (6 letters)
    //  r2 [ → ] [ A  ] [ A  ] [ A  ] [ A  ] [ A  ] [ A  ]
    //   …
    //  r8 [ → ] [ A  ] [ A  ] [ A  ] [ A  ] [ A  ] [ A  ]
    //
    // [hdr] = corner cell (Clue, slotId=-1, shows nothing)
    // [ ↓ ] = Down clue cell; slotId=-1 when no Down word for that column
    // [ →  ] = Across clue cell
    // [ A  ] = Answer cell; belongs to its row's Across word and optionally a column's Down word

    public static class SampleLevelCreator
    {
        private const string OutputPath = "Assets/Levels/Sample";
        private const int Rows = 9;
        private const int Cols = 7;

        [MenuItem("Tools/Crossword GO/Create Sample Levels")]
        public static void CreateSampleLevels()
        {
            EnsureFolder(OutputPath);
            CreateTutorialLevel();
            CreateStandardLevel();
            CreateChallengeLevel();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[CrosswordGO] Sample levels created at " + OutputPath);
        }

        // ── Level 1: Tutorial ───────────────────────────────────────────────
        // 8 Across words, no Down words — no intersection constraints.
        // Across words (rows 1-8): CASTLE BRIGHT SIMPLE WONDER FINGER TURTLE GROUND PLANET
        private static void CreateTutorialLevel()
        {
            var (cells, slots) = BuildGrid(
                acrossWords: new[] { "CASTLE", "BRIGHT", "SIMPLE", "WONDER", "FINGER", "TURTLE", "GROUND", "PLANET" },
                acrossClues: new[] {
                    "Medieval stone fortification",
                    "Filled with light",
                    "Easy to understand",
                    "Filled with amazement",
                    "One of five on a hand",
                    "Slow reptile with a shell",
                    "Solid earth beneath your feet",
                    "Celestial body orbiting a star"
                },
                downWords: null,
                downClues: null,
                downCols: null
            );

            var level = MakeLevel("Tutorial", Cols, Rows, Difficulty.Easy, 60, cells, slots);
            Save(level, OutputPath + "/Level_Tutorial.asset");
        }

        // ── Level 2: Standard ───────────────────────────────────────────────
        // 8 Across words + 1 Down word (TOGETHER at col 2).
        // Verified: letter[1] of each Across word = T,O,G,E,T,H,E,R = TOGETHER
        //   STATUE·POCKET·AGENCY·RECIPE·ATTACK·THRONE·NEPHEW·BRIDGE
        private static void CreateStandardLevel()
        {
            var (cells, slots) = BuildGrid(
                acrossWords: new[] { "STATUE", "POCKET", "AGENCY", "RECIPE", "ATTACK", "THRONE", "NEPHEW", "BRIDGE" },
                acrossClues: new[] {
                    "Sculpted likeness of a person",
                    "Small bag or pouch",
                    "Organisation that acts for others",
                    "Set of instructions to follow",
                    "Sudden forceful assault",
                    "Royal seat of power",
                    "Son of your brother or sister",
                    "Structure spanning a gap"
                },
                downWords: new[] { "TOGETHER" },
                downClues: new[] { "As a group, not apart" },
                downCols: new[] { 2 }
            );

            var level = MakeLevel("Speed Round", Cols, Rows, Difficulty.Medium, 45, cells, slots);
            Save(level, OutputPath + "/Level_Standard.asset");
        }

        // ── Level 3: Challenge ──────────────────────────────────────────────
        // 8 Across words + 1 Down word (PICTURES at col 4).
        // Verified: letter[3] of each Across word = P,I,C,T,U,R,E,S = PICTURES
        //   CHAPEL·ARTIST·PENCIL·PARTLY·RITUAL·ASTRAY·INDEED·THESIS
        private static void CreateChallengeLevel()
        {
            var (cells, slots) = BuildGrid(
                acrossWords: new[] { "CHAPEL", "ARTIST", "PENCIL", "PARTLY", "RITUAL", "ASTRAY", "INDEED", "THESIS" },
                acrossClues: new[] {
                    "Small place of worship",
                    "Person who creates art",
                    "Writing instrument with graphite",
                    "To some degree, not fully",
                    "A repeated ceremony or rite",
                    "Lost and far from the right path",
                    "Truly, certainly",
                    "A long essay or argument"
                },
                downWords: new[] { "PICTURES" },
                downClues: new[] { "Photographs or illustrations" },
                downCols: new[] { 4 }
            );

            var level = MakeLevel("Chain Reaction", Cols, Rows, Difficulty.Hard, 30, cells, slots);
            Save(level, OutputPath + "/Level_Challenge.asset");
        }

        // ── Grid builder ────────────────────────────────────────────────────
        // Builds cells + slots for the standard 9×7 no-black layout.
        // downCols[i] is the column index (1-6) where downWords[i] runs (rows 1-8).
        private static (CellData[] cells, WordSlotData[] slots) BuildGrid(
            string[] acrossWords, string[] acrossClues,
            string[] downWords, string[] downClues, int[] downCols)
        {
            int total = Rows * Cols;
            var cells = new CellData[total];
            var slots = new List<WordSlotData>();

            // Row 0: corner (0,0) + Down clue cells for cols 1-6
            cells[I(0, 0)] = HeaderClue();
            for (int c = 1; c < Cols; c++)
                cells[I(0, c)] = HeaderClue(); // placeholder; overwritten for real Down words below

            // Down word slots (slot IDs come after the 8 Across slots, i.e. start at 8)
            if (downWords != null)
            {
                for (int d = 0; d < downWords.Length; d++)
                {
                    int slotId = 8 + d;
                    int col = downCols[d];
                    cells[I(0, col)] = DownClue(slotId);
                    slots.Add(Slot(slotId, Direction.Down, 0, col, 1, col, downWords[d], downClues[d]));
                }
            }

            // Rows 1-8: Across clue in col 0, Answer cells in cols 1-6
            for (int r = 1; r < Rows; r++)
            {
                int slotId = r - 1; // Across slots 0-7
                cells[I(r, 0)] = AcrossClue(slotId);
                for (int c = 1; c < Cols; c++)
                    cells[I(r, c)] = Answer();
                slots.Add(Slot(slotId, Direction.Across, r, 0, r, 1, acrossWords[slotId], acrossClues[slotId]));
            }

            // Sort slots by id so array index == id
            slots.Sort((a, b) => a.id.CompareTo(b.id));
            return (cells, slots.ToArray());
        }

        // ── Helpers ─────────────────────────────────────────────────────────

        private static LevelData MakeLevel(string title, int w, int h, Difficulty diff, int timer,
            CellData[] cells, WordSlotData[] slots)
        {
            var level = ScriptableObject.CreateInstance<LevelData>();
            level.title = title;
            level.gridWidth = w;
            level.gridHeight = h;
            level.difficulty = diff;
            level.cells = cells;
            level.wordSlots = slots;
            return level;
        }

        private static WordSlotData Slot(int id, Direction dir,
            int clueRow, int clueCol, int startRow, int startCol,
            string answer, string clue) => new WordSlotData
        {
            id = id,
            direction = dir,
            clueRow = clueRow, clueCol = clueCol,
            startRow = startRow, startCol = startCol,
            length = answer.Length,
            answer = answer,
            clue = clue,
        };

        private static int I(int row, int col) => row * Cols + col;
        private static CellData HeaderClue() => new CellData { cellType = CellType.Clue, acrossSlotId = -1, downSlotId = -1 };
        private static CellData AcrossClue(int id) => new CellData { cellType = CellType.Clue, acrossSlotId = id, downSlotId = -1 };
        private static CellData DownClue(int id) => new CellData { cellType = CellType.Clue, acrossSlotId = -1, downSlotId = id };
        private static CellData Answer() => new CellData { cellType = CellType.Answer, acrossSlotId = -1, downSlotId = -1 };

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            string folderName = Path.GetFileName(path);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, folderName);
        }

        private static void Save(LevelData level, string assetPath)
        {
            AssetDatabase.CreateAsset(level, assetPath);
            Debug.Log("[CrosswordGO] Created " + assetPath);
        }
    }
}
