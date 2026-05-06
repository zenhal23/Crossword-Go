using System;

namespace CrosswordGo
{
    [Serializable]
    public struct CellData
    {
        public CellType cellType;
        public int acrossSlotId; // -1 if this cell is not an Across clue
        public int downSlotId;   // -1 if this cell is not a Down clue
    }
}
