using System;
using UnityEngine;

namespace CrosswordGo
{
    [Serializable]
    public struct WordSlotData
    {
        public int id;
        public Direction direction;
        public int clueRow;
        public int clueCol;
        public int startRow;
        public int startCol;
        public int length;
        public string answer;
        public string clue;
        public WordClueType clueType;
        public Sprite clueSprite;
    }
}
