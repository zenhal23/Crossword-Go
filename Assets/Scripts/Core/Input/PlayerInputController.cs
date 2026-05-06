using System;
using System.Collections.Generic;
using UnityEngine;

namespace CrosswordGo
{
    // Mediates between the player's input and TurnManager.
    // Flow: drag a letter tile from the hand → drop it on an answer cell (places pending).
    //       Tap a pending cell to return its tile to the hand.
    public class PlayerInputController : MonoBehaviour
    {
        [SerializeField] private GridView gridView;
        [SerializeField] private LetterHandView handView;
        [SerializeField] private UnityEngine.UI.Button submitButton;

        public Action<List<(int, char)>> OnSubmit;

        private LevelData _level;
        private BoardState _board;
        private bool _enabled;

        // cellIndex → letter placed there this turn
        private readonly Dictionary<int, char> _pending = new Dictionary<int, char>();
        // cellIndex → the tile hidden when placed (so it can be restored)
        private readonly Dictionary<int, LetterTile> _pendingTiles = new Dictionary<int, LetterTile>();

        // Set to true in OnCellDrop so OnTileEndDrag knows whether to hide or return the tile
        private bool _dropSucceeded;

        private void Awake()
        {
            if (submitButton != null)
                submitButton.onClick.AddListener(HandleSubmit);
        }

        public void Initialize(LevelData level, BoardState board)
        {
            _level = level;
            _board = board;

            gridView.RegisterTapHandler(OnCellTapped);
            gridView.RegisterDropHandler(OnCellDrop);
            handView.OnTileBeginDrag += OnTileBeginDrag;
            handView.OnTileEndDrag   += OnTileEndDrag;
        }

        public void SetEnabled(bool value)
        {
            _enabled = value;
            handView.SetInteractable(value);
            if (!value) ClearAllPending();
        }

        private void OnTileBeginDrag(LetterTile tile)
        {
            if (!_enabled) return;
            _dropSucceeded = false;
        }

        private void OnTileEndDrag(LetterTile tile)
        {
            if (!_enabled) return;
            if (_dropSucceeded)
                tile.gameObject.SetActive(false);
            else
                tile.ReturnToHand();
            _dropSucceeded = false;
        }

        private void OnCellDrop(int cellIndex, LetterTile tile)
        {
            if (!_enabled) return;
            if (_board.lockedCells[cellIndex]) return;
            if (_level.cells[cellIndex].cellType != CellType.Answer) return;

            // If this cell already has a pending tile, return it to the hand first
            if (_pendingTiles.TryGetValue(cellIndex, out var displaced))
            {
                _pendingTiles.Remove(cellIndex);
                _pending.Remove(cellIndex);
                displaced.gameObject.SetActive(true);
                displaced.ReturnToHand();
            }

            _pending[cellIndex] = tile.Letter;
            _pendingTiles[cellIndex] = tile;
            gridView.ShowPending(cellIndex, tile.Letter);
            _dropSucceeded = true;
        }

        // Tap on a pending cell to return its tile to the hand
        private void OnCellTapped(int cellIndex)
        {
            if (!_enabled) return;
            if (!_pending.ContainsKey(cellIndex)) return;

            _pending.Remove(cellIndex);
            gridView.ClearPending(cellIndex);
            if (_pendingTiles.TryGetValue(cellIndex, out var tile))
            {
                _pendingTiles.Remove(cellIndex);
                tile.gameObject.SetActive(true);
                tile.ReturnToHand();
            }
        }

        public void HandleSubmit()
        {
            if (!_enabled) return;
            var placements = new List<(int, char)>();
            foreach (var kvp in _pending)
                placements.Add((kvp.Key, kvp.Value));
            _pending.Clear();
            _pendingTiles.Clear();
            OnSubmit?.Invoke(placements);
        }

        private void ClearAllPending()
        {
            foreach (var kvp in _pendingTiles)
            {
                kvp.Value.gameObject.SetActive(true);
                kvp.Value.ReturnToHand();
            }
            _pendingTiles.Clear();
            foreach (int idx in _pending.Keys)
                gridView.ClearPending(idx);
            _pending.Clear();
        }
    }
}
