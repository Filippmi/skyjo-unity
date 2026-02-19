using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// Menu: Skyjo > Setup Scene — creates a playable UI hierarchy and wires SkyjoGameManager.
/// </summary>
public static class SkyjoSetupScene
{
    private const string MenuName = "Skyjo/Setup Scene";

    [MenuItem(MenuName)]
    public static void Setup()
    {
        if (Object.FindObjectOfType<SkyjoGameManager>() != null)
        {
            Debug.Log("SkyjoGameManager already in scene. Skipping setup.");
            return;
        }

        EnsureEventSystem();

        var canvas = Object.FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            var canvasGo = new GameObject("Canvas");
            canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGo.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasGo.AddComponent<GraphicRaycaster>();
        }

        var root = canvas.transform;

        // --- Game manager (holds references)
        var managerGo = new GameObject("SkyjoGameManager");
        var manager = managerGo.AddComponent<SkyjoGameManager>();

        // --- Score
        var scoreGo = CreateText(root, "ScoreText", "Score: 0", 24);
        var scoreRect = scoreGo.GetComponent<RectTransform>();
        scoreRect.anchorMin = new Vector2(0.5f, 1f);
        scoreRect.anchorMax = new Vector2(0.5f, 1f);
        scoreRect.pivot = new Vector2(0.5f, 1f);
        scoreRect.anchoredPosition = new Vector2(0, -20);
        scoreRect.sizeDelta = new Vector2(300, 40);

        // --- Piles row
        var pilesGo = new GameObject("Piles");
        pilesGo.transform.SetParent(root, false);
        var pilesRect = pilesGo.AddComponent<RectTransform>();
        pilesRect.anchorMin = new Vector2(0.5f, 1f);
        pilesRect.anchorMax = new Vector2(0.5f, 1f);
        pilesRect.pivot = new Vector2(0.5f, 1f);
        pilesRect.anchoredPosition = new Vector2(0, -70);
        pilesRect.sizeDelta = new Vector2(280, 100);

        var drawPileBtn = CreateButton(pilesGo.transform, "DrawPile", "Draw\n(", 90, 90);
        var drawPileRect = drawPileBtn.GetComponent<RectTransform>();
        drawPileRect.anchorMin = new Vector2(0, 0.5f);
        drawPileRect.anchorMax = new Vector2(0, 0.5f);
        drawPileRect.anchoredPosition = new Vector2(50, 0);
        var drawCountText = drawPileBtn.GetComponentInChildren<Text>();
        drawCountText.name = "DrawCount";

        var discardBtn = CreateButton(pilesGo.transform, "DiscardPile", "Discard", 90, 90);
        var discardRect = discardBtn.GetComponent<RectTransform>();
        discardRect.anchorMin = new Vector2(1, 0.5f);
        discardRect.anchorMax = new Vector2(1, 0.5f);
        discardRect.anchoredPosition = new Vector2(-50, 0);
        var discardTopText = discardBtn.GetComponentInChildren<Text>();
        discardTopText.name = "DiscardTop";
        discardTopText.text = "";

        // --- Grid
        var gridGo = new GameObject("PlayerGrid");
        gridGo.transform.SetParent(root, false);
        var gridRect = gridGo.AddComponent<RectTransform>();
        gridRect.anchorMin = new Vector2(0.5f, 0.5f);
        gridRect.anchorMax = new Vector2(0.5f, 0.5f);
        gridRect.pivot = new Vector2(0.5f, 0.5f);
        gridRect.anchoredPosition = new Vector2(0, 20);
        gridRect.sizeDelta = new Vector2(320, 240);

        var gridLayout = gridGo.AddComponent<GridLayoutGroup>();
        gridLayout.cellSize = new Vector2(72, 96);
        gridLayout.spacing = new Vector2(8, 8);
        gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gridLayout.constraintCount = 4;
        gridLayout.childAlignment = TextAnchor.MiddleCenter;

        var slotViews = new CardSlotView[SkyjoGame.GridSize];
        for (int i = 0; i < SkyjoGame.GridSize; i++)
        {
            var slotGo = new GameObject("Slot" + i);
            slotGo.transform.SetParent(gridGo.transform, false);
            var slotRect = slotGo.AddComponent<RectTransform>();
            var img = slotGo.AddComponent<Image>();
            img.color = new Color(0.3f, 0.4f, 0.55f);
            var labelGo = CreateText(slotGo.transform, "Label", "?", 22);
            var labelRect = labelGo.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = labelRect.offsetMax = Vector2.zero;
            var slotView = slotGo.AddComponent<CardSlotView>();
            slotView.SlotIndex = i;
            slotViews[i] = slotView;
        }

        // --- Message
        var msgGo = CreateText(root, "MessageText", "Draw from deck or take the discard.", 18);
        var msgRect = msgGo.GetComponent<RectTransform>();
        msgRect.anchorMin = new Vector2(0.5f, 0);
        msgRect.anchorMax = new Vector2(0.5f, 0);
        msgRect.pivot = new Vector2(0.5f, 0);
        msgRect.anchoredPosition = new Vector2(0, 120);
        msgRect.sizeDelta = new Vector2(500, 50);

        // --- Buttons row
        var buttonsGo = new GameObject("ActionButtons");
        buttonsGo.transform.SetParent(root, false);
        var buttonsRect = buttonsGo.AddComponent<RectTransform>();
        buttonsRect.anchorMin = new Vector2(0.5f, 0);
        buttonsRect.anchorMax = new Vector2(0.5f, 0);
        buttonsRect.pivot = new Vector2(0.5f, 0);
        buttonsRect.anchoredPosition = new Vector2(0, 60);
        buttonsRect.sizeDelta = new Vector2(500, 50);

        var btnDraw = CreateButton(buttonsGo.transform, "BtnDraw", "Draw from deck", 140, 36);
        var btnTake = CreateButton(buttonsGo.transform, "BtnTakeDiscard", "Take discard", 120, 36);
        var btnDiscardDrawn = CreateButton(buttonsGo.transform, "BtnDiscardDrawn", "Discard drawn", 120, 36);
        btnDiscardDrawn.gameObject.SetActive(false);
        var btnNewRound = CreateButton(buttonsGo.transform, "BtnNewRound", "New round", 100, 36);
        btnNewRound.gameObject.SetActive(false);

        LayoutHorizontal(buttonsRect, 10, btnDraw.transform, btnTake.transform, btnDiscardDrawn.transform, btnNewRound.transform);

        // --- Wire manager via SerializedObject
        var so = new SerializedObject(manager);
        so.FindProperty("drawPileButton").objectReferenceValue = drawPileBtn;
        so.FindProperty("discardPileButton").objectReferenceValue = discardBtn;
        so.FindProperty("drawCountText").objectReferenceValue = drawCountText;
        so.FindProperty("discardTopText").objectReferenceValue = discardTopText;
        so.FindProperty("slotViews").arraySize = slotViews.Length;
        for (int i = 0; i < slotViews.Length; i++)
            so.FindProperty("slotViews").GetArrayElementAtIndex(i).objectReferenceValue = slotViews[i];
        so.FindProperty("scoreText").objectReferenceValue = scoreGo.GetComponent<Text>();
        so.FindProperty("messageText").objectReferenceValue = msgGo.GetComponent<Text>();
        so.FindProperty("drawFromDeckButton").objectReferenceValue = btnDraw;
        so.FindProperty("takeDiscardButton").objectReferenceValue = btnTake;
        so.FindProperty("discardDrawnButton").objectReferenceValue = btnDiscardDrawn;
        so.FindProperty("newRoundButton").objectReferenceValue = btnNewRound;
        so.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("Skyjo scene setup complete. Press Play to run.");
    }

    private static void EnsureEventSystem()
    {
        if (Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            var esGo = new GameObject("EventSystem");
            esGo.AddComponent<UnityEngine.EventSystems.EventSystem>();
            esGo.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }
    }

    private static GameObject CreateText(Transform parent, string name, string content, int fontSize)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rect = go.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(200, 30);
        var text = go.AddComponent<Text>();
        text.text = content;
        text.fontSize = fontSize;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        if (UnityEngine.Font.GetOSInstalledFontNames().Length > 0)
            text.font = UnityEngine.Font.CreateDynamicFontFromOSFont(UnityEngine.Font.GetOSInstalledFontNames()[0], fontSize);
        return go;
    }

    private static Button CreateButton(Transform parent, string name, string label, float w, float h)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rect = go.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(w, h);
        var img = go.AddComponent<Image>();
        img.color = new Color(0.2f, 0.5f, 0.4f);
        var btn = go.AddComponent<Button>();
        var textGo = CreateText(go.transform, "Text", label, 16);
        textGo.GetComponent<Text>().color = Color.black;
        var textRect = textGo.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = textRect.offsetMax = Vector2.zero;
        return btn;
    }

    private static void LayoutHorizontal(RectTransform parent, float spacing, params Transform[] children)
    {
        float x = 0;
        for (int i = 0; i < children.Length; i++)
        {
            var r = children[i].GetComponent<RectTransform>();
            r.anchorMin = new Vector2(0, 0.5f);
            r.anchorMax = new Vector2(0, 0.5f);
            r.pivot = new Vector2(0, 0.5f);
            r.anchoredPosition = new Vector2(x, 0);
            x += r.sizeDelta.x + spacing;
        }
    }
}
