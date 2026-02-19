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

        _game.NewRound();
        RefreshUI();
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
        if (!_game.WaitingForGridClick) return;
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

        if (messageText)
        {
            if (_game.AllRevealed())
                messageText.text = "Round over! Your score: " + _game.GetScore();
            else if (_game.ShowDiscardDrawnButton)
                messageText.text = "You drew " + _game.DrawnCard + ". Swap with a grid card or discard and flip one.";
            else if (_game.WaitingForGridClick)
                messageText.text = "Click a card in your grid to replace or flip.";
            else
                messageText.text = "Draw from deck or take the discard.";
        }
    }
}
