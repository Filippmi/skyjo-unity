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
    private GameObject _drawnCardVisual; // card shown in hand (from draw or take discard)
    private bool _drawnFromDeck; // true if current drawn card came from deck, false if from discard

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
        if (_animating || !_game.ShowDiscardDrawnButton) return;
        StartCoroutine(AnimateDiscardDrawn());
    }

    public void OnNewRound()
    {
        if (_drawnCardVisual != null) { Destroy(_drawnCardVisual); _drawnCardVisual = null; }
        ClearDiscardStack();
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
        // Flip only: animate flip then apply
        var cell = _game.GetCell(index);
        if (!cell.FaceUp && !cell.Removed)
        {
            StartCoroutine(AnimateFlipGridCard(index));
            return;
        }
        if (_game.ReplaceOrFlip(index))
            RefreshUI();
    }

    private void RefreshUI()
    {
        if (scoreText) scoreText.text = "Score: " + _game.GetScore();
        if (drawnCardText) drawnCardText.gameObject.SetActive(false); // drawn card shown as visual, not text

        var top = _game.TopDiscard;
        if (discardTopText)
            discardTopText.text = top.HasValue ? top.Value.ToString() : "";
        if (discardPileButton)
        {
            var img = discardPileButton.GetComponent<UnityEngine.UI.Image>();
            if (img) img.color = top.HasValue ? CardSlotView.GetCardColor(top.Value) : new Color(0.3f, 0.4f, 0.55f);
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
        if (_drawnCardVisual != null) { Destroy(_drawnCardVisual); _drawnCardVisual = null; }

        var canvas = GetCanvas();
        if (canvas == null) { _animating = false; RefreshUI(); SetButtonsInteractable(true); yield break; }

        var drawPileRect = drawPileButton != null ? drawPileButton.GetComponent<RectTransform>() : null;
        if (drawPileRect == null) { _animating = false; RefreshUI(); SetButtonsInteractable(true); yield break; }

        var card = CreateFlyingCard(canvas.transform);
        card.position = GetWorldPosition(drawPileRect);
        card.SetParent(canvas.transform, true);
        SetFlyingCardFace(card, false, 0);
        int drawnValue = _game.DrawnCard ?? 0;

        yield return StartCoroutine(FlipFlyingCard(card, drawnValue));

        if (card != null)
        {
            // Keep card on top of draw pile (don't move to drawn holder)
            _drawnCardVisual = card.gameObject;
            _drawnFromDeck = true;
        }
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

        if (_drawnCardVisual != null) { Destroy(_drawnCardVisual); _drawnCardVisual = null; }
        _drawnCardVisual = card.gameObject;
        _drawnFromDeck = false;
        RefreshUI();
        _animating = false;
        SetButtonsInteractable(true);
    }

    private IEnumerator AnimateDiscardDrawn()
    {
        _animating = true;
        SetButtonsInteractable(false);
        if (_drawnCardVisual == null) { _game.DiscardDrawnAndFlip(); RefreshUI(); _animating = false; SetButtonsInteractable(true); yield break; }

        var canvas = GetCanvas();
        var discardRect = discardPileButton != null ? discardPileButton.GetComponent<RectTransform>() : null;
        if (canvas == null || discardRect == null) { Destroy(_drawnCardVisual); _drawnCardVisual = null; _game.DiscardDrawnAndFlip(); RefreshUI(); _animating = false; SetButtonsInteractable(true); yield break; }

        var cardRect = _drawnCardVisual.GetComponent<RectTransform>();
        Vector3 startPos = GetWorldPosition(cardRect);
        Vector3 endPos = GetWorldPosition(discardRect);
        float elapsed = 0f;

        while (elapsed < ReplaceAnimDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / ReplaceAnimDuration);
            t = t * t * (3f - 2f * t);
            cardRect.position = Vector3.Lerp(startPos, endPos, t);
            yield return null;
        }

        _game.DiscardDrawnAndFlip();
        AddCardToDiscardStack(_drawnCardVisual.transform, discardRect);
        _drawnCardVisual = null;
        RefreshUI();
        _animating = false;
        SetButtonsInteractable(true);
    }

    private const int MaxDiscardStackCards = 15;
    private const float DiscardStackAngleRange = 14f;
    private const float DiscardStackOffsetRange = 6f;

    private void ClearDiscardStack()
    {
        if (discardPileButton == null) return;
        for (int i = discardPileButton.transform.childCount - 1; i >= 0; i--)
        {
            var c = discardPileButton.transform.GetChild(i);
            if (c.name == "DiscardCard") Object.Destroy(c.gameObject);
        }
    }

    private void AddCardToDiscardStack(Transform cardTransform, RectTransform discardParent)
    {
        if (cardTransform == null || discardParent == null) return;
        cardTransform.SetParent(discardParent, true);
        cardTransform.name = "DiscardCard";
        var rect = cardTransform.GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.anchoredPosition = new Vector2(
                Random.Range(-DiscardStackOffsetRange, DiscardStackOffsetRange),
                Random.Range(-DiscardStackOffsetRange, DiscardStackOffsetRange));
            rect.localEulerAngles = new Vector3(0f, 0f, Random.Range(-DiscardStackAngleRange, DiscardStackAngleRange));
        }
        int discardCards = 0;
        for (int i = 0; i < discardParent.childCount; i++)
            if (discardParent.GetChild(i).name == "DiscardCard") discardCards++;
        while (discardCards > MaxDiscardStackCards)
        {
            for (int i = 0; i < discardParent.childCount; i++)
            {
                var c = discardParent.GetChild(i);
                if (c.name == "DiscardCard") { Object.Destroy(c.gameObject); discardCards--; break; }
            }
        }
    }

    private IEnumerator AnimateFlipGridCard(int slotIndex)
    {
        _animating = true;
        SetButtonsInteractable(false);

        var cell = _game.GetCell(slotIndex);
        if (cell.FaceUp || cell.Removed) { _animating = false; SetButtonsInteractable(true); yield break; }

        var canvas = GetCanvas();
        var slotRect = slotViews != null && slotIndex < slotViews.Length && slotViews[slotIndex] != null
            ? slotViews[slotIndex].GetComponent<RectTransform>()
            : null;
        if (canvas == null || slotRect == null) { _animating = false; SetButtonsInteractable(true); yield break; }

        var card = CreateFlyingCard(canvas.transform);
        card.position = GetWorldPosition(slotRect);
        card.SetParent(canvas.transform, true);
        SetFlyingCardFace(card, false, 0);
        int value = cell.Value;

        yield return StartCoroutine(FlipFlyingCard(card, value));

        if (card != null) Object.Destroy(card.gameObject);
        _game.ReplaceOrFlip(slotIndex);
        RefreshUI();
        _animating = false;
        SetButtonsInteractable(true);
    }

    private IEnumerator FlipFlyingCard(RectTransform card, int value)
    {
        if (card == null) yield break;
        float duration = 0.15f;
        float elapsed = 0f;
        Vector3 scale = card.localScale;
        while (elapsed < duration)
        {
            if (card == null) yield break;
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            scale.x = 1f - t;
            card.localScale = scale;
            yield return null;
        }
        if (card == null) yield break;
        SetFlyingCardFace(card, true, value);
        scale.x = 0f;
        card.localScale = scale;
        elapsed = 0f;
        while (elapsed < duration)
        {
            if (card == null) yield break;
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            scale.x = t;
            card.localScale = scale;
            yield return null;
        }
        if (card != null) { scale.x = 1f; card.localScale = scale; }
    }

    private IEnumerator AnimateReplace(int slotIndex)
    {
        _animating = true;
        SetButtonsInteractable(false);

        var canvas = GetCanvas();
        var slotRect = slotViews != null && slotIndex < slotViews.Length && slotViews[slotIndex] != null
            ? slotViews[slotIndex].GetComponent<RectTransform>()
            : null;
        var drawPileRect = drawPileButton != null ? drawPileButton.GetComponent<RectTransform>() : null;
        var discardRect = discardPileButton != null ? discardPileButton.GetComponent<RectTransform>() : null;
        var drawnRect = GetDrawnCardHolderRect();
        if (canvas == null || slotRect == null || drawPileRect == null || discardRect == null || drawnRect == null)
        {
            if (_game.ReplaceOrFlip(slotIndex)) RefreshUI();
            _animating = false;
            SetButtonsInteractable(true);
            yield break;
        }

        var cell = _game.GetCell(slotIndex);
        int outgoingValue = cell.Value;
        int incomingValue = _game.DrawnCard ?? 0;
        Vector3 sourcePos = _drawnFromDeck ? GetWorldPosition(drawPileRect) : GetWorldPosition(discardRect);

        if (slotViews != null && slotIndex < slotViews.Length && slotViews[slotIndex] != null)
            slotViews[slotIndex].SetReplacing(true);

        var cardOut = CreateFlyingCard(canvas.transform);
        cardOut.position = GetWorldPosition(slotRect);
        cardOut.SetParent(canvas.transform, true);
        SetFlyingCardFace(cardOut, true, outgoingValue); // discard pile always shows cards face-up

        RectTransform cardIn = null;
        bool usingDrawnVisual = _drawnCardVisual != null;
        if (usingDrawnVisual)
        {
            cardIn = _drawnCardVisual.GetComponent<RectTransform>();
            _drawnCardVisual = null;
            cardIn.position = sourcePos; // start from draw pile or discard pile
        }
        else
        {
            cardIn = CreateFlyingCard(canvas.transform);
            cardIn.position = sourcePos;
            cardIn.SetParent(canvas.transform, true);
            SetFlyingCardFace(cardIn, true, incomingValue);
        }

        Vector3 slotPos = GetWorldPosition(slotRect);
        Vector3 discardPos = GetWorldPosition(discardRect);

        float elapsed = 0f;
        while (elapsed < ReplaceAnimDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / ReplaceAnimDuration);
            t = t * t * (3f - 2f * t);
            cardOut.position = Vector3.Lerp(slotPos, discardPos, t);
            cardIn.position = Vector3.Lerp(sourcePos, slotPos, t);
            yield return null;
        }

        _game.ReplaceOrFlip(slotIndex);
        AddCardToDiscardStack(cardOut.transform, discardRect);
        Destroy(cardIn.gameObject);
        if (slotViews != null && slotIndex < slotViews.Length && slotViews[slotIndex] != null)
            slotViews[slotIndex].SetReplacing(false);
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
