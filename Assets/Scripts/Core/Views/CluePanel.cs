using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CrosswordGo
{
    // Two scroll lists (Across / Down) showing all clues, toggled by tab buttons.
    public class CluePanel : MonoBehaviour
    {
        [SerializeField] private Transform acrossContainer;
        [SerializeField] private Transform downContainer;
        [SerializeField] private GameObject clueItemPrefab;
        [SerializeField] private Button acrossTabButton;
        [SerializeField] private Button downTabButton;

        private readonly Dictionary<int, GameObject> _items = new Dictionary<int, GameObject>();

        private void Awake()
        {
            acrossTabButton?.onClick.AddListener(() => ShowTab(true));
            downTabButton?.onClick.AddListener(() => ShowTab(false));
        }

        private void ShowTab(bool showAcross)
        {
            acrossContainer.gameObject.SetActive(showAcross);
            downContainer.gameObject.SetActive(!showAcross);
        }

        public void Build(LevelData level)
        {
            foreach (Transform t in acrossContainer) Destroy(t.gameObject);
            foreach (Transform t in downContainer) Destroy(t.gameObject);
            _items.Clear();

            foreach (var slot in level.wordSlots)
            {
                var container = slot.direction == Direction.Across ? acrossContainer : downContainer;
                var go = Instantiate(clueItemPrefab, container);
                var label = go.GetComponentInChildren<TextMeshProUGUI>();
                if (label != null)
                    label.text = string.IsNullOrEmpty(slot.clue) ? $"[Image clue {slot.id}]" : slot.clue;

                // Show image if present
                if (slot.clueSprite != null)
                {
                    var img = go.GetComponentInChildren<Image>();
                    if (img != null) img.sprite = slot.clueSprite;
                }

                _items[slot.id] = go;
            }
        }

        public void MarkComplete(int slotId)
        {
            if (!_items.TryGetValue(slotId, out var go)) return;
            var label = go.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null) label.color = new Color(0.5f, 0.5f, 0.5f);
        }
    }
}
