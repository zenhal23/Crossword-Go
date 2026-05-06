using System;
using System.Collections.Generic;
using UnityEngine;

namespace CrosswordGo
{
    public class LetterPool
    {
        private readonly List<char> _pool = new List<char>();
        private readonly System.Random _rng = new System.Random();

        public int Remaining => _pool.Count;
        public event Action OnPoolEmpty;

        public void Initialize(LevelData level, BoardState board)
        {
            _pool.Clear();
            // One correct letter per unique unfilled answer cell
            var seen = new HashSet<int>();
            foreach (var slot in level.wordSlots)
            {
                for (int i = 0; i < slot.length; i++)
                {
                    int r = slot.direction == Direction.Across ? slot.startRow : slot.startRow + i;
                    int c = slot.direction == Direction.Across ? slot.startCol + i : slot.startCol;
                    int idx = r * level.gridWidth + c;
                    if (seen.Add(idx) && !board.lockedCells[idx])
                        _pool.Add(char.ToUpper(slot.answer[i]));
                }
            }
            Shuffle();
        }

        public List<char> Deal(int count)
        {
            var result = new List<char>();
            int take = Math.Min(count, _pool.Count);
            for (int i = 0; i < take; i++)
            {
                result.Add(_pool[0]);
                _pool.RemoveAt(0);
            }
            if (_pool.Count == 0) OnPoolEmpty?.Invoke();
            return result;
        }

        private void Shuffle()
        {
            for (int i = _pool.Count - 1; i > 0; i--)
            {
                int j = _rng.Next(i + 1);
                (_pool[i], _pool[j]) = (_pool[j], _pool[i]);
            }
        }
    }
}
