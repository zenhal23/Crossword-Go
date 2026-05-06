using System;
using UnityEngine;

namespace CrosswordGo
{
    [Serializable]
    public class SlotDraft
    {
        public Direction direction;
        public int clueRow; // row of the clue cell (word starts at clueRow+1 for Down, clueRow for Across)
        public int clueCol; // col of the clue cell (word starts at clueCol+1 for Across, clueCol for Down)
        public string answer = "";
        public string clue = "";
        public WordClueType clueType = WordClueType.Text;
        public Sprite clueImage; // optional image clue
    }
}
