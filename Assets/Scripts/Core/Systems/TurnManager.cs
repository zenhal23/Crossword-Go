using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CrosswordGo
{
    public class TurnManager : MonoBehaviour
    {
        // Wired by LevelManager
        public LevelData Level { get; set; }
        public BoardState Board { get; set; }
        public PlayerState PlayerState { get; set; }
        public PlayerState BotState { get; set; }

        [SerializeField] private BotController botController;

        private readonly ScoringSystem _scoring = new ScoringSystem();
        private readonly LetterPool _pool = new LetterPool();
        private GameStateManager _gsm;
        private bool _playerSubmitted;
        private List<(int, char)> _pendingPlayerPlacements = new List<(int, char)>();

        // Events consumed by views
        public event Action<List<char>, bool> OnHandRefreshed;       // hand, isPlayerTurn
        public event Action<int, int> OnScoresUpdated;               // playerScore, botScore
        public event Action<int, int> OnCellLocked;                  // cellIndex, ownerPlayer
        public event Action<int> OnCellPendingCleared;              // cellIndex (wrong placement returned)
        public event Action<int> OnSlotCompleted;                    // slotId
        public event Action<bool> OnTurnStarted;                     // isPlayerTurn
        public event Action<bool, int, int> OnGameOver;              // playerWon, playerScore, botScore

        private void Awake() => _gsm = GetComponent<GameStateManager>();

        public void StartMatch()
        {
            _pool.Initialize(Level, Board);
            _pool.OnPoolEmpty += HandlePoolEmpty;
            DealInitialHands();
            StartCoroutine(RunTurn(isPlayerTurn: true));
        }

        private void DealInitialHands()
        {
            PlayerState.hand.AddRange(_pool.Deal(5));
            BotState.hand.AddRange(_pool.Deal(5));
            OnHandRefreshed?.Invoke(new List<char>(PlayerState.hand), true);
        }

        // Called by PlayerInputController when player submits placements
        public void PlayerSubmit(List<(int cellIndex, char letter)> placements)
        {
            _pendingPlayerPlacements = placements;
            _playerSubmitted = true;
        }

        // Called by PlayerInputController when player passes
        public void PlayerPass() => _playerSubmitted = true;

        private IEnumerator RunTurn(bool isPlayerTurn)
        {
            _gsm.SetPhase(isPlayerTurn ? GamePhase.PlayerTurn : GamePhase.BotTurn);
            OnTurnStarted?.Invoke(isPlayerTurn);

            if (isPlayerTurn)
                yield return StartCoroutine(RunPlayerTurn());
            else
                yield return StartCoroutine(RunBotTurn());
        }

        private IEnumerator RunPlayerTurn()
        {
            _playerSubmitted = false;
            _pendingPlayerPlacements.Clear();

            // Auto-pass if player has nothing to place
            if (PlayerState.hand.Count == 0)
                _playerSubmitted = true;

            while (!_playerSubmitted)
                yield return null;

            ApplyTurn(PlayerState, _pendingPlayerPlacements, playerOwner: 1);
            RefillHand(PlayerState);
            OnHandRefreshed?.Invoke(new List<char>(PlayerState.hand), true);

            if (!CheckGameOver())
                yield return StartCoroutine(RunTurn(isPlayerTurn: false));
        }

        private IEnumerator RunBotTurn()
        {
            yield return StartCoroutine(
                botController.TakeTurn(Level, Board, BotState, placements =>
                {
                    ApplyTurn(BotState, placements, playerOwner: 2);
                    RefillHand(BotState);
                    OnHandRefreshed?.Invoke(new List<char>(PlayerState.hand), false);
                }));

            if (!CheckGameOver())
                yield return StartCoroutine(RunTurn(isPlayerTurn: true));
        }

        private void ApplyTurn(PlayerState state, List<(int, char)> placements, int playerOwner)
        {
            // Capture hand size before removing placed letters (used for efficiency bonus)
            int handSizeBeforeTurn = state.hand.Count;

            // Remove placed letters from hand — they were consumed this turn
            foreach (var (_, letter) in placements)
            {
                int i = state.hand.IndexOf(letter);
                if (i >= 0) state.hand.RemoveAt(i);
            }

            var result = _scoring.Evaluate(Level, Board, placements, playerOwner, handSize: handSizeBeforeTurn);

            state.score += result.scoreDelta;
            state.lastCorrectCount = result.correctCount;
            state.turnsPlayed++;

            // Return incorrect letters to hand
            state.hand.AddRange(result.returnedLetters);

            // Update grid: lock correct cells, clear wrong ones
            foreach (var (idx, _) in placements)
            {
                if (Board.lockedCells[idx])
                    OnCellLocked?.Invoke(idx, playerOwner);
                else
                    OnCellPendingCleared?.Invoke(idx);
            }

            foreach (var slotId in result.newlyCompletedSlotIds)
                OnSlotCompleted?.Invoke(slotId);

            OnScoresUpdated?.Invoke(PlayerState.score, BotState.score);
        }

        private void RefillHand(PlayerState state)
        {
            int need = 5 - state.hand.Count;
            if (need > 0 && _pool.Remaining > 0)
                state.hand.AddRange(_pool.Deal(need));
        }

        private bool CheckGameOver()
        {
            bool boardDone = IsBoardComplete();
            bool poolEmpty = _pool.Remaining == 0 &&
                             PlayerState.hand.Count == 0 &&
                             BotState.hand.Count == 0;

            if (!boardDone && !poolEmpty) return false;

            // Board-complete bonus (+50) is already awarded by ScoringSystem.Evaluate
            // to the player whose turn finished the board — do not add it again here.

            _gsm.SetPhase(GamePhase.Result);
            bool playerWon = PlayerState.score > BotState.score;
            OnGameOver?.Invoke(playerWon, PlayerState.score, BotState.score);
            return true;
        }

        private bool IsBoardComplete()
        {
            for (int i = 0; i < Level.cells.Length; i++)
                if (Level.cells[i].cellType == CellType.Answer && !Board.lockedCells[i])
                    return false;
            return true;
        }

        private void HandlePoolEmpty()
        {
            // Pool empty is handled in CheckGameOver per turn
        }
    }
}
