using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CrosswordGo
{
    public class ScoreboardView : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI playerLabel;
        [SerializeField] private TextMeshProUGUI playerScore;
        [SerializeField] private TextMeshProUGUI opponentLabel;
        [SerializeField] private TextMeshProUGUI opponentScore;
        [SerializeField] private Image playerHighlight;
        [SerializeField] private Image opponentHighlight;
        [SerializeField] private Color activeColor = Color.white;
        [SerializeField] private Color inactiveColor = new Color(0.7f, 0.7f, 0.7f);

        public void SetLabels(string player, string opponent)
        {
            playerLabel.text = player;
            opponentLabel.text = opponent;
            UpdateScores(0, 0);
        }

        public void UpdateScores(int pScore, int bScore)
        {
            playerScore.text = pScore.ToString();
            opponentScore.text = bScore.ToString();
        }

        // activePlayer: 0 = human, 1 = bot
        public void SetActivePlayer(int activePlayer)
        {
            bool playerActive = activePlayer == 0;
            playerHighlight.color = playerActive ? activeColor : inactiveColor;
            opponentHighlight.color = playerActive ? inactiveColor : activeColor;
        }
    }
}
