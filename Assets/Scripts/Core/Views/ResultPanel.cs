using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace CrosswordGo
{
    public class ResultPanel : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private GameObject victoryImage;
        [SerializeField] private GameObject defeatImage;
        [SerializeField] private GameObject drawImage;
        [SerializeField] private TextMeshProUGUI playerScoreLabel;
        [SerializeField] private TextMeshProUGUI botScoreLabel;

        private bool _awaitingTap;

        public void Show(bool playerWon, int playerScore, int botScore)
        {
            gameObject.SetActive(true);
            

            if (playerScore == botScore)
            {
                drawImage.SetActive(true);
            }
            else
            {
                drawImage.SetActive(false);
                victoryImage.SetActive(playerWon);
                defeatImage.SetActive(!playerWon);
            }

            if (playerScoreLabel != null) playerScoreLabel.text = playerScore.ToString();
            if (botScoreLabel    != null) botScoreLabel.text    = botScore.ToString();

            StartCoroutine(EnableTapAfterDelay(0.6f));
        }

        private IEnumerator EnableTapAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            _awaitingTap = true;
        }

        public void OnPointerClick(PointerEventData eventData) => OnTapped();

        public void OnTapped()
        {
            if (!_awaitingTap) return;

            _awaitingTap = false;
            SceneManager.LoadScene("Home");
        }
    }
}
