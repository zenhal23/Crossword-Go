using System.IO;
using CrosswordGo;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

/// <summary>
/// Run Tools → Crossword GO → Setup Scenes once to build the Game and Home scenes.
/// All component references are wired automatically.
/// </summary>
public static class SceneSetupEditor
{
    private const string PrefabsPath = "Assets/Prefabs";

    [MenuItem("Tools/Crossword GO/Setup Scenes")]
    public static void SetupAll()
    {
        EnsureDirectories();
        CreatePrefabs();
        SetupGameScene();
        SetupHomeScene();
        AssetDatabase.SaveAssets();
        Debug.Log("[CrosswordGO] Scene setup complete.");
    }

    // -------------------------------------------------------------------------
    // Directory helpers
    // -------------------------------------------------------------------------

    private static void EnsureDirectories()
    {
        foreach (var path in new[] { PrefabsPath, "Assets/Levels/Sample", "Assets/Resources" })
            if (!AssetDatabase.IsValidFolder(path))
            {
                var parts = path.Split('/');
                var parent = parts[0];
                for (int i = 1; i < parts.Length; i++)
                {
                    var full = parent + "/" + parts[i];
                    if (!AssetDatabase.IsValidFolder(full))
                        AssetDatabase.CreateFolder(parent, parts[i]);
                    parent = full;
                }
            }
    }

    // -------------------------------------------------------------------------
    // Prefab creation
    // -------------------------------------------------------------------------

    private static void CreatePrefabs()
    {
        CreateCellViewPrefab();
        CreateLetterTilePrefab();
        CreateClueItemPrefab();
        CreateLevelItemPrefab();
    }

    private static void CreateCellViewPrefab()
    {
        string path = $"{PrefabsPath}/CellView.prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null) return;

        var root = new GameObject("CellView");
        root.AddComponent<RectTransform>();

        var bg = AddImage(root, "Background", Color.white);
        bg.GetComponent<RectTransform>().anchorMin = Vector2.zero;
        bg.GetComponent<RectTransform>().anchorMax = Vector2.one;
        bg.GetComponent<RectTransform>().sizeDelta = Vector2.zero;

        var btn = root.AddComponent<Button>();
        btn.targetGraphic = bg.GetComponent<Image>();

        // Letter display (large, centred)
        var letterGo = new GameObject("Letter");
        letterGo.transform.SetParent(root.transform, false);
        var letterTMP = letterGo.AddComponent<TextMeshProUGUI>();
        letterTMP.alignment = TextAlignmentOptions.Center;
        letterTMP.fontSize = 28;
        letterTMP.fontStyle = FontStyles.Bold;
        letterTMP.color = Color.black;
        FillRect(letterTMP.rectTransform);

        // Clue text (small, top-left)
        var clueTextGo = new GameObject("ClueText");
        clueTextGo.transform.SetParent(root.transform, false);
        var clueTMP = clueTextGo.AddComponent<TextMeshProUGUI>();
        clueTMP.alignment = TextAlignmentOptions.TopLeft;
        clueTMP.fontSize = 8;
        clueTMP.color = new Color(0.15f, 0.15f, 0.15f);
        FillRect(clueTMP.rectTransform, new Vector4(3, 3, 3, 14)); // left,right,top padding + arrow space

        // Clue image (fills cell minus arrow row)
        var clueImgGo = new GameObject("ClueImage");
        clueImgGo.transform.SetParent(root.transform, false);
        var clueImg = clueImgGo.AddComponent<Image>();
        clueImg.preserveAspect = true;
        var clueImgRect = clueImg.rectTransform;
        clueImgRect.anchorMin = new Vector2(0.05f, 0.15f);
        clueImgRect.anchorMax = new Vector2(0.95f, 0.95f);
        clueImgRect.sizeDelta = Vector2.zero;
        clueImgGo.SetActive(false);

        // Arrow (bottom-right corner)
        var arrowGo = new GameObject("Arrow");
        arrowGo.transform.SetParent(root.transform, false);
        var arrowImg = arrowGo.AddComponent<Image>();
        arrowImg.color = new Color(0.3f, 0.3f, 0.3f);
        var arrowRect = arrowImg.rectTransform;
        arrowRect.anchorMin = new Vector2(0.7f, 0f);
        arrowRect.anchorMax = new Vector2(1f, 0.25f);
        arrowRect.sizeDelta = Vector2.zero;
        arrowGo.SetActive(false);

        var cv = root.AddComponent<CellView>();
        SerializedObjectSet(cv, "background", bg.GetComponent<Image>());
        SerializedObjectSet(cv, "letterText", letterTMP);
        SerializedObjectSet(cv, "clueText", clueTMP);
        SerializedObjectSet(cv, "clueImage", clueImg);
        SerializedObjectSet(cv, "arrowImage", arrowImg);
        SerializedObjectSet(cv, "button", btn);

        PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
    }

    private static void CreateLetterTilePrefab()
    {
        string path = $"{PrefabsPath}/LetterTile.prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null) return;

        var root = new GameObject("LetterTile");
        var rect = root.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(64, 64);

        var bg = AddImage(root, "Background", new Color(0.93f, 0.78f, 0.45f));
        bg.GetComponent<RectTransform>().anchorMin = Vector2.zero;
        bg.GetComponent<RectTransform>().anchorMax = Vector2.one;
        bg.GetComponent<RectTransform>().sizeDelta = Vector2.zero;

        var btn = root.AddComponent<Button>();
        btn.targetGraphic = bg.GetComponent<Image>();

        var labelGo = new GameObject("Label");
        labelGo.transform.SetParent(root.transform, false);
        var tmp = labelGo.AddComponent<TextMeshProUGUI>();
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontSize = 30;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = Color.black;
        FillRect(tmp.rectTransform);

        var tile = root.AddComponent<LetterTile>();
        SerializedObjectSet(tile, "letterLabel", tmp);
        SerializedObjectSet(tile, "button", btn);
        SerializedObjectSet(tile, "background", bg.GetComponent<Image>());

        PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
    }

    private static void CreateClueItemPrefab()
    {
        string path = $"{PrefabsPath}/ClueItem.prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null) return;

        var root = new GameObject("ClueItem");
        var rect = root.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(200, 36);
        root.AddComponent<HorizontalLayoutGroup>().childAlignment = TextAnchor.MiddleLeft;

        var imgGo = AddImage(root, "Image", Color.white);
        imgGo.GetComponent<RectTransform>().sizeDelta = new Vector2(30, 30);

        var labelGo = new GameObject("Label");
        labelGo.transform.SetParent(root.transform, false);
        var tmp = labelGo.AddComponent<TextMeshProUGUI>();
        tmp.fontSize = 12;
        tmp.color = Color.black;
        tmp.rectTransform.sizeDelta = new Vector2(160, 36);

        PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
    }

    private static void CreateLevelItemPrefab()
    {
        string path = $"{PrefabsPath}/LevelItem.prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null) return;

        var root = new GameObject("LevelItem");
        root.AddComponent<RectTransform>().sizeDelta = new Vector2(320, 60);

        var bg = AddImage(root, "Background", new Color(0.9f, 0.9f, 1f));
        bg.GetComponent<RectTransform>().anchorMin = Vector2.zero;
        bg.GetComponent<RectTransform>().anchorMax = Vector2.one;
        bg.GetComponent<RectTransform>().sizeDelta = Vector2.zero;

        var btn = root.AddComponent<Button>();
        btn.targetGraphic = bg.GetComponent<Image>();

        var labelGo = new GameObject("Label");
        labelGo.transform.SetParent(root.transform, false);
        var tmp = labelGo.AddComponent<TextMeshProUGUI>();
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontSize = 18;
        tmp.color = Color.black;
        FillRect(tmp.rectTransform);

        PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
    }

    // -------------------------------------------------------------------------
    // Game Scene
    // -------------------------------------------------------------------------

    private static void SetupGameScene()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/Game.unity", OpenSceneMode.Single);

        foreach (var go in scene.GetRootGameObjects())
            Object.DestroyImmediate(go);

        // ── Camera ──────────────────────────────────────────────────────────
        var camGo = new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        var cam = camGo.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.13f, 0.13f, 0.18f);
        cam.orthographic = true;
        cam.depth = -1;
        camGo.AddComponent<AudioListener>();

        // ── Event system ────────────────────────────────────────────────────
        var es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
        es.AddComponent<StandaloneInputModule>();

        // ── Canvas ──────────────────────────────────────────────────────────
        // Reference 1080×1920, match width so grid always fits horizontally.
        var canvasGo = new GameObject("Canvas");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var cs = canvasGo.AddComponent<CanvasScaler>();
        cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1080, 1920);
        cs.matchWidthOrHeight = 0f; // match width — grid always fits horizontally
        canvasGo.AddComponent<GraphicRaycaster>();

        // ── Scoreboard (top ~150px) ──────────────────────────────────────────
        // anchors: y 0.922 → 1.0  (150 / 1920 ≈ 0.078)
        var scoreboard = MakePanel(canvasGo, "Scoreboard",
            new Vector2(0, 0.922f), new Vector2(1, 1f), Vector2.zero, Vector2.zero);
        AddImage(scoreboard, "BG", new Color(0.18f, 0.35f, 0.62f));

        var playerPanel = MakePanel(scoreboard, "PlayerPanel",
            new Vector2(0, 0), new Vector2(0.4f, 1), Vector2.zero, Vector2.zero);
        var playerNameLabel = AddTMP(playerPanel, "Name", "You", 18, FontStyles.Normal);
        playerNameLabel.rectTransform.anchorMin = new Vector2(0, 0f);
        playerNameLabel.rectTransform.anchorMax = new Vector2(1, 0.35f);
        playerNameLabel.rectTransform.sizeDelta = Vector2.zero;
        var playerScoreLabel = AddTMP(playerPanel, "Score", "0", 44, FontStyles.Bold);
        playerScoreLabel.rectTransform.anchorMin = new Vector2(0, 0.35f);
        playerScoreLabel.rectTransform.anchorMax = Vector2.one;
        playerScoreLabel.rectTransform.sizeDelta = Vector2.zero;

        var vsLabel = AddTMP(scoreboard, "VsLabel", "VS", 22, FontStyles.Bold);
        vsLabel.rectTransform.anchorMin = new Vector2(0.4f, 0);
        vsLabel.rectTransform.anchorMax = new Vector2(0.6f, 1);
        vsLabel.rectTransform.sizeDelta = Vector2.zero;

        var opponentPanel = MakePanel(scoreboard, "OpponentPanel",
            new Vector2(0.6f, 0), new Vector2(1, 1), Vector2.zero, Vector2.zero);
        var opponentNameLabel = AddTMP(opponentPanel, "Name", "Opponent", 18, FontStyles.Normal);
        opponentNameLabel.rectTransform.anchorMin = new Vector2(0, 0f);
        opponentNameLabel.rectTransform.anchorMax = new Vector2(1, 0.35f);
        opponentNameLabel.rectTransform.sizeDelta = Vector2.zero;
        var opponentScoreLabel = AddTMP(opponentPanel, "Score", "0", 44, FontStyles.Bold);
        opponentScoreLabel.rectTransform.anchorMin = new Vector2(0, 0.35f);
        opponentScoreLabel.rectTransform.anchorMax = Vector2.one;
        opponentScoreLabel.rectTransform.sizeDelta = Vector2.zero;

        var playerHighlight = MakePanel(scoreboard, "PlayerHighlight",
            new Vector2(0, 0.9f), new Vector2(0.45f, 1f), Vector2.zero, Vector2.zero);
        var opponentHighlight = MakePanel(scoreboard, "OpponentHighlight",
            new Vector2(0.55f, 0.9f), new Vector2(1, 1f), Vector2.zero, Vector2.zero);

        // Timer bar along bottom edge of scoreboard
        var timerBarBg = MakePanel(scoreboard, "TimerBarBG",
            new Vector2(0, 0), new Vector2(1, 0.08f), Vector2.zero, Vector2.zero);
        AddImage(timerBarBg, "BG", new Color(0.3f, 0.3f, 0.3f));
        var timerFill = AddImage(timerBarBg, "Fill", new Color(0.2f, 0.85f, 0.35f)).GetComponent<Image>();
        timerFill.type = Image.Type.Filled;
        timerFill.fillMethod = Image.FillMethod.Horizontal;
        timerFill.fillOrigin = 0;
        FillRect(timerFill.rectTransform);

        // ── Grid area (9×7 cells, centered) ─────────────────────────────────
        // anchors: y 0.141 → 0.922  (~1494px — grid itself is 1330px so it centers nicely)
        // Cell size: 146×146, spacing 2×2, 7 columns → grid = 1034×1330px
        // childAlignment = MiddleCenter auto-centers within this container.
        var gridArea = MakePanel(canvasGo, "GridArea",
            new Vector2(0, 0.141f), new Vector2(1, 0.922f), Vector2.zero, Vector2.zero);
        AddImage(gridArea, "BG", new Color(0.92f, 0.90f, 0.88f));

        var gridContent = new GameObject("GridContent");
        gridContent.transform.SetParent(gridArea.transform, false);
        var gridContentRect = gridContent.AddComponent<RectTransform>();
        FillRect(gridContentRect);
        var layout = gridContent.AddComponent<GridLayoutGroup>();
        layout.cellSize = new Vector2(146, 146);
        layout.spacing = new Vector2(2, 2);
        layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        layout.constraintCount = 7;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.padding = new RectOffset(4, 4, 4, 4);

        // ── Hand letters (above submit, ~150px) ─────────────────────────────
        // anchors: y 0.063 → 0.141
        var handPanel = MakePanel(canvasGo, "HandPanel",
            new Vector2(0, 0.063f), new Vector2(1, 0.141f), Vector2.zero, Vector2.zero);
        AddImage(handPanel, "BG", new Color(0.22f, 0.40f, 0.68f));

        var tileContainer = MakePanel(handPanel, "TileContainer",
            Vector2.zero, Vector2.one, new Vector2(8, 8), new Vector2(-8, -8));
        var tileLayout = tileContainer.AddComponent<HorizontalLayoutGroup>();
        tileLayout.childAlignment = TextAnchor.MiddleCenter;
        tileLayout.spacing = 10;
        tileLayout.childControlWidth = false;
        tileLayout.childControlHeight = false;
        tileLayout.padding = new RectOffset(4, 4, 4, 4);

        // ── Submit button (bottom ~120px) ────────────────────────────────────
        // anchors: y 0 → 0.063
        var submitArea = MakePanel(canvasGo, "SubmitArea",
            new Vector2(0, 0), new Vector2(1, 0.063f), Vector2.zero, Vector2.zero);
        AddImage(submitArea, "BG", new Color(0.15f, 0.15f, 0.20f));

        var submitBtn = MakeButton(submitArea, "SubmitButton", "SUBMIT");
        var submitBtnRect = submitBtn.GetComponent<RectTransform>();
        submitBtnRect.anchorMin = new Vector2(0.08f, 0.12f);
        submitBtnRect.anchorMax = new Vector2(0.92f, 0.88f);
        submitBtnRect.sizeDelta = Vector2.zero;
        // Green accent for submit
        submitBtn.transform.Find("BG").GetComponent<Image>().color = new Color(0.18f, 0.72f, 0.38f);
        submitBtn.transform.Find("Label").GetComponent<TextMeshProUGUI>().color = Color.white;

        // ── Result panel (hidden overlay) ────────────────────────────────────
        var resultRoot = MakePanel(canvasGo, "ResultPanel",
            new Vector2(0.05f, 0.25f), new Vector2(0.95f, 0.75f), Vector2.zero, Vector2.zero);
        AddImage(resultRoot, "BG", new Color(0.08f, 0.08f, 0.22f, 0.97f));
        var resultLabel = AddTMP(resultRoot, "ResultLabel", "You Win!", 52, FontStyles.Bold);
        FillRect(resultLabel.rectTransform, new Vector4(0, 0, 60, 0));
        var scoresLabel = AddTMP(resultRoot, "ScoresLabel", "0 — 0", 30, FontStyles.Normal);
        FillRect(scoresLabel.rectTransform, new Vector4(0, 0, 0, 60));
        var homeBtn = MakeButton(resultRoot, "HomeButton", "HOME");
        homeBtn.GetComponent<RectTransform>().anchorMin = new Vector2(0.2f, 0.06f);
        homeBtn.GetComponent<RectTransform>().anchorMax = new Vector2(0.8f, 0.24f);
        resultRoot.SetActive(false);

        // ── Game Manager object ──────────────────────────────────────────────
        var gmGo = new GameObject("GameManager");
        var gsm = gmGo.AddComponent<GameStateManager>();
        var tm = gmGo.AddComponent<TurnManager>();
        var bc = gmGo.AddComponent<BotController>();
        var lm = gmGo.AddComponent<LevelManager>();
        var pic = gmGo.AddComponent<PlayerInputController>();

        // GridView
        var gv = gridContent.AddComponent<GridView>();
        var cellPrefab = AssetDatabase.LoadAssetAtPath<CellView>($"{PrefabsPath}/CellView.prefab");
        SerializedObjectSet(gv, "cellViewPrefab", cellPrefab);
        SerializedObjectSet(gv, "gridLayout", layout);

        // LetterHandView (no shuffle/pass/hint)
        var tilePrefab = AssetDatabase.LoadAssetAtPath<LetterTile>($"{PrefabsPath}/LetterTile.prefab");
        var hv = handPanel.AddComponent<LetterHandView>();
        SerializedObjectSet(hv, "tilePrefab", tilePrefab);
        SerializedObjectSet(hv, "tileContainer", tileContainer.transform);

        // ScoreboardView
        var sv = scoreboard.AddComponent<ScoreboardView>();
        SerializedObjectSet(sv, "playerLabel", playerNameLabel);
        SerializedObjectSet(sv, "playerScore", playerScoreLabel);
        SerializedObjectSet(sv, "opponentLabel", opponentNameLabel);
        SerializedObjectSet(sv, "opponentScore", opponentScoreLabel);
        SerializedObjectSet(sv, "timerBar", timerFill);
        SerializedObjectSet(sv, "playerHighlight", playerHighlight.AddComponent<Image>());
        SerializedObjectSet(sv, "opponentHighlight", opponentHighlight.AddComponent<Image>());

        // ResultPanel
        var rp = resultRoot.AddComponent<ResultPanel>();
        SerializedObjectSet(rp, "root", resultRoot);
        SerializedObjectSet(rp, "resultLabel", resultLabel);
        SerializedObjectSet(rp, "scoresLabel", scoresLabel);
        SerializedObjectSet(rp, "homeButton", homeBtn.GetComponent<Button>());

        // PlayerInputController
        SerializedObjectSet(pic, "gridView", gv);
        SerializedObjectSet(pic, "handView", hv);
        SerializedObjectSet(pic, "submitButton", submitBtn.GetComponent<Button>());

        // TurnManager
        SerializedObjectSet(tm, "botController", bc);

        // LevelManager (cluePanel left null — handled with null-check in LevelManager)
        SerializedObjectSet(lm, "turnManager", tm);
        SerializedObjectSet(lm, "gameStateManager", gsm);
        SerializedObjectSet(lm, "gridView", gv);
        SerializedObjectSet(lm, "handView", hv);
        SerializedObjectSet(lm, "scoreboardView", sv);
        SerializedObjectSet(lm, "playerInput", pic);
        SerializedObjectSet(lm, "resultPanel", rp);

        EditorSceneManager.SaveScene(scene);
        Debug.Log("[CrosswordGO] Game scene setup complete.");
    }

    // -------------------------------------------------------------------------
    // Home Scene
    // -------------------------------------------------------------------------

    private static void SetupHomeScene()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/Home.unity", OpenSceneMode.Single);

        foreach (var go in scene.GetRootGameObjects())
            Object.DestroyImmediate(go);

        var homeCam = new GameObject("Main Camera");
        homeCam.tag = "MainCamera";
        var hcam = homeCam.AddComponent<Camera>();
        hcam.clearFlags = CameraClearFlags.SolidColor;
        hcam.backgroundColor = new Color(0.34f, 0.45f, 0.68f);
        hcam.orthographic = true;
        hcam.depth = -1;
        homeCam.AddComponent<AudioListener>();

        var es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
        es.AddComponent<StandaloneInputModule>();

        var canvasGo = new GameObject("Canvas");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var cs = canvasGo.AddComponent<CanvasScaler>();
        cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1080, 1920);
        cs.matchWidthOrHeight = 0f;
        canvasGo.AddComponent<GraphicRaycaster>();

        // Background
        var bg = canvasGo.AddComponent<Image>();
        bg.color = new Color(0.55f, 0.65f, 0.85f);

        // Title
        var titleGo = MakePanel(canvasGo, "Title",
            new Vector2(0.1f, 0.8f), new Vector2(0.9f, 0.95f), Vector2.zero, Vector2.zero);
        var titleTMP = AddTMP(titleGo, "TitleLabel", "Crossword GO!", 52, FontStyles.Bold);
        titleTMP.color = Color.white;
        titleTMP.alignment = TextAlignmentOptions.Center;
        FillRect(titleTMP.rectTransform);

        // Level list scroll
        var listScroll = MakePanel(canvasGo, "LevelListScroll",
            new Vector2(0.1f, 0.1f), new Vector2(0.9f, 0.78f), Vector2.zero, Vector2.zero);
        var sr = listScroll.AddComponent<ScrollRect>();
        var vp = MakePanel(listScroll, "Viewport", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        vp.AddComponent<Mask>().showMaskGraphic = false;
        AddImage(vp, "BG", Color.clear);

        var content = new GameObject("Content");
        content.transform.SetParent(vp.transform, false);
        var contentRect = content.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0, 1);
        contentRect.anchorMax = new Vector2(1, 1);
        contentRect.pivot = new Vector2(0.5f, 1);
        var vLayout = content.AddComponent<VerticalLayoutGroup>();
        vLayout.childAlignment = TextAnchor.UpperCenter;
        vLayout.spacing = 10;
        vLayout.padding = new RectOffset(10, 10, 10, 10);
        vLayout.childControlWidth = true;
        vLayout.childControlHeight = false;
        vLayout.childForceExpandWidth = true;
        content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        sr.content = contentRect;
        sr.viewport = vp.GetComponent<RectTransform>();
        sr.horizontal = false;
        sr.vertical = true;

        // LevelSelectView
        var lsv = content.AddComponent<LevelSelectView>();
        var levelItemPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabsPath}/LevelItem.prefab");
        SerializedObjectSet(lsv, "listContainer", content.transform);
        SerializedObjectSet(lsv, "levelItemPrefab", levelItemPrefab);

        EditorSceneManager.SaveScene(scene);
        Debug.Log("[CrosswordGO] Home scene setup complete.");
    }

    // -------------------------------------------------------------------------
    // UI helpers
    // -------------------------------------------------------------------------

    private static GameObject MakePanel(GameObject parent, string name,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax,
        float height = 0)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
        if (height > 0) rect.sizeDelta = new Vector2(rect.sizeDelta.x, height);
        return go;
    }

    private static GameObject AddImage(GameObject parent, string name, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        var img = go.AddComponent<Image>();
        img.color = color;
        return go;
    }

    private static TextMeshProUGUI AddTMP(GameObject parent, string name,
        string text, float size, FontStyles style)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.fontStyle = style;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
        return tmp;
    }

    private static GameObject MakeButton(GameObject parent, string name, string label)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        go.AddComponent<RectTransform>();
        var bg = AddImage(go, "BG", new Color(0.9f, 0.9f, 0.9f));
        bg.GetComponent<RectTransform>().anchorMin = Vector2.zero;
        bg.GetComponent<RectTransform>().anchorMax = Vector2.one;
        bg.GetComponent<RectTransform>().sizeDelta = Vector2.zero;
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = bg.GetComponent<Image>();
        var labelTMP = AddTMP(go, "Label", label, 22, FontStyles.Bold);
        labelTMP.color = Color.black;
        FillRect(labelTMP.rectTransform);
        return go;
    }

    private static void FillRect(RectTransform rect, Vector4 padding = default)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(padding.x, padding.y);
        rect.offsetMax = new Vector2(-padding.z, -padding.w);
    }

    // Sets a serialized field by name using SerializedObject reflection
    private static void SerializedObjectSet(Object target, string fieldName, Object value)
    {
        var so = new SerializedObject(target);
        var prop = so.FindProperty(fieldName);
        if (prop != null)
        {
            prop.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
        else
        {
            Debug.LogWarning($"[SceneSetup] Field '{fieldName}' not found on {target.GetType().Name}");
        }
    }
}
