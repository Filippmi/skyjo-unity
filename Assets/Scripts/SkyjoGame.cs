using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Pure Skyjo game state and rules. No Unity dependencies.
/// </summary>
public class SkyjoGame
{
    public const int Rows = 3;
    public const int Cols = 4;
    public const int GridSize = Rows * Cols;

    public enum TurnPhase
    {
        ChooseSource,   // Draw from deck or take discard
        SwapOrDiscard,  // Drew from deck: swap or discard and flip
        PickTarget      // Click a grid card to replace or flip
    }

    public class GridCell
    {
        public int Value;
        public bool FaceUp;
        public bool Removed;
    }

    private readonly List<int> _deck = new List<int>();
    private readonly List<int> _discard = new List<int>();
    private readonly GridCell[] _grid = new GridCell[GridSize];
    private int? _drawnCard;
    private TurnPhase _phase = TurnPhase.ChooseSource;

    public IReadOnlyList<int> Deck => _deck;
    public IReadOnlyList<int> Discard => _discard;
    public GridCell GetCell(int index) => _grid[index];
    public int? DrawnCard => _drawnCard;
    public TurnPhase Phase => _phase;

    private static List<int> BuildDeck()
    {
        var list = new List<int>();
        for (int v = -2; v <= 12; v++)
            for (int i = 0; i < 10; i++)
                list.Add(v);
        Shuffle(list);
        return list;
    }

    private static void Shuffle<T>(List<T> list)
    {
        var rng = new Random();
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    public void NewRound()
    {
        _deck.Clear();
        _deck.AddRange(BuildDeck());
        _discard.Clear();
        _drawnCard = null;
        _phase = TurnPhase.ChooseSource;

        for (int i = 0; i < GridSize; i++)
        {
            int value = _deck[_deck.Count - 1];
            _deck.RemoveAt(_deck.Count - 1);
            _grid[i] = new GridCell { Value = value, FaceUp = false, Removed = false };
        }

        // Flip 2 random cards
        var indices = Enumerable.Range(0, GridSize).ToList();
        Shuffle(indices);
        _grid[indices[0]].FaceUp = true;
        _grid[indices[1]].FaceUp = true;

        _discard.Add(_deck[_deck.Count - 1]);
        _deck.RemoveAt(_deck.Count - 1);

        CheckColumnTriples();
    }

    private int GridIndex(int row, int col) => row * Cols + col;

    private void CheckColumnTriples()
    {
        for (int c = 0; c < Cols; c++)
        {
            var a = _grid[GridIndex(0, c)];
            var b = _grid[GridIndex(1, c)];
            var c_ = _grid[GridIndex(2, c)];
            if (a.FaceUp && !a.Removed && b.FaceUp && !b.Removed && c_.FaceUp && !c_.Removed
                && a.Value == b.Value && b.Value == c_.Value)
            {
                a.Removed = b.Removed = c_.Removed = true;
                _discard.Add(a.Value);
                _discard.Add(b.Value);
                _discard.Add(c_.Value);
            }
        }
    }

    public int GetScore()
    {
        int sum = 0;
        for (int i = 0; i < GridSize; i++)
        {
            var cell = _grid[i];
            if (cell.Removed) continue;
            if (cell.FaceUp) sum += cell.Value;
        }
        return sum;
    }

    public bool AllRevealed()
    {
        for (int i = 0; i < GridSize; i++)
            if (!_grid[i].FaceUp && !_grid[i].Removed)
                return false;
        return true;
    }

    public bool CanDrawFromDeck => _deck.Count > 0 && _phase == TurnPhase.ChooseSource;
    public bool CanTakeDiscard => _discard.Count > 0 && _phase == TurnPhase.ChooseSource;
    public bool ShowDiscardDrawnButton => _phase == TurnPhase.SwapOrDiscard && _drawnCard.HasValue;
    public bool WaitingForGridClick => _phase == TurnPhase.PickTarget;

    public void DrawFromDeck()
    {
        if (!CanDrawFromDeck) return;
        _drawnCard = _deck[_deck.Count - 1];
        _deck.RemoveAt(_deck.Count - 1);
        _phase = TurnPhase.SwapOrDiscard;
    }

    public void TakeFromDiscard()
    {
        if (!CanTakeDiscard) return;
        _drawnCard = _discard[_discard.Count - 1];
        _discard.RemoveAt(_discard.Count - 1);
        _phase = TurnPhase.PickTarget;
    }

    public void DiscardDrawnAndFlip()
    {
        if (_phase != TurnPhase.SwapOrDiscard || !_drawnCard.HasValue) return;
        _discard.Add(_drawnCard.Value);
        _drawnCard = null;
        _phase = TurnPhase.PickTarget;
    }

    /// <summary>
    /// Replace grid slot with drawn card, or flip face-down. Returns true if action was valid.
    /// Allowed when: PickTarget (replace/flip), or SwapOrDiscard with a drawn card (swap).
    /// </summary>
    public bool ReplaceOrFlip(int gridIndex)
    {
        bool canPickGrid = _phase == TurnPhase.PickTarget || (_phase == TurnPhase.SwapOrDiscard && _drawnCard.HasValue);
        if (!canPickGrid || gridIndex < 0 || gridIndex >= GridSize) return false;
        var cell = _grid[gridIndex];
        if (cell.Removed) return false;

        if (_drawnCard.HasValue)
        {
            _discard.Add(cell.Value);
            cell.Value = _drawnCard.Value;
            cell.FaceUp = true;
            _drawnCard = null;
        }
        else
        {
            if (!cell.FaceUp)
                cell.FaceUp = true;
            else
                return false;
        }

        EndTurn();
        return true;
    }

    private void EndTurn()
    {
        _drawnCard = null;
        _phase = TurnPhase.ChooseSource;
        CheckColumnTriples();
    }

    public int? TopDiscard => _discard.Count > 0 ? _discard[_discard.Count - 1] : (int?)null;
}
