using System;
using System.Collections.Generic;
using UnityEngine;

namespace CrosswordGo
{
    [Serializable]
    public class WordClue
    {
        public string text = "";
        public WordClueType clueType = WordClueType.Text;
        public Sprite image; // optional
    }

    [Serializable]
    public class WordEntry
    {
        public string word = "";
        public List<WordClue> clues = new List<WordClue>();
    }
}
