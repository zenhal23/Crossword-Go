using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CrosswordGo
{
    // Draggable letter tile in the hand tray.
    // Reparents to the root canvas during drag so it renders above everything.
    public class LetterTile : MonoBehaviour,
        IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField] private TextMeshProUGUI letterLabel;
        [SerializeField] private Image background;
        [SerializeField] private Color normalColor  = new Color(0.93f, 0.78f, 0.45f);
        [SerializeField] private Color draggingColor = new Color(1f, 0.95f, 0.55f);

        public char Letter { get; private set; }

        // Fired when drag begins/ends (payload = this tile)
        public Action<LetterTile> OnBeginDragged;
        public Action<LetterTile> OnEndDragged;

        private RectTransform _rect;
        private CanvasGroup   _group;
        private Canvas        _rootCanvas;
        private Transform     _originalParent;
        private int           _originalSiblingIndex;

        private void Awake()
        {
            _rect  = GetComponent<RectTransform>();
            _group = GetComponent<CanvasGroup>();
            if (_group == null)
                _group = gameObject.AddComponent<CanvasGroup>();
        }

        public void Setup(char letter)
        {
            Letter = letter;
            letterLabel.text = letter.ToString();
            background.color = normalColor;
        }

        // ── drag handlers ─────────────────────────────────────────────────────

        public void OnBeginDrag(PointerEventData eventData)
        {
            _rootCanvas = GetComponentInParent<Canvas>().rootCanvas;

            _originalParent       = transform.parent;
            _originalSiblingIndex = transform.GetSiblingIndex();

            // Lift to root canvas so the tile renders above the grid
            transform.SetParent(_rootCanvas.transform, worldPositionStays: true);
            transform.SetAsLastSibling();

            // Let raycasts pass through so IDropHandler fires on cells beneath
            _group.blocksRaycasts = false;
            background.color = draggingColor;

            OnBeginDragged?.Invoke(this);
        }

        public void OnDrag(PointerEventData eventData)
        {
            RectTransformUtility.ScreenPointToWorldPointInRectangle(
                _rootCanvas.transform as RectTransform,
                eventData.position,
                eventData.pressEventCamera,
                out var worldPos);
            transform.position = worldPos;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            _group.blocksRaycasts = true;
            OnEndDragged?.Invoke(this);
        }

        // Called by PlayerInputController when no valid cell was dropped on
        public void ReturnToHand()
        {
            background.color = normalColor;
            transform.SetParent(_originalParent, worldPositionStays: true);
            transform.SetSiblingIndex(_originalSiblingIndex);
        }

        public void SetInteractable(bool v)
        {
            _group.interactable   = v;
            _group.blocksRaycasts = v;
        }
    }
}
