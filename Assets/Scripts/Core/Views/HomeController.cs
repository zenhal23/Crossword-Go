using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CrosswordGo
{
    // Home scene: shows current level number and launches it when Play is pressed.
    // Level index is stored in PlayerPrefs so progress survives app restarts.
    public class HomeController : MonoBehaviour
    {
        [SerializeField] private LevelLibrary levelLibrary;
        [SerializeField] private Button playButton;
        [SerializeField] private TextMeshProUGUI levelLabel;

        private const string LevelIndexKey = "LevelIndex";

        private void Start()
        {
            RefreshLabel();
            playButton.onClick.AddListener(OnPlayClicked);
        }

        private void RefreshLabel()
        {
            int idx = PlayerPrefs.GetInt(LevelIndexKey, 0);
            levelLabel.text = $"Level {levelLibrary.DisplayNumber(idx)}";
        }

        private void OnPlayClicked()
        {
            int idx = PlayerPrefs.GetInt(LevelIndexKey, 0);
            var level = levelLibrary.GetLevel(idx);
            if (level == null)
            {
                Debug.LogError("HomeController: LevelLibrary is empty — assign levels in the inspector.");
                return;
            }
            LevelManager.PendingLevel = level;
            SceneManager.LoadScene("Game");
        }
    }
}
