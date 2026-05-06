using System.Collections.Generic;

namespace CrosswordGo
{
    public class PlayerState
    {
        public int score;
        public int turnsPlayed;
        public int lastCorrectCount;
        public readonly List<char> hand = new List<char>();
        public readonly bool isHuman;

        public PlayerState(bool isHuman)
        {
            this.isHuman = isHuman;
        }
    }
}
