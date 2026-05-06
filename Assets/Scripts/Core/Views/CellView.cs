using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CrosswordGo
{
    public enum CellVisualState
    {
        Black, Clue, Empty, Pending, LockedPlayer, LockedBot, SlotComplete, Hint
    }

    // One cell in the grid. Attached to a prefab instantiated by GridView.
    public class CellView : MonoBehaviour,
        IPointerClickHandler, IDropHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private Image background;
        [SerializeField] private TextMeshProUGUI letterText;
        [SerializeField] private TextMeshProUGUI acrossClueText;
        [SerializeField] private TextMeshProUGUI downClueText;
        [SerializeField] private GameObject divider;
        [SerializeField] private Image clueImage;

        [Header("Clue Font Sizes")]
        [SerializeField] private float clueFontSizeSingle = 18f;
        [SerializeField] private float clueFontSizeDual   = 11f;

        [Header("Colors")]
        [SerializeField] private Color colorBlack       = new Color(0.06f, 0.09f, 0.16f);
        [SerializeField] private Color colorEmpty       = new Color(0.12f, 0.16f, 0.20f);
        [SerializeField] private Color colorClue        = new Color(0.22f, 0.25f, 0.32f);
        [SerializeField] private Color colorPending     = new Color(0.98f, 0.80f, 0.08f);
        [SerializeField] private Color colorDragHover   = new Color(0.23f, 0.51f, 0.96f);
        [SerializeField] private Color colorLockedPlayer = new Color(0.13f, 0.77f, 0.37f);
        [SerializeField] private Color colorLockedBot   = new Color(0.94f, 0.27f, 0.27f);
        [SerializeField] private Color colorComplete    = new Color(0.15f, 0.39f, 0.92f);
        [SerializeField] private Color colorHint        = new Color(0.55f, 0.55f, 0.65f);

        public int CellIndex { get; private set; }
        public System.Action<int> OnTapped;
        public System.Action<int, LetterTile> OnLetterDropped;

        private CellVisualState _currentState = CellVisualState.Empty;

        public void Setup(int cellIndex, CellData data, WordSlotData? acrossSlot, WordSlotData? downSlot)
        {
            CellIndex = cellIndex;

            switch (data.cellType)
            {
                case CellType.Black:
                    SetVisual(CellVisualState.Black, '\0', null, Direction.Across);
                    break;
                case CellType.Clue:
                    SetupClue(acrossSlot, downSlot);
                    break;
                case CellType.Answer:
                    SetVisual(CellVisualState.Empty, '\0', null, Direction.Across);
                    break;
            }
        }

        private void SetupClue(WordSlotData? acrossSlot, WordSlotData? downSlot)
        {
            _currentState = CellVisualState.Clue;
            background.color = colorClue;
            letterText.gameObject.SetActive(false);
            clueImage.gameObject.SetActive(false);

            bool acrossIsImage = acrossSlot.HasValue && acrossSlot.Value.clueType == WordClueType.Image
                                                     && acrossSlot.Value.clueSprite != null;
            bool downIsImage   = downSlot.HasValue   && downSlot.Value.clueType   == WordClueType.Image
                                                     && downSlot.Value.clueSprite != null;

            bool hasAcross = acrossSlot.HasValue &&
                             (acrossIsImage || !string.IsNullOrEmpty(acrossSlot.Value.clue));
            bool hasDown   = downSlot.HasValue   &&
                             (downIsImage   || !string.IsNullOrEmpty(downSlot.Value.clue));

            float fontSize = (hasAcross && hasDown) ? clueFontSizeDual : clueFontSizeSingle;

            if (acrossClueText != null)
            {
                acrossClueText.gameObject.SetActive(hasAcross && !acrossIsImage);
                if (hasAcross && !acrossIsImage)
                {
                    acrossClueText.fontSizeMax = fontSize;
                    acrossClueText.text = $"► {acrossSlot.Value.clue}";
                }
            }

            if (downClueText != null)
            {
                downClueText.gameObject.SetActive(hasDown && !downIsImage);
                if (hasDown && !downIsImage)
                {
                    downClueText.fontSizeMax = fontSize;
                    downClueText.text = $"▼ {downSlot.Value.clue}";
                }
            }

            // Show sprite for whichever slot is image-type (across takes priority if both)
            if (acrossIsImage || downIsImage)
            {
                var imageSlot = acrossIsImage ? acrossSlot.Value : downSlot.Value;
                clueImage.sprite = imageSlot.clueSprite;
                clueImage.gameObject.SetActive(true);
            }

            if (divider != null)
                divider.SetActive(hasAcross && hasDown);
        }

        public void SetLetter(char letter, bool isPending)
        {
            _currentState = isPending ? CellVisualState.Pending : CellVisualState.Empty;
            letterText.gameObject.SetActive(true);
            letterText.text = letter == '\0' ? "" : letter.ToString();
            background.color = isPending ? colorPending : colorEmpty;
        }

        public void SetLocked(int owner)
        {
            if (owner == 0)
            {
                _currentState = CellVisualState.Hint;
                background.color = colorHint;
            }
            else
            {
                _currentState = owner == 1 ? CellVisualState.LockedPlayer : CellVisualState.LockedBot;
                background.color = owner == 1 ? colorLockedPlayer : colorLockedBot;
            }
        }

        public void SetSlotComplete()
        {
            _currentState = CellVisualState.SlotComplete;
            background.color = colorComplete;
        }

        // ── event handlers ────────────────────────────────────────────────────

        public void OnPointerClick(PointerEventData eventData)
        {
            if (_currentState != CellVisualState.Empty && _currentState != CellVisualState.Pending) return;
            OnTapped?.Invoke(CellIndex);
        }

        public void OnDrop(PointerEventData eventData)
        {
            var tile = eventData.pointerDrag?.GetComponent<LetterTile>();
            if (tile != null)
                OnLetterDropped?.Invoke(CellIndex, tile);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (eventData.pointerDrag?.GetComponent<LetterTile>() == null) return;
            if (_currentState != CellVisualState.Empty && _currentState != CellVisualState.Pending) return;
            background.color = colorDragHover;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (eventData.pointerDrag?.GetComponent<LetterTile>() == null) return;
            if (_currentState != CellVisualState.Empty && _currentState != CellVisualState.Pending) return;
            background.color = _currentState == CellVisualState.Pending ? colorPending : colorEmpty;
        }

        private void SetVisual(CellVisualState state, char letter, Sprite img, Direction dir)
        {
            _currentState = state;
            if (acrossClueText != null) acrossClueText.gameObject.SetActive(false);
            if (downClueText   != null) downClueText.gameObject.SetActive(false);
            if (divider        != null) divider.SetActive(false);
            clueImage.gameObject.SetActive(false);

            background.color = state switch
            {
                CellVisualState.Black        => colorBlack,
                CellVisualState.Clue         => colorClue,
                CellVisualState.Pending      => colorPending,
                CellVisualState.LockedPlayer => colorLockedPlayer,
                CellVisualState.LockedBot    => colorLockedBot,
                CellVisualState.SlotComplete => colorComplete,
                CellVisualState.Hint         => colorHint,
                _                            => colorEmpty
            };

            bool showLetter = letter != '\0';
            letterText.gameObject.SetActive(showLetter);
            if (showLetter) letterText.text = letter.ToString();
        }
    }
}
