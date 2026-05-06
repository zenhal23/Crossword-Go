using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CrosswordGo
{
    // Home scene: lists all LevelData assets and lets player pick one.
    public class LevelSelectView : MonoBehaviour
    {
        [SerializeField] private Transform listContainer;
        [SerializeField] private GameObject levelItemPrefab;
        [SerializeField] private LevelData[] levels; // Drag assets in Inspector

        private void Start()
        {
            foreach (var level in levels)
            {
                var go = Instantiate(levelItemPrefab, listContainer);
                var label = go.GetComponentInChildren<TextMeshProUGUI>();
                if (label != null)
                    label.text = $"{level.title}  ({level.difficulty})";

                var btn = go.GetComponentInChildren<Button>();
                if (btn != null)
                {
                    var captured = level;
                    btn.onClick.AddListener(() => StartLevel(captured));
                }
            }
        }

        private void StartLevel(LevelData level)
        {
            LevelManager.PendingLevel = level;
            SceneManager.LoadScene("Game");
        }
    }
}
