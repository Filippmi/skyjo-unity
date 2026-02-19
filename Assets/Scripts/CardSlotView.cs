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

    public void Set(SkyjoGame.GridCell cell)
    {
        if (cell.Removed)
        {
            if (background) background.color = new Color(0.2f, 0.2f, 0.25f);
            if (label) { label.text = "—"; label.color = Color.gray; }
        }
        else if (cell.FaceUp)
        {
            if (background) background.color = new Color(0.95f, 0.95f, 0.9f);
            if (label) { label.text = cell.Value.ToString(); label.color = cell.Value < 0 ? new Color(0.18f, 0.49f, 0.2f) : Color.black; }
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
