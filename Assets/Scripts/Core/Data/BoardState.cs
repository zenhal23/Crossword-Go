using System.Collections.Generic;

namespace CrosswordGo
{
    public class BoardState
    {
        public readonly char[] playerFill;
        public readonly char[] botFill;
        public readonly bool[] lockedCells;
        public readonly int[] cellOwner;
        public readonly HashSet<int> completedSlotIds = new HashSet<int>();

        public BoardState(int cellCount)
        {
            playerFill = new char[cellCount];
            botFill = new char[cellCount];
            lockedCells = new bool[cellCount];
            cellOwner = new int[cellCount];
        }

        public void LockCell(int index, int owner)
        {
            lockedCells[index] = true;
            cellOwner[index] = owner;
            playerFill[index] = '\0';
            botFill[index] = '\0';
        }

        public void SetPending(int index, char letter, int owner)
        {
            if (owner == 1) playerFill[index] = letter;
            else botFill[index] = letter;
        }

        public void ClearPending(int owner)
        {
            var fill = owner == 1 ? playerFill : botFill;
            for (int i = 0; i < fill.Length; i++) fill[i] = '\0';
        }
    }
}
