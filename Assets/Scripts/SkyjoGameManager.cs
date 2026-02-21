using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Drives the Skyjo game: owns SkyjoGame, wires UI, and refreshes display.
/// Assign UI references in the Inspector, or use Skyjo > Setup Scene to create them.
/// </summary>
public class SkyjoGameManager : MonoBehaviour
{
    private const float CardWidth = 72f;
    private const float CardHeight = 96f;
    private const float DrawAnimDuration = 0.5f;
    private const float ReplaceAnimDuration = 0.4f;

    [Header("Piles")]
    [SerializeField] private Button drawPileButton;
    [SerializeField] private Button discardPileButton;
    [SerializeField] private Text drawCountText;
    [SerializeField] private Text discardTopText;

    [Header("Grid")]
    [SerializeField] private CardSlotView[] slotViews = new CardSlotView[SkyjoGame.GridSize];

    [Header("Score & Messages")]
    [SerializeField] private Text scoreText;
    [SerializeField] private Text drawnCardText;
    [SerializeField] private Text messageText;

    [Header("Actions")]
    [SerializeField] private Button drawFromDeckButton;
    [SerializeField] private Button takeDiscardButton;
    [SerializeField] private Button discardDrawnButton;
    [SerializeField] private Button newRoundButton;

    [Header("Animation (optional)")]
    [SerializeField] private Canvas overlayCanvas;
    [Tooltip("Where the drawn card lands (left of board). If unset, uses Drawn Card Text position.")]
    [SerializeField] private RectTransform drawnCardHolder;

    private SkyjoGame _game;
    private bool _animating;

    private void Start()
    {
        _game = new SkyjoGame();
        if (drawPileButton) drawPileButton.onClick.AddListener(OnDrawFromDeck);
        if (discardPileButton) discardPileButton.onClick.AddListener(OnTakeDiscard);
        if (drawFromDeckButton) drawFromDeckButton.onClick.AddListener(OnDrawFromDeck);
        if (takeDiscardButton) takeDiscardButton.onClick.AddListener(OnTakeDiscard);
        if (discardDrawnButton) discardDrawnButton.onClick.AddListener(OnDiscardDrawn);
        if (newRoundButton) newRoundButton.onClick.AddListener(OnNewRound);

        for (int i = 0; i < slotViews.Length && i < SkyjoGame.GridSize; i++)
            if (slotViews[i]) slotViews[i].SlotIndex = i;

        RemoveDuplicateMessageTexts();

        _game.NewRound();
        RefreshUI();
    }

    /// <summary>
    /// Scene can end up with multiple MessageText objects (duplicate UI), causing stacked text. Keep exactly one and use it.
    /// </summary>
    private void RemoveDuplicateMessageTexts()
    {
        var all = FindObjectsOfType<Text>();
        var messageTexts = new List<Text>();
        foreach (var t in all)
        {
            if (t.gameObject.name == "MessageText")
                messageTexts.Add(t);
        }
        if (messageTexts.Count <= 1) return;

        // Keep the one we're already assigned to, or the first if ref is missing/wrong
        Text keep = (messageText != null && messageTexts.Contains(messageText)) ? messageText : messageTexts[0];
        foreach (var t in messageTexts)
        {
            if (t != keep)
                Destroy(t.gameObject);
        }
        messageText = keep;
    }

    public void OnDrawFromDeck()
    {
        if (_animating || !_game.CanDrawFromDeck) return;
        _game.DrawFromDeck();
        StartCoroutine(AnimateDrawFromDeck());
    }

    public void OnTakeDiscard()
    {
        if (_animating || !_game.CanTakeDiscard) return;
        _game.TakeFromDiscard();
        StartCoroutine(AnimateTakeFromDiscard());
    }

    public void OnDiscardDrawn()
    {
        _game.DiscardDrawnAndFlip();
        RefreshUI();
    }

    public void OnNewRound()
    {
        _game.NewRound();
        RefreshUI();
    }

    public void OnGridSlotClicked(int index)
    {
        if (_animating) return;
        if (!_game.ShowDiscardDrawnButton && !_game.WaitingForGridClick) return;
        if (index < 0 || index >= SkyjoGame.GridSize || _game.GetCell(index).Removed) return;

        // Swap: animate card out to discard and drawn card into slot, then apply
        if (_game.DrawnCard.HasValue)
        {
            StartCoroutine(AnimateReplace(index));
            return;
        }
        // Flip only: no animation
        if (_game.ReplaceOrFlip(index))
            RefreshUI();
    }

    private void RefreshUI()
    {
        if (scoreText) scoreText.text = "Score: " + _game.GetScore();
        if (drawCountText) drawCountText.text = "Draw\n" + _game.Deck.Count;

        if (discardTopText)
        {
            var top = _game.TopDiscard;
            discardTopText.text = top.HasValue ? top.Value.ToString() : "";
        }

        for (int i = 0; i < slotViews.Length && i < SkyjoGame.GridSize; i++)
            if (slotViews[i])
                slotViews[i].Set(_game.GetCell(i));

        // Buttons and message
        if (drawFromDeckButton) drawFromDeckButton.interactable = _game.CanDrawFromDeck;
        if (takeDiscardButton) takeDiscardButton.interactable = _game.CanTakeDiscard;
        if (drawPileButton) drawPileButton.interactable = _game.CanDrawFromDeck;
        if (discardPileButton) discardPileButton.interactable = _game.CanTakeDiscard;

        if (discardDrawnButton) discardDrawnButton.gameObject.SetActive(_game.ShowDiscardDrawnButton);
        if (newRoundButton) newRoundButton.gameObject.SetActive(_game.AllRevealed());

        // Drawn card: show value so player knows what they picked
        if (drawnCardText)
        {
            bool hasDrawn = _game.DrawnCard.HasValue;
            drawnCardText.gameObject.SetActive(hasDrawn);
            if (hasDrawn)
                drawnCardText.text = "Drawn: " + _game.DrawnCard.Value;
        }

        // Message: update the single MessageText (duplicates removed in Start)
        if (messageText != null && messageText.gameObject != null)
        {
            string msg;
            if (_game.AllRevealed())
                msg = "Round over! Your score: " + _game.GetScore();
            else if (_game.ShowDiscardDrawnButton)
                msg = "Swap with a grid card, or discard and flip one.";
            else if (_game.WaitingForGridClick)
                msg = "Click a card in your grid to replace or flip.";
            else
                msg = "Draw from deck or take the discard.";
            messageText.text = msg;
        }
    }

    private Canvas GetCanvas()
    {
        if (overlayCanvas != null) return overlayCanvas;
        var c = GetComponentInParent<Canvas>();
        return c != null ? c : FindObjectOfType<Canvas>();
    }

    private RectTransform CreateFlyingCard(Transform parent)
    {
        var go = new GameObject("FlyingCard");
        var rect = go.AddComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.sizeDelta = new Vector2(CardWidth, CardHeight);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);

        var img = go.AddComponent<Image>();
        img.raycastTarget = false;

        var labelGo = new GameObject("Label");
        var labelRect = labelGo.AddComponent<RectTransform>();
        labelRect.SetParent(rect, false);
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = labelRect.offsetMax = Vector2.zero;
        var label = labelGo.AddComponent<Text>();
        label.alignment = TextAnchor.MiddleCenter;
        label.fontSize = 22;
        label.raycastTarget = false;
        if (Font.GetOSInstalledFontNames().Length > 0)
            label.font = Font.CreateDynamicFontFromOSFont(Font.GetOSInstalledFontNames()[0], 22);

        return rect;
    }

    private void SetFlyingCardFace(RectTransform card, bool faceUp, int value)
    {
        var img = card.GetComponent<Image>();
        var label = card.GetComponentInChildren<Text>();
        if (faceUp)
        {
            img.color = CardSlotView.GetCardColor(value);
            if (label) { label.text = value.ToString(); label.color = Color.white; }
        }
        else
        {
            img.color = new Color(0.3f, 0.4f, 0.55f);
            if (label) { label.text = "?"; label.color = new Color(0.5f, 0.6f, 0.7f); }
        }
    }

    private Vector3 GetWorldPosition(RectTransform rt)
    {
        if (rt == null) return Vector3.zero;
        return rt.TransformPoint(Vector3.zero);
    }

    private RectTransform GetDrawnCardHolderRect()
    {
        if (drawnCardHolder != null) return drawnCardHolder;
        if (drawnCardText != null) return drawnCardText.rectTransform;
        return null;
    }

    private IEnumerator AnimateDrawFromDeck()
    {
        _animating = true;
        SetButtonsInteractable(false);

        var canvas = GetCanvas();
        if (canvas == null) { _animating = false; RefreshUI(); SetButtonsInteractable(true); yield break; }

        var fromRect = drawPileButton != null ? drawPileButton.GetComponent<RectTransform>() : null;
        var toRect = GetDrawnCardHolderRect();
        if (fromRect == null || toRect == null) { _animating = false; RefreshUI(); SetButtonsInteractable(true); yield break; }

        var card = CreateFlyingCard(canvas.transform);
        card.position = GetWorldPosition(fromRect);
        card.SetParent(canvas.transform, true);
        SetFlyingCardFace(card, false, 0);
        int drawnValue = _game.DrawnCard ?? 0;

        float elapsed = 0f;
        Vector3 startPos = card.position;
        Vector3 endPos = GetWorldPosition(toRect);
        float flipAt = DrawAnimDuration * 0.5f;
        bool flipped = false;

        while (elapsed < DrawAnimDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / DrawAnimDuration);
            t = t * t * (3f - 2f * t); // smoothstep
            card.position = Vector3.Lerp(startPos, endPos, t);

            if (!flipped && elapsed >= flipAt)
            {
                flipped = true;
                StartCoroutine(FlipFlyingCard(card, drawnValue));
            }
            yield return null;
        }

        Destroy(card.gameObject);
        RefreshUI();
        _animating = false;
        SetButtonsInteractable(true);
    }

    private IEnumerator AnimateTakeFromDiscard()
    {
        _animating = true;
        SetButtonsInteractable(false);

        var canvas = GetCanvas();
        var fromRect = discardPileButton != null ? discardPileButton.GetComponent<RectTransform>() : null;
        var toRect = GetDrawnCardHolderRect();
        if (canvas == null || fromRect == null || toRect == null)
        {
            _animating = false;
            RefreshUI();
            SetButtonsInteractable(true);
            yield break;
        }

        int value = _game.DrawnCard ?? 0;
        var card = CreateFlyingCard(canvas.transform);
        card.position = GetWorldPosition(fromRect);
        card.SetParent(canvas.transform, true);
        SetFlyingCardFace(card, true, value);

        Vector3 startPos = card.position;
        Vector3 endPos = GetWorldPosition(toRect);
        float elapsed = 0f;

        while (elapsed < DrawAnimDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / DrawAnimDuration);
            t = t * t * (3f - 2f * t);
            card.position = Vector3.Lerp(startPos, endPos, t);
            yield return null;
        }

        Destroy(card.gameObject);
        RefreshUI();
        _animating = false;
        SetButtonsInteractable(true);
    }

    private IEnumerator FlipFlyingCard(RectTransform card, int value)
    {
        float duration = 0.15f;
        float elapsed = 0f;
        Vector3 scale = card.localScale;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            scale.x = 1f - t;
            card.localScale = scale;
            yield return null;
        }
        SetFlyingCardFace(card, true, value);
        scale.x = 0f;
        card.localScale = scale;
        elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            scale.x = t;
            card.localScale = scale;
            yield return null;
        }
        scale.x = 1f;
        card.localScale = scale;
    }

    private IEnumerator AnimateReplace(int slotIndex)
    {
        _animating = true;
        SetButtonsInteractable(false);

        var canvas = GetCanvas();
        var slotRect = slotViews != null && slotIndex < slotViews.Length && slotViews[slotIndex] != null
            ? slotViews[slotIndex].GetComponent<RectTransform>()
            : null;
        var discardRect = discardPileButton != null ? discardPileButton.GetComponent<RectTransform>() : null;
        var drawnRect = GetDrawnCardHolderRect();
        if (canvas == null || slotRect == null || discardRect == null || drawnRect == null)
        {
            if (_game.ReplaceOrFlip(slotIndex)) RefreshUI();
            _animating = false;
            SetButtonsInteractable(true);
            yield break;
        }

        var cell = _game.GetCell(slotIndex);
        int outgoingValue = cell.Value;
        int incomingValue = _game.DrawnCard ?? 0;

        var cardOut = CreateFlyingCard(canvas.transform);
        cardOut.position = GetWorldPosition(slotRect);
        cardOut.SetParent(canvas.transform, true);
        SetFlyingCardFace(cardOut, cell.FaceUp, outgoingValue);

        var cardIn = CreateFlyingCard(canvas.transform);
        cardIn.position = GetWorldPosition(drawnRect);
        cardIn.SetParent(canvas.transform, true);
        SetFlyingCardFace(cardIn, true, incomingValue);

        Vector3 slotPos = GetWorldPosition(slotRect);
        Vector3 discardPos = GetWorldPosition(discardRect);
        Vector3 drawnPos = GetWorldPosition(drawnRect);

        float elapsed = 0f;
        while (elapsed < ReplaceAnimDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / ReplaceAnimDuration);
            t = t * t * (3f - 2f * t);
            cardOut.position = Vector3.Lerp(slotPos, discardPos, t);
            cardIn.position = Vector3.Lerp(drawnPos, slotPos, t);
            yield return null;
        }

        Destroy(cardOut.gameObject);
        Destroy(cardIn.gameObject);
        _game.ReplaceOrFlip(slotIndex);
        RefreshUI();
        _animating = false;
        SetButtonsInteractable(true);
    }

    private void SetButtonsInteractable(bool on)
    {
        if (drawFromDeckButton) drawFromDeckButton.interactable = on && _game.CanDrawFromDeck;
        if (takeDiscardButton) takeDiscardButton.interactable = on && _game.CanTakeDiscard;
        if (drawPileButton) drawPileButton.interactable = on && _game.CanDrawFromDeck;
        if (discardPileButton) discardPileButton.interactable = on && _game.CanTakeDiscard;
        if (discardDrawnButton) discardDrawnButton.interactable = on;
    }
}
