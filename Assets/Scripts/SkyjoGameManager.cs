using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Drives the Skyjo game: owns SkyjoGame, wires UI, and refreshes display.
/// Assign UI references in the Inspector, or use Skyjo > Setup Scene to create them.
/// </summary>
public class SkyjoGameManager : MonoBehaviour
{
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

    private SkyjoGame _game;

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
        _game.DrawFromDeck();
        RefreshUI();
    }

    public void OnTakeDiscard()
    {
        _game.TakeFromDiscard();
        RefreshUI();
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
        // Allow when we have a drawn card to swap, or when we need to pick a target (replace/flip)
        if (!_game.ShowDiscardDrawnButton && !_game.WaitingForGridClick) return;
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
}
