using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace CrosswordGo
{
    public class LevelEditorWindow : EditorWindow
    {
        [MenuItem("Tools/Crossword GO/Level Editor")]
        public static void Open() => GetWindow<LevelEditorWindow>("Level Editor");

        // ── state ────────────────────────────────────────────────────────────

        private string _title = "New Level";
        private Difficulty _difficulty = Difficulty.Medium;
        private int _gridWidth  = LevelBuilder.GridWidth;
        private int _gridHeight = LevelBuilder.GridHeight;

        private WordDatabase _wordDatabase;
        private LevelData _loadTarget;

        private readonly List<SlotDraft> _slots = new List<SlotDraft>();
        private readonly Dictionary<int, string> _cellHints = new Dictionary<int, string>();
        private int _selRow = -1;
        private int _selCol = -1;

        private List<string> _errors = new List<string>();
        private string _validationStatus = "";
        private Vector2 _scrollPos;
        private string _savePath = "Assets/Levels/NewLevel.asset";

        // ── constants ────────────────────────────────────────────────────────

        private const int CellSize = 44;
        private int GridPanelWidth => _gridWidth * (CellSize + 2) + 8;

        // ── styles (lazy) ────────────────────────────────────────────────────

        private GUIStyle _errorStyle;
        private GUIStyle _cellStyle;

        private void OnGUI()
        {
            if (_errorStyle == null)
            {
                _errorStyle = new GUIStyle(EditorStyles.helpBox);
                _errorStyle.normal.textColor = new Color(0.85f, 0.2f, 0.2f);
            }
            if (_cellStyle == null)
            {
                _cellStyle = new GUIStyle(GUI.skin.button);
                _cellStyle.fontSize = 11;
                _cellStyle.wordWrap = true;
            }

            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

            DrawLevelInfo();
            EditorGUILayout.Space(8);

            EditorGUILayout.BeginHorizontal();
            DrawGridPanel();
            GUILayout.Space(8);
            DrawInspectorPanel();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);
            DrawValidationAndSave();

            EditorGUILayout.EndScrollView();
        }

        // ── level info ───────────────────────────────────────────────────────

        private void DrawLevelInfo()
        {
            EditorGUILayout.LabelField("Level Info", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                _title = EditorGUILayout.TextField("Title", _title);
                _difficulty = (Difficulty)EditorGUILayout.EnumPopup("Difficulty", _difficulty);

                EditorGUILayout.Space(4);
                _wordDatabase = (WordDatabase)EditorGUILayout.ObjectField(
                    "Word Database", _wordDatabase, typeof(WordDatabase), false);
                if (_wordDatabase == null)
                    EditorGUILayout.HelpBox("No Word Database assigned — using built-in word list.", MessageType.Info);

                EditorGUILayout.Space(4);
                EditorGUILayout.LabelField("Grid Size", EditorStyles.boldLabel);
                int newW = Mathf.Clamp(EditorGUILayout.IntField("Columns", _gridWidth), 3, 26);
                int newH = Mathf.Clamp(EditorGUILayout.IntField("Rows",    _gridHeight), 3, 26);
                if (newW != _gridWidth || newH != _gridHeight)
                {
                    _gridWidth  = newW;
                    _gridHeight = newH;
                    _selRow = -1;
                    _selCol = -1;
                }

                EditorGUILayout.Space(4);
                if (GUILayout.Button($"Randomize ({_difficulty})", GUILayout.Height(28)))
                    RandomizeLevel();

                EditorGUILayout.Space(4);
                EditorGUILayout.LabelField("Load Existing Level", EditorStyles.boldLabel);
                using (new EditorGUILayout.HorizontalScope())
                {
                    _loadTarget = (LevelData)EditorGUILayout.ObjectField(_loadTarget, typeof(LevelData), false);
                    using (new EditorGUI.DisabledScope(_loadTarget == null))
                    {
                        if (GUILayout.Button("Load", GUILayout.Width(60)))
                            LoadLevel(_loadTarget);
                    }
                }
            }
        }

        private void LoadLevel(LevelData level)
        {
            _title      = level.title;
            _difficulty = level.difficulty;
            _gridWidth  = level.gridWidth  > 0 ? level.gridWidth  : LevelBuilder.GridWidth;
            _gridHeight = level.gridHeight > 0 ? level.gridHeight : LevelBuilder.GridHeight;
            _slots.Clear();
            _slots.AddRange(LevelBuilder.LoadFromLevel(level));
            _cellHints.Clear();
            foreach (var kvp in LevelBuilder.LoadCellHints(level))
                _cellHints[kvp.Key] = kvp.Value;
            _selRow = -1;
            _selCol = -1;
            _errors.Clear();
            _validationStatus = $"Loaded: {level.title}";
            string assetPath = AssetDatabase.GetAssetPath(level);
            if (!string.IsNullOrEmpty(assetPath))
                _savePath = assetPath;
            Repaint();
        }

        private void RandomizeLevel()
        {
            _slots.Clear();
            _cellHints.Clear();
            _slots.AddRange(LevelGenerator.Generate(_difficulty, _wordDatabase));
            _selRow = -1;
            _selCol = -1;
            _errors.Clear();
            _validationStatus = "";
            Repaint();
        }

        // ── grid panel ───────────────────────────────────────────────────────

        private void DrawGridPanel()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(GridPanelWidth));
            EditorGUILayout.LabelField("Grid — click a cell to edit", EditorStyles.boldLabel);

            for (int r = 0; r < _gridHeight; r++)
            {
                EditorGUILayout.BeginHorizontal();
                for (int c = 0; c < _gridWidth; c++)
                {
                    bool selected = r == _selRow && c == _selCol;
                    var prevBg = GUI.backgroundColor;
                    GUI.backgroundColor = CellButtonColor(r, c, selected);

                    if (GUILayout.Button(CellButtonLabel(r, c), _cellStyle,
                            GUILayout.Width(CellSize), GUILayout.Height(CellSize)))
                    {
                        _selRow = r;
                        _selCol = c;
                        GUI.FocusControl(null);
                    }

                    GUI.backgroundColor = prevBg;
                }
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("► = Across   ▼ = Down   · = answer cell", EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();
        }

        private string CellButtonLabel(int r, int c)
        {
            bool hasA = HasSlotAt(Direction.Across, r, c);
            bool hasD = HasSlotAt(Direction.Down, r, c);
            if (hasA && hasD) return "►\n▼";
            if (hasA) return "►";
            if (hasD) return "▼";
            _cellHints.TryGetValue(r * _gridWidth + c, out string hint);
            return string.IsNullOrEmpty(hint) ? "·" : hint;
        }

        private Color CellButtonColor(int r, int c, bool selected)
        {
            if (selected) return new Color(1f, 0.9f, 0.3f);
            bool hasA = HasSlotAt(Direction.Across, r, c);
            bool hasD = HasSlotAt(Direction.Down, r, c);
            if (hasA && hasD) return new Color(0.8f, 0.6f, 1f);
            if (hasA) return new Color(0.6f, 0.8f, 1f);
            if (hasD) return new Color(0.6f, 1f, 0.7f);
            return Color.white;
        }

        // ── inspector panel ──────────────────────────────────────────────────

        private void DrawInspectorPanel()
        {
            EditorGUILayout.BeginVertical(GUILayout.MinWidth(260));

            if (_selRow < 0)
            {
                EditorGUILayout.HelpBox("Click a grid cell to add or edit slots.", MessageType.Info);
                EditorGUILayout.EndVertical();
                return;
            }

            EditorGUILayout.LabelField($"Cell ({_selRow}, {_selCol})", EditorStyles.boldLabel);

            // ── cell hint (independent of slots) ─────────────────────────────
            int cellIndex = _selRow * _gridWidth + _selCol;
            _cellHints.TryGetValue(cellIndex, out string currentHint);
            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Hint Letter", GUILayout.Width(84));
                string raw    = EditorGUILayout.TextField(currentHint ?? "", GUILayout.Width(30));
                string newHint = string.IsNullOrEmpty(raw) ? "" : raw[raw.Length - 1].ToString().ToUpperInvariant();
                if (newHint != (currentHint ?? ""))
                {
                    if (string.IsNullOrEmpty(newHint)) _cellHints.Remove(cellIndex);
                    else _cellHints[cellIndex] = newHint;
                }
                if (GUILayout.Button("✕", GUILayout.Width(22)))
                    _cellHints.Remove(cellIndex);
            }

            EditorGUILayout.Space(8);
            DrawSlotInspectorSection(Direction.Across);
            EditorGUILayout.Space(8);
            DrawSlotInspectorSection(Direction.Down);

            EditorGUILayout.EndVertical();
        }

        private void DrawSlotInspectorSection(Direction dir)
        {
            int maxLen = dir == Direction.Across
                ? _gridWidth  - 1 - _selCol
                : _gridHeight - 1 - _selRow;

            string arrow = dir == Direction.Across ? "►" : "▼";
            EditorGUILayout.LabelField($"{arrow} {dir}  (max {maxLen} letters)", EditorStyles.boldLabel);

            var slot = FindSlotAt(dir, _selRow, _selCol);
            bool hadSlot = slot != null;
            bool wantSlot = EditorGUILayout.ToggleLeft($"Enable {dir} slot here", hadSlot);

            if (wantSlot && !hadSlot)
            {
                slot = new SlotDraft { direction = dir, clueRow = _selRow, clueCol = _selCol };
                _slots.Add(slot);
            }
            else if (!wantSlot && hadSlot)
            {
                _slots.Remove(slot);
                slot = null;
            }

            using (new EditorGUI.DisabledScope(slot == null))
            {
                string answer        = slot?.answer ?? "";
                string clue          = slot?.clue ?? "";
                WordClueType clueType = slot?.clueType ?? WordClueType.Text;
                Sprite clueImage     = slot?.clueImage;

                string newAnswer = EditorGUILayout.TextField("Answer", answer).ToUpperInvariant();

                // If a database exists and the answer changed, auto-populate the first clue
                if (_wordDatabase != null && newAnswer != answer && newAnswer.Length >= 1)
                {
                    var entry = _wordDatabase.GetEntry(newAnswer);
                    if (entry != null && entry.clues.Count > 0)
                    {
                        clue = entry.clues[0].text;
                        clueType = entry.clues[0].clueType;
                        clueImage = entry.clues[0].image;
                    }
                }

                clueType = (WordClueType)EditorGUILayout.EnumPopup("Clue Type", clueType);
                clue = EditorGUILayout.TextField("Clue Text", clue);
                clueImage = (Sprite)EditorGUILayout.ObjectField("Clue Image", clueImage, typeof(Sprite), false);

                // Clue picker when database is available
                if (_wordDatabase != null && slot != null && !string.IsNullOrEmpty(newAnswer))
                {
                    var entry = _wordDatabase.GetEntry(newAnswer);
                    if (entry != null && entry.clues.Count > 1)
                    {
                        if (GUILayout.Button("Next Clue from Database"))
                        {
                            int idx = entry.clues.FindIndex(c => c.text == clue);
                            int next = (idx + 1) % entry.clues.Count;
                            clue = entry.clues[next].text;
                            clueType = entry.clues[next].clueType;
                            clueImage = entry.clues[next].image;
                        }
                    }
                }

                if (slot != null)
                {
                    slot.answer    = newAnswer;
                    slot.clue      = clue;
                    slot.clueType  = clueType;
                    slot.clueImage = clueImage;
                }
            }
        }

        // ── validation & save ────────────────────────────────────────────────

        private void DrawValidationAndSave()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Validate", GUILayout.Width(100)))
                    Validate();
                if (GUILayout.Button("Clear", GUILayout.Width(60)))
                {
                    _errors.Clear();
                    _validationStatus = "";
                }
            }

            if (!string.IsNullOrEmpty(_validationStatus))
                EditorGUILayout.HelpBox(_validationStatus, MessageType.Info);

            foreach (var err in _errors)
                EditorGUILayout.LabelField(err, _errorStyle);

            EditorGUILayout.Space(4);

            using (new EditorGUILayout.HorizontalScope())
            {
                _savePath = EditorGUILayout.TextField("Save Path", _savePath);
                if (GUILayout.Button("...", GUILayout.Width(30)))
                {
                    string path = EditorUtility.SaveFilePanelInProject(
                        "Save Level", Path.GetFileNameWithoutExtension(_savePath), "asset", "");
                    if (!string.IsNullOrEmpty(path))
                        _savePath = path;
                }
            }

            if (GUILayout.Button("Save Level Asset"))
                TrySave();
        }

        private void Validate()
        {
            _errors = new List<string>(LevelBuilder.GetAnswerErrors(_slots, _gridWidth, _gridHeight));
            _errors.AddRange(LevelBuilder.GetIntersectionErrors(_slots));
            _errors.AddRange(LevelBuilder.GetCellHintErrors(_slots, _cellHints, _gridWidth));
            _validationStatus = _errors.Count == 0 ? "No errors found." : "";
            Repaint();
        }

        private void TrySave()
        {
            var answerErrors       = LevelBuilder.GetAnswerErrors(_slots, _gridWidth, _gridHeight);
            var intersectionErrors = LevelBuilder.GetIntersectionErrors(_slots);
            var hintErrors         = LevelBuilder.GetCellHintErrors(_slots, _cellHints, _gridWidth);

            if (answerErrors.Count > 0 || intersectionErrors.Count > 0 || hintErrors.Count > 0)
            {
                _errors = new List<string>(answerErrors);
                _errors.AddRange(intersectionErrors);
                _errors.AddRange(hintErrors);
                _validationStatus = "";
                Repaint();
                EditorUtility.DisplayDialog("Validation Failed",
                    "Fix errors before saving. Run Validate to see details.", "OK");
                return;
            }

            string dir = Path.GetDirectoryName(_savePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            var level = LevelBuilder.Build(_title, _slots, _difficulty, _gridWidth, _gridHeight, _cellHints);
            LevelBuilder.SaveAsset(level, _savePath);

            EditorUtility.DisplayDialog("Saved", $"Level saved to {_savePath}", "OK");
            Selection.activeObject = level;
        }

        // ── helpers ──────────────────────────────────────────────────────────

        private bool HasSlotAt(Direction dir, int row, int col) =>
            FindSlotAt(dir, row, col) != null;

        private SlotDraft FindSlotAt(Direction dir, int row, int col)
        {
            foreach (var s in _slots)
                if (s.direction == dir && s.clueRow == row && s.clueCol == col)
                    return s;
            return null;
        }
    }
}
