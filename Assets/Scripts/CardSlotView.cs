using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// One card slot in the 3x4 grid. Displays value / face-down / removed and reports clicks.
/// </summary>
[RequireComponent(typeof(Image))]
public class CardSlotView : MonoBehaviour, IPointerClickHandler
{
    public int SlotIndex { get; set; }

    [SerializeField] private Image background;
    [SerializeField] private UnityEngine.UI.Text label;

    private SkyjoGameManager _manager;

    private void Awake()
    {
        if (background == null) background = GetComponent<Image>();
        if (label == null) label = GetComponentInChildren<UnityEngine.UI.Text>();
        _manager = FindObjectOfType<SkyjoGameManager>();
    }

    /// <summary>Card background color by value: -2/-1 purple, 0-4 green, 5-8 yellow, 9-12 red.</summary>
    private static Color GetCardColor(int value)
    {
        if (value <= -1) return new Color(0.5f, 0.2f, 0.6f);   // purple
        if (value <= 4) return new Color(0.2f, 0.65f, 0.35f);  // green
        if (value <= 8) return new Color(0.9f, 0.75f, 0.2f);   // yellow
        return new Color(0.8f, 0.25f, 0.25f);                   // red (9-12)
    }

    public void Set(SkyjoGame.GridCell cell)
    {
        if (cell.Removed)
        {
            if (background) background.color = new Color(0.2f, 0.2f, 0.25f);
            if (label) { label.text = "—"; label.color = Color.gray; }
        }
        else if (cell.FaceUp)
        {
            if (background) background.color = GetCardColor(cell.Value);
            if (label) { label.text = cell.Value.ToString(); label.color = Color.white; }
        }
        else
        {
            if (background) background.color = new Color(0.3f, 0.4f, 0.55f);
            if (label) { label.text = "?"; label.color = new Color(0.5f, 0.6f, 0.7f); }
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        _manager?.OnGridSlotClicked(SlotIndex);
    }
}
