using UnityEngine;

namespace CrosswordGo
{
    [CreateAssetMenu(menuName = "CrosswordGo/Level", fileName = "NewLevel")]
    public class LevelData : ScriptableObject
    {
        public string title;
        public int gridWidth;
        public int gridHeight;
        public CellData[] cells;
        public WordSlotData[] wordSlots;
        public string[] cellHints; // parallel to cells[]; "" or single uppercase letter
        public Difficulty difficulty;

        public CellData GetCell(int row, int col) =>
            cells[row * gridWidth + col];

        public void SetCell(int row, int col, CellData data) =>
            cells[row * gridWidth + col] = data;

        public int CellIndex(int row, int col) =>
            row * gridWidth + col;

        public bool InBounds(int row, int col) =>
            row >= 0 && row < gridHeight && col >= 0 && col < gridWidth;
    }
}
