using UnityEngine;
using UnityEngine.SceneManagement;

namespace CrosswordGo
{
    // Entry point for the Game scene.
    // Owns LevelData, creates runtime state, wires all systems together.
    public class LevelManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private TurnManager turnManager;
        [SerializeField] private GameStateManager gameStateManager;
        [SerializeField] private GridView gridView;
        [SerializeField] private CluePanel cluePanel;
        [SerializeField] private LetterHandView handView;
        [SerializeField] private ScoreboardView scoreboardView;
        [SerializeField] private PlayerInputController playerInput;
        [SerializeField] private ResultPanel resultPanel;

        // Set by HomeScene before loading Game scene
        public static LevelData PendingLevel;

        private LevelData _level;
        private BoardState _board;
        private PlayerState _player;
        private PlayerState _bot;

        private void Start()
        {
            _level = PendingLevel;
            if (_level == null)
            {
                Debug.LogError("LevelManager: no level loaded. Set LevelManager.PendingLevel before entering Game scene.");
                return;
            }

            _board = new BoardState(_level.cells.Length);
            _player = new PlayerState(isHuman: true);
            _bot = new PlayerState(isHuman: false);

            // Wire turn manager
            turnManager.Level = _level;
            turnManager.Board = _board;
            turnManager.PlayerState = _player;
            turnManager.BotState = _bot;

            // Wire views
            gridView.Build(_level, _board);

            // Pre-lock hint cells (owner 0 = pre-filled, neutral color)
            if (_level.cellHints != null)
                for (int i = 0; i < _level.cellHints.Length; i++)
                    if (!string.IsNullOrEmpty(_level.cellHints[i]))
                    {
                        _board.LockCell(i, 0);
                        gridView.OnCellLocked(i, 0);
                    }

            cluePanel?.Build(_level);
            scoreboardView.SetLabels("You", "Opponent");

            // Wire events
            turnManager.OnHandRefreshed += (hand, isPlayer) =>
            {
                if (isPlayer) handView.Refresh(hand);
            };
            turnManager.OnScoresUpdated += scoreboardView.UpdateScores;
            turnManager.OnCellLocked += gridView.OnCellLocked;
            turnManager.OnCellPendingCleared += gridView.ClearPending;
            turnManager.OnSlotCompleted += (slotId) =>
            {
                gridView.OnSlotCompleted(slotId);
                cluePanel?.MarkComplete(slotId);
            };
            turnManager.OnTurnStarted += isPlayer =>
            {
                playerInput.SetEnabled(isPlayer);
                scoreboardView.SetActivePlayer(isPlayer ? 0 : 1);
            };
            turnManager.OnGameOver += (playerWon, pScore, bScore) =>
            {
                if (playerWon) AdvanceLevelIndex();
                resultPanel.Show(playerWon, pScore, bScore);
            };

            // Wire player input back to turn manager
            playerInput.OnSubmit += turnManager.PlayerSubmit;
            playerInput.Initialize(_level, _board);

            gameStateManager.SetPhase(GamePhase.LoadingLevel);
            turnManager.StartMatch();
        }

        public void ReturnToHome() =>
            SceneManager.LoadScene("Home");

        private static void AdvanceLevelIndex()
        {
            int next = PlayerPrefs.GetInt("LevelIndex", 0) + 1;
            PlayerPrefs.SetInt("LevelIndex", next);
            PlayerPrefs.Save();
        }
    }
}
