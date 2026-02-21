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
        var scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler != null)
        {
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.matchWidthOrHeight = 0.5f;
            scaler.referencePixelsPerUnit = 100f;
        }

        var root = canvas.transform;

        // --- Game area (top ~78%): score, piles, grid, drawn card — avoids overlap with bottom bar
        var gameAreaGo = new GameObject("GameArea");
        gameAreaGo.transform.SetParent(root, false);
        var gameAreaRect = gameAreaGo.AddComponent<RectTransform>();
        gameAreaRect.anchorMin = new Vector2(0f, 0.22f);
        gameAreaRect.anchorMax = new Vector2(1f, 1f);
        gameAreaRect.offsetMin = Vector2.zero;
        gameAreaRect.offsetMax = Vector2.zero;

        // --- Bottom bar (bottom ~22%): message + buttons — fixed fraction so resizing doesn't overlap grid
        var bottomPanelGo = new GameObject("BottomPanel");
        bottomPanelGo.transform.SetParent(root, false);
        var bottomPanelRect = bottomPanelGo.AddComponent<RectTransform>();
        bottomPanelRect.anchorMin = new Vector2(0f, 0f);
        bottomPanelRect.anchorMax = new Vector2(1f, 0.22f);
        bottomPanelRect.offsetMin = Vector2.zero;
        bottomPanelRect.offsetMax = Vector2.zero;
        var bottomLayout = bottomPanelGo.AddComponent<VerticalLayoutGroup>();
        bottomLayout.spacing = 8f;
        bottomLayout.childAlignment = TextAnchor.MiddleCenter;
        bottomLayout.childControlWidth = true;
        bottomLayout.childControlHeight = true;
        bottomLayout.childForceExpandWidth = false;
        bottomLayout.childForceExpandHeight = false;
        bottomLayout.padding = new RectOffset(16, 16, 12, 12);

        // --- Game manager (holds references)
        var managerGo = new GameObject("SkyjoGameManager");
        var manager = managerGo.AddComponent<SkyjoGameManager>();

        // --- Score (inside GameArea)
        var scoreGo = CreateText(gameAreaRect.transform, "ScoreText", "Score: 0", 24);
        var scoreRect = scoreGo.GetComponent<RectTransform>();
        scoreRect.anchorMin = new Vector2(0.5f, 1f);
        scoreRect.anchorMax = new Vector2(0.5f, 1f);
        scoreRect.pivot = new Vector2(0.5f, 1f);
        scoreRect.anchoredPosition = new Vector2(0, -24);
        scoreRect.sizeDelta = new Vector2(300, 40);

        // --- Piles row (inside GameArea)
        var pilesGo = new GameObject("Piles");
        pilesGo.transform.SetParent(gameAreaRect.transform, false);
        var pilesRect = pilesGo.AddComponent<RectTransform>();
        pilesRect.anchorMin = new Vector2(0.5f, 1f);
        pilesRect.anchorMax = new Vector2(0.5f, 1f);
        pilesRect.pivot = new Vector2(0.5f, 1f);
        pilesRect.anchoredPosition = new Vector2(0, -70);
        pilesRect.sizeDelta = new Vector2(280, 100);

        const float cardW = 72f, cardH = 96f; // match SkyjoGameManager card size
        var drawPileBtn = CreateButton(pilesGo.transform, "DrawPile", "", cardW, cardH);
        var drawPileRect = drawPileBtn.GetComponent<RectTransform>();
        drawPileRect.anchorMin = new Vector2(0, 0.5f);
        drawPileRect.anchorMax = new Vector2(0, 0.5f);
        drawPileRect.anchoredPosition = new Vector2(50, 0);
        var drawPileText = drawPileBtn.GetComponentInChildren<Text>();
        if (drawPileText != null) drawPileText.gameObject.SetActive(false);

        var discardBtn = CreateButton(pilesGo.transform, "DiscardPile", "Discard", cardW, cardH);
        var discardRect = discardBtn.GetComponent<RectTransform>();
        discardRect.anchorMin = new Vector2(1, 0.5f);
        discardRect.anchorMax = new Vector2(1, 0.5f);
        discardRect.anchoredPosition = new Vector2(-50, 0);
        var discardTopText = discardBtn.GetComponentInChildren<Text>();
        discardTopText.name = "DiscardTop";
        discardTopText.text = "";

        // --- Grid (inside GameArea, centered)
        var gridGo = new GameObject("PlayerGrid");
        gridGo.transform.SetParent(gameAreaRect.transform, false);
        var gridRect = gridGo.AddComponent<RectTransform>();
        gridRect.anchorMin = new Vector2(0.5f, 0.5f);
        gridRect.anchorMax = new Vector2(0.5f, 0.5f);
        gridRect.pivot = new Vector2(0.5f, 0.5f);
        gridRect.anchoredPosition = new Vector2(0, 0);
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

        // --- Drawn card (inside GameArea, shown when you have a card in hand)
        var drawnCardGo = CreateText(gameAreaRect.transform, "DrawnCardText", "Drawn: —", 28);
        var drawnCardRect = drawnCardGo.GetComponent<RectTransform>();
        drawnCardRect.anchorMin = new Vector2(0.5f, 0.5f);
        drawnCardRect.anchorMax = new Vector2(0.5f, 0.5f);
        drawnCardRect.pivot = new Vector2(0.5f, 0.5f);
        drawnCardRect.anchoredPosition = new Vector2(0, -100);
        drawnCardRect.sizeDelta = new Vector2(200, 50);
        drawnCardGo.SetActive(false);

        // --- Message (inside BottomPanel — stays in bottom bar so no overlap with grid)
        var msgGo = CreateText(bottomPanelRect.transform, "MessageText", "Draw from deck or take the discard.", 18);
        var msgRect = msgGo.GetComponent<RectTransform>();
        msgRect.anchorMin = new Vector2(0.5f, 0.5f);
        msgRect.anchorMax = new Vector2(0.5f, 0.5f);
        msgRect.pivot = new Vector2(0.5f, 0.5f);
        msgRect.anchoredPosition = Vector2.zero;
        msgRect.sizeDelta = new Vector2(520, 36);
        var msgText = msgGo.GetComponent<Text>();
        msgText.horizontalOverflow = HorizontalWrapMode.Wrap;
        msgText.verticalOverflow = VerticalOverflowMode.Truncate;
        msgText.supportRichText = false;
        var msgLE = msgGo.AddComponent<LayoutElement>();
        msgLE.preferredHeight = 36;
        msgLE.minHeight = 28;

        // --- Buttons row (inside BottomPanel)
        var buttonsGo = new GameObject("ActionButtons");
        buttonsGo.transform.SetParent(bottomPanelRect.transform, false);
        var buttonsRect = buttonsGo.AddComponent<RectTransform>();
        buttonsRect.anchorMin = new Vector2(0.5f, 0.5f);
        buttonsRect.anchorMax = new Vector2(0.5f, 0.5f);
        buttonsRect.pivot = new Vector2(0.5f, 0.5f);
        buttonsRect.anchoredPosition = Vector2.zero;
        buttonsRect.sizeDelta = new Vector2(500, 50);
        var horzLayout = buttonsGo.AddComponent<HorizontalLayoutGroup>();
        horzLayout.spacing = 10f;
        horzLayout.childAlignment = TextAnchor.MiddleCenter;
        horzLayout.childControlWidth = horzLayout.childControlHeight = false;
        horzLayout.childForceExpandWidth = horzLayout.childForceExpandHeight = false;
        var buttonsLE = buttonsGo.AddComponent<LayoutElement>();
        buttonsLE.preferredHeight = 50;
        buttonsLE.minHeight = 36;

        var btnDraw = CreateButton(buttonsGo.transform, "BtnDraw", "Draw from deck", 140, 36);
        var btnTake = CreateButton(buttonsGo.transform, "BtnTakeDiscard", "Take discard", 120, 36);
        var btnDiscardDrawn = CreateButton(buttonsGo.transform, "BtnDiscardDrawn", "Discard drawn", 120, 36);
        btnDiscardDrawn.gameObject.SetActive(false);
        var btnNewRound = CreateButton(buttonsGo.transform, "BtnNewRound", "New round", 100, 36);
        btnNewRound.gameObject.SetActive(false);

        // --- Wire manager via SerializedObject
        var so = new SerializedObject(manager);
        so.FindProperty("drawPileButton").objectReferenceValue = drawPileBtn;
        so.FindProperty("discardPileButton").objectReferenceValue = discardBtn;
        so.FindProperty("discardTopText").objectReferenceValue = discardTopText;
        so.FindProperty("slotViews").arraySize = slotViews.Length;
        for (int i = 0; i < slotViews.Length; i++)
            so.FindProperty("slotViews").GetArrayElementAtIndex(i).objectReferenceValue = slotViews[i];
        so.FindProperty("scoreText").objectReferenceValue = scoreGo.GetComponent<Text>();
        so.FindProperty("drawnCardText").objectReferenceValue = drawnCardGo.GetComponent<Text>();
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
