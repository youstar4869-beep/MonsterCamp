using System.IO;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

// 메뉴 MonsterCamp > Build Main Scene 으로 Main 씬을 새로 만든다.
// 씬 파일을 직접 편집하지 않기 위한 생성 스크립트. 다시 실행하면 씬을 덮어쓴다.
public static class MainSceneBuilder
{
    private const string ScenePath = "Assets/_Project/Scenes/Main.unity";
    private const string ShopConfigPath = "Assets/_Project/Data/ShopConfig.asset";
    private const string CreatureFolder = "Assets/_Project/Data/Creatures";
    private const string TypeChartPath = "Assets/_Project/Data/TypeChart.asset";
    private const string RunConfigPath = "Assets/_Project/Data/RunConfig.asset";
    private const string SynergyConfigPath = "Assets/_Project/Data/SynergyConfig.asset";
    private const string TrainerFolder = "Assets/_Project/Data/Trainers";
    private const string ItemFolder = "Assets/_Project/Data/Items";
    private const string MetaConfigPath = "Assets/_Project/Data/MetaConfig.asset";
    private const string LockSpritePath = "Assets/_Project/Art/Placeholders/UI/Lock.png";
    private const string TutorialFolder = "Assets/_Project/Data/Tutorial";
    // 전투 스킬 글·데미지 숫자 풀 크기 (동시에 떠 있을 수 있는 글 수)
    private const int FloatingTextCount = 40;
    // 도감 크리처 칸 (1080x1920 기준)
    private const int CodexColumns = 5;
    private const float CodexCellWidth = 186f;
    private const float CodexRowHeight = 290f;
    private const string TutorialDataPath = "Assets/_Project/Data/Tutorial/TutorialData.asset";
    private static readonly Vector2 ReferenceResolution = new Vector2(1080, 1920);

    private static Font _font;

    [MenuItem("MonsterCamp/Build Main Scene")]
    public static void Build()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            return;
        }

        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null &&
            !EditorUtility.DisplayDialog("Build Main Scene", ScenePath + " 를 덮어쓸까요?", "덮어쓰기", "취소"))
        {
            return;
        }

        _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        CreateCamera();
        CreateEventSystem();
        Transform canvas = CreateCanvas();

        var flow = new GameObject("ScreenFlow").AddComponent<ScreenFlow>();
        var run = new GameObject("RunManager").AddComponent<RunManager>();
        var tutorial = new GameObject("Tutorial").AddComponent<TutorialController>();
        var meta = new GameObject("MetaProgress").AddComponent<MetaProgress>();
        var serializedMeta = new SerializedObject(meta);
        serializedMeta.FindProperty("_config").objectReferenceValue = GetOrCreateMetaConfig();
        serializedMeta.ApplyModifiedPropertiesWithoutUndo();

        GameObject title = CreateScreen(canvas, "TitleScreen", "몬스터 캠프");
        GameObject trainer = CreateScreen(canvas, "TrainerSelectScreen", "트레이너 선택");
        GameObject shop = CreateScreen(canvas, "ShopScreen", "상점 · 배치");
        GameObject battle = CreateScreen(canvas, "BattleScreen", "전투");
        GameObject result = CreateScreen(canvas, "ResultScreen", "결과");
        GameObject codex = CreateScreen(canvas, "CodexScreen", "도감");

        // 화면들보다 나중에 만들어 가장 위에 그려지게 한다
        GameObject hud = CreateHud(canvas, out Text roundText, out Text livesText, out Text goldText);
        // 정보 창·시너지 표: 화면·상단 표시보다 위, 튜토리얼보다 아래 (다른 화면에서도 다시 쓸 수 있게 캔버스에)
        CreatureInfoPopup infoPopup = CreateCreatureInfoPopup(canvas);
        SynergyPanel synergyPanel = CreateSynergyPanel(canvas);

        BuildTitleUI(title, flow, run, tutorial);
        BuildCodexUI(codex, flow, meta);

        // 결과 화면: 클리어/게임 오버 + 설명 + 별 + 타이틀로
        Text resultTitle = result.transform.Find("Title").GetComponent<Text>();
        Text resultDetail = CreateText(result.transform, "Detail", "", 56, new Vector2(0.05f, 0.62f), new Vector2(0.95f, 0.72f));
        Text resultStars = CreateText(result.transform, "Stars", "", 56, new Vector2(0.05f, 0.44f), new Vector2(0.95f, 0.6f));
        resultStars.color = new Color(1f, 0.85f, 0.3f);
        AddButton(result, "타이틀로", flow, GameScreen.Title);

        BattleScreen battleScreen = BuildBattleUI(battle, run);

        // 팀이 비었는지 확인한 뒤 전투로 넘어간다
        Button battleButton = CreateButton(shop, "전투 시작");
        ShopScreen shopScreen = BuildShopUI(shop, run, meta, goldText, infoPopup, synergyPanel, out Text trainerText);
        UnityEventTools.AddVoidPersistentListener(battleButton.onClick, shopScreen.StartBattle);

        // 튜토리얼 화면은 모든 화면·상단 표시보다 위에 그려지도록 여기서 만든다
        BuildTutorialUI(canvas, tutorial, meta, shopScreen, (RectTransform)battleButton.transform, infoPopup);

        // 새 런 시작 (1라운드 상점으로 이동은 RunManager가 한다)
        BuildTrainerSelectUI(trainer, run, meta);

        var runConfig = AssetDatabase.LoadAssetAtPath<RunConfig>(RunConfigPath);
        if (runConfig == null)
        {
            Debug.LogError("RunConfig 에셋이 없습니다. 먼저 MonsterCamp > Create Round Data 를 실행하세요.");
        }
        var serializedRun = new SerializedObject(run);
        serializedRun.FindProperty("_config").objectReferenceValue = runConfig;
        SetArray(serializedRun.FindProperty("_trainers"), GetOrCreateTrainers());
        serializedRun.FindProperty("_flow").objectReferenceValue = flow;
        serializedRun.FindProperty("_shop").objectReferenceValue = shopScreen;
        serializedRun.FindProperty("_battle").objectReferenceValue = battleScreen;
        serializedRun.FindProperty("_meta").objectReferenceValue = meta;
        serializedRun.FindProperty("_tutorial").objectReferenceValue = tutorial;
        serializedRun.FindProperty("_resultStars").objectReferenceValue = resultStars;
        serializedRun.FindProperty("_roundText").objectReferenceValue = roundText;
        serializedRun.FindProperty("_livesText").objectReferenceValue = livesText;
        serializedRun.FindProperty("_trainerText").objectReferenceValue = trainerText;
        serializedRun.FindProperty("_resultTitle").objectReferenceValue = resultTitle;
        serializedRun.FindProperty("_resultDetail").objectReferenceValue = resultDetail;
        serializedRun.ApplyModifiedPropertiesWithoutUndo();

        // GameScreen enum 순서와 같아야 한다.
        var serializedFlow = new SerializedObject(flow);
        SetArray(serializedFlow.FindProperty("_screens"), new Object[] { title, trainer, shop, battle, result, codex });
        serializedFlow.FindProperty("_hud").objectReferenceValue = hud;
        serializedFlow.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };

        Debug.Log("Main 씬 생성 완료: " + ScenePath);
    }

    private static void CreateCamera()
    {
        var go = new GameObject("Main Camera");
        go.tag = "MainCamera";
        go.transform.position = new Vector3(0, 0, -10);
        var cam = go.AddComponent<Camera>();
        cam.orthographic = true;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.black;
    }

    private static void CreateEventSystem()
    {
        var go = new GameObject("EventSystem");
        go.AddComponent<EventSystem>();
        // Player Settings의 Active Input Handling이 Input System이므로 이 모듈을 쓴다.
        go.AddComponent<InputSystemUIInputModule>();
    }

    private static Transform CreateCanvas()
    {
        var go = new GameObject("Canvas");
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = ReferenceResolution;
        scaler.matchWidthOrHeight = 0.5f;

        go.AddComponent<GraphicRaycaster>();
        return go.transform;
    }

    // 전체 화면 패널 + 상단 제목 + 하단 버튼 영역(ButtonArea)
    private static GameObject CreateScreen(Transform canvas, string name, string titleText)
    {
        var screen = new GameObject(name, typeof(RectTransform), typeof(Image));
        screen.transform.SetParent(canvas, false);
        Stretch(screen.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);
        screen.GetComponent<Image>().color = new Color(0.12f, 0.14f, 0.18f);

        var title = new GameObject("Title", typeof(RectTransform), typeof(Text));
        title.transform.SetParent(screen.transform, false);
        Stretch(title.GetComponent<RectTransform>(), new Vector2(0f, 0.75f), new Vector2(1f, 0.9f));
        SetupText(title.GetComponent<Text>(), titleText, 96);

        var buttonArea = new GameObject("ButtonArea", typeof(RectTransform), typeof(VerticalLayoutGroup));
        buttonArea.transform.SetParent(screen.transform, false);
        Stretch(buttonArea.GetComponent<RectTransform>(), new Vector2(0.15f, 0.1f), new Vector2(0.85f, 0.35f));
        var layout = buttonArea.GetComponent<VerticalLayoutGroup>();
        layout.spacing = 40;
        layout.childAlignment = TextAnchor.LowerCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        return screen;
    }

    // 타이틀 화면: 제목 / 안내 문구 / 이어하기(저장된 판이 있을 때만) / 새 게임 + 새 게임 확인 창
    private static void BuildTitleUI(GameObject screen, ScreenFlow flow, RunManager run, TutorialController tutorial)
    {
        // 버튼 3개(이어하기, 새 게임, 도감)가 들어가도록 버튼 영역을 넓히고, 맨 아래에 작은 "튜토리얼 다시 보기"
        Stretch((RectTransform)screen.transform.Find("ButtonArea"), new Vector2(0.15f, 0.09f), new Vector2(0.85f, 0.41f));
        Button continueButton = CreateButton(screen, "이어하기");
        Button newGameButton = CreateButton(screen, "새 게임");
        AddButton(screen, "도감", flow, GameScreen.Codex);
        Button replayButton = CreateFreeButton(screen.transform, "튜토리얼 다시 보기",
            new Vector2(0.28f, 0.015f), new Vector2(0.72f, 0.08f), new Color(0.5f, 0.53f, 0.6f));
        replayButton.GetComponentInChildren<Text>().resizeTextMaxSize = 34;

        Text messageText = CreateText(screen.transform, "MessageText", "", 40, new Vector2(0.05f, 0.4f), new Vector2(0.95f, 0.5f));
        messageText.color = new Color(1f, 0.5f, 0.45f);

        // 확인 창: 화면 전체를 어둡게 덮어 뒤의 버튼이 눌리지 않게 한다 (가장 위에 그려지도록 마지막에 만든다)
        var panel = new GameObject("ConfirmPanel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(screen.transform, false);
        Stretch(panel.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);
        panel.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.7f);

        var box = new GameObject("Box", typeof(RectTransform), typeof(Image));
        box.transform.SetParent(panel.transform, false);
        Stretch(box.GetComponent<RectTransform>(), new Vector2(0.08f, 0.36f), new Vector2(0.92f, 0.64f));
        box.GetComponent<Image>().color = new Color(0.2f, 0.22f, 0.28f);

        CreateText(box.transform, "Question", "진행 중인 판이 지워져요.\n새로 시작할까요?", 52,
            new Vector2(0.05f, 0.48f), new Vector2(0.95f, 0.92f));
        Button confirmButton = CreateFreeButton(box.transform, "새로 시작", new Vector2(0.06f, 0.1f), new Vector2(0.48f, 0.4f),
            new Color(0.95f, 0.5f, 0.4f));
        Button cancelButton = CreateFreeButton(box.transform, "취소", new Vector2(0.52f, 0.1f), new Vector2(0.94f, 0.4f),
            new Color(0.7f, 0.72f, 0.78f));
        panel.SetActive(false);

        var title = screen.AddComponent<TitleScreen>();
        var so = new SerializedObject(title);
        so.FindProperty("_flow").objectReferenceValue = flow;
        so.FindProperty("_run").objectReferenceValue = run;
        so.FindProperty("_tutorial").objectReferenceValue = tutorial;
        so.FindProperty("_continueButton").objectReferenceValue = continueButton.gameObject;
        so.FindProperty("_confirmPanel").objectReferenceValue = panel;
        so.FindProperty("_messageText").objectReferenceValue = messageText;
        so.ApplyModifiedPropertiesWithoutUndo();

        UnityEventTools.AddVoidPersistentListener(continueButton.onClick, title.Continue);
        UnityEventTools.AddVoidPersistentListener(newGameButton.onClick, title.NewGame);
        UnityEventTools.AddVoidPersistentListener(confirmButton.onClick, title.ConfirmNewGame);
        UnityEventTools.AddVoidPersistentListener(cancelButton.onClick, title.CancelNewGame);
        UnityEventTools.AddVoidPersistentListener(replayButton.onClick, title.ReplayTutorial);
    }

    // 위치를 직접 정하는 버튼 (ButtonArea 밖)
    private static Button CreateFreeButton(Transform parent, string label, Vector2 anchorMin, Vector2 anchorMax, Color color)
    {
        var go = new GameObject(label + "Button", typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        Stretch(go.GetComponent<RectTransform>(), anchorMin, anchorMax);
        go.GetComponent<Image>().color = color;
        Text text = CreateText(go.transform, "Text", label, 52, Vector2.zero, Vector2.one);
        text.color = Color.black;
        Button button = go.GetComponent<Button>();
        AddPressFeedback(button);
        return button;
    }

    // 누름 효과: 누르는 동안 더 어둡게 + 살짝 작게
    private static void AddPressFeedback(Button button)
    {
        ColorBlock colors = button.colors;
        colors.pressedColor = new Color(0.7f, 0.7f, 0.7f);
        button.colors = colors;
        button.gameObject.AddComponent<ButtonPressFeedback>();
    }

    // 보이는 크기는 그대로 두고 눌리는 범위만 넓힌다 (투명한 판, 1080x1920 기준 픽셀).
    // 버튼이 작을 때 손가락으로 누르기 쉽게 48dp(약 120px) 이상으로 맞추는 데 쓴다
    private static void AddHitArea(Component target, float left, float bottom, float right, float top)
    {
        var go = new GameObject("HitArea", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(target.transform, false);
        go.transform.SetAsFirstSibling();
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(-left, -bottom);
        rect.offsetMax = new Vector2(right, top);
        go.GetComponent<Image>().color = Color.clear;
    }

    // 크리처 정보 창: 어두운 바탕(누르면 닫힘) + 가운데 창
    // 창: 그림 / 이름 / 타입·가격·레벨 / 능력치 / 능력 / 장착 아이템 / 시너지 한 줄 / 안내 / 버튼(왼쪽 확인, 오른쪽 선택·취소)
    private static CreatureInfoPopup CreateCreatureInfoPopup(Transform canvas)
    {
        RectTransform root = CreateModalRoot(canvas, "CreatureInfoPopup", out Button backgroundButton);
        RectTransform window = CreatePanel(root, "Window", new Color(0.16f, 0.18f, 0.24f), true);
        Stretch(window, new Vector2(0.06f, 0.22f), new Vector2(0.94f, 0.8f));

        Button closeButton = CreateFreeButton(window, "닫기", new Vector2(0.74f, 0.875f), new Vector2(0.97f, 0.985f),
            new Color(0.7f, 0.72f, 0.78f));
        closeButton.GetComponentInChildren<Text>().resizeTextMaxSize = 40;

        var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        iconGo.transform.SetParent(window, false);
        Stretch(iconGo.GetComponent<RectTransform>(), new Vector2(0.04f, 0.63f), new Vector2(0.34f, 0.95f));
        var icon = iconGo.GetComponent<Image>();
        icon.preserveAspect = true;
        icon.raycastTarget = false;

        Text nameText = CreateLeftText(window, "Name", 60, new Vector2(0.37f, 0.83f), new Vector2(0.72f, 0.95f));
        Text infoText = CreateLeftText(window, "Info", 38, new Vector2(0.37f, 0.73f), new Vector2(0.97f, 0.82f));
        infoText.color = new Color(1f, 0.85f, 0.3f);
        Text statsText = CreateLeftText(window, "Stats", 34, new Vector2(0.37f, 0.64f), new Vector2(0.97f, 0.72f));
        Text skillText = CreateLeftText(window, "Skill", 36, new Vector2(0.05f, 0.46f), new Vector2(0.95f, 0.61f));
        Text itemText = CreateLeftText(window, "Item", 32, new Vector2(0.05f, 0.36f), new Vector2(0.95f, 0.45f));
        itemText.color = new Color(0.8f, 0.82f, 0.9f);
        Text synergyText = CreateLeftText(window, "Synergy", 32, new Vector2(0.05f, 0.26f), new Vector2(0.95f, 0.35f));
        synergyText.color = new Color(0.6f, 0.95f, 0.65f);
        Text messageText = CreateText(window, "Message", "", 40, new Vector2(0.05f, 0.17f), new Vector2(0.95f, 0.25f));
        messageText.color = new Color(1f, 0.6f, 0.5f);

        // 선택 버튼(오른쪽)과 확인 단계의 취소(같은 자리), 실행(반대쪽 왼쪽)
        Button actionButton = CreateFreeButton(window, "구매", new Vector2(0.52f, 0.03f), new Vector2(0.95f, 0.15f),
            new Color(0.95f, 0.75f, 0.25f));
        Button cancelButton = CreateFreeButton(window, "취소", new Vector2(0.52f, 0.03f), new Vector2(0.95f, 0.15f),
            new Color(0.7f, 0.72f, 0.78f));
        Button confirmButton = CreateFreeButton(window, "팔기", new Vector2(0.05f, 0.03f), new Vector2(0.48f, 0.15f),
            new Color(0.95f, 0.5f, 0.4f));
        actionButton.gameObject.SetActive(false);
        cancelButton.gameObject.SetActive(false);
        confirmButton.gameObject.SetActive(false);

        var popup = root.gameObject.AddComponent<CreatureInfoPopup>();
        var so = new SerializedObject(popup);
        so.FindProperty("_icon").objectReferenceValue = icon;
        so.FindProperty("_nameText").objectReferenceValue = nameText;
        so.FindProperty("_infoText").objectReferenceValue = infoText;
        so.FindProperty("_statsText").objectReferenceValue = statsText;
        so.FindProperty("_skillText").objectReferenceValue = skillText;
        so.FindProperty("_itemText").objectReferenceValue = itemText;
        so.FindProperty("_synergyText").objectReferenceValue = synergyText;
        so.FindProperty("_messageText").objectReferenceValue = messageText;
        so.FindProperty("_closeButton").objectReferenceValue = closeButton.transform;
        so.FindProperty("_actionButton").objectReferenceValue = actionButton;
        so.FindProperty("_actionLabel").objectReferenceValue = actionButton.GetComponentInChildren<Text>(true);
        so.FindProperty("_confirmButton").objectReferenceValue = confirmButton;
        so.FindProperty("_confirmLabel").objectReferenceValue = confirmButton.GetComponentInChildren<Text>(true);
        so.FindProperty("_cancelButton").objectReferenceValue = cancelButton;
        so.ApplyModifiedPropertiesWithoutUndo();

        UnityEventTools.AddVoidPersistentListener(backgroundButton.onClick, popup.Close);
        UnityEventTools.AddVoidPersistentListener(closeButton.onClick, popup.Close);
        UnityEventTools.AddVoidPersistentListener(actionButton.onClick, popup.OnActionClicked);
        UnityEventTools.AddVoidPersistentListener(confirmButton.onClick, popup.OnConfirmClicked);
        UnityEventTools.AddVoidPersistentListener(cancelButton.onClick, popup.OnCancelClicked);

        root.gameObject.SetActive(false);
        return popup;
    }

    // 시너지 표: 어두운 바탕(누르면 닫힘) + 가운데 창. 타입마다 한 줄: "불 3/4" / 1단계 / 2단계
    private static SynergyPanel CreateSynergyPanel(Transform canvas)
    {
        RectTransform root = CreateModalRoot(canvas, "SynergyPanel", out Button backgroundButton);
        RectTransform window = CreatePanel(root, "Window", new Color(0.16f, 0.18f, 0.24f), true);
        Stretch(window, new Vector2(0.04f, 0.2f), new Vector2(0.96f, 0.82f));

        CreateText(window, "Title", "타입 시너지", 64, new Vector2(0.05f, 0.88f), new Vector2(0.72f, 0.98f));
        Button closeButton = CreateFreeButton(window, "닫기", new Vector2(0.75f, 0.88f), new Vector2(0.97f, 0.985f),
            new Color(0.7f, 0.72f, 0.78f));
        closeButton.GetComponentInChildren<Text>().resizeTextMaxSize = 40;

        int typeCount = TypeSynergy.TypeCount;
        var typeTexts = new Text[typeCount];
        var tier1Texts = new Text[typeCount];
        var tier2Texts = new Text[typeCount];
        const float top = 0.86f;
        const float rowHeight = 0.15f;
        for (int i = 0; i < typeCount; i++)
        {
            float rowTop = top - rowHeight * i;
            float rowBottom = rowTop - rowHeight + 0.01f;
            RectTransform row = CreatePanel(window, "Row" + i, new Color(1f, 1f, 1f, 0.05f), false);
            Stretch(row, new Vector2(0.03f, rowBottom), new Vector2(0.97f, rowTop));
            typeTexts[i] = CreateLeftText(row, "Type", 46, new Vector2(0.03f, 0.1f), new Vector2(0.27f, 0.9f));
            tier1Texts[i] = CreateLeftText(row, "Tier1", 30, new Vector2(0.29f, 0.52f), new Vector2(0.98f, 0.95f));
            tier2Texts[i] = CreateLeftText(row, "Tier2", 30, new Vector2(0.29f, 0.05f), new Vector2(0.98f, 0.48f));
        }

        Text note = CreateText(window, "Note", "같은 크리처는 1마리로, 5골드 크리처는 2마리로 세요", 34,
            new Vector2(0.04f, 0.03f), new Vector2(0.96f, 0.1f));
        note.color = new Color(0.85f, 0.85f, 0.9f);

        var panel = root.gameObject.AddComponent<SynergyPanel>();
        var so = new SerializedObject(panel);
        SetArray(so.FindProperty("_typeTexts"), typeTexts);
        SetArray(so.FindProperty("_tier1Texts"), tier1Texts);
        SetArray(so.FindProperty("_tier2Texts"), tier2Texts);
        so.ApplyModifiedPropertiesWithoutUndo();

        UnityEventTools.AddVoidPersistentListener(backgroundButton.onClick, panel.Close);
        UnityEventTools.AddVoidPersistentListener(closeButton.onClick, panel.Close);

        root.gameObject.SetActive(false);
        return panel;
    }

    // 화면 전체를 덮는 어두운 바탕. 바탕(창 바깥)을 누르면 backgroundButton이 눌린다
    private static RectTransform CreateModalRoot(Transform canvas, string name, out Button backgroundButton)
    {
        RectTransform root = CreatePanel(canvas, name, new Color(0f, 0f, 0f, 0.65f), true);
        backgroundButton = root.gameObject.AddComponent<Button>();
        backgroundButton.transition = Selectable.Transition.None;
        return root;
    }

    // 왼쪽 정렬 글 (여러 줄이면 줄바꿈)
    private static Text CreateLeftText(Transform parent, string name, int size, Vector2 anchorMin, Vector2 anchorMax)
    {
        Text text = CreateText(parent, name, "", size, anchorMin, anchorMax);
        text.alignment = TextAnchor.MiddleLeft;
        return text;
    }

    // 튜토리얼 화면 (캔버스 맨 위): 어두운 판 4장 + 구멍 막이 + 노란 테두리 + 말풍선(확인) + 건너뛰기
    private static void BuildTutorialUI(Transform canvas, TutorialController tutorial, MetaProgress meta,
        ShopScreen shop, RectTransform battleButton, CreatureInfoPopup infoPopup)
    {
        BuildTutorialOverlay(canvas, tutorial, meta, shop, battleButton);
        var so = new SerializedObject(tutorial);
        so.FindProperty("_infoPopup").objectReferenceValue = infoPopup;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void BuildTutorialOverlay(Transform canvas, TutorialController tutorial, MetaProgress meta,
        ShopScreen shop, RectTransform battleButton)
    {
        var overlay = new GameObject("TutorialOverlay", typeof(RectTransform));
        overlay.transform.SetParent(canvas, false);
        var overlayRect = overlay.GetComponent<RectTransform>();
        Stretch(overlayRect, Vector2.zero, Vector2.one);

        Color dark = new Color(0f, 0f, 0f, 0.72f);
        RectTransform blockTop = CreatePanel(overlay.transform, "BlockTop", dark, true);
        RectTransform blockBottom = CreatePanel(overlay.transform, "BlockBottom", dark, true);
        RectTransform blockLeft = CreatePanel(overlay.transform, "BlockLeft", dark, true);
        RectTransform blockRight = CreatePanel(overlay.transform, "BlockRight", dark, true);
        RectTransform holeBlocker = CreatePanel(overlay.transform, "HoleBlocker", Color.clear, true);

        // 테두리: 구멍 바깥쪽에 두께 8의 노란 선 4개
        var frame = new GameObject("Frame", typeof(RectTransform));
        frame.transform.SetParent(overlay.transform, false);
        var frameRect = frame.GetComponent<RectTransform>();
        Color frameColor = new Color(1f, 0.85f, 0.25f);
        const float thickness = 8f;
        SetBorder(CreatePanel(frame.transform, "Top", frameColor, false), new Vector2(0f, 1f), Vector2.one,
            new Vector2(-thickness, 0f), new Vector2(thickness, thickness));
        SetBorder(CreatePanel(frame.transform, "Bottom", frameColor, false), Vector2.zero, new Vector2(1f, 0f),
            new Vector2(-thickness, -thickness), new Vector2(thickness, 0f));
        SetBorder(CreatePanel(frame.transform, "Left", frameColor, false), Vector2.zero, new Vector2(0f, 1f),
            new Vector2(-thickness, 0f), Vector2.zero);
        SetBorder(CreatePanel(frame.transform, "Right", frameColor, false), new Vector2(1f, 0f), Vector2.one,
            Vector2.zero, new Vector2(thickness, 0f));

        // 말풍선: 설명 + (설명 단계에서만) 확인 버튼
        RectTransform bubble = CreatePanel(overlay.transform, "Bubble", new Color(0.1f, 0.12f, 0.18f, 0.97f), true);
        Text bubbleText = CreateText(bubble, "Text", "", 42, new Vector2(0.04f, 0.06f), new Vector2(0.74f, 0.94f));
        bubbleText.alignment = TextAnchor.MiddleLeft;
        Button confirmButton = CreateFreeButton(bubble, "확인", new Vector2(0.77f, 0.18f), new Vector2(0.97f, 0.82f),
            new Color(0.95f, 0.75f, 0.25f));

        // 오른쪽 위 건너뛰기 (항상 맨 위)
        Button skipButton = CreateFreeButton(overlay.transform, "건너뛰기", new Vector2(0.76f, 0.88f), new Vector2(0.98f, 0.945f),
            new Color(0.75f, 0.77f, 0.82f));
        skipButton.GetComponentInChildren<Text>().resizeTextMaxSize = 38;

        overlay.SetActive(false);

        var so = new SerializedObject(tutorial);
        so.FindProperty("_data").objectReferenceValue = GetOrCreateTutorialData();
        so.FindProperty("_meta").objectReferenceValue = meta;
        so.FindProperty("_shop").objectReferenceValue = shop;
        so.FindProperty("_battleButton").objectReferenceValue = battleButton;
        so.FindProperty("_overlay").objectReferenceValue = overlayRect;
        so.FindProperty("_blockTop").objectReferenceValue = blockTop;
        so.FindProperty("_blockBottom").objectReferenceValue = blockBottom;
        so.FindProperty("_blockLeft").objectReferenceValue = blockLeft;
        so.FindProperty("_blockRight").objectReferenceValue = blockRight;
        so.FindProperty("_holeBlocker").objectReferenceValue = holeBlocker;
        so.FindProperty("_frame").objectReferenceValue = frameRect;
        so.FindProperty("_bubble").objectReferenceValue = bubble;
        so.FindProperty("_bubbleText").objectReferenceValue = bubbleText;
        so.FindProperty("_confirmButton").objectReferenceValue = confirmButton.gameObject;
        so.ApplyModifiedPropertiesWithoutUndo();

        UnityEventTools.AddVoidPersistentListener(confirmButton.onClick, tutorial.Confirm);
        UnityEventTools.AddVoidPersistentListener(skipButton.onClick, tutorial.Skip);
    }

    // 색 판 (위치는 튜토리얼이 정한다). blocksInput: 뒤의 버튼이 눌리지 않게 막는지
    private static RectTransform CreatePanel(Transform parent, string name, Color color, bool blocksInput)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var image = go.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = blocksInput;
        var rect = go.GetComponent<RectTransform>();
        Stretch(rect, Vector2.zero, Vector2.one);
        return rect;
    }

    private static void SetBorder(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
    }

    // 튜토리얼 문구·정해 둔 상점·약한 적 팀. 없으면 기본값으로 만들고, 있으면 그대로 쓴다 (고친 문구 보호)
    // 정보 창으로 구매·판매하게 바뀐 뒤의 문구
    private const string NewBuyMessage = "상점에서 크리처를 사 볼까요?\n빛나는 칸을 눌러 정보를 보고, 구매 버튼을 누르세요.";
    private const string NewMergeMessage = "불똥이를 2마리 더 사서 3마리를 모아 보세요.\n칸을 누르고 구매 버튼을 누르면 돼요.";
    private const string NewSellMessage = "크리처를 누르면 정보 창이 떠요.\n판매 버튼을 누르고 한 번 더 확인하면 팔 수 있어요.";

    // 예전 기본 문구 → 새 문구 (직접 고친 문구는 그대로 둔다)
    private static readonly (string oldMessage, string newMessage)[] TutorialMessageUpdates =
    {
        ("상점에서 크리처를 사 볼까요?\n빛나는 칸을 눌러 불똥이를 사세요.", NewBuyMessage),
        ("불똥이를 2마리 더 사서\n3마리를 모아 보세요.", NewMergeMessage),
        ("대기석의 크리처를 누르면 팔아서\n골드를 돌려받을 수 있어요.", NewSellMessage),
    };

    private static TutorialData GetOrCreateTutorialData()
    {
        var data = AssetDatabase.LoadAssetAtPath<TutorialData>(TutorialDataPath);
        if (data != null)
        {
            UpdateTutorialMessages(data);
            return data;
        }

        Directory.CreateDirectory(TutorialFolder);
        AssetDatabase.Refresh();

        data = ScriptableObject.CreateInstance<TutorialData>();
        AssetDatabase.CreateAsset(data, TutorialDataPath);
        var so = new SerializedObject(data);

        (TutorialStep step, string message)[] steps =
        {
            (TutorialStep.BuyCreature, NewBuyMessage),
            (TutorialStep.PlaceInTeam, "산 크리처는 아래 대기석에 들어가요.\n손가락으로 끌어서 팀 칸(앞줄)에 놓으세요."),
            (TutorialStep.StartBattle, "팀이 준비됐어요!\n전투 시작을 누르세요."),
            (TutorialStep.BattleInfo, "전투는 자동으로 진행돼요.\n크리처마다 공격 속도에 맞춰 상대 앞줄부터 공격해요."),
            (TutorialStep.MergeInfo, "같은 크리처 3마리를 모으면\n1마리로 합쳐져 더 강해져요! (★ → ★★)"),
            (TutorialStep.Merge, NewMergeMessage),
            (TutorialStep.SellInfo, NewSellMessage),
            (TutorialStep.SynergyInfo, "같은 타입 크리처 2마리를 팀에 두면 여기에 시너지가 켜져요. (예: 불 2/4)\n이제 마음껏 플레이해 보세요!"),
        };
        SerializedProperty stepList = so.FindProperty("_steps");
        stepList.arraySize = steps.Length;
        for (int i = 0; i < steps.Length; i++)
        {
            SerializedProperty element = stepList.GetArrayElementAtIndex(i);
            element.FindPropertyRelative("Step").enumValueIndex = (int)steps[i].step;
            element.FindPropertyRelative("Message").stringValue = steps[i].message;
        }

        so.FindProperty("_mergeCreature").objectReferenceValue = LoadCreature("Embit");
        SetArray(so.FindProperty("_round1Shop"), LoadCreatures("Embit", "Sproutee", "Plopper", "Pebblet", "Zappy"));
        SetArray(so.FindProperty("_round2Shop"), LoadCreatures("Embit", "Zappy", "Embit", "Plopper", "Sproutee"));
        SetArray(so.FindProperty("_enemyTeams"), new Object[]
        {
            CreateTutorialEnemyTeam("TutorialRound01", "연습 상대", "Sproutee"),
            CreateTutorialEnemyTeam("TutorialRound02", "연습 상대 2", "Pebblet"),
        });

        so.ApplyModifiedPropertiesWithoutUndo();
        AssetDatabase.SaveAssets();
        return data;
    }

    // 이미 있는 튜토리얼 문구 중 예전 기본 문구 그대로인 것만 새 문구로 바꾼다
    private static void UpdateTutorialMessages(TutorialData data)
    {
        var so = new SerializedObject(data);
        SerializedProperty steps = so.FindProperty("_steps");
        int updated = 0;
        for (int i = 0; i < steps.arraySize; i++)
        {
            SerializedProperty message = steps.GetArrayElementAtIndex(i).FindPropertyRelative("Message");
            foreach ((string oldMessage, string newMessage) in TutorialMessageUpdates)
            {
                if (message.stringValue == oldMessage)
                {
                    message.stringValue = newMessage;
                    updated++;
                }
            }
        }
        if (updated > 0)
        {
            so.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();
            Debug.Log($"[튜토리얼] 문구 {updated}개를 정보 창 흐름에 맞게 바꿨습니다");
        }
    }

    // 약한 적 팀: 앞줄 첫 칸에 Lv1 크리처 1마리
    private static EnemyTeamData CreateTutorialEnemyTeam(string id, string displayName, string creatureId)
    {
        string path = $"{TutorialFolder}/{id}.asset";
        var team = AssetDatabase.LoadAssetAtPath<EnemyTeamData>(path);
        if (team != null)
        {
            return team;
        }

        team = ScriptableObject.CreateInstance<EnemyTeamData>();
        AssetDatabase.CreateAsset(team, path);
        var so = new SerializedObject(team);
        so.FindProperty("_displayName").stringValue = displayName;
        SerializedProperty members = so.FindProperty("_members");
        members.arraySize = 5;
        for (int i = 0; i < members.arraySize; i++)
        {
            SerializedProperty member = members.GetArrayElementAtIndex(i);
            member.FindPropertyRelative("Creature").objectReferenceValue = i == 0 ? LoadCreature(creatureId) : null;
            member.FindPropertyRelative("Level").intValue = 1;
        }
        so.ApplyModifiedPropertiesWithoutUndo();
        return team;
    }

    private static CreatureData LoadCreature(string id)
    {
        return AssetDatabase.LoadAssetAtPath<CreatureData>($"{CreatureFolder}/{id}.asset");
    }

    private static Object[] LoadCreatures(params string[] ids)
    {
        var creatures = new Object[ids.Length];
        for (int i = 0; i < ids.Length; i++)
        {
            creatures[i] = LoadCreature(ids[i]);
        }
        return creatures;
    }

    // 도감 화면: 제목 / 별 / 트레이너 3칸 / 크리처 35칸(위아래 스크롤, 줄 = 가격, 같은 가격은 타입 순) / 안내 문구 / 뒤로
    private static void BuildCodexUI(GameObject screen, ScreenFlow flow, MetaProgress meta)
    {
        Stretch((RectTransform)screen.transform.Find("Title"), new Vector2(0f, 0.925f), new Vector2(1f, 0.985f));
        Stretch((RectTransform)screen.transform.Find("ButtonArea"), new Vector2(0.25f, 0.015f), new Vector2(0.75f, 0.1f));

        Text starsText = CreateText(screen.transform, "StarsText", "", 52, new Vector2(0f, 0.88f), new Vector2(1f, 0.925f));
        starsText.color = new Color(1f, 0.85f, 0.3f);

        Text trainerLabel = CreateText(screen.transform, "TrainerLabel", "트레이너", 36, new Vector2(0.03f, 0.85f), new Vector2(0.97f, 0.88f));
        trainerLabel.alignment = TextAnchor.MiddleLeft;
        Transform trainerRow = CreateRow(screen.transform, "TrainerRow", new Vector2(0.03f, 0.7f), new Vector2(0.97f, 0.85f));
        TrainerData[] trainers = GetOrCreateTrainers();
        var trainerEntries = new CodexEntryView[trainers.Length];
        for (int i = 0; i < trainers.Length; i++)
        {
            trainerEntries[i] = CreateCodexEntry(trainerRow, "Trainer" + i, false);
        }

        Text creatureLabel = CreateText(screen.transform, "CreatureLabel", "크리처", 36, new Vector2(0.03f, 0.665f), new Vector2(0.97f, 0.695f));
        creatureLabel.alignment = TextAnchor.MiddleLeft;
        // 크리처 35종은 위아래로 스크롤. 가격이 바뀌면 새 줄, 한 줄에 최대 5칸 (1·2골드 2줄, 3·4·5골드 1줄)
        CreatureData[] creatures = GetSortedCreatures();
        Transform scrollContent = CreateVerticalScroll(screen.transform, "CreatureScroll",
            new Vector2(0.03f, 0.135f), new Vector2(0.97f, 0.66f));
        var creatureEntries = new CodexEntryView[creatures.Length];
        Transform row = null;
        int rowTier = 0;
        int column = 0;
        for (int i = 0; i < creatures.Length; i++)
        {
            if (row == null || column >= CodexColumns || creatures[i].Tier != rowTier)
            {
                row = CreateScrollRow(scrollContent, "Row" + creatures[i].Tier + "Gold");
                rowTier = creatures[i].Tier;
                column = 0;
            }
            creatureEntries[i] = CreateCodexEntry(row, "Creature" + i, true);
            creatureEntries[i].gameObject.AddComponent<LayoutElement>().preferredWidth = CodexCellWidth;
            column++;
        }

        Text messageText = CreateText(screen.transform, "MessageText", "", 36, new Vector2(0f, 0.1f), new Vector2(1f, 0.13f));
        messageText.color = new Color(0.6f, 0.95f, 0.65f);

        AddButton(screen, "뒤로", flow, GameScreen.Title);

        var codex = screen.AddComponent<CodexScreen>();
        var so = new SerializedObject(codex);
        so.FindProperty("_meta").objectReferenceValue = meta;
        SetArray(so.FindProperty("_trainers"), trainers);
        SetArray(so.FindProperty("_trainerEntries"), trainerEntries);
        SetArray(so.FindProperty("_creatures"), creatures);
        SetArray(so.FindProperty("_creatureEntries"), creatureEntries);
        so.FindProperty("_starsText").objectReferenceValue = starsText;
        so.FindProperty("_messageText").objectReferenceValue = messageText;
        so.ApplyModifiedPropertiesWithoutUndo();

        for (int i = 0; i < trainerEntries.Length; i++)
        {
            UnityEventTools.AddIntPersistentListener(trainerEntries[i].GetComponentInChildren<Button>(true).onClick, codex.UnlockTrainer, i);
        }
        for (int i = 0; i < creatureEntries.Length; i++)
        {
            UnityEventTools.AddIntPersistentListener(creatureEntries[i].GetComponentInChildren<Button>(true).onClick, codex.UnlockCreature, i);
        }
    }

    // 위아래 스크롤 목록. 돌려주는 Transform에 줄을 넣는다 (줄 높이만큼 자동으로 길어짐)
    private static Transform CreateVerticalScroll(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax)
    {
        var scroll = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(RectMask2D), typeof(ScrollRect));
        scroll.transform.SetParent(parent, false);
        var scrollRect = scroll.GetComponent<RectTransform>();
        Stretch(scrollRect, anchorMin, anchorMax);
        // 빈 곳을 끌어도 스크롤되도록 거의 투명한 바탕
        scroll.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.15f);

        var content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        content.transform.SetParent(scroll.transform, false);
        var contentRect = content.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.offsetMin = Vector2.zero;
        contentRect.offsetMax = Vector2.zero;
        var layout = content.GetComponent<VerticalLayoutGroup>();
        layout.spacing = 14;
        layout.padding = new RectOffset(0, 0, 8, 8);
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        content.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var scrollView = scroll.GetComponent<ScrollRect>();
        scrollView.content = contentRect;
        scrollView.viewport = scrollRect;
        scrollView.horizontal = false;
        scrollView.vertical = true;
        scrollView.movementType = ScrollRect.MovementType.Clamped;
        scrollView.scrollSensitivity = 40;
        return content.transform;
    }

    // 스크롤 목록의 한 줄 (칸은 가운데 정렬, 칸 너비는 칸의 LayoutElement)
    private static Transform CreateScrollRow(Transform content, string name)
    {
        var row = new GameObject(name, typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        row.transform.SetParent(content, false);
        var layout = row.GetComponent<HorizontalLayoutGroup>();
        layout.spacing = 12;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = true;
        row.GetComponent<LayoutElement>().preferredHeight = CodexRowHeight;
        return row.transform;
    }

    // 도감 한 칸. withIcon: 크리처 칸(그림 있음) / 트레이너 칸(이름 + 설명)
    private static CodexEntryView CreateCodexEntry(Transform parent, string name, bool withIcon)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(CodexEntryView));
        go.transform.SetParent(parent, false);
        go.GetComponent<Image>().color = new Color(0.25f, 0.28f, 0.34f);

        Image icon = null;
        Text nameText;
        Text infoText;
        if (withIcon)
        {
            var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconGo.transform.SetParent(go.transform, false);
            Stretch(iconGo.GetComponent<RectTransform>(), new Vector2(0.15f, 0.47f), new Vector2(0.85f, 0.93f));
            icon = iconGo.GetComponent<Image>();
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            nameText = CreateText(go.transform, "Name", "", 28, new Vector2(0.02f, 0.34f), new Vector2(0.98f, 0.46f));
            infoText = CreateText(go.transform, "Info", "", 22, new Vector2(0.02f, 0.25f), new Vector2(0.98f, 0.34f));
        }
        else
        {
            nameText = CreateText(go.transform, "Name", "", 40, new Vector2(0.04f, 0.7f), new Vector2(0.96f, 0.95f));
            infoText = CreateText(go.transform, "Info", "", 24, new Vector2(0.04f, 0.3f), new Vector2(0.96f, 0.7f));
        }
        infoText.color = new Color(0.85f, 0.85f, 0.9f);

        // 잠금 덮개는 해금 버튼보다 먼저 만들어 버튼은 어두워지지 않게 한다
        // 크리처 칸은 그림 위에, 트레이너 칸은 오른쪽 위에 자물쇠
        Vector2 lockMin = withIcon ? new Vector2(0.3f, 0.55f) : new Vector2(0.8f, 0.6f);
        Vector2 lockMax = withIcon ? new Vector2(0.7f, 0.88f) : new Vector2(0.96f, 0.95f);
        GameObject lockOverlay = CreateLockOverlay(go.transform, lockMin, lockMax, out _, Vector2.zero, Vector2.zero, 24);

        Button unlockButton = CreateFreeButton(go.transform, "해금", new Vector2(0.06f, 0.03f), new Vector2(0.94f, 0.23f),
            new Color(0.95f, 0.75f, 0.25f));
        Text unlockLabel = unlockButton.GetComponentInChildren<Text>();
        unlockLabel.resizeTextMaxSize = 30;
        unlockLabel.fontSize = 30;
        // 버튼 높이가 약 60px라 위쪽(이름·설명 글자 쪽)으로 눌리는 범위를 넓힌다
        AddHitArea(unlockButton, 0f, 6f, 0f, 60f);

        var view = go.GetComponent<CodexEntryView>();
        var so = new SerializedObject(view);
        so.FindProperty("_icon").objectReferenceValue = icon;
        so.FindProperty("_nameText").objectReferenceValue = nameText;
        so.FindProperty("_infoText").objectReferenceValue = infoText;
        so.FindProperty("_lockOverlay").objectReferenceValue = lockOverlay;
        so.FindProperty("_unlockButton").objectReferenceValue = unlockButton;
        so.FindProperty("_unlockLabel").objectReferenceValue = unlockLabel;
        so.ApplyModifiedPropertiesWithoutUndo();
        return view;
    }

    // 잠금 덮개: 부모 전체를 어둡게 + 자물쇠 그림 + 글자 (처음엔 숨김, 눌림은 막지 않는다)
    private static GameObject CreateLockOverlay(Transform parent, Vector2 lockMin, Vector2 lockMax,
        out Text label, Vector2 labelMin, Vector2 labelMax, int fontSize)
    {
        var overlay = new GameObject("LockOverlay", typeof(RectTransform), typeof(Image));
        overlay.transform.SetParent(parent, false);
        Stretch(overlay.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);
        var dim = overlay.GetComponent<Image>();
        dim.color = new Color(0f, 0f, 0f, 0.6f);
        dim.raycastTarget = false;

        var lockGo = new GameObject("Lock", typeof(RectTransform), typeof(Image));
        lockGo.transform.SetParent(overlay.transform, false);
        Stretch(lockGo.GetComponent<RectTransform>(), lockMin, lockMax);
        var lockImage = lockGo.GetComponent<Image>();
        lockImage.sprite = GetOrCreateLockSprite();
        lockImage.preserveAspect = true;
        lockImage.raycastTarget = false;

        label = CreateText(overlay.transform, "LockText", "", fontSize, labelMin, labelMax);
        label.color = new Color(1f, 0.85f, 0.3f);

        overlay.SetActive(false);
        return overlay;
    }

    // 자물쇠 그림 (임시): 흰색 자물쇠 64x64 PNG를 코드로 그린다
    private static Sprite GetOrCreateLockSprite()
    {
        if (!File.Exists(LockSpritePath))
        {
            const int size = 64;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var center = new Vector2(32f, 34f);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    // 몸통 (아래 사각형, 가운데 열쇠 구멍)
                    bool body = x >= 12 && x < 52 && y >= 4 && y < 34;
                    bool keyhole = x >= 29 && x < 35 && y >= 12 && y < 24;
                    // 고리 (위쪽 반원 띠)
                    float distance = Vector2.Distance(new Vector2(x, y), center);
                    bool shackle = y >= 34 && distance >= 10f && distance <= 16f;
                    bool filled = (body && !keyhole) || shackle;
                    texture.SetPixel(x, y, filled ? Color.white : Color.clear);
                }
            }

            Directory.CreateDirectory(Path.GetDirectoryName(LockSpritePath));
            File.WriteAllBytes(LockSpritePath, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);

            AssetDatabase.ImportAsset(LockSpritePath);
            var importer = (TextureImporter)AssetImporter.GetAtPath(LockSpritePath);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(LockSpritePath);
    }

    // 도감 순서: 가격(등급) 순, 같은 가격은 타입 순
    private static CreatureData[] GetSortedCreatures()
    {
        string[] guids = AssetDatabase.FindAssets("t:CreatureData", new[] { CreatureFolder });
        var creatures = new CreatureData[guids.Length];
        for (int i = 0; i < guids.Length; i++)
        {
            creatures[i] = AssetDatabase.LoadAssetAtPath<CreatureData>(AssetDatabase.GUIDToAssetPath(guids[i]));
        }
        System.Array.Sort(creatures, (a, b) => a.Tier != b.Tier ? a.Tier.CompareTo(b.Tier) : a.Type.CompareTo(b.Type));
        return creatures;
    }

    // 메타 성장 설정. 없으면 기본값(GAME_DESIGN.md 9-2)으로 만들고, 있으면 그대로 쓴다 (조정한 값 보호)
    private static MetaConfig GetOrCreateMetaConfig()
    {
        var config = AssetDatabase.LoadAssetAtPath<MetaConfig>(MetaConfigPath);
        if (config != null)
        {
            return config;
        }

        config = ScriptableObject.CreateInstance<MetaConfig>();
        AssetDatabase.CreateAsset(config, MetaConfigPath);
        var so = new SerializedObject(config);

        SerializedProperty trainers = so.FindProperty("_lockedTrainers");
        trainers.arraySize = 1;
        trainers.GetArrayElementAtIndex(0).FindPropertyRelative("Trainer").objectReferenceValue =
            AssetDatabase.LoadAssetAtPath<TrainerData>($"{TrainerFolder}/Merchant.asset");
        trainers.GetArrayElementAtIndex(0).FindPropertyRelative("Cost").intValue = 15;

        // 5골드 5종을 각 별 10개로 잠근다 (1~5골드 개편, CREATURE_TIERS.md)
        string[] lockedCreatures = { "SunPhoenix", "AbyssDragon", "ElderTreant", "VoltQilin", "MountainTortoise" };
        SerializedProperty creatures = so.FindProperty("_lockedCreatures");
        creatures.arraySize = lockedCreatures.Length;
        for (int i = 0; i < lockedCreatures.Length; i++)
        {
            SerializedProperty element = creatures.GetArrayElementAtIndex(i);
            element.FindPropertyRelative("Creature").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<CreatureData>($"{CreatureFolder}/{lockedCreatures[i]}.asset");
            element.FindPropertyRelative("Cost").intValue = 10;
        }

        so.ApplyModifiedPropertiesWithoutUndo();
        AssetDatabase.SaveAssets();
        return config;
    }

    // 트레이너 선택 화면: 제목 / 카드 3장(이름·설명, 눌러서 선택) / 안내 문구 / 선택 완료
    private static void BuildTrainerSelectUI(GameObject screen, RunManager run, MetaProgress meta)
    {
        Stretch((RectTransform)screen.transform.Find("Title"), new Vector2(0f, 0.8f), new Vector2(1f, 0.92f));

        var cardArea = new GameObject("Cards", typeof(RectTransform), typeof(VerticalLayoutGroup));
        cardArea.transform.SetParent(screen.transform, false);
        Stretch(cardArea.GetComponent<RectTransform>(), new Vector2(0.08f, 0.3f), new Vector2(0.92f, 0.78f));
        var layout = cardArea.GetComponent<VerticalLayoutGroup>();
        layout.spacing = 36;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = true;

        TrainerData[] trainers = GetOrCreateTrainers();
        var cards = new TrainerCardView[trainers.Length];
        var cardButtons = new Button[trainers.Length];
        for (int i = 0; i < trainers.Length; i++)
        {
            var card = new GameObject("TrainerCard" + i, typeof(RectTransform), typeof(Image), typeof(Button), typeof(TrainerCardView));
            card.transform.SetParent(cardArea.transform, false);
            var background = card.GetComponent<Image>();

            Text nameText = CreateText(card.transform, "Name", "", 56, new Vector2(0.05f, 0.55f), new Vector2(0.95f, 0.92f));
            Text descriptionText = CreateText(card.transform, "Description", "", 36, new Vector2(0.05f, 0.08f), new Vector2(0.95f, 0.55f));
            descriptionText.color = new Color(0.9f, 0.9f, 0.9f);

            // 잠겼을 때: 카드 전체를 어둡게 + 오른쪽에 자물쇠와 필요한 별
            GameObject lockOverlay = CreateLockOverlay(card.transform, new Vector2(0.8f, 0.5f), new Vector2(0.96f, 0.92f),
                out Text lockText, new Vector2(0.62f, 0.06f), new Vector2(0.98f, 0.48f), 32);

            cards[i] = card.GetComponent<TrainerCardView>();
            var cardSo = new SerializedObject(cards[i]);
            cardSo.FindProperty("_background").objectReferenceValue = background;
            cardSo.FindProperty("_nameText").objectReferenceValue = nameText;
            cardSo.FindProperty("_descriptionText").objectReferenceValue = descriptionText;
            cardSo.FindProperty("_lockOverlay").objectReferenceValue = lockOverlay;
            cardSo.FindProperty("_lockText").objectReferenceValue = lockText;
            cardSo.ApplyModifiedPropertiesWithoutUndo();
            background.color = new Color(0.25f, 0.28f, 0.34f);
            cardButtons[i] = card.GetComponent<Button>();
        }

        Text messageText = CreateText(screen.transform, "MessageText", "", 44, new Vector2(0f, 0.2f), new Vector2(1f, 0.26f));
        messageText.color = new Color(1f, 0.5f, 0.45f);

        Button confirmButton = CreateButton(screen, "선택 완료");

        var select = screen.AddComponent<TrainerSelectScreen>();
        var so = new SerializedObject(select);
        so.FindProperty("_run").objectReferenceValue = run;
        so.FindProperty("_meta").objectReferenceValue = meta;
        SetArray(so.FindProperty("_trainers"), trainers);
        SetArray(so.FindProperty("_cards"), cards);
        so.FindProperty("_messageText").objectReferenceValue = messageText;
        so.ApplyModifiedPropertiesWithoutUndo();

        for (int i = 0; i < cardButtons.Length; i++)
        {
            UnityEventTools.AddIntPersistentListener(cardButtons[i].onClick, select.Select, i);
        }
        UnityEventTools.AddVoidPersistentListener(confirmButton.onClick, select.Confirm);
    }

    // 트레이너 3명 에셋. 없는 것만 기본값(GAME_DESIGN.md 5번)으로 만든다 (조정한 값 보호)
    private static TrainerData[] GetOrCreateTrainers()
    {
        Directory.CreateDirectory(TrainerFolder);
        AssetDatabase.Refresh();

        return new[]
        {
            GetOrCreateTrainer("FlameTamer", "불꽃 조련사", "불 타입 공격력 +20%",
                TrainerAbility.TypeAttackBonus, CreatureType.Fire, 0.2f),
            GetOrCreateTrainer("ForestKeeper", "숲지기", "풀 타입이 전투 중 체력이 처음 50% 아래로 떨어지면\n최대 체력의 30%를 한 번 회복",
                TrainerAbility.TypeLowHealthHeal, CreatureType.Grass, 0.3f),
            GetOrCreateTrainer("Merchant", "상인", "라운드마다 골드 +1",
                TrainerAbility.RoundGoldBonus, CreatureType.Fire, 1f),
        };
    }

    private static TrainerData GetOrCreateTrainer(string id, string name, string description,
        TrainerAbility ability, CreatureType targetType, float value)
    {
        string path = $"{TrainerFolder}/{id}.asset";
        var trainer = AssetDatabase.LoadAssetAtPath<TrainerData>(path);
        if (trainer != null)
        {
            return trainer;
        }

        trainer = ScriptableObject.CreateInstance<TrainerData>();
        AssetDatabase.CreateAsset(trainer, path);
        var so = new SerializedObject(trainer);
        so.FindProperty("_displayName").stringValue = name;
        so.FindProperty("_description").stringValue = description;
        so.FindProperty("_ability").enumValueIndex = (int)ability;
        so.FindProperty("_targetType").enumValueIndex = (int)targetType;
        so.FindProperty("_value").floatValue = value;
        so.FindProperty("_healthThreshold").floatValue = 0.5f;
        so.ApplyModifiedPropertiesWithoutUndo();
        AssetDatabase.SaveAssets();
        return trainer;
    }

    // 상단 표시: 라운드 / 목숨 / 골드 (상점·전투 화면에서만 보인다)
    private static GameObject CreateHud(Transform canvas, out Text roundText, out Text livesText, out Text goldText)
    {
        var hud = new GameObject("Hud", typeof(RectTransform), typeof(Image));
        hud.transform.SetParent(canvas, false);
        Stretch(hud.GetComponent<RectTransform>(), new Vector2(0f, 0.945f), Vector2.one);
        var background = hud.GetComponent<Image>();
        background.color = new Color(0.06f, 0.07f, 0.1f);
        background.raycastTarget = false;

        roundText = CreateText(hud.transform, "RoundText", "", 44, new Vector2(0.03f, 0f), new Vector2(0.42f, 1f));
        roundText.alignment = TextAnchor.MiddleLeft;
        livesText = CreateText(hud.transform, "LivesText", "", 44, new Vector2(0.42f, 0f), new Vector2(0.7f, 1f));
        livesText.color = new Color(1f, 0.45f, 0.5f);
        goldText = CreateText(hud.transform, "GoldText", "", 44, new Vector2(0.7f, 0f), new Vector2(0.97f, 1f));
        goldText.alignment = TextAnchor.MiddleRight;
        goldText.color = new Color(1f, 0.85f, 0.3f);

        hud.SetActive(false);
        return hud;
    }

    // 화면 전환 버튼
    private static Button AddButton(GameObject screen, string label, ScreenFlow flow, GameScreen target)
    {
        Button button = CreateButton(screen, label);
        UnityEventTools.AddIntPersistentListener(button.onClick, flow.ShowScreen, (int)target);
        return button;
    }

    // 하단 ButtonArea에 버튼만 만든다 (연결은 호출하는 쪽에서)
    private static Button CreateButton(GameObject screen, string label)
    {
        Transform area = screen.transform.Find("ButtonArea");

        var go = new GameObject(label + "Button", typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(area, false);
        go.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 160);
        go.GetComponent<Image>().color = new Color(0.95f, 0.75f, 0.25f);

        var text = new GameObject("Text", typeof(RectTransform), typeof(Text));
        text.transform.SetParent(go.transform, false);
        Stretch(text.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);
        SetupText(text.GetComponent<Text>(), label, 60);
        text.GetComponent<Text>().color = Color.black;

        Button button = go.GetComponent<Button>();
        AddPressFeedback(button);
        return button;
    }

    // 전투 화면 (세로 1920 기준, 위에서부터)
    // (상단 표시) / 적 팀 이름 / 적 뒷줄 3칸 / 적 앞줄 2칸 / 승패 문구 / 내 팀 앞줄 2칸 / 내 팀 뒷줄 3칸 / 배속 버튼
    // 두 팀의 앞줄이 가운데에서 마주 본다 (내 팀은 배치 화면과 같은 모양)
    private static BattleScreen BuildBattleUI(GameObject screen, RunManager run)
    {
        // 제목 자리는 상단 표시가 차지한다
        screen.transform.Find("Title").gameObject.SetActive(false);
        Stretch((RectTransform)screen.transform.Find("ButtonArea"), new Vector2(0.25f, 0.015f), new Vector2(0.75f, 0.105f));

        Color playerColor = new Color(0.22f, 0.32f, 0.45f);
        Color enemyColor = new Color(0.45f, 0.24f, 0.24f);

        Text enemyLabel = CreateText(screen.transform, "EnemyLabel", "적 팀", 40, new Vector2(0f, 0.895f), new Vector2(1f, 0.935f));
        Transform enemyBack = CreateRow(screen.transform, "EnemyBackRow", new Vector2(0.03f, 0.745f), new Vector2(0.97f, 0.89f));
        Transform enemyFront = CreateRow(screen.transform, "EnemyFrontRow", new Vector2(0.2f, 0.595f), new Vector2(0.8f, 0.74f));

        Text bannerText = CreateText(screen.transform, "BannerText", "", 110, new Vector2(0f, 0.475f), new Vector2(1f, 0.585f));
        bannerText.color = new Color(1f, 0.85f, 0.3f);

        Transform playerFront = CreateRow(screen.transform, "PlayerFrontRow", new Vector2(0.2f, 0.32f), new Vector2(0.8f, 0.465f));
        Transform playerBack = CreateRow(screen.transform, "PlayerBackRow", new Vector2(0.03f, 0.17f), new Vector2(0.97f, 0.315f));
        CreateText(screen.transform, "PlayerLabel", "내 팀", 40, new Vector2(0f, 0.125f), new Vector2(1f, 0.165f));

        var playerViews = new CreatureUnitView[5];
        var enemyViews = new CreatureUnitView[5];
        for (int i = 0; i < 5; i++)
        {
            bool front = i < 2;
            playerViews[i] = CreateUnitSlot(front ? playerFront : playerBack, "PlayerSlot" + i, playerColor, true);
            enemyViews[i] = CreateUnitSlot(front ? enemyFront : enemyBack, "EnemySlot" + i, enemyColor, false);
        }

        // 스킬 글·데미지 숫자 풀: 크리처 칸보다 위에 그려지도록 칸 다음에 만든다 (전투 중 새로 만들지 않고 돌려 씀)
        FloatingTextPool floatingTexts = CreateFloatingTextPool(screen.transform, FloatingTextCount);

        Button speedButton = CreateButton(screen, "1배속");
        Text speedLabel = speedButton.GetComponentInChildren<Text>();

        var battle = screen.AddComponent<BattleScreen>();
        var so = new SerializedObject(battle);
        so.FindProperty("_run").objectReferenceValue = run;
        var typeChart = AssetDatabase.LoadAssetAtPath<TypeChart>(TypeChartPath);
        if (typeChart == null)
        {
            Debug.LogError("TypeChart 에셋이 없습니다. 먼저 MonsterCamp > Create Initial Creature Data 를 실행하세요.");
        }
        so.FindProperty("_typeChart").objectReferenceValue = typeChart;
        so.FindProperty("_synergy").objectReferenceValue = GetOrCreateSynergyConfig();
        SetArray(so.FindProperty("_playerViews"), playerViews);
        SetArray(so.FindProperty("_enemyViews"), enemyViews);
        so.FindProperty("_enemyLabel").objectReferenceValue = enemyLabel;
        so.FindProperty("_bannerText").objectReferenceValue = bannerText;
        so.FindProperty("_speedLabel").objectReferenceValue = speedLabel;
        so.FindProperty("_floatingTexts").objectReferenceValue = floatingTexts;
        so.ApplyModifiedPropertiesWithoutUndo();

        UnityEventTools.AddVoidPersistentListener(speedButton.onClick, battle.ToggleSpeed);
        return battle;
    }

    // 떠오르는 글 count개를 미리 만들어 둔 풀 (처음엔 모두 숨김)
    private static FloatingTextPool CreateFloatingTextPool(Transform parent, int count)
    {
        var pool = new GameObject("FloatingTexts", typeof(RectTransform), typeof(FloatingTextPool));
        pool.transform.SetParent(parent, false);
        Stretch(pool.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);

        var texts = new Text[count];
        for (int i = 0; i < count; i++)
        {
            // 위치는 띄울 때 정하므로 가운데 기준 고정 크기
            Text text = CreateText(pool.transform, "Text" + i, "", 44, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            text.rectTransform.sizeDelta = new Vector2(320f, 70f);
            text.fontStyle = FontStyle.Bold;
            text.gameObject.AddComponent<Outline>().effectColor = new Color(0f, 0f, 0f, 0.8f);
            text.gameObject.SetActive(false);
            texts[i] = text;
        }

        var component = pool.GetComponent<FloatingTextPool>();
        var so = new SerializedObject(component);
        SetArray(so.FindProperty("_texts"), texts);
        so.ApplyModifiedPropertiesWithoutUndo();
        return component;
    }

    // 전투 칸: 바탕(빈 칸 표시) + 크리처(체력바, 그림, 이름, 맞으면 깜빡이는 흰 판). 크리처가 죽으면 크리처만 사라진다
    // facesUp: 공격할 때 튀어나가는 쪽 (내 팀 위, 적 팀 아래)
    private static CreatureUnitView CreateUnitSlot(Transform row, string name, Color color, bool facesUp)
    {
        var slot = new GameObject(name, typeof(RectTransform), typeof(Image));
        slot.transform.SetParent(row, false);
        slot.GetComponent<Image>().color = new Color(color.r, color.g, color.b, 0.35f);

        var unit = new GameObject("Unit", typeof(RectTransform), typeof(Image), typeof(CreatureUnitView));
        unit.transform.SetParent(slot.transform, false);
        Stretch(unit.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);
        unit.GetComponent<Image>().color = color;

        var healthBack = new GameObject("HealthBar", typeof(RectTransform), typeof(Image));
        healthBack.transform.SetParent(unit.transform, false);
        Stretch(healthBack.GetComponent<RectTransform>(), new Vector2(0.08f, 0.86f), new Vector2(0.92f, 0.95f));
        healthBack.GetComponent<Image>().color = new Color(0.1f, 0.1f, 0.1f);

        // 체력 칸 뒤 흰 막대: 체력이 줄면 잠깐 뒤에 천천히 따라 줄어든다
        var healthTrail = new GameObject("Trail", typeof(RectTransform), typeof(Image));
        healthTrail.transform.SetParent(healthBack.transform, false);
        Stretch(healthTrail.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);
        healthTrail.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.85f);

        var healthFill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        healthFill.transform.SetParent(healthBack.transform, false);
        Stretch(healthFill.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);
        healthFill.GetComponent<Image>().color = new Color(0.3f, 0.9f, 0.35f);

        // 보호막: 체력 칸 뒤에 이어지는 흰 칸 (보호막이 있을 때만 보임)
        var shieldFill = new GameObject("Shield", typeof(RectTransform), typeof(Image));
        shieldFill.transform.SetParent(healthBack.transform, false);
        Stretch(shieldFill.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);
        shieldFill.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.9f);

        var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        iconGo.transform.SetParent(unit.transform, false);
        Stretch(iconGo.GetComponent<RectTransform>(), new Vector2(0.15f, 0.26f), new Vector2(0.85f, 0.73f));
        var icon = iconGo.GetComponent<Image>();
        icon.preserveAspect = true;

        // 체력바 바로 아래 레벨 별
        Text levelText = CreateText(unit.transform, "Level", "", 32, new Vector2(0f, 0.74f), new Vector2(1f, 0.85f));
        levelText.color = new Color(1f, 0.85f, 0.2f);

        Text nameText = CreateText(unit.transform, "Name", "", 30, new Vector2(0.03f, 0.04f), new Vector2(0.97f, 0.24f));

        foreach (Image image in unit.GetComponentsInChildren<Image>())
        {
            image.raycastTarget = false;
        }
        slot.GetComponent<Image>().raycastTarget = false;

        // 오른쪽 위 작은 아이템 아이콘 (처음엔 숨김)
        ItemIconView itemIcon = CreateItemIcon(unit.transform, new Vector2(0.74f, 0.6f), new Vector2(0.97f, 0.84f), 30);

        // 맞았을 때 깜빡이는 흰 판: 칸 전체를 덮도록 마지막에 만든다 (평소엔 투명)
        var hitFlash = new GameObject("HitFlash", typeof(RectTransform), typeof(Image));
        hitFlash.transform.SetParent(unit.transform, false);
        Stretch(hitFlash.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);
        var hitFlashImage = hitFlash.GetComponent<Image>();
        hitFlashImage.color = Color.clear;
        hitFlashImage.raycastTarget = false;

        var view = unit.GetComponent<CreatureUnitView>();
        var so = new SerializedObject(view);
        so.FindProperty("_icon").objectReferenceValue = icon;
        so.FindProperty("_nameText").objectReferenceValue = nameText;
        so.FindProperty("_levelText").objectReferenceValue = levelText;
        so.FindProperty("_healthFill").objectReferenceValue = healthFill.GetComponent<RectTransform>();
        so.FindProperty("_shieldFill").objectReferenceValue = shieldFill.GetComponent<RectTransform>();
        so.FindProperty("_healthTrail").objectReferenceValue = healthTrail.GetComponent<RectTransform>();
        so.FindProperty("_hitFlash").objectReferenceValue = hitFlashImage;
        so.FindProperty("_facesUp").boolValue = facesUp;
        so.FindProperty("_itemIcon").objectReferenceValue = itemIcon;
        so.ApplyModifiedPropertiesWithoutUndo();
        unit.SetActive(false);
        return view;
    }

    // 상점·배치 화면 (세로 1920 기준, 위에서부터)
    // (상단 표시: 라운드·목숨·골드) / 트레이너·새로고침 / 상점 크리처 5칸 + 아이템 1칸 / 메시지 / 시너지
    // / 팀 앞줄 2칸 / 팀 뒷줄 3칸 / 대기석 6칸 / 아이템 보관함 3칸 + 전투 시작
    private static ShopScreen BuildShopUI(GameObject screen, RunManager run, MetaProgress meta, Text goldText,
        CreatureInfoPopup infoPopup, SynergyPanel synergyPanel, out Text trainerText)
    {
        // 새로고침 버튼 왼쪽: 지금 트레이너 이름 (작게)
        trainerText = CreateText(screen.transform, "TrainerText", "", 30,
            new Vector2(0.02f, 0.875f), new Vector2(0.29f, 0.935f));
        trainerText.alignment = TextAnchor.MiddleLeft;
        trainerText.color = new Color(0.85f, 0.85f, 0.9f);

        // 제목 자리는 상단 표시가 차지한다
        screen.transform.Find("Title").gameObject.SetActive(false);
        // 전투 시작 버튼은 오른쪽 아래 (왼쪽 아래는 아이템 보관함)
        Stretch((RectTransform)screen.transform.Find("ButtonArea"), new Vector2(0.56f, 0.015f), new Vector2(0.97f, 0.115f));

        var refresh = new GameObject("RefreshButton", typeof(RectTransform), typeof(Image), typeof(Button));
        refresh.transform.SetParent(screen.transform, false);
        Stretch(refresh.GetComponent<RectTransform>(), new Vector2(0.3f, 0.87f), new Vector2(0.7f, 0.935f));
        refresh.GetComponent<Image>().color = new Color(0.4f, 0.75f, 0.95f);
        AddPressFeedback(refresh.GetComponent<Button>());
        Text refreshLabel = CreateText(refresh.transform, "Text", "새로고침", 48, Vector2.zero, Vector2.one);
        refreshLabel.color = Color.black;

        // 상점 칸: 크리처 5칸 + 아이템 1칸, 각 약 155 x 290
        Transform shopRow = CreateRow(screen.transform, "ShopSlots", new Vector2(0.03f, 0.715f), new Vector2(0.97f, 0.865f));
        var shopSlots = new CreatureSlotView[5];
        for (int i = 0; i < shopSlots.Length; i++)
        {
            shopSlots[i] = CreateSlot(shopRow, "ShopSlot" + i, "");
        }
        ItemSlotView shopItemSlot = CreateItemSlot(shopRow, "ShopItemSlot", "");
        shopItemSlot.GetComponent<Image>().color = new Color(0.32f, 0.26f, 0.4f);

        Text messageText = CreateText(screen.transform, "MessageText", "", 40,
            new Vector2(0f, 0.69f), new Vector2(1f, 0.713f));
        messageText.color = new Color(1f, 0.5f, 0.45f);

        // 팀 칸 바로 위: 지금 켜진 시너지 (눌러도 시너지 표가 열림) + 오른쪽 "시너지" 버튼
        Text synergyText = CreateText(screen.transform, "SynergyText", "", 34,
            new Vector2(0.02f, 0.665f), new Vector2(0.8f, 0.692f));
        synergyText.color = new Color(0.6f, 0.95f, 0.65f);
        synergyText.raycastTarget = true;
        Button synergyTextButton = synergyText.gameObject.AddComponent<Button>();
        synergyTextButton.targetGraphic = synergyText;
        // 시너지 줄은 높이가 약 50px라 위쪽(메시지 줄, 눌리지 않음)으로 눌리는 범위를 넓힌다 (아래는 팀 칸)
        AddHitArea(synergyText, 0f, 0f, 0f, 40f);
        Button synergyButton = CreateFreeButton(screen.transform, "시너지",
            new Vector2(0.82f, 0.664f), new Vector2(0.98f, 0.694f), new Color(0.45f, 0.75f, 0.5f));
        synergyButton.GetComponentInChildren<Text>().resizeTextMaxSize = 34;
        // 시너지 버튼 아래·위는 비어 있어(팀 앞줄은 0.2~0.8) 눌리는 범위를 약 140px로
        AddHitArea(synergyButton, 0f, 46f, 0f, 38f);

        // 팀 칸: 약 300 x 290 (가장 크게). 위가 앞줄
        Color teamColor = new Color(0.22f, 0.32f, 0.3f);
        Transform frontRow = CreateRow(screen.transform, "TeamFrontRow", new Vector2(0.2f, 0.515f), new Vector2(0.8f, 0.665f));
        Transform backRow = CreateRow(screen.transform, "TeamBackRow", new Vector2(0.03f, 0.355f), new Vector2(0.97f, 0.505f));
        var teamSlots = new CreatureSlotView[5];
        for (int i = 0; i < teamSlots.Length; i++)
        {
            bool front = i < 2;
            teamSlots[i] = CreateSlot(front ? frontRow : backRow, "TeamSlot" + i, front ? "앞줄" : "뒷줄");
            teamSlots[i].GetComponent<Image>().color = teamColor;
        }

        CreateText(screen.transform, "BenchLabel", "대기석 (끌어서 배치 / 눌러서 정보·판매)", 38,
            new Vector2(0f, 0.31f), new Vector2(1f, 0.345f));

        // 대기석 칸: 약 160 x 330
        Transform benchRow = CreateRow(screen.transform, "BenchSlots", new Vector2(0.03f, 0.13f), new Vector2(0.97f, 0.305f));
        var benchSlots = new CreatureSlotView[6];
        for (int i = 0; i < benchSlots.Length; i++)
        {
            benchSlots[i] = CreateSlot(benchRow, "BenchSlot" + i, "");
        }

        // 아이템 보관함 3칸 (왼쪽 아래): 크리처에게 끌어서 장착
        Transform itemRow = CreateRow(screen.transform, "ItemSlots", new Vector2(0.03f, 0.015f), new Vector2(0.53f, 0.115f));
        var itemSlots = new ItemSlotView[3];
        for (int i = 0; i < itemSlots.Length; i++)
        {
            itemSlots[i] = CreateItemSlot(itemRow, "ItemSlot" + i, "보관함");
        }

        // 끄는 그림은 가장 위에 그려지도록 마지막에 만든다 (아이템은 색 네모 + 글자)
        var dragIconGo = new GameObject("DragIcon", typeof(RectTransform), typeof(Image));
        dragIconGo.transform.SetParent(screen.transform, false);
        var dragRect = dragIconGo.GetComponent<RectTransform>();
        dragRect.anchorMin = dragRect.anchorMax = new Vector2(0.5f, 0.5f);
        dragRect.sizeDelta = new Vector2(200, 200);
        var dragIcon = dragIconGo.GetComponent<Image>();
        dragIcon.raycastTarget = false;
        dragIcon.preserveAspect = true;
        Text dragLabel = CreateText(dragIconGo.transform, "Label", "", 100, Vector2.zero, Vector2.one);
        dragLabel.color = Color.black;
        dragIconGo.SetActive(false);

        // 상단 골드 바로 아래: 라운드 시작 때 잠깐 보이는 "+10 +이자 2" (상단 표시 아래로 삐져나오게)
        Text goldIncomeText = CreateText(goldText.transform.parent, "GoldIncomeText", "", 38,
            new Vector2(0.7f, -1f), new Vector2(0.97f, 0f));
        goldIncomeText.alignment = TextAnchor.MiddleRight;
        goldIncomeText.color = new Color(0.55f, 1f, 0.55f);
        goldIncomeText.gameObject.SetActive(false);

        var shop = screen.AddComponent<ShopScreen>();

        // 상단 골드 글자를 누르면 골드 규칙 안내
        goldText.raycastTarget = true;
        Button goldButton = goldText.gameObject.AddComponent<Button>();
        goldButton.targetGraphic = goldText;
        UnityEventTools.AddVoidPersistentListener(goldButton.onClick, shop.ShowGoldHelp);
        // 상단 표시 높이(약 105px)보다 아래로 조금 더 눌리게
        AddHitArea(goldText, 0f, 20f, 0f, 0f);

        var so = new SerializedObject(shop);
        so.FindProperty("_config").objectReferenceValue = GetOrCreateShopConfig();
        so.FindProperty("_synergy").objectReferenceValue = GetOrCreateSynergyConfig();
        so.FindProperty("_synergyText").objectReferenceValue = synergyText;
        so.FindProperty("_infoPopup").objectReferenceValue = infoPopup;
        so.FindProperty("_synergyPanel").objectReferenceValue = synergyPanel;
        so.FindProperty("_run").objectReferenceValue = run;
        so.FindProperty("_meta").objectReferenceValue = meta;
        so.FindProperty("_goldText").objectReferenceValue = goldText;
        so.FindProperty("_goldIncomeText").objectReferenceValue = goldIncomeText;
        so.FindProperty("_messageText").objectReferenceValue = messageText;
        so.FindProperty("_refreshLabel").objectReferenceValue = refreshLabel;
        SetArray(so.FindProperty("_shopSlots"), shopSlots);
        SetArray(so.FindProperty("_benchSlots"), benchSlots);
        SetArray(so.FindProperty("_teamSlots"), teamSlots);
        so.FindProperty("_shopItemSlot").objectReferenceValue = shopItemSlot;
        SetArray(so.FindProperty("_itemSlots"), itemSlots);
        so.FindProperty("_dragIcon").objectReferenceValue = dragIcon;
        so.FindProperty("_dragLabel").objectReferenceValue = dragLabel;
        so.ApplyModifiedPropertiesWithoutUndo();

        for (int i = 0; i < benchSlots.Length; i++)
        {
            AddDragHandler(benchSlots[i], shop, SlotArea.Bench, i);
        }
        for (int i = 0; i < teamSlots.Length; i++)
        {
            AddDragHandler(teamSlots[i], shop, SlotArea.Team, i);
        }
        for (int i = 0; i < itemSlots.Length; i++)
        {
            AddDragHandler(itemSlots[i], shop, SlotArea.Items, i);
        }

        UnityEventTools.AddVoidPersistentListener(refresh.GetComponent<Button>().onClick, shop.Refresh);
        UnityEventTools.AddVoidPersistentListener(shopItemSlot.GetComponent<Button>().onClick, shop.BuyItem);
        UnityEventTools.AddVoidPersistentListener(synergyTextButton.onClick, shop.OpenSynergyPanel);
        UnityEventTools.AddVoidPersistentListener(synergyButton.onClick, shop.OpenSynergyPanel);
        // 크리처 칸을 누르면 정보 창 (구매·판매는 창의 버튼으로)
        for (int i = 0; i < shopSlots.Length; i++)
        {
            UnityEventTools.AddIntPersistentListener(shopSlots[i].GetComponent<Button>().onClick, shop.ShowShopInfo, i);
        }
        for (int i = 0; i < benchSlots.Length; i++)
        {
            UnityEventTools.AddIntPersistentListener(benchSlots[i].GetComponent<Button>().onClick, shop.ShowBenchInfo, i);
        }
        for (int i = 0; i < teamSlots.Length; i++)
        {
            UnityEventTools.AddIntPersistentListener(teamSlots[i].GetComponent<Button>().onClick, shop.ShowTeamInfo, i);
        }

        return shop;
    }

    // 시너지 수치 에셋. 없으면 기본값(GAME_DESIGN.md 8-1)으로 만들고, 있으면 그대로 쓴다
    private static SynergyConfig GetOrCreateSynergyConfig()
    {
        var config = AssetDatabase.LoadAssetAtPath<SynergyConfig>(SynergyConfigPath);
        if (config == null)
        {
            config = ScriptableObject.CreateInstance<SynergyConfig>();
            AssetDatabase.CreateAsset(config, SynergyConfigPath);
            AssetDatabase.SaveAssets();
        }
        return config;
    }

    // 상점 설정 에셋. 이미 있으면 그대로 쓴다 (조정한 값 보호).
    // 아이템 목록이 비어 있을 때만 아이템 6종을 채운다
    private static ShopConfig GetOrCreateShopConfig()
    {
        var config = AssetDatabase.LoadAssetAtPath<ShopConfig>(ShopConfigPath);
        if (config == null)
        {
            string[] guids = AssetDatabase.FindAssets("t:CreatureData", new[] { CreatureFolder });
            if (guids.Length == 0)
            {
                Debug.LogError("크리처 데이터가 없습니다. 먼저 MonsterCamp > Create Initial Creature Data 를 실행하세요.");
            }

            config = ScriptableObject.CreateInstance<ShopConfig>();
            AssetDatabase.CreateAsset(config, ShopConfigPath);

            var creatures = new Object[guids.Length];
            for (int i = 0; i < guids.Length; i++)
            {
                creatures[i] = AssetDatabase.LoadAssetAtPath<CreatureData>(AssetDatabase.GUIDToAssetPath(guids[i]));
            }
            var newSo = new SerializedObject(config);
            SetArray(newSo.FindProperty("_creaturePool"), creatures);
            newSo.ApplyModifiedPropertiesWithoutUndo();
        }

        var so = new SerializedObject(config);
        SerializedProperty itemPool = so.FindProperty("_itemPool");
        if (itemPool.arraySize == 0)
        {
            SetArray(itemPool, GetOrCreateItems());
            so.ApplyModifiedPropertiesWithoutUndo();
        }
        AssetDatabase.SaveAssets();
        return config;
    }

    // 아이템 6종 에셋. 없는 것만 기본값(GAME_DESIGN.md 9-1)으로 만든다 (조정한 값 보호)
    private static Object[] GetOrCreateItems()
    {
        Directory.CreateDirectory(ItemFolder);
        AssetDatabase.Refresh();

        return new Object[]
        {
            GetOrCreateItem("SharpClaw", "날카로운 발톱", "발", new Color(0.95f, 0.45f, 0.35f),
                "공격력 +20%", ItemEffect.AttackBonus, 0.2f),
            GetOrCreateItem("HardShell", "단단한 껍질", "껍", new Color(0.65f, 0.5f, 0.35f),
                "체력 +25%", ItemEffect.HealthBonus, 0.25f),
            GetOrCreateItem("ThunderFeather", "번개 깃털", "깃", new Color(0.55f, 0.8f, 1f),
                "공격 속도 +20%", ItemEffect.SpeedBonus, 0.2f),
            GetOrCreateItem("HealingBerry", "회복 열매", "열", new Color(0.45f, 0.85f, 0.45f),
                "전투 중 1초마다 최대 체력 3% 회복", ItemEffect.Regen, 0.03f),
            GetOrCreateItem("ThornArmor", "가시 갑옷", "가", new Color(0.7f, 0.5f, 0.9f),
                "받은 피해의 20%를 공격자에게 되돌림", ItemEffect.Thorns, 0.2f),
            GetOrCreateItem("LuckyCoin", "행운 동전", "동", new Color(1f, 0.8f, 0.2f),
                "전투에서 이기면 골드 +1 (다음 라운드)", ItemEffect.WinGold, 1f),
        };
    }

    private static ItemData GetOrCreateItem(string id, string name, string iconLabel, Color iconColor,
        string description, ItemEffect effect, float value)
    {
        string path = $"{ItemFolder}/{id}.asset";
        var item = AssetDatabase.LoadAssetAtPath<ItemData>(path);
        if (item != null)
        {
            return item;
        }

        item = ScriptableObject.CreateInstance<ItemData>();
        AssetDatabase.CreateAsset(item, path);
        var so = new SerializedObject(item);
        so.FindProperty("_displayName").stringValue = name;
        so.FindProperty("_iconLabel").stringValue = iconLabel;
        so.FindProperty("_iconColor").colorValue = iconColor;
        so.FindProperty("_description").stringValue = description;
        so.FindProperty("_effect").enumValueIndex = (int)effect;
        so.FindProperty("_value").floatValue = value;
        so.FindProperty("_price").intValue = 3;
        so.ApplyModifiedPropertiesWithoutUndo();
        return item;
    }

    private static Transform CreateRow(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(HorizontalLayoutGroup));
        go.transform.SetParent(parent, false);
        Stretch(go.GetComponent<RectTransform>(), anchorMin, anchorMax);
        var layout = go.GetComponent<HorizontalLayoutGroup>();
        layout.spacing = 16;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = true;
        return go.transform;
    }

    // 끌기 처리 (CanvasGroup은 RequireComponent로 같이 붙는다)
    private static void AddDragHandler(Component slot, ShopScreen shop, SlotArea area, int index)
    {
        var handler = slot.gameObject.AddComponent<SlotDragHandler>();
        var so = new SerializedObject(handler);
        so.FindProperty("_shop").objectReferenceValue = shop;
        so.FindProperty("_area").enumValueIndex = (int)area;
        so.FindProperty("_index").intValue = index;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // 배경 버튼 + 레벨 별 + 그림 + 이름 + 가격(또는 판매 안내) + 반짝임 덮개. 비어 있으면 emptyLabel 표시
    private static CreatureSlotView CreateSlot(Transform parent, string name, string emptyLabel)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(CreatureSlotView));
        go.transform.SetParent(parent, false);
        go.GetComponent<Image>().color = new Color(0.25f, 0.28f, 0.34f);

        Text levelText = CreateText(go.transform, "Level", "", 34, new Vector2(0f, 0.83f), new Vector2(1f, 0.98f));
        levelText.color = new Color(1f, 0.85f, 0.2f);

        var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        iconGo.transform.SetParent(go.transform, false);
        Stretch(iconGo.GetComponent<RectTransform>(), new Vector2(0.15f, 0.4f), new Vector2(0.85f, 0.83f));
        var icon = iconGo.GetComponent<Image>();
        icon.preserveAspect = true;
        icon.raycastTarget = false;

        Text nameText = CreateText(go.transform, "Name", "", 30, new Vector2(0.03f, 0.2f), new Vector2(0.97f, 0.4f));
        Text priceText = CreateText(go.transform, "Price", "", 28, new Vector2(0.03f, 0.02f), new Vector2(0.97f, 0.2f));
        priceText.color = new Color(1f, 0.85f, 0.3f);

        // 오른쪽 위 작은 아이템 아이콘 (장착했을 때만 보임)
        ItemIconView itemIcon = CreateItemIcon(go.transform, new Vector2(0.7f, 0.64f), new Vector2(0.97f, 0.86f), 30);

        // 가장 위에 그려지도록 마지막에 만든다
        var flashGo = new GameObject("Flash", typeof(RectTransform), typeof(Image));
        flashGo.transform.SetParent(go.transform, false);
        Stretch(flashGo.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);
        var flash = flashGo.GetComponent<Image>();
        flash.color = Color.clear;
        flash.raycastTarget = false;

        var view = go.GetComponent<CreatureSlotView>();
        var so = new SerializedObject(view);
        so.FindProperty("_button").objectReferenceValue = go.GetComponent<Button>();
        so.FindProperty("_icon").objectReferenceValue = icon;
        so.FindProperty("_nameText").objectReferenceValue = nameText;
        so.FindProperty("_priceText").objectReferenceValue = priceText;
        so.FindProperty("_levelText").objectReferenceValue = levelText;
        so.FindProperty("_itemIcon").objectReferenceValue = itemIcon;
        so.FindProperty("_flash").objectReferenceValue = flash;
        so.FindProperty("_emptyLabel").stringValue = emptyLabel;
        so.ApplyModifiedPropertiesWithoutUndo();
        return view;
    }

    // 아이템 칸: 배경 버튼 + 아이콘(색 네모 + 글자) + 이름 + 가격. 비어 있으면 emptyLabel 표시
    private static ItemSlotView CreateItemSlot(Transform parent, string name, string emptyLabel)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(ItemSlotView));
        go.transform.SetParent(parent, false);
        go.GetComponent<Image>().color = new Color(0.25f, 0.28f, 0.34f);

        ItemIconView icon = CreateItemIcon(go.transform, new Vector2(0.22f, 0.42f), new Vector2(0.78f, 0.92f), 64);
        Text nameText = CreateText(go.transform, "Name", "", 28, new Vector2(0.03f, 0.2f), new Vector2(0.97f, 0.42f));
        Text priceText = CreateText(go.transform, "Price", "", 28, new Vector2(0.03f, 0.02f), new Vector2(0.97f, 0.2f));
        priceText.color = new Color(1f, 0.85f, 0.3f);

        var view = go.GetComponent<ItemSlotView>();
        var so = new SerializedObject(view);
        so.FindProperty("_button").objectReferenceValue = go.GetComponent<Button>();
        so.FindProperty("_icon").objectReferenceValue = icon;
        so.FindProperty("_nameText").objectReferenceValue = nameText;
        so.FindProperty("_priceText").objectReferenceValue = priceText;
        so.FindProperty("_emptyLabel").stringValue = emptyLabel;
        so.ApplyModifiedPropertiesWithoutUndo();
        return view;
    }

    // 아이템 아이콘: 색 네모 + 한 글자 (처음엔 숨김)
    private static ItemIconView CreateItemIcon(Transform parent, Vector2 anchorMin, Vector2 anchorMax, int fontSize)
    {
        var go = new GameObject("ItemIcon", typeof(RectTransform), typeof(Image), typeof(ItemIconView));
        go.transform.SetParent(parent, false);
        Stretch(go.GetComponent<RectTransform>(), anchorMin, anchorMax);
        var background = go.GetComponent<Image>();
        background.raycastTarget = false;

        Text label = CreateText(go.transform, "Label", "", fontSize, Vector2.zero, Vector2.one);
        label.color = Color.black;

        var view = go.GetComponent<ItemIconView>();
        var so = new SerializedObject(view);
        so.FindProperty("_background").objectReferenceValue = background;
        so.FindProperty("_label").objectReferenceValue = label;
        so.ApplyModifiedPropertiesWithoutUndo();
        go.SetActive(false);
        return view;
    }

    private static Text CreateText(Transform parent, string name, string value, int size, Vector2 anchorMin, Vector2 anchorMax)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        Stretch(go.GetComponent<RectTransform>(), anchorMin, anchorMax);
        var text = go.GetComponent<Text>();
        SetupText(text, value, size);
        // 칸이 좁아도 글자가 잘리지 않게
        text.resizeTextForBestFit = true;
        text.resizeTextMinSize = 16;
        text.resizeTextMaxSize = size;
        return text;
    }

    private static void SetArray(SerializedProperty property, Object[] values)
    {
        property.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++)
        {
            property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }
    }

    private static void SetupText(Text text, string value, int size)
    {
        text.text = value;
        text.font = _font;
        text.fontSize = size;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        text.raycastTarget = false;
    }

    private static void Stretch(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
