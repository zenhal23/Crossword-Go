using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CrosswordGo
{
    // Instantiates the crossword grid as a UI grid of CellView prefabs.
    public class GridView : MonoBehaviour
    {
        [SerializeField] private CellView cellViewPrefab;
        [SerializeField] private GridLayoutGroup gridLayout;

        private LevelData _level;
        private CellView[] _cells;
        // Maps slot id → list of answer cell indices in that slot
        private Dictionary<int, List<int>> _slotCells = new Dictionary<int, List<int>>();

        public void Build(LevelData level, BoardState board)
        {
            _level = level;

            // Configure grid layout
            gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gridLayout.constraintCount = level.gridWidth;

            // Fit cells into available rect
            Canvas.ForceUpdateCanvases();
            var rt = gridLayout.GetComponent<RectTransform>();
            float availW = rt.rect.width  - gridLayout.padding.left - gridLayout.padding.right;
            float availH = rt.rect.height - gridLayout.padding.top  - gridLayout.padding.bottom;
            float cellW = (availW - gridLayout.spacing.x * (level.gridWidth  - 1)) / level.gridWidth;
            float cellH = (availH - gridLayout.spacing.y * (level.gridHeight - 1)) / level.gridHeight;
            float cellSize = Mathf.Floor(Mathf.Min(cellW, cellH));
            gridLayout.cellSize = new Vector2(cellSize, cellSize);

            // Pre-build slot → cells map
            foreach (var slot in level.wordSlots)
            {
                var indices = new List<int>();
                for (int i = 0; i < slot.length; i++)
                {
                    int r = slot.direction == Direction.Across ? slot.startRow : slot.startRow + i;
                    int c = slot.direction == Direction.Across ? slot.startCol + i : slot.startCol;
                    indices.Add(r * level.gridWidth + c);
                }
                _slotCells[slot.id] = indices;
            }

            // Clear existing children
            foreach (Transform child in gridLayout.transform)
                Destroy(child.gameObject);

            _cells = new CellView[level.cells.Length];
            for (int i = 0; i < level.cells.Length; i++)
            {
                var cell = Instantiate(cellViewPrefab, gridLayout.transform);
                var data = level.cells[i];

                WordSlotData? acrossSlot = null, downSlot = null;
                if (data.cellType == CellType.Clue)
                {
                    if (data.acrossSlotId >= 0) acrossSlot = level.wordSlots[data.acrossSlotId];
                    if (data.downSlotId >= 0) downSlot = level.wordSlots[data.downSlotId];
                }

                cell.Setup(i, data, acrossSlot, downSlot);
                _cells[i] = cell;
            }
        }

        // Called by PlayerInputController — registers a tap handler on each answer cell
        public void RegisterTapHandler(System.Action<int> onCellTapped)
        {
            foreach (var cell in _cells)
                cell.OnTapped = onCellTapped;
        }

        // Called by PlayerInputController — registers a drop handler on each cell
        public void RegisterDropHandler(System.Action<int, LetterTile> onDrop)
        {
            foreach (var cell in _cells)
                cell.OnLetterDropped = onDrop;
        }

        // Called by TurnManager event, or directly for hint pre-lock (owner == 0)
        public void OnCellLocked(int cellIndex, int owner)
        {
            if (cellIndex < 0 || cellIndex >= _cells.Length) return;
            char letter;
            if (owner == 0 && _level.cellHints != null && cellIndex < _level.cellHints.Length
                           && !string.IsNullOrEmpty(_level.cellHints[cellIndex]))
                letter = char.ToUpperInvariant(_level.cellHints[cellIndex][0]);
            else
                letter = ScoringSystem.GetCorrectLetter(_level, cellIndex);
            _cells[cellIndex].SetLetter(letter, isPending: false);
            _cells[cellIndex].SetLocked(owner);
        }

        // Called when a slot is fully completed
        public void OnSlotCompleted(int slotId)
        {
            if (!_slotCells.TryGetValue(slotId, out var indices)) return;
            foreach (int idx in indices)
                _cells[idx].SetSlotComplete();
        }

        // Called by PlayerInputController to show pending placement
        public void ShowPending(int cellIndex, char letter)
        {
            if (cellIndex < 0 || cellIndex >= _cells.Length) return;
            _cells[cellIndex].SetLetter(letter, isPending: true);
        }

        // Called by PlayerInputController to clear a pending placement
        public void ClearPending(int cellIndex)
        {
            if (cellIndex < 0 || cellIndex >= _cells.Length) return;
            _cells[cellIndex].SetLetter('\0', isPending: false);
        }
    }
}
