using System.Collections.Generic;
using UnityEngine;

namespace CrosswordGo
{
    // Displays the letter tiles for the current turn.
    public class LetterHandView : MonoBehaviour
    {
        [SerializeField] private LetterTile tilePrefab;
        [SerializeField] private Transform tileContainer;

        private readonly List<LetterTile> _tiles = new List<LetterTile>();

        public System.Action<LetterTile> OnTileBeginDrag;
        public System.Action<LetterTile> OnTileEndDrag;

        public void Refresh(List<char> hand)
        {
            foreach (var t in _tiles) Destroy(t.gameObject);
            _tiles.Clear();

            foreach (char c in hand)
            {
                var tile = Instantiate(tilePrefab, tileContainer);
                tile.Setup(c);
                tile.OnBeginDragged = t => OnTileBeginDrag?.Invoke(t);
                tile.OnEndDragged   = t => OnTileEndDrag?.Invoke(t);
                _tiles.Add(tile);
            }
        }

        public void SetInteractable(bool v)
        {
            foreach (var t in _tiles) t.SetInteractable(v);
        }
    }
}
